# GF01｜完整游戏流程与像素 UI/UX 交付

状态：**代码与资源已在独立集成分支提交；待最终人工校验**。分支 `codex/gf01-integration`，隔离工作树 `/Users/const/.codex/worktrees/3f6d/echo-in-the-shell-evo-tavern-2026`。输入基线 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；GF01-INT 共享接线提交 `8688c9b`，随后隐藏平板刷新阻断修复 `27b8ed8dd4b4097925a32fbe9cc3599d1d298a62`。本批次台账在 [STATUS.md](STATUS.md)，不代替旧 AI／Toolkit 台账，也未恢复 UI-01。

## 启动与已交付行为

在集成工作树打开 `Assets/Scenes/CommanderHome.unity` 并 Play；已提交 Build Settings 的首场景也是 `CommanderHome`，游戏场景为 `NativeDemo`。本机若已有可加载的用户未提交 `NativeDemoTilemap`，开始游戏优先进入它；干净检出回退 `NativeDemo`。GF01 通过现有 Home、Run、Hud、Phone 运行时接线，新开局说明、暂停和结局／结算都在游戏场景内；没有新结果场景，也没有改写用户未提交 Tilemap、字体、地图或 Build Settings。

- 主菜单只有开始游戏、AI 设置、退出游戏三个主要入口；未配置 AI 仍可离线开局，配置状态不冒充连接成功。设置在首页与暂停页使用同一个 `GameAiSettingsView`；Key 只在进程内保留，重试／回菜单不要求重填。测试连接针对已应用配置，不写本局聊天。
- 每局及重试均先显示 WASD、Space、E、Tab 操作说明；“开始行动”才一次性进入 Playing、初始化战斗并发 `Run.Started`，此前不推进本局计时与世界输入。统一导航负责开始、重试、再来一局和回菜单，离局取消请求／待确认合同／未执行提案，清本局会话与暂停，保留程序级设置。
- 探索／Boss 阶段 Esc 打开继续、AI 设置、返回主菜单菜单；返回前确认本局不保存。合同、手机、设置、确认框按顶层逐次消费 Esc；设置和确认保持暂停，暂停中的合法支援保留。实际应用配置变化才取消在途请求／合同并标失效未执行提案，聊天和已执行效果保留。
- 归档／逃逸／同化短文本后需玩家点“查看本局结果”；新生沿用第一拳、自主终拳、黑屏和平板接续，并将返回入口导向本局结果。死亡立即终止本局，关闭请求／合同／手机，直接显示“意识涣散”失败结算。结算只读快照显示判定时的真实用时、整局击杀、同步、差异和最多三条记录过的行为；无总分、星级、排名或奖励。结果页提供再来一局／重试及返回主菜单。
- 全流程使用原生 uGUI/TMP 文字、数据和点击区。已认可平板外壳及七张 Sprite 原样复用；主菜单、开局、暂停、HUD、结局、结算另用统一的轻量像素赛博朋克组件，没有“战斗已暂停”提示。

## 提交与美术

| 工作包 | 源提交 | 集成提交／位置 |
| --- | --- | --- |
| GF01-00 契约 | `6acb4cd` | `1e57e83`，`Docs/GameFlow/GF01_00_CONTRACT.md` |
| GF01-01 导航／开局 | `3a0815f` | `64f67a4`，`Assets/NativeGame/GameFlow/Navigation/`、`RunLifecycle/` |
| GF01-02 暂停／设置 | `9a7f29c` | `e75cdc8`，`GameFlow/Pause/`、`Settings/` 与最小提案失效接口 |
| GF01-03 终局／快照／击杀 | `3b8025c`、`b92ba6b` | `c53cd73`、`b78870b`，`GameFlow/Ending/`、`Results/` |
| GF01-04 设计／ImageGen | 最终源 `315e293` | 等价资源提交 `6308463`，`GameUI/Art/`、`Design/` |
| GF01-05 菜单／开局／暂停 UI | `b144ba7` | `26ca7d1`，`GameUI/Menu/`、`Intro/`、`Pause/` |
| GF01-06 HUD／结局／结算 UI | `f8e8a62` | `51cf596`，`GameUI/Hud/`、`Ending/`、`Results/` |
| GF01-INT 共享接线与修复 | — | `8688c9b`、`27b8ed8`，现有 Home/Run/Hud/Phone 与资源引用 |

内置 ImageGen 实际生成了两张全流程四格草图 `Assets/NativeGame/GameUI/Design/FlowConceptBoard_A.png`、`FlowConceptBoard_B.png`，以及四张正式 Sprite `Assets/NativeGame/GameUI/Art/MainMenuFacility.png`、`LightPanelCorner.png`、`TerminalButtonBlank.png`、`KeycapBlank.png`。生成 prompt、尺寸、用途及导入设置在 [GF01_04_ASSETS.md](GF01_04_ASSETS.md)。四张正式图由 `GameUI/Resources/GF01Art.asset` 引用并接入原生界面代码，实际画面待人工查看；草图只作设计参考。既有平板七图仍在 `Assets/NativeGame/PhoneUI/Art/`。

## 最后一次人工检查

由用户或指定操作者在集成工作树操作；尚未操作的项目均为**待人工校验**。

1. 从 `CommanderHome` 试开始、AI 设置、退出；未配 AI 仍能进游戏。每局和重试先见操作说明，点“开始行动”前战斗与计时不动。
2. 游玩中 Esc 暂停、继续；返回菜单先确认，取消仍暂停；设置关闭回暂停。合同和手机用 Esc 逐层关闭，同一按键不弹出暂停菜单。
3. 实际应用一项 AI 配置变化，确认聊天保留、待确认合同取消、旧未执行提案失效、已执行效果和支援次数保留；重试或回菜单后内存 Key 仍在。
4. 正常结局需主动看结果；新生接续的按钮和返回键都到结果，不回战斗；死亡直显“意识涣散”，没有额外动画／倒计时。
5. 结算核对真实用时、整局击杀、同步、差异、最多三条实际行为，以及再来一局／重试／返回菜单；不出现总分或奖励，重新开局无旧聊天、合同与暂停残留。
6. 看主菜单、HUD、暂停、平板与结算：中文和数值可读，按钮可点；四张新 Sprite 与旧平板图确实显示，动态文字仍是 TMP，没有“战斗已暂停”。

## 已知验证边界

GF01-INT 在隔离工作树用 Unity 2021.3.27f1c2 对 `8688c9b` 完成正常导入和 C# 编译，退出码 0，日志无 C# 编译错误或 Sprite 加载错误。其后现有 GUI Editor 的一次运行中，返回菜单触发隐藏平板刷新空引用；INT 已用 `27b8ed8` 定点修复，并保留旧会话与已认可平板功能。GUI Editor 仍占项目锁，**该修复后的编译和 Play 尚无复验证据**。本批次没有编写或运行自动测试、探针或回归矩阵；完整 Play、四结局逐一验证、真实 API 联网、Windows 构建及跨分辨率检查均未执行。现有 Editor 的一次异常发现不等于上述人工清单通过。
