# 当前测试版：FusionPixel 与简体中文

本轮基线为 native-integration `888f1f8`，保护原 U4 分支，在 `codex/unity-native-zh-fonts` 集成。字体安全点 `f3bef9d`；译文原提交 `8a2cb96`，本地为 `a8f5910`。仅修改 NativeDemo、NativeGame 当前预制体/文案/字体及指定许可目录；历史原型、玩法状态机/计分/战斗规则、构建设置未扩展。

## 字体与许可

- 只读来源：`/Users/const/Projects/Unity/Project Road Trip/Assets/Road Trip/Fonts/FusionPixel/`。
- TMP 字体：`Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset`，GUID `945fdb8889baf46f39b4c9a818d57d71`。
- 配套源字体：`fusion-pixel-12px-proportional-zh_hans.otf`，GUID `18e941651f58b4c17815aaae33e4cf58`，SHA-256 `82e05156031524165202ccdae6c284aaeecd798271e32c65cbf0c6568ade8cfa`。源文件字节未修改，Include Font Data 保持开启。
- 保留原 README、LICENSE.txt 和 LICENSES。相同授权文本另存于 `Assets/StreamingAssets/ThirdPartyLicenses/FusionPixel/`，供用户构建时随包分发；不依赖未引用的编辑器文本资产进入 Player。
- 复用 12px Bitmap 主字体：动态、多图集允许、1024×1024、Point 过滤；当前仅一个图集。当前源码/序列化文案字符预热后保存，材质和图集为真实子资产；材质贴图、源 OTF 和 Shader 依赖均已校验。不是 Dynamic OS，不依赖 Windows 安装字体。
- 显式替换 NativeDemo 37 个文字组件和 Boss prefab 两个文字组件，同时遍历不激活的对象及 legacy UI.Text。没有运行时全局字体扫描，也没有更改历史项目的 TMP Settings。

## 中文及排版

游戏内标题、HUD、目标、世界标记、三段记忆、通讯主题、支援合同/失败/成功、枚举拼接、网络状态、行为记录、四结局、第一拳与手机接续均为简体中文。保留拉丁键位名 WASD、Space、Enter、Tab 和单键 E；内部 GameObject 名称、资源 ID、开发日志及 Editor 菜单仍可为英文。

UI 正文/按钮主要 24px，大标题 36px；世界 TMP 字号 4.8，Boss 世界 Canvas 字号 36。关闭自动字号，保留像素风。手机宽 600，双列按钮宽 278，正文按内容滚动；合同展开正文；较长解析合同的末行仍通过滚动阅读，文字未删除或截断。对白面板加高；HUD/Boss/白屏提示文本框按新字体行高扩展。没有通过缩小中文字到难读大小来掩盖溢出。

一次性编辑器入口：`NativeChineseTextMigration.Apply` 写入译文序列化字段；`NativeFontSetup.Apply` 替换字体/预热/排版；`FinalizeChineseLayout` 额外检查活动与不活动场景文字的英文遗留。当前已提交场景无需用户再运行这些命令。

## 必要检查

- `/private/tmp/native-zh-migrate.log`：中文迁移实际保存；完整 NativeGame 源码字符串字面量及当前序列化文案共 561 个独立字符，直接字体覆盖检查缺字 0，不搜索 fallback。
- `/private/tmp/native-zh-finalize.log`：依赖/材质/图集落盘与字体检查通过；场景可见和隐藏文字扫查剩余拉丁单词仅为键位名。
- `/private/tmp/native-zh-visual-final.log`：25 个展示状态、588 次文字组件检查，字体混用/缺字/文本框高度不足均为 0；截图 `/private/tmp/native-zh-*.png`，覆盖 HUD/开场、三记忆、通讯主题、网络页、医疗/解析合同与失败、最终选择、Boss 核心提示、四结局、白屏第一拳及手机接续。
- 这些是**排版展示检查**：使用显式文字状态、节点定位及人工布置的结局数据；不声称重新验证了战斗、授权或计分，更不作为玩家真实通关记录。未重跑 U4 功能回归，未构建 Windows。
- 移除临时展示脚本后的 `/private/tmp/native-zh-final-compile.log` 退出 0，无 C# 编译错误；字体 OTF SHA-256 及 StreamingAssets 四份许可文本与来源逐字节核对一致。
- 初轮发现的旧文本框高度不足已修正后复看。临时展示脚本只保存到 `/private/tmp/native-zh-visual-source.cs`，不保留在 Assets。

尚待用户集中试玩确认不同实际窗口/缩放下的阅读与滚动手感。原先已记录的静态角色与简单 Canvas 拳击表现未在字体/翻译任务中改动。
