# INT-00 / 轮次 3：P1-01 revision1 审查通过

result: **ready_for_review**。R1/R2/R3 已关闭，允许主控接受 P1-01 契约基线并按依赖派发下游。本结论不代表完整 G1/G2/G3。

| 项目 | 精确版本 |
| --- | --- |
| 原集成基线 | `94ccceff3d00e3e929161e2024ca0a2728aa3067` |
| 指定返修交付 | `739c37e3a66cfd91e716e79cd07db4349fd84682` |
| 返修自身测试源码 | `79ba84db0a62de0d09d828f827af66fc7a9f106c` |
| INT-00 实际测试候选及批准消费版本 | `23b091a528cb525a378c51b6800fb0378687a8aa` |
| 新候选分支 | `codex/int-00-p1-01-revision1-candidate` |
| 保留的旧失败候选 | `codex/int-00-p1-01-candidate` / `87326b317f5e1e900ad94697c74d4a9ee41e567a` |

工作区为 `/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。返修从原集成基线在新候选无冲突合入；旧失败候选不覆盖、不重置。返修源码到交付仅增加 Revision1 的 10 份证据，实际候选的其余文件与返修源码逐字一致。

本目录为候选上后续的独立证据提交。集成分支推进到精确实际测试 SHA `23b091a`，不把本轮诊断证据混入 P1-01 的修改范围检查；证据提交保留在新候选分支。证据提交后再核对与实际测试候选的差异仅限本目录。

## 修复审查

- R1：ContentId 构造器与生成 Schema 的词法模式统一采用绝对末尾断言，覆盖内容 ID、资源 ID、包 ID、资源路径、版本、摘要。共享测试检查首尾 LF/CR/CRLF/Tab/空格/NUL/U+2028/U+2029；正常值仍通过，没有 trim 或放宽断言。
- R2：Schema 长度/元素数元数据先按 BigInteger 检查 0..Int32.MaxValue，再进入原 Int32 执行路径；巨大整数、错误类型、负值、非有限元数据和倒置范围提前拒绝。新整数比较保持精确，混合浮点比较处理小数方向，避免先转 double 造成边界舍入。共享用例覆盖 Int32、Int64、double 精度边界及 BigInteger 极值。
- R3：generation、case.seed、report.seed、module order 改为与公共 DTO 对应的 Int32 范围；其余 Int64 字段保留既定安全整数范围。31 份 Schema 由同一描述重建，未手改生成物。
- 原 57 个 smoke 的断言未删除或放宽，仅调用新增 23 个共享边界子例。公共接口既有签名未改；新增 Schema helper 不等于新增生产玩法能力。Actor lease 与事实历史协议、路径所有权继续沿用上轮审查范围。
- 原独立探针与 `87326b3` 中源码逐字一致，SHA-256 为 `11e63dd4d6e85540f633d6d204490808c11607881f18a98009e836a56c8ead72`；实际引用当前候选 Core 编译重跑，11/11 通过。

## 实际验证

精确命令与环境保存在 handoff.json；日志和机器报告在本目录。

| 检查 | 退出 | 结果 |
| --- | --- | --- |
| `ECHO_EVIDENCE_DIR=/tmp/int00-round3-evidence sh Tests/EchoFramework/Contracts/verify.sh` | 0 | .NET SDK 8.0.413 / runtime 8.0.19，80/80；静态 13/13；31 Schema |
| Unity batch `Echo.Editor.Contracts.ContractSmokeEntry.Run` | 0 | Unity 2021.3.27f1c2 / Mono 4.0.30319.42000，80/80 |
| Echo.ReviewProbe.csproj 构建 / Echo.ReviewProbe.dll 执行 | 0 / 0 | 原独立探针 11/11 |
| 双宿主 tested_commit、源码/输入/Schema/目录摘要 | 0 | 均绑定实际候选；四摘要与 Revision1 交付一致 |
| 验证后 `git status --porcelain=v1` | 0、空输出 | 生成物与原证据未被改写 |
| `git diff --exit-code 79ba84d HEAD -- . ':(exclude)Docs/Framework/Contracts/Evidence/Revision1'` | 0 | 候选和返修被测源码一致 |
| `git diff --check f96e972 79ba84d` | 0 | 返修源码/文档差异无空白错误 |

初次 verify.sh 返回 1：80 项逻辑均通过，但切换分支后本任务上轮 Probe/bin、Probe/obj 缓存变成未跟踪文件，被原 write_boundary 检查正确拒绝。已检查这些路径全部是本任务生成缓存且没有跟踪文件，将其完整移到 `/tmp/int00-round2-probe-build-preserved`；原失败日志与报告保存在 dotnet-initial.log / static-initial.json。随后原命令重跑退出 0；没有修改检查范围或断言，也没有删除用户文件。

Unity 原始日志以 unity.log.gz 无损保留，其原始字节摘要在 handoff.json。本工作区 batch 已退出 0，唯一 Unity 名额释放；保存项目的用户编辑器未被操作。

## 摘要与交接

| 类型 | SHA-256 |
| --- | --- |
| 源码、工具与共享回归 | `de5a109ce989e235093f10a73b2cccae8e71be5699e2df9a83d3b0d39a2b3f97` |
| 文件夹具 | `91bdce9f9300a6082b7ae4c33d2dabf4279ab3842ddee6aef5e3aec6088d7b8b` |
| Schema | `3c778f1ffbec9b62de3ebb0e1ab52edc938fed019ce713236d649eb3eef35888` |
| 空生产目录 | `d345c063ad76ae7b712a19a10f9ca8eb39122b631a89cfdf33e1de44ae751512` |

下游必须使用修复后的 Schema 和源码摘要，不能继续使用初次交付的旧摘要。契约仍为尚未正式发布的 0.1.0；没有生产能力、资源清单或完整可运行样例。共享契约与工程由 INT-00 保管，接口修改返还 P1-01，具体实现由主控按原路径所有权派发。本轮没有实现公共组装。

完整内容包加载、真实作用域/角色/地图/资源联调、A06—A26、Play Mode、standalone、视觉/分辨率及原型玩法回归未执行；无视觉证据。G1/G2/G3 均为 not_run。记录的通过只适用于本次契约与工具链检查，不将其扩展为阶段验收。
