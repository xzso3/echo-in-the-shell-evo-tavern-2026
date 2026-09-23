using System;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    public struct GameAiSettingsSnapshot
    {
        public string BaseUrl;
        public string ModelId;
        public int TimeoutSeconds;
        public bool HasKey;
        public string EndpointUrl;
    }

    public struct GameAiSettingsDraft
    {
        public string BaseUrl;
        public string ModelId;
        public int TimeoutSeconds;
        public string NewKey;
        public bool ClearKey;
    }

    public struct GameAiSettingsApplyResult
    {
        public bool Success;
        public bool Changed;
        public string Error;
    }

    public struct GameAiConnectionResult
    {
        public bool Success;
        public string Message;
    }

    // Both scenes instantiate this same form. The controller owns settings and transport.
    public sealed class GameAiSettingsBindings
    {
        public Func<GameAiSettingsSnapshot> Read;
        public Func<GameAiSettingsDraft, GameAiSettingsApplyResult> Apply;
        // Test only the already applied settings through the shared transport.
        public Func<Action<GameAiConnectionResult>, bool> TestConnection;
        public Action CancelTest;
        public Action Closed;
    }

    [DisallowMultipleComponent]
    public sealed class GameAiSettingsView : MonoBehaviour
    {
        TMP_InputField urlField, modelField, keyField, timeoutField;
        TMP_Text keyHint, feedback, endpoint;
        UnityEngine.UI.Button backButton, cancelButton, clearKeyButton, testButton, applyButton;
        UnityEngine.UI.Image[] cornerImages;
        GameAiSettingsBindings bindings;
        int testGeneration;
        bool clearKeyRequested;
        bool testing;
        bool built;

        public bool IsOpen => gameObject.activeSelf;
        public event Action<GameAiConnectionResult> ConnectionTestCompleted;
        public event Action<bool> SettingsApplied;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            GameUiElements.Image("Modal Shade", transform, new Color(0.02f, 0.06f, 0.07f, 0.82f), true);
            var panel = GameUiElements.CenterBox("Settings Panel", transform, 636, 576);
            GameUiElements.Image("Panel Surface", panel, GameUiElements.Panel, true);
            cornerImages = GameUiElements.PanelCorners(panel, 96);
            var rule = GameUiElements.LocalBox("Top Rule", panel, 24, 60, 588, 2)
                .gameObject.AddComponent<UnityEngine.UI.Image>();
            rule.color = GameUiElements.Line;
            rule.raycastTarget = false;
            var title = GameUiElements.LocalBox("Title", panel, 24, 12, 430, 44);
            GameUiElements.Text("Text", title, "AI 设置", font, 32, GameUiElements.Primary);
            backButton = GameUiElements.Button("Back", GameUiElements.LocalBox("Back Bounds", panel,
                516, 14, 96, 42), "返回", font, 18, GameUiElements.Line, GameUiElements.Primary);
            backButton.onClick.AddListener(Close);

            var scrollRoot = GameUiElements.LocalBox("Fields Scroll", panel, 24, 76, 588, 346);
            var scrollHitArea = scrollRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            scrollHitArea.color = new Color(0f, 0f, 0f, 0.001f);
            scrollHitArea.raycastTarget = true;
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = GameUiElements.Fill("Viewport", scrollRoot);
            // Stencil masks use the viewport graphic's alpha. A transparent Image can
            // mask every field, so clip by geometry without a viewport graphic.
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var content = GameUiElements.Rect("Content", viewport,
                new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;
            urlField = FieldRow(content, font, "API Base URL", "https://服务地址/v1");
            modelField = FieldRow(content, font, "模型 ID", "模型名称");
            keyField = FieldRow(content, font, "API Key（仅本次程序会话）", "留空保持现有 Key", true);
            timeoutField = FieldRow(content, font, "超时（秒）", "15");
            timeoutField.contentType = TMP_InputField.ContentType.IntegerNumber;
            var keyRow = keyField.transform.parent.parent;
            clearKeyButton = GameUiElements.Button("Clear Key", GameUiElements.LocalBox("Clear Bounds",
                keyRow, 450, 32, 136, 42), "清除 Key", font, 16,
                GameUiElements.Line, GameUiElements.Primary);
            keyField.GetComponent<RectTransform>().offsetMax = new Vector2(-150, 0);
            keyHint = GameUiElements.Text("Key Hint", GameUiElements.LocalBox("Hint Bounds",
                panel, 24, 426, 588, 25), string.Empty, font, 15, GameUiElements.Secondary);
            endpoint = GameUiElements.Text("Endpoint", GameUiElements.LocalBox("Endpoint Bounds",
                panel, 24, 452, 588, 25), string.Empty, font, 15, GameUiElements.Secondary);
            feedback = GameUiElements.Text("Feedback", GameUiElements.LocalBox("Feedback Bounds",
                panel, 24, 478, 588, 41), string.Empty, font, 16, GameUiElements.Secondary);
            cancelButton = GameUiElements.Button("Cancel", GameUiElements.LocalBox("Cancel Bounds",
                panel, 24, 520, 116, 44), "取消", font, 18,
                GameUiElements.Line, GameUiElements.Primary);
            testButton = GameUiElements.Button("Test", GameUiElements.LocalBox("Test Bounds",
                panel, 330, 520, 132, 44), "测试连接", font, 18,
                GameUiElements.Line, GameUiElements.Primary);
            applyButton = GameUiElements.Button("Apply", GameUiElements.LocalBox("Apply Bounds",
                panel, 474, 520, 138, 44), "应用", font, 18,
                GameUiElements.Mint, GameUiElements.Graphite);
            cancelButton.onClick.AddListener(Close);
            clearKeyButton.onClick.AddListener(MarkClearKey);
            testButton.onClick.AddListener(TestConnection);
            applyButton.onClick.AddListener(Apply);
            urlField.onValueChanged.AddListener(_ => DraftChanged());
            modelField.onValueChanged.AddListener(_ => DraftChanged());
            keyField.onValueChanged.AddListener(_ => DraftChanged());
            timeoutField.onValueChanged.AddListener(_ => DraftChanged());
            gameObject.SetActive(false);
        }

        public void Bind(GameAiSettingsBindings value) { bindings = value; }
        public void SetArt(Sprite corner)
        {
            GameUiElements.ApplyCorners(cornerImages, corner);
        }

        public void Open()
        {
            if (!built) return;
            gameObject.SetActive(true);
            Reload();
            SetFeedback("编辑后点击应用；测试连接只检查已应用配置。", GameUiElements.Secondary);
        }

        public void Close()
        {
            if (!IsOpen) return;
            StopTest();
            gameObject.SetActive(false);
            bindings?.Closed?.Invoke();
        }

        public void SetExternalFeedback(string message, bool error)
        {
            if (!built) return;
            SetFeedback(message, error ? GameUiElements.Failure : GameUiElements.Secondary);
        }

        void OnDisable() { StopTest(); }
        void OnDestroy() { StopTest(); }

        void Reload()
        {
            var current = bindings?.Read?.Invoke() ?? default;
            urlField.SetTextWithoutNotify(current.BaseUrl ?? string.Empty);
            modelField.SetTextWithoutNotify(current.ModelId ?? string.Empty);
            timeoutField.SetTextWithoutNotify((current.TimeoutSeconds > 0
                ? current.TimeoutSeconds : 15).ToString());
            keyField.SetTextWithoutNotify(string.Empty);
            clearKeyRequested = false;
            RefreshKeyHint(current.HasKey);
            endpoint.text = string.IsNullOrEmpty(current.EndpointUrl)
                ? "请求地址：待配置" : "请求地址：" + current.EndpointUrl;
        }

        void RefreshKeyHint(bool hasKey)
        {
            keyHint.text = clearKeyRequested ? "应用后清除内存 Key；取消不会清除。"
                : hasKey ? "Key 已在内存中设置；留空保持现值。"
                : "Key 未设置；退出程序后不会保留。";
        }

        void MarkClearKey()
        {
            keyField.SetTextWithoutNotify(string.Empty);
            clearKeyRequested = true;
            RefreshKeyHint(bindings != null && bindings.Read != null && bindings.Read().HasKey);
            DraftChanged();
        }

        void DraftChanged()
        {
            StopTest();
            if (clearKeyRequested && !string.IsNullOrEmpty(keyField.text))
            {
                clearKeyRequested = false;
                RefreshKeyHint(bindings != null && bindings.Read != null && bindings.Read().HasKey);
            }
            SetFeedback("草稿尚未应用。", GameUiElements.Secondary);
        }

        bool TryDraft(out GameAiSettingsDraft draft)
        {
            draft = default;
            var url = urlField.text.Trim();
            var model = modelField.text.Trim();
            if ((url.Length == 0) != (model.Length == 0))
            {
                SetFeedback("请同时填写 API 地址和模型 ID，或同时留空使用离线模式。",
                    GameUiElements.Amber);
                return false;
            }
            if (!int.TryParse(timeoutField.text, out var timeout) || timeout < 1 || timeout > 120)
            {
                SetFeedback("超时请输入 1–120 秒。", GameUiElements.Amber);
                return false;
            }
            draft = new GameAiSettingsDraft
            {
                BaseUrl = url,
                ModelId = model,
                TimeoutSeconds = timeout,
                NewKey = keyField.text.Trim(),
                ClearKey = clearKeyRequested && string.IsNullOrWhiteSpace(keyField.text)
            };
            return true;
        }

        void Apply()
        {
            if (!TryDraft(out var draft)) return;
            if (bindings?.Apply == null)
            {
                SetFeedback("配置服务尚未接线。", GameUiElements.Amber);
                return;
            }
            var result = bindings.Apply(draft);
            if (!result.Success)
            {
                SetFeedback(string.IsNullOrEmpty(result.Error) ? "配置未能应用。" : result.Error,
                    GameUiElements.Amber);
                return;
            }
            Reload();
            SettingsApplied?.Invoke(result.Changed);
            SetFeedback(result.Changed ? "配置已应用。" : "配置没有变化。", GameUiElements.Mint);
        }

        void TestConnection()
        {
            if (!TryDraft(out var draft)) return;
            if (bindings?.Read == null)
            {
                SetFeedback("配置服务尚未接线。", GameUiElements.Amber);
                return;
            }
            var current = bindings.Read();
            if (HasUnappliedChanges(draft, current))
            {
                SetFeedback("请先应用草稿，再测试连接。", GameUiElements.Amber);
                return;
            }
            if (!current.HasKey)
            {
                SetFeedback("请先设置 Key，再测试连接。", GameUiElements.Amber);
                return;
            }
            if (bindings?.TestConnection == null)
            {
                SetFeedback("连接测试尚未接线。", GameUiElements.Amber);
                return;
            }
            StopTest();
            var generation = ++testGeneration;
            testing = true;
            testButton.interactable = false;
            SetFeedback("正在检查连接…", GameUiElements.Mint);
            if (!bindings.TestConnection(result =>
                {
                    if (!this || !IsOpen || generation != testGeneration) return;
                    testing = false;
                    testButton.interactable = true;
                    SetFeedback(string.IsNullOrEmpty(result.Message)
                        ? (result.Success ? "连接正常。" : "连接检查失败。") : result.Message,
                        result.Success ? GameUiElements.Mint : GameUiElements.Amber);
                    ConnectionTestCompleted?.Invoke(result);
                }))
            {
                testing = false;
                testButton.interactable = true;
                SetFeedback("未能启动连接检查；请稍后重试。", GameUiElements.Amber);
            }
        }

        static bool HasUnappliedChanges(GameAiSettingsDraft draft, GameAiSettingsSnapshot current)
        {
            return !string.Equals(draft.BaseUrl.TrimEnd('/'), (current.BaseUrl ?? string.Empty).TrimEnd('/'),
                       StringComparison.Ordinal) ||
                   !string.Equals(draft.ModelId, (current.ModelId ?? string.Empty).Trim(),
                       StringComparison.Ordinal) ||
                   draft.TimeoutSeconds != current.TimeoutSeconds ||
                   !string.IsNullOrEmpty(draft.NewKey) || draft.ClearKey;
        }

        void StopTest()
        {
            testGeneration++;
            if (testing) bindings?.CancelTest?.Invoke();
            testing = false;
            if (testButton) testButton.interactable = true;
        }

        void SetFeedback(string message, Color color)
        {
            if (!feedback) return;
            feedback.text = message ?? string.Empty;
            feedback.color = color;
        }

        static TMP_InputField FieldRow(Transform parent, TMP_FontAsset font, string label,
            string placeholder, bool password = false)
        {
            var row = new GameObject(label + " Row", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 76);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 76;
            GameUiElements.Text("Label", GameUiElements.LocalBox("Label Bounds", row.transform,
                0, 0, 588, 28), label, font, 16, GameUiElements.Secondary);
            return GameUiElements.Input("Input", GameUiElements.LocalBox("Input Bounds", row.transform,
                0, 30, 588, 42), font, placeholder, password);
        }
    }
}
