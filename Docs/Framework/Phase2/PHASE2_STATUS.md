# P2 状态台账

更新时间：2026-09-23。状态来源为 P2-01 执行记录、P2-02 集成核对及主控派发。用户已确认 P2 后续优先目标、9 月 24 日 11:00 截止与未完成项移交下阶段，详见 [队列](PHASE2_ROADMAP.md)；尚未人工试玩的项目不记作通过。

## 当前基线

- 交付工作区：`/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。
- 上一集成基线：`c8083c1853049d189d94f3c5ea25ee006abb3cfa`。P2 NPC 原提交 `6daddd38c3f4b7be2cdb50ed3c88c0ba80796019` → 本地集成 `0e256054b695ab2c73c47510b4017fc10b483085`。本文件所在提交为 P2 交接点，`codex/unity-native-integration` 同步到该点。
- P2 文档按原提交 `42deb75d015ba3a83bf4c6c98dae7728cf2a3a16` → `b1240326f8b62fdc120a9096aeec2553090859e7` → `db1bb65877774c221a6cfa836c7587e9fbc620e2` 导入；本地提交依次为 `26b6593` → `14f000c` → `67f4159`。仅导入指定提交，没有引入原分支旧 P1 祖先或主工作区未提交改动。
- 入口 `Assets/Scenes/NativeDemo.unity`，仍为 Build Settings 唯一启用场景；场景 NPC、ECA、对白按钮、玩家及字体引用静态核对完整，FusionPixel 字体和 StreamingAssets 许可文件存在。此工作区未启动 Unity，也未构建 Windows。
- 用户此前反馈新版 Windows 人工试玩总体正常，小问题暂缓；该反馈属于 NPC 合入前版本，具体路线和四象限未逐项确认，不能作为本轮 NPC 验收。
- 主控文档工作区：`codex/unity-native-docs`；用户原有未提交文档、项目设置改动及历史未跟踪材料保持原样。

## 任务

| ID | 状态 | 任务/工作区 | 证据或下一步 |
| --- | --- | --- | --- |
| P2-00 | complete | 主控文档工作区 | P2 规范、任务、验收、主控规则和台账；阶段范围已确认 |
| P2-01 | complete | P1-07 `01a0c9bb-fdbe-7f62-a3d1-8a80fd0fb604`，4113 工作区 | `6daddd3`；Unity 2021.3.27f1c2 场景接线/编译命令退出 0，聚焦 Play Mode 21 断言、6 张快照；A/B 事件链、重复结算保护及未完成 B 时三记忆→relay 通过。检查含显式定位和按钮回调，不是人工键鼠通关；未重测 Boss/结局或 Windows |
| P2-02 | complete | INT-00 `01a0c881-9454-7fa3-a276-cb5756debad3`，de32 工作区 | 指定 NPC 和 P2 文档提交已集成；核对唯一构建场景、Prefab/脚本/按钮/字体/许可引用及交接说明。仅静态核对，未重复启动 Unity |
| P2-03 | queued | P1-07，待最新集成 SHA | 下一项：Chunk/Map，可走区域 |
| P2-04 | queued | P1-05，待最新集成 SHA | Actor/Combat；可与 P2-03 做边界明确的并行代码工作 |
| P2-05—P2-09 | queued | 依 [任务卡](PHASE2_TASKS.md)按序续派 | 道具/装备/Buff → 音效/BGM → ECA 复用 → 叙事/支援 → 在线指挥官 |
| P2-10 | pending | INT-00 与用户 | 截止前集成/用户手动 Windows 试玩：靠近档案员实际选择并交付一条支线，再确认主线可继续；另一条未玩的支线保持未测。未完成项移交下阶段 |

所有可复用任务已收到 GPT-6 Sol / Extra High 的后续运行设置消息；主控后续派发继续显式指定。P1-05、P1-06 当前无 P2 工作，保持待命。

## 检查边界与恢复点

P2-01 证据见 `Assets/NativeGame/README.md`、`/private/tmp/native-npc-connect.log`、`/private/tmp/native-npc-smoke.log`；临时探针已移除。P2-02 只核对集成后的文件和引用，没有把原工作区的运行结果冒称为 de32 重测。A2—A6 的完整人工操作、A8 Windows 路线，以及 Boss/结局全流程仍待用户试玩；P2 整体尚未完成。取得路线文档集成 SHA 与旧工作区收束报告后，从共同基线派发 P2-03 地图、P2-04 战斗、P2-05 道具、P2-06 音频等文件边界分离的任务；INT 保持集成槽，并发总数不超过 5。开发期只做必要编译/启动/当前阻断检查，08:00 停止新增功能，11:00 收束，未完成项转后续阶段。
