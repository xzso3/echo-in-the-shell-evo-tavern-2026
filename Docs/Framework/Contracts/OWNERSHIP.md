# 冻结后的路径所有权

以下是精确的后续写入分工；未创建路径是分配位置，不能当作实现证据。本轮已创建路径以 Git 清单为准。所有模块需提供独立 descriptor，禁止直接改全局注册清单。

| 负责人 | 独占路径与交付 |
|---|---|
| P1-01；验收合入后由主控移交 INT-00 保管，接口修订返还 P1-01 | Assets/EchoFramework/Core/Contracts/、Gameplay/Contracts/；Core/Echo.Framework.Core.asmdef、Gameplay/Echo.Gameplay.asmdef、UnityHost/Echo.UnityHost.asmdef、Editor/Echo.Editor.asmdef；Tools/EchoContent/Contracts/；Assets/EchoFramework/Tests/Contracts/；Tests/EchoFramework/Contracts/；Tests/EchoFramework/Host/*.csproj、Program.cs；global.json、Directory.Build.props、EchoFramework.sln；Docs/Framework/Contracts/ |
| P1-02 | Assets/EchoFramework/Core/Runtime/、Registration/、Eca/、Events/、Scopes/、Time/、Tracing/；Core/Runtime/CoreModuleDescriptor.cs；Tests/EchoFramework/Core/；Assets/EchoFramework/Tests/Core/ |
| P1-03 | Tools/EchoContent/Cli/、Validation/、Runner/、Generation/、Generated/；Tests/EchoFramework/Content/；工具入口工程单独放 Tools/EchoContent/Cli/Echo.Content.Cli.csproj；通过主控申请加入共享 sln |
| P1-04 | Assets/EchoFramework/Gameplay/Entity/、Map/、Spatial/；各目录内 EntityModuleDescriptor.cs / MapModuleDescriptor.cs；Tests/EchoFramework/Entity/、Map/、Spatial/；Assets/EchoFramework/Tests/Map/ |
| P1-05 | Assets/EchoFramework/Gameplay/Actor/、Combat/；各目录内 ActorModuleDescriptor.cs / CombatModuleDescriptor.cs；Tests/EchoFramework/Actor/、Combat/；Assets/EchoFramework/Tests/Combat/；实现共同 IActorQuery/IActorControl 服务 |
| P1-06 | Assets/EchoFramework/Gameplay/Interaction/、Quest/、Dialogue/、Level/；各目录内对应 *ModuleDescriptor.cs；Tests/EchoFramework/Interaction/、Quest/、Dialogue/、Level/；Assets/EchoFramework/Tests/Flow/；仅消费共同 Actor 接口 |
| P1-07 | Assets/EchoFramework/UnityHost/Bootstrap/、Input/、Presentation/、Resources/、Generated/；Assets/EchoFramework/Editor/Import/、Build/；Assets/EchoFramework/Samples/；Assets/EchoFramework/Tests/UnityHost/；Docs/Framework/Resources/；首批稳定资源清单和 Host 生成映射（既有素材只读） |
| P1-08 | ContentPackages/p1.sample_a/（definitions/rules/assets/cases/reports）；额外依赖素材包需主控明确分配，不写共享素材清单 |
| P1-10 | ContentPackages/p1.sample_b/（definitions/rules/assets/cases/reports）；冻结后仅配置生产，不改框架、Schema 或目录 |
| INT-00 | Assets/EchoFramework/Gameplay/Composition/Phase1Composition.cs（唯一模块列表及纯逻辑组装入口）；Docs/Framework/Integration/；集成分支；接收交付后维护共享工程/程序集引用，不让模块并发修改 |
| P1-09 | Tests/EchoFramework/Acceptance/G1G2/；Docs/Framework/Acceptance/G1G2/；Docs/Framework/Freeze/；缺陷返还原模块 |
| P1-11 | Tests/EchoFramework/Acceptance/G3/；Docs/Framework/Acceptance/G3/；Docs/Framework/Delivery/；缺陷返还原模块 |
| 主控 | Docs/Framework/Orchestration/ 及调度/移交记录 |

P1-07 的 Bootstrap 调用 INT-00 的 Phase1Composition；P1-03 的生产运行器也调用同一入口。模块只在自己 descriptor 中注册能力，不直接接入全局文件。Core descriptor 先注册，Gameplay 根据依赖拓扑组装；服务接口在 Contracts，组装不能引入 Unity 依赖。

每个模块负责所属新目录/文件 .meta。Assets/EchoFramework、Core、Gameplay、UnityHost、Editor、Tests 及公共父目录 .meta 已由 P1-01 建立，不重建 GUID。后续共享父目录需要第一次建立时交给 INT-00 串行处理。

P1-01 保留 Editor/Contracts/ 的契约 smoke 适配；P1-07 不把它改成产品工具。Generated 中生产 Schema/目录唯一生成者 P1-03；Host 导入映射唯一生成者 P1-07；冻结摘要记录 P1-09，发布验收记录 P1-11。任何 Packages/ProjectSettings 变更必须先由主控具体分配，本次未修改。
