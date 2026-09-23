using System;
using Echo.NativeGame.Commander;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.NativeGame.GameFlow
{
    public enum FlowDestination { Game, Menu }

    // Scene-owned navigation. Every path out of a run uses the same lock and cleanup.
    // UI may bind the public button methods or use TryLoad directly.
    [DisallowMultipleComponent]
    public sealed class FlowNavigation : MonoBehaviour
    {
        [SerializeField] NativeRunController run;
        [SerializeField] string preferredGameSceneName = "NativeDemoTilemap";
        [SerializeField] string fallbackGameSceneName = "NativeDemo";
        [SerializeField] string menuSceneName = "CommanderHome";

        CommanderSettings settings;

        public bool IsNavigating { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public string PreferredGameSceneName
        {
            get => preferredGameSceneName;
            set { if (!string.IsNullOrWhiteSpace(value)) preferredGameSceneName = value; }
        }
        public bool AiConfigured => settings != null && settings.HasKey &&
            !string.IsNullOrEmpty(settings.EndpointUrl) && !string.IsNullOrWhiteSpace(settings.ModelId);
        // Configuration says nothing about connection health. Test status belongs to the settings view.
        public string AiConfigurationLabel => AiConfigured ? "已配置" : "离线模式";

        public event Action<bool> NavigatingChanged;
        public event Action<string> ErrorChanged;
        public event Action AiConfigurationChanged;
        public event Action AiSettingsRequested;

        void Awake()
        {
            if (!run) run = GetComponent<NativeRunController>();
            settings = CommanderSettings.Instance;
            settings.Changed += OnSettingsChanged;
        }

        void OnDestroy()
        {
            if (settings != null) settings.Changed -= OnSettingsChanged;
        }

        void OnSettingsChanged() => AiConfigurationChanged?.Invoke();

        // For CommanderHomeBootstrap's existing EnterGame(bool offline) binding.
        // Missing configuration remains a playable offline entry.
        public bool TryStartGame(bool offline)
        {
            if (IsNavigating) return false;
            bool previous = CommanderLaunchState.OfflineForCurrentRun;
            CommanderLaunchState.OfflineForCurrentRun = offline || !AiConfigured;
            if (TryLoad(FlowDestination.Game)) return true;
            CommanderLaunchState.OfflineForCurrentRun = previous;
            return false;
        }

        public void StartGame() { TryStartGame(false); }
        public void StartOffline() { TryStartGame(true); }
        public void RetryRun() { TryLoad(FlowDestination.Game); }
        public void ReturnToMenu() { TryLoad(FlowDestination.Menu); }
        public void OpenAiSettings() { AiSettingsRequested?.Invoke(); }

        public void QuitApplication()
        {
            if (IsNavigating || SceneManager.GetActiveScene().name != menuSceneName) return;
#if UNITY_EDITOR
            SetError("在 Unity 编辑器中请停止 Play 模式。");
#else
            Application.Quit();
#endif
        }

        public bool TryLoad(FlowDestination destination)
        {
            if (IsNavigating) return false;
            if (!TryResolveScene(destination, out string target)) return false;
            if (destination == FlowDestination.Menu && SceneManager.GetActiveScene().name == target)
            {
                SetError(string.Empty);
                return true;
            }

            IsNavigating = true;
            SetError(string.Empty);
            NavigatingChanged?.Invoke(true);
            try
            {
                RunExitCleanup.PrepareToLeave(run);
                SceneManager.LoadScene(target, LoadSceneMode.Single);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                IsNavigating = false;
                NavigatingChanged?.Invoke(false);
                SetError("场景载入失败。请重试，或返回主菜单。");
                return false;
            }
        }

        bool TryResolveScene(FlowDestination destination, out string target)
        {
            target = string.Empty;
            if (destination == FlowDestination.Menu)
            {
                target = menuSceneName;
            }
            else if (run)
            {
                // Retry the actual game scene, including a Tilemap scene selected by INT.
                target = SceneManager.GetActiveScene().name;
            }
            else if (Application.CanStreamedLevelBeLoaded(preferredGameSceneName))
            {
                target = preferredGameSceneName;
            }
            else
            {
                target = fallbackGameSceneName;
            }

            if (!string.IsNullOrWhiteSpace(target) && Application.CanStreamedLevelBeLoaded(target))
                return true;
            SetError(destination == FlowDestination.Menu
                ? "主菜单场景未加入构建。请重试当前操作。"
                : "游戏场景未加入构建。请检查场景配置后重试。");
            return false;
        }

        void SetError(string message)
        {
            LastError = message ?? string.Empty;
            ErrorChanged?.Invoke(LastError);
        }
    }
}
