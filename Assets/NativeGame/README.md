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

## Boss 集成

Boss owner 独占 `Boss/`、`NativeCombat.cs`、`NativeProjectile.cs`，已合入 Boss owner 提交 `9def779`（本地 `da4b5f4`），未手改其玩法实现。主线仅依赖 `INativeBossEncounter`（ActivateEncounter、Defeated、CanInteractCore、InteractCore、IsDefeated）。`NativeEcaRules.bossComponent` 已绑定真实 NativeBossController；接口保持不变。

场景 `Actors/Development Boss` 位于 **(23, 0)**；净空 x=15..31、y=-7..7。入口门 x=14，入口触发区中心 (17,0)，最终门 x=31，最终节点 (33,0)。真实 prefab 已生成并绑定 Level，ECA 绑定其组件；保留缺引用时的阻断分支。Boss 仅为开发占位身份，不指认为最终故事角色。

## 检查与边界

- U1 本机 Unity 编译、模块/ECA 冒烟已通过，日志 `/private/tmp/native-u1-final-compile.log`、`native-u1-eca-smoke.log`，安全点 `91ca855`。
- 主控同步：用户已确认集成提交 `66ea557` 的首次 Windows 构建能启动、移动、切换开火、操作终端和重开，无错误。此为 U2 用户实测，不等同 U3 全路线实测。
- U3 场景原地迁移及编译：`/private/tmp/native-u3-connect.log`，退出 0。
- U3 三记忆/relay/物理绕行及缺 Boss 时拒绝结局：`/private/tmp/native-u3-main-smoke.log`，退出 0。对白/手机 1280×720 运行时相机截图已检查可读；场景实际为 Screen Space Overlay。
- 真实 Boss 首次编译、Prefab 生成和场景接线：`/private/tmp/native-u3-boss-connect.log`，退出 0。
- 真实 Boss 新增路径 Play Mode：`/private/tmp/native-u3-boss-smoke.log`，退出 0。休眠→区域激活/封门→真实三连射和圈轰炸→真实弹丸破壳（49.48 秒）→远距核心拒绝→错过窗口后重开→实际物理靠近/核心互动→一次 Defeated→开最终门→单结局→重开全部初始状态。角色实受 12 点攻击伤害，未加无敌、未强制破壳或注入胜利。
- 检查采用故事节点/入口显式定位、脚本驱动物理绕行/核心靠近以及直接组件互动；不是人工键鼠完整通关。49.48 秒是探针持续射击测量，不是代表性玩家战斗时长；不据此改数值。
- 记忆/手机、锁定射线、核心与结局截图位于 `/private/tmp/native-u3-*.png`；已查看。修正旧最终文字标记位置、旧 U1 序列化目标文案，并下移 Boss 世界提示避免页头遮挡；最终编译/场景保存重读日志 `/private/tmp/native-u3-final-compile.log`。
- 使用临时本地 Play Mode 探针，不新增测试框架；探针已从 Assets 移除，源码仅留 `/private/tmp/native-u3-{main,boss}-smoke-source.cs`。

当前交付范围为 U3 三记忆→真实 Boss→最终节点→单结局→重开的主线；完整键鼠通关和约五分钟节奏仍需玩家实测。本阶段不做 U4 支援、四象限多结局或新生演出，不声称已有在线服务或真实玩家回声。
