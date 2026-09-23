using System;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    public sealed class GamePauseBindings
    {
        public Action ContinueGame;
        public Func<bool> OpenSettings;
        public Func<bool> RequestReturnToMenu;
        public Func<bool> ConfirmReturnToMenu;
        public Action CancelReturnToMenu;
        public GameAiSettingsBindings Settings;
    }

    [DisallowMultipleComponent]
    public sealed class GamePauseView : MonoBehaviour
    {
        UnityEngine.UI.Image[] panelCorners, confirmCorners;
        UnityEngine.UI.Button continueButton, settingsButton, returnButton, cancelButton, confirmButton;
        TMP_Text returnError;
        RectTransform confirmRoot;
        GameAiSettingsView settingsView;
        GamePauseBindings bindings;
        bool built;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsReturnConfirmationOpen => confirmRoot && confirmRoot.gameObject.activeSelf;
        public bool IsSettingsOpen => settingsView && settingsView.IsOpen;
        public GameAiSettingsView SettingsView => settingsView;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            GameUiElements.Image("World Shade", transform, new Color(0.02f, 0.05f, 0.06f, 0.55f), true);
            var panel = GameUiElements.Box("Pause Panel", transform, 778, 108, 402, 504);
            GameUiElements.Image("Panel Surface", panel, GameUiElements.Panel, true);
            panelCorners = GameUiElements.PanelCorners(panel, 100);
            GameUiElements.BoxText("Eyebrow", transform, "SHELL / SESSION", font, 17,
                GameUiElements.Mint, 814, 139, 324, 27);
            GameUiElements.BoxText("Title", transform, "暂停", font, 38,
                GameUiElements.Primary, 814, 178, 324, 58);
            GameUiElements.BoxImage("Rule", transform, 814, 242, 326, 2, GameUiElements.Line);
            continueButton = GameUiElements.BoxButton("Continue", transform, "继续游戏", font, 22,
                GameUiElements.Mint, GameUiElements.Graphite, 814, 277, 326, 56);
            settingsButton = GameUiElements.BoxButton("AI Settings", transform, "AI 设置", font, 22,
                GameUiElements.Line, GameUiElements.Primary, 814, 349, 326, 56);
            returnButton = GameUiElements.BoxButton("Return To Menu", transform, "返回主菜单", font, 22,
                GameUiElements.Line, GameUiElements.Primary, 814, 421, 326, 56);
            GameUiElements.BoxText("Footer", transform, "ESC 继续", font, 16,
                GameUiElements.Secondary, 814, 558, 326, 27);
            continueButton.onClick.AddListener(() => bindings?.ContinueGame?.Invoke());
            settingsButton.onClick.AddListener(OpenSettings);
            returnButton.onClick.AddListener(RequestReturnConfirmation);

            var settingsRoot = GameUiElements.Fill("Shared AI Settings", transform);
            settingsView = settingsRoot.gameObject.AddComponent<GameAiSettingsView>();
            settingsView.Build(font);

            confirmRoot = GameUiElements.Fill("Return Confirmation", transform);
            GameUiElements.Image("Confirmation Shade", confirmRoot,
                new Color(0.02f, 0.05f, 0.06f, 0.78f), true);
            var dialog = GameUiElements.Box("Confirmation Panel", confirmRoot, 378, 235, 524, 250);
            GameUiElements.Image("Panel Surface", dialog, GameUiElements.Panel, true);
            confirmCorners = GameUiElements.PanelCorners(dialog, 88);
            GameUiElements.BoxText("Confirmation Title", confirmRoot, "返回主菜单", font, 28,
                GameUiElements.Primary, 410, 259, 460, 45);
            GameUiElements.BoxText("Confirmation Message", confirmRoot, "本局进度不会保存", font, 22,
                GameUiElements.Amber, 410, 316, 460, 47);
            returnError = GameUiElements.BoxText("Return Error", confirmRoot, string.Empty, font, 16,
                GameUiElements.Failure, 410, 370, 460, 35);
            cancelButton = GameUiElements.BoxButton("Cancel Return", confirmRoot, "取消", font, 20,
                GameUiElements.Line, GameUiElements.Primary, 410, 416, 207, 48);
            confirmButton = GameUiElements.BoxButton("Confirm Return", confirmRoot, "确认返回", font, 20,
                GameUiElements.Mint, GameUiElements.Graphite, 651, 416, 219, 48);
            cancelButton.onClick.AddListener(CancelReturnConfirmation);
            confirmButton.onClick.AddListener(ConfirmReturn);
            confirmRoot.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Bind(GamePauseBindings value)
        {
            bindings = value;
            if (!settingsView) return;
            var source = value?.Settings;
            settingsView.Bind(new GameAiSettingsBindings
            {
                Read = source?.Read,
                Apply = source?.Apply,
                TestConnection = source?.TestConnection,
                CancelTest = source?.CancelTest,
                Closed = source?.Closed
            });
        }

        public void SetArt(Sprite corner, Sprite button)
        {
            GameUiElements.ApplyCorners(panelCorners, corner);
            GameUiElements.ApplyCorners(confirmCorners, corner);
            GameUiElements.ApplyButtonSprite(continueButton, button, 108);
            GameUiElements.ApplyButtonSprite(settingsButton, button, 108);
            GameUiElements.ApplyButtonSprite(returnButton, button, 108);
        }

        public void Show()
        {
            if (!built) return;
            gameObject.SetActive(true);
            confirmRoot.gameObject.SetActive(false);
            if (settingsView.IsOpen) settingsView.Close();
            SetReturnError(string.Empty);
        }

        public void ShowSettings()
        {
            if (!IsOpen) Show();
            confirmRoot.gameObject.SetActive(false);
            if (!settingsView.IsOpen) settingsView.Open();
        }

        public void ShowPauseMenu()
        {
            if (!IsOpen) Show();
            confirmRoot.gameObject.SetActive(false);
            if (settingsView.IsOpen) settingsView.Close();
        }

        public void Hide()
        {
            if (settingsView && settingsView.IsOpen) settingsView.Close();
            if (confirmRoot) confirmRoot.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void ShowReturnConfirmation()
        {
            if (!IsOpen) Show();
            if (IsSettingsOpen) settingsView.Close();
            SetReturnError(string.Empty);
            confirmRoot.gameObject.SetActive(true);
        }

        public void CancelReturnConfirmation()
        {
            if (!IsReturnConfirmationOpen) return;
            confirmRoot.gameObject.SetActive(false);
            bindings?.CancelReturnToMenu?.Invoke();
        }

        public void SetReturnLoading(bool loading)
        {
            if (!built) return;
            confirmButton.interactable = !loading;
            cancelButton.interactable = !loading;
            if (loading) returnError.text = "正在返回主菜单…";
        }

        public void SetReturnError(string message)
        {
            if (!built) return;
            confirmButton.interactable = true;
            cancelButton.interactable = true;
            returnError.text = message ?? string.Empty;
        }

        void OpenSettings()
        {
            if (!IsOpen || IsReturnConfirmationOpen) return;
            if (bindings?.OpenSettings?.Invoke() == true) ShowSettings();
        }

        void RequestReturnConfirmation()
        {
            if (!IsOpen || IsSettingsOpen) return;
            if (bindings?.RequestReturnToMenu?.Invoke() == true) ShowReturnConfirmation();
        }

        void ConfirmReturn()
        {
            if (!IsReturnConfirmationOpen || !confirmButton.interactable) return;
            SetReturnLoading(true);
            if (bindings?.ConfirmReturnToMenu?.Invoke() != true)
            {
                if (returnError.text == "正在返回主菜单…")
                    SetReturnError("返回未能开始，请重试。");
            }
        }
    }
}
