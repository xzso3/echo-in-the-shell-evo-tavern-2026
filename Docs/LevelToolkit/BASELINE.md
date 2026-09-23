# 关卡工具包启动基线

记录时间：2026-09-23 11:34（UTC+8）。本文件只记录启动时观察到的状态，不表示 KT-00 已完成。

## Git 与 Unity

- 主目录：`/Users/const/Projects/Unity/echo-in-the-shell-evo-tavern-2026`。
- 当前分支：`codex/unity-native-docs`；HEAD 与 `codex/unity-native-integration` 同为 `7e274fab32ef6a1a986853c97cef76de3c829d4c`。
- Unity UI 显示已打开 `NativeDemo - echo-in-the-shell-evo-tavern-2026`，Unity `2021.3.27f1c2 Personal`，场景 URL 指向主目录 `Assets/Scenes/NativeDemo.unity`。这是用户当前打开的工程；执行任务不得关闭或占用这个 Editor。沙箱不允许 `ps`，故未通过进程列表补充核验。
- P2 按用户最新指示暂缓；已有 P2 工作树原样保留。此前被自动审批拒绝的工作树移除与定时唤醒均不执行。

## 启动时未提交内容

已跟踪修改：`Docs/Boss关卡实现与验证.md`、`Docs/Boss战开发计划.md`、`Docs/Framework/Orchestration/PHASE1_STATUS.md`、`Docs/README.md`、`Docs/项目进度说明.md`、`ProjectSettings/ProjectSettings.asset`。

未跟踪：`.codex-retired-worktrees/`、`.codex-worktree-audits/`、`AGENTS.md`、`Docs/LEVEL_DESIGN_TOOLKIT_REQUIREMENTS.md`、整个 `Docs/LevelToolkit/`，以及 `Docs/Framework/Orchestration/` 下既有的 `DISPATCH_LOG.md`、`P03_*`、`P04_*`、`P05_*`、`P06_*`、`P1-00/01/02/05/06/07_DISPATCH.md`、`P102_NULLABLE_PENDING.md`。本轮只新增本工具包主控文档，不暂存、清理、覆盖或提交上述其他内容。

指定输入文件的 SHA-256：

| 文件 | SHA-256 |
| --- | --- |
| `AGENTS.md` | `2a27932f7eeadc7d55b4656b8a0c1d5b86c63ede5f421d44ad065df31fa0499b` |
| `Docs/LEVEL_DESIGN_TOOLKIT_REQUIREMENTS.md` | `5dd7986c30b88bc852c659f6e454c3f96bb9407d336c11d1383b94f5b5abe1b7` |
| `Docs/LevelToolkit/DEVELOPMENT_PLAN.md` | `acf35f2d15de1c4b7339fbe427991515e3bf099a696a6611912287cfc9298019` |
| `Docs/LevelToolkit/NATIVE_GAP_PLAN.md` | `81b3f1b105a3ec311afd0094c504cceb4ea12ba3745bc7833892f9e72e567c65` |
| `Docs/LevelToolkit/TASKS.md` | `20414e76542a87b6c657cac59948f913593da68ef20e1fd5deb599b1e6e20610` |
| `Docs/LevelToolkit/PARALLEL_EXECUTION.md` | `b3642fd69dcbf271dcea3685c92c867f621fea3654299c668f85a427a898d786` |
| `Docs/LevelToolkit/ACCEPTANCE.md` | `434545b948ac1a66b78a9f0575de3823e19e17217c1d1b7352c5c7ddbe3dd942` |

## 模型与服务层初查

- 本机用户级 `/Users/const/.codex/config.toml` 当前有 `service_tier = "priority"`；本主控任务的本地 `event_msg.payload.thread_settings` 显示 `model=gpt-6-sol`、`reasoning_effort=xhigh`、`service_tier=priority`。
- 较早的 P2 执行任务日志曾显示 `service_tier=default`，当时配置文件尚未更新。该旧证据不能替新任务背书；每个新执行任务启动后必须检查自己的实际 `thread_settings`，未核验前不放行实现。
- 可见的 `create_thread`／`send_message_to_thread` 工具没有 `service_tier` 参数；不得伪造字段或仅凭派发文字声称 priority 生效。实际服务端排队层级不可从本地日志保证。
