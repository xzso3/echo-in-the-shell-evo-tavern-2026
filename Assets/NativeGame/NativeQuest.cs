using UnityEngine;
namespace Echo.NativeGame
{
    public enum NativeSideBranch { None, PrivateRecord, RouteReport }
    public enum NativeSideProgress { NotChosen, InProgress, ReadyToSubmit, Completed }
    public sealed class NativeQuest : MonoBehaviour
    {
        public NativeNarrative narrative;
        public bool Active { get; private set; }
        public bool RelayRestored { get; private set; }
        public bool BossStarted { get; private set; }
        public bool BossCleared { get; private set; }
        public bool Completed { get; private set; }
        [TextArea] public string initialObjective = "01 / 找回信号\n收集这片区域中的三段记忆。";
        [TextArea] public string completedObjective = "05 / 最终节点\n穿过东侧大门，靠近档案节点按 E。";
        public string ObjectiveText
        {
            get
            {
                if (Completed) return "06 / 信号仍在\n你选择留下的记忆，将与你同行。";
                if (!narrative) return initialObjective;
                if (narrative.MemoryCount < 3) return "01 / 找回信号   " + narrative.MemoryCount + " / 3\n私人记忆：西南。系统记录：东北。初始回声：东南。";
                if (!RelayRestored) return "02 / 重新连接\n三段记忆已收齐，返回青色终端。";
                if (!BossStarted) return "03 / 穿过封锁\n沿东侧通路前进，准备迎战。";
                if (!BossCleared) return "04 / 解除封锁\n击破外壳。核心暴露时，靠近并按 E。";
                return completedObjective;
            }
        }
        public NativeSideBranch SideBranch { get; private set; }
        public int SideSettlementCount { get; private set; }
        // Read the existing fact rather than storing a second memory or bypass flag.
        public bool HasSideEvidence => narrative && (SideBranch == NativeSideBranch.PrivateRecord ? narrative.HasMemory(NativeMemoryKind.Private) : SideBranch == NativeSideBranch.RouteReport && narrative.TookBypass);
        public NativeSideProgress SideProgress => SideBranch == NativeSideBranch.None ? NativeSideProgress.NotChosen : SideSettlementCount > 0 ? NativeSideProgress.Completed : HasSideEvidence ? NativeSideProgress.ReadyToSubmit : NativeSideProgress.InProgress;
        public string SideResult => SideBranch == NativeSideBranch.PrivateRecord
            ? "个人记录已收录：档案员为那段私人记忆保留了单独的记录位置。"
            : "路线报告已收录：档案员在本地检修图上标记了你实际走过的通道。";
        public string SideObjectiveText
        {
            get
            {
                if (SideProgress == NativeSideProgress.NotChosen) return "可选支线 / 出生点北侧的本地档案员有两份委托，可任选一项。";
                string name = SideBranch == NativeSideBranch.PrivateRecord ? "个人记录" : "路线报告";
                if (SideProgress == NativeSideProgress.Completed) return "支线已完成 / " + SideResult + " 本局已交付一次，另一委托已锁定。";
                if (SideProgress == NativeSideProgress.ReadyToSubmit) return "支线可提交 / " + name + "的条件已满足，返回本地档案员按 E 提交。";
                return SideBranch == NativeSideBranch.PrivateRecord ? "支线进行中 / 找回西南侧的私人记忆，再返回本地档案员提交个人记录。" : "支线进行中 / 实际走过上方检修通道，再返回本地档案员提交路线报告。";
            }
        }
        public bool TryChooseSideBranch(NativeSideBranch branch)
        {
            if (!Active || Completed || SideBranch != NativeSideBranch.None || (branch != NativeSideBranch.PrivateRecord && branch != NativeSideBranch.RouteReport)) return false;
            SideBranch = branch; return true;
        }
        public bool TrySubmitSideBranch()
        {
            if (!Active || Completed || SideProgress != NativeSideProgress.ReadyToSubmit) return false;
            SideSettlementCount = 1; return true;
        }
        public void Activate() { Active = true; }
        public bool CanRecover(NativeMemoryKind kind) => Active && !Completed && narrative && !narrative.HasMemory(kind);
        public bool CanRecordTerminal() => Active && narrative && narrative.MemoryCount == 3 && !RelayRestored;
        public bool RecordTerminal() { if (!CanRecordTerminal()) return false; RelayRestored = true; return true; }
        public bool CanStartBoss() => Active && RelayRestored && !BossStarted;
        public bool RecordBossStart() { if (!CanStartBoss()) return false; BossStarted = true; return true; }
        public bool RecordBossDefeat() { if (!Active || !BossStarted || BossCleared) return false; BossCleared = true; return true; }
        public bool CanLeaveSector() => Active && RelayRestored && BossCleared && narrative && narrative.MemoryCount == 3;
        public bool RecordFinalObjective() { if (!CanLeaveSector() || Completed) return false; Completed = true; return true; }
    }
}
