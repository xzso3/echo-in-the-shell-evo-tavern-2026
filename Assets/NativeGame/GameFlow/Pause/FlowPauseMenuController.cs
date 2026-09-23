using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Echo.NativeGame.GameFlow
{
    public enum FlowPausePage { None, Menu, Settings, ReturnConfirm }

    // UI is supplied by GameUI/Pause. NativeHud forwards one Esc press here and
    // skips its old Esc branch when this method returns true.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController), typeof(FlowPauseController))]
    public sealed class FlowPauseMenuController : MonoBehaviour
    {
        NativeRunController run;
        FlowPauseController pause;
        NativeHud hud;

        public FlowPausePage Page { get; private set; }
        public event Action<FlowPausePage> PageChanged;
        public event Action TerminalPhoneBackRequested;
        public Func<bool> TryReturnToMenu;

        void Awake()
        {
            run = GetComponent<NativeRunController>();
            pause = GetComponent<FlowPauseController>();
            hud = run ? run.hud : null;
        }

        public bool HandleEscape()
        {
            // An active text field owns its first Esc. This also prevents that key
            // from closing a contract, phone, or settings panel in the same frame.
            if (BlurTextEditor()) return true;

            if (hud && hud.phonePanel && hud.phonePanel.activeSelf)
            {
                if (!run || !run.Running)
                    TerminalPhoneBackRequested?.Invoke();
                else if (hud.phone && hud.phone.tabletView &&
                    hud.phone.tabletView.gameObject.activeSelf)
                    hud.phone.tabletView.HandleBack();
                else
                    hud.ClosePhone();
                return true;
            }

            switch (Page)
            {
                case FlowPausePage.Settings: CloseSettings(); return true;
                case FlowPausePage.ReturnConfirm: CancelReturnToMenu(); return true;
                case FlowPausePage.Menu: Continue(); return true;
            }

            if (!run || !run.Running) return true; // Intro and all end stages have visible actions.
            OpenPause();
            return true;
        }

        public bool OpenPause()
        {
            if (!run || !run.Running || Page != FlowPausePage.None ||
                hud && hud.phonePanel && hud.phonePanel.activeSelf)
                return false;
            pause.SetPaused(FlowPauseReason.PauseMenu, true);
            Show(FlowPausePage.Menu);
            return true;
        }

        public void Continue()
        {
            if (Page != FlowPausePage.Menu) return;
            Show(FlowPausePage.None);
            pause.SetPaused(FlowPauseReason.PauseMenu, false);
        }

        public bool OpenSettings()
        {
            if (Page != FlowPausePage.Menu) return false;
            Show(FlowPausePage.Settings); // PauseMenu remains held.
            return true;
        }

        public void CloseSettings()
        {
            if (Page == FlowPausePage.Settings) Show(FlowPausePage.Menu);
        }

        public bool AskReturnToMenu()
        {
            if (Page != FlowPausePage.Menu) return false;
            pause.SetPaused(FlowPauseReason.ReturnConfirm, true);
            Show(FlowPausePage.ReturnConfirm);
            return true;
        }

        public void CancelReturnToMenu()
        {
            if (Page != FlowPausePage.ReturnConfirm) return;
            Show(FlowPausePage.Menu);
            pause.SetPaused(FlowPauseReason.ReturnConfirm, false);
        }

        public bool ConfirmReturnToMenu()
        {
            if (Page != FlowPausePage.ReturnConfirm || TryReturnToMenu == null) return false;
            return TryReturnToMenu(); // Navigation owns cleanup and scene loading.
        }

        public void CloseForRunEnd()
        {
            Show(FlowPausePage.None);
            if (!pause) return;
            pause.SetPaused(FlowPauseReason.ReturnConfirm, false);
            pause.SetPaused(FlowPauseReason.PauseMenu, false);
        }

        void OnDisable() { CloseForRunEnd(); }

        void Show(FlowPausePage page)
        {
            if (Page == page) return;
            Page = page;
            PageChanged?.Invoke(page);
        }

        static bool BlurTextEditor()
        {
            var events = EventSystem.current;
            if (!events || !events.currentSelectedGameObject) return false;
            var selected = events.currentSelectedGameObject;
            var tmp = selected.GetComponent<TMP_InputField>();
            if (tmp)
            {
                tmp.DeactivateInputField();
                events.SetSelectedGameObject(null);
                return true;
            }
            var legacy = selected.GetComponent<UnityEngine.UI.InputField>();
            if (!legacy) return false;
            legacy.DeactivateInputField();
            events.SetSelectedGameObject(null);
            return true;
        }
    }
}
