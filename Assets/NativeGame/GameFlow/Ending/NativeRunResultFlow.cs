using System;
using System.Collections.Generic;
using Echo.NativeGame.GameFlow.Results;
using UnityEngine;

namespace Echo.NativeGame.GameFlow.Ending
{
    public enum RunResultStage
    {
        None,
        EndingPresentation,
        BirthSequence,
        BirthContinuation,
        Results
    }

    // UI binds StageChanged; NativeRunController remains the sole phase and victory owner.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class NativeRunResultFlow : MonoBehaviour
    {
        public RunResultSnapshot Snapshot { get; private set; }
        public RunResultStage Stage { get; private set; }
        public bool CanViewResults => Snapshot != null &&
            (Stage == RunResultStage.EndingPresentation || Stage == RunResultStage.BirthContinuation);
        public event Action<RunResultStage, RunResultSnapshot> StageChanged;

        public bool FreezeSuccess(NativeRunController run,
            IReadOnlyList<NativeBehaviorRecord> behaviorRecords)
        {
            if (Snapshot != null || !run || run.gameObject != gameObject || behaviorRecords == null) return false;
            var captured = RunResultSnapshot.CaptureSuccess(run, behaviorRecords);
            if (captured == null) return false;
            Snapshot = captured;
            SetStage(captured.Ending == NativeEnding.Birth
                ? RunResultStage.BirthSequence : RunResultStage.EndingPresentation);
            return true;
        }

        public bool FreezeDeath(NativeRunController run,
            IReadOnlyList<NativeBehaviorRecord> behaviorRecords)
        {
            if (Snapshot != null || !run || run.gameObject != gameObject || behaviorRecords == null) return false;
            var captured = RunResultSnapshot.CaptureDeath(run, behaviorRecords);
            if (captured == null) return false;
            Snapshot = captured;
            SetStage(RunResultStage.Results);
            return true;
        }

        public bool ReachBirthContinuation(NativeNarrative narrative)
        {
            if (Stage != RunResultStage.BirthSequence || !narrative ||
                narrative.BirthStage != NativeBirthStage.PhoneContinuation) return false;
            SetStage(RunResultStage.BirthContinuation);
            return true;
        }

        public bool ViewResults()
        {
            if (!CanViewResults) return false;
            if (Stage == RunResultStage.BirthContinuation)
            {
                var run = GetComponent<NativeRunController>();
                if (run && run.hud) run.hud.ClosePhone();
                if (run && run.endingSequence && run.endingSequence.overlay)
                    run.endingSequence.overlay.SetActive(false);
            }
            SetStage(RunResultStage.Results);
            return true;
        }

        void SetStage(RunResultStage next)
        {
            Stage = next;
            StageChanged?.Invoke(next, Snapshot);
        }
    }
}
