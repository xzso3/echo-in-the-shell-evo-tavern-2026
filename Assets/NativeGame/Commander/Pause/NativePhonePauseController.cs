using Echo.NativeGame.GameFlow;
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

    // Compatibility surface for existing phone and combat callers. GF01's flow pause
    // component is the only owner of Time.timeScale and all pause reasons.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class NativePhonePauseController : MonoBehaviour, IPhonePauseController
    {
        FlowPauseController pause;
        FlowPauseController Pause
        {
            get
            {
                if (!pause) pause = GetComponent<FlowPauseController>();
                if (!pause) pause = gameObject.AddComponent<FlowPauseController>();
                return pause;
            }
        }

        public bool IsPhoneOpen => Pause.IsPhoneOpen;
        public bool IsRunActive => Pause.IsRunActive;
        public bool IsCombatAdvancing => Pause.IsCombatAdvancing;

        void Awake() { _ = Pause; }
        public void OpenPhone() { Pause.OpenPhone(); }
        public void ClosePhone() { Pause.ClosePhone(); }
        public void ReleaseForRunEnd() { Pause.ReleaseAllForRunEnd(); }
        void OnDisable() { if (pause) pause.ReleaseAllForRunEnd(); }
    }
}
