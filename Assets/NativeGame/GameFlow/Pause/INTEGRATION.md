# GF01-02 接线入口

本切片从 `3a7390f7258e73eec38a42098d33cb9a17dc3cab` 构建。共享 `NativeRunController`、`NativeHud`、`NativePhone`、`CommanderHomeBootstrap`、旧 `NativePhonePauseController` 和场景由 GF01-INT 串行改动。

1. 在游戏 Level 上取得唯一 `FlowPauseController` 和 `FlowPauseMenuController`。旧 `NativePhonePauseController` 的 `OpenPhone`／`ClosePhone`／`ReleaseForRunEnd` 与 `IsRunActive`／`IsCombatAdvancing` 须转发到它，删去旧组件内保存与恢复 `Time.timeScale` 的字段和写入。不能在新旧两个组件各留一份恢复值。
2. 开局说明出现前调用 `SetPaused(FlowPauseReason.Intro, true)`；`BeginAction` 成功开始战斗时释放 Intro。探索与 Boss 都读同一个 `IsCombatAdvancing`；`Run.Running` 仍为真，暂停中的合法支援继续允许。进入任何终局演出前调用菜单的 `CloseForRunEnd()` 和暂停的 `ReleaseAllForRunEnd()`，使现有 `NativeEndingSequence` 的缩放时钟可以推进。终局手机 `OpenPhone()` 不再冻结时钟。离局时 `ReleaseAllForSceneExit()`。
3. `NativeHud` 每次 Esc 只调用一次 `FlowPauseMenuController.HandleEscape()` 并跳过旧 Esc 分支。同帧的世界移动／开火／互动输入要在菜单或 Intro 原因持有时停止；关闭面板清零移动输入但保留原自动开火选择。合同窗先关合同，下次 Esc 才关手机。文字输入框先失焦。新生接续手机的返回由 `TerminalPhoneBackRequested` 接 GF01-03 结果入口，不能回战斗。
4. GF01-05 的暂停 UI 订阅 `PageChanged` 显示 `Menu`／`Settings`／`ReturnConfirm`。按钮分别调用 `Continue`、`OpenSettings`、`CloseSettings`、`AskReturnToMenu`、`CancelReturnToMenu`、`ConfirmReturnToMenu`。`TryReturnToMenu` 绑定 GF01-01 的去重导航；失败时保持确认框并显示可操作错误。
5. GF01-05 的唯一 `GameAiSettingsView` 同时用于首页和局内。表单打开时读取 `FlowSettingsDraft.FromCurrent()` 到临时编辑缓冲，点击应用时调用 `FlowSettings.TryApplySettings(draft, run, out changed, out error)`；首页传 `run=null`。局内 `Settings` 页继承 `PauseMenu` 原因。返回而不应用时只丢弃缓冲。空 Key 保持内存值，显式 `ClearApiKey` 才清除。
6. `FlowSettings` 只在配置实际变化后取消旧请求／手动待确认合同，并调用 `CommanderSupportBridge.ExpireUnexecuted()`。此单一 Commander 补丁要随切片导入；它保留聊天和已执行效果，不调用 `Session.Reset()`。测试连接使用程序级 `CommanderHttpClient`，不写局内消息。

本切片没有 Unity 时段；导入与正常编译交 GF01-INT。没有写或运行自动测试。
