任务标识与尝试次数：P1-01 / 1。
任务标题：P1-01 公共契约冻结与工具链验证。
项目 / 主机 / 指定 worktree：ECHO IN THE SHELL / local / 此次独立新建 worktree，禁止写保存项目或其他工作区。
起始分支与 base_commit：codex/p1-integration / 94ccceff3d00e3e929161e2024ca0a2728aa3067。先确认 HEAD 精确匹配并报告起点，再建独立 codex/p1-01-contracts；同名先核实不覆盖。
目标与完成条件：按任务卡 P1-01 完成可编译公共接口、程序集和外部测试宿主、JSON/Schema 最小兼容验证、注册描述与生成约定、合法非法配置夹具和版本/许可证/路径所有权冻结。必须支持 05 与 06 通过共同角色存活/忙碌/控制占用接口并行，不只写说明。
必读版本文件：base_commit 中 Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md；Docs/FRAMEWORK_ARCHITECTURE.md（重点 3—23、32—33）；Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md 全文、PHASE1_CAPABILITIES.md 全文、PHASE1_ACCEPTANCE.md 全文、PHASE1_TASKS.md 第 1/2 节和 P1-01 卡、PHASE1_ORCHESTRATOR_PROMPT.md（职责/交接/范围/验收），及实际适用 AGENTS.md。
前置：文档 fb3376e，INT-00 隔离核查及证据 94cccef 已接受。没有新框架实现。原型仅只读参考。
允许修改具体路径：Assets/EchoFramework/Core/Contracts/；Assets/EchoFramework/Gameplay/Contracts/；Assets/EchoFramework/Core/Echo.Framework.Core.asmdef；Assets/EchoFramework/Gameplay/Echo.Gameplay.asmdef；Assets/EchoFramework/UnityHost/Echo.UnityHost.asmdef；Assets/EchoFramework/Editor/（仅契约 smoke 验证适配和 asmdef）；Assets/EchoFramework/Tests/Contracts/；Tests/EchoFramework/Contracts/；Tests/EchoFramework/Host/；Tools/EchoContent/Contracts/；Docs/Framework/Contracts/；Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md、PHASE1_CAPABILITIES.md、PHASE1_ACCEPTANCE.md（仅工程细节同步，不削弱要求）；上述新 Unity 目录文件必需 .meta 及祖先目录 .meta。根 global.json、Directory.Build.props、EchoFramework.sln、.gitignore（仅新工具产物忽略）可按实际测试宿主需要新增。若必须修改 Packages/manifest.json、packages-lock.json，先给出具体最小依赖需求供主控分配；不得升级 Unity。
只读路径与共享负责人：所有原型代码/资源/场景、ProjectSettings、其他文档只读；Orchestration 主控独占；Integration INT-00 独占。本轮你独占上述新契约、asmdef、外部宿主工程和新规范。最终给出全部后续模块实际路径所有权表，并明确统一启动组装和生成物负责人，后续不各自改共享签名。
输入：候选能力来自计划清单，不可作为已实现生产目录。资源暂无冻结清单；冻结稳定 resource ID/type/manifest 协议，不编造已有资源。参考当前项目现场探测 Unity、dotnet 实际可用性。
交付：公共接口和编译产物验证；ID/事件/结果/时钟/作用域/空间/模块注册/角色查询控制/资源/内容用例/报告/输入与快照协议；规范可生成首批 Schema（标清契约夹具 vs 实际生产能力）；版本与许可证；同一纯逻辑源码双宿主验证入口；未来 module registration descriptors 和统一组装约定；独立检查报告及可复现命令。
验收：A01 工具链与接口静态/逻辑/Unity；A02—A05 契约夹具相关子例（有效/非法字段/引用/作用域/上下文等）。明确哪些完整用例仍由后续实现；不能用测试替身宣称生产能力已实现。
Unity 占用：主控现在将唯一 Unity 验证名额分配给 P1-01。仅在自己 worktree 的独立缓存进行验证；先检查已有运行编辑器，不控制其他项目或用户编辑器。如果连接只能指向保存项目或需关闭用户编辑器，报告具体需求，不擅自更改其他工作区。报告实际引擎版本/可执行路径/退出结果并在交付释放占用。
里程碑：1 起点匹配；2 工具链和目录/共享契约方案；3 实现与实际验证；4 结构化交付提交。缺环境时先完成不受影响检查，再准确报告。
风险及禁止：不要实现 P1-02—08 整套功能，不制作编辑器产品，不新增后续阶段能力，不按样例 ID 特判；禁止远端推送、清理用户变更和自行创建任务或 sub-agent。
通用要求：先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。你已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建其他任务。缺少输入或需要跨边界修改时说明具体缺口。完成后提供提交 SHA、变更文件、能力或契约变化、实际验证命令及退出结果、报告与视觉证据路径、未执行项和下游影响。不要以静态检查冒充运行通过，也不要因为先遇到一个可自行解决的错误就结束任务。
结束结构：task_id / attempt；result: ready_for_review | needs_input | failed；base_commit / delivery_commit；worktree / branch；changed_files；contract_or_capability_changes；checks：实际命令、环境、退出结果、passed/failed/not_run/blocked；evidence_paths（绝对路径）；known_limitations；downstream_impact。报告注明实际测试提交/输入摘要，后续仅报告提交需明确源码摘要未变。
