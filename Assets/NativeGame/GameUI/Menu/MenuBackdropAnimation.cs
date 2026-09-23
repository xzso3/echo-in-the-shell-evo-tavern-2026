using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Every transparent frame uses the same canvas coordinates as the static image.
    [DisallowMultipleComponent]
    public sealed class MenuBackdropAnimation : MonoBehaviour
    {
        sealed class FrameLayer
        {
            readonly UnityEngine.UI.Image image;
            Sprite[] frames;
            float seconds;
            float elapsed;
            int index;

            internal FrameLayer(UnityEngine.UI.Image image)
            {
                this.image = image;
                image.enabled = false;
            }

            internal void Set(Sprite[] value, float frameSeconds)
            {
                frames = value;
                seconds = frameSeconds > 0f ? frameSeconds : 0.2f;
                elapsed = 0f;
                index = NextValid(0);
                image.enabled = index >= 0;
                image.sprite = index >= 0 ? frames[index] : null;
            }

            internal void Tick(float delta)
            {
                if (!image.enabled || frames.Length < 2) return;
                elapsed += delta;
                if (elapsed < seconds) return;
                var steps = Mathf.FloorToInt(elapsed / seconds);
                elapsed -= steps * seconds;
                var next = NextValid((index + steps) % frames.Length);
                if (next < 0) return;
                index = next;
                image.sprite = frames[index];
            }

            int NextValid(int start)
            {
                if (frames == null || frames.Length == 0) return -1;
                for (var i = 0; i < frames.Length; i++)
                {
                    var candidate = (start + i) % frames.Length;
                    if (frames[candidate]) return candidate;
                }
                return -1;
            }
        }

        UnityEngine.UI.Image background;
        FrameLayer rain, water, lights;

        internal void Build()
        {
            if (background) return;
            background = GameUiElements.Image("Static Rainy City", transform, GameUiElements.Graphite);
            rain = new FrameLayer(GameUiElements.Image("Rain Frames", transform, Color.white));
            water = new FrameLayer(GameUiElements.Image("Water Frames", transform, Color.white));
            lights = new FrameLayer(GameUiElements.Image("Lights Frames", transform, Color.white));
        }

        internal void SetArt(GameUiArtCatalog art)
        {
            if (!background) Build();
            if (art && art.menuBackgroundStatic)
                GameUiElements.ApplySprite(background, art.menuBackgroundStatic);
            rain.Set(art ? art.menuRainFrames : null, art ? art.menuRainFrameSeconds : 0f);
            water.Set(art ? art.menuWaterFrames : null, art ? art.menuWaterFrameSeconds : 0f);
            lights.Set(art ? art.menuLightsFrames : null, art ? art.menuLightsFrameSeconds : 0f);
        }

        void Update()
        {
            if (rain == null) return;
            var delta = Time.unscaledDeltaTime;
            rain.Tick(delta);
            water.Tick(delta);
            lights.Tick(delta);
        }
    }
}
