using UnityEngine;

namespace Echo.NativeGame.Commander
{
    public interface IPhonePauseController
    {
        bool IsPhoneOpen { get; }
        bool IsRunActive { get; }
        bool IsCombatAdvancing { get; }
        void OpenPhone();
        void ClosePhone();
        void ReleaseForRunEnd();
    }

    // Keeps the run and its actors alive so contracts can still execute while the phone is open.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class NativePhonePauseController : MonoBehaviour, IPhonePauseController
    {
        NativeRunController run;
        float timeScaleBeforePhone;
        bool hasTimeScaleSnapshot;

        public bool IsPhoneOpen { get; private set; }
        public bool IsRunActive => run && run.Running;
        public bool IsCombatAdvancing => IsRunActive && !IsPhoneOpen;

        void Awake() { run = GetComponent<NativeRunController>(); }

        public void OpenPhone()
        {
            if (IsPhoneOpen) return;
            IsPhoneOpen = true;
            if (!IsRunActive) return; // The result/continuation phone has no battle to pause.
            timeScaleBeforePhone = Time.timeScale;
            hasTimeScaleSnapshot = true;
            ClearMovementInput();
            Time.timeScale = 0f;
        }

        public void ClosePhone()
        {
            if (!IsPhoneOpen && !hasTimeScaleSnapshot) return;
            IsPhoneOpen = false;
            if (!hasTimeScaleSnapshot) return;
            hasTimeScaleSnapshot = false;
            // Keep a pause that was already in effect before the phone opened.
            if (Time.timeScale == 0f) Time.timeScale = timeScaleBeforePhone;
        }

        public void ReleaseForRunEnd() { ClosePhone(); }

        void OnDisable() { ReleaseForRunEnd(); }

        void ClearMovementInput()
        {
            if (!run) return;
            if (run.player) run.player.MoveInput = Vector2.zero;
            if (run.hud && run.hud.ActiveToolkitPlayer)
                run.hud.ActiveToolkitPlayer.MoveInput = Vector2.zero;
        }
    }
}
