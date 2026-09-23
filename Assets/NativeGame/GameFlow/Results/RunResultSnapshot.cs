using System;
using System.Collections.Generic;
using Echo.NativeGame.ToolkitIntegration.LevelHost;
using UnityEngine;

namespace Echo.NativeGame.GameFlow.Results
{
    // One terminal read of the existing run owners. This is display data, not a second story state.
    public sealed class RunResultSnapshot
    {
        public Guid RunId { get; }
        public bool Success { get; }
        public NativeEnding? Ending { get; }
        public string Title { get; }
        public string Summary { get; }
        public string PresentationText { get; }
        public float ElapsedSeconds { get; }
        public int Kills { get; }
        public int Sync { get; }
        public int Difference { get; }
        public IReadOnlyList<string> KeyEvents { get; }

        RunResultSnapshot(Guid runId, bool success, NativeEnding? ending, string title,
            string summary, string presentationText, float elapsedSeconds, int kills,
            int sync, int difference, IReadOnlyList<string> keyEvents)
        {
            RunId = runId;
            Success = success;
            Ending = ending;
            Title = title;
            Summary = summary;
            PresentationText = presentationText;
            ElapsedSeconds = elapsedSeconds;
            Kills = kills;
            Sync = sync;
            Difference = difference;
            KeyEvents = keyEvents;
        }

        public static RunResultSnapshot CaptureSuccess(NativeRunController run,
            IReadOnlyList<NativeBehaviorRecord> behaviorRecords)
        {
            if (!run || !run.quest || !run.quest.Completed || !run.narrative ||
                !run.narrative.EndingCommitted) return null;

            string story = run.narrative.EndingText ?? string.Empty;
            int firstBreak = story.IndexOf("\n\n", StringComparison.Ordinal);
            string summary = firstBreak < 0 ? story : story.Substring(0, firstBreak);
            int secondBreak = firstBreak < 0 ? -1 : story.IndexOf("\n\n", firstBreak + 2, StringComparison.Ordinal);
            string presentation = secondBreak < 0 ? story : story.Substring(0, secondBreak);
            return Capture(run, true, run.narrative.Ending, run.narrative.EndingTitle,
                summary, presentation, behaviorRecords);
        }

        public static RunResultSnapshot CaptureDeath(NativeRunController run,
            IReadOnlyList<NativeBehaviorRecord> behaviorRecords)
        {
            if (!run || !run.narrative) return null;
            const string summary = "本局意识连接已中断。";
            return Capture(run, false, null, "意识涣散", summary, summary, behaviorRecords);
        }

        static RunResultSnapshot Capture(NativeRunController run, bool succeeded,
            NativeEnding? ending, string title, string summary, string presentation,
            IReadOnlyList<NativeBehaviorRecord> behaviorRecords)
        {
            string rawId = run.combatIntegration ? run.combatIntegration.CurrentRunId.ToString() : string.Empty;
            if (rawId.Length == 0)
            {
                var host = run.GetComponent<NativeLevelHost>();
                if (host) rawId = host.RunId.ToString();
            }
            if (!Guid.TryParseExact(rawId, "N", out Guid id)) id = Guid.NewGuid();

            var focusedWeapon = run.hud ? run.hud.ActiveToolkitWeapon : null;
            int kills = focusedWeapon ? focusedWeapon.Kills : run.combat ? run.combat.Kills : 0;
            return new RunResultSnapshot(id, succeeded, ending, title ?? string.Empty,
                summary ?? string.Empty, presentation ?? string.Empty,
                Mathf.Max(0f, run.Elapsed), Mathf.Max(0, kills), run.narrative.Sync,
                run.narrative.Difference, KeyBehaviorSelector.Select(behaviorRecords));
        }
    }
}
