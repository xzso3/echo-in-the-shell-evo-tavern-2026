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
        MenuBackdropAnimation backdrop;
        UnityEngine.UI.Image titleLogo, teamLogo;
        UnityEngine.UI.Button startButton, settingsButton, quitButton;
        TMP_Text configurationLabel, testStatusLabel, loadingLabel, errorLabel;
        GameAiSettingsView settingsView;
        GameMenuBindings bindings;
        bool built;
        bool startRequested;

        public GameAiSettingsView SettingsView => settingsView;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            var backdropClip = GameUiElements.Fill("Backdrop Clip", transform);
            backdropClip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var artCanvas = GameUiElements.Fill("Background Art Canvas", backdropClip);
            var aspect = artCanvas.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 1672f / 941f;
            backdrop = artCanvas.gameObject.AddComponent<MenuBackdropAnimation>();
            backdrop.Build();

            titleLogo = GameUiElements.BoxImage("ECHO IN THE SHELL Logo", transform,
                72, 74, 500, 250, Color.white);
            titleLogo.preserveAspect = true;
            titleLogo.enabled = false;
            teamLogo = GameUiElements.BoxImage("CipherWorks Logo", transform,
                970, 585, 300, 100, Color.white);
            teamLogo.preserveAspect = true;
            teamLogo.enabled = false;

            startButton = GameUiElements.BoxButton("Start Game", transform, "开始游戏", font, 22,
                GameUiElements.Graphite, GameUiElements.Primary, 100, 355, 360, 58);
            startButton.gameObject.AddComponent<GamePrimaryButtonVisual>();
            settingsButton = GameUiElements.BoxButton("AI Settings", transform, "AI 设置", font, 22,
                new Color(0.04f, 0.12f, 0.13f, 0.86f), GameUiElements.Primary,
                100, 427, 360, 54);
            quitButton = GameUiElements.BoxButton("Quit Game", transform, "退出游戏", font, 22,
                new Color(0.04f, 0.12f, 0.13f, 0.86f), GameUiElements.Primary,
                100, 493, 360, 54);
            configurationLabel = GameUiElements.BoxText("Configuration", transform, "离线模式", font, 17,
                GameUiElements.Secondary, 100, 613, 520, 25);
            testStatusLabel = GameUiElements.BoxText("Test Status", transform, "连接未测试", font, 15,
                GameUiElements.Secondary, 100, 642, 520, 42);
            loadingLabel = GameUiElements.BoxText("Loading", transform, string.Empty, font, 17,
                GameUiElements.Mint, 100, 555, 520, 25);
            errorLabel = GameUiElements.BoxText("Error", transform, string.Empty, font, 16,
                GameUiElements.Failure, 100, 578, 520, 33);
            var settingsRoot = GameUiElements.Fill("Shared AI Settings", transform);
            settingsView = settingsRoot.gameObject.AddComponent<GameAiSettingsView>();
            settingsView.Build(font);
            settingsView.ConnectionTestCompleted += result => SetTestStatus(result.Message, result.Success);
            settingsView.SettingsApplied += changed =>
            {
                RefreshConfiguration();
                if (changed) SetTestStatus(null, false);
            };
            startButton.onClick.AddListener(StartGame);
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

        public void SetArt(GameUiArtCatalog art)
        {
            if (!built) return;
            backdrop.SetArt(art);
            titleLogo.enabled = art && art.echoInTheShellLogo;
            teamLogo.enabled = art && art.cipherWorksLogo;
            if (titleLogo.enabled) GameUiElements.ApplySprite(titleLogo, art.echoInTheShellLogo);
            if (teamLogo.enabled) GameUiElements.ApplySprite(teamLogo, art.cipherWorksLogo);
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
            startRequested = loading;
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

        void StartGame()
        {
            if (startRequested || !startButton.interactable || bindings?.StartGame == null) return;
            startRequested = true;
            startButton.interactable = false;
            bindings.StartGame();
        }
    }
}
