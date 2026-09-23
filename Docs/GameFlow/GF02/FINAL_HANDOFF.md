# GF02 集成交付（编译待补的草稿）

本文件由 GF02-INT 维护。**当前是集成草稿，Unity 包解析、导入和编译尚未完成；整体不得标为通过或“已集成，待人工校验”。**最终只按 [ACCEPTANCE.md](ACCEPTANCE.md) 进行一次人工校验，未操作的项目保持“未测”。

## 版本与启动

| 项 | 当前事实 |
| --- | --- |
| 唯一集成分支 | `codex/gf02-integration` |
| 冻结输入 | `ef8c18cf64302ba8439c45cd96b8db9bb7044aeb`；其中 GF01 代码父基线为 `c04f7d9040b4d5a7170dbd6cd3b0d56ff09a16b1`。见 [BASELINE_HANDOFF.md](BASELINE_HANDOFF.md)。 |
| 截至本草稿的代码/资产头 | `c62710c19894c228aee766cef63f5b29dae21637`。后续包锁、编译修复及本文提交会改变分支 HEAD，最终以交付时 `git rev-parse HEAD` 为准。 |
| 独立工作树 | `/Users/const/.codex/worktrees/4e44/echo-in-the-shell-evo-tavern-2026`；主目录和 GF01 工作树中的用户未提交资料未覆盖、清理或丢弃。 |
| 引擎 | 仓库固定 Unity `2021.3.27f1c2`、URP `12.1.12`；未升级。 |
| 启动 | 在该工作树打开 `Assets/Scenes/CommanderHome.unity`，它是已提交 Build Settings 的首场景；进入游戏时优先 `NativeDemoTilemap`，干净集成检出没有用户未提交的 Tilemap 场景，因此现有 `FlowNavigation` 回退到已提交的 `Assets/Scenes/NativeDemo.unity`。不重跑一次性场景创建菜单。 |

## 十项交付映射

下表“已接入”仅指源代码、资源和序列化引用进入集成分支；Unity 编译与人工画面/行为尚待下文补充。

| 需求 | 当前接入结果 | 集成提交例证 | 校验 |
| --- | --- | --- | --- |
| GF02-001 | `CommanderHome` 保存场景有独立 MainCamera、Display 0、纯色清屏与唯一 AudioListener；Overlay Canvas 及三个动作不变。 | `51626ed` | 待导入；直接启动/进局返回未测 |
| GF02-002 | 纯雨夜背景、12 张局部 Rain/Water/Lights 透明帧、英文 `ECHO IN THE SHELL` 与 `CipherWorks` 独立图像已生成并绑定 `GF01Art`；菜单文字/按钮/状态仍为原生 UI。三组时长 0.16/0.20/0.30 秒每帧。 | `e848e47`、`4f2f842`、`0700128`、`623f33f`、`9f5c18b` | 导入、循环与比例未测 |
| GF02-003 | “开始游戏”采用亮字深底和正常/悬停/键盘焦点/按下/禁用状态，菜单仍仅三入口；导航一次请求锁保留。 | `42d7120`、`4f2f842` | 鼠标/键盘未测 |
| GF02-004 | 首页与局内共享 AI 设置 Viewport 从近透明 Image + stencil Mask 改为 RectMask2D，并保留滚动射线目标；平板同类裁剪也修正。 | `d5ae1a5`、`73737df` | 两处设置/平板滚动未测 |
| GF02-005 | “开始行动”及重试按钮采用同一亮字状态组件；原开局门槛和单次调用未改。 | `42d7120` | 实际点击未测 |
| GF02-006 | 指定 `com.openai.unity` SDK 8.8.9 适配保留 `ICommanderTransport`，非流式发送、取消、非缩放超时、结果分类与独立连接测试；设置页只取消测试请求。固定 manifest 已写，**UPM 锁文件与工程编译待完成**。 | `3b21086`、`d32bff7`、`04c5fee` | 在线成功/失败/超时均未测 |
| GF02-007 | 会话层保留草稿并唯一处理未配置、传输/解析失败本地回应；自由输入明确没有本地任意问答模型；Busy、取消、配置变更、换局与失效焦点分流。 | `dcca3c4`、`b9d9d97`、`1c2cf3e` | 离线与故障路径未测 |
| GF02-008 | 通讯预设主题、支援/合同、记录/档案/行为/分数/初始回声、最终节点与新生接续已接回本地服务；平板有真实绑定和合法空状态。详见 [TABLET_MIGRATION.md](Execution/GF02-CHAT/TABLET_MIGRATION.md)。Host 设置变更使未执行合同/提案失效。 | `d74d030`、`2b4f18e`、`73737df` | 全部功能人工路径未测 |
| GF02-009 | 新 HUD 创建时关闭场景旧 `Title`，不再显示“壳中回响 / 信号01”；保留出口门与玩法碰撞。 | `33ef68d` | 实际场景未测 |
| GF02-010 | 第二版紧凑 HUD 已绑定生命、任务/三记忆、开火、用时、击败、同步/差异、装备、平板可用性及近距离 E 提示；四枚透明像素图标已绑定，场景橙条经核实是可开启的中继出口门并保留。 | `33ef68d`、`6b12f71`、`c62710c` | 画面、分辨率和状态变化未测 |

## SDK 与资源

- SDK：`com.openai.unity@8.8.9`，由 OpenUPM 官方 CLI 写入 `Packages/manifest.json` 的固定依赖及 `https://package.openupm.com` 精确作用域。上游包声明 Unity 2021.3、MIT；四个直接依赖为 `com.utilities.audio@3.0.3`、`com.utilities.encoder.wav@3.0.2`、`com.utilities.rest@5.1.1`、`com.utilities.websockets@2.0.0`。`com.utilities.async`/`extensions` 等实际传递版本**待 UPM 解析后从 `packages-lock.json` 填写**。现有锁文件在 GF02 解析前含 `com.unity.nuget.newtonsoft-json@3.2.1`，不额外复制 Newtonsoft DLL。版本、Endpoint 映射与具体限制见 [SDK_DEPENDENCIES.md](Execution/GF02-NET/SDK_DEPENDENCIES.md)。
- Endpoint 保留 BaseUrl/模型/进程内 Key 与超时。SDK 的 `apiVersion` 必须非空，裸根 `/chat/completions` 不能被当前适配精确表示；当前明确拒绝该输入，目标服务如只支持它需另行做兼容方案。任何 Key 都不进入资源、提交或日志。
- ImageGen 正式资源 3 张静态层 + 12 张局部循环帧 + 4 枚 HUD 图标，全部在 `Assets/NativeGame/GameUI/Art/GF02/`；逐项尺寸、透明度、顺序、时长及生成/包装 prompt 见 [ASSET_MANIFEST.md](Execution/GF02-ART/ASSET_MANIFEST.md)、[PROMPTS.md](Execution/GF02-ART/PROMPTS.md)。各 Sprite 已有稳定 `.meta`、Point、无 mipmap 与共享资源引用；**Unity 实际导入待执行**。

## Unity 导入与未测范围

| 检查 | 当前事实 |
| --- | --- |
| UPM 包解析与 `packages-lock.json` | 待执行。用户主目录 Unity Editor 仍持有项目窗口/锁；INT 不强关，也不同时启动第二 Editor。 |
| 新 Sprite `.meta` 与 `GF01Art.asset` 导入 | 待执行；目前仅源文件、GUID 和序列化引用已提交。 |
| Unity C# 编译 | **待执行，无通过结论**。NET 代码依赖 SDK 包，包未解析前的编译结果不能作为本轮结论。 |
| Play、真实 API、离线/故障、视觉、跨比例、Windows Player | 全部未测。未写/跑自动测试、mock、探针、截图巡检或独立评审；不以未测冒充通过。 |

## 唯一最终人工清单

操作者应先在 [ACCEPTANCE.md](ACCEPTANCE.md) 填写同一集成提交、工作树、Unity 版本、场景和日期；Key 仅本地进程内输入。下列 13 项全部**待人工校验**：

1. `CommanderHome` 直接启动，相机唯一；进局与返回仍有正确画面/监听器。
2. 英文标题、CipherWorks、三个入口与雨夜局部循环，文字/建筑静止。
3. 开始游戏/开始行动的鼠标键盘状态可读，开始行动只触发一次。
4. 首页与局内 AI 设置可见、可滚动、可输入、可应用/取消/返回。
5. 未配置 API 时本地主题、支援和记录可用；空数据有解释。
6. 真实可用服务测试连接与发送：玩家行、等待、在线回复、状态区分。
7. 错误地址/凭据或实际不可达时，明确错误与本地 fallback，可继续。
8. 真实可控超时；取消、配置变化、回菜单的旧回复不回灌。
9. 通讯任务/记忆/身份/授权、异议与记录页档案/行为/分数/改写。
10. 医疗/弱点、授权层级/记忆、合同查看/拒绝/一次接受与原限制。
11. 换页、关闭、Esc、暂停、重试、离局、最终节点和新生接续。
12. 旧 HUD 标题消失，生命/任务/装备/开火/统计/平板随真实状态变化；橙色出口门保留玩法作用。
13. 16:9、16:10、较低分辨率查看菜单、设置、开局、HUD、平板的裁剪与可读性。

人工结论：**待填写**。在线凭据缺席时，第 6～8 项按 [ACCEPTANCE.md](ACCEPTANCE.md) 记“未测”，其余继续。Windows 构建、全结局逐一重跑、穷举供应商与压力测试不属于本批强制项，交付后仍未覆盖。
