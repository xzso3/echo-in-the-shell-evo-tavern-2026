using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Echo.NativeGame.Commander
{
    public interface ICommanderTransport
    {
        bool Busy { get; }
        bool SendAsync(IReadOnlyList<CommanderMessage> messages, Action<CommanderTransportResult> completed);
        void Cancel();
    }

    // One shared, scene-independent transport for home checks and the current run.
    public sealed class CommanderHttpClient : MonoBehaviour, ICommanderTransport
    {
        [Serializable] sealed class RequestMessage { public string role; public string content; }
        [Serializable] sealed class RequestBody
        {
            public string model;
            public bool stream;
            public RequestMessage[] messages;
        }
        [Serializable] sealed class ResponseMessage { public string content; }
        [Serializable] sealed class ResponseChoice
        {
            public ResponseMessage message;
            public string finish_reason;
        }
        [Serializable] sealed class ResponseBody { public ResponseChoice[] choices; }

        sealed class RequestState
        {
            public Action<CommanderTransportResult> Callback;
            public Coroutine Coroutine;
            public UnityWebRequest Web;
            public bool Finished;
        }

        const ulong MaxResponseBytes = 65536;
        static CommanderHttpClient instance;
        CommanderSettings settings;
        RequestState active;

        public bool Busy => active != null;

        public static CommanderHttpClient GetOrCreate()
        {
            if (instance != null) return instance;
            var host = new GameObject("Commander HTTP Client");
            DontDestroyOnLoad(host);
            return host.AddComponent<CommanderHttpClient>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = CommanderSettings.Instance;
            settings.Changed += Cancel;
        }

        public bool SendAsync(IReadOnlyList<CommanderMessage> messages,
            Action<CommanderTransportResult> completed)
        {
            if (!isActiveAndEnabled || settings == null || Busy || completed == null ||
                messages == null || messages.Count == 0 ||
                string.IsNullOrWhiteSpace(settings.ModelId) ||
                string.IsNullOrEmpty(settings.EndpointUrl) || !settings.TryGetApiKey(out var key))
                return false;

            var entries = new RequestMessage[messages.Count];
            for (int index = 0; index < messages.Count; index++)
            {
                string role;
                switch (messages[index].Role)
                {
                    case CommanderMessageRole.System: role = "system"; break;
                    case CommanderMessageRole.User: role = "user"; break;
                    case CommanderMessageRole.Assistant: role = "assistant"; break;
                    default: return false;
                }
                if (string.IsNullOrWhiteSpace(messages[index].Content)) return false;
                entries[index] = new RequestMessage { role = role, content = messages[index].Content };
            }

            string json;
            try
            {
                json = JsonUtility.ToJson(new RequestBody
                {
                    model = settings.ModelId, stream = false, messages = entries
                });
            }
            catch (Exception) { return false; }
            var state = new RequestState { Callback = completed };
            active = state;
            try
            {
                state.Coroutine = StartCoroutine(Exchange(state, settings.EndpointUrl,
                    settings.TimeoutSeconds, key, Encoding.UTF8.GetBytes(json)));
            }
            catch (Exception)
            {
                active = null;
                state.Finished = true;
                state.Callback = null;
                return false;
            }
            return true;
        }

        // This is a plain connectivity check; it does not touch CommanderSession or proposals.
        public bool TestConnection(Action<CommanderTransportResult> completed)
        {
            return SendAsync(new[]
            {
                new CommanderMessage(CommanderMessageRole.User, "请简短回复：连接正常。")
            }, completed);
        }

        public void Cancel()
        {
            var state = active;
            if (state == null) return;
            active = null;
            state.Finished = true;
            if (state.Web != null)
            {
                try { state.Web.Abort(); }
                catch (Exception) { /* Cancellation still has one result. */ }
            }
            if (state.Coroutine != null) StopCoroutine(state.Coroutine);
            Deliver(state, new CommanderTransportResult(CommanderTransportStatus.Cancelled,
                null, "请求已取消。"));
        }

        IEnumerator Exchange(RequestState state, string url, int timeoutSeconds,
            string key, byte[] body)
        {
            // Keep completion asynchronous even when request setup fails immediately.
            yield return null;
            if (state.Finished) yield break;
            using (var web = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                UnityWebRequestAsyncOperation operation = null;
                try
                {
                    state.Web = web;
                    web.uploadHandler = new UploadHandlerRaw(body);
                    web.downloadHandler = new DownloadHandlerBuffer();
                    web.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                    web.SetRequestHeader("Accept", "application/json");
                    web.SetRequestHeader("Authorization", "Bearer " + key);
                    web.timeout = timeoutSeconds;
                    web.redirectLimit = 0;
                    operation = web.SendWebRequest();
                }
                catch (Exception) { /* Errors are classified without exposing request data. */ }
                if (operation == null)
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.NetworkError,
                        null, "网络连接失败。"));
                    yield break;
                }

                float deadline = Time.realtimeSinceStartup + timeoutSeconds;
                while (!operation.isDone && Time.realtimeSinceStartup < deadline &&
                       web.downloadedBytes <= MaxResponseBytes)
                    yield return null;

                if (state.Finished) yield break;
                if (web.downloadedBytes > MaxResponseBytes)
                {
                    web.Abort();
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.InvalidResponse,
                        null, "服务回复过长。"));
                    yield break;
                }
                if (!operation.isDone || IsTimeout(web))
                {
                    web.Abort();
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.Timeout,
                        null, "连接超时。"));
                    yield break;
                }

                int httpStatus = (int)web.responseCode;
                if (web.result == UnityWebRequest.Result.ConnectionError)
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.NetworkError,
                        null, "网络连接失败。"));
                    yield break;
                }
                if (web.result == UnityWebRequest.Result.ProtocolError)
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.HttpError,
                        null, "服务返回 HTTP " + httpStatus + "。", httpStatus));
                    yield break;
                }
                if (web.result != UnityWebRequest.Result.Success)
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.InvalidResponse,
                        null, "服务回复无法读取。"));
                    yield break;
                }
                if (httpStatus < 200 || httpStatus >= 300)
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.HttpError,
                        null, "服务返回 HTTP " + httpStatus + "。", httpStatus));
                    yield break;
                }

                ResponseBody response = null;
                try { response = JsonUtility.FromJson<ResponseBody>(web.downloadHandler.text); }
                catch (Exception) { /* Never expose a provider response or request in diagnostics. */ }
                if (response?.choices == null || response.choices.Length == 0 ||
                    response.choices[0]?.message == null ||
                    response.choices[0].finish_reason == "length" ||
                    string.IsNullOrWhiteSpace(response.choices[0].message.content))
                {
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.InvalidResponse,
                        null, "服务回复格式无效。"));
                    yield break;
                }

                Complete(state, new CommanderTransportResult(CommanderTransportStatus.Success,
                    response.choices[0].message.content.Trim()));
            }
        }

        static bool IsTimeout(UnityWebRequest web)
        {
            return web.result == UnityWebRequest.Result.ConnectionError && web.error != null &&
                (web.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 web.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        void Complete(RequestState state, CommanderTransportResult result)
        {
            if (state.Finished) return;
            state.Finished = true;
            if (active == state) active = null;
            state.Web = null;
            Deliver(state, result);
        }

        static void Deliver(RequestState state, CommanderTransportResult result)
        {
            var callback = state.Callback;
            state.Callback = null;
            callback?.Invoke(result);
        }

        void OnDisable() { Cancel(); }

        void OnDestroy()
        {
            Cancel();
            if (settings != null) settings.Changed -= Cancel;
            if (instance == this) instance = null;
        }
    }
}
