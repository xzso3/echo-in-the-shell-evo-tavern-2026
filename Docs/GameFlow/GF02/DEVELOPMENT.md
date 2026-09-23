# GF02 开发规范：问题修复、平板完整迁移与像素 UI 改进

版本：1.0，2026-09-23。此文档是实施规范；用户已授权启动，执行状态以 [STATUS.md](STATUS.md) 为准。需求依据为 [REQUIREMENTS.md](REQUIREMENTS.md)，不得把设计确认视为代码已经完成。

## 1. 交付目标与边界

一次交付修复后的 CommanderHome → 开局说明 → 局内 HUD → 战术平板 → 返回菜单流程。覆盖 GF02-001～010：相机、主菜单英文图像标题与背景帧动画、CipherWorks 标识、两处主按钮、所有 AI 设置可见性、指定 SDK 对话、离线与故障 fallback、旧手机功能完整迁移、HUD 旧标签移除及第二版设计落地。

保留 GF01 的开始行动门槛、暂停归属、离局清理、结局和结算规则；不借本轮新增存档、商城、全服网络、战斗机制、计分规则、LLM 自动支援或新场景。平板沿用已认可外壳，不重新设计整套。

## 2. 输入基线与代码定位

编写文档时只读观察：主目录 HEAD 为 `3a7390f`；`codex/gf01-integration` 工作树 HEAD 为 `c04f7d9`，路径 `/Users/const/.codex/worktrees/3f6d/echo-in-the-shell-evo-tavern-2026`。主目录尚不包含该工作树中的 GameUI/GameFlow 完整文件。这是观察记录，**不是授权冻结的提交**，启动时由 GF02-INT 复核最新集成状态。

GF02-INT 先建立包含已交付 GF01 的独立集成基线，记录精确提交、工作树、必要未提交输入、场景入口和版本。只将本轮文档及批准参考明确带入基线，不能假定创建工作树会自动复制当前未跟踪的 GF02 文档。用户主目录的 Tilemap、字体、地图和设置改动不覆盖、不丢弃；需要其中输入时做限定范围交接，保留 .meta/GUID。

项目固定 Unity 2021.3.27f1c2、URP 12.1.12；如用外部 .NET，沿用 global.json 的 8.0.413。本轮不升级引擎或渲染管线。

| 模块 | 已观察到的入口；冻结后重新确认 |
| --- | --- |
| 场景与装配 | `Assets/Scenes/CommanderHome.unity`、`Commander/Integration/CommanderHomeBootstrap.cs`、`CommanderRuntimeHost.cs`、`NativeHud.cs`、`NativeRunController.cs` |
| 传输与设置 | `Commander/Transport/CommanderHttpClient.cs` 内含 `ICommanderTransport`；`Commander/Configuration/CommanderSettings.cs` |
| 会话 | `Commander/Conversation/CommanderSession.cs`、`ICommanderSession.cs`、`CommanderResponseParser.cs` |
| 平板 | `PhoneUI/CommanderTabletView.cs`、`CommanderUiFactory.cs`、`Commander/Integration/CommanderTabletRunAdapter.cs` |
| 旧行为参考 | `NativePhone.cs`、`NativeCommanderProxy.cs`、现有 Support / Narrative / ToolkitIntegration 模块 |
| GF01 菜单与开局 | `GameUI/Menu/GameMenuView.cs`、`GameAiSettingsView.cs`、`GameFlowUiFactory.cs`、`GameUiElements.cs`、`GameUiArtCatalog.cs`、`GameUI/Intro/GameIntroView.cs` |
| GF01 HUD | `GameUI/Hud/NativeHudView.cs`、`FlowUiElements.cs` |

上表相对路径均以 `Assets/NativeGame/` 为前缀（已写完整路径者除外）。不得仅在主目录缺失文件时重建同名系统。

## 3. 对话 SDK 与请求链路

### 3.1 选型与依赖

必须采用用户指定的 [RageAgainstThePixel/com.openai.unity](https://github.com/RageAgainstThePixel/com.openai.unity)。2026-09-23 查阅仓库 README：它是非官方 Unity 客户端，声明 Unity 2021.3 LTS 及以上；支持 OpenUPM 安装，Git 方式还需处理其依赖。这个声明不等于已在本工程编译验证。

GF02-NET 选择并固定具体版本及依赖，记录来源、许可、安装方式、自定义 Endpoint/模型映射；不用浮动 latest，不做版本大范围试验或引擎升级。优先复用项目现有 Newtonsoft 与依赖，避免重复程序集。包清单和锁文件由 INT 独占应用。阻断时报告具体依赖和最小可行调整，不静默退回旧 HTTP 实现冒充 SDK 已接入。

### 3.2 最小改造

在现有 `ICommanderTransport` 后接 SDK，保留会话、UI、支援领域边界。可保留 `CommanderHttpClient` 名称作为兼容门面，内部改用 SDK；不要让 UI 引用 SDK 类型。最快路径是非流式完整回复，本轮不增设 Responses/Realtime、语音、工具调用平台或新会话框架。

保留模型 ID、BaseUrl/完整 Endpoint、进程内 Key 与超时设置。按 SDK 的实际 URI 规则做一次转换，避免重复拼接 `/v1` 或 `/chat/completions`。针对用户配置的 OpenAI-compatible 服务，不硬编码官方域名、模型名或强制供应商不支持的额外参数。仅承诺实际使用的服务已验证。

发送流程：输入合法且当前状态允许 → 即时显示玩家消息 → 明确等待状态 → SDK 请求 → 统一结果 → 现有回复解析与提案校验 → 显示回复。发送失败、请求异常、空回复、无效格式均要结束 Busy 并显示明确结果，不允许吞异常后永久空白。

测试连接复用传输适配，使用已应用配置；结果不得写入本局聊天、触发 fallback 对话或生成支援。共享单请求资源被占用时明确反馈“正在通讯”，不能抢占或覆盖原会话。

### 3.3 生命周期与结果语义

复用 `SessionId`、`RequestGeneration` 和当前焦点身份校验。每次请求最多一个有效终态；主动取消、离局、配置更新、焦点失效后的迟到回复一律忽略，不追加本地或在线内容。不用受暂停 Time.timeScale 控制的超时。SDK 异步回调更新 Unity 对象前回主线程。

成功、超时、连接失败、认证/服务错误、无效内容、取消、未配置须可被会话层区分；沿用现有结果枚举，缺项再最小扩展。Busy 拒绝与无效输入不是网络故障。已配置只表示配置存在，健康结果和离线模式分别呈现。

凭据沿用现有内存配置及服务端代理边界；不写入场景、Prefab、ScriptableObject、PlayerPrefs、仓库或日志。SDK 接入不授权在发布客户端嵌入团队服务端密钥。

## 4. fallback 与旧手机完整迁移

只读检查显示旧版 fallback 的核心是**可用的预设通讯主题和本地玩法**，不是任意问题的本地大模型。GF02 必须保留该能力，并在自由消息不可在线处理时显示本地状态摘要及可选主题，标记“本地预设 / 离线回应”，不伪造模型输出。

| 情况 | 目标行为 |
| --- | --- |
| 无配置/无 Key | 不发网络请求；通讯页仍能使用本地主题和允许的本地功能；自由输入发送提供明确本地反馈 |
| 网络失败、超时、认证失败、服务错误 | 当前消息一次性结束等待；原因短句 + 本地 fallback；保留可恢复输入与手动重试路径 |
| 空回复/解析失败/非法提案 | 不执行模型动作；显示格式问题并进入本地回应；不制造支援效果 |
| 主动取消、离局、换局、设置变更使请求失效 | 丢弃请求结果；不触发 fallback，不把旧内容写入新局 |
| Busy、空输入、超长输入、非安全通讯状态 | 明确输入/状态提示，不发网络，不当作离线请求 |
| 服务恢复 | 下次用户发送可再次尝试；不自动无限重试、不重复播放旧消息 |

NET 只负责传输，不生成 fallback；CHAT 是唯一 fallback 决策方，避免网络层和 UI 各追加一次。保留现有安全节点、剧情阶段、合同与支援授权限制，不以“离线可用”为由绕开玩法权限。

CHAT 输出一页旧新功能对照 `TABLET_MIGRATION.md`，边阅读边实现，不作为独立审计阶段阻塞开发。至少逐项覆盖下表，旧入口重命名允许，但功能不能无声消失。

| 旧功能 | 平板目标 |
| --- | --- |
| 任务、记忆、你是谁、关于授权 | 通讯页本地主题入口与内容，在线失败也可读 |
| 自由通讯、草稿、等待/错误、在线回复 | SDK 会话链路、明确状态、可取消/恢复 |
| 保留异议 | 保留可操作入口及前置条件，一次性计入既有规则 |
| 医疗/弱点支援 | 支援页类型选择、合法范围和效果展示 |
| 有限/深度授权、记忆选择 | 原有可用性和成本说明，不改变数值 |
| 查看合同、接受、拒绝/取消 | 实际服务与提案状态接线，防同帧误确认/重复执行 |
| 记忆档案、本局行为、同步/差异 | 记录页真实数据，无数据有说明 |
| 改写初始回声 | 记录页保留入口与一次性规则，明确未向网络发布 |
| 最终写入/销毁、返回、新生通讯接续 | 保持 GF01 当前终局流程，不恢复旧重启冲突 |
| 换页、关闭、Esc、暂停、离局清理 | 与 GF01 状态机保持一致，无重复订阅/旧局残留 |
| Toolkit 焦点/多 Boss 支援适配 | 复用既有适配，不直接写死一个 Boss 或对象 |

原本标为“网络”的信息可迁移到“记录”，但不得声称全服联网。先区分渲染不可见、委托未绑定、无数据和真正缺功能；空状态文案不能代替功能实现。

## 5. 可见性、按钮和默认相机

UI 修复所有设置入口共享的 Viewport；CHAT 检查平板滚动容器同类问题。按实际组件选最小方案：保留 Mask 时保证遮罩图形参与 stencil 写入、隐藏图形用 showMaskGraphic；矩形裁剪适合时使用 RectMask2D。不要同时叠两种方案，也不靠把整个面板涂成不透明色掩盖问题。检查 Content 尺寸、激活状态、CanvasGroup 与文字颜色，原因结论以实际代码为准。

“开始游戏”“开始行动”采用批准的亮字深底方案。按钮背景状态与 TMP 文字色分别控制，处理正常/悬停/键盘焦点/按下/禁用；不保留黑色可用态文字。共用样式先由 UI 定义，HUD 只消费已发布规范或自身局部组件，不并行改工厂。

INT 为 CommanderHome 保存合适的默认 Camera，核对 tag、目标 Display、渲染层与现有 Canvas 模式；配置适配现有 URP，按实际需要保留唯一监听器。启动和返回菜单不重复创建相机。修保存场景与必要的创建入口，不重跑一次性 NativeDemo/Tilemap 构建菜单。

## 6. 美术生产与原生接入

以 [主菜单批准方向](MAIN_MENU_DESIGN_APPROVED.md) 和 [HUD 第二版](HUD_DESIGN_APPROVED.md) 为唯一方向输入，不重新组织风格投票。ART 实际使用内置 image-gen 生成最终必要资源；图像生成不替代原生按钮、文字和状态绑定。

建议资产统一放 `Assets/NativeGame/GameUI/Art/GF02/`，ART 拥有源 PNG、最终图像和 .meta；INT 独占场景与共享资源目录资产的最终绑定。资源交付 `ASSET_MANIFEST.md` 至少写路径、用途、尺寸、透明背景、帧顺序/时长、生成 prompt、是否最终可用、引用建议。

主菜单拆为纯背景、动态雨/水/灯光帧、英文 Logo、CipherWorks 标识；Logo 精确拼写，菜单文字不烘焙。先做少量一致的可循环帧，以 4–8 帧、约4–8 FPS为制作起点而非硬验收指标；现有四格草案不能直接当最终动画，静态建筑不得呼吸漂移。优先局部动态层，避免整屏逐帧重绘引起闪动。UI 实现轻量 Sprite 序列播放，时钟不受游戏暂停影响，停止/禁用能释放订阅；不用视频系统或新增复杂动画依赖。

共享素材没就绪时 UI/HUD 先完成布局和资源槽位，使用现有占位；占位不可计作最终交付。像素图片采用 Point/无 mipmap 等适合现有项目的导入设置，统一逻辑像素尺度；导入与 .meta 保存由 INT 串行完成。中文保持原生可读文本，必要字体字符补充仍由 INT 处理，不能覆盖用户已有字体改动。

HUD 绑定现有生命、任务、开火、用时、击败、同步/差异、装备和平板可用性。隐藏旧标签，去除厚重边框，保留动态意义。右侧橙条先定位用途，不凭草图删除玩法信息。设计图背景变化不授权改关卡。

## 7. 最小接口交接

启动后 INT 发布 `BASELINE_HANDOFF.md` 一页表格：基线、精确文件所有者、保留接口、最小新增签名、资源槽位；这是开工交接，不是评审或验收关口。采用现有签名优先，避免新建通用框架。

| 边界 | 固定约定 | 提供 / 消费 |
| --- | --- | --- |
| 传输 | 保留 SendAsync/Cancel/Busy 和统一结果；SDK 类型不外泄；测试连接独立于游戏会话 | NET → CHAT/UI/INT |
| 会话 | Send/Cancel/Reset、Messages、Draft、Busy、Changed；单一 session/generation 归属 | CHAT → UI/INT |
| 平板绑定 | 通讯、SupportText/Actions、RecordsText/Actions、合同和阶段可用性；缺绑定视为待修复 | CHAT → INT |
| 运行装配 | 服务只初始化一次；重开/离局取消和清理；设置变更保持现有失效语义 | 各工作包 → INT |
| 美术 | 图像路径、尺寸、透明度、帧顺序/时长、状态色；资源目录键由 UI 与 ART 提前约定 | ART → UI/HUD/INT |
| 场景 | 场景路径、Canvas/Camera配置、serialized 引用与 .meta；唯一保存方 | INT |

同一字段/文件的变更需求通过主控协调后由唯一所有者实现；其他任务提交说明或独立补丁文件，不直接越界修改。

## 8. 极致速度策略与交付

按用户要求：不编写/运行单元、集成、契约、EditMode、PlayMode 自动测试；不新建测试 harness、探针、mock服务、截图巡检、CI、独立评审轮次、基准或大范围回归。不设中间人工验收，也不逐项要求截图才允许合入。

正常包解析、Unity导入、C#编译和实际遇到的阻断错误修复属于实现，不能为省时间交付已知编译失败。由 INT 集中执行，避免每个工作树打开 Editor。无变化不重复编译。实现中的定点故障诊断不扩展成测试工程。

全部集成后只保留 [最终人工校验](ACCEPTANCE.md)，由用户或明确指定人员执行。未做标为未做，不声称通过。最终交付记录真实提交、启动场景、资源与 prompt、SDK版本、迁移对照、已知问题、未测范围。无凭据时联网项待用户校验；不能因此虚报或取消 SDK 交付目标。
