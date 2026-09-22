# Unity Native 集成交接 · P2 本地 NPC 支线

交付主目录：`/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`。分支：`codex/unity-native-docs`；de32 集成 `bb39ca51922e79f4c3c73a5a9fe95a8cc21d667c` 经 `5d0c168f89623dcd452bab21dcbfb5053c241900` 合入主目录且提交树相同。本文件所在提交为当前交接点，`codex/unity-native-integration` 同步到该点。使用 Unity 2021.3.27f1c2 打开此目录，入口 `Assets/Scenes/NativeDemo.unity`，无需执行创建/迁移菜单。

Build Settings 保留 NativeDemo 首位唯一勾选，四个历史场景未勾选但未删除。Windows x64 / Mono 由用户手动构建；本次 P2 集成只做源码、文档和引用静态核对，没有在集成环节重跑 Unity、构建或安装。主目录原有 6 个已跟踪未提交文件及 13 个未跟踪调度文档逐文件核对未变，也未暂存或提交。

## P2 本地 NPC 支线

从旧集成基线 `c8083c1853049d189d94f3c5ea25ee006abb3cfa`，只接入 NPC 原提交 `6daddd38c3f4b7be2cdb50ed3c88c0ba80796019` → 本地 `0e256054b695ab2c73c47510b4017fc10b483085`。P2 文档原提交按 `42deb75d015ba3a83bf4c6c98dae7728cf2a3a16` → `b1240326f8b62fdc120a9096aeec2553090859e7` → `db1bb65877774c221a6cfa836c7587e9fbc620e2` → `c5ba0a5999445f6319f39353a1f7166efd680b58` → `1fc50ef24a9c19ff095f81de50188c7b353de02f` 导入，本地依次为 `26b6593` → `14f000c` → `67f4159` → `518a249` → `198e965`。没有导入旧 P1 祖先或主工作区未提交内容。后续目标、最多五项并发及 9 月 24 日 11:00 截止见 [P2 路线](Framework/Phase2/PHASE2_ROADMAP.md)。

出生点北侧 `(-11, 3)` 的金色「本地档案员」可选支线：靠近按 E，点击 **A：个人记录** 或 **B：路线报告**。A 需取得西南私人记忆，B 需实际走过上方检修通道；先取得证据再选择也有效。选定后另一项锁定，关闭对白，再回到档案员身边按 E 交付；每局仅结算一次，重复交谈只显示完成反馈。手机通讯的任务／本局记录可查看进度，重开清空支线。未接取或未完成不阻挡三记忆、中继、Boss 和最终节点；支线不额外改变同步度、差异度或支援授权。

P2-01 执行者报告 Unity 2021.3.27f1c2 的场景接线/编译命令退出 0；临时 Play Mode 聚焦检查 21 个断言、6 张界面快照：两项均从真实既有证据经 Interaction → ECA → Quest → Dialogue 完成交付，重复交付与改选被拦截，未完成 B 时三段记忆与 relay 仍可继续。新增中文未见缺字或文本高度溢出。证据在 `Assets/NativeGame/README.md`、`/private/tmp/native-npc-connect.log` 和 `/private/tmp/native-npc-smoke.log`；临时探针已从 Assets 移除。检查使用显式定位和按钮回调，并非人工键鼠通关；未重测 Boss／结局全流程，未构建 Windows。

本次静态核对确认场景中 LocalArchivist Prefab、NpcEcaRules、玩家、Quest/Narrative、对白按钮与 HUD 互动表引用存在；FusionPixel 字体及 `Assets/StreamingAssets/ThirdPartyLicenses/FusionPixel/` 许可文件存在。交付后的 Windows 人工试玩请实际靠近 NPC、完成任选一条支线并继续主线；未玩的另一条支线和未走的结局仍标未测。其余 P2 目标与截止时间见 [P2 路线](Framework/Phase2/PHASE2_ROADMAP.md)。本轮后暂停，等待用户再次授权，不启动 P2-03 及后续任务。七个旧开发工作树和 de32 已关闭，分支与 Git 历史保留；非 Git 跟踪的产物及 de32 未提交字体快照在 `/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026/.codex-retired-worktrees/2026-09-23-p2`。

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
