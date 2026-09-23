using System;
using Echo.NativeGame.Commander;
using Echo.NativeGame.GameFlow;

namespace Echo.NativeGame.GameUI
{
    // Both scenes use GameAiSettingsView with this one settings and transport pair.
    public static class GameFlowSettingsAdapter
    {
        public static bool IsConfigured
        {
            get
            {
                var settings = CommanderSettings.Instance;
                return settings.HasKey && !string.IsNullOrEmpty(settings.EndpointUrl) &&
                    !string.IsNullOrWhiteSpace(settings.ModelId);
            }
        }

        public static GameAiSettingsBindings Create(NativeRunController run, Action closed = null)
        {
            var transport = CommanderHttpClient.GetOrCreate();
            return new GameAiSettingsBindings
            {
                Read = Read,
                Apply = draft => Apply(draft, run),
                TestConnection = completed => transport.TestConnection(result =>
                    completed?.Invoke(new GameAiConnectionResult
                    {
                        Success = result.Status == CommanderTransportStatus.Success &&
                            !string.IsNullOrWhiteSpace(result.Content),
                        Message = ConnectionMessage(result.Status)
                    })),
                CancelTest = transport.CancelTestConnection,
                Closed = closed
            };
        }

        static GameAiSettingsSnapshot Read()
        {
            var settings = CommanderSettings.Instance;
            return new GameAiSettingsSnapshot
            {
                BaseUrl = settings.BaseUrl,
                ModelId = settings.ModelId,
                TimeoutSeconds = settings.TimeoutSeconds,
                HasKey = settings.HasKey,
                EndpointUrl = settings.EndpointUrl
            };
        }

        static GameAiSettingsApplyResult Apply(GameAiSettingsDraft draft, NativeRunController run)
        {
            var edit = new FlowSettingsDraft
            {
                BaseUrl = draft.BaseUrl,
                ModelId = draft.ModelId,
                TimeoutSeconds = draft.TimeoutSeconds,
                NewApiKey = draft.NewKey,
                ClearApiKey = draft.ClearKey
            };
            bool success = FlowSettings.TryApplySettings(edit, run, out bool changed, out string error);
            if (success && changed && run)
                CommanderLaunchState.OfflineForCurrentRun = !IsConfigured;
            return new GameAiSettingsApplyResult
            {
                Success = success,
                Changed = changed,
                Error = error
            };
        }

        static string ConnectionMessage(CommanderTransportStatus status)
        {
            switch (status)
            {
                case CommanderTransportStatus.Success: return "连接正常，可开始游戏。";
                case CommanderTransportStatus.Timeout: return "连接超时；可调整超时后重试。";
                case CommanderTransportStatus.NetworkError: return "网络不可达；请检查地址与网络。";
                case CommanderTransportStatus.HttpError: return "服务拒绝请求；请检查 Key、模型和地址。";
                case CommanderTransportStatus.InvalidConfiguration: return "服务配置无效；请检查填写内容。";
                case CommanderTransportStatus.Cancelled: return "连接检查已取消。";
                default: return "响应无有效文本；可重试。";
            }
        }
    }
}
