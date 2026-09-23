# GF02 HUD → INT 最小接线

输入基线：`ef8c18cf64302ba8439c45cd96b8db9bb7044aeb`。本包只修改 `GameUI/Hud/`。

## NativeHud 接线

`NativeHudView.Create(Transform, TMP_FontAsset)` 和旧的九参数 `NativeHudDisplay` 构造保持可用。要启用真实线索、装备和平板可用性，在 `NativeHud` 缓存场景的 `NativeEquipment`（一次查找，不在每帧查找），然后把当前 `Bind` 调用换成新增的十参数构造：

```csharp
// NativeHud 私有字段：NativeEquipment hudEquipment;
// InitializeFlowUi 中：hudEquipment = FindObjectOfType<NativeEquipment>();
flowHudView.Bind(new NativeHudDisplay(health, maxHealth, autoFire,
    level.quest.ObjectiveText, interactionPrompt, level.Elapsed, kills,
    level.narrative.Sync, level.narrative.Difference,
    NativeHudExtras.FromRun(level, hudEquipment,
        level.IsCombatAdvancing && !phonePanel.activeSelf)));
```

新快照到达且装备组件存在时，View 才关闭场景里的旧 `Pulse coil status` 面板，防止同屏重复并保留未接线时的信息。`Create` 定向关闭游戏 Canvas 的旧 `Title`，即“壳中回响 / 信号 01”；INT 若编辑该场景，可同时移除这个旧标题对象。现有 `Header`/`Footer` 隐藏逻辑仍由 `NativeHud.InitializeFlowUi` 执行。

## 数据映射

| HUD | 真实来源 |
| --- | --- |
| 生命值/最大值/血条 | `NativeHud.Update` 现有 `ActiveToolkitPlayer` 或 `level.player` |
| 开火状态/Space 操作 | `ActiveToolkitWeapon.AutoFire` 或 `level.combat.AutoFire` |
| 任务标题、三条地点线索、后续阶段指引 | `level.quest.ObjectiveText`；线索行由它的第二行拆分，不复制示例值 |
| 记忆进度/三项完成标记 | `level.narrative.MemoryCount`、`HasMemory(NativeMemoryKind)`；分母为 `NativeMemoryKind` 数量 |
| 用时/击败/同步/差异 | `level.Elapsed`、现有 `RunKillLedger` 回退逻辑、`level.narrative.Sync/Difference` |
| 装备/超频剩余秒数 | 缓存的 `NativeEquipment` 的 `HasCoil/Equipped/OverclockActive/OverclockSecondsLeft`、现有加成与持续时间字段 |
| Tab 可用性 | `level.IsCombatAdvancing && !phonePanel.activeSelf`；文案“安全通讯 / 本地”不宣称 LLM 连通 |
| E 交互提示 | `NativeHud.Update` 现有近距离目标选择与 `interactionPrompt` |

## 图标

运行时创建的 View 有四个公开 `Sprite` 槽：`healthIconSprite`、`weaponIconSprite`、`equipmentIconSprite`、`tabletIconSprite`。对应 ART 的 `GameUI/Art/GF02/HealthCross.png`、`WeaponAuto.png`、`EquipmentChip.png`、`Tablet.png`。资源准备后由 INT 从 UI 所有的 `GameUiArtCatalog` / 共享资源资产接入，再调用 `ApplyArt()`。缺图时四槽有原生几何占位，不会遮蔽动态文本。已有 `keycapSprite` 槽继续使用。

## 原图右侧橙条

它是世界空间的 `Gate - relay opens this collider`：`NativeDemoBuilder` 在 `(10,0)` 创建橙红色 SpriteRenderer，场景把对象连到 `NativeMap.exitGate`，`NativeMap.OpenExit()` 按玩法状态关闭门。HUD 代码没有删除或重绘它；它的碰撞和通路信息保留。

## 待 INT 集中验证

本工作包按分工不运行 Unity、不编译、不跑自动测试或截图巡检。请在合入后检查编译、图标导入、16:9/16:10/低分辨率文字裁剪、伤害恢复、三条记忆、拾取/装备/超频、平板与近距离交互，以及标题和旧装备面板的可见性。
