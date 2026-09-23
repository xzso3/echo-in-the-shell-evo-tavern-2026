# GF01 独立批次状态台账

批次：整体游戏流程与全流程 UI/UX。状态：**开发交付完成，待人工校验；末次平板修复尚无编译／Play 复验证据**。更新日期：2026-09-23。

## 输入与执行边界

- GF01-00 已核对冻结输入为 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`，独立工作树干净。启动入口 `CommanderHome`；本机目标游戏场景 `NativeDemoTilemap`，干净检出回退 `NativeDemo`。共享 Run/Hud/Phone/HomeBootstrap、场景、Build Settings 的最终写入只归 GF01-INT。
- 前 AI 批次代码、场景、平板七张 Sprite 已集成，Unity 导入、编译和保存有交接记录；Play、真实联网及其人工检查未执行。GF01 只复用这些成果，不回写旧 AI 台账，不恢复 UI-01。
- 主目录存在用户未提交 Tilemap／地图、字体、资源、Build Settings 等；独立工作树不得覆盖。`Docs/GameFlow/` 和 `Docs/AICommander/` 输入文档在源工作树可能缺失，执行任务须从主目录绝对路径只读。
- GF01-00 冻结边界已写入提交 `6acb4cd1f112f86138e85ff1e869ec1ba692474a` 的 `Docs/GameFlow/GF01_00_CONTRACT.md`：`Run.Start` 只准备，`BeginAction` 一次进入 Playing／发 Started；GF01-02 持有唯一暂停原因；GF01-03 终局冻结只读快照并主动进入结算；GF01-01 持有导航去重及离局清理；实际应用设置变化才取消请求／合同、使未执行提案失效。现有 Combat 初始化要求 `run.Running`，因此 `BeginAction` 同帧先进入 Playing、初始化成功再发 Started，Intro 不偷跑。
- 集成分支 `codex/gf01-integration` 由 GF01-INT 创建与维护。主控只编辑本批次文档、派发、协调和交付核对；代码、美术、Unity、Git 集成由 GF01 新独立 task 完成。禁止 subagent／spawn_agent／嵌套代理。Unity 始终只由一个任务使用。
- 八个执行任务的本地 session `turn_context` 已逐项核验为 `model=gpt-6-sol`、`effort=xhigh`。当前 `create_thread`／`send_message_to_thread` 工具 schema 不提供 `service_tier` 字段，session 亦未记录此字段，无法实际请求或证明 `priority` 生效；不能把提示词当配置。沿用用户此前授权的缺口处理规则继续。无自动测试、探针、夹具、截图矩阵、中间验收或独立评审；最终人工检查未操作时保持待校验。

## 任务

| ID | 名称 | 状态 | threadId／hostId | 基线／工作树／交付提交 | 下一依赖 |
| --- | --- | --- | --- | --- | --- |
| GF01-00 | 输入基线与接口交接 | 已完成 | `01a0cd9e-e13a-7233-9a73-be01aceca6d1`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/70ec/echo-in-the-shell-evo-tavern-2026`；`6acb4cd` | INT 已精准导入 |
| GF01-01 | 主菜单／开局／离局 | 已提交，待 INT 共享接线 | `01a0cda2-0824-71a2-8eb1-b96002f00a25`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/ac71/echo-in-the-shell-evo-tavern-2026`；`3a0815ff6061cd44a755c982b999b5e03dcdfa49` | INT 接线；05 绑定 |
| GF01-02 | 暂停与局内设置 | 已提交，待 INT 共享接线 | `01a0cda2-55a8-7cb2-acda-6ea7f98ea888`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/4985/echo-in-the-shell-evo-tavern-2026`；`9a7f29c979cc6b027e243d0f620c56d73190ff72` | INT 接线；05 绑定唯一设置 View |
| GF01-03 | 结局／死亡／结算 | 结果组件和击杀聚合已提交，INT 接线中 | `01a0cda2-aa2f-7882-9899-28718eeb1a3b`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/90f9/echo-in-the-shell-evo-tavern-2026`；`3b8025c146f2dcc100e537c70b18e128c52bbb6f`、`b92ba6b33187caa7b486eadf5fa508028f8dd143`；INT 首提交 `c53cd73` | INT 接线并导入击杀修正；06 绑定 |
| GF01-04 | 全流程 UI/UX 与 ImageGen 资产 | 已提交两张草图与四张最终 Sprite，待实际 UI 绑定 | `01a0cd9f-3e76-7cb2-bdb4-8f13efa2e144`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/8098/echo-in-the-shell-evo-tavern-2026`；最终源 `315e2939aebd7190a4b4b7832c8ecff9b0e3a80c`；INT 已接等价资源 `6308463` | INT/05/06 引用最终资源；prompt 在 `GF01_04_ASSETS.md` |
| GF01-05 | 主菜单／开局／暂停 UI | 已提交并导入，INT 接线中 | `01a0cda2-fd9b-7c20-b205-90e60e8e4de3`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/910d/echo-in-the-shell-evo-tavern-2026`；`b144ba781be213edb6d63ebab26bd9bdcd6295a7`；INT 接入 `26ca7d1` | 同一设置 View 两场景复用，INT 绑定资源 |
| GF01-06 | HUD／结局／结算 UI | 已提交，待 INT 接线 | `01a0cda3-56a0-7cf2-96f6-ce47aecf7322`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/5144/echo-in-the-shell-evo-tavern-2026`；`f8e8a620e04925aa15497bd8580ed6dcdfc776df` | INT 接线／资源绑定，说明在 `GameUI/Results/INT_WIRING.md` |
| GF01-INT | 独立集成与批次交付 | 集成代码、资源与交付文档已提交 | `01a0cda3-de98-7a11-9d58-b69a5929cc11`／`local` | `3a7390f`；`/Users/const/.codex/worktrees/3f6d/echo-in-the-shell-evo-tavern-2026`；分支 `codex/gf01-integration`；00=`1e57e83`、01=`64f67a4`、03=`c53cd73`、04等价资源=`6308463`、02=`e75cdc8`、03击杀修正=`b78870b`、06=`51cf596`、05=`26ca7d1`、共享=`8688c9b`、平板刷新修复=`27b8ed8dd4b4097925a32fbe9cc3599d1d298a62`；交付文档已纳入分支 | 最终人工检查待做；修复后编译／Play 无证据 |

## 批次交付

统一版本与 `FINAL_HANDOFF.md` 已在独立分支提交。GF01-INT 使用 Unity 2021.3.27f1c2 完成 `8688c9b` 版本的正常导入和 C# 编译，退出码 0，日志无 C# 编译错误或 Sprite 目录加载错误；四张新 Sprite 的引用资产已由 Unity 保存。其后现有 GUI Editor 中观察到返回菜单时隐藏平板刷新空引用，INT 已以 `27b8ed8` 定点修复；该 Editor 仍持有项目锁，修复后的编译与 Play 复验**尚无证据**。未由本批次执行自动测试、完整 Play、真实联网或最终人工视觉检查。最终人工校验：**待用户或指定操作者执行**。`FINAL_HANDOFF.md` 列出代码／资源提交、场景入口、实际行为、ImageGen 资产与 prompt、一页人工清单和未测范围。旧 AI／Toolkit 记录仅作输入参考。集成工作树尚有 Unity 导入生成的字体资产修改及 `.vscode/`，二者未纳入 GF01 提交；主目录用户未提交文件未覆盖。
