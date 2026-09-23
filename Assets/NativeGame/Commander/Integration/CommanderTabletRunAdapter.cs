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
        int recordTopic = -1;
        int localTopic, taskEntry = -1;
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
                OnlineAvailable = OnlineAvailable,
                LocalPage = LocalPage,
                SupportPage = SupportPage,
                RecordsPage = RecordsPage,
                TryBack = TryBack,
                ResetPresentation = ResetPresentation,
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
            recordTopic = -1;
            view.SetDecisionMode(false);
        }

        bool CanCompose()
        {
            return OnlineAvailable() && !finalDecision && phone.safeNode &&
                phone.safeNode.CanCompose(out _) && (!runtime.Session.TransportBusy || runtime.Session.Busy);
        }

        string ComposeReason()
        {
            if (!phone.safeNode) return "安全通讯节点未接线。";
            if (!phone.safeNode.CanCompose(out string reason))
                return reason ?? "请到安全通讯节点输入。";
            if (CommanderLaunchState.OfflineForCurrentRun || !OnlineConfigured())
                return "本地简报可用；本模式没有在线输入。";
            if (runtime.Session.TransportBusy && !runtime.Session.Busy)
                return "在线通道正在通讯；请等待连接测试结束。";
            return "安全节点内可发送；结果以本次在线请求为准。";
        }

        bool OnlineConfigured() => settings.HasKey &&
            !string.IsNullOrEmpty(settings.EndpointUrl) &&
            !string.IsNullOrWhiteSpace(settings.ModelId);

        bool OnlineAvailable() => !CommanderLaunchState.OfflineForCurrentRun && OnlineConfigured();

        string ConnectionStatus()
        {
            if (CommanderLaunchState.OfflineForCurrentRun) return "SHELL-LINK / 本地模式";
            if (!OnlineConfigured()) return "SHELL-LINK / 未配置 · 本地可用";
            return runtime.Session.Busy ? "SHELL-LINK / 请求中" :
                runtime.Session.TransportBusy ? "SHELL-LINK / 共享通道忙" :
                "SHELL-LINK / 已配置 · " + runtime.Session.LastOnlineStatus;
        }

        IReadOnlyList<CommanderTabletAction> CommunicationsActions()
        {
            return new List<CommanderTabletAction>
            {
                new CommanderTabletAction("任务", () => ShowLocalTopic(0), true, localTopic == 0),
                new CommanderTabletAction("记忆", () => ShowLocalTopic(1), true, localTopic == 1),
                new CommanderTabletAction("身份", () => ShowLocalTopic(2), true, localTopic == 2),
                new CommanderTabletAction("授权", () => ShowLocalTopic(3), true, localTopic == 3)
            };
        }

        void ShowLocalTopic(int topic)
        {
            localTopic = topic;
        }

        string HealthStatus()
        {
            var focused = level.hud.ActiveToolkitPlayer;
            return "生命 " + Mathf.CeilToInt(focused ? focused.Health : level.player.Health) +
                " / " + (focused ? focused.maxHealth : level.player.maxHealth);
        }

        void ResetPresentation()
        {
            localTopic = 0;
            taskEntry = recordTopic = -1;
            kind = NativeSupportKind.Medical;
            tier = NativeSupportTier.Limited;
            memory = NativeMemoryKind.Private;
            finalDecision = false;
            manualPendingVersion = 0;
            supportFeedback = recordsFeedback = string.Empty;
        }

        bool TryBack()
        {
            // Terminal/continuation back is owned by NativePhone/ResultFlow.
            if (finalDecision || level.narrative.BirthStage == NativeBirthStage.PhoneContinuation) return false;
            if (view.SelectedPage == CommanderTabletPage.Communications && localTopic == 0 && taskEntry >= 0)
            { taskEntry = -1; return true; }
            if (view.SelectedPage == CommanderTabletPage.Records && recordTopic >= 0)
            { SelectRecord(-1); return true; }
            return false;
        }

        CommanderTabletReading LocalPage()
        {
            if (localTopic != 0)
                return new CommanderTabletReading { Key = "local/" + localTopic,
                    Source = "本地预设 · 本局状态", Body = phone.CommsText(localTopic) };
            var quest = level.quest;
            var narrative = level.narrative;
            // Stage labels use facts, never parse localized ObjectiveText.
            string mainTitle = quest.Completed ? "信号仍在" : narrative.MemoryCount < 3 ? "找回信号" :
                !quest.RelayRestored ? "重新连接" : !quest.BossStarted ? "穿过封锁" :
                !quest.BossCleared ? "解除封锁" : !quest.AllRequiredLevelObjectivesCompleted ? "外来关卡目标" : "最终节点";
            string mainStatus = quest.Completed ? "已完成" : narrative.MemoryCount < 3 ?
                "进行中 · " + narrative.MemoryCount + "/3" : "进行中";
            string sideStatus = quest.SideProgress == NativeSideProgress.NotChosen ? "可选支线" :
                quest.SideProgress == NativeSideProgress.Completed ? "已完成" :
                quest.SideProgress == NativeSideProgress.ReadyToSubmit ? "可提交" : "进行中";
            var entries = new List<CommanderTabletEntry>
            {
                new CommanderTabletEntry { Title = mainTitle, Status = mainStatus,
                    Preview = ObjectivePreview(quest.ObjectiveText), Open = () => taskEntry = 0 },
                new CommanderTabletEntry { Title = "本地档案员的委托", Status = sideStatus,
                    Preview = quest.SideObjectiveText, Open = () => taskEntry = 1 },
                new CommanderTabletEntry { Title = "行动简报", Status = "本地预设",
                    Preview = "收齐三段记忆，连接中继终端，再迎战战斗机体。", Open = () => taskEntry = 2 }
            };
            if (taskEntry < 0) return new CommanderTabletReading { Key = "tasks/list", Entries = entries };
            var selected = entries[Mathf.Clamp(taskEntry, 0, entries.Count - 1)];
            return new CommanderTabletReading { Key = "tasks/" + taskEntry, Title = selected.Title,
                Source = selected.Status + " / " + (taskEntry == 2 ? "本地预设 · 本局状态" : "本局状态"),
                Body = taskEntry == 0 ? quest.ObjectiveText + "\n\n" + narrative.MemorySummary() :
                    taskEntry == 1 ? quest.SideObjectiveText : phone.CommsText(0),
                Back = () => taskEntry = -1 };
        }

        // Remove the repeated display heading only; no rule or state depends on this formatting.
        static string ObjectivePreview(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            int newline = text.IndexOf('\n');
            return newline >= 0 && newline + 1 < text.Length ? text.Substring(newline + 1).Trim() : text;
        }

        CommanderTabletReading SupportPage()
        {
            if (support == null) return new CommanderTabletReading { Key = "support", Body = "支援暂不可用。" };
            bool selectable = level.Running && !support.HasPending;
            string reason;
            bool eligible = support.CanApply(kind, tier, memory, out reason);
            if (!level.Running) reason = "当前无法使用支援。";
            else if (support.HasPending) reason = OwnsManual() ? "当前合同待确认。" : "已有其他合同待确认；请在在线通讯提案中处理。";
            else if (eligible) reason = "当前选择可用；确认合同后才执行支援。";
            return new CommanderTabletReading
            {
                Key = "support",
                Groups = new List<CommanderTabletGroup>
                {
                    new CommanderTabletGroup { Label = "支援类型", Options = new List<CommanderTabletAction>
                    {
                        new CommanderTabletAction("医疗支援", () => kind = NativeSupportKind.Medical, selectable, kind == NativeSupportKind.Medical),
                        new CommanderTabletAction("弱点解析", () => kind = NativeSupportKind.Weakpoint, selectable, kind == NativeSupportKind.Weakpoint)
                    } },
                    new CommanderTabletGroup { Label = "授权范围", Options = new List<CommanderTabletAction>
                    {
                        new CommanderTabletAction("有限授权 · +20", () => tier = NativeSupportTier.Limited, selectable, tier == NativeSupportTier.Limited),
                        new CommanderTabletAction("深度授权 · +45", () => tier = NativeSupportTier.Deep, selectable, tier == NativeSupportTier.Deep)
                    } },
                    new CommanderTabletGroup { Label = "使用记忆", Options = new List<CommanderTabletAction>
                    {
                        new CommanderTabletAction("切换记忆：" + NativeMemoryNode.KindLabel(memory),
                            () => memory = (NativeMemoryKind)(((int)memory + 1) % 3), selectable, true)
                    } }
                },
                Body = reason + "\n\n" + level.narrative.ScoreSummary() + "\n\n" +
                    (string.IsNullOrEmpty(supportFeedback) ? support.StatusText : supportFeedback),
                Footer = "确认合同后才执行支援",
                Actions = new List<CommanderTabletAction> { new CommanderTabletAction("查看授权合同", OpenManualContract, selectable && eligible) }
            };
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

        CommanderTabletReading RecordsPage()
        {
            if (finalDecision)
                return new CommanderTabletReading { Key = "records/final", Body = RecordsText(),
                    Actions = new List<CommanderTabletAction>
                    {
                        new CommanderTabletAction("写入", () => ChooseFinal(NativeFinalChoice.Upload), level.Running),
                        new CommanderTabletAction("销毁", () => ChooseFinal(NativeFinalChoice.Destroy), level.Running),
                        new CommanderTabletAction("返回记录", phone.CloseDecision)
                    } };
            if (recordTopic < 0)
                return new CommanderTabletReading { Key = "records/list", Title = "本局档案",
                    Source = "选择条目查看详情。", Entries = new List<CommanderTabletEntry>
                    {
                        RecordEntry("本局记录", 5), RecordEntry("记忆档案", 6), RecordEntry("本局行为", 7),
                        RecordEntry("同步与差异", 8), RecordEntry("初始回声", 4)
                    } };
            var narrative = level.narrative;
            var actions = new List<CommanderTabletAction>();
            string condition = string.Empty;
            if (recordTopic == 6)
            {
                condition = narrative.PreservedAnomaly ? "已保留私人记忆中的异议。" :
                    !narrative.HasMemory(NativeMemoryKind.Private) ? "找回私人记忆后可保留异议。" :
                    !level.Running ? "本局已结束，无法新增选择。" : "私人记忆：可保留异议，差异度+" + narrative.preserveDifference + "，仅计一次。";
                actions.Add(new CommanderTabletAction(narrative.PreservedAnomaly ? "已保留异议" : "保留异议 · 差异度+" + narrative.preserveDifference,
                    () => recordsFeedback = level.rules.PreserveAnomaly()
                        ? "已保留私人记忆中的异议。差异度+" + narrative.preserveDifference + "，仅计一次。" : "本次没有新增选择记录。",
                    level.Running && narrative.HasMemory(NativeMemoryKind.Private) && !narrative.PreservedAnomaly));
            }
            if (recordTopic == 4)
            {
                condition = narrative.RewroteEcho ? "本局已改写，尚未向网络发布。" :
                    !narrative.HasMemory(NativeMemoryKind.InitialEcho) ? "找回初始回声后可改写。" :
                    !level.Running ? "本局已结束，无法新增改写。" : "可改写初始回声，差异度+" + narrative.rewriteDifference + "，仅计一次。";
                actions.Add(new CommanderTabletAction(narrative.RewroteEcho ? "已改写初始回声" : "改写回声 · 差异度+" + narrative.rewriteDifference,
                    () => recordsFeedback = level.rules.RewriteEcho()
                        ? "已在本局改写初始回声。差异度+" + narrative.rewriteDifference + "，仅计一次，未向网络发布。" : "本次没有新增改写。",
                    level.Running && narrative.HasMemory(NativeMemoryKind.InitialEcho) && !narrative.RewroteEcho));
            }
            return new CommanderTabletReading { Key = "records/" + recordTopic,
                Body = RecordsText() + (string.IsNullOrEmpty(condition) ? "" : "\n\n" + condition),
                Back = narrative.BirthStage == NativeBirthStage.PhoneContinuation ? (Action)null : () => SelectRecord(-1),
                Actions = actions };
        }

        CommanderTabletEntry RecordEntry(string title, int topic) => new CommanderTabletEntry
        { Title = title, Open = () => SelectRecord(topic) };

        void SelectRecord(int topic) { recordTopic = topic; recordsFeedback = string.Empty; }

        void ChooseFinal(NativeFinalChoice choice)
        {
            if (!level.rules.ChooseFinal(choice)) recordsFeedback = "请靠近最终节点后再选择。";
        }
    }
}
