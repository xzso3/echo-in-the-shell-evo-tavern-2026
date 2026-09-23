# UI-03B 平板主体接线

基线 `d1bbea1f0aacb032d4d98cd422703df1526fc6e2`。本切片只新增 `CommanderTabletView.cs` 和 `.meta`；没有修改 NativePhone/Hud、支援、场景或 Art。与 UI-03 的 `CommanderUiFactory.CreateTablet(canvasParent, chineseFont)` 调用兼容：工厂创建 `RectTransform`，挂 `CommanderTabletView`，调用 `Build(font)`。INT 也可直接在 Canvas 下创建 RectTransform 并调用 `Build`。Canvas 需要 `GraphicRaycaster`、EventSystem，以及 1280×720、Match 0.5 的 `CanvasScaler`。`Build` 幂等。

## 绑定

创建 `CommanderTabletBindings`，调用 `view.Bind(bindings)`：

- `Session` 接本局 `ICommanderSession`；`Support` 接同局 `ICommanderSupportBridge`。会话 `Changed` 自动刷新聊天、草稿、等待和提案状态。`RefreshExternal()` 刷新不经过会话事件的生命、连接、手动支援与记录变化。
- `CanCompose` 调当前安全节点 `CanCompose(out reason)` 的布尔结果；`ComposeReason` 返回原因。不要把敌近作为附加限制。`ConnectionStatus` 只描述 AI 通讯连接；`HealthStatus` 返回当前焦点玩家生命文字。
- `SupportText` 与 `SupportActions` 从现有 `INativeSupport`/`NativeSupportController`、合法记忆与授权档位生成本地操作；`RecordsText` 与 `RecordsActions` 从现有 Narrative/Phone 读取预设通讯、本局记录、保留异议、回声改写及最终选择/结局接续操作。`CommanderTabletAction` 是带 `Label/Enabled/Invoke` 的纯 UI 按钮。按钮动作执行后会调用 `RefreshExternal()`。INT 不需要把模型输出当作支援真值。
- 手动支援在合法 `Support.Request` 后，可用 `ShowManualContract(support.StatusText, () => support.Confirm(), () => support.Cancel())` 展示同一覆盖层；其中动作委托由 INT 绑定到现有真值。关闭／拒绝会调用取消委托。执行失败详情应通过 `SupportText` 显示。
- `CloseRequested` 接 NativeHud 的统一关手机入口；平板 `HandleBack()` 先关合同，再请求关机。由 NativeHud 把 Esc **只路由一次** 到 `HandleBack`；本组件 `handleEscapeLocally` 默认关闭，以免与 NativeHud 旧 Esc 分支同帧双重关闭。页签切换只改视图，不调用 `Session.Cancel()`；真正禁用平板时 `OnDisable` 取消未完成请求与打开的合同。重开应重建/Reset Session、SupportBridge 并隐藏平板。
- `ProposalKind` 可选，用提案 ID 映射经 Unity 校验后的 `NativeSupportKind`，以显示医疗/弱点解析标题；仅医疗显示医疗授权图标。若桥接口没有公开这种只读映射，可暂留空，通用“支援提案”仍可点击且不会猜测模型文字。`ProposalTitle` 允许提供更明确的本地标题。

## 资源与状态

`tabletFrameSprite`、`physicalReturnKeySprite`、`commanderSignalAvatarSprite`、三个 Tab Sprite、`authorizationMedicalSprite` 为七个 UI-02 资源的 Inspector 绑定点。赋值后调用 `ApplySprites()`；INT 应在最终场景/装配处序列化七项引用。图片不拦截聊天/按钮，返回键背景仍有独立 128×48 点击区。字、生命、消息、合同与数值都是 TMP/uGUI。

合同 `OpenContract` 和 `Accept` 均交支援桥校验；关闭、拒绝调用桥终结提案，接受失败保留失败文字并禁用再次接受。会话提供本地事实和错误消息；平板不发送 HTTP、不计算代价。长中文聊天、记录和合同均有独立 ScrollRect；聊天输入与底部通讯/支援/记录页签固定。无“战斗已暂停”文案。

## 待 INT 完成

1. 合入 UI-03 原三个文件、AI-02 会话、AI-03 支援桥与 UI-02 七图，序列化 Sprite 和中文 TMP 字体/fallback；本基线单独不含会话接口实现，因此不具备独立 Unity 编译环境。
2. 让 NativeHud/Phone 的所有打开与关闭入口统一控制平板/暂停，关闭时不重复消费 Esc；保留原最终选择和结局手机入口。
3. 把现有手动支援、预设通讯与记录行为填入页动作委托，连接状态、生命与安全节点原因填入只读委托。

按派发要求，本切片没有调用 Unity，也没有编写或运行自动测试；集成时处理实际编译与接线问题，最后再进行人工校验。
