using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeMap : MonoBehaviour
    {
        public GameObject exitGate;
        public GameObject arenaEntryGate;
        public GameObject finalGate;
        [Header("North route: two instances of one reusable chunk")]
        public Transform northRouteEntry;
        public NativeChunk[] northRouteChunks = new NativeChunk[0];
        public GameObject northRouteDoor;
        public bool ExitOpen { get; private set; }
        public bool ArenaLocked { get; private set; }
        public bool FinalOpen { get; private set; }
        public bool NorthRouteOpen { get; private set; }
        void Awake()
        {
            if (northRouteChunks == null || northRouteChunks.Length == 0) return;
            if (northRouteChunks.Length < 2 || !northRouteEntry || !northRouteDoor ||
                !northRouteChunks[0] || !northRouteChunks[0].southPort ||
                Vector2.Distance(northRouteEntry.position, northRouteChunks[0].southPort.position) > .02f)
            { Debug.LogError("NativeMap: north route entrance is not connected to its first chunk.", this); return; }
            for (int i = 1; i < northRouteChunks.Length; i++)
            {
                if (!northRouteChunks[i] || !northRouteChunks[i - 1].northPort || !northRouteChunks[i].southPort ||
                    Vector2.Distance(northRouteChunks[i - 1].northPort.position, northRouteChunks[i].southPort.position) > .02f)
                    Debug.LogError("NativeMap: north route chunk ports do not meet at link " + i + ".", this);
            }
            northRouteDoor.SetActive(true);
        }
        public bool OpenNorthRoute()
        {
            if (NorthRouteOpen || !northRouteDoor) return false;
            northRouteDoor.SetActive(false);
            NorthRouteOpen = true;
            return true;
        }
        public void OpenExit()
        { if (ExitOpen) return; exitGate.SetActive(false); ExitOpen = true; }
        public void LockArena()
        { if (ArenaLocked) return; if (arenaEntryGate) arenaEntryGate.SetActive(true); ArenaLocked = true; }
        public void ReleaseArena()
        { if (arenaEntryGate) arenaEntryGate.SetActive(false); if (finalGate) finalGate.SetActive(false); ArenaLocked = false; FinalOpen = true; }
    }
}
