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
        [TextArea] public string initialObjective = "01 / RECOVER YOUR SIGNAL\nFind the three memory records in this sector.";
        [TextArea] public string completedObjective = "05 / KEEP THE SIGNAL\nReach the final node beyond the eastern gate. Press E.";
        public string ObjectiveText
        {
            get
            {
                if (Completed) return "06 / SIGNAL KEPT\nThe chosen memories leave with you.";
                if (!narrative) return initialObjective;
                if (narrative.MemoryCount < 3) return "01 / RECOVER YOUR SIGNAL   " + narrative.MemoryCount + " / 3\nPrivate: southwest. System: northeast. Initial echo: southeast.";
                if (!RelayRestored) return "02 / RECONNECT\nAll memories recovered. Return to the cyan terminal.";
                if (!BossStarted) return "03 / CROSS THE THRESHOLD\nFollow the east passage to the development encounter.";
                if (!BossCleared) return "04 / BREAK THE LOCK\nSurvive the encounter. Use E when its core is exposed.";
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
