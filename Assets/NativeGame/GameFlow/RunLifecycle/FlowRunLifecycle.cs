using System;
using UnityEngine;

namespace Echo.NativeGame.GameFlow
{
    // One instance per loaded game scene. INT binds NativeRunController.BeginAction
    // and the single GF01-02 Intro pause owner after the run has prepared.
    [DisallowMultipleComponent]
    public sealed class FlowRunLifecycle : MonoBehaviour
    {
        [SerializeField] FlowNavigation navigation;
        Func<bool> beginAction;
        Action<bool> setIntroPaused;
        bool initialized;
        bool actionBegun;
        bool beginningAction;

        public bool IsIntroOpen => initialized && !actionBegun;
        public bool HasBegunAction => actionBegun;
        public string LastError { get; private set; } = string.Empty;

        public event Action IntroOpened;
        public event Action ActionStarted;
        public event Action<string> ErrorChanged;

        // Call once for every new run, including retries. A scene reload creates a fresh gate.
        // The pause delegate must use FlowPauseReason.Intro; this class never owns Time.timeScale.
        public bool Initialize(Func<bool> beginAction, Action<bool> setIntroPaused)
        {
            if (initialized || beginAction == null || setIntroPaused == null) return false;
            if (!navigation) navigation = GetComponent<FlowNavigation>();
            this.beginAction = beginAction;
            this.setIntroPaused = setIntroPaused;
            initialized = true;
            setIntroPaused(true);
            IntroOpened?.Invoke();
            return true;
        }

        public void BeginActionFromButton() { TryBeginAction(); }

        public bool TryBeginAction()
        {
            if (!IsIntroOpen || beginningAction || navigation && navigation.IsNavigating) return false;
            bool started;
            beginningAction = true;
            try { started = beginAction(); }
            catch (Exception exception)
            {
                beginningAction = false;
                Debug.LogException(exception, this);
                SetError("本局尚未准备好。请重试开始行动。");
                return false;
            }
            beginningAction = false;
            if (!started)
            {
                SetError("本局尚未准备好。请重试开始行动。");
                return false;
            }
            actionBegun = true;
            SetError(string.Empty);
            setIntroPaused(false);
            ActionStarted?.Invoke();
            return true;
        }

        void SetError(string message)
        {
            LastError = message ?? string.Empty;
            ErrorChanged?.Invoke(LastError);
        }
    }
}
