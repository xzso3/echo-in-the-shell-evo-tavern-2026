# P1-01 / attempt 1 交付检查报告

result: **ready_for_review**。仅公共契约与工具链范围，不代表 G1 或第一阶段完成。

- base_commit: `94ccceff3d00e3e929161e2024ca0a2728aa3067`
- 实际测试源码提交: `a0290050b84591248148df63c79da8dbf2dc6acf`
- worktree: `/Users/const/.codex/worktrees/1e89/echo-in-the-shell-evo-tavern-2026`
- branch: `codex/p1-01-contracts`
- 最终交付提交为包含本报告的后续证据提交；仅新增此 Evidence 目录，源码/夹具/工具/Schema 未变。最终 SHA 在任务交付消息中提供。

## 实际验证

| 检查 | 环境 / 命令 | 退出 / 结果 |
|---|---|---|
| 外部编译与逻辑 | .NET SDK 8.0.413，runtime 8.0.19，osx-arm64；`sh Tests/EchoFramework/Contracts/verify.sh` | 0；4 项目构建，0 warnings / 0 errors；57/57 契约子例 passed |
| 独立静态检查 | 同脚本调用 `python3 Tests/EchoFramework/Contracts/verify_static.py` | 0；13/13 passed，包含分层、.meta、写入边界、基线包锁/版本不变、生成物摘要与空生产目录 |
| Unity 编译/执行 | `/Applications/Unity/Hub/Editor/2021.3.27f1c2/Unity.app/Contents/MacOS/Unity`，2021.3.27f1c2 (3b3d9646cb47)，Editor Mono 4.0.30319.42000；README 的 batch 命令 | 0；57/57 同源契约子例 passed，无 C# 编译错误 |
| 同源绑定核对 | 两报告 tested_commit、framework_digest、content_digest、schema_digest、catalog_digest | 全部相等 |
| 工程清单 | `dotnet sln EchoFramework.sln list` | 0，4 个项目 |
| 源码与范围 | `git diff --exit-code && git diff --check`，运行后仅 Evidence 未跟踪 | 0 |

Unity 仅使用本 worktree 的 Library/Temp 缓存。启动前发现保存项目已有用户编辑器，不控制或关闭它。独立 batch 正常退出，Unity 名额已释放。UnityHost.asmdef 尚无脚本，出现明确警告；此项是 P1-07 骨架，不能声称可运行 Host 已完成。原有 Visual Scripting 符号匹配提示与目标平台扩展提示保留在原始日志，未修改其包或资源。

初期沙箱内 dotnet restore 因本地构建通信限制挂起，本任务只终止了自己两个构建 PID；批准本地构建访问后以离线 restore 成功。开发中的解析/括号/跨语言数字序列化问题已修正，上表仅对应固定源码的最终实际运行。

## 输入与源码摘要

- framework/source: `520455a480edf30b557fba7a08c8b48f28c61e1137576307a1d558d7a51f3237`
- contract fixture input: `91bdce9f9300a6082b7ae4c33d2dabf4279ab3842ddee6aef5e3aec6088d7b8b`
- Schema: `2df46d5949ad376903098d944bd97d3c3b8cf01c030d3942e0eddf494a7107c0`（31 份）
- production catalog: `d345c063ad76ae7b712a19a10f9ca8eb39122b631a89cfdf33e1de44ae751512`（能力列表为空）
- JSON DLL: `7292d3eb508652d14726749dd27094f2d481aeccf2db6427b62f68a71460897e`（两宿主字节一致）

完整机器记录见 handoff.json、dotnet-report.json、unity-report.json、static-report.json；实际命令日志见 dotnet.log、unity.log。源码摘要包括公共接口、同源 smoke、外部工程/工具/检查脚本、.meta 与工具链设置；报告和生成物不混入源码摘要，Schema/目录独立绑定。

## 交付与下游

公共 C# 覆盖 ID/引用/事件/结果/时钟/作用域/注册/执行与 ECA 定义、空间/实体、Actor alive/busy/control lease、资源、输入/快照、包/用例/报告。参数描述生成 Schema/目录；未知 Schema 关键字、未知配置字段均拒绝。合法与非法夹具覆盖字段、JSON、版本、引用、依赖、作用域、上下文、类型及身份。

P1-05 提供 IActorQuery/IActorControl，P1-06 注入同接口；本轮 double 仅验证可消费性及约定，不冒充真实 Actor。各模块提供 descriptor；INT-00 唯一维护统一组合入口和共享工程。03 生成生产目录/Schema，07 提供真实资源清单及导入映射。完整实际路径表见 ../OWNERSHIP.md；版本和许可见 ../VERSIONS_LICENSES.md。暂不需要 Packages/ProjectSettings 改动。

## 未执行与限制

- 当前 production catalogue 为空；fixture.wait / fixture.fire 仅契约探针，计划事件 Schema 不代表发布器已实现。
- A02—A05 只覆盖报告中具体契约子例。完整 A 包加载、权威事实发布检查、运行时真实过期引用/作用域、资源重复导入、真实角色/流程联调由后续模块完成。
- Schema 为显式有限子集，不支持 $ref 等关键字；不能静默按完整 Draft-07 处理。P1-03 必须保留递归 CheckSchema 和语义检查。
- 当前 JSON 为已有传递依赖 3.2.1；后续移除上游包时需共享负责人显式固定直接依赖。
- 没有冻结现有美术清单，空资源夹具不证明实际素材可用或可分发。
- Play Mode、standalone 构建、视觉/分辨率、原型玩法回归及 A06—A26 完整验收均 not_run。视觉证据：无，契约批处理无需画面；不将静态或 Editor smoke 算作视觉通过。

changed_files 全清单见 changed-files.txt（含配套 .meta、生成物和本证据目录）。
