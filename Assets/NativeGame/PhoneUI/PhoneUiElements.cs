using TMPro;
using UnityEngine;

namespace Echo.NativeGame.PhoneUI
{
    // Native uGUI construction shared by the home screen and the field tablet.
    internal static class PhoneUiElements
    {
        internal static readonly Color Ink = new Color32(9, 25, 24, 255);
        internal static readonly Color Screen = new Color32(15, 42, 34, 255);
        internal static readonly Color Jade = new Color32(143, 220, 172, 255);
        internal static readonly Color Pale = new Color32(217, 237, 220, 255);
        internal static readonly Color Muted = new Color32(145, 177, 158, 255);
        internal static readonly Color Amber = new Color32(234, 187, 109, 255);
        internal static readonly Color Edge = new Color32(60, 105, 80, 255);

        internal static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        internal static RectTransform Fill(string name, Transform parent) =>
            Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        internal static UnityEngine.UI.Image Panel(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax, Color color, bool raycast = false)
        {
            var image = Rect(name, parent, min, max, offsetMin, offsetMax).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TextMeshProUGUI Text(string name, Transform parent, string value, TMP_FontAsset font,
            int size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var text = Fill(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        internal static UnityEngine.UI.Button Button(string name, Transform parent, string label,
            TMP_FontAsset font, Color background, Color foreground, int size = 19)
        {
            var image = Fill(name, parent).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = background;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.78f, 1f, 0.85f);
            colors.pressedColor = new Color(0.55f, 0.8f, 0.65f);
            colors.disabledColor = new Color(0.36f, 0.47f, 0.4f);
            button.colors = colors;
            var labelRect = Rect("Label", image.transform, Vector2.zero, Vector2.one,
                new Vector2(7, 3), new Vector2(-7, -3));
            Text("Text", labelRect, label, font, size, foreground, TextAlignmentOptions.Center);
            return button;
        }

        internal static TMP_InputField Input(string name, Transform parent, TMP_FontAsset font,
            string placeholder, bool password = false, int size = 19)
        {
            var image = Fill(name, parent).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Ink;
            var field = image.gameObject.AddComponent<TMP_InputField>();
            var area = Rect("Text Area", image.transform, Vector2.zero, Vector2.one,
                new Vector2(13, 5), new Vector2(-13, -5));
            area.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hint = Text("Placeholder", area, placeholder, font, size, Muted);
            var input = Text("Input Text", area, "", font, size, Pale);
            field.textViewport = area;
            field.textComponent = input;
            field.placeholder = hint;
            field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.customCaretColor = true;
            field.caretColor = Jade;
            field.selectionColor = new Color(0.29f, 0.62f, 0.43f, 0.45f);
            return field;
        }

        internal static UnityEngine.UI.ScrollRect Scroll(string name, Transform parent,
            out RectTransform content, out RectTransform viewport)
        {
            var root = Fill(name, parent);
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            viewport = Fill("Viewport", root);
            var viewImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            viewImage.color = new Color(0, 0, 0, 0.001f);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one,
                Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1);
            var group = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.spacing = 10;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;
            return scroll;
        }
    }
}
