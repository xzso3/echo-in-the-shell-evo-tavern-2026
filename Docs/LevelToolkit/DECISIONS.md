# 关卡工具包技术决定与证据

更新：2026-09-23 12:16（UTC+8）。本文件汇总 W0 只读核验、KT-01 v0 和当前工程决策。**部分技术路线已冻结供实现，实体通行与包往返仍待必要检查。** 需求文档的产品参数未被擅自更改；用户已要求直接放行实现任务，故未验证项以可配置实现继续，不冒称检查通过。当前波次已按用户要求落安全点并暂停后续派发。

| 项 | 当前结论／实现约束 | 证据与状态 |
| --- | --- | --- |
| 格子与 1 格净空 | 保留需求候选 **1 格＝1 世界单位、默认 1 格道路接口**；地图数据用整数格，世界格尺寸在宿主显式配置，暂不硬编码为已验证可通行。若 S2 实体角色失败，先报告对 N=3、素材比例、相机和战斗手感的影响，再请用户决定是否改规格。 | W0-A/B 读取保存 Player 半径 0.32、速度 4.5；理论 1 格通道两侧总余量 0.36，居中每侧 0.18。W0-B 隔离 Unity 探针在 LicenseClient IPC 阶段退出 199，无 Play/XML 结果；直角和混合接缝**未验证**。 |
| 标准角色和模板 | 标准角色从保存资产快照追踪，不允许作品改其能力。普通敌人首版为 Chase 接触与 Orbit/FanAttack 远程，Boss 保留既有破壳→核心 E 机制；共享战斗行为唯一来源，Native 留兼容组件与 GUID。 | W0-A 已读取 Player/Combat、SecurityDrone、ArcSentry、DevelopmentBoss Prefab 真实数值和 GUID；KT-02 应从资产提取，不取代码默认字段。Boss 依赖旧 BossArenaConfig/Visual，提取前检查独立依赖和美术许可。 |
| 程序集与依赖 | 公共 `Echo.LevelToolkit.Runtime` asmdef 不依赖默认 NativeGame、PlagueSurvivor、CyberCity 或 UnityEditor；Editor asmdef 单向引用 Runtime。Native 适配器依赖 Runtime，保持原组件 GUID，禁止整个 NativeGame 一次性转 asmdef。 | KT-01 `0343e59` 已给出 Foundation v0 与无额外引用的 Runtime asmdef，静态依赖/meta/GUID 核对通过；Unity 编译 S1 尚未运行。仓库版本为 Unity 2021.3.27f1c2、URP 12.1.12、Tilemap 1.0.0、Tilemap Extras 2.2.5、TMP 3.0.6；独立工程 UPM 可用性待 S3。 |
| 地图与区域 | Chunk 正方形 3～32，四边端口按北南左→右、东西下→上使用半开区间 `[offset,end)`，整数格摆放，Prefab 不旋转/镜像。区域默认 PolygonCollider2D trigger，矩形是创建捷径；互不重叠。 | 需求和开发方案；KT-01 v0 已实现端口与内容元数据。W0-B 确认旧 NorthRouteChunk 是 SpriteRenderer/BoxCollider 原型，不能代替 Tilemap。区域几何是工程默认，待首个纵向切片核对。 |
| 物理净空与障碍 | 校验必须分离格子拓扑和标准玩家圆体积的连续物理通行；大端口可由多个小接口不重叠分段覆盖，未覆盖段需实体封闭。TilemapCollider 的玩家碰撞、敌人视线、弹丸与预警使用统一障碍识别；不声称现有短触须绕障是完整寻路。 | W0-B 发现 NativeObstacle 只查命中 collider 同对象，Projectile/预警查父级，存在 Tilemap 层级不一致风险；旧 NativeMap 只查两个北侧端点 0.02 距离。KT-04 与共享战斗接线须统一，物理验证后置到 S2 最短路径。 |
| 门状态与激活顺序 | **Map 是门的唯一状态拥有者**；首版同一门只由一个遭遇封门，剧情许可不得绕过未解除的战斗锁。先验证并取得 Boss/遭遇实际激活成功，再提交 Active 与封门；失败不提交已开始，门保持原状态或回滚。目标完成只来自真实死亡/核心 E 事件，销毁、禁用、卸载不算击杀。 | 需求、开发方案；W0-A 静态确认旧 ECA 先记 Boss 开始/关门再调用 `void ActivateEncounter`，缺依赖可能锁死；NativeEnemy 仅 Destroy，无死亡事件。KT-02/05 必须修正共享成功契约。 |
| 稳定内容与运行身份 | 持久 ID 为 `author/work/local`，端点移动或重导入不自动换 ID；每局独立 RunId、每次挂载独立 LevelInstanceId，事件和动作均携带 Run＋实例＋端点，内容 ID 不用于单独寻址。运行 ID 不写回 ScriptableObject 或作品包。 | KT-01 Foundation v0 `0343e59`：`ContentIdentity`、`RunId`、`LevelInstanceId`、`RuntimeScope`、显式 `ILevelRunContext`；W0-C 确认当前 ECA 仅具体引用，无跨实例路由。此小段 API 已准许消费者使用，后续破坏性改动先协调。 |
| 最小事件、动作与模式 | 首批事件采用 `RegionEntered`、`InteractionConfirmed`、`EncounterCompleted`、`ExitReached`；动作采用 `ActivateEncounter`、`SetDoorOpen`、`SetInteractionEnabled`、`UnlockExit`。动作结果至少区分成功、已满足、条件不满足、目标缺失、模式禁止。开局前固定 Sandbox 或 Integrated，仅安装一套绑定；Integrated 缺必需绑定须失败，不回落 Sandbox；预览／导入不写 Native Quest/Narrative。 | W0-C 对 NativeRun/Quest/Narrative/ECA 的只读追踪及开发方案 8.1；这是 KT-06 v0 消费契约，具体方法签名由 KT-06 提案、INT-LT 最终冻结。当前 Native Quest 固定单 Boss，Narrative `boss`／`bypass` 键不能用于任意外来关卡。 |
| 作品依赖、版本与导入 | 工具版本先使用工程迭代号 `0.1.0-dev.1`、内容格式 `1`，首版精确匹配。作品根资源递归依赖分类为作品自有、工具公共、UPM/引擎、外部未声明；只打包作品自有，外部未声明阻止导出。导入**前**对路径、GUID、内容摘要、公共覆盖与同作品更新作冲突检查；导入/预览不自动接正式主线。 | KT-01 有 `ToolkitVersion` 精确声明模型；W0-C 确认仓库当前没有 Manifest/导出器，Unity GetDependencies 会遍历引用，不能用无过滤 IncludeDependencies。S3 一次真实包往返才可验证清单和版本。版本号是本轮工程标识，可在发布前由 INT-LT 统一升版，不改变产品行为。 |
| 人工精修保护 | 后续 AI 编辑以“上次 AI 基线、当前内容、新提案”三方比较；人工改格、端口、装饰与属性默认保留，冲突预览/局部应用/Undo，缺基线时不得推断可覆盖。内容指纹和工具版本绑定人工验收记录；本轮内部候选包可用于 S3，不伪造正式人工验收。 | 需求 3.5/6.2 与开发方案 6.4/10.2；KT-03/08 实现功能，S3 检查一次往返，其余矩阵后置。 |
| Skill 分发 | 五个实际 `SKILL.md` 作为工具包源码分发；Unity 包中采用可保留的 `Assets/EchoLevelToolkit/SkillSources/` 文本载体，显式安装器预览覆盖差异后输出到目标工程 `.agents/skills/<name>/SKILL.md`，不在打开 Unity 时自动改全局 Codex 设置。若 Markdown 原文件在 `.unitypackage` 内保留性不可靠，安装器从 `.txt` TextAsset 还原；以 S3 包往返验证，不靠目录名猜测。 | W0-C 确认当前仓库没有项目 Skill；Codex 项目 Skill 发现位置为 `.agents/skills`，仅在 Unity Assets 中出现文件不等于可发现。`Documentation~`/Markdown 导出行为未实测，KT-09/08 需以实际包和安装结果决定最终载体。 |
| 素材与许可 | 先以可运行占位素材完成纵向切片。FusionPixel 有随包许可；旧 CyberCity Sprite 和 Generated Boss 美术只有项目内引用证据，分发权限不足时不得未经核实纳入对外包，改用自制/许可明确占位并列清单。 | W0-A 保存引用与许可目录只读检查；KT-02/03/08 打包时分类并记录，S3 检查实际依赖。 |

## 未完成的必要证据与下一关口

1. KT-01 原 `0343e59` 已集成为 `7dde3ae`；KT-02、KT-03、KT-06 代码随后按精确提交链集成到 `c1a995e`。Native 兼容、地图样例生成与正式绑定仍未完成，代码存在不等于工具包或本体目标达成。
2. W0-B 的 Unity 探针因 LicenseClient IPC 失败，1 格转角／混合接缝的实体通行仍未核验。不得把静态 0.36 净空当 S2 通过。
3. INT-LT 在隔离工程对 `c1a995e` 做了一次 Unity 2021 batch 编译并打开两个 Combat Preview 场景，exit 0、无 Missing Script；日志 `/private/tmp/int-lt-wave2-c1a995e.log`。未跑 Play、KT-03 样例尚未生成，不将此记录扩大为 S2 或交付版全项通过。
4. KT-02/03/06 已消费冻结的身份／Chunk 元数据 v0，后续场景、Native 适配、公共 Prefab 与最终依赖仍由 INT-LT 串行保存；必要签名变更须先通知消费者。S1～S4 在最终可交付会合点最少各做一次；G4 与完整 A/B、第二作者体验后置。
