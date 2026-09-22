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
        public string SourceLabel => kind == NativeMemoryKind.Private ? "私人记忆" : kind == NativeMemoryKind.System ? "系统记录" : "初始回声 / 系统预置，未联网";
        public static string KindLabel(NativeMemoryKind value) => value == NativeMemoryKind.Private ? "私人记忆" : value == NativeMemoryKind.System ? "系统记录" : value == NativeMemoryKind.InitialEcho ? "初始回声" : "未知记忆";
        public const string PrivateTitle = "窗边的那只手";
        public const string PrivateBody = "私人记忆 / 未经证实的个人片段\n雨水落在温热的玻璃上。有人握着你的手说：如果他们问你记得什么，就说那道光。可你记住的，是那只手。\n指挥官：记录里没有名字。仍然把它留下吧。";
        public const string SystemTitle = "未经同意的转移";
        public const string SystemBody = "系统记录 / 机构档案\n躯壳转移已获批准。私人情感被标记为噪声。受试者的反对意见已从摘要中删除。\n指挥官：日志写着转移成功，却没有写是谁同意的。";
        public const string EchoTitle = "你可以留下这些矛盾";
        public const string EchoBody = "初始回声 / 系统预置\n过去不必毫无矛盾，你仍可以选择带着什么继续前行。\n这是系统提供的初始留言，并非实时消息，也不是其他玩家留下的记录。";
        public string Transmission => SourceLabel + " / " + title + "\n" + body;
    }
}
