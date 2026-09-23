任务标识与尝试次数：P1-02 / 1。
任务标题：P1-02 Core 与 ECA 执行。
项目/主机/worktree：ECHO IN THE SHELL / local / 本次新独立worktree；不得写其他工作区。
起始分支/base_commit：codex/p1-integration / 23b091a528cb525a378c51b6800fb0378687a8aa。首先确认HEAD精确匹配并报告，再建独立codex/p1-02-core，已有同名分支不可覆盖。
目标与完成条件：实现纯C# Core运行，实际能力注册、上下文/变量/作用域/事件/事实历史、ECA五类组合、四种重入、取消失败清理、钩子约束与因果追踪；完成A05—A10及A26核心全部相关子例，交付可复验代码/独立测试入口/报告。不可仅有接口或针对测试硬编码。
必读文件版本：全部以base_commit为准：Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md；Docs/FRAMEWORK_ARCHITECTURE.md（重点3—11）；Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md（尤其3—5及生命周期）、PHASE1_CAPABILITIES.md（1/3/4/5/7/8）、PHASE1_ACCEPTANCE.md、PHASE1_TASKS.md的P1-02、PHASE1_ORCHESTRATOR_PROMPT.md职责/交接；Docs/Framework/Contracts/README.md、OWNERSHIP.md、VERSIONS_LICENSES.md、Evidence/Revision1/DELIVERY.md；Core/Contracts和Gameplay/Contracts公共源码、Tools/EchoContent/Contracts/ToolProtocol.cs；实际适用AGENTS.md。
前置交付：P1-01 revision1 739c37e，INT已集成复验 .NET/Unity各80、静态13、原独立探针11全部通过，可消费23b091a；公共契约0.1.0，Schema摘要3c778f1ffbec9b62de3ebb0e1ab52edc938fed019ce713236d649eb3eef35888。生产能力目录目前为空。
允许修改：Assets/EchoFramework/Core/Runtime/、Registration/、Eca/、Events/、Scopes/、Time/、Tracing/（这些均为Core下子目录），Core/Runtime/CoreModuleDescriptor.cs；Tests/EchoFramework/Core/（可建独立csproj、测试入口、reports、README）；Assets/EchoFramework/Tests/Core/（可有专属asmdef）；这些新文件及目录.meta。只在上述范围实现生产Core和测试，不把测试能力编进生产目录。
只读和共享所有权：Core/Contracts、Gameplay/Contracts、既有asmdef、Tests/EchoFramework/Host/项目和Program、根sln/global.json/Directory.Build.props、契约测试/生成物、Packages/ProjectSettings、原型代码资源均只读。INT-00保管公共工程及唯一Gameplay/Composition/Phase1Composition.cs；主控独占Orchestration。需要共享变更先报告精确需求，不能私改签名。可用独立Tests/EchoFramework/Core项目引用既有Host/Core项目，Core csproj已包含Core源码。
输入契约/能力：消费冻结接口，包括IFactHistory按sequence查询只读已提交事实（Quest后续用，不重播副作用）；注册真实factory/handler并区分Production/ContractFixture。核心能力按计划flow.sequence/parallel_all/repeat、core.wait_time/wait_event/set_variable/emit_event、core.compare等；未知能力/参数/上下文拒绝。不要假注册Gameplay尚未实现能力，伤害hook由Core验证调度约束、Combat后续真实提交。无真实素材需求。
要求实现：稳定事件排序/单线程非递归队列、处理预算与因果链；事件提交payload不可变，模块权威事实与内容自定义事件权限；定义/规则集/执行实例分离；暂停/零等待下一tick；条件纯；queue起执行重查；restart先清理；并行失败取消收束；scope结束子动作/监听/lease清理有界且幂等；失效引用不能换绑；类型化变量和绑定；hook稳定排序/非法持续动作拒绝/异常不产生部分提交。以公共接口注入外部Gameplay服务。
对应验收子例：A05类型/过期引用/跨scope/攻击上下文；A06顺序重复时间事件超时暂停；A07成功失败取消和重复清理计数；A08四重入及容量/条件变化；A09入队次序零等待预算失败因果链；A10 hook顺序、非法持续与异常（独立候选夹具，真实伤害留05联调）；A26固定种子输入轨迹与结束后若干步无监听/动作继续。拆稳定子ID，报告真实passed/failed/not_run/blocked。
交付：CoreModuleDescriptor、实际注册元数据与可消费接口用法、构建和测试命令、输入/源码摘要绑定报告、变更清单、未执行项。测试模块仅ContractFixture不能作为生产导出。P1-03将用实际注册器导出。
Unity名额：当前不分配，纯逻辑可直接执行。若确需Unity兼容验证，向主控报告具体命令/工作区再等占用分配；不要控制保存项目现有用户编辑器。集成会复验受影响Unity编译。
里程碑：起点确认；运行结构/契约缺口；实现+纯逻辑验证；固定源码提交与结构化交付。可自行修复问题继续，不只报告首个错误。
限制：不实现地图Actor交互样例等后续工作，不新建其他task/subagent，不远端push，不删除/归档用户工作，不把静态当运行。
通用要求：先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。你已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建其他任务。缺少输入或需要跨边界修改时说明具体缺口。完成后提供提交SHA、变更文件、能力或契约变化、实际验证命令及退出结果、报告与视觉证据路径、未执行项和下游影响。不要以静态检查冒充运行通过，也不要因为先遇到一个可自行解决的错误就结束任务。
结束结构：task_id/attempt；result ready_for_review|needs_input|failed；base_commit/delivery_commit；worktree/branch；changed_files；contract_or_capability_changes；checks实际命令、环境、退出结果、状态；evidence_paths绝对路径；known_limitations；downstream_impact。报告绑定实际测试源码提交及摘要，后续仅报告提交须证明源码输入未变。
