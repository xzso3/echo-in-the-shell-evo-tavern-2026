using System;
using UnityEngine;

namespace Echo.NativeGame.GameFlow
{
    [Flags]
    public enum FlowPauseReason
    {
        None = 0,
        Intro = 1,
        Phone = 2,
        PauseMenu = 4,
        ReturnConfirm = 8
    }

    // The run's only Time.timeScale owner. The existing phone pause component must
    // delegate to this component instead of keeping its own scale snapshot.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class FlowPauseController : MonoBehaviour
    {
        const FlowPauseReason AllReasons = FlowPauseReason.Intro | FlowPauseReason.Phone |
            FlowPauseReason.PauseMenu | FlowPauseReason.ReturnConfirm;

        NativeRunController run;
        FlowPauseReason reasons;
        float scaleBeforePause;
        bool ownsScale;
        bool phoneOpen;

        public FlowPauseReason Reasons => reasons;
        public bool IsPaused => reasons != FlowPauseReason.None;
        public bool IsPhoneOpen => phoneOpen;
        public bool IsRunActive => run && run.Running;
        public bool IsCombatAdvancing => IsRunActive && !IsPaused;
        public event Action<FlowPauseReason> ReasonsChanged;

        void Awake() { run = GetComponent<NativeRunController>(); }

        public bool HasReason(FlowPauseReason reason) => reason != FlowPauseReason.None &&
            (reasons & reason) == reason;

        public void SetPaused(FlowPauseReason reason, bool paused)
        {
            int bits = (int)reason;
            if (reason == FlowPauseReason.None || (reason & ~AllReasons) != 0 ||
                (bits & (bits - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(reason), "Set one pause reason at a time.");

            var next = paused ? reasons | reason : reasons & ~reason;
            if (next == reasons) return;
            if (reasons == FlowPauseReason.None && next != FlowPauseReason.None)
            {
                scaleBeforePause = Time.timeScale;
                ownsScale = true;
            }
            reasons = next;
            ClearMovementInput();
            if (reasons != FlowPauseReason.None) Time.timeScale = 0f;
            else RestoreScale();
            ReasonsChanged?.Invoke(reasons);
        }

        // A terminal/continuation phone is visible but must not stop its animation clock.
        public void OpenPhone()
        {
            if (phoneOpen) return;
            phoneOpen = true;
            if (IsRunActive) SetPaused(FlowPauseReason.Phone, true);
        }

        public void ClosePhone()
        {
            if (!phoneOpen && !HasReason(FlowPauseReason.Phone)) return;
            phoneOpen = false;
            SetPaused(FlowPauseReason.Phone, false);
        }

        public void ReleaseAllForRunEnd()
        {
            phoneOpen = false;
            if (reasons == FlowPauseReason.None) { RestoreScale(); return; }
            reasons = FlowPauseReason.None;
            ClearMovementInput();
            RestoreScale();
            ReasonsChanged?.Invoke(reasons);
        }

        public void ReleaseAllForSceneExit() => ReleaseAllForRunEnd();

        void OnDisable() { ReleaseAllForRunEnd(); }

        void RestoreScale()
        {
            if (!ownsScale) return;
            ownsScale = false;
            Time.timeScale = scaleBeforePause;
        }

        void ClearMovementInput()
        {
            if (!run) return;
            if (run.player) run.player.MoveInput = Vector2.zero;
            if (run.hud && run.hud.ActiveToolkitPlayer)
                run.hud.ActiveToolkitPlayer.MoveInput = Vector2.zero;
        }
    }
}
