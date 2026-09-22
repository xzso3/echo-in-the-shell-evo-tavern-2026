# Unity 原生路线安全检查点（U0）

2026-09-23。当前执行路线仅见 UNITY_NATIVE_PLAN.md；旧 INT/P1 工作停止，不续写 world、Tools 或旧验收。

- 文档分支从主工作区原 HEAD `371fcd6` 新建 `codex/unity-native-docs`；原生集成分支 `codex/unity-native-integration` 从本次文档提交创建。
- 旧 accepted `58e281af3ea0b0d7673d300499021b2753cf72eb`、未验证 world `be40cd3aa4108767c910b3cdc57799458a36caf1`、旧战斗 `8325ab1` 及全部历史分支/worktree保留，未重置、删除或合入新路线。
- 修订前文档、tracked.patch、refs、worktrees 已由主控保存至 `/private/tmp/eits-native-checkpoint-20260923-002144`。此处记录摘要定位；不另做大型归档。
- 已将当前 Docs 与 docs-before.tar.gz 逐文件比较。两份 Boss 文档、项目进度文档的既有未提交改动原样保留；README只暂存原生路线段落，其他既有改动不混入本提交。主控未提交的 Orchestration 目录仍由主控维护，未无差别暂存。
- 当前主工作区保持用户原有内容；不reset/stash，不push。U1独占Unity运行槽；U0只负责Git和模块准备，本检查点不证明Windows构建或试玩通过。

备份文件 SHA-256：

- `docs-before.tar.gz`: `1df08fbe1540f81f6c46d11923dd0fa5bd8e12313b01a5eb1b3049c00e867595`
- `refs.txt`: `30e13f3e22b3984105e56178e6281c0c7ffd1634b5347ddb71f3c54f4450e21d`
- `status.txt`: `de327744693c9a43214773f694ad130bc492c7239759c3192e5cf8f92faa4e60`
- `tracked.patch`: `a76de8fc85dbe0c0a4114ecaf5d5441cd1c5543bd806a1639e69839fa264c889`
- `worktrees.txt`: `e8e20aeb4b9a4fba1a6ca018d329e1e17ae322b9c6e236493dc2ed869c38eb77`
