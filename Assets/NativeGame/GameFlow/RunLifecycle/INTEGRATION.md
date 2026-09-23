# GF01-01 接线点

本提交只含 `GameFlow/Navigation`、`GameFlow/RunLifecycle`。共享 Run、Hud、Phone、HomeBootstrap、场景及 Build Settings 由 GF01-INT 独占写入。基线是 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；本工作树未运行 Unity、自动测试或探针。

## 菜单

- 在 `CommanderHome` 的首页宿主挂 `FlowNavigation`。`CommanderHomeBootstrap` 取同一个组件，将 `PreferredGameSceneName` 设为其 `gameSceneName`，把既有 `EnterGame = offline => ... SceneManager.LoadScene(...)` 替换为 `offline => navigation.TryStartGame(offline)`。既有 `CommanderHomeView` 的设置表单及传输绑定继续复用，不再直载场景。
- UI05 的主菜单按钮可绑定 `StartGame()`、`OpenAiSettings()`、`QuitApplication()`。`AiSettingsRequested` 由 UI05 打开共用设置表单；`AiConfigurationLabel`、`AiConfigurationChanged` 只表示配置存在，不表示实网健康。没有配置时 `StartGame()` 自动选择离线。明确的离线入口可绑定 `StartOffline()`。
- `LastError`／`ErrorChanged` 与 `IsNavigating`／`NavigatingChanged` 给 UI05 的可操作加载／错误面板。失败后保持按钮可用；在游戏场景可重试 `RetryRun()` 或 `ReturnToMenu()`。Editor 退出只提示停止 Play，不伪报退出。

## 游戏场景与开局

- 在实际游戏场景的 Run 对象挂 `FlowNavigation`（`run` 指向同对象）和 `FlowRunLifecycle`。`FlowNavigation` 优先选本机可加载的 `NativeDemoTilemap`，否则 `NativeDemo`；局内 `RetryRun()` 直接重载当前实际场景。`CommanderHome` 必须在 Build Settings 可加载。不要保存或覆盖主目录用户未提交的 Tilemap、字体、地图及 Build Settings；由 INT 在唯一 Unity 时段接线。
- `NativeRunController.Start()` 改为只准备本局：`Phase=Starting`、`Elapsed=0`，不发 `Started`，不让战斗／互动推进。新增 `BeginAction(): bool`：仅 `Starting` 可调用；同一调用内先置 `Playing`，调用现有 `combatIntegration.Initialize(this)`；成功后恰好一次发 `Started`，失败恢复 `Starting` 且不发事件。因为 `Initialize` 目前要求 `run.Running`，必须保持此顺序，且两步之间不能跨帧。初始化失败如已创建部分 World，还需由 Run／Combat 清理后才允许重试。
- Run 准备好后调用 `intro.Initialize(BeginAction, paused => pause.SetPaused(FlowPauseReason.Intro, paused))`。此时 UI05 依据 `IsIntroOpen` 显示每局操作说明：WASD、Space、E、Tab；“开始行动”绑定 `BeginActionFromButton()`。`TryBeginAction()` 幂等，失败保留说明和错误，成功才解 Intro 暂停并发 `ActionStarted`。GF01-02 的唯一暂停拥有者负责 Intro 时 `Time.timeScale=0`；本组件不持有时标。
- INT 在 `NativeHud` 输入入口屏蔽 `Starting` 阶段的世界输入；只在 `Running` 时交给战斗、互动和手机。旧 `NativeHud.restartButton`／`NativePhone.restart` 的 `level.Restart` 监听必须移除或改走 `navigation.RetryRun()`；共享 `NativeRunController.Restart()` 同样委托导航，避免绕过清理和再次说明。

## 离局

`FlowNavigation.TryLoad(FlowDestination)` 先选可加载场景，再上锁。`RunExitCleanup` 随后按顺序取消 Session 请求和旧代理、取消全局 HTTP、取消待确认 Support 合同、重置未执行提案与本局 Session、关闭平板／对白、释放暂停，然后载入目标场景。Unity 销毁本局 Run 时，战斗、计时、结果快照及订阅随场景释放。`CommanderSettings.Instance` 与 `CommanderHttpClient` 保留，内存 Key 不清；返回菜单不测试连接。

GF01-02／INT 应让既有 `PhonePause.ReleaseForRunEnd()` 委托给同一拥有者的 `ReleaseAllForRunEnd()`，清掉 Intro、Phone、PauseMenu、ReturnConfirm 全部原因。导航失败预检查时不清本局，异常时释放导航锁并经 `ErrorChanged` 显示“重试／返回主菜单”；UI05 需在错误面板保留这两个入口。若异常发生在清理后，当前场景仍可重载，但聊天等本局会话已清，不能宣称原局完整恢复。

GF01-03 的结果按钮只调用 `RetryRun()`／`ReturnToMenu()`，不再次截取或持久化结果。GF01-02 的返回确认框在确认后调用 `ReturnToMenu()`，取消确认不触发导航。导航期间 UI05／06 依据 `IsNavigating` 禁用重复按钮。
