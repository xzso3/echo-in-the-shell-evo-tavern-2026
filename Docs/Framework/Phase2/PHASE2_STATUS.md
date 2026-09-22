# P2 状态台账

更新时间：2026-09-23。状态来源为 P2-01 执行记录、P2-02 集成核对及主控派发。用户已确认 P2 后续优先目标、9 月 24 日 11:00 截止与未完成项移交下阶段，详见 [队列](PHASE2_ROADMAP.md)；尚未人工试玩的项目不记作通过。用户现已授权接续主控工作，先前暂停指令结束。

## 当前基线

- 交付主目录：`/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`，分支 `codex/unity-native-docs`。NPC 与 P2 文档集成 `bb39ca51922e79f4c3c73a5a9fe95a8cc21d667c` 经合并提交 `5d0c168f89623dcd452bab21dcbfb5053c241900` 无损迁入；两者提交树相同。P2-03 从该共同基线建立隔离分支，Map 原提交 `231a97c75b0c9aba7fd417ccb4003baf1dbe2b23` → 本地 `4c5758280a6830d955186b39b2862c2e2e96696a`；本文件所在交接提交及 `codex/unity-native-integration` 为新的可玩基线。
- 上一集成基线：`c8083c1853049d189d94f3c5ea25ee006abb3cfa`。P2 NPC 原提交 `6daddd38c3f4b7be2cdb50ed3c88c0ba80796019` → 本地集成 `0e256054b695ab2c73c47510b4017fc10b483085`。
- P2 文档按原提交 `42deb75d015ba3a83bf4c6c98dae7728cf2a3a16` → `b1240326f8b62fdc120a9096aeec2553090859e7` → `db1bb65877774c221a6cfa836c7587e9fbc620e2` → `c5ba0a5999445f6319f39353a1f7166efd680b58` → `1fc50ef24a9c19ff095f81de50188c7b353de02f` 导入；本地依次为 `26b6593` → `14f000c` → `67f4159` → `518a249` → `198e965`。仅导入指定提交，没有引入原分支旧 P1 祖先或主工作区未提交改动。
- 入口 `Assets/Scenes/NativeDemo.unity`，仍为 Build Settings 唯一启用场景；场景 NPC、ECA、对白按钮、玩家及字体引用静态核对完整，FusionPixel 字体和 StreamingAssets 许可文件存在。集成过程未重跑 Unity，也未构建 Windows。
- 用户此前反馈新版 Windows 人工试玩总体正常，小问题暂缓；该反馈属于 NPC 合入前版本，具体路线和四象限未逐项确认，不能作为本轮 NPC 验收。
- 主目录原有 6 个已跟踪未提交文件和 13 个未跟踪调度文档逐文件哈希核对未变，未暂存或提交；备份与已关闭工作区归档在 `/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026/.codex-retired-worktrees/2026-09-23-p2`。原 7 个 P1 工作树及 de32 均已关闭，分支和 Git 历史保留；仅主目录保持检出。

## 任务

| ID | 状态 | 任务/工作区 | 证据或下一步 |
| --- | --- | --- | --- |
| P2-00 | complete | 主控文档工作区 | P2 规范、任务、验收、主控规则和台账；阶段范围已确认 |
| P2-01 | complete | P1-07 `01a0c9bb-fdbe-7f62-a3d1-8a80fd0fb604`，原 4113 分支（工作树已关闭） | `6daddd3`；Unity 2021.3.27f1c2 场景接线/编译命令退出 0，聚焦 Play Mode 21 断言、6 张快照；A/B 事件链、重复结算保护及未完成 B 时三记忆→relay 通过。检查含显式定位和按钮回调，不是人工键鼠通关；未重测 Boss/结局或 Windows |
| P2-02 | complete | INT-00 `01a0c881-9454-7fa3-a276-cb5756debad3`，原 de32（工作树已关闭） | 指定 NPC 和 P2 文档提交已集成；核对唯一构建场景、Prefab/脚本/按钮/字体/许可引用及交接说明。仅静态核对，未重复启动 Unity |
| P2-03 | integrated_dev | `codex/p2-03-chunk-map` 原 `231a97c` → 隔离集成 `4c57582` | 两块同源 NorthRouteChunk、端口相接、Map 门与 E 开关已保存于 NativeDemo。执行者 Unity batchmode 保存/编译及聚焦 Play Mode 退出 0，检查预制体复用、端口、主线引用和实体门阻挡/放行；集成端仅核对文件/引用，未重跑 Unity、人工键鼠或 Windows |
| P2-04 | scene_wired_dev | 原代码 `54dc362` → 本地 `092f537`，原场景 `f37c632` → 本地 `8f4384e` | ArcSentry 位于北区 `(-8,25)`，Unity batchmode 场景接线/编译正常退出；Prefab、攻击脚本与场景引用静态核对。短 Play Mode 已核对敌人与主线接线；实际交战、死亡/重开及 Windows 未测；`8f4384e` 为独立可回退安全点 |
| P2-05 | scene_wired_dev | 原代码 `daa7de3` → 本地 `a7dba48`，原场景 `dd0350b` → 本地 `1f6b8a6` | 第一块北区 `(-12,14)` 的线圈拾取、装备组件和状态条已接 NativeDemo；Unity batchmode 接线/编译正常退出。短 Play Mode 已核对 E 拾取、F 装备、Q 超频效果；实际战斗结果、人工节奏及 Windows 未测；`1f6b8a6` 为独立安全点 |
| P2-06 | scene_wired_dev | 原音频 `484f59d` → 本地 `2f3d731`，原场景 `7422528` → 本地 `b71a911`，补字形 `5d94c0c` → 本地 `c98e037` | NativeDemo 根级唯一 AudioDirector、四段自制 WAV、一个 AudioListener 与运行引用静态核对；Unity batchmode 导入/接线/编译正常退出。短 Play Mode 已核对 BGM AudioSource 循环启动；声音听感、F5—F8 音量操作及 Windows 未测 |
| P2-07 | integrated_dev | `codex/p2-07-passage-eca` 原代码 `25e876b` → 本地 `ca8f40e`，原场景 `048c613` → 本地 `2081d90` | 两项 PassageRule 已保存于 NativeDemo，终端与北侧开关各绑定一次。执行者 Unity batchmode 编译/保存退出 0，聚焦冒烟确认 0/3 记忆阻断、中继 Quest/Map 开门和北侧 Map 门开关；集成端仅静态核对，人工键鼠、Windows、Boss/结局未测 |
| P2-08 | integrated_dev | `codex/p2-08-north-memory` 原代码 `cf01980` → 本地 `d001bce`，字形预热 `fb6711f` → `974180a`，场景 `c44fcee` → `192e99e` | 北侧第二块 `(-12,22)` 定向脉冲终端已接现有 ArcSentry、Combat、Narrative、对白与手机。源任务离线编译 0 错误，Unity batch 保存/编译正常退出；聚焦 Play `P2_08_SMOKE_PASS` 验证拒绝/离距失败无代价、真实命中后同步+20/行为一次、重复保护和手机记录。集成端静态核对；敌人已死/关卡结束分支、人工键鼠、四结局及 Windows 未测 |
| P2-09 | in_progress_prep | 新执行任务 `01a0cac5-8b8a-7283-b219-0a36c38bcfd4`，工作树 `9355` | 从 `2fbe16b` 起步；先做安全节点自由输入、受限代理请求和离线回退的服务商无关部分。实际服务地址、凭据和在线回复仍待用户提供/选择，不能把预设或模拟回复记作在线通过 |
| P2-10 | pending | INT-00 与用户 | 截止前集成/用户手动 Windows 试玩：靠近档案员实际选择并交付一条支线，再确认主线可继续；另一条未玩的支线保持未测。未完成项移交下阶段 |

本轮新执行任务均指定 GPT-6 Sol / Extra High；Unity Editor 槽由当前场景接线任务协调。P2-04—P2-06 的代码与场景实例均已接入，但实际交战、装备效果和音频听感尚未通过人工试玩；不能将场景接线等同于 Windows 验收。用户先前要求的暂停已由本次接续指令解除。自动审批拒绝关闭四个已完成工作树，理由是潜在未跟踪材料或历史证据丢失风险；只读审计与非缓存材料归档已完成，工作树原样保留，清单见 `.codex-worktree-audits/2026-09-23-p2-slices/README.md`。每 30 分钟自动唤醒续派也被自动审批拒绝，尚未设置；在用户明确授权前，不假设无人值守运行。

## 检查边界与恢复点

P2-01 证据见 `Assets/NativeGame/README.md`、`/private/tmp/native-npc-connect.log`、`/private/tmp/native-npc-smoke.log`；临时探针已移除。P2-02 只核对集成后的文件和引用，没有把原工作区的运行结果冒称为 de32 重测。A2—A6 的完整人工操作、A8 Windows 路线，以及 Boss/结局全流程仍待用户试玩；P2 整体尚未完成。路线文档已入主目录，旧工作区已安全收束。P2-03 源任务从 `4d64c4c` 起步，先只选择地图 `231a97c` 形成 `fabefdc`；随后按父链精确导入 P2-04 代码/场景、P2-05 代码/场景、P2-06 音频/场景。源场景任务最新短 Play Mode `/private/tmp/p2-integrated-play.log` 报 `NATIVE_INTEGRATED_SMOKE: PASS`，涵盖物理穿越、Map 门、装备拾取/效果、音频启动及敌人与主线接线；集成端只做静态核对，未重跑 Unity 或构建 Windows。P2-07 从主目录文档 HEAD `27df239` 精确导入代码与场景提交，不引入其他执行分支；P2-08 以 P2-07 集成 `a99f8e2` 为基线，按源父链精确导入代码、字形预热和场景三笔提交；本文件所在交接提交与集成引用是后续共同基线。开发期只做必要编译/启动/当前阻断检查，08:00 停止新增功能，11:00 收束，未完成项转后续阶段。
