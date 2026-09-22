using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeMap : MonoBehaviour
    {
        public GameObject exitGate;
        public bool ExitOpen { get; private set; }
        public void OpenExit()
        {
            if (ExitOpen) return;
            exitGate.SetActive(false); ExitOpen = true;
        }
    }
}
