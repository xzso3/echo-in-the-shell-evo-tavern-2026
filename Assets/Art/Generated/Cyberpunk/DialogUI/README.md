# 赛博对话 UI

生成方式：Codex 内置 `imagegen`。角色立绘与立绘占位区均未制作；界面设计保持原创，仅延续当前项目的冷灰义体都市视觉语言。

## 成品文件

- `dialogue_panel_1280x320.png`：主对话框，1280×320。
- `dialogue_option_1024x128.png`：单个可复用选项框，1024×128。
- `dialogue_panel_source.png`、`dialogue_option_source.png`：未经尺寸整理的生成母图。

图片不包含任何文字。角色名称、正文和选项内容应使用 TextMeshPro 动态显示。

## 建议 UI 结构

```text
DialogueRoot
├─ DialoguePanelImage
├─ CharacterNameText
├─ DialogueBodyText
└─ OptionsContainer
   ├─ OptionButton (dialogue_option_1024x128.png)
   └─ OptionButton (dialogue_option_1024x128.png)
```

## 推荐排版

### 主对话框

- 保持 4:1 左右的宽高比，建议使用原生尺寸或只进行等比例缩放。
- 角色名称区域：左边距约 70 px，右边距约 70 px，上边距约 28 px，高度约 70 px。
- 正文区域：左边距约 70 px，右边距约 70 px，从青色分隔线下方约 24 px 开始，底边距约 35 px。
- 文本建议左对齐，名称使用琥珀白或浅灰，正文使用偏冷的灰白色。

### 选项框

- 在 `OptionsContainer` 中实例化同一个选项框两次即可。
- 选项文字左右边距建议至少 96 px。
- 两个选项之间建议保留 12–20 px 间距。
- 普通状态保持原色；Hover/Selected 可提升青色亮度；Pressed 可略微降低整体亮度。
- 若作为九宫格 Sprite 使用，建议先尝试 Border：Left 72、Right 72、Top 28、Bottom 28。

## Unity 导入建议

- Texture Type：Sprite (2D and UI)
- Sprite Mode：Single
- Filter Mode：Point (no filter)
- Compression：None
- Generate Mip Maps：关闭
- Alpha Is Transparency：开启
- Mesh Type：Full Rect

主对话框内部包含固定的名称分隔线，不建议随意拉伸高度。选项框的中段较均匀，更适合使用九宫格横向扩展。
