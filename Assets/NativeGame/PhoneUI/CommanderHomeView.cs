using System;
using System.Collections.Generic;
using Echo.NativeGame.Commander;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.PhoneUI
{
    // INT supplies method groups from one shared ICommanderSettings/ICommanderTransport pair.
    // This contains no credential field and never persists or logs a key.
    public sealed class CommanderHomeBindings
    {
        public Func<string> BaseUrl;
        public Func<string> ModelId;
        public Func<int> TimeoutSeconds;
        public Func<bool> HasKey;
        public Func<string> EndpointUrl;
        public Action<string, string, int> SetEndpoint;
        public Action<string> SetApiKey;
        public Action ClearKey;
        public Func<IReadOnlyList<CommanderMessage>, Action<CommanderTransportResult>, bool> SendAsync;
        public Action CancelTransport;
        public Func<bool> TransportBusy;
        public Action<bool> EnterGame; // true: explicit offline entry
    }

    public sealed class CommanderHomeView : MonoBehaviour
    {
        TMP_InputField urlField, keyField, modelField, timeoutField;
        TMP_Text endpointLabel, statusLabel, keyHint;
        UnityEngine.UI.Button testButton, onlineButton, offlineButton, clearKeyButton;
        CommanderHomeBindings bindings;
        int testGeneration;
        bool testing;
        bool built;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            PhoneUiElements.Panel("Backdrop", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color32(11, 25, 27, 255), true);
            var shell = PhoneUiElements.Panel("Configuration Shell", transform, new Vector2(.17f, .07f),
                new Vector2(.83f, .93f), Vector2.zero, Vector2.zero, new Color32(29, 43, 45, 255));
            PhoneUiElements.Panel("Display", shell.transform, new Vector2(.035f, .045f),
                new Vector2(.965f, .955f), Vector2.zero, Vector2.zero, PhoneUiElements.Screen);
            var display = shell.transform.Find("Display");
            PlaceText("Eyebrow", display, "SHELL-LINK / 指挥官连接", font, 17, PhoneUiElements.Jade, 0, -20, 0, -56);
            PlaceText("Title", display, "进入本局", font, 33, PhoneUiElements.Pale, 0, -62, 0, -113);
            PlaceText("Description", display, "填写兼容 /v1/chat/completions 的服务配置，或直接离线试玩。", font,
                17, PhoneUiElements.Muted, 0, -101, 0, -125);
            var form = PhoneUiElements.Rect("Form", display, new Vector2(.065f, .24f),
                new Vector2(.935f, .76f), Vector2.zero, Vector2.zero);
            var group = form.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.spacing = 8;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlHeight = false;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            urlField = FieldRow(form, "API Base URL（例如以 /v1 结尾）", "https://服务地址/v1", font);
            keyField = FieldRow(form, "API Key（仅本次程序会话）", "输入 Key", font, true);
            var keyRow = keyField.transform.parent.parent;
            clearKeyButton = PhoneUiElements.Button("Clear Key", keyRow, "清除 Key", font,
                new Color32(50, 69, 61, 255), PhoneUiElements.Pale, 14);
            var clearRect = clearKeyButton.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(.8f, .21f);
            clearRect.anchorMax = new Vector2(1f, .67f);
            clearRect.offsetMin = new Vector2(4, 0);
            clearRect.offsetMax = Vector2.zero;
            var keyRect = keyField.GetComponent<RectTransform>();
            keyRect.anchorMax = new Vector2(.79f, 1f);
            keyHint = PlaceText("Key State", keyRow, "", font, 12, PhoneUiElements.Muted,
                0, -61, 0, -75);
            modelField = FieldRow(form, "模型 ID", "模型名称", font);
            timeoutField = FieldRow(form, "超时（秒）", "15", font);
            timeoutField.contentType = TMP_InputField.ContentType.IntegerNumber;
            var footer = PhoneUiElements.Rect("Actions", display, new Vector2(.065f, .04f),
                new Vector2(.935f, .23f), Vector2.zero, Vector2.zero);
            endpointLabel = PlaceText("Endpoint", footer, "请求地址：待填写", font, 14,
                PhoneUiElements.Muted, 0, -1, 0, -29);
            statusLabel = PlaceText("Status", footer, "待检查", font, 16,
                PhoneUiElements.Pale, 0, -31, 0, -58);
            testButton = ActionButton(footer, "Test Connection", "测试连接", font, 0f, .31f,
                new Color32(43, 83, 65, 255), PhoneUiElements.Pale);
            onlineButton = ActionButton(footer, "Start Online", "开始游戏", font, .34f, .65f,
                PhoneUiElements.Jade, PhoneUiElements.Ink);
            offlineButton = ActionButton(footer, "Start Offline", "离线试玩", font, .68f, 1f,
                new Color32(55, 68, 63, 255), PhoneUiElements.Pale);
            urlField.onValueChanged.AddListener(_ => InvalidateTest());
            keyField.onValueChanged.AddListener(_ => InvalidateTest());
            modelField.onValueChanged.AddListener(_ => InvalidateTest());
            timeoutField.onValueChanged.AddListener(_ => InvalidateTest());
            clearKeyButton.onClick.AddListener(ClearKey);
            testButton.onClick.AddListener(TestConnection);
            onlineButton.onClick.AddListener(() => Enter(false));
            offlineButton.onClick.AddListener(() => Enter(true));
        }

        public void Bind(CommanderHomeBindings value)
        {
            bindings = value;
            if (!built || value == null) return;
            urlField.SetTextWithoutNotify(value.BaseUrl?.Invoke() ?? "");
            modelField.SetTextWithoutNotify(value.ModelId?.Invoke() ?? "");
            timeoutField.SetTextWithoutNotify((value.TimeoutSeconds?.Invoke() ?? 15).ToString());
            keyField.SetTextWithoutNotify("");
            RefreshKeyHint();
            RefreshEndpoint();
            SetStatus("待检查", PhoneUiElements.Muted);
        }

        void OnDisable() => InvalidateTest();
        void OnDestroy() => InvalidateTest();

        void InvalidateTest()
        {
            testGeneration++;
            if (testing) bindings?.CancelTransport?.Invoke();
            testing = false;
            if (statusLabel) SetStatus("配置已修改，待检查", PhoneUiElements.Muted);
            if (testButton) testButton.interactable = true;
            if (endpointLabel) endpointLabel.text = "请求地址：保存设置后显示";
        }

        bool ApplySettings()
        {
            if (bindings == null || bindings.SetEndpoint == null || bindings.SetApiKey == null)
            {
                SetStatus("配置服务尚未接线。", PhoneUiElements.Amber);
                return false;
            }
            var url = urlField.text.Trim();
            var model = modelField.text.Trim();
            if (url.Length == 0 || model.Length == 0)
            {
                SetStatus("请填写 API Base URL 和模型 ID。", PhoneUiElements.Amber);
                return false;
            }
            if (!int.TryParse(timeoutField.text, out var timeout) || timeout < 1 || timeout > 120)
            {
                SetStatus("超时请输入 1–120 秒。", PhoneUiElements.Amber);
                return false;
            }
            try
            {
                bindings.SetEndpoint(url, model, timeout);
                if (!string.IsNullOrWhiteSpace(keyField.text))
                {
                    bindings.SetApiKey(keyField.text.Trim());
                    keyField.SetTextWithoutNotify("");
                }
            }
            catch (ArgumentException)
            {
                SetStatus("服务地址或模型配置无效。", PhoneUiElements.Amber);
                return false;
            }
            catch (FormatException)
            {
                SetStatus("服务地址或模型配置无效。", PhoneUiElements.Amber);
                return false;
            }
            RefreshKeyHint();
            RefreshEndpoint();
            return true;
        }

        void TestConnection()
        {
            if (!ApplySettings()) return;
            if (bindings.HasKey == null || !bindings.HasKey())
            {
                SetStatus("请填写 API Key 后测试连接。", PhoneUiElements.Amber);
                return;
            }
            if (bindings.SendAsync == null || (bindings.TransportBusy?.Invoke() ?? false))
            {
                SetStatus("通讯正在使用，请稍后再试。", PhoneUiElements.Amber);
                return;
            }
            var generation = ++testGeneration;
            testing = true;
            testButton.interactable = false;
            SetStatus("正在检查连接…", PhoneUiElements.Jade);
            var messages = new[] { new CommanderMessage(CommanderMessageRole.User, "请回复一条简短文本以确认连接。") };
            if (!bindings.SendAsync(messages, result =>
                {
                    if (!this || generation != testGeneration) return;
                    testing = false;
                    testButton.interactable = true;
                    if (result.Status == CommanderTransportStatus.Success && !string.IsNullOrWhiteSpace(result.Content))
                        SetStatus("连接正常，可开始游戏。", PhoneUiElements.Jade);
                    else
                        SetStatus(ConnectionFailure(result.Status), PhoneUiElements.Amber);
                }))
            {
                testing = false;
                testButton.interactable = true;
                SetStatus("未能启动连接检查；请核对设置后重试。", PhoneUiElements.Amber);
            }
        }

        static string ConnectionFailure(CommanderTransportStatus status)
        {
            switch (status)
            {
                case CommanderTransportStatus.Timeout: return "连接超时；可调整超时或离线试玩。";
                case CommanderTransportStatus.NetworkError: return "网络不可达；请检查地址与网络。";
                case CommanderTransportStatus.HttpError: return "服务拒绝请求；请检查 Key、模型和地址。";
                case CommanderTransportStatus.InvalidConfiguration: return "服务配置无效；请检查填写内容。";
                case CommanderTransportStatus.Cancelled: return "连接检查已取消。";
                default: return "响应无有效文本；可重试或离线试玩。";
            }
        }

        void Enter(bool offline)
        {
            if (!offline && !ApplySettings()) return;
            if (!offline && (bindings?.HasKey?.Invoke() != true))
            {
                SetStatus("Key 未设置；请填写 Key 或选择离线试玩。", PhoneUiElements.Amber);
                return;
            }
            InvalidateTest();
            bindings?.EnterGame?.Invoke(offline);
        }

        void ClearKey()
        {
            InvalidateTest();
            bindings?.ClearKey?.Invoke();
            keyField.SetTextWithoutNotify("");
            RefreshKeyHint();
        }

        void RefreshKeyHint() => keyHint.text = bindings?.HasKey?.Invoke() == true
            ? "Key 已在内存中设置；留空保持现值。" : "Key 未设置；退出程序后不会保留。";
        void RefreshEndpoint() => endpointLabel.text = "请求地址：" +
            (bindings?.EndpointUrl?.Invoke() ?? "待填写");
        void SetStatus(string value, Color color) { statusLabel.text = value; statusLabel.color = color; }

        static TMP_InputField FieldRow(Transform parent, string label, string hint, TMP_FontAsset font,
            bool password = false)
        {
            var row = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = password ? 76 : 62;
            PlaceText("Label", row.transform, label, font, 15, PhoneUiElements.Muted,
                0, 0, 0, -23);
            var inputRect = PhoneUiElements.Rect("Field", row.transform, new Vector2(0, 1),
                new Vector2(1, 1), new Vector2(0, -59), new Vector2(0, -25));
            inputRect.pivot = new Vector2(.5f, 1);
            return PhoneUiElements.Input("Input", inputRect, font, hint, password);
        }

        static TMP_Text PlaceText(string name, Transform parent, string value, TMP_FontAsset font,
            int size, Color color, float left, float top, float right, float bottom)
        {
            var rect = PhoneUiElements.Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(left, bottom), new Vector2(right, top));
            return PhoneUiElements.Text("Text", rect, value, font, size, color);
        }

        static UnityEngine.UI.Button ActionButton(Transform parent, string name, string label,
            TMP_FontAsset font, float minX, float maxX, Color bg, Color fg)
        {
            var rect = PhoneUiElements.Rect(name, parent, new Vector2(minX, 0), new Vector2(maxX, 0),
                new Vector2(0, 0), new Vector2(0, 48));
            return PhoneUiElements.Button("Button", rect, label, font, bg, fg);
        }
    }
}
