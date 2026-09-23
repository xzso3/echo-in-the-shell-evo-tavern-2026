# GF01-04｜全流程 UI/UX 与 ImageGen 美术交付

工作基线 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；设计依据为主目录只读的 `Docs/GameFlow/DEVELOPMENT.md`、`TASKS.md`、`Docs/AICommander/FINAL_HANDOFF.md`、已认可 `Docs/AICommander/References/tactical-tablet-concept.png` 与仓库现有 `Docs/AICommander/UI02_ASSETS.md`。主目录的未提交文件未改动。完整坐标、状态和组件层级见 `Assets/NativeGame/GameUI/Design/GF01_LAYOUT.md`。

## 草图与生产资源

工具均为 Codex 内置 ImageGen。草图是布局决策参考，**不可整张贴入 Canvas**。最终 Sprite 从内置输出无损复制到本项目，没有把文案或点击区烘焙进图像。

| 文件 | 像素 / 导入上限 | 用途与接线 |
| --- | --- | --- |
| `GameUI/Design/FlowConceptBoard_A.png` | 1672×941 / 2048 | 四格草图：主菜单、游玩 HUD、暂停/确认、结算。只做设计参考。 |
| `GameUI/Design/FlowConceptBoard_B.png` | 1672×941 / 2048 | 四格草图：开局、共享设置、平板/合同、结局。平板格的示意屏幕内容**不替代**现有聊天/支援/记录，也不替代既有正式外壳。只做设计参考。 |
| `GameUI/Art/MainMenuFacility.png` | 1672×941 / 2048 | 主菜单专用设施远景，原生 `Image` 底层，按 16:9 等比填充；左侧暗区留给 TMP/按钮。RGB 不需要透明。不要延伸为所有流程屏的固定背景。 |
| `GameUI/Art/LightPanelCorner.png` | 1254×1254 / 512 | 单个透明 L 角装饰，显示约 96×96；原生色块面板上以四个 `Image` 翻转/旋转拼角，`raycastTarget=false`。复用开局、暂停、设置、结局、结算的轻面板；边线可由原生 Image 补齐。不要当可拉伸整框或对其九宫切片。原图有透明留白，以可见部分定位。 |
| `GameUI/Art/TerminalButtonBlank.png` | 2172×724 / 1024 | 无字按钮装饰；建议装饰 `Image` 约 360×120，实际可见按钮约 360×64，置于独立原生 `Button` 点击区后/内，TMP 标签在上。按钮点击区至少 360×56 或随布局适配，高不小于 44。装饰图不可接收射线。主菜单、开局和结果共用；危险/取消可由原生色块或 Image tint 区分。 |
| `GameUI/Art/KeycapBlank.png` | 1254×1254 / 256 | 无字方键帽，建议 `Image` 约 52×52、可见键帽约 38×38；WASD/Space/E/Tab 由 TMP 叠写。开局说明与临时互动提示共用。非点击装饰，`raycastTarget=false`。 |

四张生产图使用 Unity 2021 `Sprite (2D and UI)`、Single、Point、Clamp、无 Mipmap、透明 Alpha（背景为 RGB）、无纹理压缩；已附固定 GUID `.meta`。`maxTextureSize` 是导入上限，不改变仓库原 PNG 的像素尺寸。草图也附 `.meta` 以保持 GUID，但 UI 不引用草图。

已有 `Assets/NativeGame/PhoneUI/Art/` 七张图保持原样：`TacticalTabletFrame`、`PhysicalReturnKey`、`CommanderSignalAvatar`、`TabCommunications`、`TabSupport`、`TabRecords`、`AuthorizationMedical`。平板与合同按 `UI02_ASSETS.md` 的 1088×612 外壳、786×388 屏幕安全区和 3 页签配置装配；不重新画外壳/图标，不把合同做成静态位图。全流程动态中文、按钮、数值、生命、目标、结局数据及合同真值都由 uGUI/TMP 提供。

## 给 GF01-05/B 与 GF01-INT 的装配入口

1. GF01-05 可立即按照 `GF01_LAYOUT.md` 建立原生主菜单、共享设置、开局、暂停、确认和按钮状态；图像为独立装饰 `Image`，不阻塞任何原生搭建或逻辑接入。最终场景/Prefab 序列化引用 `GameUI/Art` 的 Sprite，运行时不要用 `Resources.Load` 猜路径。
2. GF01-06/B 可立即搭 HUD、互动、结局和结算；生命填充、同步度、差异度、结局名、用时、击杀和最多三条事件使用实际绑定。`KeycapBlank` 可服务 E 互动，`LightPanelCorner` 可服务结果页与普通结局文本区。新生平板接续仍用 `PhoneUI/Art` 既有图。禁止叠加旧结果 UI。
3. INT 独占共享场景、`NativeHud`/`NativePhone`、HomeBootstrap 与字体引用；集成后确认这四张生产 Sprite **实际有引用**，并保持旧七张平板图引用。资源 PNG 与草图不得作为全屏不可点击的上层蒙版。
4. 任意 `Image` 装饰设 `raycastTarget=false`，实际 `Button`、TMP_InputField 和遮罩拦截保持原生组件。按钮 Hover/禁用可用原生颜色状态，不额外生成变体。顶部输入优先级按布局规范和 GF01-02 路由处理。

## 生成记录（原始 Prompt）

以下每项是独立 ImageGen 调用。风格来源为已认可平板概念和本任务全流程布局；未将其当作编辑目标。输出源文件保留在 `$CODEX_HOME/generated_images/01a0cd9f-3e76-7cb2-bdb4-8f13efa2e144/`，本项目中的 PNG 是用于装配的正式副本。

### FlowConceptBoard_A.png

`Use case: game-ui-concept. Asset type: visual design sketch board for a 1280x720 Unity pixel cyberpunk game, FOUR distinct screen concepts arranged as an orderly 2x2 contact sheet with generous gutters. The four screens: (top left) main menu with moody abandoned underground maintenance facility background and a thin left-side terminal rail for large native title and three native buttons; (top right) game HUD over a dim top-down facility combat scene, only four restrained corner modules and a small contextual E prompt, middle of gameplay unobstructed; (bottom left) pause menu over darkened gameplay with a light narrow panel at the right, and a small central confirmation dialog shown as an inset alternative; (bottom right) results screen with a quiet central thin-framed data panel, a title area, four metric cells, three event rows, two bottom buttons. Art direction: cohesive exquisite late-1990s tactical pixel art, graphite, desaturated jade and mint, tiny amber only as incidental light; crisp pixel clusters, restrained industrial scuffs, 16:9 screen within each quadrant. Existing tactical tablet reference direction: rugged dark graphite chassis, deep green phosphor screen, quiet mint borders, but use its visual language only; DO NOT put a thick tablet shell around these screens. Show layout hierarchy using empty rectangles and thin guides. No legible text, letters, words, logos, numbers, watermarks, real UI labels, or final game art sheet slicing marks. This is a layout/concept study rather than a production sprite.`

### FlowConceptBoard_B.png

`Use case: game-ui-concept. Asset type: second pixel-art UI layout study contact sheet for a Unity 1280x720 game, FOUR 16:9 screens in a clean 2x2 grid with gutters. Top left: opening instruction overlay on a dark top-down industrial map, centered thin panel with four keyboard instruction cells, single prominent action button at lower right. Top right: shared AI settings panel, centered/right thin panel with four native empty input rows, a small test-feedback region and three bottom actions, darkened facility behind it. Bottom left: tactical tablet with a rugged graphite chassis framing an entirely native dark-green screen, three bottom tabs and one contract overlay inside the screen, two distinct decision buttons; this is a visual reminder to reuse the existing approved tablet artwork, not a proposal to replace its shell. Bottom right: restrained ordinary ending reveal over nearly black facility background, a large quiet native text region and one result action; a small inset hints at a black-screen tablet continuation. Cohesive late-1990s pixel cyberpunk, graphite, dark teal, mint phosphor, small amber contract accents. Crisp deliberate pixel clusters, readable hierarchy and ample empty space. No legible words, letters, numbers, logos, watermarks, fake language, character portraits, or large thick tablet shell on the other three screens. Concept board only; native UI text and hit targets will be added in Unity.`

### MainMenuFacility.png

`Use case: game-asset. Asset type: final production background sprite for the MAIN MENU of a 16:9 Unity 2021 pixel-art cyberpunk game. Draw only an empty abandoned underground maintenance corridor, seen in cinematic wide perspective: wet graphite floor tiles, broad circular tunnel opening centered slightly right, cable trays, stained concrete, sparse industrial crates and grates, subtle jade-green emergency light and a few distant warm amber utility lamps. Most visual detail should occupy the center and right side. The entire left 42% is darker, calmer negative space so native TMP title, configuration status, and three native Unity buttons will be placed there. Palette harmonizes with an already approved rugged tactical tablet: charcoal #111B1F, slate steel, desaturated jade, dim mint highlights, amber only on tiny lamps. Pixel art with deliberate square pixel clusters, crisp hard edges, restrained contrast, no smeared blur, no photorealistic finish, no decorative UI frame. Strictly environment only: NO text, letters, numbers, symbols, logos, HUD, menus, buttons, icons, characters, silhouettes, watermarks, or source-code artifacts. A seamless self-contained full-bleed backdrop, composition suitable for filling 1280x720 without cropping important details.`

### LightPanelCorner.png

`Use case: game-asset. Asset type: FINAL reusable Unity uGUI transparent decorative corner sprite for a 1280x720 pixel cyberpunk game's LIGHT interface panels (opening instructions, pause menu, AI settings, results, ending text). Draw exactly ONE top-left 90-degree L-shaped terminal corner bracket, about one quarter of a panel border: a short horizontal metal strip to the right and short vertical strip downward, stepped 8-bit square-pixel cuts, narrow graphite body, very thin muted jade inner line, one tiny square rivet, subtle wear. The bracket sits snug in the top-left of a square canvas and occupies about 60 percent of the canvas width and height. The rest of the canvas, including the entire inner open region and all exterior around the bracket, must be genuine fully transparent alpha. Crisp pixel clusters, hard nearest-neighbor steps, no antialiasing or glow. Designed so Unity can mirror/rotate one sprite to make four corners over a native Image-colored panel of any size. Do not draw a whole frame, tablet, window, content, background, text, letters, numbers, symbol, logo, watermark, or shadow. Functional decor only, 1:1 square canvas.`

### TerminalButtonBlank.png

`Use case: game-asset. Asset type: FINAL standalone reusable Unity uGUI pixel-art button background sprite, blank face for native TextMeshPro overlay. ONE single wide horizontal rectangular button, aspect ratio about 6:1, nearly fills the image canvas. Dark graphite and desaturated jade metal, light thin mint border with squared stepped corners, shallow inset dark-green center with enough uninterrupted open space for short Chinese native UI labels. Slight industrial pixel wear at the two ends only. Genuine transparent alpha around the outer button silhouette. Hard crisp discrete square pixel blocks, no gradients, glow, blur or antialiasing. This is a restrained lightweight terminal button, much thinner and flatter than the approved tactical tablet physical return key. No letters, words, numbers, icons, arrows, checkmarks, branding, logos, watermark, UI panel, environment or scene. One button only, not a sprite sheet. It will be displayed at roughly 360x56 and optionally tinted by Unity Image.`

### KeycapBlank.png

`Use case: game-asset. Asset type: FINAL transparent Unity uGUI pixel-art keyboard keycap sprite for opening controls and small HUD interaction prompts. One single nearly square front-facing low-profile keycap, centered, occupying 75 percent of a square canvas. Dark slate graphite rim, a very thin muted mint edge, dark empty recessed center. Genuine transparent alpha everywhere outside its stepped silhouette. No characters on the key face: native TMP will draw WASD, Space, E or Tab on top as appropriate. Crisp hard nearest-neighbor square pixel steps and sparse tactile wear, visible at a 42x42 Unity display size. No letters, numbers, icons, arrows, logo, watermark, hands, keyboard, backdrop, other keys, gradient, glow, soft shadow, or antialiasing.`
