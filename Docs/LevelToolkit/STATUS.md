# 关卡工具包主控台账

更新：2026-09-23 12:16（UTC+8）。**当前波次已落安全点并暂停调度**：KT-01/02/03/06 与 W0 已交付本轮代码／只读结果，INT-LT 已串行集成；所有本轮执行 task thread 均 idle。用户要求当前已启动任务完成后安全中止，下一波 KT/NB 不派发，等待再次授权。本轮输入基线和用户改动见 [BASELINE.md](BASELINE.md)。需求文档定义产品范围，[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) 为工程基线；当前只要求 [ACCEPTANCE.md](ACCEPTANCE.md) 的 S1～S4，其他矩阵后置。

## 全局锁与派发规则

- 最新代码集成安全点为 `c1a995e07ee967107d1b1940dc5f12272d326dc8`；`codex/unity-native-integration` 与 INT-LT 分支在此提交。主目录文档分支随后只有本台账/决策的文档提交，没有新增实现改动。KT-02/03/06 原任务基线为 `7dde3ae`，按提交链精确导入；无 Native 旧文件、Packages 或 ProjectSettings 改动。输入需求/方案文档在主目录尚未提交，未来任务仍须从主目录**只读**读取指定绝对路径。
- 并行上限：5 个独立执行 task thread，含 INT-LT；禁用 subagent／嵌套代理。主控只写文档与调度。
- 统一目标 `gpt-6-sol`／`xhigh`／请求级 `priority`。`M/T/P=待核` 表示任务尚未创建；创建后从该任务的有效 `thread_settings` 核验，不以 prompt 或用户级配置代替证据。用户最新指示：若不是 priority，记录缺口并继续执行，不静默降级或虚报。
- Unity 使用者：**当前无执行任务占用**；用户主目录 NativeDemo Editor 仍打开且不得关闭。W0-B 与 KT-03 的独立物理／样例生成尝试均在许可证 IPC 阶段退出，未产生 Play 结果；INT-LT 在其隔离工作树完成一次合并编译与两个 Combat Preview 场景打开检查后已释放 Unity。共享 `NativeDemo`、最终集成场景、Packages、ProjectSettings、公共 Prefab 与共享接口最终版本只由 INT-LT 保存。
- W0 先只读并冻结 G0 接口，再派消费者；功能完成后只执行 S1～S4 会合检查。不得把未测写成通过。

## W0 与集成任务

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许范围与依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-00 | 决策已记录，物理证据待补 | 主控 / local | 主控文档；`7e274fa` | Sol/xhigh/priority 已核 | W0-A/B/C 已汇总到 `DECISIONS.md`；1 格实体通行未通过探针，产品候选不改 | 仅协调 | G0 保留未测项 |
| W0-A 战斗／宿主 | 已完成只读 | `01a0cc56-cc48-73d2-bfa0-f94175cf217e` / local | `e8fd`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 保存 Player/Combat/Chase/Orbit/Boss 参数与依赖、真实死亡/激活失败风险已回报；无文件改动 | 未使用 | KT-00 战斗决定 |
| W0-B 地图／物理 | 静态完成，物理未测 | `01a0cc57-09a0-7831-b75a-d32d0d7286db` / local | `f014`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 玩家半径、Tilemap/UPM/障碍层级已回报；独立 Editor LicenseClient IPC 失败 exit 199，无 Play/XML，探针移出 Assets，证据 `/private/tmp/echo-g0-w0b-probe` | 已释放 | S2 实体通行 |
| W0-C 叙事／打包／Skill | 已完成只读 | `01a0cc57-4453-7320-b8a9-9e0479b0d337` / local | `a0b2`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 稳定 ID、事件动作、模式隔离和包/Skill 风险已回报；无文件改动 | 未使用 | KT-00 接口决定 |
| INT-LT | 当前波次集成完成，已停 | `01a0cc5c-97c1-7070-9f78-e5036a812954` / local | `8003`／`codex/level-toolkit-integration`／`4d020e8`→`c1a995e` | Sol/xhigh/priority 已核 | 从主控文档 HEAD 顺序导入 KT-02 两笔、KT-06 两笔和 KT-03 一笔；静态/GUID 检查与一次 Unity 编译／两个 Preview 场景打开通过，用户脏文件 58/58 哈希未变 | 已释放 | 恢复后共享战斗 Native 适配及后续波次 |

## 功能任务

以下 `threadId/hostId/工作树/分支` 均未分配；每次派发从当时已核对的集成 SHA 起步，在本表补全。路径是任务允许范围，跨越共享路径须交 INT-LT。

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许路径；依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-01 | 已集成，S1 子项已编译 | `01a0cc56-5afe-74c3-abdc-6e7a63fa77de` / local | `7da4`／`codex/kt-01-foundation`／`7e274fa`，原 `0343e59`→集成 `7dde3ae` | Sol/xhigh/priority 已核 | Foundation v0／独立 asmdef/meta；INT-LT Unity batch 编译 exit 0，尚无示例启动 | 已释放 | 消费者实现 |
| KT-02 | 代码已集成，Native 兼容未接 | `01a0cc60-57fd-7540-aa2f-617002292296` / local | `cae5`／`codex/kt-02-combat`／`7dde3ae`；原 `ab76cee`→`be9c5c5`、`b88192e`→`f952069` | Sol/xhigh/priority 已核 | 共享 Combat/轻量 Preview/四模板已交；原 Native 外壳仍执行旧逻辑，不能称唯一来源。Unity Play 与本体回归未测 | 已释放 | Native 兼容、S2 |
| KT-03 | 代码已集成，样例未生成 | `01a0cc60-b0c3-7ef3-9f00-a7ca89f8f97c` / local | `84a9`／`codex/kt-03-map`／`7dde3ae`；原 `c7dbc87`→`c1a995e` | Sol/xhigh/priority 已核 | Tilemap 创建/混合摆放/宽端口几何/AI 三方精修保护代码已交；Unity LicenseClient exit 199，未导入、生成样例或实体走图，1格仍未证实 | 已释放 | 样例生成、KT-04、S2 |
| KT-04 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Editor/Validation/`；KT-01／03，封门核验等 KT-05 | 排队 | G2 校验器 |
| KT-05 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | `Runtime/Level/`、门集合／遭遇编辑；KT-02／06 | 排队 | G2 遭遇闭环 |
| KT-06 | 代码已集成，正式绑定未接 | `01a0cc60-f383-7861-b92c-5ce0c9dc18d6` / local | `8573`／`codex/kt-06-binding`／`7dde3ae`；原 `5fd9a71`→`cae7611`、`52b1a7a`→`c7f56fd` | Sol/xhigh/priority 已核 | 局部端点、Run/实例路由、Sandbox/Integrated 互斥与失败完成事件重送；未从真实组件发事实，NB-03 正式绑定未实现 | 已释放 | KT-05／NB-03／S4 |
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
| S1 编译启动 | 当前集成快照的最小步骤已查，最终未放行 | INT-LT 在隔离工程 Unity 2021 batch 编译 exit 0、两个 Combat Preview 场景可打开且无 Missing Script；脱敏日志 `/private/tmp/int-lt-wave2-c1a995e.log`。未进入 Play、KT-03 样例未生成，完整交付版 S1 待后续变更会合再判定 |
| S2 核心短流程 | 未执行 | 等混合 Chunk、战斗与终点会合 |
| S3 一次包往返 | 未执行 | 等工具包与完整示例包 |
| S4 一次正式接入 | 未执行 | 等 NB-01～03 与 KT-07 |

## 配置与并发证据

本主控任务和 KT-01、W0-A/B/C、INT-LT、KT-02/03/06 的本地 session `event_msg.payload.thread_settings` 均显示 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`；这是**请求配置**证据，不保证服务端实际调度层级。`create_thread`／`send_message_to_thread` 没有服务层参数，派发没有伪造字段。当前运行中的独立执行任务 **0/5**；所有本轮线程 idle，Unity 执行槽释放，用户主目录 Editor 仍打开。此前其他回合的 P2 工作树仍保留，不作清理。

## 安全中止与恢复入口

- 代码停止点：`c1a995e07ee967107d1b1940dc5f12272d326dc8`；集成引用与 INT-LT 隔离分支一致，主目录仅在其上追加本台账/决策文档提交。没有新建定时任务，所有分支、工作树和用户修改保留。
- 优先缺口：共享战斗尚未接回 Native（KT-02 交接清单）、KT-03 Tilemap 样例与实体净空未运行、KT-04/05 与 NB-01～04 尚未开发、导出／导入防护、五 Skill、完整示例与 S2～S4 均未完成。不得把本轮安全点称为工具包快速版交付。
- 用户再次授权后，先读本表与 `DECISIONS.md`，核对上述 SHA 和用户脏文件；从未完成的当前产品能力继续，保持最多五个独立 Sol task thread、一处 Unity 时段及精确路径租约。S1～S4 只在可交付会合点做必要检查。
