# UI-02｜战术平板最终资源与接线规格

基线：`d1bbea1f0aacb032d4d98cd422703df1526fc6e2`。设计继承主目录只读的 `Docs/AICommander/References/tactical-tablet-layout.svg`、`tactical-tablet-concept.png` 和 `tactical-tablet-prompt.txt`。原 UI-01 已撤销。本包只有装饰性像素资源；聊天、合同、状态、数字、中文、输入和点击区都必须由 Unity uGUI/TMP 绘制。**不要把概念图整张贴到 Canvas。**

## 资源清单与导入

全部资源位于 `Assets/NativeGame/PhoneUI/Art/`，并已附 Unity 2021 Sprite `.meta`：Sprite (2D and UI)、Single、Point、Clamp、无 Mipmap、透明 Alpha、无纹理压缩。PNG 均保留真正透明像素；这是内置 ImageGen 原始成品的无损复制，没有烘焙文案。`maxTextureSize` 是导入上限，不改变仓库中原始 PNG 尺寸。

| 文件 | 原始像素 | 导入上限 | uGUI 用途与 1280×720 参考尺寸 |
| --- | ---: | ---: | --- |
| `TacticalTabletFrame.png` | 1672×941 | 2048 | `Image` Simple，1088×612，置于触控屏上层；透明屏幕开口允许下层原生 UI 显示；`raycastTarget=false`。固定 16:9 比例，不整体九宫切片。 |
| `PhysicalReturnKey.png` | 2172×724 | 512 | 右下实体键可视层约 118×40；独立 `Button` 点击区至少 128×48，TMP 叠“返回”。不要把图本身当点击判定。 |
| `CommanderSignalAvatar.png` | 1254×1254 | 256 | 头像 `Image` 约 100×100（可见方牌约 50×50），旁边 TMP“指挥官”；不要用它承载状态字。 |
| `TabCommunications.png` | 1254×1254 | 256 | “通讯”页签符号；`Image` 约 34×34，TMP 单独绘制文字。 |
| `TabSupport.png` | 1254×1254 | 256 | “支援”页签符号；同上。 |
| `TabRecords.png` | 1254×1254 | 256 | “记录”页签符号；同上。 |
| `AuthorizationMedical.png` | 1254×1254 | 256 | 医疗提案/合同的琥珀授权标记；`Image` 约 32×32。弱点解析卡只用琥珀细边与原生类型文字，避免把医疗十字误当成弱点图标。 |

这些图的原始画布带透明留白。上述 uGUI 尺寸已把可见部分计入；不要误按 PNG 像素尺寸自动布局。若 UI-03 在 Editor 装配预制件，用 `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/NativeGame/PhoneUI/Art/<文件名>")` 序列化 `Image.sprite`，构建运行时不依赖 `Resources.Load`。所有装饰图的 `raycastTarget=false`，让按钮/输入框接收事件。返回键图片放在 `Button` 背景上，按钮本体必须保持可点击。

外壳是完整一张 16:9 帧，像素角件由图内固定装配；不能将它任意拉成其他比例，也不要对整张作九宫切片。屏幕背景与聊天面板用原生 Image 色块。若屏幕比例不同，等比缩放并保持可见游戏边缘。实体键独立于外壳，以免重复扭曲转角。

## 1280×720 接线尺寸

CanvasScaler：`Scale With Screen Size`，Reference Resolution `1280×720`，Match `0.5`。平板 RectTransform 居中，`1088×612`（屏宽 85%）。以平板左上角为 `(0,0)`：

- 屏幕原生内容安全矩形 `(151,104)–(937,492)`，即 `786×388`。这一矩形位于外壳透明开口内部，内侧再留 `12–16 px` 垫边；屏幕绿色基底建议 `#0D291F`，暗边 `#385F4C`。外壳 `Image` 排在内容之后绘制，上层仅覆盖边框；不要让其阻挡输入。
- 顶状态栏约 `30 px`：左侧安全节点与**AI 连接**状态，右侧真实生命。在线字样只用于 AI 通讯。顶部硬件铭牌 `SHELL-LINK` 如果需要，用 TMP 放在外壳，不写入贴图。
- 状态栏下身份行约 `58 px`；指挥官头像可见约 `50 px`。聊天区域用独立 `ScrollRect`，留底部固定输入 `44 px`、三页签 `42 px`，避免长回复顶走输入和标签。聊天消息可换行，消息泡背景、边框和来源标识都用原生组件。
- 右下实体返回键可视位置建议在平板内 `(820,527)` 起，`118×40`，上层 TMP“返回”。它关闭平板；合同覆盖时先关合同（等同取消本提案），Esc 遵循相同优先级。
- 页签三等分、整块可点，最小点击高 `42 px`；图标与 TMP 标签分开。输入和“发送”/“取消”按钮的有效点击高至少 `44 px`。无需为 Hover 单独生成图，原生颜色/边框切换即可。

色彩从已认可草图收束：石墨外壳与深绿屏，文字主色 `#D5E8D8`、次级 `#88A498`、可操作薄荷 `#9CDEB3`、边线 `#426650`；琥珀 `#DFB16D` **只**标识待授权提案与合同操作；失败提示用克制的 `#CF8A78`。这些是原生 UI 颜色，不要求修改图像材质。字体复用工程现有中文 TMP 字体及动态回复所需 fallback，资源中没有文字。

## 状态与合同视图

| 状态 | 原生 UI 规则 |
| --- | --- |
| 可输入 | 安全节点且配置有效时，输入框与发送可用；无文字时发送禁用。非安全节点显示原因并禁用输入，不显示“战斗已暂停”。 |
| 等待回复 | 顶栏显示“连接中/等待回复”；发送改为或并列“取消”，禁止再次发送，草稿保留；聊天滚动和支援/记录切页照常可用。完整响应校验后一次显示，不做逐字动画。 |
| 离线/配置缺失/连接错误 | 顶栏明确“离线”或可理解的错误类别；聊天内独立本地错误条，保留草稿与重试入口。不得露出 API Key、完整带密钥 URL 或模型原始异常栈。原记录和手动支援仍可看。 |
| 提案可查看 | 聊天中的琥珀细边卡，左图标与真实“医疗/弱点解析”标题，副行“尚未执行 · 需要授权”；右侧独立“查看合同”按钮。不会仅因模型说话就执行。 |
| 合同覆盖 | 在当前屏幕内覆盖聊天，保持原滚动位置。标题、Unity 真值的支援效果、涉及记忆、授权范围、同步度变化逐项列出，底部“接受并执行”与“拒绝”，右上关闭。覆盖层拦截背后点击；关闭等同取消提案。两处校验失效时明确显示原因并禁用接受。 |
| 已执行/拒绝/取消/失效 | 卡片保持可读，状态字与按钮禁用；不能再次执行。实际回血/核心窗口/同步变化或失败原因显示为**本地事实**消息，不伪装成 AI 回答。 |
| 结束/重开 | 结束手机保留必要剧情入口；新一局清空旧聊天与合同。图像只有装饰作用，不承载这些状态。 |

错误短文案由 UI-03 根据真实状态选择，示例：缺配置“尚未配置 AI 连接，可离线游玩”；超时“连接超时，输入已保留”；网络错误“暂时无法连接服务”；HTTP 错误“服务拒绝了请求，请检查配置”；回复格式无效“回复格式无法识别，请重试”；取消“已取消，未执行操作”。详细诊断留在不含凭据的内部错误类别，界面不打印原始响应体。它们是状态文字，不属于贴图。

## 绑定交接

UI-03 在自己的 PhoneUI 装配代码或 Prefab 中至少绑定 `TacticalTabletFrame`、`PhysicalReturnKey`、`CommanderSignalAvatar`、三个 Tab 图标与 `AuthorizationMedical`。INT-AI 合并本包后确认它们在最终 Unity UI 实际引用，不能只把资源留在项目。共享场景、NativePhone/Hud、字体引用与最终人工检查由 INT-AI 独占。UI-02 不占 Unity Editor，不改这些文件。

## 生成来源与最终 Prompt

工具：Codex 内置 ImageGen。外壳、返回键、头像以主目录认可 `tactical-tablet-concept.png` 作**风格参考**；其余图标由以下独立 Prompt 生成。输出原始 PNG 均经目视检查，随后无像素改写复制到项目目录。

1. `TacticalTabletFrame.png` — `Use case: stylized-concept. Asset type: production Unity uGUI pixel-art tablet FRAME SPRITE, a single transparent PNG. Use the attached approved tactical tablet concept only as visual style reference. Create one orthographic, perfectly front-facing rugged horizontal handheld tactical computer chassis, dark graphite with blue-green hints, stepped chamfered pixel silhouette, rubber side grips, restrained screws, sparse speaker vents and a tiny jade connection LED. Full chassis centered and occupying nearly the full canvas width. Crucial: the entire interior display opening must be completely transparent alpha from edge to edge, a clean large rectangular hole occupying about 76% of chassis width and 62% of chassis height so native Unity UI will show through. Entire exterior beyond chassis also fully transparent alpha. No background, scene, display glass, UI, text, numbers, icons, labels, buttons, markings, shadow, glow or watermark. Pixel art with crisp discrete pixel clusters, nearest-neighbor edges, no antialiasing, no photorealism, no glossy gradient. Shape should be symmetric enough to frame a dynamic 16:9 Unity UI, with corners intended to remain at fixed size; front surface and inner bevel are distinct. Treat the image as a ready-to-use transparent frame sprite, not a whole concept illustration.`
2. `PhysicalReturnKey.png` — `Use case: stylized-concept. Asset type: production Unity uGUI pixel-art PHYSICAL RETURN KEY background sprite. Refer to the approved tablet concept for the exact rugged graphite-and-jade equipment style. Draw one front-facing wide low-profile rectangular hardware key, 3:1 proportions, stepped pixel bevel, dark slate shell, recessed nearly black key surface, faint desaturated jade edge highlight, a tiny physical scuff or two. The key itself should almost fill the canvas and be centered. Fully transparent alpha outside the key. Leave the key face completely blank: no letters, arrow, labels, glyphs, numbers, icon, watermark. Native TMP will render the localized return label on top. Crisp square pixel clusters and nearest-neighbor edges; no antialiasing, no photographic 3D or glossy gradients, no tablet body and no scene. Standalone reusable button sprite.`
3. `CommanderSignalAvatar.png` — `Use case: stylized-concept. Asset type: production Unity uGUI commander signal avatar sprite. Use the attached approved pixel tactical tablet only as a palette and style reference. Draw ONE centered square icon: an anonymous abstract radio-signal face glyph, late-1990s tactical terminal pixel-art, mint phosphor pixels on a dark desaturated jade inset plaque. Simple symmetrical mask-like head formed by chunky pixel blocks, two small dark eye apertures, one short horizontal signal line below. Strong readable silhouette at 64x64 display size; intentionally sparse detail and crisp discrete square pixel clusters. Canvas fully transparent outside the single square plaque, with sharp square edges. No words, no numbers, no surrounding UI, no scene, no extra objects, no watermark, no smoothing or gradients. This is a standalone avatar sprite, not an interface mockup.`
4. `TabCommunications.png` — `Use case: stylized-concept. Asset type: production Unity uGUI COMMUNICATION TAB icon sprite. One single abstract radio speech symbol for a pixel-art tactical handheld: a compact square speech bubble containing exactly three ascending signal bars, no other motifs. Desaturated pale jade/green phosphor pixels, bold very simple silhouette readable at 24x24. Center the icon and let it occupy 85% of the square canvas. Genuine fully transparent alpha outside and inside open spaces. Hard stepped square-pixel edges, tiny coherent clusters, no glow, gradients, texture, speckles, shadows or antialiasing. No letters, words, numbers, border, button background, tablet, scene or watermark. Standalone monochrome tintable game UI icon.`
5. `TabSupport.png` — `Use case: stylized-concept. Asset type: production Unity uGUI SUPPORT TAB icon sprite. Draw exactly ONE single compact medical/support cross icon for a tactical handheld game's navigation, in monochrome muted jade green. The cross has a square central block and four short equal square-ended arms, with four tiny detached corner notches to suggest a rugged hardware stencil. Very simple bold silhouette legible at 24x24 display size. Center it to occupy around 80% of a square canvas. Genuine transparent alpha outside the icon and in cutouts. Exact hard square-pixel staircase edges, no gradients, antialiasing, glow, texture, noise or shadow. No words, numbers, button background, border, tablet body, scene or watermark. One tintable standalone pixel sprite.`
6. `TabRecords.png` — `Use case: stylized-concept. Asset type: production Unity uGUI RECORD TAB icon sprite. Draw exactly ONE simple pixel-art archive/logbook glyph: one upright rectangular field notebook with a stepped upper-right page corner and three short horizontal lines, in a single desaturated jade-green phosphor color. Compact bold clean silhouette legible at 24x24 display size. Icon centered, occupies about 80% of square canvas. Genuine fully transparent alpha outside and inside its open spaces. Hard square pixel steps, no antialiasing, gradients, glow, noise, texture or shadow. No words, numbers, button background, frame, tablet, scene, watermark or other objects. Standalone monochrome tintable game UI icon.`
7. `AuthorizationMedical.png` — `Use case: stylized-concept. Asset type: production Unity uGUI amber authorization icon sprite for a tactical pixel-art game. Draw ONE centered medical support authorization emblem: a solid compact chunky pixel cross nested inside a subtle incomplete square contract stamp. Pure warm muted amber pixels (#d9ae68 to #f0c782) with dark cutouts, no background plaque. Designed to be readable at 32x32 display size; only a few large discrete blocks, symmetric, exact crisp square edges. Transparent alpha everywhere outside the emblem, which should occupy about 80% of the square canvas. No text, numbers, letters, watermark, UI card, scene, glow, smoothing, dither or gradients. Standalone game icon suitable for tinting and state changes by Unity Image color.`
