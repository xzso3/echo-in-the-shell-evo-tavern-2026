# AI 指挥官与战术平板阶段状态

更新日期：2026-09-23。主控只负责文档、派发、协调和交付核对；代码、Unity 操作与 Git 集成由独立 task 完成。原 UI-01 已撤销，本阶段设计任务使用 UI-02。

## 启动基线与用户改动

- 当前工作目录：`/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`；分支 `codex/unity-native-docs`；启动 HEAD `131c6d4ceeaa75db208cef2f61fea9196b9e27d4`。不得沿用旧 P2 或 Toolkit 的固定 SHA。
- 启动时已有跟踪文件修改：`Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset`、`Assets/NativeGame/README.md`、两份 Boss 文档、`Docs/Framework/Orchestration/PHASE1_STATUS.md`、`Docs/README.md`、`Docs/项目进度说明.md`、`ProjectSettings/EditorBuildSettings.asset`、`ProjectSettings/ProjectSettings.asset`。这些均视为用户现有改动，不归本阶段任务提交。
- 启动时已有未跟踪内容：`AGENTS.md`、`Docs/AICommander/`（本阶段输入文档及 References）、关卡工具包地图／作品内容、Tilemap Demo 场景和素材、`Captures/NativeTilemap/`、`Deliverables/LevelToolkit/`、`Docs/LevelToolkit/`、`Docs/Framework/Orchestration/` 及本地工作树审计／归档目录。执行任务从上述 HEAD 创建隔离工作树，并从主目录绝对路径只读阶段文档与 References；不得清理、移动、覆盖或捆绑这些未提交内容。
- 共享文件归属：`NativePhone`、`NativeHud`、正式场景、首页接线与最终 Unity 保存均由 INT-AI 独占；`NativeSupportController`／`INativeSupport` 由 AI-03 负责必要改动；`NativeRunController`／`NativeCommanderSafeNode` 由 AI-04 负责。Unity 同时只给一个任务使用，不执行旧 NativeDemo 一次性重建／迁移菜单。
- 派发后主目录 HEAD 由既有主控任务前进到 `281bbe8458b2bc59b2bfde7604b748a5880b1256`，该提交只修改 `Docs/LevelToolkit/STATUS.md`，不是本阶段的 AI-00 基线。INT-AI 从此实时 HEAD 建隔离集成分支，再精确导入 AI-00 和后续切片；启动时用户未提交内容仍须保留。

## 调度与能力核验

- 独立 task 固定 `model: gpt-6-sol`、`thinking: xhigh`；不使用 subagent、spawn_agent 或嵌套代理。最新用户授权并发上限 15，实际只按文件独立性和依赖启用。AI-00、AI-01～04、UI-02、UI-03、UI-03B、INT-AI 的本地 session `turn_context` 已逐项核对为 `model=gpt-6-sol`、`effort=xhigh`。
- 请求级 `service_tier: priority` 是目标。当前 Codex `create_thread`／`send_message_to_thread` 工具模式提供 model 和 thinking，但不提供 service_tier 字段；上述 session `turn_context` 也未记录 service_tier。故无法证明 priority 生效，不在 prompt 中伪称生效。此前用户已授权 priority 未生效仍继续。
- 不写／不跑自动测试、探针、夹具或回归矩阵；不设中间验收／独立评审轮次。正常 Unity 导入和编译错误由对应任务直接修复。只保留 `EXECUTION.md` 的最后一次人工校验，未操作时标“待人工校验”。

## 任务登记

| 任务 | 状态 | threadId | 提交／接入点 |
| --- | --- | --- | --- |
| AI-00 最小接口 | 已提交；隔离工作树 `/Users/const/.codex/worktrees/2967/echo-in-the-shell-evo-tavern-2026` | `01a0cd1b-edb7-7a40-98f5-77cec2d70c3d` | `d1bbea1f0aacb032d4d98cd422703df1526fc6e2`；`Commander/Contracts`、`AI00_CONTRACT.md` |
| AI-01 配置与 HTTP | 已提交，`8229` worktree | `01a0cd26-23ba-7ad0-aa27-ce5ed6a38f7b` | `fdbbbd4a95d456434cb4abd90ac74785cf0513e8`；Configuration、Transport、.meta；已派 INT 合入 |
| AI-02 状态与会话 | 已提交，`054f` worktree | `01a0cd27-10dd-7ef2-b174-55d6202e16dd` | `feaf4f71fde228fee8159cfd39b2c46745c290a8`；Commander/Context、Conversation、.meta；已派 INT 合入 |
| AI-03 支援桥接 | 已提交，`fbc5` worktree | `01a0cd27-2c52-7a80-b577-c40f5a89ce28` | `6c64ee0d981875c8dc503c29830a9b04855386d1`；Commander/Support 与 Support 权威接口；已派 INT 合入 |
| AI-04 暂停与生命周期 | 已提交，`d7f9` worktree | `01a0cd27-4527-7c11-9c40-ab721ffe342b` | `2da6b5f`；Commander/Pause、Run/SafeNode、Native 与 Toolkit Combat 守卫；已派 INT 合入 |
| UI-02 设计与最终资源 | 已提交，`c10c` worktree | `01a0cd27-6848-7933-b336-4cbf7d9ef252` | `dd4ddbc095866e5bed2f4820c2ae425f6bb60a86`；七张 Art Sprite、.meta、`UI02_ASSETS.md`；已派 INT 合入并实际绑定 |
| UI-03 原生首页与平板 | 首页与通用组件已提交，`dd35` worktree | `01a0cd27-81bb-76b0-9cee-ae047b5268cf` | `0436dda`；HomeView、UiFactory、PhoneUiElements、.meta 与接线说明；平板主体由 UI-03B 交，已派 INT 合入 |
| UI-03B 平板主体收敛切片 | 已提交，`0fa4` worktree | `01a0cd46-0942-7742-a598-93c199c00fce` | `1b772d23adbd37dc230c129b702f56db1c4bd71f`；只新增 `CommanderTabletView.cs`、.meta 和接线说明；已派 INT 合入 |
| INT-AI 串行集成 | 已合入主目录；开发完成、待人工校验 | `01a0cd29-ca07-7410-b67f-4bc38ff1f797` | 核心集成提交 `9bbb86e83d1daf868e1d577fd48994ba86b74aa0`；`PhoneUI.meta` 保留先有 GUID `0baee393c64f435eafcad45f8a4651fb` |
| INT-AI-TILEMAP 场景补接 | 已完成；本机 Tilemap 场景待人工校验 | `01a0cd29-ca07-7410-b67f-4bc38ff1f797` | 主目录 HEAD `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；本机 `NativeDemoTilemap.unity` 已有平板对象与七张 Sprite 引用，原 GUID `b2325c2a6efb54237952e6f13ab944f8` 不变；场景、.meta、地图依赖仍为用户未提交状态 |

## 最终交付

统一代码与资源版本、启动入口、配置方式、功能范围、资源位置和一页人工检查单由 INT-AI 汇总，主控核对实际差量与提交。人工未操作时准确标记“待人工校验”。

本轮最终交接见[交付与人工检查单](FINAL_HANDOFF.md)。Unity 2021.3.27f1c2 正常导入、C# 编译及场景保存已完成；未编写或运行自动测试，也未执行人工 Play／真实联网。主目录 Build Settings 现在为 `CommanderHome → NativeDemoTilemap → NativeDemo`；Tilemap 条目仍是用户未提交修改，其他启动时用户脏文件和未跟踪内容也未捆绑进本轮提交。首页在本机进入 `NativeDemoTilemap`；没有本地 Tilemap 场景的干净检出回退 `NativeDemo`。

## 阶段关闭与远端交接

- 已推送 `origin/codex/ai-commander-integration`，远端 HEAD `3a7390f7258e73eec38a42098d33cb9a17dc3cab`；[PR #1](https://github.com/xzso3/echo-in-the-shell-evo-tavern-2026/pull/1) 于 2026-09-23 合入 `main`，merge commit `80e44c2342c7f4fa835c776904c684bd82233832`。相对原始 `origin/main` 基线 `a17f8c207af9c1f5119288b8ed64113e1ba8ac58`，PR 含 124 个提交、612 个文件，覆盖早期 Native/LevelToolkit 与本轮 AI 指挥官。
- 本阶段 8 棵源 worktree 及 INT-AI 集成 worktree 均先备份、再关闭；对应执行 task 均已归档。本地清理任务也已归档。备份及恢复说明位于 `.codex-retired-worktrees/2026-09-23-ai-commander/`，为私有未跟踪目录，不应上传。
- PR 只含已提交内容。主目录仍未跟踪的 `NativeDemoTilemap.unity`、Tilemap 地图依赖、References／阶段主控文档及其他用户改动没有上传；干净检出会由首页回退至已提交的 `NativeDemo`。本机 Tilemap 集成仍在原工作目录，人工 Play 与真实联网待校验。

首次 AI-00 worktree 创建只返回排队中的 `clientThreadId=client-new-thread:708e638b-5740-4679-9c9f-08e52077be31`，数分钟未产生正式 threadId；已存在干净隔离目录后，改用上述可跟踪的独立 local task 并明确只准在隔离目录操作。若前一排队任务随后出现，须停止重复写入。

AI-00 排队任务随后正常完成，实际 threadId 为 `01a0cd1b-edb7-7a40-98f5-77cec2d70c3d`；恢复任务 `01a0cd21-cc68-7ad1-aa64-ded2b0b8e1bd` 发现同目录已有提交后停止写入。AI-00 的契约、DTO 及差量已核对。六条实现任务均从 AI-00 提交派发，正式 threadId 待 worktree 创建完成后回填。
