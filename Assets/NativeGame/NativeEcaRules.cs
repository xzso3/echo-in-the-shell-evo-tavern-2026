using System;
using UnityEngine;
namespace Echo.NativeGame
{
    // Concrete rules of this level: events -> read-only conditions -> actions on owning Unity components.
    public sealed class NativeEcaRules : MonoBehaviour
    {
        public enum PassageKind { RelayTerminal, NorthRoute }

        [Serializable]
        public sealed class PassageRule
        {
            public PassageKind kind;
            public NativeInteraction source;
            [TextArea] public string successMessage;
        }

        [Header("Event sources")]
        public NativeRunController level;
        public NativeInteraction terminal, exit;
        public NativeInteraction northRouteSwitch;
        public NativeMemoryNode[] memoryNodes = new NativeMemoryNode[0];
        public NativeRegion[] regions = new NativeRegion[0];
        [Tooltip("Assign the real NativeBossController implementing INativeBossEncounter. No placeholder victory.")]
        public MonoBehaviour bossComponent;
        public MonoBehaviour supportComponent;
        INativeSupport support;
        public INativeSupport Support => support;
        [Header("Conditions and actions")]
        public NativeQuest quest;
        public NativeMap map;
        public NativeDialogue dialogue;
        public NativeNarrative narrative;
        [TextArea] public string terminalMessage = "指挥官 / 三段记忆都保留下来了。\n东侧通路已开启。前方是测试用战斗机体，其正式身份尚未确定。";
        [Header("P2-07: two existing passage interactions")]
        [Tooltip("Configure Relay terminal and North route switch here. Empty uses the existing scene references until the one-time connector is run.")]
        public PassageRule[] passageRules = new PassageRule[0];
        PassageRule[] boundPassageRules;
        INativeBossEncounter boss;
        public bool BossConnected => bossComponent && boss != null;
        void OnEnable()
        {
            if (!level || !terminal || !exit || !quest || !map || !dialogue)
            { if (Application.isPlaying) Debug.LogError("Native ECA: missing scene references", this); return; }
            level.Started += OnLevelStarted; exit.Confirmed += OnExitConfirmed;
            boundPassageRules = passageRules != null && passageRules.Length != 0 ? passageRules : ExistingPassageRules();
            foreach (var rule in boundPassageRules)
                if (rule != null && rule.source) rule.source.Confirmed += OnPassageConfirmed;
            foreach (var memory in memoryNodes) if (memory && memory.interaction) memory.interaction.Confirmed += OnMemoryConfirmed;
            foreach (var region in regions) if (region) region.Entered += OnRegionEntered;
            boss = bossComponent as INativeBossEncounter;
            if (boss != null) boss.Defeated += OnBossDefeated;
            support = supportComponent as INativeSupport;
            if (support != null) support.Authorized += OnSupportAuthorized;
        }
        void OnDisable()
        {
            if (level) level.Started -= OnLevelStarted;
            if (exit) exit.Confirmed -= OnExitConfirmed;
            if (boundPassageRules != null)
                foreach (var rule in boundPassageRules)
                    if (rule != null && rule.source) rule.source.Confirmed -= OnPassageConfirmed;
            boundPassageRules = null;
            foreach (var memory in memoryNodes) if (memory && memory.interaction) memory.interaction.Confirmed -= OnMemoryConfirmed;
            foreach (var region in regions) if (region) region.Entered -= OnRegionEntered;
            if (boss != null) boss.Defeated -= OnBossDefeated; boss = null;
            if (support != null) support.Authorized -= OnSupportAuthorized; support = null;
        }
        void OnLevelStarted()
        {
            quest.Activate(); if (narrative) narrative.Begin();
            dialogue.Show("指挥官 / 你的信号很不稳定。\n请找回私人记忆、系统记录和初始回声。你可以突破守卫，也可以走检修通道绕行。停火不会让敌人停止攻击。");
        }
        void OnMemoryConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used || !narrative) return;
            foreach (var node in memoryNodes)
            {
                if (!node || node.interaction != source || !quest.CanRecover(node.kind)) continue;
                if (narrative.Recover(node)) { source.Consume(); dialogue.Show(node.Transmission); }
                return;
            }
        }
        PassageRule[] ExistingPassageRules()
        {
            return new[]
            {
                new PassageRule { kind = PassageKind.RelayTerminal, source = terminal, successMessage = terminalMessage },
                new PassageRule { kind = PassageKind.NorthRoute, source = northRouteSwitch,
                    successMessage = "北侧通路 / 门锁已解除。\n沿连接的两块区段向北继续，原东侧主线仍可返回。" }
            };
        }

        void OnPassageConfirmed(NativeInteraction source)
        {
            foreach (var rule in boundPassageRules)
            {
                if (rule == null || rule.source != source) continue;
                if (!level.Running || source.Used || !source.CanReach(level.player)) return;
                if (!CanOpenPassage(rule))
                {
                    if (rule.kind == PassageKind.RelayTerminal && !quest.CanRecordTerminal())
                        dialogue.Show("指挥官 / 连接需要完整的三段记忆。\n已收集 " + (narrative ? narrative.MemoryCount : 0) + " / 3。请先找到标记的记忆节点，再回来连接。");
                    return;
                }
                OpenPassage(rule);
                return;
            }
        }

        // Condition: only inspect the owning Quest and Map state.
        bool CanOpenPassage(PassageRule rule)
        {
            switch (rule.kind)
            {
                case PassageKind.RelayTerminal: return quest.CanRecordTerminal() && narrative != null;
                case PassageKind.NorthRoute: return !map.NorthRouteOpen && map.northRouteDoor != null;
                default: return false;
            }
        }

        // Action: the owners commit their own state; ECA keeps only rule wiring.
        void OpenPassage(PassageRule rule)
        {
            if (rule.kind == PassageKind.RelayTerminal)
            {
                if (!quest.RecordTerminal()) return;
                rule.source.Consume(); map.OpenExit(); narrative.RecordRelay(); dialogue.Show(rule.successMessage);
                Debug.Log("Native ECA: terminal -> Quest relay objective -> Map passage -> Dialogue", this);
            }
            else if (rule.kind == PassageKind.NorthRoute && map.OpenNorthRoute())
            {
                rule.source.Consume(); dialogue.Show(rule.successMessage);
            }
        }
        void OnRegionEntered(NativeRegion region, NativePlayer actor)
        {
            if (!level.Running || actor != level.player) return;
            if (region.purpose == NativeRegion.Purpose.ServiceBypass)
            { if (narrative) narrative.RecordBypass(); return; }
            if (!quest.CanStartBoss()) return;
            if (!BossConnected)
            { dialogue.Show("暂时无法继续\n战斗区域尚未准备就绪，本局尚未通关。请使用已完成战斗区域配置的试玩版本。"); return; }
            if (boss.IsDefeated || !quest.RecordBossStart()) return;
            map.LockArena(); boss.ActivateEncounter();
            dialogue.Show("测试战斗 / 战斗机体\n注意预警，及时躲避。击破外壳后，靠近暴露的核心按 E。错过机会也不用担心，核心会再次开放。机体的正式身份尚未确定。");
        }
        public bool CanInteractBossCore(NativePlayer actor) => level.Running && quest.BossStarted && !quest.BossCleared && BossConnected && boss.CanInteractCore(actor);
        public bool InteractBossCore(NativePlayer actor) => CanInteractBossCore(actor) && boss.InteractCore(actor);
        void OnBossDefeated()
        {
            if (!level.Running || !BossConnected || !boss.IsDefeated || !quest.RecordBossDefeat()) return;
            narrative.RecordBossDefeated(); map.ReleaseArena();
            dialogue.Show("指挥官 / 封锁已解除。\n最终档案节点就在东侧大门后。你可以留下彼此矛盾的记录，不必让系统抹平它们的差异。");
            Debug.Log("Native ECA: real Boss.Defeated -> Quest -> Narrative -> Map final gate -> Dialogue", this);
        }
        void OnSupportAuthorized(NativeSupportAuthorization authorization)
        {
            if (level.Running) narrative.RecordSupport(authorization);
        }
        public bool PreserveAnomaly() => level.Running && narrative.PreserveAnomaly();
        public bool RewriteEcho() => level.Running && narrative.RewriteEcho();
        void OnExitConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used) return;
            if (!quest.CanLeaveSector()) { dialogue.Show("最终节点 / 暂不可用\n请先收齐三段记忆，并完成核心互动。"); return; }
            level.hud.phone.ShowFinalDecision();
        }
        public bool ChooseFinal(NativeFinalChoice choice)
        {
            if (!level.Running || !quest.CanLeaveSector() || !exit.CanReach(level.player)) return false;
            if (!narrative.CommitEnding(level.combat.Kills, level.Elapsed, choice) || !quest.RecordFinalObjective()) return false;
            if (level.Complete()) exit.Consume();
            Debug.Log("Native ECA: final choice -> Quest condition -> Narrative quadrant -> Level ending", this);
            return true;
        }
    }
}
