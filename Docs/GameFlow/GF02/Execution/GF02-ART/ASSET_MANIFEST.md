# GF02-ART asset manifest

Input: `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`, approved GF02 menu and HUD direction. All paths below are relative to the repository root. Assets are generated with the built-in image-gen tool. Source prompts are recorded in [PROMPTS.md](PROMPTS.md).

| Path | Use | Pixels | Alpha | State | Placement / binding advice |
| --- | --- | ---: | --- | --- | --- |
| `Assets/NativeGame/GameUI/Art/GF02/MenuBackgroundStatic.png` | Stable menu environment; no title, menu, status, or team mark baked in | 1672×941 | Opaque RGB | Final art, Unity import pending | Full-bleed 16:9 background. Preserve aspect; center crop only for other display ratios. Architecture, camera and scene remain fixed while local transparent overlays animate. |
| `Assets/NativeGame/GameUI/Art/GF02/EchoInTheShellLogo.png` | Exact English title, two lines | 1774×887 | RGBA, transparent surround | Final art, Unity import pending | Separate static overlay in upper-left dark field. Suggested visible width 32–37% of 16:9 screen; do not animate its transform or Sprite. Actual artwork nearly fills this PNG, so size against the on-screen result. |
| `Assets/NativeGame/GameUI/Art/GF02/CipherWorksLogo.png` | Team mark in lower-right | 2172×724 | RGBA, transparent surround | Final art, Unity import pending | Separate static overlay, suggested visible width 13–17% of 16:9 screen with 4–5% right/bottom margin. Artwork is inset within the PNG; size against visible mark. |

## Transparent menu animation layers

All twelve frame PNGs are **1672×941 RGBA**, the same canvas and origin as `MenuBackgroundStatic.png`. They are full-screen transparent canvases with artwork only in small local areas. Bind each group in the listed order to a separate `Sprite[]` player, stretched exactly over the static background without aspect-independent offsets. The camera, architecture, title, menu and team mark never occur in these frames.

| Group | Actual paths, ascending frame order | Visible area / alpha | Suggested loop timing | State |
| --- | --- | --- | --- | --- |
| Rain | `Assets/NativeGame/GameUI/Art/GF02/MenuRainFrame_00.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuRainFrame_01.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuRainFrame_02.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuRainFrame_03.png` | Sparse cool rain dashes in the right-side window/city, roughly x=56–95%, y=5–92%; left title region transparent. True alpha. | 0→1→2→3→0, **0.16 s/frame** (6.25 FPS). | Final art, Unity import pending |
| Water | `Assets/NativeGame/GameUI/Art/GF02/MenuWaterFrame_00.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuWaterFrame_01.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuWaterFrame_02.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuWaterFrame_03.png` | Three small expanding/fading cyan ripple clusters in the lower-right floor, roughly x=62–95%, y=66–89%. True alpha. | 0→1→2→3→0, **0.20 s/frame** (5 FPS). | Final art, Unity import pending |
| Lights | `Assets/NativeGame/GameUI/Art/GF02/MenuLightsFrame_00.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuLightsFrame_01.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuLightsFrame_02.png`<br>`Assets/NativeGame/GameUI/Art/GF02/MenuLightsFrame_03.png` | Tiny fixed-position mint and amber point lights around terminal/city, roughly x=53–93%, y=21–73%; only brightness changes. True alpha. | 0→1→2→3→0, **0.30 s/frame** (3.33 FPS). | Final art, Unity import pending |

The built-in image-gen tool made a distinct transparent four-frame sprite sheet for each group. Each quadrant was extracted and nearest-neighbor enlarged to the fixed 1672×941 canvas; low-alpha stray pixels (alpha ≤64) were cleared. No procedural content, noise, architectural redraw or new marks were added in this packaging step. These sheets were newly generated from the GF02 static background as a positional reference; they are not crops from the approved four-cell design concept.

## Integration notes

- Render order: static background → local rain / water / light frames → static title and team logo → native menu buttons and live status text. UI can vary only `Sprite[]` frame indices; keep each overlay `RectTransform` fixed and identical to the background image rect.
- Keep menu labels and live online/offline state in native UI. The approved concept panels are references only.
- Use Point filtering and no mipmaps for the pixel assets. INT owns Unity import, `.meta` generation and serialized resource binding. No Unity Editor was opened by ART.
- The background was produced as a single image and is suitable as the static fallback while animation integration is underway. UI/HUD/INT should retain their own font and native text scaling.
- Formal verification follows `Docs/GameFlow/GF02/ACCEPTANCE.md`; art has not been checked in Unity, at 16:10, or at low resolution.
