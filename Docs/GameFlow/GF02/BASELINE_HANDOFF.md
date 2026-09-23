# GF02 共享基线交接（GF02-INT）

冻结日期：2026-09-23。代码及批准资料的**精确输入提交**为 `da85fbd3b28ad8f36e7f1c9b47c6c3c1dfa31ac5`，其父提交是 GF01 集成头 `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`。唯一集成分支 `codex/gf02-integration`；INT 独立工作树 `/Users/const/.codex/worktrees/4e44/echo-in-the-shell-evo-tavern-2026`。代码任务从 `da85fbd3b28ad8f36e7f1c9b47c6c3c1dfa31ac5` 或包含本交接文件的后续集成头启动，派发时记录实际 SHA，不从主目录默认分支启动。本文件提交只增交接说明，不改变代码基线。

## 输入与入口

| 输入 | 复核结果 |
| --- | --- |
| GF01 分支 | `codex/gf01-integration` 当前 HEAD `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`，工作树 `/Users/const/.codex/worktrees/3f6d/echo-in-the-shell-evo-tavern-2026`。GF01 未提交字体资产及 `.vscode/` 不纳入。GF01 末次平板修复后尚无编译／Play 复验证据。 |
| 主目录 | `/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`，HEAD `3a7390f7258e73eec38a42098d33cb9a17dc3cab`。用户未提交 Tilemap、地图、字体、设置和其他资料保持原位；本次未复制或改写。 |
| GF02 资料 | 仅将主目录 `Docs/GameFlow/GF02/` 的 10 份文档及 8 个 `References/` 文件纳入 `da85fbd`，共 18 个文件；该目录没有 Unity `.meta`。未顺带纳入 `Docs/GameFlow/` 其他未跟踪目录。`STATUS.md` 是准备时快照，执行状态由主控继续维护。 |
| 引擎 | 仓库规定 Unity `2021.3.27f1c2`、URP `12.1.12`，保留现有版本。 |
| 启动场景 | `Assets/Scenes/CommanderHome.unity`，已提交 Build Settings 的第一场景。菜单 Canvas 是 Screen Space Overlay，当前场景无 Camera 或 AudioListener。Bootstrap 的首选游戏场景是 `NativeDemoTilemap`；该场景只在用户主目录未跟踪文件中，干净基线由 `FlowNavigation` 回退到已提交并启用的 `Assets/Scenes/NativeDemo.unity`。各任务以干净基线开发，不假定 Tilemap 存在。 |

## 唯一文件所有者

路径省略 `Assets/NativeGame/` 前缀；完整边界见 `PARALLEL_EXECUTION.md`。跨界改动交 INT 说明或小补丁，不用旧文件整份覆盖集成版。

| 负责方 | 独占写入 |
| --- | --- |
| NET | `Commander/Transport/` 及其局部 SDK 适配；仅给 INT 依赖建议。 |
| CHAT | `Commander/Conversation/`、`PhoneUI/CommanderTabletView.cs`、`PhoneUI/CommanderUiFactory.cs`、`Commander/Integration/CommanderTabletRunAdapter.cs` 及局部适配。 |
| UI | `GameUI/Menu/`（含 `GameUiArtCatalog.cs`、`GameFlowUiFactory.cs`）、`GameUI/Intro/`、独立菜单动画组件。 |
| HUD | `GameUI/Hud/` 及自身局部显示组件。 |
| ART | `GameUI/Art/GF02/` 图像、资源清单与 prompt；需 Editor 产生或调整的 `.meta` 由 INT 串行落地，保留 GUID。 |
| INT | `NativeHud.cs`、`NativeRunController.cs`、`NativePhone.cs`、`Commander/Integration/CommanderRuntimeHost.cs`、`CommanderHomeBootstrap.cs`、`Commander/Configuration/CommanderSettings.cs`、`GameUI/GameFlowSettingsAdapter.cs`、Support/Narrative/Toolkit 共享接线、`Commander/Contracts/` 共享契约、场景、Prefab、`GameUI/Resources/`、Packages/锁文件、asmdef、必要 ProjectSettings。 |
| 主控 | GF02 规格、`STATUS.md`、派发与交付索引；INT 独占本文和 `FINAL_HANDOFF.md`。 |

## 保留接口与装配约定

| 边界 | 现有签名和最小约定 |
| --- | --- |
| NET → CHAT | `ICommanderTransport.Busy`、`SendAsync(IReadOnlyList<CommanderMessage>, Action<CommanderTransportResult>)`、`Cancel()`；`CommanderHttpClient.GetOrCreate()` 与 `TestConnection(Action<CommanderTransportResult>)` 是现有 Host/设置入口。结果类型和状态在 `Commander/Contracts/CommanderContracts.cs`。SDK 类型不要泄露到会话/UI。需要扩展共享结果契约时先交 INT 具体签名。 |
| CHAT → INT | `ICommanderSession`: `SessionId`、`RequestGeneration`、`Busy`、`Draft`、`Messages`、`Changed`、`Send(string)`、`Cancel()`、`Reset()`。`CommanderRuntimeHost.Session`/`SupportBridge`，`CommanderTabletRunAdapter(NativePhone, CommanderTabletView, CommanderRuntimeHost)` 保持可装配。平板 `CommanderTabletBindings` 已有 `SupportText/Actions`、`RecordsText/Actions`、`CanCompose` 等委托；CHAT 先完成真实绑定与本地 fallback。 |
| 设置 | `CommanderSettings.Instance` 提供 `BaseUrl`、`EndpointUrl`、`ModelId`、`TimeoutSeconds`、进程内 Key、`Changed`；`GameFlowSettingsAdapter.Create(NativeRunController, Action closed = null)` 复用同一传输做独立连接测试。Key 不进入资产、日志或提交。 |
| UI → INT | `CommanderHomeBootstrap` 调用 `GameFlowUiFactory.CreateMenu(transform, font, art)`；`NativeHud` 调用 `GameFlowUiFactory.CreateIntro(canvas.transform, font, art)`。UI 保留这些工厂入口或提前交具体签名变更。`GameUiArtCatalog` 由 UI 扩充字段；INT 更新 `Resources/GF01Art.asset` 序列化引用。菜单仍仅三个入口。 |
| HUD → INT | `NativeHudView.Create(Transform, TMP_FontAsset)`、`NativeHudDisplay(...)` 与 `NativeHud` 现有显示接线为起点。HUD 若需新增真实状态字段，交 INT 最小构造／更新签名及数据源，不在 View 中改玩法规则。 |
| ART → UI/HUD/INT | `GameUI/Art/GF02/`：纯背景、局部循环帧、英文标题、CipherWorks、必要 HUD 图标；ART 清单给尺寸、透明度、帧顺序/时长和导入建议。`GameUiArtCatalog` 是菜单代码槽位，`Resources/GF01Art.asset` 是现有共享资产，新增序列化槽位由 UI 定义、INT 绑定。正文与动态数值保持 TMP。 |
| 场景 | `CommanderHome` 的默认 Camera/唯一 Listener 由 INT 保存；菜单 Canvas 保持既有输入与三个动作。`NativeDemo` 是共享干净基线，Tilemap 场景如后续确需纳入，先限定范围交接并保留其 `.meta`/GUID。 |

当前可并行开工的非阻断事项：Tilemap 场景不在冻结提交、GF01 末次修复未复验、GF02 SDK/图像/功能尚未实施。Unity Editor 由 INT 唯一占用；如用户正在占用 Editor，先协调可用时段，源码与资源工作继续。按 `ACCEPTANCE.md` 最终一次人工校验；当前全部未测。
