# GF02 主控与并行执行安排

本文件为 GF02 已启动批次的执行边界；实际进度见 [STATUS.md](STATUS.md)。采用用户指定的 [接续会话并主控推进](codex://threads/01a0ca99-e997-7733-9a45-d4ee7b146ff8) 模式，并延续 GF01 主控规范：**主控管文档、派发、协调和交付核对，独立 Sol task thread 执行，INT 串行集成。禁止 subagent / spawn_agent / 嵌套代理。**

## 1. 角色与模型

- 主控：创建新的 GF02 独立任务，维护 STATUS、收集提交、解决归属与依赖；不亲自写游戏代码、美术或操作 Unity。
- 执行任务：统一 `gpt-6-sol`，建议沿用既有主控配置 `thinking=xhigh`。实际任务工具支持什么就如实设置，不伪造 service_tier 等未暴露字段；若可配置 priority 则沿用，没有入口不阻塞开发。
- INT：独立 Sol 任务，负责基线、共享装配、依赖、场景、Unity和集成分支。所有执行任务禁止继续创建子任务；新增分工只能回报主控。
- 并发：默认五条生产线 NET / CHAT / UI / ART / HUD 加一条 INT，共最多六个执行 task；这是工作拆分建议，不是平台保证。如可用配额较少按关键路径启动，不套用 subagent 的并发槽位来定义独立任务数量。

## 2. 工作包与需求映射

需求编号 GF02-001～010保持不变；并行工作包使用语义后缀，避免把一个需求拆成多个相互覆盖的所有者。

| 工作包 | 主责需求 | 实际交付 |
| --- | --- | --- |
| GF02-INT | 001；全体最终装配 | 基线、集成分支、相机、共享组件装配、场景与依赖、最终交付 |
| GF02-NET | 006传输部分 | 指定SDK适配、依赖清单、URI/错误/超时/取消/测试连接映射 |
| GF02-CHAT | 006会话显示、007、008 | fallback、平板完整迁移、合同/记录/本地入口，迁移对照 |
| GF02-UI | 003、004、005；002界面接入 | 菜单/设置/开局按钮、动画播放器、图像标题与队伍Logo槽位 |
| GF02-ART | 002图像；010必要像素资源 | ImageGen最终分层图像、循环帧、Logo、必要HUD图标及清单 |
| GF02-HUD | 009、010 | 轻量HUD与动态数据、提示和旧标签替代装配说明 |

## 3. 波次与关键路径

| 波次 | 并行安排 | 推进条件 |
| --- | --- | --- |
| A 启动 | INT确认基线和归属；ART可同时读取文档设计分层资产；其他线只读定位 | 不等完整审计；只冻结必须共享的接口 |
| B 生产 | NET做SDK；CHAT做旧新映射、本地主题和绑定；UI修显示/按钮并搭菜单；HUD搭布局；ART生成最终资源 | 所有代码任务都从同一冻结基线开工 |
| C 滚动合入 | INT先合UI显示修复和NET依赖，再CHAT会话/功能，再菜单图像与HUD；已完成的文件随到随合 | 只消费明确提交和交接，不要求中间测试/截图验收 |
| D 收口 | INT完成共享场景/引用及一次必要导入编译，修编译阻断；主控交最终人工清单 | 不等待未授权的测试体系，未测如实记录 |

两条主要依赖链：NET适配 → CHAT在线接线 → INT；ART资源 → UI菜单与HUD资源绑定 → INT。CHAT在NET未完成时直接基于现有接口实现本地功能，不空等；UI/HUD用槽位先做布局，不空等图像。ART先交纯背景与Logo，再循环帧与细图标，避免一批大图全部完成才交。

若只能三条生产线并发：优先NET、ART、UI；NET完成后转CHAT，UI完成后转HUD；INT用最短时段做基线和滚动合入。复用当前GF02任务顺序补做可以，不能回旧GF01线程续派。不要给出无依据的固定工时承诺。

## 4. 文件所有权（启动时按实际文件细化）

所有路径以下默认在 `Assets/NativeGame/`。

| 唯一写入方 | 可写 | 禁止直接修改 |
| --- | --- | --- |
| NET | `Commander/Transport/`；新增SDK适配文件 | 会话、配置持久化、平板、场景；Packages只交变更建议 |
| CHAT | `Commander/Conversation/`、`PhoneUI/CommanderTabletView.cs`、`PhoneUI/CommanderUiFactory.cs`、`Commander/Integration/CommanderTabletRunAdapter.cs`；自身新增局部适配 | 传输、GameUI/Menu、NativePhone/Run/Hud、共享Support/Narrative规则 |
| UI | `GameUI/Menu/`、`GameUI/Intro/`；新增菜单动画组件 | 平板View/工厂、HUD、场景、共享资源资产 |
| HUD | `GameUI/Hud/`及自身新显示组件 | NativeHud/NativeRun、Menu工厂、场景、玩法规则 |
| ART | `GameUI/Art/GF02/`与自身资源清单、prompt | 代码、场景、全局导入设置、共享资源目录资产 |
| INT | `NativeHud.cs`、`NativeRunController.cs`、`NativePhone.cs`、`CommanderRuntimeHost.cs`、`CommanderHomeBootstrap.cs`、`CommanderSettings.cs`、`GameUI/GameFlowSettingsAdapter.cs`、Support/Narrative/Toolkit共享接线、所有场景/Prefab、Packages、ProjectSettings、共享Resources与必要asmdef | 不无理由重写工作包内部实现；不覆盖用户未提交资产 |
| 主控 | GF02规格、任务、状态、交付索引 | 游戏代码、美术生产、Unity和执行任务内部实现 |

ART图像 .meta 需要 Editor 保存时交INT处理后回报，ART不要在另一Editor里重导入。UI所有的GameUiArtCatalog代码与INT所有的序列化资源分开写。旧NativePhone仅供CHAT读取，若确需修改，由INT应用最小接线；这样不与最终HUD装配抢文件。

## 5. 最小交接与合入

每个任务提前报告接口变化、允许文件与共享补丁需求；无需长设计报告。交付格式：工作包ID / threadId / hostId / 精确输入 / 提交 / 文件清单 / 入口或调用方式 / 依赖 / 阻断 / 未测范围。

任务提交只含自己的文件和.meta，不提交Library、Temp、生成缓存或凭据。以工作包/功能小批提交便于滚动cherry-pick；不混入其他任务工作。冲突由INT按所有权交原作者修，主控不手动拼共享场景。共享文件补丁作为说明/patch交INT，不能拿整个旧文件覆盖集成版。

集成分支默认 `codex/gf02-integration`，新建前先核实是否已存在本批次分支；不要重复建平行集成头。子任务命名 `codex/gf02-net` 等，使用精确基线。只采用明确新task，不启动旧任务或自动归档历史任务。

Unity始终单一占用者INT。其他任务用源码和资源文件交付，不开多个Editor，不对同一checkout启动batchmode。用户Editor占用时先确认当前用途，通过主控协调可用时段，不强关、不重跑创建菜单；不受影响的源码工作继续。

## 6. 主控调度纪律

后续授权启动后用 create_thread 创建独立任务；先 list_projects 确认项目。Git项目默认独立worktree，并明确传入已冻结分支/引用；创建默认分支并不会自动得到GF01基线或当前未提交文档。若工具无法从指定基线创建，由INT提供明确的后续切换步骤后再动代码。

记录实际threadId、hostId、工作树、提交和当前状态；等待用 wait_threads 的游标方式，避免反复读全量历史。只在进度、依赖、阻断或完成变化时更新，不刷相同状态。卡在SDK版本不应阻塞UI/ART/HUD，卡在图像不应阻塞会话。需用户决定的只升级实际范围/不兼容问题，常规实现不反复询问。

所有工作包均不创建subagent，不调用spawn_agent，不自行create_thread。执行主体只有主控创建的独立Sol任务。主控自身也不使用subagent代替这套模式。
