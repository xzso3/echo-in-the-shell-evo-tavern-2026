# AI-00｜最小公共接口与接入点

基线：`131c6d4ceeaa75db208cef2f61fea9196b9e27d4`，隔离工作树，2026-09-23。主目录的 `AGENTS.md` 与 `Docs/AICommander/{PHASE_TASKS,DEVELOPMENT,EXECUTION,STATUS}.md` 是只读输入；它们尚未进入本基线。本文件冻结本阶段并行任务之间的调用约定，跨任务值类型在 `Assets/NativeGame/Commander/Contracts/CommanderContracts.cs`，命名空间统一为 `Echo.NativeGame.Commander`，位于现有 Native 默认程序集。C# 文件不包含实现或序列化的 Key。

## 1. 所有者与精确调用面

下列为实现签名，任务在各自目录实现；公共 DTO 不再各自复制。接口若作为 C# `interface` 落地，采用这里的成员名和类型。

| 所有者 | 对外调用面 | 调用者与约束 |
| --- | --- | --- |
| AI-01 `Commander/Configuration` | `ICommanderSettings`: `string BaseUrl`, `string ModelId`, `int TimeoutSeconds`, `bool HasKey`, `string EndpointUrl`; `void SetEndpoint(string baseUrl, string modelId, int timeoutSeconds)`; `void SetApiKey(string key)`; `void ClearKey()` | UI-03 首页编辑；仅 URL、模型、超时可存本机。Key 只在程序内存，重开本局保留。不要在 DTO、Prefab、日志或 PlayerPrefs 留 Key。 |
| AI-01 `Commander/Transport` | `ICommanderTransport`: `bool Busy`; `bool SendAsync(IReadOnlyList<CommanderMessage> messages, Action<CommanderTransportResult> completed)`; `void Cancel()` | AI-02 游戏会话与 UI-03 首页连接检查共用一个传输实例。`false` 表示未开始，调用者同步显示配置／忙碌原因；已开始的请求恰好回调一次，取消回 `Cancelled`。回调仅交 `choices[0].message.content` 纯文本或不含凭据的错误类别。 |
| AI-02 `Commander/Context` | `ICommanderSnapshotSource`: `bool TryCapture(Guid sessionId, out CommanderSnapshot snapshot, out string reason)` | 每次发送即时读取真值；由 AI-03 提供本次 `snapshotId` 下合法 `CommanderSupportOption`。失败不发请求。 |
| AI-02 `Commander/Conversation` | `ICommanderSession`: `Guid SessionId`, `long RequestGeneration`, `bool Busy`, `string Draft { get; set; }`, `IReadOnlyList<CommanderChatItem> Messages`; `event Action Changed`; `bool Send(string input)`; `void Cancel()`; `void Reset()` | UI-03 绑定。`CommanderChatItem` 由 AI-02 定义，至少有只读 `Text`、`Source`（玩家／在线AI／本地事实／错误）、可选 `Guid? ProposalId`。`Changed` 在忙碌、消息、草稿和提案状态变化时触发。 |
| AI-03 `Commander/Support` | `ICommanderSupportBridge`: `IReadOnlyList<CommanderSupportOption> GetAvailableOptions(Guid sessionId, Guid snapshotId)`; `bool TryRegister(CommanderProposal proposal, out string reason)`; `bool OpenContract(Guid proposalId, out string contractText, out string reason)`; `bool Accept(Guid proposalId, out string resultText)`; `void Reject(Guid proposalId)`; `void Cancel(Guid proposalId)`; `CommanderProposalState GetState(Guid proposalId)`; `event Action<Guid, CommanderProposalState, string> ProposalChanged` | AI-02 注册已经解析通过的卡片并接收实际结果；UI-03 查看／确认／拒绝／关闭合同。`contractText` 与 `resultText` 取自 Unity 真值，不取模型对白。终态单向且不可重试执行。 |
| AI-04 `Commander/Pause` | `IPhonePauseController`: `bool IsPhoneOpen`, `bool IsRunActive`, `bool IsCombatAdvancing`; `void OpenPhone()`; `void ClosePhone()`; `void ReleaseForRunEnd()` | INT-AI 从 NativeHud 所有打开／关闭入口调用；方法幂等。`IsRunActive` 对应当前 `NativeRunController.Running` 存活语义；`IsCombatAdvancing = IsRunActive && !IsPhoneOpen`。关闭结局手机不得重启已结束战斗。 |

`CommanderMessage.Role` 只准 `System/User/Assistant`，由 AI-01 序列化为小写 `role`；`Content` 为消息内容。`CommanderTransportResult.Content` 是外层 chat completion 解析后的内层字符串，AI-02 独占将其解析为 `{ "reply": string, "proposal": null | { "kind": "medical" | "weakpoint", "option_id": string } }`。外层 HTTP 及超时错误属 AI-01；内层格式、选项合法性属 AI-02。首页连接检查走同一 `SendAsync`，只要求有效非空文本，不进 `ICommanderSession`、不产生提案。

`EndpointUrl` 由 AI-01 归一化：去尾斜杠；完整 `/chat/completions` 原样；末尾 `/v1` 补 `/chat/completions`；仅主机补 `/v1/chat/completions`；其他显式路径补 `/chat/completions`。仅支持 `/v1/chat/completions` 兼容 POST、Bearer、`stream:false`、`messages`；不传 tools。默认超时 15 秒，基于真实时间；单请求、无流式、无自动重试。旧 `NativeCommanderProxy` 的 StreamingAssets 代理协议不覆盖新设置。

## 2. 快照、身份与提案

`CommanderSnapshot` 只公开生命、当前目标、已知记忆、已知路线与通行情况、当前已知 Boss 阶段、可用支援选项及近期有效事实。空白／未知内容省略，不能把设计文本、未发现剧情、隐藏路线、未来结局或整份 `NativeNarrative` 送出。`KnownRoutes` 来自已配置路线与本局可见通行状态，不做寻路。`RecentFacts` 从已确认本局行为中截取有界尾部。每次请求顺序为固定规则、最新快照、最近 6 轮成功问答及相关实际支援反馈、当前输入；当前输入只出现一次。失败／取消不占成功历史。

AI-03 在只读查询中给每个合法 kind／tier／已找回 memory 组合生成不透明 `option_id`，并持有 `snapshotId → optionId → 当前目标` 映射；不能为探测可用性调用有副作用的 `NativeSupportController.Request`。`CommanderSupportOption` 的 kind/tier/memory 供 Unity 校验与显示，给模型只发送 ID 与必要的合法含义。模型不得指定提案 ID、会话 ID、快照 ID、伤害、回血量或同步分值。无合法选项时 `proposal:null`。AI-03 需为现有 Support 提供窄只读可用性查询，避免额外合同占槽。

AI-02 每局新建 `SessionId`，每次 Send 增加 `RequestGeneration` 并捕获最新 `SnapshotId`、Native Run 身份与当前焦点 scope／玩家／Boss 目标。模型成功返回后必须先核对仍是本局、当前代次、当前有效焦点与请求，再验 `kind` 和 `option_id` 对应，最后由 Unity 生成 `ProposalId` 并 `TryRegister`。关闭手机、取消、配置变化、离开有效 Run、重开先令代次失效，再取消 HTTP；迟到回调不入聊天。切换手机页签只改变视图，不取消 HTTP。重开清空草稿、消息、提案和合同；API 设置与内存 Key 留在程序级对象。

AI-03 注册仅生成 `Available` 聊天卡，不预占 Support 合同。`OpenContract` 再核对当前 Run、焦点、目标、记忆、选项和一次性使用状态，且 `Support.HasPending` 时提示已有合同，不覆盖手动支援。查看成功才调用 `Support.Request`，把 `StatusText` 作为合同真值。`Accept` 再校验并调用 `Support.Confirm`；后者产生实际效果和 `Authorized`，既有 ECA 唯一调用 `NativeNarrative.RecordSupport`。AI-03 根据真实前后值／正式状态回传结果消息；拒绝与关闭合同调用 `Support.Cancel` 且终结卡片，失效和失败不收费。仅一张合同待确认；模型没有任何直接游戏写权限。

## 3. 现场接线与文件归属

- **当前可玩入口**：此 SHA 的 Build Settings 仅启用 `Assets/Scenes/NativeDemo.unity`，`Echo → Native → Open Playable` 也打开它；没有已提交的首页场景。UI-03 交独立首页配置 UI／装配代码，INT-AI 独占创建或选定首页场景、设置启动顺序并保存共享场景。主目录 README 所述 Tilemap 版及 Build Settings 是用户未提交状态，不可从本隔离工作树覆盖。
- **手机入口**：`NativeHud.TogglePhone/ClosePhone/ShowResult`、Tab/Esc、关闭按钮，`NativePhone.ShowFinalDecision/ShowContinuation`，以及 `NativeRunController.Complete/PlayerDied/Restart`。INT-AI 将所有显示／隐藏接到 `OpenPhone/ClosePhone/ReleaseForRunEnd`，并把会话取消只放在真正关机、重开、配置变更等生命周期点。当前 `NativePhone.SelectPage`、`ShowFinalDecision`、`ShowContinuation`、`CancelComposition` 把失焦和取消 HTTP 混在一起；最终修改必须拆开。UI-03 不自行改 `NativePhone` 或 `NativeHud`。
- **支援真值**：`NativeEcaRules.Support` 是 `INativeSupport`；`NativeSupportController.Request/Confirm/Cancel/StatusText/HasPending/Authorized` 已实现效果、锁定目标和重复保护。AI-03 独占必要的 Support／接口改动。`NativeEcaRules.OnSupportAuthorized` 依赖 `level.Running`，所以手机暂停期间此语义必须保持 true。
- **焦点真值**：`NativeHud.ActiveToolkitPlayer/ActiveInputScope/ActiveInputReady/CurrentSupportBoss()/CurrentCombatSupportBoss()`；`NativeLevelHost.RunId/FocusedInstanceId/TryGet/Focus` 与 `Mounted/Unmounted`。快照与支援优先取当前焦点 Toolkit actor；无集成焦点时读 `NativeRunController.player` 与原生 Boss。焦点变更使旧选项和合同失效，不以静态 `NativeEnemy.Active` 或未聚焦实例作目标。
- **安全节点和暂停**：AI-04 独占 `NativeCommanderSafeNode.CanCompose`，以当前焦点玩家位置与有效 Run 判定，移除敌近排斥及旧“不暂停”文案。`NativeRunController.Running` 保持存活语义；AI-04 添加推进守卫并冻结移动、攻击、伤害、冷却、核心窗口、Buff 和计时，UI／HTTP 用未缩放时间。不能停用战斗 GameObject。INT-AI 接入全部输入屏蔽、旧移动输入清零及结局演出前释放暂停。

共享文件最终写入者：`NativePhone/Hud`、场景、Build Settings、首页启动与字体引用均为 INT-AI；`NativeSupportController/INativeSupport` 为 AI-03；`NativeRunController/NativeCommanderSafeNode` 为 AI-04。AI-01、AI-02、UI-03 只写各自独立目录并交 INT-AI 接线。无 Unity 运行权限；最后人工核验由 INT-AI 协调。
