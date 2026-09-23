using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    public interface ICommanderSupportBridge
    {
        IReadOnlyList<CommanderSupportOption> GetAvailableOptions(Guid sessionId, Guid snapshotId);
        bool TryRegister(CommanderProposal proposal, out string reason);
        bool OpenContract(Guid proposalId, out string contractText, out string reason);
        bool Accept(Guid proposalId, out string resultText);
        void Reject(Guid proposalId);
        void Cancel(Guid proposalId);
        CommanderProposalState GetState(Guid proposalId);
        event Action<Guid, CommanderProposalState, string> ProposalChanged;
    }

    // Proposal cards are local records. Only the existing Support service opens and
    // executes contracts; its Authorized event remains the sole ECA scoring path.
    public sealed class CommanderSupportBridge : MonoBehaviour, ICommanderSupportBridge
    {
        sealed class Focus
        {
            internal NativeRunController Run;
            internal NativeNarrative Narrative;
            internal NativePlayer Player;
            internal NativeHud Hud;
            internal INativeSupport Support;
            internal bool Integrated;
            internal RuntimeScope Scope;
            internal CombatPlayer CombatPlayer;
            internal NativeBossController NativeBoss;
            internal CombatBoss CombatBoss;
            internal int SceneHandle;
        }

        sealed class Snapshot
        {
            internal Guid SessionId;
            internal Focus Focus;
            internal readonly Dictionary<string, CommanderSupportOption> Options =
                new Dictionary<string, CommanderSupportOption>();
            internal IReadOnlyList<CommanderSupportOption> List;
        }

        sealed class Proposal
        {
            internal CommanderProposal Value;
            internal CommanderSupportOption Option;
            internal Focus Focus;
            internal CommanderProposalState State;
            internal long PendingVersion;
        }

        static readonly NativeSupportKind[] Kinds =
            { NativeSupportKind.Medical, NativeSupportKind.Weakpoint };
        static readonly NativeSupportTier[] Tiers =
            { NativeSupportTier.Limited, NativeSupportTier.Deep };
        static readonly NativeMemoryKind[] Memories =
            { NativeMemoryKind.Private, NativeMemoryKind.System, NativeMemoryKind.InitialEcho };
        static readonly IReadOnlyList<CommanderSupportOption> NoOptions =
            Array.Empty<CommanderSupportOption>();

        public NativeRunController level;
        public event Action<Guid, CommanderProposalState, string> ProposalChanged;

        readonly Dictionary<Guid, Snapshot> snapshots = new Dictionary<Guid, Snapshot>();
        readonly Dictionary<Guid, Proposal> proposals = new Dictionary<Guid, Proposal>();
        Guid currentSession;

        // Call on a session reset or run restart. A new GetAvailableOptions session also
        // performs this transition, so late replies from the prior session cannot register.
        public void Reset()
        {
            foreach (var entry in new List<Proposal>(proposals.Values))
            {
                if (IsTerminal(entry.State)) continue;
                CancelOwnedContract(entry);
                Change(entry, CommanderProposalState.Cancelled, "本局已重置，提案已取消。");
            }
            snapshots.Clear();
            proposals.Clear();
            currentSession = Guid.Empty;
        }

        // A configuration edit invalidates only work that has not executed. Keep
        // terminal cards and this run's chat/authorization history intact.
        public void ExpireUnexecuted(string reason)
        {
            string text = string.IsNullOrWhiteSpace(reason)
                ? "AI 设置已更新，请重新询问。" : reason;
            foreach (var entry in new List<Proposal>(proposals.Values))
            {
                if (IsTerminal(entry.State)) continue;
                CancelOwnedContract(entry);
                Change(entry, CommanderProposalState.Expired, text);
            }
            snapshots.Clear();
        }

        void OnDisable() { Reset(); }

        public IReadOnlyList<CommanderSupportOption> GetAvailableOptions(Guid sessionId, Guid snapshotId)
        {
            if (sessionId == Guid.Empty || snapshotId == Guid.Empty) return NoOptions;
            if (currentSession != sessionId)
            {
                Reset();
                currentSession = sessionId;
            }
            if (snapshots.TryGetValue(snapshotId, out Snapshot existing))
                return existing.SessionId == sessionId && SameFocus(existing.Focus) ? existing.List : NoOptions;
            if (!TryCapture(out Focus focus, out _) || focus.Support.HasPending) return NoOptions;

            var snapshot = new Snapshot { SessionId = sessionId, Focus = focus };
            var options = new List<CommanderSupportOption>();
            foreach (NativeSupportKind kind in Kinds)
                foreach (NativeSupportTier tier in Tiers)
                    foreach (NativeMemoryKind memory in Memories)
                    {
                        if (!focus.Support.CanApply(kind, tier, memory, out _)) continue;
                        var option = new CommanderSupportOption(snapshotId, Guid.NewGuid().ToString("N"),
                            kind, tier, memory);
                        options.Add(option);
                        snapshot.Options.Add(option.OptionId, option);
                    }
            snapshot.List = options.AsReadOnly();
            snapshots.Add(snapshotId, snapshot);
            return snapshot.List;
        }

        public bool TryRegister(CommanderProposal proposal, out string reason)
        {
            if (proposal.ProposalId == Guid.Empty || proposal.SessionId == Guid.Empty ||
                proposal.SessionId != currentSession || proposal.RequestGeneration < 1 ||
                proposals.ContainsKey(proposal.ProposalId) ||
                !snapshots.TryGetValue(proposal.SnapshotId, out Snapshot snapshot) ||
                snapshot.SessionId != proposal.SessionId || string.IsNullOrEmpty(proposal.OptionId) ||
                !snapshot.Options.TryGetValue(proposal.OptionId, out CommanderSupportOption option) ||
                option.Kind != proposal.Kind)
            { reason = "提案与当前请求的合法选项不匹配。"; return false; }
            if (!Validate(snapshot.Focus, option, out reason)) return false;
            var entry = new Proposal
            {
                Value = proposal, Option = option, Focus = snapshot.Focus,
                State = CommanderProposalState.Available
            };
            proposals.Add(proposal.ProposalId, entry);
            ProposalChanged?.Invoke(proposal.ProposalId, entry.State, "可查看支援合同。");
            reason = null;
            return true;
        }

        public bool OpenContract(Guid proposalId, out string contractText, out string reason)
        {
            contractText = null;
            if (!proposals.TryGetValue(proposalId, out Proposal entry))
            { reason = "提案不存在或已属于旧局。"; return false; }
            Refresh(entry);
            if (entry.State != CommanderProposalState.Available)
            { reason = "提案已" + StateLabel(entry.State) + "，请重新询问。"; return false; }
            if (!Validate(entry.Focus, entry.Option, out reason))
            { Change(entry, CommanderProposalState.Expired, reason); return false; }
            if (entry.Focus.Support.HasPending)
            { reason = "已有支援合同待处理，请先完成或关闭该合同。"; return false; }
            if (!entry.Focus.Support.Request(entry.Option.Kind, entry.Option.Tier, entry.Option.Memory))
            {
                reason = entry.Focus.Support.StatusText;
                Change(entry, CommanderProposalState.Expired, reason);
                return false;
            }
            entry.PendingVersion = entry.Focus.Support.PendingVersion;
            contractText = entry.Focus.Support.StatusText;
            reason = null;
            Change(entry, CommanderProposalState.Viewing, contractText);
            return true;
        }

        public bool Accept(Guid proposalId, out string resultText)
        {
            if (!proposals.TryGetValue(proposalId, out Proposal entry))
            { resultText = "提案不存在或已属于旧局。"; return false; }
            Refresh(entry);
            if (entry.State != CommanderProposalState.Viewing)
            { resultText = "提案已" + StateLabel(entry.State) + "，不能再次执行。"; return false; }
            if (!OwnsPending(entry))
            {
                resultText = "合同已被其他操作关闭或替换，请重新询问。";
                Change(entry, CommanderProposalState.Expired, resultText);
                return false;
            }
            if (!Validate(entry.Focus, entry.Option, out resultText))
            {
                CancelOwnedContract(entry);
                Change(entry, CommanderProposalState.Expired, resultText);
                return false;
            }
            int syncBefore = entry.Focus.Narrative.Sync;
            float healthBefore = CurrentHealth(entry.Focus);
            bool executed = entry.Focus.Support.Confirm();
            resultText = entry.Focus.Support.StatusText;
            if (executed)
            {
                int syncAfter = entry.Focus.Narrative ? entry.Focus.Narrative.Sync : syncBefore;
                float healed = CurrentHealth(entry.Focus) - healthBefore;
                resultText = "已执行 / " + NativeSupportController.KindLabel(entry.Option.Kind) +
                    " / " + NativeSupportController.TierLabel(entry.Option.Tier) +
                    "\n已共享记忆：" + NativeMemoryNode.KindLabel(entry.Option.Memory) +
                    "\n" + (entry.Option.Kind == NativeSupportKind.Medical
                        ? "实际恢复生命：" + healed.ToString("0.#") + "点。"
                        : "弱点解析已由支援执行器生效，核心窗口增加3秒。") +
                    "\n实际同步度变化：" + (syncAfter - syncBefore) +
                    "，当前同步度：" + syncAfter + "。";
                Change(entry, CommanderProposalState.Executed, resultText);
                return true;
            }
            Change(entry, CommanderProposalState.Failed, resultText);
            return false;
        }

        public void Reject(Guid proposalId) { Finish(proposalId, CommanderProposalState.Rejected, "已拒绝提案，未执行支援。"); }
        public void Cancel(Guid proposalId) { Finish(proposalId, CommanderProposalState.Cancelled, "已关闭合同，未执行支援。"); }

        public CommanderProposalState GetState(Guid proposalId)
        {
            if (!proposals.TryGetValue(proposalId, out Proposal entry)) return CommanderProposalState.Expired;
            Refresh(entry);
            return entry.State;
        }

        // Presentation may name a registered card without trusting the model's text.
        public NativeSupportKind? GetKind(Guid proposalId) =>
            proposals.TryGetValue(proposalId, out Proposal entry) ? entry.Option.Kind : (NativeSupportKind?)null;

        void Finish(Guid proposalId, CommanderProposalState state, string text)
        {
            if (!proposals.TryGetValue(proposalId, out Proposal entry) || IsTerminal(entry.State)) return;
            Refresh(entry);
            if (IsTerminal(entry.State)) return;
            CancelOwnedContract(entry);
            Change(entry, state, text);
        }

        void Update()
        {
            foreach (var entry in new List<Proposal>(proposals.Values)) Refresh(entry);
        }

        void Refresh(Proposal entry)
        {
            if (IsTerminal(entry.State)) return;
            if (!SameFocus(entry.Focus) ||
                entry.State == CommanderProposalState.Viewing && !OwnsPending(entry))
            {
                CancelOwnedContract(entry);
                Change(entry, CommanderProposalState.Expired, "当前关卡、焦点或合同已变化，请重新询问。");
            }
        }

        bool Validate(Focus focus, CommanderSupportOption option, out string reason)
        {
            if (!SameFocus(focus))
            { reason = "当前关卡或焦点目标已变化，请重新询问。"; return false; }
            if (!focus.Support.CanApply(option.Kind, option.Tier, option.Memory, out reason)) return false;
            reason = null;
            return true;
        }

        bool TryCapture(out Focus focus, out string reason)
        {
            focus = null;
            if (!isActiveAndEnabled || !level || !level.isActiveAndEnabled || !level.Running ||
                !level.gameObject.scene.isLoaded || !level.player || !level.narrative ||
                level.narrative.EndingCommitted || !level.rules || level.rules.Support == null)
            { reason = "当前无法使用支援。"; return false; }
            var hud = level.hud;
            bool integrated = hud && hud.IntegratedInput;
            if (integrated && !hud.ActiveInputReady)
            { reason = "当前关卡实例不可用。"; return false; }
            var controller = level.rules.Support as NativeSupportController;
            focus = new Focus
            {
                Run = level, Narrative = level.narrative, Player = level.player,
                Hud = hud, Support = level.rules.Support, Integrated = integrated,
                Scope = integrated ? hud.ActiveInputScope : default(RuntimeScope),
                CombatPlayer = integrated ? hud.ActiveToolkitPlayer : null,
                NativeBoss = integrated ? hud.CurrentSupportBoss() : controller ? controller.boss : null,
                CombatBoss = integrated ? hud.CurrentCombatSupportBoss() : null,
                SceneHandle = level.gameObject.scene.handle
            };
            reason = null;
            return true;
        }

        bool SameFocus(Focus original)
        {
            if (original == null || !TryCapture(out Focus current, out _)) return false;
            return original.Run == current.Run && original.Narrative == current.Narrative &&
                original.Player == current.Player && original.Hud == current.Hud &&
                ReferenceEquals(original.Support, current.Support) &&
                original.Integrated == current.Integrated && original.Scope == current.Scope &&
                original.CombatPlayer == current.CombatPlayer &&
                original.NativeBoss == current.NativeBoss && original.CombatBoss == current.CombatBoss &&
                original.SceneHandle == current.SceneHandle;
        }

        static bool OwnsPending(Proposal entry) => entry.State == CommanderProposalState.Viewing &&
            entry.Focus.Support.HasPending && entry.Focus.Support.PendingVersion == entry.PendingVersion;

        static float CurrentHealth(Focus focus) => focus.CombatPlayer
            ? focus.CombatPlayer.Health : focus.Player ? focus.Player.Health : 0;

        static void CancelOwnedContract(Proposal entry)
        {
            if (OwnsPending(entry)) entry.Focus.Support.Cancel();
        }

        void Change(Proposal entry, CommanderProposalState state, string text)
        {
            if (entry.State == state) return;
            entry.State = state;
            ProposalChanged?.Invoke(entry.Value.ProposalId, state, text);
        }

        static bool IsTerminal(CommanderProposalState state) =>
            state == CommanderProposalState.Executed || state == CommanderProposalState.Rejected ||
            state == CommanderProposalState.Cancelled || state == CommanderProposalState.Expired ||
            state == CommanderProposalState.Failed;

        static string StateLabel(CommanderProposalState state)
        {
            switch (state)
            {
                case CommanderProposalState.Viewing: return "打开";
                case CommanderProposalState.Executed: return "执行";
                case CommanderProposalState.Rejected: return "拒绝";
                case CommanderProposalState.Cancelled: return "取消";
                case CommanderProposalState.Failed: return "失败";
                default: return "失效";
            }
        }
    }
}
