using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeQuest : MonoBehaviour
    {
        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        [TextArea] public string initialObjective = "01 / RECONNECT\nReach the cyan terminal. Press E nearby to open the exit.";
        [TextArea] public string completedObjective = "02 / SIGNAL RESTORED\nThe eastern gate is open. Reach the exit and press E.";
        public string ObjectiveText => Completed ? completedObjective : initialObjective;
        public void Activate() { Active = true; }
        // ECA conditions are queries. Only this module commits objective progress.
        public bool CanRecordTerminal() => Active && !Completed;
        public bool CanLeaveSector() => Active && Completed;
        public bool RecordTerminal()
        { if (!CanRecordTerminal()) return false; Completed = true; return true; }
    }
}
