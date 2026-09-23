# GF02 批次状态

更新：2026-09-23。GF02 已集成，待最终人工校验。

- 批次归属：用户已确认本轮统一为 GF02。
- 当前阶段：六个独立工作包的代码与资产已合入 INT 工作树；固定版本 Unity 已完成 UPM 解析、19 张 GF02 图片导入与 C# 编译，INT 正收口交接。
- 输入：用户对 GF01 版本的人工测试现象及截图；GF02 开发冻结在已核实的 GF01 集成头 `c04f7d9` 之上。用户当时人工测试的具体运行提交未独立确认，不把 GF01 历史编译证据计入 GF02。
- 冻结基线：`ef8c18cf64302ba8439c45cd96b8db9bb7044aeb`，含 GF02 文档、批准参考和 `BASELINE_HANDOFF.md`；父链含 GF01 集成 `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`。主目录仍在 `3a7390f7258e73eec38a42098d33cb9a17dc3cab`，其既有修改与未跟踪资料保持原状。
- 已完成到当前阶段：10 项需求归档、两份批准设计归档、冻结基线、默认相机与主要代码路径的滚动合入、旧手机功能对照、全部正式图像与原生槽位绑定、固定 SDK 及实际依赖锁、Unity 2021.3.27f1c2 集中导入/编译（退出码 0，无 `error CS`）。
- 尚未完成：按 [ACCEPTANCE.md](ACCEPTANCE.md) 由用户或指定人员进行唯一最终人工校验；当前不宣称整批通过或真实联网已验证。

| 任务 | 状态 |
| --- | --- |
| GF02-001 | 默认 Camera/Listener 场景改动已集成并编译；直接启动与进局返回人工复测待执行 |
| GF02-002 | 纯背景、双 Logo、菜单分层代码和局部循环帧已集成/导入/绑定；视觉与循环人工复测待执行 |
| GF02-003 | 开始游戏亮色全状态代码已集成；人工复测待执行 |
| GF02-004 | 共享设置 Viewport 的 Mask/透明度原因已核实并改 RectMask2D；首页/局内人工复测待执行 |
| GF02-005 | 开始行动亮色全状态代码已集成；人工复测待执行 |
| GF02-006 | 指定 SDK 8.8.9 传输代码与固定包锁已集成并编译；真实在线/故障/超时人工校验待执行 |
| GF02-007 | 会话层本地主题/失败反馈/旧请求失效规则已集成；人工复测待执行 |
| GF02-008 | 平板支援、记录、合同、档案与旧新对照已集成；人工复测待执行 |
| GF02-009 | HUD 创建时隐藏旧标题代码已集成；实际画面人工复测待执行 |
| GF02-010 | 第二版轻量 HUD、真实数据接线、四个正式图标已集成/导入/绑定；画面与状态变化人工复测待执行 |

本批按授权不写/跑自动测试；GF02 集中工程编译已成功，最后人工校验仍全部未测，GF01 历史结果不计入。


## 执行准备记录

2026-09-23：用户指定后续采用主控＋独立Sol task模式，不使用subagent；极致速度优先，跳过自动测试和中间验收，只保留必要导入编译与最后人工校验。已编写DEVELOPMENT、PARALLEL_EXECUTION、ORCHESTRATOR_PROMPT、ACCEPTANCE及详细TASKS。

文档阶段只读观察：主目录HEAD `3a7390f`，GF01集成工作树HEAD `c04f7d9`；尚未冻结GF02输入。现有代码签名、旧手机入口已有限阅读，SDK README声明兼容Unity 2021.3+；没有安装或编译验证，不等于完成根因排查与迁移审计。

| 工作包 | 状态 | threadId / hostId | 基线 / 工作树 | 提交 / 依赖 |
| --- | --- | --- | --- | --- |
| GF02-INT | 已集成，待人工校验 | `01a0ce89-aec3-7863-a021-1b51043a8fb2` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/4e44/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-integration`，冻结 `ef8c18c` | Camera `51626ed`；ART 静态 `e848e47`、动效 `0700128`、四图标 `9f5c18b`，资源绑定 `623f33f`/`c62710c`；设置 `d5ae1a5`，CHAT 全批至 `73737df`，按钮 `42d7120`、菜单 `4f2f842`、帧时长 `4becda8`，HUD `33ef68d`/NativeHud `6b12f71`，NET 文档 `5e50094`/代码 `3b21086`/测试取消 `d32bff7`，OpenUPM manifest `04c5fee`，解析锁 `da2ea0a`；Unity 编译退出码 0，人工未测 |
| GF02-NET | 已集成，待人工联网校验 | `01a0ce8d-9367-7080-b2be-0fd8c29a6477` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/ae08/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-net`，输入 `ef8c18c` | SDK 8.8.9 依赖文档 `c011dc5`→`5e50094`、传输 `abc89f4`→`3b21086`、测试取消 `d32bff7`、包锁 `da2ea0a`；Unity 编译成功。裸根 `/chat/completions` 不能由该 SDK 公共设置精确表达，记录为限制 |
| GF02-CHAT | 已集成，待人工校验 | `01a0ce8e-1cb3-7713-9136-9dfc24653966` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/85e6/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-chat`，输入 `ef8c18c` | 完整提交顺序 `031acb9`→`4d2c0ea`→`86626b2`→`a729339`→`e530346`，均已合入，最新平板滚动修复集成 `73737df`；Host 设置变更接线 `2b4f18e`；编译成功 |
| GF02-UI | 已集成，待人工校验 | `01a0ce8e-7372-7011-9d63-368f080dd49d` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/b402/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-ui`，输入 `ef8c18c` | 设置 `2aec706`→`d5ae1a5`；按钮 `fd80bfd`→`42d7120`；菜单 `1b69ac2`→`4f2f842`；帧时长 `7cfe593`→`4becda8`，三组帧绑定 `623f33f`；编译成功 |
| GF02-ART | 已集成，待人工视觉校验 | `01a0ce89-fd60-7c61-af78-2b76eda39aad` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/6412/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-art`，输入 `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1` | 静态/双 Logo/清单/prompt `54fdf5d`→`e848e47`；雨/水/灯 12 帧 `0bc2727`→`0700128`，绑定 `623f33f`；四图标 `b2b1b6d`→`9f5c18b`，绑定 `c62710c`；19/19 PNG 导入 |
| GF02-HUD | 已集成，待人工校验 | `01a0ce8e-e343-7872-bf95-2751a3294589` / `local` / `gpt-6-sol xhigh` | `/Users/const/.codex/worktrees/db35/echo-in-the-shell-evo-tavern-2026`，`codex/gf02-hud`，输入 `ef8c18c` | 轻量 HUD `9de51ab`→`33ef68d`、交接补充 `81d359a`→`e97241b`，NativeHud 接线 `6b12f71`，图标绑定 `c62710c`；出口橙门保留，编译成功 |

允许状态：待派发 → 开发中 → 已提交待集成 → 已集成待人工校验 → 人工通过；阻断另写具体原因与可继续工作。设计采纳/编译成功不等于最后人工通过。

## 本轮主控执行记录

- 所有执行任务指定模型 `gpt-6-sol`、thinking `xhigh`；当前 `create_thread` 工具无 priority 字段，未伪造。
- 用户指定的旧主控模式仅作为调度规范，本轮未重启旧 GF01 或 AI 任务。
- ART 已交资源及尺寸清单：`MenuBackgroundStatic.png` 为 1672×941；雨/水/灯各四帧同画布，建议 0.16/0.20/0.30 秒；双独立 Logo 与四图标的具体尺寸、透明度和 prompt 见 `Execution/GF02-ART/ASSET_MANIFEST.md`、`PROMPTS.md`。
- 用户已再次明确通知原 Editor 退出；主控复核主目录 `Temp/UnityLockfile` 已不存在。INT 独占集成 Editor 完成包解析/导入/编译，未强关用户进程。
- 固定 Unity 2021.3.27f1c2 批处理退出码 0，日志结尾 `Exiting batchmode successfully now!`，无 `error CS`/编译失败。实际解析：OpenAI 8.8.9；audio 3.0.3、encoder.wav 3.0.2、rest 5.1.1、websockets 2.0.0、async 3.0.2、extensions 1.3.8、Newtonsoft 3.2.1。UPM 同时把 test-framework 锁值提升至 1.4.2（manifest 直接声明仍 1.1.33）并新增 collections/editorcoroutines/mono-cecil；未运行任何测试。
- HUD 已核实原截图右侧橙条是 `NativeMap.exitGate` 关联的中继出口门，不属 HUD 装饰；保留该世界空间玩法对象。
- 不安排自动测试、mock、harness、探针、截图巡检、独立评审或中间人工验收。INT 负责实际需要的包解析、导入与编译；最终人工校验仍全部未测。
