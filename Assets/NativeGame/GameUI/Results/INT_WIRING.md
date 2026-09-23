# GF01-06 原生 UI 接线交接

输入：冻结基线 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；GF01-03 提交 `3b8025c146f2dcc100e537c70b18e128c52bbb6f` 提供 `RunResultSnapshot` 和 `NativeRunResultFlow`，后续 `b92ba6b` 增加真实本局击杀聚合；GF01-04 最终提交 `315e2939aebd7190a4b4b7832c8ecff9b0e3a80c` 提供 `GameUI/Design/GF01_LAYOUT.md` 和四张生产 Sprite。此提交只新增 `GameUI/Hud`、`Ending`、`Results`，没有保存共享场景或改 `NativeHud`/`NativePhone`。

## 创建与绑定

在游戏场景现有 `UI` Canvas（1280×720、`Scale With Screen Size`、Match 0.5）下，用已有 FusionPixel 中文 `TMP_FontAsset`：

```csharp
var hudView = NativeHudView.Create(canvas.transform, chineseFont);
var endingView = NativeEndingView.Create(canvas.transform, chineseFont);
var resultsView = NativeResultsView.Create(canvas.transform, chineseFont);
var birthAction = NativeBirthResultAction.Create(phone.tabletView, chineseFont);
var terminalUi = run.gameObject.AddComponent<NativeTerminalUiPresenter>();
terminalUi.Bind(run.GetComponent<NativeRunResultFlow>(), hudView,
    endingView, resultsView, birthAction);
```

`NativeBirthResultAction.Create` 要在现有 `CommanderTabletView.Build` 之后调用。它只在原平板的底边加一个原生 `Button`，不修改外壳、七张 Sprite、聊天和合同。初始 `hudView.Show(false)`，只在 `BeginAction()` 成功并进入 `Playing` 后显示。一个 Canvas 保持一个 `GraphicRaycaster`，场景保持一个 `EventSystem`。

`NativeHud` 在原本计算 health/fire/objective/nearby 的同一帧传入原始值：

```csharp
hudView.Bind(new NativeHudDisplay(health, maxHealth, autoFire,
    run.quest.ObjectiveText, nearbyWorldAction, run.Elapsed, kills,
    run.narrative.Sync, run.narrative.Difference));
```

`nearbyWorldAction` 仅在确有可互动目标时传入短动作与目标名，否则传空字符串；不要把常驻 WASD/Space/Tab 帮助文案塞进互动提示，也不要再重复带 `E /` 前缀。Toolkit 输入实例活跃时取当前 Toolkit 玩家、武器数值，其他时候取已有 Native 玩家、战斗数值；`NativeHudView` 不解析旧 HUD 字符串。平板或暂停等顶层窗口打开时隐藏新 HUD，普通游玩才显示。隐藏旧 HUD 的宽 `Header`/`Footer` 文字与新视图的重叠，但保留 `NativeHud` 输入和其他模块引用；共享 `NativeHud` 修改由 INT 独占。

## 终局与按钮

GF01-03 的 `NativeRunResultFlow.StageChanged` 由 `NativeTerminalUiPresenter` 订阅。`EndingPresentation` 绑定快照的 `Title`、`PresentationText`，玩家点“查看本局结果”后调用 `flow.ViewResults()`；`BirthSequence` 隐藏 HUD，不显示结果按钮；`BirthContinuation` 显示平板底边“查看本局结果”，其点击走同一个 `flow.ViewResults()`；`Results` 绑定冻结快照。死亡直接进入 `Results`，结果视图固定显示“意识涣散”，按钮为“重试”。成功按钮为“再来一局”。关键经历只渲染 `snapshot.KeyEvents` 前三条，不补写推测行为。

平板原物理返回键仍复用 `CommanderTabletView.HandleBack`。在已有 `CommanderTabletBindings.CloseRequested` 的回调中先调用 `terminalUi.TryHandleTabletBack()`；返回 `false` 才走普通 `hud.ClosePhone()`。GF01-02 的 `FlowPauseMenuController.TerminalPhoneBackRequested` 也接 `terminalUi.TryHandleTabletBack()`，覆盖新生接续时的 Esc。合同开着时 `HandleBack` 已先关闭合同，不会进入此回调。新生接续时物理键文字由 `NativeBirthResultAction` 临时显示“查看本局结果”，实际按钮仍走同一个结果入口。终局旧 `NativePhone.restart` 按钮不能直接重开，旧 `NativeHud.resultPanel`/`ShowResult` 不能与新结果页叠加；INT 统一关掉这些旧可见入口，不改原平板 Sprite。

```csharp
resultsView.ReplayRequested += () => {
    if (!navigation.TryLoad(FlowDestination.Game))
        resultsView.ShowNavigationError(navigation.LastError);
};
resultsView.MenuRequested += () => {
    if (!navigation.TryLoad(FlowDestination.Menu))
        resultsView.ShowNavigationError(navigation.LastError);
};
```

上例只示意按钮与 GF01-01 导航的方向；实际 `FlowNavigation` 获取和离局清理顺序由 INT 按 GF01-01 接口接线。按钮触发时会暂时禁止重复点击，加载失败须调用 `ShowNavigationError` 恢复可操作状态。结果页返回菜单无需游玩中退出确认。

GF01-04 的 `KeycapBlank` 赋给 `NativeHudView.keycapSprite`，`LightPanelCorner` 分别赋给 `NativeEndingView.panelCornerSprite` 和 `NativeResultsView.panelCornerSprite`，`TerminalButtonBlank` 可赋给 `NativeResultsView.buttonArtSprite`，再调用各 `ApplyArt()`。单角图被四角翻转复用，不拉伸成整框。`TMP` 文案、数值、点击区始终保持原生。现有 `PhoneUI/Art` 七张 Sprite 已获认可，继续使用现有场景序列化引用。此任务没有 Unity 使用权；编译、场景接线、视觉与 Play 校验由 INT／最终人工操作者完成。
