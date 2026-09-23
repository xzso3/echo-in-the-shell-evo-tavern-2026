正式派发 P1-06 attempt1，标题 P1-06 交互任务与对话。精确 base_commit=d15606c298c6ad1019211d3a5f540e816c727186，起始 codex/p1-integration；地图已验收，公共Actor查询控制契约已冻结。此首段及用户最新设计纠正覆盖下文准备稿和冲突内容。本次创建已临时使用官方service_tier=fast+features.fast_mode=true默认，工具不暴露实际服务回执；不要自行改配置或模型。
本轮仅交付足够开发与联调的简短模块设计，先不写实现、不构建大文档体系。核对HEAD后报告，以Tests/EchoFramework/Level/DEVELOPMENT_DESIGN.md（可引用已有规范）记录：Interaction/Quest/Dialogue/Level职责和状态唯一归属、合法真实输入示例、输出与具体接口、依赖、生命周期、关键失败处理、最小真实联调路线。接口明确已存在声明/已验收实现/本任务待补，不要猜测缺口。完整开发范围及原验收仍以下文为准，但本轮设计安全交付后结束，待主控核对INT跨模块设计再恢复开发。
INT-00正在同轮补跨模块接入设计，负责人ID 01a0c881-9454-7fa3-a276-cb5756debad3；不要直接发消息启动其他任务，缺口由最终交付/主控读取。可只读参考P103工作树3300的Tests/EchoFramework/Content/README.md，交付ca7ea3e16fc64109f18fc0bdfa05a6963b0898fd尚待验收（identity/body+共享rule_set管线）。P105工作树b785检查点e3153bc89755a840924498c74b98d51df97e4174及Tests/EchoFramework/Combat/README.md，尚无descriptor/真实运行，不能作为生产实现验收证据。工作树前缀 /Users/const/.codex/worktrees/，项目目录echo-in-the-shell-evo-tavern-2026。所有其他任务文件只读。
优先最小合法集成包地图加载→真实玩家移动→终端→目标完成→出口开放，然后战斗；小包由INT拥有，不替代A/B，绝不提前B生产。明确如何消费真实Actor与Map、领域事实/一次结果、目标激活/出口校验、Level启动所需Actor Spawn接点及谁负责组装。数据装载必须消费{id,body}和共享Core compiler/RuleSet，不复制ECA。设计引用真实接口代码位置，待补接口指定owner；不用测试替身伪装完成。当前无Unity槽；不要创建任务/subagent。

准备稿：尚未派发，必须先填实已验收地图基线。

P1-06 / attempt1：交互、任务与对话。
目标：实现候选选择/确认重查、一次性终端、目标all/any/依赖与事实历史策略、关卡完成重查、JSON对话会话输入/选择/取消、正式模块事实。
允许修改Assets/EchoFramework/Gameplay/Interaction/、Quest/、Dialogue/、Level/及各descriptor/meta，Tests/EchoFramework/Interaction/、Quest/、Dialogue/、Level/，Assets/EchoFramework/Tests/Flow/及meta。分支codex/p1-06-interaction-quest-dialogue。只消费公共IActorQuery/IActorControl（P105唯一实现），可测试替身但不把它导出为Production或写进产品模块。
验收A18—A22、A26：重复E/旧inputId/过时target或session/越界/LOS/busy确认重查，一次结果唯一ID；目标all/any/前置依赖及激活前事实是否追认按配置，来源过滤、历史只重建状态不重播副作用；抵达出口重查必需目标，未满足不得complete，任务/关卡结果仅一次；对话开始按键不穿透、旧节点/选项/重复输入拒绝，结束取消/scope退出释放控制，关闭不回滚已提交终端；保留逻辑的显示重建不重初始化；结束后step无遗留。真实A/B路线与Unity尚未可用明确not_run，不能用强设目标或传送冒充。
真实interaction/quest/objective/dialogue/encounter/level内容reader、Schema与typed refs；业务状态唯一归属，Map拥有门，只调用其能力/服务不复制door状态。Level配置装配地图、入口、实体/敌人与规则，需Actor创建的跨模块接线给INT说明，不能依赖未冻结具体Actor实现；使用只读Contract或注入自身模块装配接口，缺共享契约交主控。Level.request_complete必须权威查询目标，不开放内容无条件complete。
使用Core IFactHistory本局事实、scope订阅、唯一result_id去重；实体region事实是Actor实体身份、Level订阅Descendants。非暂停短对话租约忙碌/通道可配置且真实释放，所有持久监听/动作/会话计数可审查。统一Composition归INT，不建第二游戏世界。

项目/主机：保存项目 /Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026，local；应用创建的独立worktree。用户指定Fast tier，使用用户已开启的priority默认设置，不覆盖模型或推理参数。起始分支codex/p1-integration；精确base_commit以本次派发首段为准，先核对HEAD一致并确认前置，不一致立即报告不得开发。独立codex/分支，不改集成引用。
必读自己worktree中的Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md、Docs/FRAMEWORK_ARCHITECTURE.md、Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md、PHASE1_CAPABILITIES.md、PHASE1_ACCEPTANCE.md、PHASE1_TASKS.md、PHASE1_ORCHESTRATOR_PROMPT.md及适用AGENTS.md；Docs/Framework/Contracts/{README.md,OWNERSHIP.md,VERSIONS_LICENSES.md,CASE_BINDING.md}、Core和Map测试README。重点实现规范5/6、能力清单2/4/5/6/7/8，逐一对照下述验收族。以base版本接口/注册Schema为准，计划Markdown不是实现证据。
工程输入：纯逻辑netstandard2.1/C#9，外部.NET8.0.413；现有Newtonsoft13.0.2不升级。公共Gameplay/Contracts/World.cs、Host.cs和Core/Contracts只读；case独立0.2.0，其余协议0.1.0。Map真实EntityModule/MapModule/ISpatialQuery已经验收后才创建本任务，精确提交见首段。使用Map README的SpawnBody、真实circle Sweep与认证同Level实体身份；不得创建独立地图副本或传送式移动。新Assets文件配meta，不改既有GUID。
共享文件：公共契约、asmdef、Host/*.csproj、sln、全局配置和唯一Gameplay/Composition/Phase1Composition.cs只由INT保管。需要改共享接口时返回场景/输入输出/生命周期/验收例，主控交契约任务，不在本分支改签名。每模块独立descriptor，实际handler和reader才可注册Production。内容Schema/typed refs提取/模块构造顺序/服务绑定要写明确README，P103据此加载工具、INT据此统一组装。需要自有测试csproj可放自己测试目录，优先引用共享Host工程，不动共享工程。
交付包含实现、实际生产能力与Schema、正负用例、模块启动/更新/结束和Host只读状态接口；资源清理计数可观察，不能硬编码0。固定source commit运行测试，后继仅证据时提供源码/输入/Schema/目录hash证明。实际命令退出结果、not_run和错误诊断如实保留，不把替身或纯逻辑结果冒充Unity。正式A/B与资源清单尚未可用，不编造包ID或假资源，不扩展后续装备/Buff/在线功能。
本轮无Unity槽，可执行纯逻辑测试；确需Unity先报告，只有主控分配后在自己的worktree操作，不控制保存项目用户Editor。里程碑：精确起点确认；跨模块接口/API和自有reader交接；实现和实际验收；固定交付。不要每一步停下来等批准，继续所有授权可执行项。
先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。你已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建任务或subagent。缺少输入或需要跨边界修改时说明具体缺口。不要因为先遇到可自行解决的错误就结束。完成输出task_id/attempt,result ready_for_review|needs_input|failed,base_commit/delivery_commit,worktree/branch,changed_files,contract_or_capability_changes,checks实际命令环境退出passed/failed/not_run/blocked,evidence_paths绝对路径,known_limitations,downstream_impact。

P105已确认冻结Actor共享服务语义：ActorModule唯一绑定IActorQuery/IActorControl/IActorCommands，CombatModule绑定ICombatCommands；控制通道原子互斥，不同通道可并行；死亡立即Alive=false拒新控制，先取消攻击/控制，再actor.died，延后实体移除供事实处理；owner结束释放租约；重复Release/Dispose不释放别人的占用。你按这些冻结接口可先隔离开发，并尽早通过INT做真实联调。
用户最新持续优先级：真实生产正常流程第一，阻断正常使用缺陷第二，关键玩法异常第三，极端强化最后。原Axx/G1G2G3不降低，保留已发现最小复现；暂停主动极端数值矩阵、无触发内部畸形、等价排列、无变化重复全量。缺陷修复后最小复现+受影响回归+必要真实接点通过即推进。证据一次固定源码/命令/结果/输入摘要，仅说明变化不重跑无关测试。优先让正常交互→一次终端结果→任务目标→出口完成跑通，声明必要Actor替身边界，真Actor联调由INT跟进。
