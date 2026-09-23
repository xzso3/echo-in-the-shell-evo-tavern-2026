# INT-LT Native 共享战斗接线交接

本切片基于 `8a0a322`，代码提交 `ab091a1`、Unity 保存资源提交 `3031f05`。NB-03 最终状态与绑定提交随后精确导入为 `e767270`、`bf456cf`。所有操作在独立集成工作树完成；未运行旧 NativeDemo 一次性重建菜单。

## 状态与实例归属

- `NativeRunController` 在进入 Playing 后显式调用场景上的 `NativeCombatIntegration.Initialize(this)`。此适配器每局创建一个 `RunId`、一个 `RuntimeScope`/`LevelInstanceContext` 和一个 `CombatWorld`；`ContentIdentity` 为场景字段 `echo/native-demo/main`。初始化失败时不广播 `Started`，运行保持停止。
- 保存的 `NativePlayer`、`NativeCombat`、`NativeProjectile`、`NativeEnemy`、`NativeFanAttack`、`NativeBossController`、`NativeBossActor` 原脚本与字段保持兼容。实际玩家移动/生命、武器、弹丸扫掠、敌人 Chase/Orbit/Fan、Boss 阶段/外壳/核心 E 均由同对象的 `Combat*` 组件执行；旧组件仅转发输入、支援和 HUD 所读的公开入口。`NativeEnemy.health` 是兼容镜像，共享 `CombatEnemy.health` 为实际生命源。
- 五个旧 Prefab 在原路径增加共享组件，GUID 不变；`NativeDemo.unity` 增加显式宿主与五个敌人、一个 Boss 的引用。`NativeObstacle` 在运行时给既有碰撞物加 `CombatObstacle`，让共享视线与弹丸使用同一父级标记；Tilemap 障碍还需后续地图接线和 S2 物理检查。
- `CombatWorld.EnemyDied` 只来自有效伤害使共享 `CombatEnemy` 生命首次归零。禁用、销毁和卸载只注销实例，不记击杀。共享弹丸的玩家持有者负责旧 HUD `Kills` 读数。

## Boss 结果与后续宿主

- `INativeBossEncounter.TryActivateEncounter()` 返回 KT-02 的 `CombatBoss.ActivationResult`。旧 `ActivateEncounter()` 保留给兼容调用。Native ECA 仅在 `Started`（或已真实激活的 `AlreadyActive`）后提交 `Quest.RecordBossStart()` 和 `Map.LockArena()`；启动失败保持门原状。若同步 Quest 提交意外失败，`CancelUncommittedActivation()` 在 Startup 阶段撤销启动。Map 仍是门的唯一状态拥有者。
- 当前 NativeDemo 的原玩家只绑定此一个 `CombatWorld`。NB-01 的每个新挂载实例须使用各自独立的 `CombatPlayer`/`Rigidbody2D` 与 `RuntimeScope`；不能把旧 NativePlayer 或同一共享 CombatPlayer 同时挂给两个世界。NB-02 负责唯一焦点输入。新关卡的正式 Quest/Narrative 由 NB-03 `IntegratedNarrativeBinding` 在真实 `EncounterCompleted` 后处理；本场景未安装该绑定，Sandbox 与 Integrated 不会并行订阅。

## 已检查与待办

- 隔离 Unity 2021.3.27f1c2 编译和 NativeDemo Play 冒烟通过：共享自动开火击杀 SecurityDrone，`EnemyDied` 一次且 `Kills=1`；探针模拟 Boss 区域事件调用实际 ECA 入口，激活后封门，外壳破裂后核心 E 完成 Quest 并解锁 Map。运行时仅加速目标血量与先决记忆，没有保存探针或运行状态。脱敏日志：`/private/tmp/int-lt-native-smoke-3031f05.log`。
- NB-03 导入后又做一次隔离 Unity 编译，退出码 0，脱敏日志：`/private/tmp/int-lt-nb03-bf456cf.log`。NativeDemo、Packages、ProjectSettings 在该检查前后哈希不变。
- 未测：完整四结局、混合 Chunk 实体通行、多个挂载实例的焦点切换、公开包往返、正式叙事 S4。现有 NativeDemo 冒烟不能代替这些验收。
