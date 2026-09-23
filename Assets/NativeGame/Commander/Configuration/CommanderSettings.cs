using System;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    public interface ICommanderSettings
    {
        string BaseUrl { get; }
        string ModelId { get; }
        int TimeoutSeconds { get; }
        bool HasKey { get; }
        string EndpointUrl { get; }

        void SetEndpoint(string baseUrl, string modelId, int timeoutSeconds);
        void SetApiKey(string key);
        void ClearKey();
    }

    // Program lifetime only. Never attach this object to a scene or serialize its key.
    public sealed class CommanderSettings : ICommanderSettings
    {
        const string BaseUrlPreference = "Echo.Commander.BaseUrl";
        const string ModelPreference = "Echo.Commander.ModelId";
        const string TimeoutPreference = "Echo.Commander.TimeoutSeconds";
        public const int DefaultTimeoutSeconds = 15;

        static CommanderSettings instance;
        string apiKey;

        public static CommanderSettings Instance => instance ?? (instance = new CommanderSettings());
        public string BaseUrl { get; private set; }
        public string ModelId { get; private set; }
        public int TimeoutSeconds { get; private set; }
        public bool HasKey => !string.IsNullOrWhiteSpace(apiKey);
        public string EndpointUrl => TryNormalizeEndpoint(BaseUrl, out var url) ? url : string.Empty;

        // The shared transport listens here so edits invalidate an in-flight request.
        public event Action Changed;

        CommanderSettings()
        {
            BaseUrl = PlayerPrefs.GetString(BaseUrlPreference, string.Empty);
            ModelId = PlayerPrefs.GetString(ModelPreference, string.Empty);
            TimeoutSeconds = Mathf.Clamp(PlayerPrefs.GetInt(TimeoutPreference, DefaultTimeoutSeconds), 1, 120);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewProcessSession()
        {
            if (instance != null) instance.apiKey = null;
            instance = null;
        }

        public void SetEndpoint(string baseUrl, string modelId, int timeoutSeconds)
        {
            string nextBaseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            string nextModelId = (modelId ?? string.Empty).Trim();
            int nextTimeout = Mathf.Clamp(timeoutSeconds, 1, 120);
            if (BaseUrl == nextBaseUrl && ModelId == nextModelId && TimeoutSeconds == nextTimeout)
                return;

            BaseUrl = nextBaseUrl;
            ModelId = nextModelId;
            TimeoutSeconds = nextTimeout;

            // Invalid addresses may contain userinfo or query secrets. Do not persist them.
            if (TryNormalizeEndpoint(BaseUrl, out _)) PlayerPrefs.SetString(BaseUrlPreference, BaseUrl);
            else PlayerPrefs.DeleteKey(BaseUrlPreference);
            PlayerPrefs.SetString(ModelPreference, ModelId);
            PlayerPrefs.SetInt(TimeoutPreference, TimeoutSeconds);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public void SetApiKey(string key)
        {
            string nextKey = (key ?? string.Empty).Trim();
            if (string.Equals(apiKey, nextKey, StringComparison.Ordinal)) return;
            apiKey = nextKey;
            Changed?.Invoke();
        }

        public void ClearKey()
        {
            if (apiKey == null) return;
            apiKey = null;
            Changed?.Invoke();
        }

        internal bool TryGetApiKey(out string key)
        {
            key = apiKey;
            return HasKey;
        }

        public static bool TryNormalizeEndpoint(string baseUrl, out string endpointUrl)
        {
            endpointUrl = string.Empty;
            if (!Uri.TryCreate((baseUrl ?? string.Empty).Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return false;

            string path = uri.AbsolutePath.TrimEnd('/');
            string prefix = uri.GetLeftPart(UriPartial.Authority) + path;
            if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                endpointUrl = prefix;
            else if (path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                endpointUrl = prefix + "/chat/completions";
            else if (path.Length == 0)
                endpointUrl = prefix + "/v1/chat/completions";
            else
                endpointUrl = prefix + "/chat/completions";
            return true;
        }
    }
}
