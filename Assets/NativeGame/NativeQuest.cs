using UnityEngine;
namespace Echo.NativeGame
{
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
