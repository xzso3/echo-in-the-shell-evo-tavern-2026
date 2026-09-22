using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeMap : MonoBehaviour
    {
        public GameObject exitGate;
        public GameObject arenaEntryGate;
        public GameObject finalGate;
        public bool ExitOpen { get; private set; }
        public bool ArenaLocked { get; private set; }
        public bool FinalOpen { get; private set; }
        public void OpenExit()
        { if (ExitOpen) return; exitGate.SetActive(false); ExitOpen = true; }
        public void LockArena()
        { if (ArenaLocked) return; if (arenaEntryGate) arenaEntryGate.SetActive(true); ArenaLocked = true; }
        public void ReleaseArena()
        { if (arenaEntryGate) arenaEntryGate.SetActive(false); if (finalGate) finalGate.SetActive(false); ArenaLocked = false; FinalOpen = true; }
    }
}
