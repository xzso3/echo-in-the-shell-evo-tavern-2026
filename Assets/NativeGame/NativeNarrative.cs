using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Echo.NativeGame
{
    // Narrative owns collected memories and the committed story record. Quest queries it, without a second inventory.
    public sealed class NativeNarrative : MonoBehaviour
    {
        public sealed class Memory
        {
            public NativeMemoryKind Kind { get; }
            public string Title { get; }
            public string Source { get; }
            public string Text { get; }
            public Memory(NativeMemoryNode node) { Kind = node.kind; Title = node.title; Source = node.SourceLabel; Text = node.body; }
        }
        readonly List<Memory> memories = new List<Memory>();
        readonly List<string> records = new List<string>();
        readonly HashSet<string> recorded = new HashSet<string>();
        public IReadOnlyList<Memory> Memories => memories.AsReadOnly();
        public int MemoryCount => memories.Count;
        public bool TookBypass { get; private set; }
        public bool EndingCommitted { get; private set; }
        public string EndingTitle { get; private set; }
        public string EndingText { get; private set; }
        public bool HasMemory(NativeMemoryKind kind) => memories.Exists(m => m.Kind == kind);
        public void Begin() { Record("start", "Signal connected. A new shell entered the sector."); }
        public bool Recover(NativeMemoryNode node)
        {
            if (!node || HasMemory(node.kind) || EndingCommitted) return false;
            memories.Add(new Memory(node)); Record("memory_" + node.kind, node.SourceLabel + ": " + node.title); return true;
        }
        public void RecordBypass() { TookBypass = true; Record("bypass", "Used the service bypass instead of the guarded checkpoint."); }
        public void RecordRelay() { Record("relay", "Reconnected the terminal while retaining all three memory sources."); }
        public void RecordBossDefeated() { Record("boss", "Completed the exposed-core interaction in the development encounter."); }
        void Record(string key, string text) { if (recorded.Add(key)) records.Add(text); }
        public string MemorySummary()
        {
            var text = new StringBuilder("MEMORY ARCHIVE  /  " + MemoryCount + " OF 3\n");
            foreach (var memory in memories) text.Append("\n").Append(memory.Source).Append("\n").Append(memory.Title).Append("\n");
            if (memories.Count == 0) text.Append("\nNo recovered records yet. Follow the marked memory nodes.\n");
            text.Append("\nNETWORK OFFLINE\nThe echo is a system-provided seed, not another player's live message.");
            return text.ToString();
        }
        public bool CommitEnding(int kills, float elapsed)
        {
            if (EndingCommitted || MemoryCount != 3 || !recorded.Contains("boss")) return false;
            EndingCommitted = true; EndingTitle = "UNARCHIVED / SIGNAL KEPT";
            Record("final", "Chose to preserve the conflicting memories at the final node.");
            var text = new StringBuilder("The records disagree about who you were. You keep all three.\n\n");
            text.Append("COMMANDER: These memories cannot restore one original self.\nYOU: Then let them become something that was not there before.\n\n");
            text.Append("THIS RUN\nMemories kept: ").Append(MemoryCount).Append(" / 3   |   Hostiles disabled: ").Append(kills);
            text.Append("\nService bypass: ").Append(TookBypass ? "used" : "not recorded");
            text.Append("   |   Duration: ").Append(Mathf.FloorToInt(elapsed / 60)).Append(":").Append(((int)elapsed % 60).ToString("00"));
            text.Append("\nCore interaction: completed   |   Network: offline\n\nYour shell leaves the sector. The chosen memories leave with it.");
            EndingText = text.ToString(); return true;
        }
    }
}
