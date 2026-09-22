# Unity Native — U4 playable source

入口：`Assets/Scenes/NativeDemo.unity`，或菜单 **Echo → Native → Open Playable**。现有场景直接 Play，不重新执行创建/迁移菜单。使用 Unity 2021.3.27f1c2、URP12.1.12、TMP/uGUI、Input Manager。用户手动 Windows 构建；未改共享构建设置、历史场景，未增加独立引擎/服务容器/世界模拟/在线服务或测试框架。

## 实际操作

- WASD 移动；Space 切换自动开火/停火；E 互动/确认当前对白；Tab 打开虚拟手机。手机和对白不暂停，切换页签不重置移动、开火或待确认合同。
- 三段记忆：西南私人记忆、东北系统记录、东南初始回声。中间机器组上方是真实 service path，下方有守卫。三段收齐后回青色 relay，E 打开东侧通路。
- Boss 是开发占位战斗壳，没有正式剧情身份。躲避锁定射线三连射和橙色圈轰炸，外壳破裂后靠近核心按 E。普通窗口 6 秒；错过后 8 秒重新开放。子弹不能直接提交胜利。
- Boss 的真实完成事件开最终门。最终节点 E 打开 **UPLOAD / DESTROY** 两个按钮；必须仍在节点范围内才能提交。按钮不加同步/差异，不覆盖整局形成的象限。
- 非新生显示对应短文本和 Restart。新生白屏等待新的 E 第一拳，随后没有输入也会自主打出最后一拳，黑屏约 2 秒后手机接续；手机仍可读记忆/行为记录和 Restart。

## 三页签与真实支援

手机文字区域可滚轮/拖动阅读；没有文本输入框。

- **COMMS**：任务、记忆、指挥官身份、授权说明和真实本局记录，均为固定本地对白；闲聊不计分。收集私人记忆后可明确选择 **KEEP ANOMALY**，保护被系统摘要删除的异议。
- **SUPPORT**：选择 Medical / Weakpoint、Limited / Deep、已收集记忆，再 Request Contract。合同显示真实收益、授权范围、选中记忆和同步变化。确认使用新的 **Enter 或 ACCEPT 按钮**；不是 E/Space。同帧残留确认被拦截。拒绝无效果无代价；关闭手机保留未确认合同。
- **NETWORK**：明确网络未连接、全局进度未知、没有真实玩家消息。初始回声标为系统离线种子，收集后可明确改写为 `I will carry the contradiction.`；只改本局，不发布。可查看记忆归属/共享、异议保留标记、改写内容、行为记录和计分规则。

Medical 实际回复最多 40 生命；满血、死亡、关卡结束时拒绝。Weakpoint 为真实当前/后续核心窗口增加 3 秒，不破壳、不开窗、不代替决定性 E。两种效果每局各成功一次；Confirm 重新检查当前条件，成功先执行效果与合同终态，再发 Authorized，ECA 才调用 Narrative 记录同步及共享记忆。失败/拒绝不提交授权，重复确认不能重复计分。

## 可达四象限

同步阈值 60；Limited 成功 +20，Deep 成功 +45。一个 Deep + 一个 Limited 达到 65，两个 Limited 共 40。要获得医疗授权必须真的有可治疗伤害；没有效果就不会收费。

差异阈值 60；真实穿过 service path +25，明确保留私人异议 +35，明确改写系统种子 +35，各一次。仅收集强制记忆不加差异；反复点击不累积。绕行只证明真实走过替代路径，不伪称全程零击杀。

| 象限 | 路线示例 |
| --- | --- |
| 归档 Archive：低同步/低差异 | 不用支援或只选低代价；走下方路线，不保留/改写 |
| 逃逸 Escape：低同步/高差异 | 不用支援；保留异议 + 改写种子 = 70 |
| 同化 Assimilation：高同步/低差异 | 有伤后 Deep 医疗 + Limited/Deep 解析；走下方、不保留/改写 |
| 新生 Birth：高同步/高差异 | 两种成功支援中至少一种 Deep；保留+改写，或实际绕行+保留/改写 |

最后销毁/写入只改变本象限内的收束文本与行动记录。计分配置在 NativeNarrative；默认支援两档由真实 Support 提供。当前计分为本局体验配置，不声称是道德或好感评分。

## Unity 组件职责与 ECA

| 职责 | 唯一状态归属 |
| --- | --- |
| Entity / Actor | GameObject 生命周期；NativePlayer/Enemy/BossActor 生命、移动、受伤 |
| Combat | NativeCombat/Projectile 真实索敌、冷却、扫掠弹丸、伤害和击杀 |
| Map | NativeMap 门状态；Collider2D/NativeObstacle 阻挡与视线 |
| Interaction | NativeInteraction 距离、Confirmed、消耗；NativeRegion 实际碰撞进入事件 |
| Quest | 主线阶段/条件，查询 Narrative，不复制记忆/同步/差异/结局 |
| Narrative | NativeNarrative 持有记忆归属、有效行为、共享/同步/差异、结局与新生状态 |
| Support | NativeSupportController 合同、可用性、实际效果、每项成功一次；只发成功授权事件 |
| Dialogue | NativeDialogue 当前会话和确认关闭 |
| Level | NativeRunController Playing/Ending/Completed/Dead、计时、重开 |
| UI | NativeHud 输入；NativePhone 当前页和选择；NativeEndingSequence Canvas/协程演出，不复制故事状态 |
| ECA | NativeEcaRules 订阅事件、查条件、调用所属组件；不保存第二份业务状态 |

真实链路：记忆互动→Narrative 收集；区域进入→绕行记录；relay→Quest/Map；Boss 区域→激活/封门；真实 Defeated→Quest/Narrative/开最终门；Support.Authorized→Narrative 同步/共享；最终有效选择→Narrative 象限提交→Quest/Level。新生第一拳/自主拳/黑屏接续由 Narrative 的顺序状态校验，演出协程负责时间和表现。

## 本轮检查与交接边界

- U3 安全点 `9e2a787` 已交集成；U2 用户曾实测 `66ea557` Windows 基础操作通过。此证据不替代 U4 集中人工试玩。
- U4 接口 `90a3ab9`；主线安全点 `993568c`；Support 原提交 `d24ecee`，本地为 `3f7bebd`。真实 Support 场景接线及编译 `/private/tmp/native-u4-support-connect.log` 退出 0。
- `/private/tmp/native-u4-smoke.log` 退出 0：四象限均到达；默认收集不自动高差异；两个有效选择重复点击不重复加分；手机不改移动/开火/Time.timeScale；满血拒绝/取消无代价；真实治疗与延长窗口成功后 ECA 记录一次；新合同同帧确认拒绝；真实最终核心事件前置；最终按钮不改分；新生无输入等待第一拳、连按不提前第二拳、协程自主拳、黑屏约两秒、手机接续与重开初始状态。
- 上述是临时 Play Mode 组件检查：故事节点/核心用了显式定位，前三象限用 Combat 大额伤害快速布置破壳；新生路线用原数值/实际定时弹丸打破外壳，实际被 Boss 命中后医疗，再成功解析。没有直接伪造授权或 Boss 胜利事件。**不是人工键鼠通关，也不证明五分钟节奏。**
- 已查看通讯、网络、支援合同、三种结果、第一拳白屏、全黑和手机接续的 1280×720 相机截图。待确认合同随后展开文本区并隐藏选择项；只对该可读性修正补充检查 `/private/tmp/native-u4-visual.log`，不重跑整套。
- 初次 U4 场景保存后 Unity FileHasher 等待停滞；只终止本任务批处理 PID，重开确认保存引用完整后完成真实 Support 接线和 Play 检查。未触碰用户编辑器、未清空 Library。
- 临时检查源码保留在 `/private/tmp/native-u4-smoke-source.cs`，不加入 Assets/正式测试体系。移除临时探针后的最终编译 `/private/tmp/native-u4-final-compile.log` 退出 0，无 C# 编译错误。

UI 为英文、既有静态角色图；新生以简单 Canvas 方块拳和裂线表示，尚非精美动画。网络、指挥官模型、手机配对均未接在线服务；行为记录仅属当前运行，重开会清空，未声称跨进程保存或全服提交。完整人工键鼠、滚动手感、约五分钟节奏和最终 Windows 构建由后置集中试玩确认；非阻断表现问题留给该轮反馈。
