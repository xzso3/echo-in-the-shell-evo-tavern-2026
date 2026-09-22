using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Echo.NativeGame
{
    public enum NativeEnding { Archive, Escape, Assimilation, Birth }
    public enum NativeFinalChoice { Destroy, Upload }
    public enum NativeBirthStage { None, AwaitFirstPunch, AutonomousPause, Blackout, PhoneContinuation }
    // The single owner of memories, meaningful behavior, authorization cost, ending eligibility and birth state.
    public sealed class NativeNarrative : MonoBehaviour
    {
        public sealed class Memory
        {
            public NativeMemoryKind Kind { get; }
            public string Title { get; }
            public string Source { get; }
            public string Text { get; }
            public Memory(NativeMemoryNode node) { Kind = node.kind; Title = node.title; Source = node.SourceLabel; Text = node.body; }
        }
        public int highSyncThreshold = 60, highDifferenceThreshold = 60;
        public int bypassDifference = 25, preserveDifference = 35, rewriteDifference = 35;
        readonly List<Memory> memories = new List<Memory>();
        readonly List<string> records = new List<string>();
        readonly HashSet<string> recorded = new HashSet<string>();
        readonly HashSet<NativeMemoryKind> shared = new HashSet<NativeMemoryKind>();
        public IReadOnlyList<Memory> Memories => memories.AsReadOnly();
        public int MemoryCount => memories.Count;
        public int Sync { get; private set; }
        public int Difference { get; private set; }
        public bool HighSync => Sync >= highSyncThreshold;
        public bool HighDifference => Difference >= highDifferenceThreshold;
        public NativeEnding EligibleEnding => HighSync ? HighDifference ? NativeEnding.Birth : NativeEnding.Assimilation : HighDifference ? NativeEnding.Escape : NativeEnding.Archive;
        public bool TookBypass { get; private set; }
        public bool PreservedAnomaly { get; private set; }
        public bool RewroteEcho { get; private set; }
        public const int NorthPulseSyncCost = 20;
        public bool CanRecordNorthPulse => !EndingCommitted && !recorded.Contains("north_pulse");
        public bool EndingCommitted { get; private set; }
        public NativeEnding Ending { get; private set; }
        public NativeFinalChoice FinalChoice { get; private set; }
        public NativeBirthStage BirthStage { get; private set; }
        public string EndingTitle { get; private set; }
        public string EndingText { get; private set; }
        public bool HasMemory(NativeMemoryKind kind) => memories.Exists(m => m.Kind == kind);
        public bool IsShared(NativeMemoryKind kind) => shared.Contains(kind);
        public void Begin() { Record("start", "以新的躯壳进入这片区域。"); }
        public bool Recover(NativeMemoryNode node)
        {
            if (!node || HasMemory(node.kind) || EndingCommitted) return false;
            memories.Add(new Memory(node)); Record("memory_" + node.kind, "找回了" + node.SourceLabel + ": " + node.title); return true;
        }
        public void RecordBypass()
        {
            if (EndingCommitted || !Record("bypass", "走过检修通道，选择了绕行路线。差异度+" + bypassDifference + "。")) return;
            TookBypass = true; Difference += bypassDifference;
        }
        public bool PreserveAnomaly()
        {
            if (EndingCommitted || !HasMemory(NativeMemoryKind.Private) || PreservedAnomaly) return false;
            PreservedAnomaly = true; Difference += preserveDifference;
            Record("preserve", "保留了私人记忆中的异议。差异度+" + preserveDifference + "。"); return true;
        }
        public bool RewriteEcho()
        {
            if (EndingCommitted || !HasMemory(NativeMemoryKind.InitialEcho) || RewroteEcho) return false;
            RewroteEcho = true; Difference += rewriteDifference;
            Record("rewrite", "改写了初始回声：“我会带着这些矛盾继续前行。”差异度+" + rewriteDifference + "。"); return true;
        }
        public bool RecordSupport(NativeSupportAuthorization authorization)
        {
            if (EndingCommitted || authorization.SyncDelta <= 0 || !HasMemory(authorization.SharedMemory) ||
                !Record("support_" + authorization.Kind, NativeSupportController.KindLabel(authorization.Kind) + "已生效；" + NativeSupportController.TierLabel(authorization.Tier) + "，共享记忆：" + NativeMemoryNode.KindLabel(authorization.SharedMemory) + "。同步度+" + authorization.SyncDelta + "。")) return false;
            Sync += authorization.SyncDelta; shared.Add(authorization.SharedMemory); return true;
        }
        public bool RecordNorthPulse()
        {
            if (!CanRecordNorthPulse || !Record("north_pulse", "北侧定向脉冲命中弧光哨兵。同步度+" + NorthPulseSyncCost + "。")) return false;
            Sync += NorthPulseSyncCost;
            return true;
        }
        public void RecordRelay() { Record("relay", "携带三段记忆，重新连接了终端。"); }
        public void RecordBossDefeated() { Record("boss", "在核心暴露时完成互动，解除了封锁。"); }
        bool Record(string key, string text) { if (!recorded.Add(key)) return false; records.Add(text); return true; }
        public string MemorySummary()
        {
            var text = new StringBuilder("记忆 / " + MemoryCount + " / 3\n");
            foreach (var memory in memories)
            {
                text.Append("\n").Append(memory.Source).Append(IsShared(memory.Kind) ? "【已共享】" : "【本局持有】").Append("\n").Append(memory.Title).Append("\n");
                if (memory.Kind == NativeMemoryKind.Private && PreservedAnomaly) text.Append("已保留异议，不交由系统抹平。\n");
                if (memory.Kind == NativeMemoryKind.InitialEcho && RewroteEcho) text.Append("本局改写：我会带着这些矛盾继续前行。\n");
            }
            if (memories.Count == 0) text.Append("\n请先找回标记的记忆。\n");
            return text.ToString();
        }
        public string BehaviorSummary() => string.Join("\n", records.ConvertAll(value => "- " + value));
        public static string EndingLabel(NativeEnding value) => value == NativeEnding.Archive ? "归档" : value == NativeEnding.Escape ? "逃逸" : value == NativeEnding.Assimilation ? "同化" : "新生";
        public string ScoreSummary() => "同步度 " + Sync + "/" + highSyncThreshold + "（高同步分界） | 差异度 " + Difference + "/" + highDifferenceThreshold + "（高差异分界）\n当前倾向：" + EndingLabel(EligibleEnding);
        public bool CommitEnding(int kills, float elapsed, NativeFinalChoice choice)
        {
            if (EndingCommitted || MemoryCount != 3 || !recorded.Contains("boss")) return false;
            Ending = EligibleEnding; FinalChoice = choice; EndingCommitted = true;
            Record("final", choice == NativeFinalChoice.Upload ? "将携带的记忆写入了最终节点。" : "销毁了最终节点，保留本局记录。");
            Record("combat", "本局击败的敌人：" + kills);
            EndingTitle = EndingLabel(Ending);
            string story = Ending == NativeEnding.Archive ? "你的躯壳被归档为稳定的作战人格。那些矛盾，仍被挡在标准模型之外。" :
                Ending == NativeEnding.Escape ? "你切断与中枢的连接，带着不完整却独立的自我离开。你选择保留的一切，将与你同行。" :
                Ending == NativeEnding.Assimilation ? "指挥官得到了一份可以预测的副本。曾让你的声音独立存在的边界，已渐渐难以辨认。" :
                "此刻作出回应的，既不是最初的你，也不是指挥官的副本。第三个声音，正在诞生。";
            EndingText = story + "\n\n" + (choice == NativeFinalChoice.Upload ? "你选择写入记忆。无论它们去往何处，你走过的路都不会因此消失。" : "你选择销毁节点。这一举动，并不会抹去已经建立的关系。") +
                "\n\n" + ScoreSummary() + "\n记忆：" + MemoryCount + "/3 | 击败敌人：" + kills + "\n用时：" + Mathf.FloorToInt(elapsed / 60) + ":" + ((int)elapsed % 60).ToString("00") + " | 网络未连接";
            if (Ending == NativeEnding.Birth) BirthStage = NativeBirthStage.AwaitFirstPunch;
            return true;
        }
        public bool FirstPunch()
        {
            if (BirthStage != NativeBirthStage.AwaitFirstPunch) return false;
            BirthStage = NativeBirthStage.AutonomousPause; Record("first_punch", "你按下 E，挥出了第一拳。"); return true;
        }
        public bool AutonomousPunch()
        {
            if (BirthStage != NativeBirthStage.AutonomousPause) return false;
            BirthStage = NativeBirthStage.Blackout; Record("last_punch", "躯壳依照自己的意愿，挥出了最后一拳。"); return true;
        }
        public bool ContinueOnPhone()
        {
            if (BirthStage != NativeBirthStage.Blackout) return false;
            BirthStage = NativeBirthStage.PhoneContinuation; Record("continuation", "屏幕熄灭后，手机中传来了新的声音。网络仍未连接。"); return true;
        }
    }
}
