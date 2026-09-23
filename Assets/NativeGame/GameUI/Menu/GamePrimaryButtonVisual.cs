using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Echo.NativeGame.GameUI
{
    // Owns the color of the native Button and its TMP label in every input state.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class GamePrimaryButtonVisual : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISubmitHandler
    {
        enum VisualState { Normal, Hovered, Focused, Pressed, Disabled }

        static readonly Color NormalSurface = new Color32(11, 34, 37, 245);
        static readonly Color HoverSurface = new Color32(22, 60, 60, 255);
        static readonly Color FocusSurface = new Color32(22, 67, 65, 255);
        static readonly Color PressSurface = new Color32(29, 76, 71, 255);
        static readonly Color DisabledSurface = new Color32(16, 32, 35, 225);
        static readonly Color BrightText = new Color32(216, 245, 232, 255);
        static readonly Color WhiteText = new Color32(245, 255, 249, 255);
        static readonly Color DisabledText = new Color32(105, 132, 127, 255);
        static readonly Color MintLine = new Color32(112, 221, 180, 255);

        UnityEngine.UI.Button button;
        UnityEngine.UI.Image surface;
        TMP_Text label, arrow;
        UnityEngine.UI.Image[] frame;
        bool pointerOver;
        bool pointerDown;
        float submitUntil;
        VisualState lastState = (VisualState)(-1);

        void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            surface = GetComponent<UnityEngine.UI.Image>();
            label = transform.Find("Label/Text")?.GetComponent<TMP_Text>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            if (label)
            {
                var arrowBounds = GameUiElements.Rect("Arrow Bounds", transform,
                    new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(14, 3), new Vector2(42, -3));
                arrow = GameUiElements.Text("Arrow", arrowBounds, ">", label.font, 25,
                    BrightText, TextAlignmentOptions.Center);
            }
            frame = new[]
            {
                Edge("Top Frame", new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(0, -2), Vector2.zero),
                Edge("Bottom Frame", Vector2.zero, new Vector2(1, 0),
                    Vector2.zero, new Vector2(0, 2)),
                Edge("Left Frame", Vector2.zero, new Vector2(0, 1),
                    Vector2.zero, new Vector2(2, 0)),
                Edge("Right Frame", new Vector2(1, 0), Vector2.one,
                    new Vector2(-2, 0), Vector2.zero)
            };
            foreach (var edge in frame) edge.transform.SetAsFirstSibling();
            Refresh();
        }

        UnityEngine.UI.Image Edge(string name, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = GameUiElements.Rect(name, transform, min, max, offsetMin, offsetMax);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            return image;
        }

        void OnEnable() { lastState = (VisualState)(-1); Refresh(); }
        void OnDisable() { pointerOver = false; pointerDown = false; }

        void Update() { Refresh(); }

        void Refresh()
        {
            if (!button || !surface || !label || frame == null) return;
            var selected = EventSystem.current &&
                           EventSystem.current.currentSelectedGameObject == gameObject;
            var state = !button.IsActive() || !button.IsInteractable() ? VisualState.Disabled
                : pointerDown || Time.unscaledTime < submitUntil ? VisualState.Pressed
                : pointerOver ? VisualState.Hovered
                : selected ? VisualState.Focused : VisualState.Normal;
            if (state == lastState) return;
            lastState = state;
            surface.color = state == VisualState.Disabled ? DisabledSurface
                : state == VisualState.Pressed ? PressSurface
                : state == VisualState.Hovered ? HoverSurface
                : state == VisualState.Focused ? FocusSurface : NormalSurface;
            label.color = state == VisualState.Disabled ? DisabledText
                : state == VisualState.Normal ? BrightText : WhiteText;
            if (arrow) arrow.color = label.color;
            var line = state == VisualState.Disabled ? new Color32(60, 87, 83, 180)
                : state == VisualState.Normal ? new Color32(93, 173, 145, 210)
                : MintLine;
            foreach (var edge in frame) edge.color = line;
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { pointerOver = false; Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { pointerDown = true; Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { pointerDown = false; Refresh(); }
        public void OnSubmit(BaseEventData eventData)
        {
            submitUntil = Time.unscaledTime + 0.12f;
            Refresh();
        }
    }
}
