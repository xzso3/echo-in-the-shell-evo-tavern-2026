# 关卡工具包主控台账

更新：2026-09-23 11:39（UTC+8）。首次建账时所有 KT、NB、W0 与 INT-LT 均标为未执行；此后用户要求直接放行实现任务，并明确即使实际 `priority` 未生效也继续执行、优先完成任务。本轮输入基线和用户改动见 [BASELINE.md](BASELINE.md)。需求文档定义产品范围，[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) 为工程基线；当前只要求 [ACCEPTANCE.md](ACCEPTANCE.md) 的 S1～S4，其他矩阵后置。

## 全局锁与派发规则

- 最新已核对集成 SHA：`7e274fab32ef6a1a986853c97cef76de3c829d4c`；新任务必须记录自己实际起始 SHA。输入文档在主目录尚未提交，执行工作树须从主目录**只读**读取指定绝对路径，不能据旧 Git 树假设它们存在。
- 并行上限：5 个独立执行 task thread，含 INT-LT；禁用 subagent／嵌套代理。主控只写文档与调度。
- 统一目标 `gpt-6-sol`／`xhigh`／请求级 `priority`。`M/T/P=待核` 表示任务尚未创建；创建后从该任务的有效 `thread_settings` 核验，不以 prompt 或用户级配置代替证据。用户最新指示：若不是 priority，记录缺口并继续执行，不静默降级或虚报。
- Unity 使用者：**用户主目录 NativeDemo Editor**。执行任务不得关闭它；W0 的 Unity 探针须与用户 Editor 隔离且串行。共享 `NativeDemo`、最终集成场景、Packages、ProjectSettings、公共 Prefab 与共享接口最终版本只由 INT-LT 保存。
- W0 先只读并冻结 G0 接口，再派消费者；功能完成后只执行 S1～S4 会合检查。不得把未测写成通过。

## W0 与集成任务

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许范围与依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-00 | 进行中 | 主控 / local | 主控文档；`7e274fa` | Sol/xhigh/priority 已核 | 汇总 W0 只读报告，冻结 `DECISIONS.md`；无实现改动 | 仅协调 | G0 技术冻结 |
| W0-A 战斗／宿主 | 进行中 | `01a0cc56-cc48-73d2-bfa0-f94175cf217e` / local | `e8fd`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 只读 Native 战斗、Prefab 参数、Boss 依赖；原型探针须隔离 | 不占，必要探针排队 | KT-00 战斗证据 |
| W0-B 地图／物理 | 进行中 | `01a0cc57-09a0-7831-b75a-d32d0d7286db` / local | `f014`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 只读 Tilemap、碰撞、素材、UPM、1 格净空；原型探针须隔离 | 不占，必要探针排队 | KT-00 空间证据 |
| W0-C 叙事／打包／Skill | 进行中 | `01a0cc57-4453-7320-b8a9-9e0479b0d337` / local | `a0b2`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 只读任务、叙事、身份、导出与 Skill 分发；不改全局配置 | 不占 | KT-00 接口证据 |
| INT-LT | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | 独占共享接口最终写入、公共 Prefab、NativeDemo／集成场景、Packages／ProjectSettings、版本与 Git 集成；依赖 G0 | 唯一执行使用者 | 各波集成、S1～S4 |

## 功能任务

以下 `threadId/hostId/工作树/分支` 均未分配；每次派发从当时已核对的集成 SHA 起步，在本表补全。路径是任务允许范围，跨越共享路径须交 INT-LT。

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许路径；依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-01 | 进行中 | `01a0cc56-5afe-74c3-abdc-6e7a63fa77de` / local | `7da4`／`codex/kt-01-foundation`／`7e274fa` | Sol/xhigh/priority 已核 | `Assets/EchoLevelToolkit/Runtime/Foundation/`、asmdef 提案；与 KT-00 并行，产品参数仍待冻结 | 不占 | G1 公共 API v0 |
| KT-02 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Combat/`、独立 RunHost／模板；KT-01 | 排队 | G1 共享战斗 |
| KT-03 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Map/`、`Editor/Authoring/Map/`、独立样例；KT-01 | 排队 | G1 Tilemap |
| KT-04 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Editor/Validation/`；KT-01／03，封门核验等 KT-05 | 排队 | G2 校验器 |
| KT-05 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Level/`、门集合／遭遇编辑；KT-02／06 | 排队 | G2 遭遇闭环 |
| KT-06 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Binding/`、端点清单；KT-01 | 排队 | G1 身份／模式 |
| KT-07 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Assets/NativeGame/ToolkitIntegration/` 的正式样例；KT-05／06／NB-03 | 排队 | S4 正式接入 |
| KT-08 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Editor/Packaging/`、Manifest／验收记录；KT-03／04／05／06 | 排队 | G3 导出／接收 |
| KT-09 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | Skill 分发与创作者指南；KT-03／05／08 | 不占 | G3 五 Skill |
| KT-10 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Samples/`、独立工程和包；KT-02～09 | 排队 | S2／S3 示例 |
| KT-11 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | RELEASE、包清单／交接；KT-10／NB-04 | 排队 | S1～S4 快速交付 |
| NB-01 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Assets/NativeGame/ToolkitIntegration/LevelHost/`；KT-01／03／05／06 | 排队 | G2 正式宿主 |
| NB-02 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | ToolkitIntegration 输入桥接；HUD／Interaction／Support 改动交 INT-LT；KT-02／05／06／NB-01 | 排队 | G2 多目标兼容 |
| NB-03 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | ToolkitIntegration 正式绑定；Quest／Narrative 独占提案交 INT-LT；KT-06／NB-01 | 排队 | S4 叙事状态 |
| NB-04 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | 新增 NativeToolkitIntegration 场景、接入报告；KT-07／08／NB-01～03 | INT-LT 独占最终保存 | S4 本体样例 |

## 最小验收

| ID | 状态 | 证据／下一步 |
| --- | --- | --- |
| S1 编译启动 | 未执行 | 等可运行纵向切片集成 |
| S2 核心短流程 | 未执行 | 等混合 Chunk、战斗与终点会合 |
| S3 一次包往返 | 未执行 | 等工具包与完整示例包 |
| S4 一次正式接入 | 未执行 | 等 NB-01～03 与 KT-07 |

## 配置与并发证据

本主控任务和 KT-01、W0-A/B/C 的本地 session `event_msg.payload.thread_settings` 均显示 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`；这是**请求配置**证据，不保证服务端实际调度层级。`create_thread`／`send_message_to_thread` 没有服务层参数，派发没有伪造字段。当前独立执行任务 4/5，INT-LT 未启动；Unity 执行槽未授予任务，用户主目录 Editor 仍打开。
