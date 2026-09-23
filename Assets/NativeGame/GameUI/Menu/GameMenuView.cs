using System;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    public sealed class GameMenuBindings
    {
        public Func<bool> HasConfiguration;
        public Action StartGame;
        public Action QuitGame;
        public Action OpenSettings;
        public GameAiSettingsBindings Settings;
    }

    [DisallowMultipleComponent]
    public sealed class GameMenuView : MonoBehaviour
    {
        UnityEngine.UI.Image backgroundImage, cornerImage;
        UnityEngine.UI.Button startButton, settingsButton, quitButton;
        TMP_Text configurationLabel, testStatusLabel, loadingLabel, errorLabel;
        GameAiSettingsView settingsView;
        GameMenuBindings bindings;
        bool built;

        public GameAiSettingsView SettingsView => settingsView;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            backgroundImage = GameUiElements.Image("Facility Background", transform, GameUiElements.Graphite);
            var terminal = GameUiElements.Box("Menu Terminal", transform, 72, 70, 520, 580);
            GameUiElements.Image("Terminal Surface", terminal,
                new Color(0.09f, 0.16f, 0.17f, 0.88f));
            cornerImage = GameUiElements.BoxImage("Terminal Corner", transform, 57, 55, 105, 105,
                Color.clear);
            GameUiElements.BoxImage("Terminal Line", transform, 72, 70, 3, 580, GameUiElements.Line);
            GameUiElements.BoxText("Eyebrow", transform, "SHELL / PROTOCOL 01", font, 17,
                GameUiElements.Mint, 96, 82, 440, 27);
            GameUiElements.BoxText("Title", transform, "壳中回声", font, 42,
                GameUiElements.Primary, 96, 119, 450, 60);
            GameUiElements.BoxText("Subtitle", transform, "ECHO IN THE SHELL", font, 20,
                GameUiElements.Secondary, 96, 180, 450, 36);
            GameUiElements.BoxText("Lead", transform, "连接意识，进入废弃设施。", font, 20,
                GameUiElements.Primary, 96, 245, 430, 50);
            startButton = GameUiElements.BoxButton("Start Game", transform, "开始游戏", font, 22,
                GameUiElements.Mint, GameUiElements.Graphite, 96, 330, 360, 56);
            settingsButton = GameUiElements.BoxButton("AI Settings", transform, "AI 设置", font, 22,
                GameUiElements.Line, GameUiElements.Primary, 96, 400, 360, 56);
            quitButton = GameUiElements.BoxButton("Quit Game", transform, "退出游戏", font, 22,
                GameUiElements.Line, GameUiElements.Primary, 96, 470, 360, 56);
            configurationLabel = GameUiElements.BoxText("Configuration", transform, "离线模式", font, 17,
                GameUiElements.Secondary, 96, 596, 420, 24);
            testStatusLabel = GameUiElements.BoxText("Test Status", transform, "连接未测试", font, 15,
                GameUiElements.Secondary, 96, 620, 420, 24);
            loadingLabel = GameUiElements.BoxText("Loading", transform, string.Empty, font, 17,
                GameUiElements.Mint, 96, 540, 420, 24);
            errorLabel = GameUiElements.BoxText("Error", transform, string.Empty, font, 16,
                GameUiElements.Failure, 96, 558, 440, 32);
            var settingsRoot = GameUiElements.Fill("Shared AI Settings", transform);
            settingsView = settingsRoot.gameObject.AddComponent<GameAiSettingsView>();
            settingsView.Build(font);
            settingsView.ConnectionTestCompleted += result => SetTestStatus(result.Message, result.Success);
            settingsView.SettingsApplied += changed =>
            {
                RefreshConfiguration();
                if (changed) SetTestStatus(null, false);
            };
            startButton.onClick.AddListener(() => bindings?.StartGame?.Invoke());
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(() => bindings?.QuitGame?.Invoke());
        }

        public void Bind(GameMenuBindings value)
        {
            bindings = value;
            if (settingsView)
            {
                var source = value?.Settings;
                settingsView.Bind(new GameAiSettingsBindings
                {
                    Read = source?.Read,
                    Apply = source?.Apply,
                    TestConnection = source?.TestConnection,
                    CancelTest = source?.CancelTest,
                    Closed = () =>
                    {
                        source?.Closed?.Invoke();
                        RefreshConfiguration();
                    }
                });
            }
            RefreshConfiguration();
        }

        public void SetArt(Sprite background, Sprite corner, Sprite button)
        {
            GameUiElements.ApplySprite(backgroundImage, background);
            GameUiElements.ApplySprite(cornerImage, corner);
            GameUiElements.ApplyButtonSprite(startButton, button, 120);
            GameUiElements.ApplyButtonSprite(settingsButton, button, 120);
            GameUiElements.ApplyButtonSprite(quitButton, button, 120);
        }

        public void RefreshConfiguration()
        {
            if (!configurationLabel) return;
            bool configured = bindings?.HasConfiguration?.Invoke() == true;
            configurationLabel.text = configured ? "已配置 · 可选择在线进入" : "离线模式 · 无需配置即可开始";
            configurationLabel.color = configured ? GameUiElements.Mint : GameUiElements.Secondary;
        }

        public void SetTestStatus(string message, bool success)
        {
            if (!testStatusLabel) return;
            testStatusLabel.text = string.IsNullOrEmpty(message) ? "连接未测试" : message;
            testStatusLabel.color = string.IsNullOrEmpty(message) ? GameUiElements.Secondary
                : success ? GameUiElements.Mint : GameUiElements.Amber;
        }

        public void SetLoading(bool loading, string message = null)
        {
            if (!built) return;
            startButton.interactable = !loading;
            settingsButton.interactable = !loading;
            quitButton.interactable = !loading;
            loadingLabel.text = loading ? (string.IsNullOrEmpty(message) ? "正在载入游戏…" : message)
                : string.Empty;
            if (loading) errorLabel.text = string.Empty;
        }

        public void SetError(string message)
        {
            if (!built) return;
            SetLoading(false);
            errorLabel.text = message ?? string.Empty;
        }

        public bool HandleBack()
        {
            if (!settingsView || !settingsView.IsOpen) return false;
            settingsView.Close();
            return true;
        }

        void OpenSettings()
        {
            if (!settingsView) return;
            bindings?.OpenSettings?.Invoke();
            settingsView.Open();
        }
    }
}
