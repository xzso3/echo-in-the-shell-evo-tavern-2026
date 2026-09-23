using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    public sealed class GameIntroBindings
    {
        public Func<bool> BeginAction;
        public Action RetryLoad;
        public Action ReturnToMenu;
    }

    [DisallowMultipleComponent]
    public sealed class GameIntroView : MonoBehaviour
    {
        UnityEngine.UI.Image[] cornerImages;
        readonly List<UnityEngine.UI.Image> keycaps = new List<UnityEngine.UI.Image>();
        UnityEngine.UI.Button beginButton, retryButton, returnButton;
        TMP_Text errorLabel;
        GameIntroBindings bindings;
        bool built;
        bool submitted;

        public bool IsOpen => gameObject.activeSelf;

        public void Build(TMP_FontAsset font)
        {
            if (built) return;
            built = true;
            GameUiElements.Image("World Shade", transform, new Color(0.02f, 0.05f, 0.06f, 0.72f), true);
            var panel = GameUiElements.Box("Intro Panel", transform, 280, 116, 720, 488);
            GameUiElements.Image("Panel Surface", panel, GameUiElements.Panel, true);
            cornerImages = GameUiElements.PanelCorners(panel, 100);
            GameUiElements.BoxText("Section", transform, "FIELD MANUAL / 01", font, 17,
                GameUiElements.Mint, 320, 138, 500, 28);
            GameUiElements.BoxText("Title", transform, "操作说明", font, 34,
                GameUiElements.Primary, 320, 174, 600, 55);
            GameUiElements.BoxImage("Rule", transform, 320, 235, 640, 2, GameUiElements.Line);
            KeyCell(font, "WASD", "移动", 320, 258);
            KeyCell(font, "Space", "开火／停火", 650, 258);
            KeyCell(font, "E", "互动", 320, 344);
            KeyCell(font, "Tab", "平板", 650, 344);
            GameUiElements.BoxText("Begin Note", transform, "开始行动后，本局计时与战斗启动。", font, 19,
                GameUiElements.Secondary, 320, 438, 640, 40);
            errorLabel = GameUiElements.BoxText("Error", transform, string.Empty, font, 17,
                GameUiElements.Failure, 320, 480, 600, 45);
            returnButton = GameUiElements.BoxButton("Return To Menu", transform, "返回主菜单",
                font, 19, GameUiElements.Line, GameUiElements.Primary, 320, 526, 188, 56);
            beginButton = GameUiElements.BoxButton("Begin Action", transform, "开始行动", font, 22,
                GameUiElements.Mint, GameUiElements.Graphite, 748, 526, 212, 56);
            retryButton = GameUiElements.BoxButton("Retry Loading", transform, "重试加载", font, 20,
                GameUiElements.Mint, GameUiElements.Graphite, 748, 526, 212, 56);
            beginButton.onClick.AddListener(Begin);
            retryButton.onClick.AddListener(() => bindings?.RetryLoad?.Invoke());
            returnButton.onClick.AddListener(() => bindings?.ReturnToMenu?.Invoke());
            returnButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Bind(GameIntroBindings value) { bindings = value; }
        public void SetArt(Sprite corner, Sprite button, Sprite keycap)
        {
            GameUiElements.ApplyCorners(cornerImages, corner);
            GameUiElements.ApplyButtonSprite(beginButton, button, 72);
            GameUiElements.ApplyButtonSprite(retryButton, button, 72);
            GameUiElements.ApplyButtonSprite(returnButton, button, 64);
            foreach (var image in keycaps) GameUiElements.ApplySprite(image, keycap);
        }

        public void Show()
        {
            if (!built) return;
            submitted = false;
            beginButton.interactable = true;
            beginButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            errorLabel.text = string.Empty;
            gameObject.SetActive(true);
        }

        public void Hide() { gameObject.SetActive(false); }

        public void SetLoadingError(string message)
        {
            if (!built) return;
            submitted = false;
            beginButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(true);
            errorLabel.text = string.IsNullOrEmpty(message) ? "加载失败，请返回主菜单。" : message;
            returnButton.gameObject.SetActive(true);
            gameObject.SetActive(true);
        }

        public void SetActionError(string message)
        {
            if (!built) return;
            submitted = false;
            beginButton.gameObject.SetActive(true);
            beginButton.interactable = true;
            retryButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            errorLabel.text = message ?? string.Empty;
            gameObject.SetActive(true);
        }

        // Intro consumes Esc without allowing the run to begin.
        public bool HandleBack() { return IsOpen; }

        void Begin()
        {
            if (submitted) return;
            submitted = true;
            beginButton.interactable = false;
            if (bindings?.BeginAction?.Invoke() == true)
            {
                Hide();
                return;
            }
            submitted = false;
            beginButton.interactable = true;
            if (string.IsNullOrEmpty(errorLabel.text))
                errorLabel.text = "行动尚未就绪，请重试。";
        }

        void KeyCell(TMP_FontAsset font, string key, string action, float x, float y)
        {
            var rect = GameUiElements.Box("Key " + key, transform, x, y, 310, 70);
            GameUiElements.Image("Cell Surface", rect, new Color32(19, 48, 47, 255));
            var cap = GameUiElements.LocalBox("Key Cap", rect, 12, 12, 106, 46);
            keycaps.Add(GameUiElements.Image("Cap Surface", cap, GameUiElements.Line));
            GameUiElements.Text("Key Label", cap, key, font, 20, GameUiElements.Primary,
                TextAlignmentOptions.Center);
            GameUiElements.Text("Action", GameUiElements.LocalBox("Action Bounds", rect,
                132, 12, 166, 46), action, font, 20, GameUiElements.Primary);
        }
    }
}
