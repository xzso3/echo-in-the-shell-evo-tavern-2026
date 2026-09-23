# GF02 在线通讯诊断

输入：`a0fcb62321258bf0d29b656fd98a43f5ad942058`。诊断分支：`codex/gf02-llm-diagnostics`。
开发来源工作树：`/Users/const/.codex/worktrees/911b/echo-in-the-shell-evo-tavern-2026`。当前复现与交付入口为集成工作树 `/Users/const/.codex/worktrees/4e44/echo-in-the-shell-evo-tavern-2026`，分支 `codex/gf02-integration`。
保留该基线的平板布局/入口文字；未复制其他工作树的未提交字体或设置。

## 复现与复制

1. 用 Unity 2021.3.27f1c2 打开上述 **4e44 集成工作树**，启动 `Assets/Scenes/CommanderHome.unity`。
2. 进入 Play 后勾选菜单 **Tools → Echo → GF02 → LLM Diagnostics (full text)**。默认关闭；域重载会清除开关，请在 Play 后开启。开关只在进程内，不写 PlayerPrefs/资产。
3. 在 AI 设置输入自己的配置，进入平板“在线通讯”，发送一条短消息。
4. Console 开启普通 Log、关闭 Collapse，搜索 `GF02-LLM`。选取本次 `session.send` 的 `rid=...`，用它过滤，再选择全部匹配日志复制到此任务。多块正文按 `seq`、`part=x/y` 顺序读取。不要只复制最后一条错误。
5. 复现完成取消勾选。诊断正文可能包含聊天/游戏状态；Unity 会把 Console 输出写入其 Editor/Player 日志，本功能不另建持久日志。密钥在输出前脱敏，且从不读取/打印请求头、异常全文或 SDK Debug 输出。

Development Build 可由开发调试入口显式设置 `CommanderDiagnostics.Enabled = true`（默认 false）；普通发布构建 getter 恒为 false，日志输出编译移除。无需发布构建复现本次问题。

## 日志含义

每条含 `rid`、`session`、`gen`、递增 `seq`、从请求开始计时的 `ms` 与分块编号。`ms` 差值用于看各阶段耗时。

- `session.send`、`snapshot`、`configuration`：输入、会话门槛、合法选项数量、离线/配置完整性、脱敏 Endpoint、模型、超时。
- `transport.enter/rejected`、`sdk.request/start`：传输门槛、SDK 实际 ChatRequest 序列化正文（完整消息和实际参数）。未设置的采样/输出参数由供应商默认处理。
- `sdk.success/exception`、`sdk.choice`、`transport.complete`：SDK 结果、finish reason、content 类型/长度、统一状态。SDK 8.8.9 成功接口不暴露 HTTP 数值状态，明确记为 `not_exposed_by_sdk`；失败从 `RestException.Response.Code` 获取状态码，0 表示未知。
- `sdk.response` 是 SDK 反序列化后的响应 JSON，**不是原始 HTTP 字节**；`reply.raw` 是解析前 `choices[0].message.content` 完整正文（脱敏后、未 Trim）。HTTP 错误可获取时输出 `http.error_body`。SDK 在反序列化 HTTP envelope 时失败则无法从公开接口获得原始成功响应；不伪造缺失证据。
- `parse.accepted/rejected`：解析后结构或明确拒绝原因。现有规则只允许严格 JSON，恰好包含 `reply`（1–1200 字）与 `proposal`；不能有 Markdown 包装/额外字段。支援提案必须匹配本次快照列出的 ID 与 kind。自然语言回复会被拒绝；实际故障属于哪一条仍待真实输出。
- `support.registered/rejected`：本地支援验证结果。注册只是待确认提案，绝不自动执行合同。
- `cancel/invalidate/discard`：取消、设置变更、换局或焦点变化；失效结果不触发 fallback。
- `fallback`、`ui.fallback/online/error`、`ui.render/message`、`ui.send.return`：最终决策、文案、实际 UI 消息渲染与发送返回值。平板隐藏时会话仍可完成，UI 渲染要等重新打开。

Endpoint 隐去代理路径、userinfo/query/fragment；凭据值和常见凭据标签、Bearer/sk-形式在分块前脱敏。不启用 SDK 自带 Debug，因为它可能输出请求头。拒绝原因不包含 JSON 异常原文。

## 当前结论与验证范围

后端收到请求只证明请求到达服务，不证明客户端获得可解析/合法的回复。本次先交付观测能力，不放宽解析规则、不跳过支援授权。待用户提供同一 rid 的真实 Console 输出，再针对确切原因修复。

不新增或运行自动测试、harness、mock 或截图巡检；真实联网、Play 与 Player 构建尚未验证。Unity 导入编译结果见任务交付记录。

编译记录：源码提交 `2234a29`。本工作树用固定 Editor 执行 `-batchmode -nographics -quit` 导入编译，退出码 0；日志 `/private/tmp/gf02-llm-diagnostics-import.log` 包含 `Tundra build success`、修改后的脚本再次导入编译成功、`Mono: successfully reloaded assembly` 和 `Exiting batchmode successfully now!`，无 `error CS`。其他工作树 Editor 未关闭，字体/设置未改动。

集成记录：`2234a290b4525c8acb7f007abb9fda8688574b52`、`7d4682b8acf62a4fc46c461f95bcd76b959b9771` 已按序 fast-forward 到唯一 `codex/gf02-integration`。INT 在 4e44 用固定 Unity `2021.3.27f1c2` 做一次正常导入/C# 编译，退出码 **0**；日志 `/private/tmp/gf02-llm-diagnostics-integration.log` 有 `Tundra build success (2.77 seconds), 9 items updated`、`AssetDatabase: script compilation time: 4.338745s`、`Mono: successfully reloaded assembly` 及成功退出，未检出 `error CS`、编译失败或包解析/导入错误。未运行 Play、真实联网、自动测试、mock 或截图巡检；本轮仅证明诊断代码在集成树可编译。集成树原有未提交 FusionPixel 字体、PackageManager 设置和 `mono_crash.7330f4927.0.json` 的内容哈希在合入前后相同，未清理或提交。
