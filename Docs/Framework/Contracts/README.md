# P1-01 公共契约 0.1.0

本目录冻结工程边界，未宣称完成生产框架。公共 C# 在 `Assets/EchoFramework/Core/Contracts/` 与 `Gameplay/Contracts/`；工具协议在 `Tools/EchoContent/Contracts/`。P1-02—08 依此实现，不各自修改共享签名。变更交还契约负责人，经主控与 INT-00 接受后统一升级。计划事件 Schema 是协议，不是生产注册；当前生产目录为空。

## 编译与运行

要求 .NET SDK 8.0.413（global.json 精确锁定）、Unity 2021.3.27f1c2，不升级 Unity。

```sh
sh Tests/EchoFramework/Contracts/verify.sh
```

脚本从当前 worktree 独立 `Library/PackageCache` 获取 Newtonsoft.Json，缺少时只读本机 Unity 全局包缓存；其他平台可设置 `ECHO_JSON_DLL=/absolute/path/to/13.0.2/Runtime/Newtonsoft.Json.dll`。不从保存项目读取 DLL，不复制供应商 DLL 入仓。离线 restore 使用 SDK 自带 targeting packs，无 NuGet 包下载。沙箱不允许 .NET 本地构建进程通信时需要运行许可；普通终端不需要特殊参数。Core/Gameplay 的 csproj 直接包含 Assets 下同一源码，目标 netstandard2.1，语言 C# 9；外部可执行宿主 net8.0。程序集输出在 Tests/EchoFramework/Host/bin，obj 独立按项目名划分。

分配 Unity 名额并确认没有本 worktree 的运行编辑器后执行：

```sh
ECHO_TESTED_COMMIT="$(git rev-parse HEAD)" \
  /Applications/Unity/Hub/Editor/2021.3.27f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Echo.Editor.Contracts.ContractSmokeEntry.Run \
  -logFile "$PWD/Docs/Framework/Contracts/Evidence/unity.log"
```

入口主动按结果退出，失败返回 1。外部与 Unity 调用同一个 `ContractSmoke.Run`，报告分别生成 `dotnet-report.json` 与 `unity-report.json`。这是 Editor 批处理逻辑 smoke，不是 Play Mode、玩家构建或视觉验收。UnityHost.asmdef 为空骨架，Unity 会给出“no scripts associated”警告；可运行 Host 由 P1-07 填充。Core、Gameplay、契约 smoke、Editor 适配实际编译。

## 类型、引用与结果

ContentId 为 `package:type/local_id`，小写、区分大小写、无 `..` 段；类型化定义引用和运行 InstanceRef 不混用。InstanceRef 的身份包括 run_id、kind、serial、generation，禁止自动换绑；默认结构值无效，不可作为已校验引用。传入边界必须先验证 kind/run/generation，再向所属模块查询存活性。重复本地名字不影响身份。

ScopeRef 仅允许 run/level/encounter/entity/execution；层级固定为 run → level → encounter（可选）→ entity → execution，execution 可直接归属任一仍存活祖先。父级结束先拒绝新工作，取消后代并清理，再失效。事件匹配显式 Exact/Descendants。IScopeService.Contains 在同一作用域也返回 true。跨作用域交接必须显式创建新执行并复制必要快照，不能延长失效引用。

Result<T> 的失败包含 Diagnostic；动作另用 running/succeeded/failed/cancelled。Cancel 幂等、清理有界，终态不再改变。稳定诊断分类见夹具实际代码；后续模块可增加 owner 前缀的错误码，并在描述中声明。

TypedValue 的变量值只允许 boolean/integer/finite number/string/content_id/instance_ref。Object/Array 是不可变事件及动作参数容器，不能声明为变量类型；字段由各能力 Schema 约束。集合构造复制输入。事件信封携带提交快照，时间与 sequence 单调；省略 source/target 时使用 null 的 C# nullable，JSON 省略字段，不能伪造实体。结构化 payload 中坐标等由 Object/Array 表达；定义引用和实例引用保留自己的种类。

序号采用不超过 2^53−1 的正整数，tick 从 0 开始。JSON 使用 snake_case，C# 使用 PascalCase；编码器由 P1-03 显式映射，不能依赖默认序列化推断。enum 按 `DescriptorJson.Wire`，例如 BeforeDamageCommit → before_damage_commit。输入和快照按生成 Schema 映射，快照中的 EntityPresentation.Entity 展平为 entity/definition/scope/map/position；choices 字典在 JSON 中为 choice_id/text 数组。报告 DTO Environment 使用生成 Schema 的字段。

## 时钟、执行与输入

默认固定步 1/60 秒。Step 的顺序：收取按 sequence 排序的输入 → 按组装顺序推进模块/到期动作 → 事实队列与 ECA → 快照和追踪。事件按 sequence、规则按 priority（小值先）、rule ID、绑定实例 ID 排序。持续动作不使用 Unity 时间；SimulationPaused 时模拟 tick/等待不前进，对话仍接收新输入。零等待至少让出下一逻辑步。Presentation 时钟仅供 Host UI，不允许第一阶段 core.wait_time 配置成它。

IR 的 ActionDefinition/ConditionDefinition/ValueBinding/RuleDefinition 是不可变编排 DTO；P1-02 负责解析后语义检查与执行。Flow.Children 保序；leaf 不接受 children；not 恰好一个 child；repeat 有界。BindingSource 只取指定的事实/绑定/变量路径，不执行表达式。Get/条件保持纯查询，动作开始与提交重查。IActionFactory 接收已解析参数；组合调度由 P1-02 读取 ActionDefinition，不另建玩法执行器。

InputIntent 身份为 run_id + input_id；sequence 单调，每次新按键一个 ID。旧局、重复、过时 session/node/revision 拒绝；方向归一化到长度≤1（零允许）。对话开启的 openingInputId 被会话消费，不得继续推进。Start/End 是测试/宿主生命周期 API，不是内容可发出的任意动作。输入 UI 禁用直到预检、实例建立、level.started 和首轮任务激活结束。

## 05/06 并行角色接口

`IActorQuery.Get/CanUse`、`IActorControl.TryAcquire/Release/ReleaseOwner` 和 `IControlLease` 由 P1-05 唯一实现。06 只注入这些接口，不引用 Actor 实现类。Get 对失效实例返回 entity.stale，对存在但死亡的角色返回 Alive=false；Busy 来自仍活动且 MarksBusy 的 lease（后续实际状态可加所属模块原因），Occupied 为活动 lease 通道按位或。

Movement/Attack/Interaction 通道逐位互斥；不同通道可并行，TryAcquire 必须原子全获或不获，不抢占旧 lease。必须检查角色存活、同局、owner scope 存活、非空通道。CanUse 允许同 actor、同 owner 的活动 lease 操作自己的通道，不允许伪造 ID、其它 actor/owner 或额外通道。与其他 lease 冲突返回 false。调用者不能仅因一次 Get 成功而跳过提交重查。

Release 与 Dispose 重复调用成功无副作用；仅移除自己那份占用，未知或已失效 token 不释放别人的 lease。owner scope 结束清理其后代所有占用；死亡先禁止新控制并取消未完成攻击/控制，再提交死亡事实，最后按生命周期移除实体。06 对话取消/关闭在 finally 中释放自己的 lease，不撤销已提交交互。契约 double 只验证消费方式和预期语义，真实状态/死亡/作用域联调在 P1-05/06 与集成任务重跑。

## 空间、资源与历史事实

ISpatialQuery 使用逻辑 XY，连续位置、圆半径、完整位移 sweep；Blocking 三位独立。结果 Fraction 为 [0,1]，无碰撞为 1；法线面向阻挡外侧。FindPath 返回从起点到终点的点列，失败返回诊断，不以空路径假装可达。Revision 在阻挡改变后递增，路径/视线缓存据此失效。MarkerRef 明确 map/chunk/local_id，区域 ref 明确 map/local_id；坐标、N、端口与不支持旋转/镜像继续遵守实现规范。

ResourceEntry 的 ID 使用 `package:resource/local`，type 为 sprite/texture/audio/text/font；source_path 为包内相对路径，禁止绝对路径、反斜杠和父目录逃逸。P1-07 导入映射使用稳定 resource ID + 类型 + 源摘要；GUID/subasset 身份只在生成的 Host 映射内，不进入逻辑内容。源资源重导入同 ID 更新映射，禁止重复身份。当前资源夹具为空清单，没有冻结实际素材、许可或虚构资源。

IFactHistory 保存本局已提交不可变事实，按 sequence 查询 (after, through]。Quest 激活按策略查询匹配作用域的历史快照并按 result_id 去重，不把历史重新 Enqueue，不重做交互、开门或对话。默认 future-only；显式 current-run-history 才追认。P1-02 提供日志，P1-06 实现目标语义，不要求持久化或跨进程恢复。

## Schema、注册与生成

唯一规范源码为 SchemaCatalog/CapabilityDescriptor/DescriptorJson。运行 `verify.sh` 重建 Generated。已包含 package/resources/case/report/input/world_snapshot/descriptor/reference/value 和计划事件 payload。领域内容（地图/Actor/Quest/Dialogue 等）结构随各模块真实注册描述交付，P1-01 不伪造已实现生产定义。

支持的 Draft-07 子集关键字完整列表：`$schema $id title description type properties required additionalProperties items minItems maxItems minLength maxLength minimum maximum enum pattern oneOf default`。type 只接受单个 object/array/string/integer/number/boolean/null；nullable 用 oneOf。additionalProperties 只接受布尔；items 只接受单 schema。未知关键字（包括 $ref/allOf/anyOf/not/if/format/patternProperties/unevaluatedProperties）、未知 dialect、tuple items、boolean schema 均拒绝。`default` 仅注释，不自动补值；业务默认必须加载器显式应用并验证。minLength/maxLength 以 Unicode scalar 数计数。pattern 使用可移植锚定正则子集，执行超时 1 秒；不允许内容提供 Schema，描述由已审查代码提供。

CheckSchema 在验证前递归检查 Schema；即使 oneOf 未选中分支有不支持关键字也拒绝。所有封闭内容对象 additionalProperties=false；描述内 parameter_schema/result_schema 是 schema 对象，注册时必须再次 CheckSchema。JSON 拒绝重复键、注释、单引号、非有限数和尾逗号，最大深度 64。

结构校验不代替语义。ReferenceContract 是元数据检查，覆盖重复、缺失、循环、精确版本/摘要、导出与引用类型；文件枚举、规范化摘要、安全路径解析、实际资源存在性、内容加载事务由 P1-03/P1-07 实现。实例失效检查属于运行时所属模块。

模块提供 IModuleDescriptor，Register 绑定真实 factory/handler/type reader，纯描述不能宣称 action/condition 可用；Event 注册只能由 owner module 发出，内容自定义事件必须带包命名空间且显式声明。Registry 校验唯一 ID+version、owner、scope/context、钩子合法性，Seal 后不再变更。Production 导出只允许 Production 注册；测试模块只允许 ContractFixture。当前 fixture.wait 只有契约探针，无生产 handler，因此 fixture-catalog 用于 Schema 互操作，不能作为已实现执行能力清单。

统一组装由 INT-00 的 Gameplay/Composition/Phase1Composition.cs 接入各真实模块，先拓扑依赖，再 Order，最后 ordinal ModuleId；循环/缺失/重复服务拒绝。模块创建后统一绑定服务，再 Initialize。Unity 与 Tooling 调用相同组合入口；不做反射扫描，不各自维护模块清单。启动先预检所有内容/资源，失败不发布 level.started、不开放输入；后续初始化失败逆序 Dispose 已建实例。

生成的生产 Schema/目录由 P1-03 写 `Tools/EchoContent/Generated/`，Unity 资源导入物由 P1-07 写 `Assets/EchoFramework/UnityHost/Generated/`。本目录 Generated 是契约基线，计划事件与测试夹具明确分离。共享组装/依赖/程序集/宿主项目更改按 OWNERSHIP.md 单一负责人处理。

## 摘要与验收边界

报告 status 仅 passed/failed/not_run/blocked，expected/actual 必填，错误有文件/内容 ID/字段位置。契约输入摘要覆盖 Fixtures；源码摘要按排序相对路径和各文件 SHA-256 聚合，覆盖 Assets/EchoFramework、测试源码/工程、工具契约、global.json/Directory.Build.props/EchoFramework.sln/.gitignore，包含新 .meta，不含 bin/obj/报告和 Generated。源码摘要算法在 ContractSmoke，可跨 worktree 重现。实际内容包摘要由 P1-03 统一实现：UTF-8 ordinal 排序包内相对路径 + SHA-256，覆盖 manifest/definitions/rules/assets/cases，排除 reports 和导入生成物，并记录依赖摘要；禁止把 P1-01 夹具摘要当作 A 包摘要。

A01 完成工具链与契约层；A02—05 仅完成报告中明确列出的契约子例。完整有效 A 包加载、权威事件发布、真实 runtime scope/actor、资源导入幂等和能力运行均需后续实现。A06—A26、Play Mode、画面和原型玩法回归没有在本任务宣称通过。
