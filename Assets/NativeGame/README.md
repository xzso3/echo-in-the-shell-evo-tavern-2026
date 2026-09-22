# Unity Native U1 — first playable source

2026-09-23，按用户最新修订保留 Unity 内部模块化玩法与实际 ECA，取消独立引擎/适配层和旧 P1 流程。基线 `2378b25c14a74691474df4d656294b9d65b46714`；开发分支 `codex/unity-native-playable`。首次原生安全点 `988b163` 已保留；随后原地拆分职责并接入 ECA，未重建 NativeDemo。历史资源/场景/证据、共享构建设置未修改。没有执行 Windows 构建、安装模块或新增打包体系。

## 入口与手动打包

打开 `Assets/Scenes/NativeDemo.unity`，点击 Play。也可用菜单 **Echo → Native → Open Playable**。已有场景直接打开，不用重新执行 Create First Playable。用户手动构建时将 NativeDemo 作为首个/唯一勾选场景；使用既有 Unity 2021.3.27f1c2、URP12.1.12、TMP/uGUI，原生 Input Manager（现有 activeInputHandler=0）。不改 Player/Build Settings；具体 Windows 配置由用户设置。

必要素材由场景/Prefab 直接引用 Assets/CyberCity 的角色、弹丸、地面纹理、道具网格和材质，以及既有 LiberationSans TMP 字体。NativeGame 内保存三个具体 Prefab、地面裁片及墙体标记；没有运行时资源 ID/JSON/独立世界依赖。

## 首版操作和内容

- WASD 移动；Space 切换自动开火/停火，右上 HUD 常显状态。停火仍受敌人攻击。
- E 靠近青色终端触发真实 ECA 链：Quest 记录目标→Map 开门→Dialogue 提示；下一次 E 或确认按钮关闭对白。再去东侧出口按 E，ECA 检查 Quest 后由 Level 显示首个可玩检查点结果。中间机器组有实碰撞，可走上下绕行。
- Tab 打开/关闭手机占位，不暂停；Escape 或手机按钮关闭。没有文本输入框；输入路由已处理今后文本焦点屏蔽。
- 四个基础追击敌人、自动索敌与实体墙视线、Physics2D 扫掠弹丸、接触伤害；死亡/检查点结束均可点击 Restart Run。
- 具体玩法状态按下表归属；数值、引用和目标文案可在 Inspector 修改。重开重载当前场景。未接入旧 PlagueGame、旧 Tab 地图/E 背包/F 对话。

## Unity 内部模块与实际 ECA

| 职责 | 唯一状态与组件 |
| --- | --- |
| Entity | GameObject 引用、启用/销毁生命周期，无第二套实体世界 |
| Actor | NativePlayer/NativeEnemy：生命、存活、移动、受伤反馈 |
| Combat | NativeCombat：开火、索敌、冷却、伤害调用、击杀；NativeProjectile：Unity物理扫掠与弹丸寿命 |
| Map | NativeMap：门开闭；NativeObstacle/Collider2D：阻挡与视线 |
| Interaction | NativeInteraction：距离、候选选择、一次性消耗、Confirmed事件 |
| Quest | NativeQuest：目标激活/完成及纯条件查询 |
| Dialogue | NativeDialogue：当前会话/序号、对白面板、确认关闭 |
| Level | NativeRunController：Starting/Playing/Completed/Dead、局时间显示、启动与重开 |
| UI/Input | NativeHud：集中采样输入，读取上列状态，无第二份任务/开火/门状态 |
| ECA | NativeEcaRules：订阅/解除事件、查询条件、顺序调用业务组件，不保存业务状态 |

已读旧 EcaCompiler/CoreRuntime，后者依赖 CapabilityRegistry、ServiceContainer、Scope、SimulationClock 等独立世界组装，直接复用反而扩大本轮范围。这里只增加本关具体规则组件：Level.Started→Quest.Activate；Interaction.Confirmed→Quest.CanRecordTerminal→Quest.RecordTerminal→Map.OpenExit→Dialogue.Show；Exit.Confirmed→Quest.CanLeaveSector→Level.Complete。条件失败不完成关卡，出口可给出未完成提示。移动和逐帧战斗保持 Unity 原生。各组件直接引用，未添加服务容器/Schema/JSON或独立时钟。ECA 在 OnEnable 订阅、OnDisable 解除；重开重载各模块。

## 实际检查

1. `unity run /Users/const/.codex/worktrees/4113/echo-in-the-shell-evo-tavern-2026 --editor-version 2021.3.27f1c2 --timeout 600 -- -nographics -executeMethod Echo.NativeGame.Editor.NativeDemoBuilder.Create -logFile /private/tmp/native-u1-create.log`：退出0；Unity真实编译并生成场景，确认URP。
2. 指定同一 worktree 的 Unity 2021.3 图形 Play Mode 临时组件冒烟：最终 `/private/tmp/native-u1-smoke-final.log`，退出0。检查启动/自动开火、没有旧PlagueGame、Rigidbody2D移动及中央墙阻挡、停火/手机不改Time.timeScale、远距交互拒绝、近距终端开门、重复互动、出口结果、Restart按钮回调重载干净场景、伤害导致死亡面板。只做开发关键路径，没有全量回归或测试体系；临时探针已移除。
3. `Unity -batchmode -quit -nographics -projectPath <4113> -executeMethod Echo.NativeGame.Editor.NativeDemoBuilder.ConnectSceneEca -logFile /private/tmp/native-u1-eca-connect.log`：退出0，原地保存模块和规则引用。随后针对新增ECA的临时Play Mode冒烟（`-executeMethod NativeEcaSmokeTemp.Begin`，完整临时源码保存在 `/private/tmp/native-u1-eca-smoke-source.cs`），`/private/tmp/native-u1-eca-smoke.log`退出0：Level启动激活Quest、Combat实际自动攻击/击杀、互动事件→Quest→Map→Dialogue、对白确认关闭、出口规则→Level完成、重开后状态/订阅复建、Combat伤害→Actor死亡→Level终态。探针已移除。最后仅补齐最新方案要求的出口条件失败对白，并移除探针后做最终Unity编译 `/private/tmp/native-u1-final-compile.log`退出0、无C#编译错误；该失败对白分支未额外运行验证。
4. 最终ECA截图 `/private/tmp/native-u1-eca-phone.png`、`native-u1-eca-dialogue.png`、`native-u1-eca-result.png`；早期截图 `/private/tmp/native-u1-start.png`、`native-u1-phone.png`、`native-u1-result.png`、`native-u1-death.png`。1280×720运行时相机渲染，临时将Canvas转为相机模式用于截图；实际场景为Screen Space Overlay。已检查文字、停火提示及手机关闭按钮可见。

冒烟通过直接调用组件与按钮回调检查；终端/出口部分显式摆放玩家检查距离条件，不作为真实玩家通关路线。键鼠实际输入、鼠标点击、Windows编译/启动、分辨率适配和完整通关**未测**。临时探针首轮定位同步失败已修正，仅探针修改；不把它当生产代码故障。旧空UnityHost asmdef警告和本机包/许可日志不作为原生玩法通过证据。未控制用户BossArena编辑器；连接信息不明确的MCP未用于操作。

## 当前限制与下一步

U1只达到可供用户手动打包的最小场景，不是五分钟主线完成。当前UI为英文（复用既有字体）；角色静态帧，部分旧敌人Sprite有底色残留，敌人绕障仅短距离探测。Boss、三段记忆、支援、四象限结局及新生演出尚未实现。不新增在线服务或宣布旧G1/G2/G3通过。

U3建议用现有 ECA 把终端/出口检查点接成记忆→Boss→最终节点→一种结局；Narrative 自己持有记忆/同步/差异，ECA只调用其记录方法。继续复用现有素材，保留停火/手机不暂停。可并行独占目录：`Assets/NativeGame/Boss/`（Boss具体组件和Prefab，由新分工owner负责）；本任务保留`NativeRunController.cs`、`NativePlayer.cs`、`NativeInteraction.cs`、`NativeCamera.cs`、NativeDemo场景、现有Prefabs和HUD。Boss只暴露具体开始/结束事件与只读存活/阶段，攻击由Boss/Combat组件处理；由NativeEcaRules订阅其完成事件衔接主线。场景与规则接线由主线owner串行修改，避免两人改同一场景。主线owner保留现有 NativeEcaRules/Quest/Map/Dialogue/Hud，并可新增 Narrative 具体组件，不把记忆计分放回巨型Run。此次没有创建其他任务。
