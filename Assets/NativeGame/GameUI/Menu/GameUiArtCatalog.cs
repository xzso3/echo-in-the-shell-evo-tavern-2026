using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // INT binds these shared serialized references in Resources/GF01Art.asset.
    public sealed class GameUiArtCatalog : ScriptableObject
    {
        public Sprite mainMenuFacility;
        public Sprite lightPanelCorner;
        public Sprite terminalButtonBlank;
        public Sprite keycapBlank;

        [Header("GF02 Menu")]
        public Sprite menuBackgroundStatic;
        public Sprite echoInTheShellLogo;
        public Sprite cipherWorksLogo;
        public Sprite[] menuRainFrames;
        public Sprite[] menuWaterFrames;
        public Sprite[] menuLightsFrames;
        public float menuRainFrameSeconds = 0.16f;
        public float menuWaterFrameSeconds = 0.20f;
        public float menuLightsFrameSeconds = 0.30f;

        [Header("GF02 HUD")]
        public Sprite healthIcon;
        public Sprite weaponIcon;
        public Sprite equipmentIcon;
        public Sprite tabletIcon;
    }
}
