using System;
using Echo.NativeGame.GameFlow.Results;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // The ordinary ending page. The flow owner decides when to show it and supplies
    // Narrative's actual title and short text; this view never chooses an ending.
    [RequireComponent(typeof(RectTransform))]
    public sealed class NativeEndingView : MonoBehaviour
    {
        [Header("Optional GF01-04 decoration")]
        public Sprite panelCornerSprite;

        TextMeshProUGUI titleLabel, bodyLabel;
        UnityEngine.UI.ScrollRect bodyScroll;
        RectTransform bodyContent, panel;
        readonly UnityEngine.UI.Image[] cornerArt = new UnityEngine.UI.Image[4];
        UnityEngine.UI.Button resultsButton;
        bool built;

        public event Action ViewResultsRequested;

        public static NativeEndingView Create(Transform canvasParent, TMP_FontAsset chineseFont)
        {
            var root = FlowUiElements.OverlayRoot("GF01 Ending", canvasParent);
            var view = root.gameObject.AddComponent<NativeEndingView>();
            view.Build(chineseFont);
            return view;
        }

        public void Build(TMP_FontAsset chineseFont)
        {
            if (built) return;
            built = true;
            var root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            FlowUiElements.Image(FlowUiElements.Fill("World Dim", root),
                new Color(0f, .025f, .027f, .83f), true);
            panel = new GameObject("Ending Text", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(root, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(792, 408);
            FlowUiElements.Image(FlowUiElements.Fill("Face", panel),
                new Color32(24, 40, 43, 243));
            cornerArt[0] = FlowUiElements.Image(FlowUiElements.Box("Top Left Corner", panel,
                0, 0, 96, 96), Color.clear);
            cornerArt[1] = FlowUiElements.Image(FlowUiElements.Box("Top Right Corner", panel,
                792, 0, 96, 96), Color.clear);
            cornerArt[1].rectTransform.localScale = new Vector3(-1, 1, 1);
            cornerArt[2] = FlowUiElements.Image(FlowUiElements.Box("Bottom Left Corner", panel,
                0, 408, 96, 96), Color.clear);
            cornerArt[2].rectTransform.localScale = new Vector3(1, -1, 1);
            cornerArt[3] = FlowUiElements.Image(FlowUiElements.Box("Bottom Right Corner", panel,
                792, 408, 96, 96), Color.clear);
            cornerArt[3].rectTransform.localScale = new Vector3(-1, -1, 1);
            FlowUiElements.Outline(panel, 792, 408, FlowUiElements.Edge);
            FlowUiElements.Image(FlowUiElements.Box("Signal", panel, 28, 35, 34, 3), FlowUiElements.Mint);
            FlowUiElements.Label(FlowUiElements.Box("Status", panel, 72, 24, 660, 30),
                chineseFont, "本局结局", 16, FlowUiElements.Muted);
            titleLabel = FlowUiElements.Label(FlowUiElements.Box("Title", panel, 28, 65, 736, 54),
                chineseFont, "", 36, FlowUiElements.Text);

            var viewport = FlowUiElements.Box("Story Viewport", panel, 28, 127, 736, 183);
            FlowUiElements.Image(viewport, new Color(0, 0, 0, .001f), true);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            bodyContent = new GameObject("Story Content", typeof(RectTransform)).GetComponent<RectTransform>();
            bodyContent.SetParent(viewport, false);
            bodyContent.anchorMin = new Vector2(0, 1);
            bodyContent.anchorMax = Vector2.one;
            bodyContent.pivot = new Vector2(.5f, 1);
            bodyContent.sizeDelta = new Vector2(0, 183);
            bodyContent.anchoredPosition = Vector2.zero;
            bodyLabel = FlowUiElements.Label(FlowUiElements.Fill("Story", bodyContent),
                chineseFont, "", 21, FlowUiElements.Text);
            bodyLabel.alignment = TextAlignmentOptions.TopLeft;
            bodyScroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            bodyScroll.viewport = viewport;
            bodyScroll.content = bodyContent;
            bodyScroll.horizontal = false;
            bodyScroll.vertical = true;
            bodyScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            bodyScroll.scrollSensitivity = 24;

            FlowUiElements.Image(FlowUiElements.Box("Rule", panel, 28, 323, 736, 2),
                FlowUiElements.Edge);
            resultsButton = FlowUiElements.Button(FlowUiElements.Box("View Results", panel,
                526, 342, 238, 52), chineseFont, "查看本局结果", FlowUiElements.Mint,
                FlowUiElements.Ink);
            resultsButton.onClick.AddListener(() => ViewResultsRequested?.Invoke());
            ApplyArt();
            gameObject.SetActive(false);
        }

        public void ApplyArt()
        {
            foreach (var image in cornerArt)
            {
                if (!image) continue;
                image.sprite = panelCornerSprite;
                image.color = panelCornerSprite ? Color.white : Color.clear;
                image.raycastTarget = false;
            }
        }

        public void Bind(string title, string narrativeText)
        {
            if (!built) return;
            titleLabel.text = title ?? string.Empty;
            bodyLabel.text = narrativeText ?? string.Empty;
            bodyContent.sizeDelta = new Vector2(0,
                Mathf.Max(183, bodyLabel.GetPreferredValues(bodyLabel.text, 736, 0).y + 10));
            bodyScroll.verticalNormalizedPosition = 1;
        }

        public void Bind(RunResultSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Success) return;
            Bind(snapshot.Title, snapshot.PresentationText);
        }

        public void Show(bool visible)
        {
            if (visible) transform.SetAsLastSibling();
            gameObject.SetActive(visible);
        }

        void LateUpdate()
        {
            if (panel) FlowUiElements.FitToParent(panel, GetComponent<RectTransform>(), 24, 24);
        }
    }
}
