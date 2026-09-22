# P1-01 / attempt 1 / revision 1

result: **ready_for_review**。R1/R2/R3 已修复；本任务等待 INT-00 重新审查，不自行标记 accepted。

- 原 base_commit：`94ccceff3d00e3e929161e2024ca0a2728aa3067`
- 返修起点（自身分支）：`f96e9728eab53252ca705831b29d9ee72af063bb`；没有使用 INT 候选 f846232 作为基线。
- 实际测试源码提交：`79ba84db0a62de0d09d828f827af66fc7a9f106c`
- worktree：`/Users/const/.codex/worktrees/1e89/echo-in-the-shell-evo-tavern-2026`
- branch：`codex/p1-01-contracts`
- 本报告所属的后续提交仅新增 Revision1 证据；delivery_commit 见任务交付消息。

## 修复与同类审计

| 范围 | 修复 / 回归 |
|---|---|
| R1：完整字符串边界 | 六类模式：ContentId、resource ID、package ID、resource path、version、digest；使用可移植绝对末尾断言 `(?![\s\S])`，不依赖 `$`。拒绝首尾 LF/CR/CRLF/Tab/空格/NUL/U+2028/U+2029，合法文本通过。ContentId 构造器和生成 Schema 同步。所有 package_id 字段采用同一模式；正文文本仍允许换行。 |
| R2：Schema 限制 | min/maxLength、min/maxItems 在转换前严格限为 0..Int32.MaxValue。对超过 Int32、Int64 的值、负值、非整数以 FormatException 拒绝，避免 OverflowException。minimum/maximum 和嵌套 Schema 数字拒绝 NaN/±Infinity，倒置区间拒绝。 |
| R2：数字比较 | 整数与整数用 BigInteger 精确比较；混合 double/整数保留整数边界与小数符号。覆盖 Int64 极值、相邻超范围值、超过 double 精度的相邻整数及程序构造的 10^400。number 类型拒绝超过有限 double 范围的整数，保留既定 DTO 数值语义。 |
| R3：DTO 上限 | generation 为 1..2147483647；case.seed、report.seed、module_descriptor.order 为 0..2147483647。覆盖上下合法端点、相邻越界、超 Int64 和小数。所有生成 Schema 内嵌引用已重建。 |
| 其他整数/数字 | 已导出的 tick/sequence/serial/revision/progress/count/timeout 等为 Int64，继续遵守原定 ±(2^53−1) 或非负子区间；未把这些误缩成 Int32。ECA Priority/Capacity 尚无生产 Schema，文档明确后续需依 DTO 定义范围。浮点协议继续要求有限 double，步长/进度等既有区间未放宽。 |

新增 `BoundaryRegression.cs` 23 个共享子例（每个边界族包含多项断言）；既有 57 个检查保留，共 80 个。INT-00 原探针复制到 Tests/EchoFramework/Contracts/ReviewProbe/Program.cs，字节 SHA-256 `11e63dd4d6e85540f633d6d204490808c11607881f18a98009e836a56c8ead72` 与只读来源一致，没有修改断言。没有写 INT 工作区或修改其探针。

## 固定源码实际检查

| 检查 | 结果 |
|---|---|
| `ECHO_EVIDENCE_DIR=Docs/Framework/Contracts/Evidence/Revision1 sh Tests/EchoFramework/Contracts/verify.sh` | exit 0；SDK 8.0.413 / runtime 8.0.19；编译 0 warnings / 0 errors；80/80 共享子例、13/13 独立静态检查 passed |
| README 中 Unity batch 命令，增加上述 ECHO_EVIDENCE_DIR 并指定测试提交 | exit 0；2021.3.27f1c2 (3b3d9646cb47) / Editor Mono 4.0.30319.42000；80/80 共享子例 passed |
| Echo.ReviewProbe.csproj 编译和 Echo.ReviewProbe.dll 执行 | exit 0；INT-00 原探针 11/11 passed，原来失败的 8 项均通过 |
| 双宿主报告绑定 | tested_commit / 源码 / 输入 / Schema / 目录摘要全部相等 |
| `git diff --exit-code`、`git diff --check` | exit 0；测试后只有未跟踪 Revision1 证据 |

逐字命令、环境和退出结果记录在 handoff.json；日志为 dotnet.log、unity.log、probe-build.log。Unity 使用本 worktree 独立缓存，未控制保存项目编辑器，已正常退出并释放唯一验证名额。UnityHost 空骨架警告仍属已知限制，不影响契约程序集和本次修复编译。

## 摘要变化

| 摘要 | 原候选 | revision 1 |
|---|---|---|
| 源码/工具/共享回归 | `520455a480edf30b557fba7a08c8b48f28c61e1137576307a1d558d7a51f3237` | `de5a109ce989e235093f10a73b2cccae8e71be5699e2df9a83d3b0d39a2b3f97` |
| Schema（31 份） | `2df46d5949ad376903098d944bd97d3c3b8cf01c030d3942e0eddf494a7107c0` | `3c778f1ffbec9b62de3ebb0e1ab52edc938fed019ce713236d649eb3eef35888` |
| 文件夹具输入 | `91bdce9f9300a6082b7ae4c33d2dabf4279ab3842ddee6aef5e3aec6088d7b8b` | `91bdce9f9300a6082b7ae4c33d2dabf4279ab3842ddee6aef5e3aec6088d7b8b` |
| 空生产目录 | `d345c063ad76ae7b712a19a10f9ca8eb39122b631a89cfdf33e1de44ae751512` | `d345c063ad76ae7b712a19a10f9ca8eb39122b631a89cfdf33e1de44ae751512` |

新回归输入在共享测试源码中，因此进入源码摘要；原文件夹具未改、摘要不变。参数/协议文档仍标识 0.1.0，因为这是未获接受候选的错误修复；摘要明确区分两次候选。公共接口签名、工具版本、Packages 和 ProjectSettings 不变。初次交付报告保留作为历史，本目录才是返修依据。

## 下游与未执行项

P1-02/03 需消费本修复后的 Schema/验证器，不能沿用旧摘要。05/06 共同角色接口和路径所有权未变；没有新增生产能力。完整生产包加载、真实作用域/角色/资源验收、A06—A26、Play Mode、standalone 构建、视觉/分辨率和原型玩法回归均 not_run，不以本次契约通过宣称 G1/G2/G3。视觉证据无（本轮是契约批处理）。

changed-files.txt 列出相对自身 f96e972 的完整返修文件。
