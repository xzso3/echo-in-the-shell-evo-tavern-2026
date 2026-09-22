using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Echo.NativeGame
{
    // Text-only transport. No model output is dispatched to gameplay systems.
    public sealed class NativeCommanderProxy : MonoBehaviour
    {
        [Serializable] sealed class Config { public string proxyUrl; public int timeoutSeconds = 8; }
        [Serializable] sealed class Context { public string objective; public string memories; }
        [Serializable] sealed class Request { public int version = 1; public string requestId; public string message; public Context context; }
        [Serializable] sealed class Response { public int version; public string requestId; public string reply; }

        public string proxyUrl;
        [Range(3, 20)] public int timeoutSeconds = 8;
        public bool Busy => active != null;
        public bool Configured => TryGetUrl(out _);
        Coroutine active;
        UnityWebRequest pending;
        int generation;

        void Awake()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "commander-proxy.json");
                if (!File.Exists(path)) return;
                var config = JsonUtility.FromJson<Config>(File.ReadAllText(path, Encoding.UTF8));
                if (config == null) return;
                proxyUrl = config.proxyUrl;
                timeoutSeconds = Mathf.Clamp(config.timeoutSeconds, 3, 20);
            }
            catch (Exception error) { Debug.LogWarning("Commander proxy configuration unavailable: " + error.GetType().Name); }
        }

        public bool Send(string message, string objective, string memories, Action<string, string> finished)
        {
            if (Busy || !TryGetUrl(out var url)) return false;
            int ticket = ++generation;
            var request = new Request
            {
                requestId = Guid.NewGuid().ToString("N"), message = message,
                context = new Context { objective = objective, memories = memories }
            };
            active = StartCoroutine(Exchange(url, request, ticket, finished));
            return true;
        }

        public void Cancel()
        {
            generation++;
            if (pending != null) pending.Abort();
            if (active != null) StopCoroutine(active);
            pending = null;
            active = null;
        }

        IEnumerator Exchange(string url, Request data, int ticket, Action<string, string> finished)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            using (var web = new UnityWebRequest(url, "POST"))
            {
                pending = web;
                web.uploadHandler = new UploadHandlerRaw(bytes);
                web.downloadHandler = new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                web.timeout = Mathf.Clamp(timeoutSeconds, 3, 20);
                web.redirectLimit = 0;
                var operation = web.SendWebRequest();
                float deadline = Time.realtimeSinceStartup + web.timeout;
                while (!operation.isDone && Time.realtimeSinceStartup < deadline && web.downloadedBytes <= 16384)
                    yield return null;
                if (!operation.isDone) web.Abort();
                if (ticket != generation) yield break;
                active = null;
                pending = null;
                bool timedOut = Time.realtimeSinceStartup >= deadline ||
                    (web.error != null && (web.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           web.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0));
                if (timedOut)
                { finished?.Invoke(null, "连接超时。草稿已保留，请使用预设通讯或稍后重试。"); yield break; }
                if (web.downloadedBytes > 16384 || web.result != UnityWebRequest.Result.Success || web.responseCode != 200)
                { finished?.Invoke(null, "在线通讯不可用。草稿已保留，预设通讯仍可使用。"); yield break; }
                Response response = null;
                try { response = JsonUtility.FromJson<Response>(web.downloadHandler.text); }
                catch (Exception) { /* Invalid responses use local fallback. */ }
                if (response == null || response.version != 1 || response.requestId != data.requestId || string.IsNullOrWhiteSpace(response.reply) || response.reply.Length > 1200)
                { finished?.Invoke(null, "在线回复无效。草稿已保留，预设通讯仍可使用。"); yield break; }
                finished?.Invoke(response.reply.Trim(), null);
            }
        }

        bool TryGetUrl(out string url)
        {
            url = (proxyUrl ?? "").Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
            if (!string.IsNullOrEmpty(parsed.UserInfo) || !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment)) return false;
            if (parsed.Scheme == Uri.UriSchemeHttps) return true;
            return parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback;
        }

        void OnDisable() { Cancel(); }
        void OnDestroy() { Cancel(); }
    }
}
