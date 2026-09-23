# 关卡工具包主控台账

更新：2026-09-23 11:52（UTC+8）。首次建账时所有 KT、NB、W0 与 INT-LT 均标为未执行；此后用户要求直接放行实现任务，并明确即使实际 `priority` 未生效也继续执行、优先完成任务。用户最新要求**当前已启动任务完成后安全中止**：本轮不再派发下一波 KT/NB，待 KT-02/03/06 落下可恢复提交并完成必要集成后暂停。本轮输入基线和用户改动见 [BASELINE.md](BASELINE.md)。需求文档定义产品范围，[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) 为工程基线；当前只要求 [ACCEPTANCE.md](ACCEPTANCE.md) 的 S1～S4，其他矩阵后置。

## 全局锁与派发规则

- 最新已核对主目录与集成引用 SHA：`7dde3aec8cf7060b825be4464e7ad18ebfdcb91a`，只增加 KT-01 Foundation v0；KT-02/03/06 均从此 SHA 起步。输入需求/方案文档在主目录尚未提交，执行工作树须从主目录**只读**读取指定绝对路径，不能据旧 Git 树假设它们存在。
- 并行上限：5 个独立执行 task thread，含 INT-LT；禁用 subagent／嵌套代理。主控只写文档与调度。
- 统一目标 `gpt-6-sol`／`xhigh`／请求级 `priority`。`M/T/P=待核` 表示任务尚未创建；创建后从该任务的有效 `thread_settings` 核验，不以 prompt 或用户级配置代替证据。用户最新指示：若不是 priority，记录缺口并继续执行，不静默降级或虚报。
- Unity 使用者：**当前无执行任务占用**；用户主目录 NativeDemo Editor 仍打开且不得关闭。W0-B 的独立物理探针在许可证 IPC 阶段退出，未产生 Play 结果；INT-LT 在独立工作树成功完成 KT-01 编译后已释放 Unity。共享 `NativeDemo`、最终集成场景、Packages、ProjectSettings、公共 Prefab 与共享接口最终版本只由 INT-LT 保存。
- W0 先只读并冻结 G0 接口，再派消费者；功能完成后只执行 S1～S4 会合检查。不得把未测写成通过。

## W0 与集成任务

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许范围与依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-00 | 决策已记录，物理证据待补 | 主控 / local | 主控文档；`7e274fa` | Sol/xhigh/priority 已核 | W0-A/B/C 已汇总到 `DECISIONS.md`；1 格实体通行未通过探针，产品候选不改 | 仅协调 | G0 保留未测项 |
| W0-A 战斗／宿主 | 已完成只读 | `01a0cc56-cc48-73d2-bfa0-f94175cf217e` / local | `e8fd`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 保存 Player/Combat/Chase/Orbit/Boss 参数与依赖、真实死亡/激活失败风险已回报；无文件改动 | 未使用 | KT-00 战斗决定 |
| W0-B 地图／物理 | 静态完成，物理未测 | `01a0cc57-09a0-7831-b75a-d32d0d7286db` / local | `f014`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 玩家半径、Tilemap/UPM/障碍层级已回报；独立 Editor LicenseClient IPC 失败 exit 199，无 Play/XML，探针移出 Assets，证据 `/private/tmp/echo-g0-w0b-probe` | 已释放 | S2 实体通行 |
| W0-C 叙事／打包／Skill | 已完成只读 | `01a0cc57-4453-7320-b8a9-9e0479b0d337` / local | `a0b2`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 稳定 ID、事件动作、模式隔离和包/Skill 风险已回报；无文件改动 | 未使用 | KT-00 接口决定 |
| INT-LT | KT-01 集成完成，待本波后续提交 | `01a0cc5c-97c1-7070-9f78-e5036a812954` / local | `8003`／`codex/level-toolkit-integration`／`b2bd036`→`7dde3ae` | Sol/xhigh/priority 已核 | 原 `0343e59` 精确导入为 `7dde3ae`；主目录与集成 ref 同 SHA，用户脏文件 60/60 哈希未变 | 已释放 | KT-02/03/06 集成、安全停机 |

## 功能任务

以下 `threadId/hostId/工作树/分支` 均未分配；每次派发从当时已核对的集成 SHA 起步，在本表补全。路径是任务允许范围，跨越共享路径须交 INT-LT。

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许路径；依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-01 | 已集成，S1 子项已编译 | `01a0cc56-5afe-74c3-abdc-6e7a63fa77de` / local | `7da4`／`codex/kt-01-foundation`／`7e274fa`，原 `0343e59`→集成 `7dde3ae` | Sol/xhigh/priority 已核 | Foundation v0／独立 asmdef/meta；INT-LT Unity batch 编译 exit 0，尚无示例启动 | 已释放 | 消费者实现 |
| KT-02 | 进行中 | `01a0cc60-57fd-7540-aa2f-617002292296` / local | `cae5`／`codex/kt-02-combat`／`7dde3ae` | Sol/xhigh/priority 已核 | `Runtime/Combat/`、独立 RunHost／模板；KT-01 v0，Native 兼容由 INT-LT | 不占 | 当前任务安全提交 |
| KT-03 | 进行中 | `01a0cc60-b0c3-7ef3-9f00-a7ca89f8f97c` / local | `84a9`／`codex/kt-03-map`／`7dde3ae` | Sol/xhigh/priority 已核 | `Runtime/Map/`、`Editor/Authoring/Map/`、独立样例与精修保护；1 格通行仍未测 | 不占 | 当前任务安全提交 |
| KT-04 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Editor/Validation/`；KT-01／03，封门核验等 KT-05 | 排队 | G2 校验器 |
| KT-05 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Level/`、门集合／遭遇编辑；KT-02／06 | 排队 | G2 遭遇闭环 |
| KT-06 | 进行中 | `01a0cc60-f383-7861-b92c-5ce0c9dc18d6` / local | `8573`／`codex/kt-06-binding`／`7dde3ae` | Sol/xhigh/priority 已核 | `Runtime/Binding/`、端点清单；KT-01 v0，正式 Native 写入留后续 NB-03 | 不占 | 当前任务安全提交 |
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
| S1 编译启动 | 部分检查 | INT-LT 在隔离工程对 KT-01 Foundation 运行 Unity 2021 batch 编译 exit 0，脱敏日志 `/private/tmp/int-lt-s1-7dde3ae.log`；新增示例尚无，S1 整项未通过 |
| S2 核心短流程 | 未执行 | 等混合 Chunk、战斗与终点会合 |
| S3 一次包往返 | 未执行 | 等工具包与完整示例包 |
| S4 一次正式接入 | 未执行 | 等 NB-01～03 与 KT-07 |

## 配置与并发证据

本主控任务和 KT-01、W0-A/B/C、INT-LT、KT-02/03/06 的本地 session `event_msg.payload.thread_settings` 均显示 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`；这是**请求配置**证据，不保证服务端实际调度层级。`create_thread`／`send_message_to_thread` 没有服务层参数，派发没有伪造字段。当前运行中的独立执行任务为 KT-02/03/06（3/5）；INT-LT 当前安全点已停。Unity 执行槽无执行任务，用户主目录 Editor 仍打开。
