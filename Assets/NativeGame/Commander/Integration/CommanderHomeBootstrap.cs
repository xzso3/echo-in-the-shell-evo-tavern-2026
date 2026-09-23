using Echo.NativeGame.GameFlow;
using Echo.NativeGame.GameUI;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    // The choice belongs to this program session, not to PlayerPrefs or the API key.
    public static class CommanderLaunchState
    {
        public static bool OfflineForCurrentRun { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { OfflineForCurrentRun = false; }
    }

    [DisallowMultipleComponent]
    public sealed class CommanderHomeBootstrap : MonoBehaviour
    {
        public TMP_FontAsset chineseFont;
        public string gameSceneName = "NativeDemoTilemap";
        GameMenuView view;
        FlowNavigation navigation;

        void Start()
        {
            navigation = GetComponent<FlowNavigation>();
            if (!navigation) navigation = gameObject.AddComponent<FlowNavigation>();
            navigation.PreferredGameSceneName = gameSceneName;
            var art = Resources.Load<GameUiArtCatalog>("GF01Art");
            view = GameFlowUiFactory.CreateMenu(transform, chineseFont, art);
            view.Bind(new GameMenuBindings
            {
                HasConfiguration = () => navigation.AiConfigured,
                StartGame = navigation.StartGame,
                QuitGame = navigation.QuitApplication,
                OpenSettings = navigation.OpenAiSettings,
                Settings = GameFlowSettingsAdapter.Create(null)
            });
            navigation.NavigatingChanged += busy => view.SetLoading(busy);
            navigation.ErrorChanged += view.SetError;
            navigation.AiConfigurationChanged += OnAiConfigurationChanged;
            view.SettingsView.ConnectionTestCompleted += result =>
                view.SetTestStatus(result.Message, result.Success);
        }

        void Update()
        {
            if (view && Input.GetKeyDown(KeyCode.Escape)) view.HandleBack();
        }

        void OnAiConfigurationChanged()
        {
            if (!view) return;
            view.RefreshConfiguration();
            view.SetTestStatus("连接未测试", false);
        }

        void OnDestroy()
        {
            if (navigation) navigation.AiConfigurationChanged -= OnAiConfigurationChanged;
        }
    }
}
