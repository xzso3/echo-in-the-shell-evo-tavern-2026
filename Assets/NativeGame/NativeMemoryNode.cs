using UnityEngine;
namespace Echo.NativeGame
{
    public enum NativeMemoryKind { Private, System, InitialEcho }
    public sealed class NativeMemoryNode : MonoBehaviour
    {
        public NativeMemoryKind kind;
        public NativeInteraction interaction;
        public string title;
        [TextArea(3, 8)] public string body;
        public string SourceLabel => kind == NativeMemoryKind.Private ? "PRIVATE MEMORY" : kind == NativeMemoryKind.System ? "SYSTEM RECORD" : "SYSTEM INITIAL ECHO / OFFLINE SEED";
        public string Transmission => SourceLabel + " / " + title + "\n" + body;
    }
}
