using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Echo.NativeGame
{
    public enum NativeEnding { Archive, Escape, Assimilation, Birth }
    public enum NativeFinalChoice { Destroy, Upload }
    public enum NativeBirthStage { None, AwaitFirstPunch, AutonomousPause, Blackout, PhoneContinuation }
    // The single owner of memories, meaningful behavior, authorization cost, ending eligibility and birth state.
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
        public int highSyncThreshold = 60, highDifferenceThreshold = 60;
        public int bypassDifference = 25, preserveDifference = 35, rewriteDifference = 35;
        readonly List<Memory> memories = new List<Memory>();
        readonly List<string> records = new List<string>();
        readonly HashSet<string> recorded = new HashSet<string>();
        readonly HashSet<NativeMemoryKind> shared = new HashSet<NativeMemoryKind>();
        public IReadOnlyList<Memory> Memories => memories.AsReadOnly();
        public int MemoryCount => memories.Count;
        public int Sync { get; private set; }
        public int Difference { get; private set; }
        public bool HighSync => Sync >= highSyncThreshold;
        public bool HighDifference => Difference >= highDifferenceThreshold;
        public NativeEnding EligibleEnding => HighSync ? HighDifference ? NativeEnding.Birth : NativeEnding.Assimilation : HighDifference ? NativeEnding.Escape : NativeEnding.Archive;
        public bool TookBypass { get; private set; }
        public bool PreservedAnomaly { get; private set; }
        public bool RewroteEcho { get; private set; }
        public bool EndingCommitted { get; private set; }
        public NativeEnding Ending { get; private set; }
        public NativeFinalChoice FinalChoice { get; private set; }
        public NativeBirthStage BirthStage { get; private set; }
        public string EndingTitle { get; private set; }
        public string EndingText { get; private set; }
        public bool HasMemory(NativeMemoryKind kind) => memories.Exists(m => m.Kind == kind);
        public bool IsShared(NativeMemoryKind kind) => shared.Contains(kind);
        public void Begin() { Record("start", "Entered the sector with a new shell."); }
        public bool Recover(NativeMemoryNode node)
        {
            if (!node || HasMemory(node.kind) || EndingCommitted) return false;
            memories.Add(new Memory(node)); Record("memory_" + node.kind, "Recovered " + node.SourceLabel + ": " + node.title); return true;
        }
        public void RecordBypass()
        {
            if (EndingCommitted || !Record("bypass", "Crossed the physical service path (not a kill-free claim). +" + bypassDifference + " difference")) return;
            TookBypass = true; Difference += bypassDifference;
        }
        public bool PreserveAnomaly()
        {
            if (EndingCommitted || !HasMemory(NativeMemoryKind.Private) || PreservedAnomaly) return false;
            PreservedAnomaly = true; Difference += preserveDifference;
            Record("preserve", "Kept the private objection outside the normalized archive. +" + preserveDifference + " difference"); return true;
        }
        public bool RewriteEcho()
        {
            if (EndingCommitted || !HasMemory(NativeMemoryKind.InitialEcho) || RewroteEcho) return false;
            RewroteEcho = true; Difference += rewriteDifference;
            Record("rewrite", "Rewrote the offline seed: I will carry the contradiction. +" + rewriteDifference + " difference"); return true;
        }
        public bool RecordSupport(NativeSupportAuthorization authorization)
        {
            if (EndingCommitted || authorization.SyncDelta <= 0 || !HasMemory(authorization.SharedMemory) ||
                !Record("support_" + authorization.Kind, authorization.Kind + " applied; " + authorization.Tier + " authorization shared " + authorization.SharedMemory + ". +" + authorization.SyncDelta + " sync")) return false;
            Sync += authorization.SyncDelta; shared.Add(authorization.SharedMemory); return true;
        }
        public void RecordRelay() { Record("relay", "Reconnected the terminal with all three sources."); }
        public void RecordBossDefeated() { Record("boss", "Completed the exposed-core interaction."); }
        bool Record(string key, string text) { if (!recorded.Add(key)) return false; records.Add(text); return true; }
        public string MemorySummary()
        {
            var text = new StringBuilder("MEMORIES / " + MemoryCount + " OF 3\n");
            foreach (var memory in memories) text.Append("\n").Append(memory.Source).Append(IsShared(memory.Kind) ? " [SHARED]" : " [LOCAL]").Append("\n").Append(memory.Title).Append("\n");
            if (memories.Count == 0) text.Append("\nRecover the marked records first.\n");
            return text.ToString();
        }
        public string BehaviorSummary() => string.Join("\n", records.ConvertAll(value => "- " + value));
        public string ScoreSummary() => "SYNC " + Sync + "/" + highSyncThreshold + " high threshold   |   DIFFERENCE " + Difference + "/" + highDifferenceThreshold + "\nCurrent quadrant: " + EligibleEnding.ToString().ToUpperInvariant();
        public bool CommitEnding(int kills, float elapsed, NativeFinalChoice choice)
        {
            if (EndingCommitted || MemoryCount != 3 || !recorded.Contains("boss")) return false;
            Ending = EligibleEnding; FinalChoice = choice; EndingCommitted = true;
            Record("final", choice == NativeFinalChoice.Upload ? "Wrote the carried records into the final node." : "Destroyed the final node; retained the local run record.");
            Record("combat", "Hostiles disabled by actual combat: " + kills);
            EndingTitle = Ending.ToString().ToUpperInvariant();
            string story = Ending == NativeEnding.Archive ? "Your shell is filed as a stable operational personality. The contradiction remains outside its standard model." :
                Ending == NativeEnding.Escape ? "You disconnect from the center and leave incomplete, but independent. What you chose to keep travels with you." :
                Ending == NativeEnding.Assimilation ? "The commander receives a predictable copy. The boundary that held your voice apart becomes difficult to find." :
                "Neither your original self nor the commander's copy can explain what answers now. A third voice begins.";
            EndingText = story + "\n\n" + (choice == NativeFinalChoice.Upload ? "You chose to write the records. Their destination does not erase how you arrived." : "You chose to destroy the node. The act does not erase the relationship already formed.") +
                "\n\n" + ScoreSummary() + "\nMemories: " + MemoryCount + "/3 | Hostiles disabled: " + kills + "\nDuration: " + Mathf.FloorToInt(elapsed / 60) + ":" + ((int)elapsed % 60).ToString("00") + " | Network: offline";
            if (Ending == NativeEnding.Birth) BirthStage = NativeBirthStage.AwaitFirstPunch;
            return true;
        }
        public bool FirstPunch()
        {
            if (BirthStage != NativeBirthStage.AwaitFirstPunch) return false;
            BirthStage = NativeBirthStage.AutonomousPause; Record("first_punch", "Player pressed E for the first punch."); return true;
        }
        public bool AutonomousPunch()
        {
            if (BirthStage != NativeBirthStage.AutonomousPause) return false;
            BirthStage = NativeBirthStage.Blackout; Record("last_punch", "The shell delivered its own last punch without player input."); return true;
        }
        public bool ContinueOnPhone()
        {
            if (BirthStage != NativeBirthStage.Blackout) return false;
            BirthStage = NativeBirthStage.PhoneContinuation; Record("continuation", "After the blackout, the local phone received the new voice. Network remains offline."); return true;
        }
    }
}
