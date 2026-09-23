using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Small, code-owned primitives. All copy and hit targets remain native TMP/uGUI.
    internal static class FlowUiElements
    {
        internal static readonly Color Ink = new Color32(17, 27, 31, 255);
        internal static readonly Color Surface = new Color32(24, 40, 43, 255);
        internal static readonly Color Edge = new Color32(66, 102, 80, 255);
        internal static readonly Color Text = new Color32(213, 232, 216, 255);
        internal static readonly Color Muted = new Color32(136, 164, 152, 255);
        internal static readonly Color Mint = new Color32(156, 222, 179, 255);
        internal static readonly Color Failure = new Color32(207, 138, 120, 255);

        internal static RectTransform Fill(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TextMeshProUGUI Label(RectTransform rect, TMP_FontAsset font, string value,
            int size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font ? font : TMP_Settings.defaultFontAsset;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        internal static UnityEngine.UI.Button Button(RectTransform rect, TMP_FontAsset font, string value,
            Color background, Color foreground, int size = 20)
        {
            var image = Image(rect, background, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(.76f, 1f, .82f);
            colors.pressedColor = new Color(.55f, .78f, .62f);
            colors.disabledColor = new Color(.38f, .46f, .43f);
            button.colors = colors;
            var label = Label(Fill("Label", rect), font, value, size, foreground,
                TextAlignmentOptions.Center);
            label.margin = new Vector4(8, 3, 8, 3);
            return button;
        }

        internal static void Outline(Transform parent, float width, float height, Color color)
        {
            Image(Box("Top Edge", parent, 0, 0, width, 2), color);
            Image(Box("Left Edge", parent, 0, 0, 2, height), color);
            Image(Box("Right Edge", parent, width - 2, 0, 2, height), color);
            Image(Box("Bottom Edge", parent, 0, height - 2, width, 2), color);
        }

        internal static RectTransform OverlayRoot(string name, Transform canvasParent)
        {
            var root = Fill(name, canvasParent);
            root.SetAsLastSibling();
            return root;
        }

        internal static void FitToParent(RectTransform panel, RectTransform parent, float horizontalPadding,
            float verticalPadding)
        {
            if (!panel || !parent) return;
            float scale = Mathf.Min(1f, (parent.rect.width - horizontalPadding) / panel.sizeDelta.x,
                (parent.rect.height - verticalPadding) / panel.sizeDelta.y);
            panel.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }
    }
}
