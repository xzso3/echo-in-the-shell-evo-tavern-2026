任务标识与尝试次数：P1-00 / 1
任务标题：P1-00 第一阶段文档基线准备
项目 / 主机 / 指定工作区：ECHO IN THE SHELL，local，直接使用保存项目 /Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026，Local。
起始分支与 base_commit：现场读取为 main / a17f8c207af9c1f5119288b8ed64113e1ba8ac58；先复核，再从实际 HEAD 建立 codex/p1-bootstrap；已有同名分支不得覆盖。
目标与完成条件：保护全部现状，建立且仅提交第一阶段文档共享基线，确认 codex/p1-bootstrap 指向交付。
必须阅读：Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md、Docs/FRAMEWORK_ARCHITECTURE.md、Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md、PHASE1_CAPABILITIES.md、PHASE1_ACCEPTANCE.md、PHASE1_TASKS.md、PHASE1_ORCHESTRATOR_PROMPT.md（后三者均位于 Docs/Framework/），及实际适用 AGENTS.md；以上工作树 2026-09-22 文档，重点主控提示词第 1、2、4、6、7 节。主控提示词仅指导本任务角色，你不担任主控。
前置：暂无提交后的设计文档基线，只有现有原型 HEAD。
允许修改和提交：上述 7 份文档与必要 Docs/README.md 索引。允许自身分支本地 Git 操作。不要新增其他交付路径；核查报告可直接在任务消息中提交。
只读与共享负责人：所有游戏代码、资源、项目设置、历史 Boss 文档和项目进度说明只读。Docs/Framework/Orchestration/ 由主控独占，不暂存提交它。
输入及现状：暂无冻结能力、Schema 或资源清单；当前 git status 显示 Boss关卡实现与验证.md、Boss战开发计划.md、项目进度说明.md、Docs/README.md 修改，核心设计和架构及 Docs/Framework/ 未跟踪；需独立核对暂存区及归属。README 仅纳入明确第一阶段索引 hunk，不把历史变化混入。
交付：基线 SHA，精确文档清单和内容摘要，剩余未提交变更及保护方式。禁止无差别暂存、stash、撤销、删除用户变更；无法安全分离则具体报告。
验收：Git 文档基线核查，不运行游戏测试，不把历史结果计为新框架通过。
Unity：本任务不占用，不启动编辑器。
里程碑：首先确认实际起点与改动保护；完成后返回结构化交付。
风险与禁止：不改代码、不升级 Unity、不远端 push、不创建其他任务。
通用要求：先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。你已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建其他任务。缺少输入或需要跨边界修改时说明具体缺口。完成后提供提交 SHA、变更文件、能力或契约变化、实际验证命令及退出结果、报告与视觉证据路径、未执行项和下游影响。不要以静态检查冒充运行通过，也不要因为先遇到一个可自行解决的错误就结束任务。
结束格式：task_id / attempt；result: ready_for_review | needs_input | failed；base_commit / delivery_commit；worktree / branch；changed_files；contract_or_capability_changes；checks（命令、环境、退出结果、状态）；evidence_paths（绝对路径）；known_limitations；downstream_impact。
