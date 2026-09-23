using System;
using System.Collections.Generic;
using Echo.NativeGame.PhoneUI;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    // Maps the existing run's local actions into the native tablet. Neither model
    // text nor the view applies support, records a choice, or changes a score.
    public sealed class CommanderTabletRunAdapter
    {
        readonly NativePhone phone;
        readonly NativeRunController level;
        readonly CommanderRuntimeHost runtime;
        readonly CommanderTabletView view;
        readonly INativeSupport support;
        readonly CommanderSettings settings;
        NativeSupportKind kind;
        NativeSupportTier tier;
        NativeMemoryKind memory;
        int recordTopic;
        long manualPendingVersion;
        bool finalDecision;
        string supportFeedback = string.Empty;
        string recordsFeedback = string.Empty;

        public CommanderTabletRunAdapter(NativePhone phone, CommanderTabletView view,
            CommanderRuntimeHost runtime)
        {
            this.phone = phone;
            this.level = phone.level;
            this.view = view;
            this.runtime = runtime;
            support = level.rules.Support;
            settings = CommanderSettings.Instance;
            view.Bind(new CommanderTabletBindings
            {
                Session = runtime.Session,
                Support = runtime.SupportBridge,
                CanCompose = CanCompose,
                ComposeReason = ComposeReason,
                ConnectionStatus = ConnectionStatus,
                HealthStatus = HealthStatus,
                ProposalKind = runtime.SupportBridge.GetKind,
                CommunicationsActions = CommunicationsActions,
                SupportText = SupportText,
                SupportActions = SupportActions,
                RecordsText = RecordsText,
                RecordsActions = RecordsActions,
                CloseRequested = phone.HandleTabletCloseRequested
            });
        }

        public void ShowFinalDecision()
        {
            runtime.Session.Cancel();
            finalDecision = true;
            recordsFeedback = string.Empty;
            view.SetDecisionMode(true);
            view.RefreshExternal();
        }

        public void ShowContinuation()
        {
            runtime.Session.Cancel();
            finalDecision = false;
            view.SetDecisionMode(false);
            recordTopic = 0;
            recordsFeedback = string.Empty;
            view.SelectPage(CommanderTabletPage.Records);
            view.RefreshExternal();
        }

        public void CloseDecision()
        {
            finalDecision = false;
            view.SetDecisionMode(false);
        }

        bool CanCompose()
        {
            return phone.safeNode && phone.safeNode.CanCompose(out _);
        }

        string ComposeReason()
        {
            if (!phone.safeNode) return "安全通讯节点未接线。";
            if (!phone.safeNode.CanCompose(out string reason))
                return reason ?? "请到安全通讯节点输入。";
            if (CommanderLaunchState.OfflineForCurrentRun || !OnlineConfigured())
                return "安全节点内可使用预设主题；自由输入仅有本地反馈。";
            if (runtime.Session.TransportBusy && !runtime.Session.Busy)
                return "在线通道正在通讯；请等待连接测试结束。";
            return "安全节点内可发送；结果以本次在线请求为准。";
        }

        bool OnlineConfigured() => settings.HasKey &&
            !string.IsNullOrEmpty(settings.EndpointUrl) &&
            !string.IsNullOrWhiteSpace(settings.ModelId);

        string ConnectionStatus()
        {
            if (CommanderLaunchState.OfflineForCurrentRun) return "AI 通讯 / 离线试玩";
            if (!OnlineConfigured()) return "AI 通讯 / 未配置 · 本地可用";
            return runtime.Session.Busy ? "AI 通讯 / 等待在线回复" :
                runtime.Session.TransportBusy ? "AI 通讯 / 在线通道正在通讯" :
                "AI 通讯 / 已配置 · " + runtime.Session.LastOnlineStatus;
        }

        IReadOnlyList<CommanderTabletAction> CommunicationsActions()
        {
            return new List<CommanderTabletAction>
            {
                new CommanderTabletAction("任务 / 本地", () => ShowLocalTopic(0)),
                new CommanderTabletAction("记忆 / 本地", () => ShowLocalTopic(1)),
                new CommanderTabletAction("身份 / 本地", () => ShowLocalTopic(2)),
                new CommanderTabletAction("授权 / 本地", () => ShowLocalTopic(3))
            };
        }

        void ShowLocalTopic(int topic)
        {
            if (!level || !level.quest || !level.narrative) return;
            runtime.Session.ShowLocalTopic(phone.CommsText(topic));
        }

        string HealthStatus()
        {
            var focused = level.hud.ActiveToolkitPlayer;
            return "生命 " + Mathf.CeilToInt(focused ? focused.Health : level.player.Health) +
                " / " + (focused ? focused.maxHealth : level.player.maxHealth);
        }

        string SupportText()
        {
            if (support == null) return "支援暂不可用。";
            string selection = "手动支援 / " + NativeSupportController.KindLabel(kind) + " / " +
                NativeSupportController.TierLabel(tier) + " / 记忆：" + NativeMemoryNode.KindLabel(memory);
            string pending = support.HasPending && !OwnsManual()
                ? "\n已有其他合同待确认；请在通讯提案中处理。" : string.Empty;
            string eligibility = string.Empty;
            if (level.Running && !support.HasPending && !support.CanApply(kind, tier, memory, out string reason))
                eligibility = "\n当前选择暂不可用：" + reason;
            return selection + "\n\n" + support.StatusText + pending + eligibility +
                (string.IsNullOrEmpty(supportFeedback) ? string.Empty : "\n\n" + supportFeedback) +
                "\n\n" + level.narrative.ScoreSummary();
        }

        IReadOnlyList<CommanderTabletAction> SupportActions()
        {
            var actions = new List<CommanderTabletAction>();
            if (support == null || !level.Running) return actions;
            bool selectable = !support.HasPending;
            actions.Add(new CommanderTabletAction("医疗支援", () => kind = NativeSupportKind.Medical, selectable));
            actions.Add(new CommanderTabletAction("弱点解析", () => kind = NativeSupportKind.Weakpoint, selectable));
            actions.Add(new CommanderTabletAction("有限授权 · 同步度+20", () => tier = NativeSupportTier.Limited, selectable));
            actions.Add(new CommanderTabletAction("深度授权 · 同步度+45", () => tier = NativeSupportTier.Deep, selectable));
            actions.Add(new CommanderTabletAction("切换记忆：" + NativeMemoryNode.KindLabel(memory),
                () => memory = (NativeMemoryKind)(((int)memory + 1) % 3), selectable));
            bool canRequest = selectable && support.CanApply(kind, tier, memory, out _);
            actions.Add(new CommanderTabletAction("查看手动支援合同", OpenManualContract, canRequest));
            return actions;
        }

        bool OwnsManual() => manualPendingVersion > 0 && support != null &&
            support.HasPending && support.PendingVersion == manualPendingVersion;

        void OpenManualContract()
        {
            if (view.ContractOpen || support == null || support.HasPending) return;
            if (!support.Request(kind, tier, memory))
            { supportFeedback = support.StatusText; return; }
            manualPendingVersion = support.PendingVersion;
            supportFeedback = string.Empty;
            view.ShowManualContract(support.StatusText, AcceptManualContract, RejectManualContract);
        }

        void AcceptManualContract()
        {
            if (!OwnsManual()) supportFeedback = "合同已失效，未执行支援。";
            else
            {
                support.Confirm();
                supportFeedback = support.StatusText;
            }
            manualPendingVersion = 0;
        }

        void RejectManualContract()
        {
            if (OwnsManual()) support.Cancel();
            supportFeedback = support == null ? "合同已关闭。" : support.StatusText;
            manualPendingVersion = 0;
        }

        string RecordsText()
        {
            var narrative = level.narrative;
            string text;
            if (finalDecision)
                text = "最终节点 / 作出最后的选择\n\n" + narrative.ScoreSummary() +
                    "\n\n“写入”会将携带的记忆写入节点；“销毁”会摧毁节点。两种选择都不会改变本局已积累的同步度和差异度。" +
                    "\n\n如果离开了互动范围，请回到节点旁再选择。";
            else if (narrative.BirthStage == NativeBirthStage.PhoneContinuation && recordTopic == 0)
                text = "信号延续 / 新的声音\n\n第一拳由你发起，最后一拳出自我的意愿。我们不再只是初来时的躯壳，也不再只是引导它的声音。" +
                    "\n\n屏幕熄灭了，这条通讯仍在。全服玩家网络尚未接入，没有向其他玩家发送消息。\n\n" + narrative.ScoreSummary();
            else if (recordTopic == 6) text = "记忆档案\n\n" + narrative.MemorySummary();
            else if (recordTopic == 7) text = "本局行为记录\n\n" + narrative.BehaviorSummary();
            else if (recordTopic == 8) text = "同步与差异\n\n" + narrative.ScoreSummary();
            else if (recordTopic == 4)
                text = "初始回声 / 系统预置\n\n" + (narrative.RewroteEcho
                    ? "你在本局写下的话：我会带着这些矛盾继续前行。\n这段改写尚未发布。"
                    : "过去不必毫无矛盾，你仍可以选择带着什么继续前行。\n找回这段记忆后，即可改写。");
            else text = phone.CommsText(recordTopic);
            if (recordTopic == 7 && string.IsNullOrWhiteSpace(narrative.BehaviorSummary()))
                text = "本局行为记录\n\n尚无已记录的本局行为。";
            if (recordTopic == 5 && string.IsNullOrWhiteSpace(narrative.BehaviorSummary()))
                text += "\n\n尚无已记录的本局行为。";
            if (!finalDecision && narrative.BirthStage != NativeBirthStage.PhoneContinuation)
                text = "网络未连接 / 以下仅保留在本局\n\n" + text;
            return text + (string.IsNullOrEmpty(recordsFeedback) ? string.Empty : "\n\n" + recordsFeedback);
        }

        IReadOnlyList<CommanderTabletAction> RecordsActions()
        {
            var actions = new List<CommanderTabletAction>();
            if (finalDecision)
            {
                actions.Add(new CommanderTabletAction("写入", () => ChooseFinal(NativeFinalChoice.Upload), level.Running));
                actions.Add(new CommanderTabletAction("销毁", () => ChooseFinal(NativeFinalChoice.Destroy), level.Running));
                actions.Add(new CommanderTabletAction("返回记录", phone.CloseDecision));
                return actions;
            }
            actions.Add(new CommanderTabletAction("任务", () => SelectRecord(0)));
            actions.Add(new CommanderTabletAction("记忆", () => SelectRecord(1)));
            actions.Add(new CommanderTabletAction("你是谁？", () => SelectRecord(2)));
            actions.Add(new CommanderTabletAction("关于授权", () => SelectRecord(3)));
            actions.Add(new CommanderTabletAction("本局记录", () => SelectRecord(5)));
            actions.Add(new CommanderTabletAction("初始回声", () => SelectRecord(4)));
            actions.Add(new CommanderTabletAction("记忆档案", () => SelectRecord(6)));
            actions.Add(new CommanderTabletAction("本局行为记录", () => SelectRecord(7)));
            actions.Add(new CommanderTabletAction("同步与差异", () => SelectRecord(8)));
            var narrative = level.narrative;
            actions.Add(new CommanderTabletAction(narrative.PreservedAnomaly ? "已保留异议" : "保留异议 · 差异度+35",
                () => recordsFeedback = level.rules.PreserveAnomaly()
                    ? "已保留私人记忆中的异议。差异度+35，仅计一次。" : "本次没有新增选择记录。",
                level.Running && narrative.HasMemory(NativeMemoryKind.Private) && !narrative.PreservedAnomaly));
            actions.Add(new CommanderTabletAction(narrative.RewroteEcho ? "已改写初始回声" : "改写回声 · 差异度+35",
                () => recordsFeedback = level.rules.RewriteEcho()
                    ? "已在本局改写初始回声。差异度+35，仅计一次，未向网络发布。" : "本次没有新增改写。",
                level.Running && narrative.HasMemory(NativeMemoryKind.InitialEcho) && !narrative.RewroteEcho));
            return actions;
        }

        void SelectRecord(int topic) { recordTopic = topic; recordsFeedback = string.Empty; }

        void ChooseFinal(NativeFinalChoice choice)
        {
            if (!level.rules.ChooseFinal(choice)) recordsFeedback = "请靠近最终节点后再选择。";
        }
    }
}
