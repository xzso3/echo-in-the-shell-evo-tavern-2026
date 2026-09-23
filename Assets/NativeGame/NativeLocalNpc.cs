using UnityEngine;
namespace Echo.NativeGame
{
    // Actor identity and availability only. No dialogue or task progress lives on the NPC.
    [RequireComponent(typeof(NativeInteraction))]
    public sealed class NativeLocalNpc : MonoBehaviour
    {
        public string displayName = "本地档案员";
        public bool available = true;
        public NativeInteraction interaction;
        public bool CanInteract => isActiveAndEnabled && available;
        public string SpeakerLabel => displayName + "（本地角色）";
    }
}
