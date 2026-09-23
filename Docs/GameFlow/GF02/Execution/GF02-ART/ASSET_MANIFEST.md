# GF02-ART asset manifest

Input: `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`, approved GF02 menu and HUD direction. All paths below are relative to the repository root. Assets are generated with the built-in image-gen tool. Source prompts are recorded in [PROMPTS.md](PROMPTS.md).

| Path | Use | Pixels | Alpha | State | Placement / binding advice |
| --- | --- | ---: | --- | --- | --- |
| `Assets/NativeGame/GameUI/Art/GF02/MenuBackgroundStatic.png` | Stable menu environment; no title, menu, status, or team mark baked in | 1672×941 | Opaque RGB | Final art, Unity import pending | Full-bleed 16:9 background. Preserve aspect; center crop only for other display ratios. Architecture, camera and scene remain fixed while local transparent overlays animate. |
| `Assets/NativeGame/GameUI/Art/GF02/EchoInTheShellLogo.png` | Exact English title, two lines | 1774×887 | RGBA, transparent surround | Final art, Unity import pending | Separate static overlay in upper-left dark field. Suggested visible width 32–37% of 16:9 screen; do not animate its transform or Sprite. Actual artwork nearly fills this PNG, so size against the on-screen result. |
| `Assets/NativeGame/GameUI/Art/GF02/CipherWorksLogo.png` | Team mark in lower-right | 2172×724 | RGBA, transparent surround | Final art, Unity import pending | Separate static overlay, suggested visible width 13–17% of 16:9 screen with 4–5% right/bottom margin. Artwork is inset within the PNG; size against visible mark. |

The final animation layers will use `MenuRainFrame_00...`, `MenuWaterFrame_00...`, and `MenuLightsFrame_00...` names. Their exact frame count, dimensions, order, opacity and duration will be appended after creation. They are not implied by the static background or the approved four-cell concept image.

## Integration notes

- Render order: static background → local rain / water / light frames → static title and team logo → native menu buttons and live status text.
- Keep menu labels and live online/offline state in native UI. The approved concept panels are references only.
- Use Point filtering and no mipmaps for the pixel assets. INT owns Unity import, `.meta` generation and serialized resource binding. No Unity Editor was opened by ART.
- The background was produced as a single image and is suitable as the static fallback while animation integration is underway. UI/HUD/INT should retain their own font and native text scaling.
- Formal verification follows `Docs/GameFlow/GF02/ACCEPTANCE.md`; art has not been checked in Unity, at 16:10, or at low resolution.
