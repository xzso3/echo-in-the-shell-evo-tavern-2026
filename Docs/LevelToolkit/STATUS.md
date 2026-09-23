# 关卡工具包主控台账

更新：2026-09-23（UTC+8）。用户已明确恢复开发，并把独立 Sol 执行任务并行上限从 5 提到 **15**，INT-LT 占一个；Unity 同时仍只归一个任务。P2 继续暂缓。需求文档定义产品范围，[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) 为工程基线；本轮只要求 S1～S4，完整 A/B 和第二作者验收后置。用户已将 S3 的交付介质从 `.unitypackage` 改为手工文件夹 ZIP，具体覆盖见 [DECISIONS.md](DECISIONS.md) 与待交的 `ZIP_HANDOFF.md`；本轮输入基线和用户改动见 [BASELINE.md](BASELINE.md)。

## 全局锁与派发规则

- 当前主目录安全点为 `60ae785`（仅台账更新）；INT-LT 隔离分支已到 `66e5a49`，尚未回写主目录。KT-01～10 与 NB-01～04 源码已接入隔离分支；S2 短流程已通过，S3 包往返和 S4 正式新场景仍在执行。输入需求/方案文档在主目录尚未提交，执行任务须从主目录**只读**读取指定绝对路径。
- 并行上限：**15** 个独立 Sol 执行 task thread，含 INT-LT；禁用 subagent／嵌套代理。主控只写文档与调度，不为填满数量制造任务。
- 统一目标 `gpt-6-sol`／`xhigh`／请求级 `priority`。`M/T/P=待核` 表示任务尚未创建；创建后从该任务的有效 `thread_settings` 核验，不以 prompt 或用户级配置代替证据。用户最新指示：若不是 priority，记录缺口并继续执行，不静默降级或虚报。
- Unity 使用者：**INT-LT**。Unity MCP 已只读确认它目前只连用户主目录 NativeDemo，绝不能误当隔离工作树；INT-LT 必须先核对保存状态和用户修改，才可在唯一时段操作。W0-B 与 KT-03 的旧物理／样例生成尝试曾在许可证 IPC 阶段退出，不算 Play 结果。共享 `NativeDemo`、最终集成场景、Packages、ProjectSettings、公共 Prefab 与共享接口最终版本只由 INT-LT 保存。
- W0 先只读并冻结 G0 接口，再派消费者；功能完成后只执行 S1～S4 会合检查。不得把未测写成通过。

## W0 与集成任务

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许范围与依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-00 | 决策已记录，物理证据待补 | 主控 / local | 主控文档；`7e274fa` | Sol/xhigh/priority 已核 | W0-A/B/C 已汇总到 `DECISIONS.md`；1 格实体通行未通过探针，产品候选不改 | 仅协调 | G0 保留未测项 |
| W0-A 战斗／宿主 | 已完成只读 | `01a0cc56-cc48-73d2-bfa0-f94175cf217e` / local | `e8fd`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 保存 Player/Combat/Chase/Orbit/Boss 参数与依赖、真实死亡/激活失败风险已回报；无文件改动 | 未使用 | KT-00 战斗决定 |
| W0-B 地图／物理 | 静态完成，物理未测 | `01a0cc57-09a0-7831-b75a-d32d0d7286db` / local | `f014`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 玩家半径、Tilemap/UPM/障碍层级已回报；独立 Editor LicenseClient IPC 失败 exit 199，无 Play/XML，探针移出 Assets，证据 `/private/tmp/echo-g0-w0b-probe` | 已释放 | S2 实体通行 |
| W0-C 叙事／打包／Skill | 已完成只读 | `01a0cc57-4453-7320-b8a9-9e0479b0d337` / local | `a0b2`／detached／`7e274fa` | Sol/xhigh/priority 已核 | 稳定 ID、事件动作、模式隔离和包/Skill 风险已回报；无文件改动 | 未使用 | KT-00 接口决定 |
| INT-LT | S2 已通过；S3 改 ZIP；S4 待接收作品 | `01a0cc5c-97c1-7070-9f78-e5036a812954` / local | `8003`／`codex/level-toolkit-integration`；隔离 HEAD `66e5a49`，主目录 `c078296` | Sol/xhigh/priority 已核 | NativeDemo 受影响短 Play 已通过；KT-01～10、NB-01～04/KT-07 源码已导入。S2 实体接缝/1格转角/真实清敌/Boss/终点、两次重开新 RunId 均有日志。Unity 包 batch 因 AssetImportWorker IPC 超时未生成包；用户决定改手工文件夹 ZIP，旧 S3 菜单清单作废。正式场景待真实接收作品 | 唯一使用者 | KT-08-ZIP 集成→S3 ZIP→S4 |

## 功能任务

路径是任务允许范围，跨越共享路径须交 INT-LT。下表来源提交由 INT-LT 精确拣入隔离分支，最终以隔离分支和交付 SHA 为准。

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许路径；依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-01 | 已集成，S1 子项已编译 | `01a0cc56-5afe-74c3-abdc-6e7a63fa77de` / local | `7da4`／`codex/kt-01-foundation`／`7e274fa`，原 `0343e59`→集成 `7dde3ae` | Sol/xhigh/priority 已核 | Foundation v0／独立 asmdef/meta；INT-LT Unity batch 编译 exit 0，尚无示例启动 | 已释放 | 消费者实现 |
| KT-02 | 共享行为与 Native 委托已接，完整回归后置 | `01a0cc60-57fd-7540-aa2f-617002292296` / local | `cae5`／`codex/kt-02-combat`／`7dde3ae`；原 `ab76cee`→`be9c5c5`、`b88192e`→`f952069` | Sol/xhigh/priority 已核 | 共享 Combat/Preview/四模板；INT-LT `ab091a1`+`3031f05` 接旧组件并做 NativeDemo 受影响短 Play，旧脚本/Prefab GUID 保留；完整原主线与新场景仍未测 | 已释放 | KT-05/NB-01 与 S2 |
| KT-03 | 代码与完整示例已集成，实体 S2 待判 | `01a0cc60-b0c3-7ef3-9f00-a7ca89f8f97c` / local | `84a9`／`codex/kt-03-map`／`7dde3ae`；原 `c7dbc87`→`c1a995e` | Sol/xhigh/priority 已核 | Tilemap 创建/混合摆放/宽端口几何/AI 三方精修保护；KT-10 的 5 Chunk 示例已保存并通过预检，1 格实体转角/接缝未证实 | 已释放 | S2 实体通行 |
| KT-04 | 代码已集成，实体 S2 待判 | `01a0cc7f-de87-7793-97db-0e7213d44ae5` / local | `48fb`／`codex/kt-04-validation`／`c1a995e`；原 `feb3ad6`→`251dfc9`，集成 `15d3ebc`→`69bf162` | Sol/xhigh/priority 已核 | 格子拓扑、宽端口、圆体积扫掠、隔离场景封门、54 稳定问题码；KT-05 正式 Level 导出回调已接且完整示例报 0 问题 | 不占 | S2 实体通行 |
| KT-05 | 代码及重开修复已集成，S2 通过 | `01a0cc80-3fb9-75d1-aaf9-8c953b450389` / local | `8dfc`／`codex/kt-05-level-encounters`／`c1a995e`；`e187f1a`→`d3aa921`→`fd9428e`→`d97c59f`，返修 `02fce88`→集成 `66e5a49` | Sol/xhigh；本次返修 **default**，早前 priority | 三遭遇模式、MapDoorSet、真实死亡/Boss完成、Sandbox终点与配置；重开只从可重载场景或实际 Prefab 资产建新局；S2 连续两次新 RunId | 已释放 | S3 打包 |
| KT-06 | 代码已集成，正式绑定未接 | `01a0cc60-f383-7861-b92c-5ce0c9dc18d6` / local | `8573`／`codex/kt-06-binding`／`7dde3ae`；原 `5fd9a71`→`cae7611`、`52b1a7a`→`c7f56fd` | Sol/xhigh/priority 已核 | 局部端点、Run/实例路由、Sandbox/Integrated 互斥与失败完成事件重送；未从真实组件发事实，NB-03 正式绑定未实现 | 已释放 | KT-05／NB-03／S4 |
| KT-07 | 代码已集成，S4 待跑 | `01a0cc8e-79ca-78e0-b53d-080c093a3fbe` / local | `136f`／`codex/kt-07-formal-binding`／`c1a995e`；原 `6eae90e`、`bfb9f39`，集成 `cf3be0f`、`c3f86f6` | Sol/xhigh/priority 已核 | 真实 InteractionConfirmed→正式配方→NB-03 scoped 目标/叙事→KT-06 动作；失败 Mount 后 pending objective 清理已由 NB-03 补齐 | 不占 | NB-04/S4 |
| KT-08 | 原 Unity 包代码已集成；ZIP 补位中 | `01a0cc80-8b2e-7e70-b63e-16c724d63b40` / local | `d36a`／`codex/kt-08-packaging`／`c1a995e`；原 `56e79ef`→`8286660`→`809991d`、文档 `0d56e14`；KT-08-ZIP 续派中 | Sol/xhigh/priority 初次已核；续派待核 | 原 Manifest/指纹、导入前路径/GUID/哈希/版本冲突保护保留；本轮以最小只读 ZIP 预检和手工目录复制取代 Unity 包收发，不自动覆盖 | 不占 Unity | ZIP guard/手册→S3 |
| KT-09 | 五 Skill 已集成，S3 发现待跑 | `01a0cc85-90c6-7fc1-9d4a-7d65c6fbaa0b` / local | `0aaa`／`codex/kt-09-level-skills`／`c1a995e`；`0805ec8`，集成 `69b514a` | Sol/xhigh/priority 已核 | 五份 SKILL.md、同内容 TextAsset 载体、显式安装器/覆盖预览；离线验证通过，干净工程发现待 S3 | 已释放 | S3 |
| KT-10 | 完整示例已重建，S2 通过 | `01a0cc95-219f-7603-a653-eddc5b85f248` / local | `33e8`／`codex/kt10-full-level-sample`／`eed5376`；源 `657b7cd`→`0b877fa`→`d8bebac`→`fe5defc`，返修 `0ef4412`→集成 `71c2384`，资产 `ccde0be` | Sol/xhigh/priority 请求已核 | 5 混合 Chunk、FreeCombat/清场/Boss、门/终点；互引两份独立根 Prefab，Boss32 单格 L 弯；KT-04 报 0 问题，S2 实体通过 | INT-LT 唯一使用 | S3 包往返 |
| KT-11 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | RELEASE、包清单／交接；KT-10／NB-04 | 排队 | S1～S4 快速交付 |
| NB-01 | 代码已集成，S4 待跑 | `01a0cc8a-8eb2-7db0-8718-2e68b4334a3d` / local | `6f58`／`codex/nb-01-native-level-host`／`c1a995e`；`eb60d93`→`c9d4a3c`→`bfb19b1`→`bd4869a` | Sol/xhigh/priority 已核 | 每实例身份/挂载/卸载、焦点暂停、保留非焦点状态与 HP；正式场景接线仍待 NB-04 | 已释放 | S4 |
| NB-02 | 代码已集成，S4 待跑 | `01a0cc8c-d234-79d2-b1ef-9eb4743c2bc7` / local | `7b4c`／`codex/nb-02-native-input-support`／`c1a995e`；`84eaa85`→`4f1f37b`→`2da55a5` | Sol/xhigh/priority 已核 | 焦点实例动态互动/Boss E、HUD与医疗/弱点目标；NativeDemo 旧模式医疗/弱点短 Play 已过 | 已释放 | S4 |
| NB-03 | 代码已集成，S4 待跑 | `01a0cc85-2234-73c3-a313-f0d22d4dc4cd` / local | `ffca`／`codex/nb-03-narrative`／`c1a995e`；原 `df1c7a3`、`5c51b57`、`b200c01`，集成 `e767270`、`bf456cf`、`f0bdc95` | Sol/xhigh/priority 已核 | 外来目标/事实来源键去重，失败 Mount pending objective 回滚；不改旧分数/结局；S4 实跑未做 | 已释放 | S4 |
| NB-04 | 装配源码已集成，场景/S4 待跑 | `01a0cca7-0135-77a1-9319-cd91c741a833` / local | `267b`／`codex/nb-04-native-integration-sample`／`a99f257`；原 `fd7d1cf`→集成 `676b08c` | Sol/xhigh/**default** 已核，priority 未生效 | 接收后的完整作品 prefab 生成 Integrated 配置、显式 Mount/Focus/Unmount 和真实事实接线；Unity 编译 exit 0，新增场景由 INT-LT 保存 | INT-LT 独占最终保存 | S4 本体样例 |

## 最小验收

| ID | 状态 | 证据／下一步 |
| --- | --- | --- |
| S1 编译启动 | 受影响子项通过，最终未放行 | KT-01/02/03/06 合并后 Unity 编译及两个 Combat Preview 场景打开无 Missing Script；NativeDemo 受影响短链已过；完整示例可加载且无 Missing Script，KT-05/04 报 0 问题。NB-04 与最终包导入后仍须一次会合检查 |
| S2 核心短流程 | 已通过，键盘 E 未直接注入 | 修复后隔离 Play `/private/tmp/int-lt-s2-fulllevel-66e5a49.log`：Rigidbody 到 x2.91/x7.91 混合接缝、Boss 区一格 L 弯到 x16.38/y5.49，两次真实 EnemyDied 开门，Boss 封门/破壳/核心交互解锁，x46.56 实体进 Finish；两次重开均 Running、新 RunId 与初态恢复，日志 `INT_LT_S2_SHORT_FLOW_OK`。核心使用宿主调用的真实 `InteractCore` 校验入口，未直接注入物理键盘 E |
| S3 一次 ZIP 文件夹往返 | 未执行 | 用户明确放弃 `.unitypackage` 交付；原 batch 没有成功包，原手工 Unity 包菜单清单作废。待 KT-08-ZIP 只读预检完成，手工压缩含 `.meta` 的 toolkit/完整作品目录，在干净匹配 Unity 工程复制、编译/启动、安装五 Skill；不将仅 ZIP 生成写成通过 |
| S4 一次正式接入 | 未执行，等 S3 接收作品 | NB-04 新场景装配 helper 源 `20bfa36` 已交；收到实际 ZIP 接收作品后由 INT-LT 精确导入、保存新增场景并实跑正式事实链 |

## 配置与并发证据

本主控及较早任务的本地 session `event_msg.payload.thread_settings` 曾可观察到 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`；这是**请求配置**证据，不保证服务端调度。NB-04 和 KT-05 本次返修的本地设置为 Sol/xhigh/**default**，priority 未生效；KT-10 本次返修仍为 Sol/xhigh/priority 请求。用户已授权继续交付。`create_thread`／`send_message_to_thread` 无服务层参数，派发没有伪造字段。当前执行任务并行上限 15，Unity 执行槽只归 INT-LT。此前 P2 工作树仍保留，不作清理。

## 当前依赖与恢复入口

- 后续顺序：KT-08-ZIP 交只读预检及手册 → INT-LT 精确集成 → 用户手工压缩含 `.meta` 的 toolkit 和完整作品目录，先在目标工程复制前预检，再做一次干净 Unity 2021.3.27f1c2 工程接收、编译/启动和 Skill 安装 → INT-LT 从实际接收作品保存新增正式场景、执行 S4 → 形成可交付 ZIP 与主目录安全点。源分支各自保留，不能带旧祖先或覆盖用户脏文件。
- 最大剩余产品缺口：NB-01～04 正式多实例输入/叙事尚未在新增场景实跑；工具包/作品 ZIP 尚未产生和接收，五 Skill 未在干净工程发现；S3/S4 尚未完成。S2 的物理键盘 E 未直接测。不得把当前安全点称为工具包快速版交付。
- 不新建定时任务，不清理用户或旧工作树；S1～S4 只在可交付会合点做必要检查，实际通过范围与未测范围分开。后续再获进度时更新本表，继续保持单 Unity 时段和精确文件租约。
