# Unity Native 集成交接 · P2 本地 NPC 支线

交付主目录：`/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`。分支：`codex/unity-native-docs`；de32 集成 `bb39ca51922e79f4c3c73a5a9fe95a8cc21d667c` 经 `5d0c168f89623dcd452bab21dcbfb5053c241900` 合入主目录且提交树相同。本文件所在提交为当前交接点，`codex/unity-native-integration` 同步到该点。使用 Unity 2021.3.27f1c2 打开此目录，入口 `Assets/Scenes/NativeDemo.unity`，无需执行创建/迁移菜单。

Build Settings 保留 NativeDemo 首位唯一勾选，四个历史场景未勾选但未删除。Windows x64 / Mono 由用户手动构建；本次 P2 集成只做源码、文档和引用静态核对，没有在集成环节重跑 Unity、构建或安装。主目录原有 6 个已跟踪未提交文件及 13 个未跟踪调度文档逐文件核对未变，也未暂存或提交。

## P2 本地 NPC 支线

从旧集成基线 `c8083c1853049d189d94f3c5ea25ee006abb3cfa`，只接入 NPC 原提交 `6daddd38c3f4b7be2cdb50ed3c88c0ba80796019` → 本地 `0e256054b695ab2c73c47510b4017fc10b483085`。P2 文档原提交按 `42deb75d015ba3a83bf4c6c98dae7728cf2a3a16` → `b1240326f8b62fdc120a9096aeec2553090859e7` → `db1bb65877774c221a6cfa836c7587e9fbc620e2` → `c5ba0a5999445f6319f39353a1f7166efd680b58` → `1fc50ef24a9c19ff095f81de50188c7b353de02f` 导入，本地依次为 `26b6593` → `14f000c` → `67f4159` → `518a249` → `198e965`。没有导入旧 P1 祖先或主工作区未提交内容。后续目标、最多五项并发及 9 月 24 日 11:00 截止见 [P2 路线](Framework/Phase2/PHASE2_ROADMAP.md)。

出生点北侧 `(-11, 3)` 的金色「本地档案员」可选支线：靠近按 E，点击 **A：个人记录** 或 **B：路线报告**。A 需取得西南私人记忆，B 需实际走过上方检修通道；先取得证据再选择也有效。选定后另一项锁定，关闭对白，再回到档案员身边按 E 交付；每局仅结算一次，重复交谈只显示完成反馈。手机通讯的任务／本局记录可查看进度，重开清空支线。未接取或未完成不阻挡三记忆、中继、Boss 和最终节点；支线不额外改变同步度、差异度或支援授权。

P2-01 执行者报告 Unity 2021.3.27f1c2 的场景接线/编译命令退出 0；临时 Play Mode 聚焦检查 21 个断言、6 张界面快照：两项均从真实既有证据经 Interaction → ECA → Quest → Dialogue 完成交付，重复交付与改选被拦截，未完成 B 时三段记忆与 relay 仍可继续。新增中文未见缺字或文本高度溢出。证据在 `Assets/NativeGame/README.md`、`/private/tmp/native-npc-connect.log` 和 `/private/tmp/native-npc-smoke.log`；临时探针已从 Assets 移除。检查使用显式定位和按钮回调，并非人工键鼠通关；未重测 Boss／结局全流程，未构建 Windows。

本次静态核对确认场景中 LocalArchivist Prefab、NpcEcaRules、玩家、Quest/Narrative、对白按钮与 HUD 互动表引用存在；FusionPixel 字体及 `Assets/StreamingAssets/ThirdPartyLicenses/FusionPixel/` 许可文件存在。交付后的 Windows 人工试玩请实际靠近 NPC、完成任选一条支线并继续主线；未玩的另一条支线和未走的结局仍标未测。其余 P2 目标与截止时间见 [P2 路线](Framework/Phase2/PHASE2_ROADMAP.md)。用户现已授权恢复 P2；后续功能按依赖与独立提交继续集成。七个旧开发工作树和 de32 已关闭，分支与 Git 历史保留；非 Git 跟踪的产物及 de32 未提交字体快照在 `/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026/.codex-retired-worktrees/2026-09-23-p2`。

## P2-03 北侧 Chunk / Map

P2-03 原提交 `231a97c75b0c9aba7fd417ccb4003baf1dbe2b23` → 隔离集成 `4c5758280a6830d955186b39b2862c2e2e96696a`，仅导入该提交。出生点往北从 `(-10, 9)` 开口进入第一块；两块同源 `NorthRouteChunk` 在 `(-10, 19)` 连接，靠近 `(-10, 16.5)` 的开关按 E 后，`NativeMap` 关闭实体门阻挡，玩家可进入第二块并原路返回。原东侧三记忆、中继、Boss 路线保留。场景已保存，直接 Play；不要重复执行一次性接线菜单。

执行者在 Unity 2021.3.27f1c2 的 batchmode 保存/编译日志 `/private/tmp/p2-03-build-escalated.log` 显示 `NATIVE_CHUNK_BUILD` 且正常退出；聚焦 Play Mode 日志 `/private/tmp/p2-03-smoke.log` 显示预制体复用、端口对齐、主线引用及实体门阻挡/放行通过并正常退出。本次集成仅静态核对两实例、端口、Map/ECA/HUD 引用和唯一构建场景，没有重跑 Unity。玩家完整键鼠路线与 Windows 构建仍待测。`fabefdc` 保留为独立地图安全点；后续接线见下节。

## P2-04—P2-06 已接线切片

按源提交 `54dc362` → `f37c632` → `daa7de3` → `dd0350b` → `484f59d` → `7422528` → `5d94c0c` 精确接入，本地依次为 `092f537` → `8f4384e` → `a7dba48` → `1f6b8a6` → `2f3d731` → `b71a911` → `c98e037`。末项补齐“脉／伤／频”字形。`8f4384e` 是 ArcSentry 场景安全点，`1f6b8a6` 是脉冲线圈场景安全点；没有重复导入另一个 P2-04 原代码提交 `f6b8580`。

第二块北区 `(-8,25)` 有绕射移动并预警三连弹的 ArcSentry；第一块北区 `(-12,14)` 有金色脉冲线圈，靠近按 E 拾取，F 装备/卸下（伤害 +12），Q 启动 8 秒超频（射击间隔乘 0.6）。NativeDemo 根级接入唯一 AudioDirector，四段本项目自制占位 WAV 对应循环 BGM、互动、受伤和击败；F5/F6 调节音乐、F7/F8 调节音效。三个一次性接线菜单均已执行并保存场景，不要重复运行。

执行者的 `/private/tmp/p2-04-connect.log`、`/private/tmp/p2-05-connect.log`、`/private/tmp/p2-06-connect.log` 均显示对应场景引用接线和 Unity batchmode 正常退出。本次隔离集成又静态核对三种 Prefab 场景实例、装备/互动/音频引用、唯一 AudioListener、四个有效 PCM WAV 与唯一启用构建场景，未重复运行 Unity。源场景任务的短 Play Mode `/private/tmp/p2-integrated-play.log` 报 `NATIVE_INTEGRATED_SMOKE: PASS`，覆盖物理穿越、ECA 门、装备 E/F/Q 效果、BGM AudioSource 循环启动及敌人/主线接线；临时探针已从 Assets 移除。集成端未重跑 Unity。ArcSentry 的实际交战及死亡/重开、线圈在人工战斗中的伤害/射速表现、声音听感和音量键、Boss/结局全程及 Windows 构建仍待测，不能把聚焦检查冒称为人工通过。

## P2-07 两处通路 ECA 复用

P2-07 原代码 `25e876b2620ed22b2324c200531ad03be3d97c95` → 本地 `ca8f40e`，原场景 `048c6139fc8cef5f2fda1c59a32682d0ece9f5b1` → 本地 `2081d9035b16206f320b36fcd7014b8432bcf11f`。`NativeDemo` 的现有中继终端和北侧开关分别配置一个 `PassageRule`，共同处理 `Confirmed` 事件；规则只读 Level/Quest/Map 条件，门、任务和叙事结果仍由各自组件提交。旧终端/北侧专用事件处理器不再并行订阅，避免重复结算。场景已保存，无需运行一次性接线菜单。

执行者的 `/private/tmp/p2-07-unity-connect.log` 和 `/private/tmp/p2-07-unity-smoke.log` 显示 Unity 2021.3.27f1c2 batchmode 正常退出；聚焦冒烟以程序交互确认 0/3 记忆时中继阻断、收齐后三记忆的 Quest/Map 结果及北侧 Map 门开启。集成端核对仅五个授权文件的差量、两条规则对现有引用的映射、无重复旧订阅及 NativeDemo 唯一构建入口，没有重跑 Unity。人工移动按 E、Windows、Boss/结局全程仍待测；详见 [P2-07 接线说明](Framework/Phase2/P2-07_PASSAGE_ECA.md)。

## P2-08 北侧定向脉冲

P2-08 从 P2-07 交接点 `a99f8e296657eae45cff6aacbae4c891368b6e85` 顺序导入原代码 `cf01980c121fc73364685ba9c4c9493790e10eae` → 本地 `d001bce`、字形预热 `fb6711f8cafcafa3e4b7fbd29728d1399ec7f67d` → `974180a`、场景 `c44fceec0b6cf46393463123dd6f247ba3062313` → `192e99e`，未导入旧祖先或临时测试探针。

走进第二块北侧区块，在 `(-12,22)` 的紫色终端旁按 E，选择 **执行脉冲** 或 **拒绝**。执行通过 Combat 对仍存活的 ArcSentry 造成最多 48 点伤害；只有实际降低目标生命后，Narrative 才记录一次行为并使同步度 +20，不共享记忆。拒绝或离开终端后确认不扣费，成功后手机的通讯／本局记录可查看结果。原手机医疗、弱点解析与四结局阈值保持；终端已保存于场景，不要重复执行接线菜单。

源任务 `/private/tmp/p2-08-compile.log` 离线编译 0 错误，`/private/tmp/p2-08-connect.log` 的 Unity batch 保存/编译正常退出；`/private/tmp/p2-08-smoke.log` 报 `P2_08_SMOKE_PASS`，覆盖拒绝、离距失败、真实 Combat 命中后一次性代价、重复保护、手机记录及旧支援/阈值。该聚焦检查使用显式定位和按钮回调。集成端仅静态核对终端、ArcSentry、HUD、Narrative、手机与字体引用及 NativeDemo 唯一构建场景，未重跑 Unity。敌人已死／关卡结束分支、人工键鼠、四结局完整路线和 Windows 仍待测。

## 既有 U4 路线与证据

以下保留 NPC 合入前的 U4 操作说明。U4 原源码集成提交 `47c487759ee191fdb8b4dc34c947f872d512aacc`；中文差量 `f3bef9d`→`6115f13`、`a8f5910`→`749e0d1`、`e4891ed`→`800242b`，译文原 `8a2cb96` 未重复合入；主控两文档提交 `50f1eeb`→`d6fdc99`。

NativeDemo 及全部依赖游戏界面已统一 FusionPixel／简体中文，保留 WASD、Space、E、Tab、Enter 键位英文；场景与 Boss Prefab 已迁移，不需要重跑菜单。旧历史场景不在本试玩构建。字模与源 OTF 随包，字体 LICENSE 通过 StreamingAssets 携带。

## 操作与一局路径

- WASD 移动；Space 切换自动开火；E 近距离互动/确认当前对白。Tab 打开手机，Escape/按钮关闭；手机和对白不暂停战斗。
- 收集西南私人记忆、东北系统记录、东南系统初始回声；上方 service path 可实际绕行，下方有守卫。收齐三段回青色 relay 按 E，再进东侧 Boss 区。
- 躲三连射和圈轰炸，破壳后靠近核心按 E；普通窗口6秒，错过8秒后重开。真实击败开启最终门，最终节点 E 后在范围内选 写入 / 销毁；该按钮不改变本局象限分数。
- 手机 通讯／支援／网络 三页签可滚动。支援选择医疗支援／弱点解析、有限授权／深度授权 和记忆，查看合同 后使用新的 Enter 或 接受并执行 确认，也可拒绝；不是 E/Space。医疗需实际缺血才有效；解析只延长真实核心窗口，不替代破壳或核心 E。每种支援每局成功一次，失败/拒绝不收费。
- 归档／逃逸／同化／新生四象限由实际同步/差异形成；通讯页“保留异议”、网络页“改写回声”是明确的计分选择。新生白屏等待新的 E 第一拳；随后不再输入也应自主打出最后一拳，黑屏约2秒后手机接续。死亡/结局/接续手机可 重新开始。

四象限具体可达路线、计分和系统职责见 [详细操作说明](../Assets/NativeGame/README.md)。不要用最终按钮代替整局行为，也不把脚本到达四结局当作人工通关。

## 既有 U4 集成与人工边界

从 `b742811` 保留既有 U3。两份状态文档主工作区提交 `83337bd` → 集成 `292cff5`；顺序精确导入 U4：`90a3ab9`→`528629b`，`993568c`→`d87a8ac`，`3f7bebd`→`c2d28ef`，`d29b004`→`47c4877`。支援原 `d24ecee` 与 `3f7bebd`等价，没有重复合入；没有合入旧 P1 祖先或 world WIP，完整旧分支/产物保留。

用户已确认旧 `66ea557` 在 Windows 启动、移动、开火切换、终端和重开无错误。该证据不外推到中文 U4 完整流程。执行者报告其原工作区 support-connect / smoke / visual / final-compile 退出0；检查含显式定位/脚本移动，前三象限经 Combat 加速布置战斗，非人工通关或五分钟节奏证明。本轮没有重复执行这些检查。

字体执行者另报告：39文字组件统一字体、561字符覆盖无缺字、25展示状态/588组件检查未见缺字/混用/溢出，移除临时脚本后最终编译退出0。这是原工作区排版展示检查，不是玩法或人工通关验证，本轮不重复执行。详情见 [字体证据](../Assets/NativeGame/Fonts/INTEGRATION.zh-CN.md)。

Windows 人工试玩还应记录真实启动与 WASD/Space/E/Tab、完整三记忆→Boss→最终节点通关、死亡及结局重开、支援确认/拒绝、四象限实际走到的结果，以及新生第一拳等待/自主末拳/黑屏后手机接续。只按真正试玩的路线记录通过；尚未试玩的象限保持未测。遇严重问题请附所在节点、操作、现象及截图/错误日志。

已知限制：静态角色图、简化 Canvas 方块拳与裂线、无声音；记录仅本局内存，重开清空。在线 LLM、外置手机、全服服务与真实继承链未实现。难度、滚动手感和约五分钟节奏仍待人工试玩。

旧 .NET bin/obj 产物位于 Assets 外，原样保留不暂存；本轮不运行旧验收，不清理历史产物。
