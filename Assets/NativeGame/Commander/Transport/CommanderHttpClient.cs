using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    public interface ICommanderTransport
    {
        bool Busy { get; }
        bool SendAsync(IReadOnlyList<CommanderMessage> messages, Action<CommanderTransportResult> completed);
        void Cancel();
    }

    // Compatibility entry point. All wire requests go through com.openai.unity.
    public sealed class CommanderHttpClient : MonoBehaviour, ICommanderTransport
    {
        sealed class RequestState
        {
            public Action<CommanderTransportResult> Callback;
            public Coroutine Coroutine;
            public CancellationTokenSource Cancellation;
            public Task<ChatResponse> Task;
            public bool IsConnectionTest;
            public bool Finished;
            public CommanderDiagnostics Trace;
        }

        const int MaxReplyCharacters = 65536;
        static CommanderHttpClient instance;
        CommanderSettings settings;
        RequestState active;

        public bool Busy => active != null;

        public static CommanderHttpClient GetOrCreate()
        {
            if (instance != null) return instance;
            var host = new GameObject("Commander SDK Client");
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
            var trace = CommanderDiagnostics.For(messages);
            trace.Log("transport.enter", "enabled=" + isActiveAndEnabled + " busy=" + Busy + " messages=" + (messages?.Count ?? 0));
            if (!isActiveAndEnabled || settings == null || Busy || completed == null ||
                messages == null || messages.Count == 0 ||
                string.IsNullOrWhiteSpace(settings.ModelId) ||
                !settings.TryGetApiKey(out var key) ||
                !TryGetSdkAddress(settings.EndpointUrl, out var domain, out var apiVersion))
            { trace.Log("transport.rejected", "inactive/busy/missing_callback/messages/model/key_or_invalid_endpoint"); return false; }

            var sdkMessages = new List<Message>(messages.Count);
            for (int i = 0; i < messages.Count; i++)
            {
                Role role;
                switch (messages[i].Role)
                {
                    case CommanderMessageRole.System: role = Role.System; break;
                    case CommanderMessageRole.User: role = Role.User; break;
                    case CommanderMessageRole.Assistant: role = Role.Assistant; break;
                    default: trace.Log("transport.rejected", "invalid_message_role index=" + i); return false;
                }
                if (string.IsNullOrWhiteSpace(messages[i].Content)) { trace.Log("transport.rejected", "empty_message index=" + i); return false; }
                sdkMessages.Add(new Message(role, messages[i].Content));
            }

            var state = new RequestState { Callback = completed, Trace = trace };
            active = state;
            try
            {
                state.Coroutine = StartCoroutine(Exchange(state, sdkMessages,
                    settings.ModelId, domain, apiVersion, key, settings.TimeoutSeconds));
            }
            catch (Exception exception)
            {
                trace.Log("transport.start_exception", exception.GetType().FullName);
                active = null;
                state.Finished = true;
                state.Callback = null;
                Release(state);
                return false;
            }
            return true;
        }

        // Uses the same SDK route, but is never passed to CommanderSession or support.
        public bool TestConnection(Action<CommanderTransportResult> completed)
        {
            bool started = SendAsync(new[]
            {
                new CommanderMessage(CommanderMessageRole.User, "请只回复 OK。")
            }, completed);
            if (started && active != null) active.IsConnectionTest = true;
            return started;
        }

        // Settings UI should use this instead of Cancel, so a failed/busy test
        // cannot cancel the current run's chat request.
        public void CancelTestConnection()
        {
            if (active != null && active.IsConnectionTest) Cancel();
        }

        public void Cancel()
        {
            var state = active;
            if (state == null) return;
            state.Trace.Log("transport.cancel", "cancel/settings_changed/disable; connectionTest=" + state.IsConnectionTest);
            state.Finished = true;
            active = null;
            Abort(state);
            if (state.Coroutine != null) StopCoroutine(state.Coroutine);
            Release(state);
            Deliver(state, new CommanderTransportResult(CommanderTransportStatus.Cancelled,
                null, "请求已取消。"));
        }

        IEnumerator Exchange(RequestState state, List<Message> messages, string model,
            string domain, string apiVersion, string key, int timeoutSeconds)
        {
            // Keep accepted requests asynchronous even if SDK setup fails immediately.
            yield return null;
            if (state.Finished) yield break;

            var clock = Stopwatch.StartNew();
            try
            {
                state.Cancellation = new CancellationTokenSource();
                var client = new OpenAIClient(new OpenAIAuthentication(key),
                    new OpenAISettings(domain, apiVersion));
                var request = new ChatRequest(messages, model: model);
                if (CommanderDiagnostics.Enabled)
                    state.Trace.Log("sdk.request", request.ToString());
                state.Trace.Log("sdk.start", "non-streaming; timeoutSeconds=" + timeoutSeconds + "; SDK debug disabled");
                state.Task = client.ChatEndpoint.GetCompletionAsync(request, state.Cancellation.Token);
            }
            catch (Exception exception)
            {
                LogException(state, exception);
                Complete(state, Classify(exception));
                yield break;
            }

            while (!state.Task.IsCompleted)
            {
                if (state.Finished) yield break;
                if (clock.Elapsed.TotalSeconds >= timeoutSeconds)
                {
                    state.Trace.Log("transport.timeout", "elapsedSeconds=" + clock.Elapsed.TotalSeconds);
                    Abort(state);
                    Complete(state, new CommanderTransportResult(CommanderTransportStatus.Timeout,
                        null, "连接超时。"));
                    yield break;
                }
                yield return null;
            }

            if (state.Finished) yield break;
            CommanderTransportResult result;
            try
            {
                var response = state.Task.GetAwaiter().GetResult();
                state.Trace.Log("sdk.success", "http_status=not_exposed_by_sdk; choices=" + (response?.Choices?.Count ?? 0));
                if (CommanderDiagnostics.Enabled)
                    state.Trace.Log("sdk.response", response?.ToJsonString());
                var choice = response?.Choices != null && response.Choices.Count > 0
                    ? response.Choices[0] : null;
                var content = choice?.Message?.Content as string;
                state.Trace.Log("sdk.choice", "finishReason=" + choice?.FinishReason + " contentType=" + choice?.Message?.Content?.GetType().FullName + " chars=" + (content?.Length ?? 0));
                state.Trace.Log("reply.raw", content);
                if (choice == null || choice.FinishReason == "length" ||
                    choice.FinishReason == "content_filter" ||
                    string.IsNullOrWhiteSpace(content) || content.Length > MaxReplyCharacters)
                    result = new CommanderTransportResult(CommanderTransportStatus.InvalidResponse,
                        null, "服务回复格式无效。");
                else
                    result = new CommanderTransportResult(CommanderTransportStatus.Success,
                        content.Trim());
            }
            catch (Exception exception)
            {
                LogException(state, exception);
                result = Classify(exception);
            }
            Complete(state, result);
        }

        // The SDK owns /{apiVersion}/chat/completions. Split only the final path
        // segment before /chat/completions, preserving any proxy prefix exactly.
        static bool TryGetSdkAddress(string endpoint, out string domain, out string apiVersion)
        {
            domain = null;
            apiVersion = null;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return false;

            string path = uri.AbsolutePath.TrimEnd('/');
            const string suffix = "/chat/completions";
            if (!path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return false;
            string prefix = path.Substring(0, path.Length - suffix.Length).TrimEnd('/');
            int split = prefix.LastIndexOf('/');
            if (split < 0 || split == prefix.Length - 1) return false;
            apiVersion = prefix.Substring(split + 1);
            if (apiVersion == "." || apiVersion == "..") return false;
            domain = uri.GetLeftPart(UriPartial.Authority) + prefix.Substring(0, split);
            return true;
        }

        static void Abort(RequestState state)
        {
            try { state.Cancellation?.Cancel(); }
            catch (Exception) { /* The terminal result is owned here. */ }
        }

        void Complete(RequestState state, CommanderTransportResult result)
        {
            if (state.Finished) return;
            state.Trace.Log("transport.complete", "status=" + result.Status + " http=" + result.HttpStatus + " error=" + result.ErrorText);
            state.Finished = true;
            if (active == state) active = null;
            Release(state);
            Deliver(state, result);
        }

        static void Release(RequestState state)
        {
            var cancellation = state.Cancellation;
            state.Cancellation = null;
            if (cancellation == null) return;
            if (state.Task == null || state.Task.IsCompleted)
            {
                if (state.Task?.IsFaulted == true) { var ignored = state.Task.Exception; }
                cancellation.Dispose();
            }
            else
                state.Task.ContinueWith(task =>
                {
                    var ignored = task.Exception;
                    cancellation.Dispose();
                }, TaskScheduler.Default);
        }

        static void Deliver(RequestState state, CommanderTransportResult result)
        {
            var callback = state.Callback;
            state.Callback = null;
            callback?.Invoke(result);
        }

        static void LogException(RequestState state, Exception exception)
        {
            state.Trace.Log("sdk.exception", "type=" + exception.GetType().FullName + " http=" + ReadHttpStatus(exception));
            // Never log Exception.Message/ToString: SDK embeds headers there.
            for (int depth = 0; exception != null && depth < 3; depth++, exception = exception.InnerException)
                if (exception is Utilities.WebRequestRest.RestException rest)
                    state.Trace.Log("http.error_body", rest.Response.Body);
        }

        // SDK exceptions can contain request data. Only a numeric HTTP code may escape.
        static CommanderTransportResult Classify(Exception exception)
        {
            if (exception is OperationCanceledException || exception is TimeoutException)
                return new CommanderTransportResult(CommanderTransportStatus.Timeout,
                    null, "连接超时。");
            if (exception is Newtonsoft.Json.JsonException)
                return new CommanderTransportResult(CommanderTransportStatus.InvalidResponse,
                    null, "服务回复格式无效。");
            if (exception is System.Security.Authentication.AuthenticationException ||
                exception is ArgumentException)
                return new CommanderTransportResult(CommanderTransportStatus.InvalidConfiguration,
                    null, "服务配置无效。");

            int httpStatus = ReadHttpStatus(exception);
            if (httpStatus >= 400 && httpStatus <= 599)
                return new CommanderTransportResult(CommanderTransportStatus.HttpError,
                    null, "服务返回 HTTP " + httpStatus + "。", httpStatus);
            if (exception?.GetType().Name == "RestException")
                return new CommanderTransportResult(CommanderTransportStatus.HttpError,
                    null, "服务拒绝请求。");
            return new CommanderTransportResult(CommanderTransportStatus.NetworkError,
                null, "网络连接失败。");
        }

        static int ReadHttpStatus(Exception exception)
        {
            for (int depth = 0; exception != null && depth < 3; depth++, exception = exception.InnerException)
            {
                if (exception is Utilities.WebRequestRest.RestException rest && rest.Response.Code >= 100 && rest.Response.Code <= 599)
                    return (int)rest.Response.Code;
                foreach (var name in new[] { "StatusCode", "ResponseCode", "HttpStatusCode" })
                {
                    try
                    {
                        var property = exception.GetType().GetProperty(name,
                            BindingFlags.Public | BindingFlags.Instance);
                        if (property == null) continue;
                        var value = property.GetValue(exception, null);
                        if (value != null)
                        {
                            int status = Convert.ToInt32(value);
                            if (status >= 100 && status <= 599) return status;
                        }
                    }
                    catch (Exception) { /* Never surface SDK exception details. */ }
                }
            }
            return 0;
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
