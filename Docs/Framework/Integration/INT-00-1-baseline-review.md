# INT-00 / attempt 1：集成起始基线审查

日期：2026-09-22。结果：ready_for_review，仅文档基线与工作区隔离检查通过。

## 起点与所有权

- worktree：`/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。
- 起点 HEAD：`fb3376eb75cb329dcda83dde00018439a0f000a2`，最初 detached；与指定 base_commit 精确匹配。
- 父提交：`a17f8c207af9c1f5119288b8ed64113e1ba8ac58`。
- `codex/p1-bootstrap` 指向指定基线，由保存项目占用。
- 创建前 `codex/p1-integration` 不存在，随后在本工作区创建；仅 INT-00 按主控明确续派推进。
- 初始 `git status --porcelain=v1 --untracked-files=all` 输出为空。本轮只新增本目录的两份证据；交付提交包含证据，不改变原八文件。
- 适用祖先文件 `/Users/const/.codex/AGENTS.md` 为空；工作区内未发现 AGENTS.md。未修改保存项目、其他工作区、Orchestration、实现、测试或配置。

## 文档审查

完整阅读核心设计、架构记录、五份 PHASE1 文档及 README。基线相对父提交恰好涉及八个 Markdown 路径：七份新增文档及 README 的七行索引增加；无删除、实现或历史文档变更。逐文件 SHA-256 与链接明细见同目录 JSON；工作树内容逐字节等于指定基线 Git blob。

41 个相对 Markdown 链接全部指向存在的文件，无片段锚点；两个外部 HTTP(S) 链接仅识别，未做联网可达性检查。局部链接完整性不代表外部站点已验证。

核心设计的完整首个可玩版本与架构第 33 节的第一阶段范围有明确区分。架构其他章节的广义“首版”能力不自动计入本阶段；第一阶段规范明确延后记忆、支援、手机、完整装备/Buff 等。没有发现阻塞文档基线消费的产品边界冲突。

主控提示词第 2/4/6/8 节约束已核对：执行者不自行派发；起始 SHA 确认期间集成推进锁由主控管理；共享路径单一所有者；后续精确交付 SHA 在独立候选分支集成，并对实际候选源码执行受影响检查，通过才推进集成分支。公共组装修改仍须单独授权。任务卡的历史“尚未启动”文字属于基线快照，不替代主控实时台账。

验收 A01—A26、报告契约和 G1/G2/G3 已全部阅读。文档为计划清单，无实际导出能力。拟建的 Assets/EchoFramework、ContentPackages、Tools/EchoContent、Tests/EchoFramework 及 Orchestration 均不在本工作区；这与初始干净状态共同表明没有带入保存项目的未提交文档或台账。

## 实际检查与状态

环境：local / macOS，zsh、Git、python3；环境详情见 JSON。

| 命令 / 操作 | 退出结果 | 状态与含义 |
| --- | --- | --- |
| `pwd`、`git rev-parse HEAD`、`git branch --show-current`、`git worktree list --porcelain`、`git status --porcelain=v1` | 0 | passed：独立路径、精确起点、detached、初始干净 |
| `rg --files -g AGENTS.md -g '!Library' -g '!Temp'` | 1 | passed：无匹配文件；祖先文件另行读取为空 |
| `git show-ref --verify refs/heads/codex/p1-integration` | 128 | passed：预期的分支不存在，无覆盖 |
| `git switch -c codex/p1-integration` | 0 | passed：建立本工作区集成分支 |
| `git show --format=fuller --stat HEAD`、`git diff-tree --no-commit-id --name-status -r HEAD`、`git diff HEAD^ HEAD -- Docs/README.md` | 0 | passed：八路径、README 仅增加七行 |
| `cat` / `sed -n` 分段读取全部必读文件 | 0 | passed：完成静态文档审查 |
| `python3 -` 内联检查（Git SHA/父提交/分支/干净状态断言、八文件 blob 比对与 SHA-256、相对链接存在性、拟建目录缺失检查） | 0 | passed：生成 INT-00-1-baseline-checks.json |
| Unity、原型测试、新框架编译/逻辑/场景测试、G1/G2/G3 | 未执行 | not_run：本轮无实现，未分配 Unity 占用 |

早期批量探查末尾的无匹配 `rg` 或缺失文件 `ls` 返回非零，不影响此前 Git 输出；后续 Python 独立断言已核实关键结论。

## 下游与限制

本报告所在交付提交为可消费的文档集成基线。契约与能力没有变化；公共接口、Schema、实际能力目录和资源清单尚未交付。P1-01 可由主控安排从明确交付 SHA 启动，负责冻结契约和验证工具链。Unity 2021.3.27f1c2 仅为文档记录，未验证安装与运行能力。

没有运行或视觉证据；静态文档检查不计入 G1/G2/G3。本轮到此结束，等待主控明确续派，不轮询其他任务、不启动实现、不推送远端。
