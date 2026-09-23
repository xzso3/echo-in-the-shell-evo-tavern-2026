准备完成但尚未派发：用户指定 Fast tier，必须确认生效后创建。本文件中的任务书不得以默认 Standard 速度启动。

任务P1-03 / attempt1；标题P1-03 内容工具与用例运行器。ECHO IN THE SHELL/local/本次新独立worktree。起点codex/p1-integration，base_commit=3d6e77aa728c726140c6ad30a9a4628c355eafec；精确核实后使用codex/p1-03-content-tools。前置P1-01及P1-02 revision2(be7326401d6577e8fd465692b82bdccfea559426)已在该候选集成通过：Core132、契约双宿主80、修复探针均通过；真实Gameplay/Combat/Host未实现。
目标：提供可调用真实能力导出、包校验、通用声明式用例运行和绑定摘要的报告入口；初期用明确ContractFixture验证通用运行器，生产仅当前Core8项；后续Gameplay集成后重新导出复验，不能伪注册计划玩法能力。
允许修改：Tools/EchoContent/Cli/、Validation/、Runner/、Generation/、Generated/，独立CLI工程Tools/EchoContent/Cli/Echo.Content.Cli.csproj；Tests/EchoFramework/Content/（专属csproj/夹具/报告/README）。不改Tools/EchoContent/Contracts或共享Host工程/sln。依赖可在自己工程引用既有Core/Gameplay/Content.Contracts工程。共享Composition尚未形成完整Gameplay世界，通用工具通过注入既有IProductionComposition/ILogicWorld协议消费；别自行维护第二份生产模块清单或把玩法搬入Tools。如需生产入口接线交INT处理，不让Gameplay反向依赖Tooling接口。
重点读取实现规范3/4/5/8，能力1/2/3/4/8，A02—A05、A09、A26全部工具子例，任务卡03，包/资源/报告/声明式用例冻结Schema。
实现与验收：
1真实Registry.Export(Production)生成能力目录/Schema，元信息来自实际handler/type reader，不手写计划目录，稳定排序/摘要，重建幂等。
2包文件枚举/严格JSON/Schema+语义：清单精确版本摘要、重复ID/循环缺失依赖、导出与跨包引用、路径逃逸/资源实际存在及类型、未知字段/能力/上下文/伪造权威事件、结构化ECA加载（五组合/重入/变量/事件绑定/钩子约束）与类型化定义。生产类型依模块描述reader注册，不用字符串猜测。错误定位文件/content_id/字段路径，预检失败无半世界。
3统一包源摘要覆盖manifest/definitions/rules/assets/cases排除reports及导入生成物，精确记录依赖摘要；框架/目录/Schema/工具版本与被测SHA绑定。报告自身变化不得改源摘要。可复现跨目录。
4声明式runner按冻结case协议，标准输入意图、可控固定步/种子、wait_event/max_ticks、expect状态/错误/事件计数顺序/资源归零，预期诊断通过/真实失败/崩溃/not_run分开。MoveToMarker只生成移动意图经过真实世界空间约束，不传送/强设血量任务；当前真实世界未就绪时通过测试世界证明调度消费并明确not_run真实路线，不能用假世界冒充生产Gameplay。
5提供完整可执行CLI命令、诊断负例集、单元fixture和README；逐项映射A02—05/A09/A26工具部分，目录真实引用8Core能力，正例/所有负例/稳定重跑实际验证。
范围：不制作编辑器产品、不制作A/B内容、不实现地图战斗目标、不改正式验收条件。无资源清单时fixture自有测试资源要明确标识，不编造生产资源。正式生产运行组装交接需输出精确API需求给INT。

必读依据：在自己的工作区读取同一base_commit的 Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md、Docs/FRAMEWORK_ARCHITECTURE.md、Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md、PHASE1_CAPABILITIES.md、PHASE1_ACCEPTANCE.md、PHASE1_TASKS.md、PHASE1_ORCHESTRATOR_PROMPT.md；Docs/Framework/Contracts/{README,OWNERSHIP,VERSIONS_LICENSES}.md；Core/Contracts、Gameplay/Contracts、Tools/EchoContent/Contracts与Tests/EchoFramework/Core/README.md及reports/Revision2/DELIVERY.md；实际适用AGENTS.md。文档计划不代表实现，生产目录来自真实注册。
共同只读：所有公共Contracts、既有asmdef、Tests/EchoFramework/Host工程、global.json/Directory.Build.props/EchoFramework.sln、Packages/ProjectSettings、既有原型与资源GUID、其他模块/管理台账。INT独占Gameplay/Composition/Phase1Composition.cs与共享组装/工程；接口修订返P1-01，Core问题返P1-02。不要直接更改共享签名/工程或写其他worktree。新Assets文件必须配.meta，已有GUID不改，不提交缓存。
工具链：公共契约0.1.0、C#9/netstandard2.1纯逻辑、外部net8 SDK8.0.413、Unity2021.3.27f1c2；既有Newtonsoft13.0.2方式复用，不升级Unity或Packages。生产能力与ContractFixture严格分离，测试替身不能作为生产实现/目录。Unity本轮不分配；如确需使用先报告具体工作区/命令/目的，由主控登记名额；禁止控制保存项目用户编辑器。
阶段里程碑：首先报告实际worktree、HEAD精确匹配与干净状态，再创建独立codex工作分支（同名已存在先核查不覆盖）；实现方案与精确接口缺口；实际自测；固定源码提交与完整handoff。主控限制同时2执行任务，不能自行创建任务/子代理。缺口及时通过本任务commentary说明，主控wait读取，别尝试向外发送消息。
通用要求：先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建其他任务。缺少输入或需要跨边界修改时说明场景、所需接口、生命周期、验收例及下游影响。自行可解决错误应修复继续，不在首错终止。没有真实验证不得标pass，不削弱断言，不按样例ID硬编码。
交付结构：task_id/attempt；result ready_for_review|needs_input|failed；base_commit/delivery_commit；worktree/branch；changed_files；contract_or_capability_changes；checks实际命令/环境/退出码及passed/failed/not_run/blocked；evidence_paths绝对路径；known_limitations；downstream_impact。报告绑定实际测试源码SHA和输入/目录/Schema摘要；后继若仅证据提交，提供输入不变证明。完成后保存干净工作树和停止自启验证进程，等待主控/INT接受；不得自称已集成。
