using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // A frame of already-owned run state. The view does not sample combat or input.
    public readonly struct NativeHudDisplay
    {
        public readonly float Health;
        public readonly float MaxHealth;
        public readonly bool AutoFire;
        public readonly string Objective;
        public readonly string InteractionPrompt;
        public readonly float ElapsedSeconds;
        public readonly int Kills;
        public readonly int Sync;
        public readonly int Difference;

        public NativeHudDisplay(float health, float maxHealth, bool autoFire, string objective,
            string interactionPrompt, float elapsedSeconds, int kills, int sync, int difference)
        {
            Health = health;
            MaxHealth = maxHealth;
            AutoFire = autoFire;
            Objective = objective;
            InteractionPrompt = interactionPrompt;
            ElapsedSeconds = elapsedSeconds;
            Kills = kills;
            Sync = sync;
            Difference = difference;
        }
    }

    // Build beneath the existing 1280x720 Canvas. NativeHud remains the sole input owner.
    [RequireComponent(typeof(RectTransform))]
    public sealed class NativeHudView : MonoBehaviour
    {
        [Header("Optional GF01-04 decorations; all labels remain TMP")]
        public Sprite healthFrameSprite;
        public Sprite objectiveFrameSprite;
        public Sprite fireFrameSprite;
        public Sprite tabletFrameSprite;
        public Sprite promptFrameSprite;
        public Sprite keycapSprite;

        TextMeshProUGUI healthLabel, objectiveLabel, fireLabel, statusLabel, scoresLabel, promptLabel;
        RectTransform healthFill;
        GameObject promptPanel;
        UnityEngine.UI.Image healthArt, objectiveArt, fireArt, tabletArt, promptArt, keycapArt;
        bool built;

        public static NativeHudView Create(Transform canvasParent, TMP_FontAsset chineseFont)
        {
            var root = FlowUiElements.OverlayRoot("GF01 HUD", canvasParent);
            var view = root.gameObject.AddComponent<NativeHudView>();
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

            var health = Corner("Health", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -22), new Vector2(286, 88));
            FlowUiElements.Image(FlowUiElements.Fill("Face", health),
                new Color(.067f, .106f, .122f, .81f));
            healthArt = FlowUiElements.Image(FlowUiElements.Fill("Decoration", health), Color.clear);
            FlowUiElements.Image(FlowUiElements.Box("Accent", health, 0, 0, 3, 88), FlowUiElements.Mint);
            healthLabel = FlowUiElements.Label(FlowUiElements.Box("Value", health, 16, 10, 254, 39),
                chineseFont, "", 23, FlowUiElements.Text);
            FlowUiElements.Image(FlowUiElements.Box("Bar", health, 16, 63, 254, 8), FlowUiElements.Edge);
            healthFill = FlowUiElements.Fill("Fill", health.Find("Bar"));
            FlowUiElements.Image(healthFill, FlowUiElements.Mint);

            var objective = Corner("Objective", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -22), new Vector2(332, 74));
            FlowUiElements.Image(FlowUiElements.Fill("Face", objective),
                new Color(.067f, .106f, .122f, .81f));
            objectiveArt = FlowUiElements.Image(FlowUiElements.Fill("Decoration", objective), Color.clear);
            FlowUiElements.Image(FlowUiElements.Box("Accent", objective, 0, 0, 3, 74), FlowUiElements.Mint);
            objectiveLabel = FlowUiElements.Label(FlowUiElements.Box("Text", objective, 14, 8, 302, 58),
                chineseFont, "", 18, FlowUiElements.Text);

            var fire = Corner("Fire and Run", Vector2.zero, Vector2.zero,
                new Vector2(24, 24), new Vector2(310, 78));
            FlowUiElements.Image(FlowUiElements.Fill("Face", fire),
                new Color(.067f, .106f, .122f, .81f));
            fireArt = FlowUiElements.Image(FlowUiElements.Fill("Decoration", fire), Color.clear);
            FlowUiElements.Image(FlowUiElements.Box("Accent", fire, 0, 0, 3, 78), FlowUiElements.Mint);
            fireLabel = FlowUiElements.Label(FlowUiElements.Box("Fire", fire, 15, 5, 285, 26),
                chineseFont, "", 18, FlowUiElements.Mint);
            statusLabel = FlowUiElements.Label(FlowUiElements.Box("Run", fire, 15, 31, 285, 21),
                chineseFont, "", 15, FlowUiElements.Text);
            scoresLabel = FlowUiElements.Label(FlowUiElements.Box("Mind", fire, 15, 52, 285, 21),
                chineseFont, "", 15, FlowUiElements.Muted);

            var tablet = Corner("Tablet Hint", new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-24, 24), new Vector2(316, 66));
            FlowUiElements.Image(FlowUiElements.Fill("Face", tablet),
                new Color(.067f, .106f, .122f, .81f));
            tabletArt = FlowUiElements.Image(FlowUiElements.Fill("Decoration", tablet), Color.clear);
            FlowUiElements.Image(FlowUiElements.Box("Accent", tablet, 313, 0, 3, 66), FlowUiElements.Mint);
            FlowUiElements.Label(FlowUiElements.Box("Key", tablet, 12, 10, 58, 46),
                chineseFont, "TAB", 18, FlowUiElements.Mint, TextAlignmentOptions.Center);
            FlowUiElements.Label(FlowUiElements.Box("Text", tablet, 78, 10, 224, 46),
                chineseFont, "战术平板", 20, FlowUiElements.Text);

            var prompt = Corner("Interaction", new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(0, 116), new Vector2(500, 60));
            promptPanel = prompt.gameObject;
            FlowUiElements.Image(FlowUiElements.Fill("Face", prompt),
                new Color(.067f, .106f, .122f, .92f));
            promptArt = FlowUiElements.Image(FlowUiElements.Fill("Decoration", prompt), Color.clear);
            keycapArt = FlowUiElements.Image(FlowUiElements.Box("Key Face", prompt, 7, 4, 52, 52),
                FlowUiElements.Edge);
            FlowUiElements.Label(FlowUiElements.Box("Key", prompt, 7, 4, 52, 52),
                chineseFont, "E", 20, FlowUiElements.Text, TextAlignmentOptions.Center);
            promptLabel = FlowUiElements.Label(FlowUiElements.Box("Action", prompt, 66, 6, 422, 48),
                chineseFont, "", 19, FlowUiElements.Text);
            promptPanel.SetActive(false);
            ApplyArt();
        }

        public void ApplyArt()
        {
            SetArt(healthArt, healthFrameSprite);
            SetArt(objectiveArt, objectiveFrameSprite);
            SetArt(fireArt, fireFrameSprite);
            SetArt(tabletArt, tabletFrameSprite);
            SetArt(promptArt, promptFrameSprite);
            SetArt(keycapArt, keycapSprite);
            if (keycapArt && !keycapSprite) keycapArt.color = FlowUiElements.Edge;
        }

        public void Bind(NativeHudDisplay state)
        {
            if (!built) return;
            healthLabel.text = "生命  " + Mathf.CeilToInt(state.Health) + " / " +
                Mathf.CeilToInt(state.MaxHealth);
            float ratio = state.MaxHealth > 0 ? Mathf.Clamp01(state.Health / state.MaxHealth) : 0;
            healthFill.anchorMax = new Vector2(ratio, 1);
            healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
            fireLabel.text = state.AutoFire ? "自动开火  /  Space 停火" : "停火  /  Space 开火";
            fireLabel.color = state.AutoFire ? FlowUiElements.Mint : new Color32(223, 177, 109, 255);
            objectiveLabel.text = state.Objective ?? string.Empty;
            int seconds = Mathf.Max(0, Mathf.FloorToInt(state.ElapsedSeconds));
            statusLabel.text = string.Format("用时 {0:00}:{1:00}  /  击败 {2}", seconds / 60,
                seconds % 60, Mathf.Max(0, state.Kills));
            scoresLabel.text = "同步 " + state.Sync + "  /  差异 " + state.Difference;
            bool hasPrompt = !string.IsNullOrWhiteSpace(state.InteractionPrompt);
            promptPanel.SetActive(hasPrompt);
            if (hasPrompt) promptLabel.text = state.InteractionPrompt;
        }

        public void Show(bool visible) => gameObject.SetActive(visible);

        RectTransform Corner(string name, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void SetArt(UnityEngine.UI.Image image, Sprite sprite)
        {
            if (!image) return;
            image.sprite = sprite;
            image.type = sprite && sprite.border.sqrMagnitude > 0 ?
                UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = sprite ? Color.white : Color.clear;
            image.raycastTarget = false;
        }
    }
}
