using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Parent is a scene Canvas with GraphicRaycaster; INT keeps one EventSystem.
    // These builders create only view objects. Flow/navigation/pause own all state.
    public static class GameFlowUiFactory
    {
        public static GameMenuView CreateMenu(Transform canvasParent, TMP_FontAsset font,
            GameUiArtCatalog art)
        {
            var root = GameUiElements.Fill("Game Main Menu", canvasParent);
            var view = root.gameObject.AddComponent<GameMenuView>();
            view.Build(font);
            if (art)
            {
                view.SetArt(art.mainMenuFacility, art.lightPanelCorner, art.terminalButtonBlank);
                view.SettingsView.SetArt(art.lightPanelCorner);
            }
            return view;
        }

        public static GameIntroView CreateIntro(Transform canvasParent, TMP_FontAsset font,
            GameUiArtCatalog art)
        {
            var root = GameUiElements.Fill("Game Intro", canvasParent);
            var view = root.gameObject.AddComponent<GameIntroView>();
            view.Build(font);
            if (art) view.SetArt(art.lightPanelCorner, art.terminalButtonBlank, art.keycapBlank);
            return view;
        }

        public static GamePauseView CreatePause(Transform canvasParent, TMP_FontAsset font,
            GameUiArtCatalog art)
        {
            var root = GameUiElements.Fill("Game Pause", canvasParent);
            var view = root.gameObject.AddComponent<GamePauseView>();
            view.Build(font);
            if (art)
            {
                view.SetArt(art.lightPanelCorner, art.terminalButtonBlank);
                view.SettingsView.SetArt(art.lightPanelCorner);
            }
            return view;
        }
    }
}
