using System;
using System.Text;
using Echo.NativeGame.GameFlow.Results;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Shared success/death result display. Every value comes from the frozen
    // GF01-03 snapshot; this view does not inspect mutable run owners.
    [RequireComponent(typeof(RectTransform))]
    public sealed class NativeResultsView : MonoBehaviour
    {
        [Header("Optional GF01-04 decoration")]
        public Sprite panelCornerSprite;
        public Sprite buttonArtSprite;

        TextMeshProUGUI titleLabel, summaryLabel, timeLabel, killsLabel, syncLabel,
            differenceLabel, eventsLabel, errorLabel, replayLabel;
        RectTransform panel, eventsContent;
        UnityEngine.UI.ScrollRect eventsScroll;
        readonly UnityEngine.UI.Image[] cornerArt = new UnityEngine.UI.Image[4];
        UnityEngine.UI.Image stateStripe, replayArt, menuArt;
        UnityEngine.UI.Button replayButton, menuButton;
        bool built;

        public event Action ReplayRequested;
        public event Action MenuRequested;

        public static NativeResultsView Create(Transform canvasParent, TMP_FontAsset chineseFont)
        {
            var root = FlowUiElements.OverlayRoot("GF01 Results", canvasParent);
            var view = root.gameObject.AddComponent<NativeResultsView>();
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
                new Color(0f, .023f, .026f, .91f), true);
            panel = new GameObject("Result Terminal", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(root, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(920, 576);
            FlowUiElements.Image(FlowUiElements.Fill("Face", panel), FlowUiElements.Surface);
            cornerArt[0] = FlowUiElements.Image(FlowUiElements.Box("Top Left Corner", panel,
                0, 0, 96, 96), Color.clear);
            cornerArt[1] = FlowUiElements.Image(FlowUiElements.Box("Top Right Corner", panel,
                920, 0, 96, 96), Color.clear);
            cornerArt[1].rectTransform.localScale = new Vector3(-1, 1, 1);
            cornerArt[2] = FlowUiElements.Image(FlowUiElements.Box("Bottom Left Corner", panel,
                0, 576, 96, 96), Color.clear);
            cornerArt[2].rectTransform.localScale = new Vector3(1, -1, 1);
            cornerArt[3] = FlowUiElements.Image(FlowUiElements.Box("Bottom Right Corner", panel,
                920, 576, 96, 96), Color.clear);
            cornerArt[3].rectTransform.localScale = new Vector3(-1, -1, 1);
            FlowUiElements.Outline(panel, 920, 576, FlowUiElements.Edge);
            stateStripe = FlowUiElements.Image(FlowUiElements.Box("State Stripe", panel, 0, 0, 7, 576),
                FlowUiElements.Mint);
            FlowUiElements.Label(FlowUiElements.Box("Eyebrow", panel, 50, 28, 820, 28),
                chineseFont, "本局结果", 16, FlowUiElements.Muted);
            titleLabel = FlowUiElements.Label(FlowUiElements.Box("Title", panel, 50, 58, 820, 54),
                chineseFont, "", 38, FlowUiElements.Text);
            summaryLabel = FlowUiElements.Label(FlowUiElements.Box("Summary", panel, 50, 116, 820, 54),
                chineseFont, "", 20, FlowUiElements.Text);
            FlowUiElements.Image(FlowUiElements.Box("Header Rule", panel, 50, 178, 820, 2),
                FlowUiElements.Edge);

            timeLabel = DataCell(panel, chineseFont, "本局用时", 50);
            killsLabel = DataCell(panel, chineseFont, "击败敌人", 260);
            syncLabel = DataCell(panel, chineseFont, "同步度", 470);
            differenceLabel = DataCell(panel, chineseFont, "差异度", 680);

            FlowUiElements.Label(FlowUiElements.Box("Events Title", panel, 50, 316, 820, 28),
                chineseFont, "关键经历", 18, FlowUiElements.Muted);
            var viewport = FlowUiElements.Box("Events Viewport", panel, 50, 350, 820, 116);
            FlowUiElements.Image(viewport, new Color(0, 0, 0, .001f), true);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            eventsContent = new GameObject("Events Content", typeof(RectTransform)).GetComponent<RectTransform>();
            eventsContent.SetParent(viewport, false);
            eventsContent.anchorMin = new Vector2(0, 1);
            eventsContent.anchorMax = Vector2.one;
            eventsContent.pivot = new Vector2(.5f, 1);
            eventsContent.sizeDelta = new Vector2(0, 116);
            eventsContent.anchoredPosition = Vector2.zero;
            eventsLabel = FlowUiElements.Label(FlowUiElements.Fill("Events", eventsContent),
                chineseFont, "", 18, FlowUiElements.Text);
            eventsLabel.alignment = TextAlignmentOptions.TopLeft;
            eventsScroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            eventsScroll.viewport = viewport;
            eventsScroll.content = eventsContent;
            eventsScroll.horizontal = false;
            eventsScroll.vertical = true;
            eventsScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            eventsScroll.scrollSensitivity = 24;

            errorLabel = FlowUiElements.Label(FlowUiElements.Box("Navigation Error", panel, 50, 475,
                820, 26), chineseFont, "", 16, FlowUiElements.Failure);
            replayButton = FlowUiElements.Button(FlowUiElements.Box("Replay", panel, 176, 502,
                274, 56), chineseFont, "再来一局", FlowUiElements.Mint, FlowUiElements.Ink);
            replayLabel = replayButton.GetComponentInChildren<TextMeshProUGUI>();
            menuButton = FlowUiElements.Button(FlowUiElements.Box("Return To Menu", panel, 470,
                502, 274, 56), chineseFont, "返回主菜单", FlowUiElements.Edge, FlowUiElements.Text);
            replayArt = AddButtonArt(replayButton);
            menuArt = AddButtonArt(menuButton);
            replayButton.onClick.AddListener(ChooseReplay);
            menuButton.onClick.AddListener(ChooseMenu);
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
            SetButtonArt(replayButton, replayArt, buttonArtSprite, FlowUiElements.Mint);
            SetButtonArt(menuButton, menuArt, buttonArtSprite, FlowUiElements.Edge);
        }

        public void Bind(RunResultSnapshot snapshot)
        {
            if (!built || snapshot == null) return;
            bool success = snapshot.Success;
            titleLabel.text = success ? snapshot.Title : "意识涣散";
            summaryLabel.text = snapshot.Summary ?? string.Empty;
            stateStripe.color = success ? FlowUiElements.Mint : FlowUiElements.Failure;
            replayLabel.text = success ? "再来一局" : "重试";
            int seconds = Mathf.Max(0, Mathf.FloorToInt(snapshot.ElapsedSeconds));
            timeLabel.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
            killsLabel.text = Mathf.Max(0, snapshot.Kills).ToString();
            syncLabel.text = snapshot.Sync.ToString();
            differenceLabel.text = snapshot.Difference.ToString();

            var events = snapshot.KeyEvents;
            var body = new StringBuilder();
            int count = events == null ? 0 : Mathf.Min(3, events.Count);
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrWhiteSpace(events[i])) continue;
                if (body.Length > 0) body.Append('\n');
                body.Append("— ").Append(events[i]);
            }
            eventsLabel.text = body.Length > 0 ? body.ToString() : "尚无关键记录";
            eventsContent.sizeDelta = new Vector2(0,
                Mathf.Max(116, eventsLabel.GetPreferredValues(eventsLabel.text, 820, 0).y + 8));
            eventsScroll.verticalNormalizedPosition = 1;
            errorLabel.text = string.Empty;
            SetNavigationBusy(false);
        }

        public void Show(bool visible)
        {
            if (visible) transform.SetAsLastSibling();
            gameObject.SetActive(visible);
        }

        public void SetNavigationBusy(bool busy)
        {
            if (replayButton) replayButton.interactable = !busy;
            if (menuButton) menuButton.interactable = !busy;
        }

        // The navigation owner calls this if TryLoad returns false.
        public void ShowNavigationError(string message)
        {
            errorLabel.text = string.IsNullOrWhiteSpace(message) ?
                "暂时无法切换场景，请重试。" : message;
            SetNavigationBusy(false);
        }

        void ChooseReplay()
        {
            if (ReplayRequested == null) return;
            SetNavigationBusy(true);
            ReplayRequested.Invoke();
        }

        void ChooseMenu()
        {
            if (MenuRequested == null) return;
            SetNavigationBusy(true);
            MenuRequested.Invoke();
        }

        void LateUpdate()
        {
            if (panel) FlowUiElements.FitToParent(panel, GetComponent<RectTransform>(), 24, 24);
        }

        static TextMeshProUGUI DataCell(Transform parent, TMP_FontAsset font, string name, float x)
        {
            var rect = FlowUiElements.Box(name, parent, x, 198, 190, 106);
            FlowUiElements.Image(FlowUiElements.Fill("Face", rect), FlowUiElements.Ink);
            FlowUiElements.Image(FlowUiElements.Box("Rule", rect, 0, 0, 190, 2), FlowUiElements.Edge);
            FlowUiElements.Label(FlowUiElements.Box("Name", rect, 14, 13, 162, 28),
                font, name, 16, FlowUiElements.Muted);
            return FlowUiElements.Label(FlowUiElements.Box("Value", rect, 14, 46, 162, 48),
                font, "", 27, FlowUiElements.Text);
        }

        static UnityEngine.UI.Image AddButtonArt(UnityEngine.UI.Button button)
        {
            var rect = new GameObject("Button Decoration", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(button.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(274, 90);
            var image = FlowUiElements.Image(rect, Color.clear);
            rect.SetAsFirstSibling();
            return image;
        }

        static void SetButtonArt(UnityEngine.UI.Button button, UnityEngine.UI.Image art,
            Sprite sprite, Color plainColor)
        {
            if (!button || !art) return;
            var hitImage = button.GetComponent<UnityEngine.UI.Image>();
            art.sprite = sprite;
            art.color = sprite ? Color.white : Color.clear;
            art.raycastTarget = false;
            hitImage.color = sprite ? new Color(1, 1, 1, .001f) : plainColor;
            button.targetGraphic = sprite ? art : hitImage;
        }
    }
}
