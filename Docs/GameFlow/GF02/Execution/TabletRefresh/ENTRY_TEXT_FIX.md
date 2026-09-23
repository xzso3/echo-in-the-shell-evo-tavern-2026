# Entry 标题消失：调查与局部修复

2026-09-24。工作基线 893d5d2；先由制作人调查用户截图和 TMP 源码，程序与 UIUX 角色独立核对字体/源码后修改。程序仅修改 `CommanderTabletView.cs` 的共用 Entry 行及一个有限高度 helper；不改字体资产、字号、全局文本 overflow 或玩法绑定。

## 证据

- 原 Entry Title：字号20、单行高度26、`Ellipsis`；任务列表和记录目录共享此布局。
- `Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset:73` 的 faceInfo：pointSize12、scale1、ascent13、descent-3、lineHeight16。
- 单行字面垂直范围 `(13 - (-3)) × 20 / 12 = 26.667`，大于原26高槽。故文本存在、颜色正确也可能整行不可见。
- 只读核对集成工作树 TMP 3.0.6 `TMPro_UGUI_Private.cs:2456`：`textHeight > marginHeight + 0.0001f` 进入垂直溢出。2523起 `Ellipsis` 候选栈为空时，将首字符替换为 `0x03` 并将字符数归零。此路径与“标题消失、部分其他文字仍显示”的截图相符。未宣称实际运行断点证明。
- 原Preview字号18两行理论需48，原48槽无安全余量；目录Chevron字号24理论32，而原40行减上下5仅剩30，也属同源风险。

## 修法与静态几何

`EntryTextHeight` 仅服务共用 Entry 的 Title/Status/Preview：取 TMP `GetPreferredValues` 高度与字体 faceInfo 度量的较大值，向上取整后加6设计单位安全余量。单行按实际文字测量，两行摘要使用字体资产已有拉丁字形样本“A\nA”测量，避免为测量额外请求中文字形；以完整faceInfo ascent/descent/lineHeight高度兜底覆盖中文；不随内容长度无限增高摘要。宽度省略、单行标题、两行摘要和详情全文仍保留。

按当前主字体度量、preferredHeight不超过该度量时：

| 槽 | 原高度 | 新高度下限 |
| --- | --- | --- |
| Title 20 | 26 | ceil(26.667)+6 = 33 |
| Status 16 | 26 | ceil(21.333)+6 = 28 |
| Preview 18 双行 | 48 | ceil(48)+6 = 54 |

目录保留至少40高；当前预计为上下4+Title33=41。邮件顶部6，header=max(Title,Status)=33，间隔4，摘要54，底部8，预计总高105。若TMP实际preferredHeight更高，标题槽与父行相应扩大。

布局依赖顺序为：测量标题/状态 → 取共用header高度 → 下移摘要 → 测量摘要 → 设置父LayoutElement高度 → Accent/Chevron跟随。目录Chevron保持原24号，纵向槽为最终行高减8，由原30增至当前33，覆盖其约32的字面高度。Title20/Status16/Preview18/箭头24均保持不变，仅修正高度。增加的内容高度由原ScrollRect承载，不缩字或拉伸外壳。

## 验证边界

已只读核对字体资产和集成TMP源码，检查布局依赖及 `git diff --check`（无输出）。未启动Unity、编译、自动测试、mock或截图巡检；未验证fallback字体混排、实际中文标题、目录长标题省略与真实滚动。由INT统一导入编译并由用户实际界面确认。UIUX_SPEC可能由并行美术角色更新；本程序角色没有编辑该文件。
