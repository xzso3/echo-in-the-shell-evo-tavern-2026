准备完成但尚未派发：用户指定 Fast tier，必须确认生效后创建。本文件中的任务书不得以默认 Standard 速度启动。

任务P1-04 / attempt1；标题P1-04 Entity 地图与空间能力。ECHO IN THE SHELL/local/本次新独立worktree。起点codex/p1-integration，base_commit=3d6e77aa728c726140c6ad30a9a4628c355eafec；精确核实后使用codex/p1-04-map-spatial。前置P1-01及P1-02 revision2(be7326401d6577e8fd465692b82bdccfea559426)已在该候选集成通过，Core132、双宿主契约80及修复Probe通过。
目标：交付可实际运行的纯C# Entity/Map/Spatial模块、真实注册描述与类型reader、空间/生命周期测试；让05/06消费稳定实体、地图、动态门和查询能力。
允许修改：Assets/EchoFramework/Gameplay/Entity/、Map/、Spatial/（含对应EntityModuleDescriptor.cs/MapModuleDescriptor.cs及新.meta）；Tests/EchoFramework/Entity/、Map/、Spatial/；Assets/EchoFramework/Tests/Map/（专属asmdef及meta）。不要写Actor/Combat/Interaction/Quest/Level、共享Composition或Contracts；独立测试工程可引用既有Host Core/Gameplay工程。
重点依据：实现规范6.1与地图四层，能力类型tile/static_object/chunk/map/entity/door及entity.exists、map.door_state_is、map.set_door_state、map.region_entered/exited与door_state_changed；A11—A13、A22、A26空间/生命周期全部相关子例。Geometry与真实运动查询同一来源。
实现要求：
稳定run/kind/serial/generation身份与精确存活查询、同定义多实例隔离、IEntityQuery/IEntityCommands，scope退出清理及幂等移除；以Core.References精确owner query注册实例，文档说明同Level领域目标合法与scope权能隔离，Entity/Scope可同身份但须模块认证。不得按名字换绑过期引用。
地图加载Tile/StaticObject/Chunk/Map四层；N、Tile尺寸内容参数，XY左下原点、y*N+x，整数Chunk位置；端口半开[start,end)、类型/几何匹配，明确拒绝旋转/镜像。引用/重复/越界/未声明跨接缝/被堵接缝/窄口与圆半径完整检查，关闭和打开门时分别验证必需路径。跨chunk物件单一逻辑身份与全占地查询。
ISpatialQuery/IDynamicBlocking同一静态/动态阻挡来源，movement/projectile/sight三标志独立；圆sweep覆盖完整位移，薄墙高速/接缝/贴边/角点/窄通道/起点贴墙，FindPath支持半径并返回确定不可达诊断，LOS一致，动态改变递增Revision使缓存失效；实际移动要消费查询，禁止仅演示射线代替体积运动。
门状态唯一属于Map，重复设置不重复事实，动态阻挡/路径更新；区域跨越按真实前后位置产生一次enter/exit，MarkerRef保留map/chunk上下文，重名标记不串线。若事件payload/服务接口需要变更先返回精确缺口。
模块提供真实type reader/schema注册及handler，公共接口实现及服务绑定说明；Content工具03并行仅消费描述协议，不直接编辑其代码。descriptor不改全局模块列表，由INT统一组装。
验证：A11所有几何/端口失败，A12三独立flag/门幂等/路径缓存，A13完整位移与半径/跨帧差异；A22只读显示重建保持领域身份/门状态（无Unity部分明确未跑）；A26固定种子轨迹/重复加载结束继续step无旧实体事件；生命周期跨局/失效generation/同定义实例/跨chunk单身份。实际Core组装执行，测试夹具不冒充角色战斗实现；Actor体积与Projectile后续真实联调需留接口说明和not_run。
不新增场景/美术/正式样例、不升级依赖、不迁移原型主循环，允许只读评估旧空间算法并记录复用来源。交付API用法和内容类型Schema供03/05/06消费。

必读依据：在自己的工作区读取同一base_commit的 Docs/ECHO_IN_THE_SHELL_GAME_DESIGN.md、Docs/FRAMEWORK_ARCHITECTURE.md、Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md、PHASE1_CAPABILITIES.md、PHASE1_ACCEPTANCE.md、PHASE1_TASKS.md、PHASE1_ORCHESTRATOR_PROMPT.md；Docs/Framework/Contracts/{README,OWNERSHIP,VERSIONS_LICENSES}.md；Core/Contracts、Gameplay/Contracts、Tools/EchoContent/Contracts与Tests/EchoFramework/Core/README.md及reports/Revision2/DELIVERY.md；实际适用AGENTS.md。文档计划不代表实现，生产目录来自真实注册。
共同只读：所有公共Contracts、既有asmdef、Tests/EchoFramework/Host工程、global.json/Directory.Build.props/EchoFramework.sln、Packages/ProjectSettings、既有原型与资源GUID、其他模块/管理台账。INT独占Gameplay/Composition/Phase1Composition.cs与共享组装/工程；接口修订返P1-01，Core问题返P1-02。不要直接更改共享签名/工程或写其他worktree。新Assets文件必须配.meta，已有GUID不改，不提交缓存。
工具链：公共契约0.1.0、C#9/netstandard2.1纯逻辑、外部net8 SDK8.0.413、Unity2021.3.27f1c2；既有Newtonsoft13.0.2方式复用，不升级Unity或Packages。生产能力与ContractFixture严格分离，测试替身不能作为生产实现/目录。Unity本轮不分配；如确需使用先报告具体工作区/命令/目的，由主控登记名额；禁止控制保存项目用户编辑器。
阶段里程碑：首先报告实际worktree、HEAD精确匹配与干净状态，再创建独立codex工作分支（同名已存在先核查不覆盖）；实现方案与精确接口缺口；实际自测；固定源码提交与完整handoff。主控限制同时2执行任务，不能自行创建任务/子代理。缺口及时通过本任务commentary说明，主控wait读取，别尝试向外发送消息。
通用要求：先核对实际工作区、基线和接口，再在允许范围内完成实现及相应验证。已获授权在自己的工作分支提交本任务修改。不要改其他工作区、集成分支或远端；不要创建其他任务。缺少输入或需要跨边界修改时说明场景、所需接口、生命周期、验收例及下游影响。自行可解决错误应修复继续，不在首错终止。没有真实验证不得标pass，不削弱断言，不按样例ID硬编码。
交付结构：task_id/attempt；result ready_for_review|needs_input|failed；base_commit/delivery_commit；worktree/branch；changed_files；contract_or_capability_changes；checks实际命令/环境/退出码及passed/failed/not_run/blocked；evidence_paths绝对路径；known_limitations；downstream_impact。报告绑定实际测试源码SHA和输入/目录/Schema摘要；后继若仅证据提交，提供输入不变证明。完成后保存干净工作树和停止自启验证进程，等待主控/INT接受；不得自称已集成。
