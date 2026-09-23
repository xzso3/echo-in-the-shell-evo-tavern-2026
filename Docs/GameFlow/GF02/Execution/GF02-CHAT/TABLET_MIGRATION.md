# GF02-CHAT 平板迁移对照

输入基线：`ef8c18cf64302ba8439c45cd96b8db9bb7044aeb`。工作树：`/Users/const/.codex/worktrees/85e6/echo-in-the-shell-evo-tavern-2026`。本表随实现更新；最终编译和人工验收由 INT 统一执行。

| 旧手机入口 | 平板入口 | 实际服务或数据 | 当前处理与缺口 |
| --- | --- | --- | --- |
| 通讯：任务、记忆、你是谁、关于授权 | 通讯页预设主题按钮；记录页同类目录 | `NativePhone.CommsText`，实时 `quest.ObjectiveText`、`narrative.MemorySummary/ScoreSummary` | 迁入通讯快捷主题，明确标记为本地预设；不冒充模型回复。 |
| 自由输入 | 通讯页输入/发送/取消 | `CommanderSession` → `ICommanderTransport`；`CommanderSnapshotSource` 核对安全节点与焦点 | 同一会话保留草稿、等待、在线回复及失败降级；未配置和失败时保留本地主题，任意自由问题只给明确本地反馈。设置页连接测试占用共享传输时显示正在通讯，不触发降级。 |
| 保留异议 | 记录页动作 | `level.rules.PreserveAnomaly()`、`narrative.HasMemory(Private)`、`PreservedAnomaly` | 沿用一次性前置条件与原计分服务。 |
| 医疗、弱点、有限/深度授权、记忆选择 | 支援页选项与查看合同 | `INativeSupport.CanApply/Request/Confirm/Cancel` | 效果和同步度由本地 Support 决定；模型提案只生成可查看合同卡。 |
| 合同接受/拒绝/关闭 | 平板合同遮罩 | 手动合同走 `INativeSupport`，在线提案走 `CommanderSupportBridge` | 确认前复核合同归属，拒绝/关闭不执行；旧结果不回灌新局。 |
| 网络：初始回声、记忆档案、行为、同步与差异 | 记录页目录 | `narrative.MemorySummary/BehaviorSummary/ScoreSummary/RewroteEcho` | 初始回声及合法空记录补齐；全服网络仍未接入。 |
| 改写初始回声 | 记录页动作 | `level.rules.RewriteEcho()` 与 `HasMemory(InitialEcho)` | 沿用每局一次的前置条件和差异度服务。 |
| 最终节点写入/销毁 | 记录页最终节点覆盖态 | `level.rules.ChooseFinal()` | 由原生流程显示/关闭；选择仍经原规则。决策期间限制为记录页，并取消旧在线请求。 |
| 新生接续 | 记录页接续文本 | `narrative.BirthStage`、`ScoreSummary` | 仅本局文本，不向其他玩家发送。 |

## 显示与共享接线

- 平板自身 `Scroll` 的 Viewport 原先使用 alpha 0.001 的 Image 加 `Mask`；改用 `RectMask2D` 与平板原有暗色不透明底图，既避免透明图形被剔除时连带裁掉子内容，又保留拖动滚动的射线目标。菜单/设置使用的共享工厂属 UI 文件归属；UI 已独立修其相同组合，CHAT 不修改该文件。
- `NativePhone.Start` 已创建 `CommanderTabletRunAdapter`；`NativeHud.OpenPhone` 已调用 `RefreshExternal`。共享 Native/Host 文件不由 CHAT 修改。
- `CommanderRuntimeHost` 已订阅 `CommanderSettings.Changed` 并通知会话；INT 可在集成时核对变更先后顺序及失效提案处理。
- `NativeHud.ClosePhone` 已取消 `ICommanderSession`，`PrepareForRestart` 已重置会话和提案桥；旧 `NativePhone.CancelComposition` 只处理旧代理。平板 `OnDisable` 也关闭合同并取消会话请求。
- 支援页直接读取 `INativeSupport.StatusText/CanApply/HasPending` 与 `narrative.ScoreSummary()`；合同接受、拒绝、关闭调用原服务。记录页读取实时 `quest`/`narrative`，无行为记录时给合法空状态。页面保持定时刷新，避免游戏状态变化后旧文字滞留。

## 尚待集中确认

- INT 的 Unity 导入编译和最后人工校验；本任务不启动 Editor、自动测试、mock 或截图巡检。
- NET 的 SDK 传输替换保持 `ICommanderTransport` 结果语义；如改变结果签名，由 INT 协调共享契约。
- 局内真实 API 成功、不可达、超时、失效焦点、合同和新生全流程均未人工验收；主控指定的最终清单由 INT/用户在同一集成头完成。
