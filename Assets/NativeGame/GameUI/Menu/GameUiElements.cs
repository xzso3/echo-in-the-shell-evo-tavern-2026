using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // All coordinates below are relative to the 1280 x 720 design canvas.
    internal static class GameUiElements
    {
        internal static readonly Color Graphite = new Color32(17, 27, 31, 255);
        internal static readonly Color Panel = new Color32(24, 40, 43, 255);
        internal static readonly Color Line = new Color32(66, 102, 80, 255);
        internal static readonly Color Primary = new Color32(213, 232, 216, 255);
        internal static readonly Color Secondary = new Color32(136, 164, 152, 255);
        internal static readonly Color Mint = new Color32(156, 222, 179, 255);
        internal static readonly Color Amber = new Color32(223, 177, 109, 255);
        internal static readonly Color Failure = new Color32(207, 138, 120, 255);

        internal static RectTransform Fill(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

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

        internal static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = Rect(name, parent, new Vector2(x / 1280f, 1f - (y + height) / 720f),
                new Vector2((x + width) / 1280f, 1f - y / 720f), Vector2.zero, Vector2.zero);
            return rect;
        }

        internal static RectTransform LocalBox(string name, Transform parent, float x, float y,
            float width, float height)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1),
                Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal static RectTransform CenterBox(string name, Transform parent, float width, float height)
        {
            var rect = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal static UnityEngine.UI.Image Image(string name, Transform parent, Color color,
            bool blocksRaycasts = false)
        {
            var image = Fill(name, parent).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = blocksRaycasts;
            return image;
        }

        internal static UnityEngine.UI.Image BoxImage(string name, Transform parent, float x, float y,
            float width, float height, Color color, bool blocksRaycasts = false)
        {
            var image = Box(name, parent, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = blocksRaycasts;
            return image;
        }

        internal static TextMeshProUGUI Text(string name, Transform parent, string value, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = Fill(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font ? font : TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.color = color;
            label.text = value;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        internal static TextMeshProUGUI BoxText(string name, Transform parent, string value,
            TMP_FontAsset font, float size, Color color, float x, float y, float width, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            return Text(name, Box(name + " Bounds", parent, x, y, width, height), value, font, size, color, alignment);
        }

        internal static UnityEngine.UI.Button Button(string name, Transform parent, string value,
            TMP_FontAsset font, float size, Color background, Color foreground)
        {
            var image = Image(name, parent, background, true);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 1f, 0.9f, 1f);
            colors.pressedColor = new Color(0.63f, 0.82f, 0.69f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.52f, 0.48f, 0.7f);
            button.colors = colors;
            var label = Rect("Label", image.transform, Vector2.zero, Vector2.one,
                new Vector2(10, 2), new Vector2(-10, -2));
            Text("Text", label, value, font, size, foreground, TextAlignmentOptions.Center);
            return button;
        }

        internal static UnityEngine.UI.Button BoxButton(string name, Transform parent, string value,
            TMP_FontAsset font, float size, Color background, Color foreground,
            float x, float y, float width, float height)
        {
            return Button(name, Box(name + " Bounds", parent, x, y, width, height), value,
                font, size, background, foreground);
        }

        internal static TMP_InputField Input(string name, Transform parent, TMP_FontAsset font,
            string placeholder, bool password = false)
        {
            var image = Image(name, parent, new Color32(13, 31, 34, 255), true);
            var field = image.gameObject.AddComponent<TMP_InputField>();
            var area = Rect("Text Area", image.transform, Vector2.zero, Vector2.one,
                new Vector2(12, 4), new Vector2(-12, -4));
            area.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hint = Text("Placeholder", area, placeholder, font, 18, Secondary);
            var input = Text("Input Text", area, string.Empty, font, 18, Primary);
            field.textViewport = area;
            field.textComponent = input;
            field.placeholder = hint;
            field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.customCaretColor = true;
            field.caretColor = Mint;
            field.selectionColor = new Color(0.37f, 0.69f, 0.49f, 0.45f);
            return field;
        }

        internal static void ApplySprite(UnityEngine.UI.Image image, Sprite sprite)
        {
            if (!image || !sprite) return;
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0f
                ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white;
        }

        internal static void ApplyButtonSprite(UnityEngine.UI.Button button, Sprite sprite,
            float artHeight)
        {
            if (!button || !sprite) return;
            var existing = button.transform.Find("Button Art");
            var decoration = existing
                ? existing.GetComponent<UnityEngine.UI.Image>()
                : Fill("Button Art", button.transform).gameObject.AddComponent<UnityEngine.UI.Image>();
            decoration.transform.SetAsFirstSibling();
            decoration.raycastTarget = false;
            var rect = decoration.rectTransform;
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.offsetMin = new Vector2(0, -artHeight * 0.5f);
            rect.offsetMax = new Vector2(0, artHeight * 0.5f);
            ApplySprite(decoration, sprite);
            // The native Button remains the full-size hit target; sprite whitespace is decorative.
            var hitImage = button.GetComponent<UnityEngine.UI.Image>();
            if (hitImage) hitImage.color = new Color(0, 0, 0, 0.001f);
            button.targetGraphic = decoration;
        }

        internal static UnityEngine.UI.Image[] PanelCorners(Transform panel, float size)
        {
            var anchors = new[]
            {
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(1, 0), new Vector2(0, 0)
            };
            var images = new UnityEngine.UI.Image[4];
            for (int i = 0; i < images.Length; i++)
            {
                var box = Rect("Corner " + i, panel, anchors[i], anchors[i],
                    Vector2.zero, Vector2.zero);
                box.pivot = anchors[i];
                box.anchoredPosition = Vector2.zero;
                box.sizeDelta = new Vector2(size, size);
                images[i] = Image("Art", box, Color.clear);
                images[i].rectTransform.localEulerAngles = new Vector3(0, 0, -90f * i);
            }
            return images;
        }

        internal static void ApplyCorners(UnityEngine.UI.Image[] images, Sprite sprite)
        {
            if (images == null) return;
            foreach (var image in images) ApplySprite(image, sprite);
        }
    }
}
