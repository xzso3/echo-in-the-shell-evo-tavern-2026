# Unity Native — U3 main-line source

2026-09-23，分支 `codex/unity-native-playable`。U1 模块/ECA 安全点 `91ca855`；Boss 最小接口独立提交 `4191c87`。本阶段原地扩展 NativeDemo，保留 Unity 内部模块和实际 ECA；不引入独立引擎、服务容器、Schema、JSON 世界或独立时钟。历史场景、共享构建设置未改。Windows 构建由用户手动执行。

## 入口与操作

打开 `Assets/Scenes/NativeDemo.unity` 后 Play，或 **Echo → Native → Open Playable**。已提交场景不需要运行创建/迁移菜单。使用既有 Unity 2021.3.27f1c2、URP12.1.12、TMP/uGUI、Input Manager。

- WASD 移动；Space 切换自动开火/停火，停火仍会受攻击。
- E 靠近节点互动；对白打开时，下一次 E 仅确认关闭对白。
- Tab 查看已收集记忆目录；Escape/按钮关闭。手机和对白均不暂停世界。
- 三个来源：西南 Private Memory、东北 System Record、东南 System Initial Echo。初始回声明确为离线系统种子，不声称来自真实玩家。
- 中央机器组两侧有实碰撞路径。上方 service path 的 Physics2D 区域记录实际绕行；下方有守卫。不是选择菜单替代移动。
- 收齐三种记忆后回到青色 relay，E 开启东侧 passage。进入 Boss 区域后由真实 Boss 组件接管战斗；靠近暴露核心时 E 调用它的真实交互。
- 真实 `Defeated` 事件开启最终门；最终节点 E 保留三段相互矛盾的记忆，提交一种结局和真实本局摘要。死亡/结局可 Restart Run。

素材直接引用既有 CyberCity 角色、弹丸、地面、网格和材质及 LiberationSans TMP 字体。UI 暂为英文；角色静态帧，旧敌人 Sprite 存在底色残留，普通敌人仅简单绕障。

## 状态归属与规则

| 职责 | 组件与状态 |
| --- | --- |
| Entity | GameObject 引用和 Unity 生命周期 |
| Actor | NativePlayer/NativeEnemy：生命、移动、受伤反馈 |
| Combat | NativeCombat/NativeProjectile：开火、索敌、伤害和击杀；Boss owner 扩展真实 Boss 目标 |
| Map | NativeMap：relay 门、战斗封门、最终门；Collider2D/NativeObstacle 阻挡 |
| Interaction | NativeInteraction：可达性、Confirmed、一次性消耗；NativeRegion：实际触发器进入事件 |
| Quest | NativeQuest：目标阶段和条件，读取 Narrative 的唯一记忆集合 |
| Narrative | NativeNarrative：三种记忆、实际绕行事实、故事记录、一次性结局；NativeMemoryNode 提供场景内容 |
| Dialogue | NativeDialogue：当前会话、对白面板和确认 |
| Level | NativeRunController：Playing/Completed/Dead、计时、启动和重开 |
| UI/Input | NativeHud：输入采样、读取状态、手机目录和结果，不拥有第二份业务状态 |
| ECA | NativeEcaRules：订阅事件、查询条件、按序调用所属组件 |

具体链路：

1. Level.Started → Quest.Activate + Narrative.Begin + 开场对白。
2. Memory.Confirmed → Quest.CanRecover → Narrative.Recover → Interaction.Consume → Dialogue。
3. ServiceRegion.Entered → Narrative.RecordBypass。
4. Relay.Confirmed → 三记忆条件 → Quest.RecordTerminal → Map.OpenExit → Narrative.RecordRelay → Dialogue。
5. BossRegion.Entered → Quest.CanStartBoss + 真实接口存在 → Quest.RecordBossStart → Map.LockArena → Boss.ActivateEncounter。
6. Boss.Defeated → 真实 IsDefeated + Quest 条件 → Quest.RecordBossDefeat → Narrative.RecordBossDefeated → Map.ReleaseArena → Dialogue。
7. Final.Confirmed → Quest.CanLeaveSector → Narrative.CommitEnding → Quest.RecordFinalObjective → Level.Complete。

ECA 在 OnEnable/OnDisable 订阅/解除，重开重载场景。未接 Boss 时明确阻断进度，不记录胜利或结局。

## Boss 集成交接

Boss owner 独占 `Boss/`、`NativeCombat.cs`、`NativeProjectile.cs`，本次没有改这些路径。主线仅依赖 `INativeBossEncounter`（ActivateEncounter、Defeated、CanInteractCore、InteractCore、IsDefeated）。`NativeEcaRules.bossComponent` 需绑定实现接口的真实组件；无需用具体类型替换接口。

场景预留 `World/BossSpawn - real component required`，位置 **(23, 0)**；净空 x=15..31、y=-7..7。入口门 x=14，入口触发区中心 (17,0)，最终门 x=31，最终节点 (33,0)。真实 Boss prefab 放在 (23,0)，连接 player/combat 等 owner 所需引用，最后将组件赋给 ECA.bossComponent。未绑定的主线源分支可验证前半段，但不可通关。

## 检查与边界

- U1 本机 Unity 编译、模块/ECA 冒烟已通过，日志 `/private/tmp/native-u1-final-compile.log`、`native-u1-eca-smoke.log`，安全点 `91ca855`。
- 主控同步：用户已确认集成提交 `66ea557` 的首次 Windows 构建能启动、移动、切换开火、操作终端和重开，无错误。此为 U2 用户实测，不等同 U3 全路线实测。
- U3 场景原地迁移及编译：`/private/tmp/native-u3-connect.log`，退出 0。
- U3 新增路径检查结果随当前交接记录更新；使用临时本地 Play Mode 探针，不新增测试框架。

当前交付范围仅 U3 主线源与真实 Boss 接线点；实际 Boss 合入、完整键鼠通关和约五分钟节奏仍需集成验证。本阶段不做 U4 支援、四象限多结局或新生演出，不声称已有在线服务或真实玩家回声。
