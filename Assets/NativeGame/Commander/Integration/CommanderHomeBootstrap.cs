using System;
using Echo.NativeGame.PhoneUI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        void Start()
        {
            var settings = CommanderSettings.Instance;
            var transport = CommanderHttpClient.GetOrCreate();
            var view = CommanderUiFactory.CreateHome(transform, chineseFont);
            view.Bind(new CommanderHomeBindings
            {
                BaseUrl = () => settings.BaseUrl,
                ModelId = () => settings.ModelId,
                TimeoutSeconds = () => settings.TimeoutSeconds,
                HasKey = () => settings.HasKey,
                EndpointUrl = () => settings.EndpointUrl,
                SetEndpoint = (baseUrl, modelId, timeout) =>
                {
                    if (!CommanderSettings.TryNormalizeEndpoint(baseUrl, out _))
                        throw new ArgumentException("Invalid commander endpoint.");
                    settings.SetEndpoint(baseUrl, modelId, timeout);
                },
                SetApiKey = settings.SetApiKey,
                ClearKey = settings.ClearKey,
                SendAsync = transport.SendAsync,
                CancelTransport = transport.Cancel,
                TransportBusy = () => transport.Busy,
                EnterGame = offline =>
                {
                    CommanderLaunchState.OfflineForCurrentRun = offline;
                    // Keep the committed Native scene playable when an optional
                    // Tilemap scene is absent from this checkout's Build Settings.
                    SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(gameSceneName)
                        ? gameSceneName : "NativeDemo");
                }
            });
        }
    }
}
