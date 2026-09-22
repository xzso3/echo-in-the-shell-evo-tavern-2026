> **2026-09-23 已替代：** 本文旧 P1 实施、派发、验收及恢复要求仅留作历史，不再执行；ECA 与系统职责按现行方案选择性保留，不能整体恢复旧要求。当前唯一开发流程为 [Unity 原生交付方案](../UNITY_NATIVE_PLAN.md)；用户最新决策优先。停止独立引擎、双宿主与 G1/G2/G3 新工作，源码和证据保留。

# 第一阶段能力清单与注册契约

更新日期：2026-09-22。版本：0.1。状态：计划清单，尚无实现或实际导出的能力目录。

依据：[实现规范](PHASE1_IMPLEMENTATION_SPEC.md)。验收：[用例清单](PHASE1_ACCEPTANCE.md)。下列 ID 为实现基线，P1-01 核对后冻结；实际目录只能从已注册实现导出，不能把本 Markdown 直接当成运行能力证明。

## 1. 每项注册信息

| 字段 | 要求 |
| --- | --- |
| id / kind / version | 稳定能力 ID、类别和契约版本 |
| parameter_schema / result_schema | 类型、必填项、默认值、单位、合法范围和引用类型 |
| owner_module | 唯一实现及状态所属模块 |
| allowed_scopes / contexts | 允许的生命周期与执行上下文 |
| timing / cancellation | 即时、持续或组合；完成、失败、取消及清理行为 |
| side_effects | 读取和修改哪些领域状态，何时提交 |
| hook_permissions | 能否用于同步钩子及可修改的候选字段 |
| emitted_events / error_codes | 已提交事实与结构化失败原因 |

参数规范同时用于 Schema、能力目录、运行校验与说明生成。对未知 ID、未注册实现和版本不匹配，在内容加载前失败。框架事件由对应模块产生，自定义事件只能使用包内声明的类型与载荷规范。

事件公共信封包含 `event_id`、`run_id`、`tick`、`sequence`、`type`、`source_ref`、`scope_ref`、可选 `target_ref`、`causation_id` 与类型化 `payload`。没有关联实体的事件显式省略相应引用，不能伪造实体来源。

## 2. 内容类型

| 类型 ID | 最小用途 |
| --- | --- |
| tile、static_object | 地形及独立物件外观与阻挡 |
| chunk、map | 区块网格、标记、端口及确定布局 |
| actor、behavior | 角色能力组合及感知、行为状态参数 |
| attack、projectile | 攻击阶段、弹丸运动与命中参数 |
| entity、interaction、door | 终端、门等通用实体与交互状态 |
| encounter、level | 参与者归属、实体布置与关卡条件 |
| quest、objective | 任务及目标跟踪方式 |
| dialogue | 稳定节点、文本、选项和出口 |
| animation_set、presentation、resource | 帧、朝向、基准点、挂点与资源映射 |
| rule_set | ECA 定义、变量、绑定和所需能力 |

类型化引用使用清单中的类型约束；事件、实体、内容定义和资源引用不可混用。定义和运行实例的引用类型也必须区分。

## 3. ECA 组合与核心动作

| ID | 参数概要 | 行为与边界 |
| --- | --- | --- |
| flow.sequence | children | 顺序执行，等待当前动作成功；失败或取消停止后续 |
| flow.parallel_all | children | 同时推进，全部成功才成功；失败时清理其他分支 |
| flow.repeat | count、body | 有界非负整数次数，保留每次迭代上下文 |
| core.wait_time | duration、clock | 逻辑时间等待；取消不再唤醒；零时长至少让出一逻辑步 |
| core.wait_event | event_type、scope、filter、timeout | 等待订阅后的匹配事实，成功返回载荷；超时失败并解除监听 |
| core.set_variable | variable_ref、typed_value | 检查声明、类型与作用域；不能替代模块正式状态 |
| core.emit_event | declared_event_id、payload | 仅发出内容声明的自定义事件，按队列处理 |

规则重入策略为 ignore/restart/queue/parallel，不作为另一个动作树执行器。queue/parallel 声明容量；排队开始前重查条件；restart 等待旧实例清理。追踪需显示触发是否被接受、忽略、排队或拒绝。

## 4. 玩法事件

以下载荷为必需信息概要，完整 JSON Schema 在 P1-01 定义。

| ID | 所属模块／关键载荷 | 用途 |
| --- | --- | --- |
| level.started | Level：level_instance | 激活任务和关卡规则 |
| level.completed / level.failed | Level：result_id、reason | 一次性结果与清理 |
| map.region_entered / map.region_exited | Map：actor_ref、region_ref | 路径与目标触发，按真实跨界产生 |
| map.door_state_changed | Map/门实体：door_ref、old_state、new_state | 状态改变与表现，幂等设置不重复发事实 |
| actor.target_changed | Actor：actor_ref、old_target、new_target | 感知结果变化 |
| actor.decision_requested | Actor：actor_ref、decision_sequence | 按可配置周期评估行为，不在渲染帧逐帧广播 |
| actor.state_changed | Actor：actor_ref、old_state、new_state | 切换状态及其行为流程 |
| actor.died | Combat/Actor：actor_ref、damage_result_id、position | 一次死亡事实，保留后续处理需要的快照 |
| combat.attack_started | Combat：attack_instance、owner、attack_definition | 攻击流程开始 |
| combat.attack_phase_changed | Combat：attack_instance、phase、progress_origin | 同步阶段与表现 |
| combat.attack_finished | Combat：attack_instance、result | 成功、失败或取消的结果 |
| combat.damage_committed | Combat：result_id、source、target、actual_damage | 结算后事实，不是提交前钩子 |
| interaction.completed | Interaction：result_id、actor、target、interaction_id | 一次有效交互结果 |
| dialogue.completed / dialogue.cancelled | Dialogue：session_id、dialogue_ref、reason | 会话结束，与业务结果分开 |
| quest.objective_changed | Quest：quest_instance、objective_id、progress、state | 目标模块提交后的进度 |
| quest.completed / quest.failed | Quest：quest_instance、result_id、reason | 正式任务结果 |

基础 Actor 的目标为空、目标消失和死亡均有明确表示。实体规则默认绑定自身范围；关卡监听其他实体时必须声明范围，不能因复用定义而串到所有同类角色。

## 5. 条件与查询

`all`、`any`、`not` 为纯组合；所有查询必须无副作用，参数可使用事件载荷、绑定上下文与声明变量中的类型化值。

| ID | 输入概要 | 判断 |
| --- | --- | --- |
| core.compare | typed_left、operator、typed_right | 类型合法的相等或数值比较 |
| entity.exists | entity_ref | 指定实例仍存在 |
| actor.is_alive | actor_ref | 当前生命状态允许普通行为 |
| actor.state_is | actor_ref、state_id | 当前行为状态 |
| actor.has_target | actor_ref | 当前存在有效感知目标 |
| actor.target_visible | actor_ref | 当前感知结果及视线允许追踪 |
| actor.target_in_range | actor_ref、range | 当前目标满足距离要求 |
| combat.can_attack | actor_ref、attack_ref | 冷却、目标与角色状态允许开始攻击 |
| interaction.is_available | actor_ref、target_ref、interaction_id | 距离、视线、忙碌和当前条件满足 |
| map.door_state_is | door_ref、state | 当前门状态 |
| quest.state_is | quest_ref、state | 当前任务实例状态 |
| quest.objective_satisfied | quest_ref、objective_id | 目标模块当前结论 |

查询通过不保证稍后命令一定成功，动作必须重新检查执行条件；失效引用返回结构化结果，不能自动换绑到新的对象。

## 6. 玩法动作

| ID | 参数概要／上下文 | 结果及取消 |
| --- | --- | --- |
| actor.set_state | actor_ref、state_id | 校验状态后切换；相同状态不重复进入；清理或交接旧行为 |
| actor.move_to_target | actor_ref、stop_distance | 持续追踪当前合法目标；到达成功，目标失效失败；取消释放本次移动控制 |
| combat.execute_attack | actor_ref、attack_ref、target_ref | 检查后创建攻击实例并启动其 ECA；完成返回攻击结果；取消停止待执行阶段 |
| combat.run_phase | phase、duration；限当前攻击实例 | 战斗模块计时并提供表现进度；取消解除该阶段临时资源 |
| combat.fire_projectile | projectile_ref、socket_id；限当前攻击实例 | 创建关卡拥有的弹丸，固定来源和本次伤害；成功发射不能被普通取消回滚 |
| map.set_door_state | door_ref、state | 提交状态及动态阻挡更新；相同状态幂等 |
| quest.activate | quest_definition、owner_scope | 建立或返回对应任务实例；不能重置已完成实例 |
| dialogue.start | dialogue_ref、speaker_bindings | 创建并等待会话；结束成功，关闭或作用域结束取消 |
| level.request_complete | level_ref | Level 重查必需目标，合法时提交一次结果 |

攻击编排使用 `combat.execute_attack` 进入新的攻击上下文，阶段流中调用 phase/fire 等低层动作；阶段流不能再次调用 execute_attack 形成无界嵌套。Schema 和语义校验共同检查上下文合法性。

伤害由弹丸命中后的战斗结算提交，任务进度由任务跟踪器消费权威事实。第一阶段不向内容开放无校验的“设置生命”“强制任务完成”动作。

## 7. 同步钩子

| ID | 类别 | 契约 |
| --- | --- | --- |
| combat.before_damage_commit | Hook | 读取来源、目标和候选伤害；返回后由 Combat 校验并提交 |
| combat.scale_candidate_damage | Hook-only Action | 有限且非负系数，修改候选伤害；禁止在普通 ECA 流程调用 |

钩子仅允许标注为该阶段合法的即时操作和纯条件。错误参数、持续动作、跨状态写入或异常不得留下部分生命变更。伤害事件只在统一提交后发出。

## 8. 输入命令与显示协议

输入命令与事实事件分开。宿主和用例运行器使用同一命令入口：

| 命令 | 处理规则 |
| --- | --- |
| 移动意图 | 归一化方向，检查角色存活与控制占用 |
| 切换自动开火 | 一次新按键对应一次意图；界面重建不改变该状态 |
| 请求交互 | 指向当前选中的候选及交互 ID；模块复查后执行 |
| 推进／选择／关闭对话 | 关联当前会话与节点或选项 ID；拒绝过时和重复输入 |
| 开始／结束测试关卡 | 由测试入口管理 run/level 作用域，清理旧实例后建立新局 |

显示快照至少提供实体位置、朝向、当前动画语义及攻击阶段进度、门状态、当前任务进度、交互提示、对话会话和关卡结果。表现资源丢失必须诊断；不能通过显示对象存在与否决定攻击是否执行。

## 9. 延后能力与扩展规则

第一阶段不宣称实现任意弹道、完整 Buff/装备、支援事务、记忆与结局计分、在线模型、手机网络、运行中流程跨进程续玩。能力缺口记录使用场景、建议 ID、输入输出、生命周期、失败方式和验收示例。

新增能力必须同时交付实现、注册描述、Schema、示例与行为验证，再进入实际导出目录。变体 B 的生产过程以冻结目录为准，不能在样例中隐藏新增代码能力。

## P1-01 工程契约冻结补充（2026-09-22）

公共契约版本为 `0.1.0`，接口、JSON/Schema 子集、精确工具链、测试入口和路径所有权见 [契约说明](Contracts/README.md)、[所有权](Contracts/OWNERSHIP.md) 与 [版本/许可](Contracts/VERSIONS_LICENSES.md)。基础协议已提供可编译源码；生产能力目录仍为空。计划清单和事件 Schema 不构成已实现生产能力。契约夹具的实际结果单列在 `Contracts/Evidence/`，不替代本文件规定的完整内容、逻辑、Unity 和视觉验收。
