using System;
using Echo.NativeGame.Commander;

namespace Echo.NativeGame.GameFlow
{
    // One edit buffer for both the home and in-run settings presentations.
    // NewApiKey is always blank on opening a form; blank means keep the memory key.
    public sealed class FlowSettingsDraft
    {
        public string BaseUrl;
        public string ModelId;
        public int TimeoutSeconds;
        public string NewApiKey;
        public bool ClearApiKey;

        public static FlowSettingsDraft FromCurrent()
        {
            var settings = CommanderSettings.Instance;
            return new FlowSettingsDraft
            {
                BaseUrl = settings.BaseUrl,
                ModelId = settings.ModelId,
                TimeoutSeconds = settings.TimeoutSeconds,
                NewApiKey = string.Empty
            };
        }
    }

    public static class FlowSettings
    {
        public static bool ApplySettings(FlowSettingsDraft draft, NativeRunController run = null)
        {
            if (!TryApplySettings(draft, run, out bool changed, out string error))
                throw new ArgumentException(error, nameof(draft));
            return changed;
        }

        // A successful no-op reports changed=false and leaves requests/contracts alone.
        public static bool TryApplySettings(FlowSettingsDraft draft, NativeRunController run,
            out bool changed, out string error)
        {
            changed = false;
            error = null;
            if (draft == null) { error = "设置内容不存在。"; return false; }

            string url = (draft.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
            string model = (draft.ModelId ?? string.Empty).Trim();
            string key = (draft.NewApiKey ?? string.Empty).Trim();
            if (draft.TimeoutSeconds < 1 || draft.TimeoutSeconds > 120)
            { error = "超时请输入 1–120 秒。"; return false; }
            if ((url.Length == 0) != (model.Length == 0) ||
                url.Length != 0 && !CommanderSettings.TryNormalizeEndpoint(url, out _))
            { error = "请填写有效的 API 地址和模型 ID，或同时留空使用离线模式。"; return false; }
            if (draft.ClearApiKey && key.Length != 0)
            { error = "清除 Key 与输入新 Key 不能同时进行。"; return false; }

            var settings = CommanderSettings.Instance;
            settings.TryGetApiKey(out string oldKey);
            bool endpointChanged = settings.BaseUrl != url || settings.ModelId != model ||
                settings.TimeoutSeconds != draft.TimeoutSeconds;
            bool keyChanged = draft.ClearApiKey ? settings.HasKey :
                key.Length != 0 && !string.Equals(oldKey, key, StringComparison.Ordinal);
            if (!endpointChanged && !keyChanged) return true;

            // Existing Changed subscribers cancel the transport and current request.
            // No asynchronous callback can interleave these synchronous setters.
            if (endpointChanged) settings.SetEndpoint(url, model, draft.TimeoutSeconds);
            if (keyChanged)
            {
                if (draft.ClearApiKey) settings.ClearKey();
                else settings.SetApiKey(key);
            }
            changed = true;

            if (run)
            {
                var runtime = run.GetComponent<CommanderRuntimeHost>();
                runtime?.Session?.ConfigurationChanged();
                if (run.rules != null && run.rules.Support != null)
                    run.rules.Support.Cancel();
                var bridge = run.GetComponent<CommanderSupportBridge>();
                if (bridge) bridge.ExpireUnexecuted("AI 设置已更新，请重新询问。");
                if (run.hud && run.hud.phone && run.hud.phone.tabletView)
                {
                    var tablet = run.hud.phone.tabletView;
                    if (tablet.ContractOpen) tablet.HandleBack();
                    tablet.RefreshExternal();
                }
            }
            return true;
        }
    }
}
