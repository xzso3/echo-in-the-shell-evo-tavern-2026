# 关卡工具包主控台账

更新：2026-09-23 12:51（UTC+8）。用户已明确恢复开发，并把独立 Sol 执行任务并行上限从 5 提到 **15**，INT-LT 占一个；Unity 同时仍只归一个任务。P2 继续暂缓。需求文档定义产品范围，[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) 为工程基线；本轮只要求 [ACCEPTANCE.md](ACCEPTANCE.md) 的 S1～S4，完整 A/B 和第二作者验收后置。本轮输入基线和用户改动见 [BASELINE.md](BASELINE.md)。

## 全局锁与派发规则

- 当前主目录与 `codex/unity-native-integration` 安全点为 `eed53760e635c4717c1be6ab1a713cc6ac14ef7d`：Native 战斗旧组件已委托共享实现，NB-03 外来事实状态 API 已导入，尚无新增正式集成关卡。INT-LT 正在独立分支继续导入 KT-08/05/NB-01，未形成新的主目录交付 SHA。输入需求/方案文档在主目录尚未提交，执行任务须从主目录**只读**读取指定绝对路径。
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
| INT-LT | Native 兼容安全点已交，后续集成中 | `01a0cc5c-97c1-7070-9f78-e5036a812954` / local | `8003`／`codex/level-toolkit-integration`；主目录安全点 `eed5376` | Sol/xhigh/priority 已核 | Native 旧战斗外壳委托共享 Combat，Prefab/NativeDemo 已保存；Play 短链验证真实敌人死亡、Boss 激活封门/核心 E；NB-03 状态 API 已导入，现排队 KT-08/05/NB-01/02 等 | 唯一使用者 | 新增关卡 S1/S4、后续串行集成 |

## 功能任务

以下 `threadId/hostId/工作树/分支` 均未分配；每次派发从当时已核对的集成 SHA 起步，在本表补全。路径是任务允许范围，跨越共享路径须交 INT-LT。

| ID | 状态 | threadId / hostId | 工作树／分支／基线 | M/T/P | 允许路径；依赖 | Unity 使用 | 下一个关口 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| KT-01 | 已集成，S1 子项已编译 | `01a0cc56-5afe-74c3-abdc-6e7a63fa77de` / local | `7da4`／`codex/kt-01-foundation`／`7e274fa`，原 `0343e59`→集成 `7dde3ae` | Sol/xhigh/priority 已核 | Foundation v0／独立 asmdef/meta；INT-LT Unity batch 编译 exit 0，尚无示例启动 | 已释放 | 消费者实现 |
| KT-02 | 共享行为与 Native 委托已接，完整回归后置 | `01a0cc60-57fd-7540-aa2f-617002292296` / local | `cae5`／`codex/kt-02-combat`／`7dde3ae`；原 `ab76cee`→`be9c5c5`、`b88192e`→`f952069` | Sol/xhigh/priority 已核 | 共享 Combat/Preview/四模板；INT-LT `ab091a1`+`3031f05` 接旧组件并做 NativeDemo 受影响短 Play，旧脚本/Prefab GUID 保留；完整原主线与新场景仍未测 | 已释放 | KT-05/NB-01 与 S2 |
| KT-03 | 代码已集成，样例未生成 | `01a0cc60-b0c3-7ef3-9f00-a7ca89f8f97c` / local | `84a9`／`codex/kt-03-map`／`7dde3ae`；原 `c7dbc87`→`c1a995e` | Sol/xhigh/priority 已核 | Tilemap 创建/混合摆放/宽端口几何/AI 三方精修保护代码已交；Unity LicenseClient exit 199，未导入、生成样例或实体走图，1格仍未证实 | 已释放 | 样例生成、KT-04、S2 |
| KT-04 | 实现中 | `01a0cc7f-de87-7793-97db-0e7213d44ae5` / local | `48fb`／待提交／`c1a995e` | Sol/xhigh/priority 已核 | `Editor/Validation/` 与独立 Runtime/Validation；格子拓扑、宽端口、圆体积扫掠、隔离场景封门和报告码；KT-03，KT-05 v0 可读 | 不占 | 完成功能、S2 会合 |
| KT-05 | 代码候选已交，场景未生成 | `01a0cc80-3fb9-75d1-aaf9-8c953b450389` / local | `8dfc`／`codex/kt-05-level-encounters`／`c1a995e`；`e187f1a`→`d3aa921`→`fd9428e`→`d97c59f` | Sol/xhigh/priority 已核 | 三遭遇模式、MapDoorSet、真实死亡/Boss完成、Sandbox终点与编辑器配置/样例生成器；离线编译通过，Unity Play/S2 未测 | 已释放 | INT-LT 导入、NB-01、S2 |
| KT-06 | 代码已集成，正式绑定未接 | `01a0cc60-f383-7861-b92c-5ce0c9dc18d6` / local | `8573`／`codex/kt-06-binding`／`7dde3ae`；原 `5fd9a71`→`cae7611`、`52b1a7a`→`c7f56fd` | Sol/xhigh/priority 已核 | 局部端点、Run/实例路由、Sandbox/Integrated 互斥与失败完成事件重送；未从真实组件发事实，NB-03 正式绑定未实现 | 已释放 | KT-05／NB-03／S4 |
| KT-07 | 实现中 | `01a0cc8e-79ca-78e0-b53d-080c093a3fbe` / local | `136f`／`codex/kt-07-formal-binding`／`c1a995e` | Sol/xhigh/priority 已核 | `ToolkitIntegration/FormalBindingSample/`；真实事件→NB-03状态→KT-06动作的正式接线配置，最终新增场景由 INT-LT 保存 | 不占 | S4 正式接入 |
| KT-08 | 代码候选已交，真实包未出 | `01a0cc80-8b2e-7e70-b63e-16c724d63b40` / local | `d36a`／`codex/kt-08-packaging`／`c1a995e`；`56e79ef`→`8286660`→`809991d` | Sol/xhigh/priority 已核 | 依赖白名单、Manifest/指纹验收、两粒度导出与导入前冲突预检；离线编译通过，实际 Unity 包与 S3 未做 | 已释放 | INT-LT 导入、S3 包往返 |
| KT-09 | 五 Skill 与安装器源码已交 | `01a0cc85-90c6-7fc1-9d4a-7d65c6fbaa0b` / local | `0aaa`／`codex/kt-09-level-skills`／`c1a995e`；`0805ec8` | Sol/xhigh/priority 已核 | 五份 SKILL.md、同内容 TextAsset 载体、显式安装器/覆盖预览；离线验证通过，Unity菜单与干净工程发现待 S3 | 已释放 | INT-LT/KT-08 导入、S3 |
| KT-10 | 实现中 | `01a0cc95-219f-7603-a653-eddc5b85f248` / local | `33e8`／`codex/kt10-full-level-sample`／`eed5376` | Sol/xhigh/priority 已核 | `Samples/FullLevel/` 与独立示例生成器；混合Chunk/三遭遇/门/终点，待 KT-05/08 代码集成和唯一 Unity 时段 | 不占 | S2 示例与 S3 包往返 |
| KT-11 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | RELEASE、包清单／交接；KT-10／NB-04 | 排队 | S1～S4 快速交付 |
| NB-01 | 宿主代码已交，正式场景未接 | `01a0cc8a-8eb2-7db0-8718-2e68b4334a3d` / local | `6f58`／`codex/nb-01-native-level-host`／`c1a995e`；`eb60d93`→`c9d4a3c`→`bfb19b1`→`bd4869a` | Sol/xhigh/priority 已核 | 每实例身份/挂载/卸载/相机与焦点暂停；离线编译/源码检查，INT-LT 尚未正式场景接线 | 已释放 | KT-05 集成后 NB-04/S4 |
| NB-02 | 输入／支援代码已交，场景未接 | `01a0cc8c-d234-79d2-b1ef-9eb4743c2bc7` / local | `7b4c`／`codex/nb-02-native-input-support`／`c1a995e`；`84eaa85`→`4f1f37b`→`2da55a5` | Sol/xhigh/priority 已核 | 焦点实例动态互动/Boss E、HUD与医疗/弱点目标；离线编译通过，INT-LT 尚未场景回归 | 已释放 | NB-01 焦点接线、S4 |
| NB-03 | 状态 API 已集成，正式场景未接 | `01a0cc85-2234-73c3-a313-f0d22d4dc4cd` / local | `ffca`／`codex/nb-03-narrative`／`c1a995e`；原 `df1c7a3`→`e767270`、`5c51b57`→`bf456cf` | Sol/xhigh/priority 已核 | 外来目标/事实来源键去重，不改旧分数/结局；IntegratedNarrativeBinding 代码与离线状态检查，S4 实跑未做 | 已释放 | KT-07/NB-04 S4 |
| NB-04 | 未执行 | — / — | 待建／待核／待集成 SHA | 待核 | 新增 NativeToolkitIntegration 场景、接入报告；KT-07／08／NB-01～03 | INT-LT 独占最终保存 | S4 本体样例 |

## 最小验收

| ID | 状态 | 证据／下一步 |
| --- | --- | --- |
| S1 编译启动 | 受影响子项通过，最终未放行 | KT-01/02/03/06 合并后 Unity 编译及两个 Combat Preview 场景打开无 Missing Script；Native 战斗接线后 NativeDemo Play 短链通过，NB-03 导入后编译通过。新增完整示例与后续模块尚未会合，最终 S1 仍待一次检查 |
| S2 核心短流程 | 未执行 | 等混合 Chunk、战斗与终点会合 |
| S3 一次包往返 | 未执行 | 等工具包与完整示例包 |
| S4 一次正式接入 | 未执行 | 等 NB-01～03 与 KT-07 |

## 配置与并发证据

本主控任务与本轮新建执行任务在本地 session `event_msg.payload.thread_settings` 可观察到 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`；这是**请求配置**证据，不保证服务端实际调度层级。`create_thread`／`send_message_to_thread` 没有服务层参数，派发没有伪造字段。用户已明确允许 tier 未生效时继续开发，但仍须如实报告。当前运行中约 5/15（INT-LT、KT-04、KT-07、KT-10、NB-02；其余安全点任务 idle，状态以各任务最新 turn 为准）；Unity 执行槽只归 INT-LT。此前 P2 工作树仍保留，不作清理。

## 当前依赖与恢复入口

- INT-LT 后续顺序：完成当前 Native/NB-03 安全点（主目录 `eed5376` 已交）→ 精确导入 KT-08 三笔、KT-05 四笔、NB-01 四笔及 NB-02 三笔 → 按接口核对 KT-04、KT-09、KT-07、KT-10。源分支各自保留，不能带旧祖先或覆盖用户脏文件。
- 最大剩余产品缺口：KT-04 物理/封门校验尚未实跑；KT-05 样例未生成，KT-03 的 1 格转角/混合接缝仍无 Play 证据；NB-01/02/03 的正式多实例输入/叙事尚未在新增场景接线；KT-08 尚未产生真实 `.unitypackage`，五 Skill 未在干净工程发现；KT-10 完整示例与 NB-04、S2～S4 尚未完成。不得把当前安全点称为工具包快速版交付。
- 不新建定时任务，不清理用户或旧工作树；S1～S4 只在可交付会合点做必要检查，实际通过范围与未测范围分开。后续再获进度时更新本表，继续保持单 Unity 时段和精确文件租约。
