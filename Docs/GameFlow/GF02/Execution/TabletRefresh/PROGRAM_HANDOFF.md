# GF02 平板刷新程序交接

2026-09-24。程序角色基于 d989a632ff01b801117d7d616750981f3c5e79a6、用户已批准设计及 UIUX_SPEC 实施。尚未运行 Unity 或验证导入编译。未提交；由制作人统一审查与提交。

## 变更文件及边界

- `Assets/NativeGame/PhoneUI/CommanderTabletView.cs`：原 View 内增加展示 DTO、固定页头/主导航、主题/模式栏、任务条目、固定返回与操作栏、内容尺寸与滚动位置缓存。
- `Assets/NativeGame/Commander/Integration/CommanderTabletRunAdapter.cs`：绑定真实本局数据与已有操作，维护仅供展示的主题、详情及目录选择。
- 未修改 Factory（装配无需变化）、Session、SDK/传输、Support/Narrative 规则、NativePhone/Hud/Run/Host、场景、ProjectSettings、资源或 meta。

## 实现映射

| 已批准要求 | 实现 |
| --- | --- |
| 邮件式任务列表/详情 | 三个整行入口读取主目标、支线、既有行动简报。主标题/状态取 quest/narrative 结构化字段；ObjectiveText/SideObjectiveText 只作内容，不解析文本决定玩法。详情读取完整内容。无发件人/时间/未读数。 |
| 保留本地主题 | 任务、记忆、身份、授权固定主题栏；非任务正文仍复用 phone.CommsText。切主题不写入会话历史。 |
| 离线无自由输入 | OnlineAvailable 排除本局离线并校验 key/endpoint/model。不可用时模式栏与 onlinePanel 隐藏、composer/send/cancel 不可见；回收本地阅读高度。Send 自身再次验证，不能从此 UI 触发离线 fallback。 |
| 在线状态与生命周期 | 复用 Busy/TransportBusy/LastOnlineStatus。安全节点及共享通道校验；进行中禁重复发送、取消独立可用。草稿仍由 Session 持有。切主页面不 Cancel，真正关闭沿用 OnDisable.Cancel。 |
| 支援表单与合同 | 支援/授权两列、记忆循环选择（明确写“切换记忆”），选中标记、真实 CanApply 原因及 StatusText，固定合同入口。沿用 Request/PendingVersion 所有权、Confirm/Cancel 和服务复核。视图新增同帧拒绝和确认立即禁用，避免重入/重复执行。 |
| 记录目录/操作 | 五项目录；保留异议位于记忆档案详情，改写位于初始回声详情。前置/完成原因可见。动作仍走 rules，展示分值读取 preserveDifference/rewriteDifference。 |
| 最终决策/新生 | 决策模式继续锁主导航；写入/销毁原规则未动。TryBack 不截获决策或 PhoneContinuation 的关闭行为，仍交 NativePhone/ResultFlow。 |
| 滚动与焦点 | 同一 page key 保存/恢复 normalized scroll。固定返回行不随长文滚走。无状态变化不重建按钮；同 key 重建按路径恢复可交互控件焦点。切页/隐藏输入前释放输入焦点。 |
| 新局清理 | Bind 与 SessionId 变化都清阅读 key/位置、在线模式与输入焦点。SessionId 变化同时重置 Adapter 展示选择、手动合同标记及 View 决策态。 |

## 几何与 uGUI 处理

原 1088×612 机壳、786×388 孔位、42 高主导航与物理返回位置不动，原等比缩放保留。页头 30，页面区 292，主题32+8。邮件行88，摘要48；目录行40；选择按钮42；正文18。长内容可滚动，不压缩字体。LayoutElement 显式 min/preferred/flexibleHeight 解决父 VerticalLayoutGroup 将按钮压塌的问题；正文按 preferredHeight 分配。固定槽禁止 Overflow，标题单行 Ellipsis，摘要省略但详情全文可达。

所有已有原生 UI 组件/事件继续使用；不导入整屏图片。合同错误提示16号/46高，合同正文缩为176以留出两行提示与原44高确认按钮。

## 静态检查及未验证范围

已阅读四张获批图片、uGUI/ScrollView 技能和相关服务，检查唯一 Bind 调用点、程序集使用边界及回调路径，`git diff --check` 无输出。美术角色初次静态审查提出的88高行、固定返回、标题单行、省略与合同提示均已落实。未运行自动测试、mock、截图巡检或 Unity；静态检查不能替代编译与运行验收。

INT 后续必须验证：Unity 编译/TMP API；真实中文度量、截断与键盘导航；16:9/16:10、小分辨率；列表滚动恢复与同 key 焦点；真实在线成功/失败/取消/配置变化/共享通道忙；安全节点离开；手动/在线合同可用/失效/拒绝及同帧点击；一次性行为、终局/新生接续。当前 INativeSupport 没有独立 used 标志，使用服务 CanApply 原因及执行 StatusText 表达，不解析中文去猜 used 枚举。授权+20/+45沿用现有服务固定规则（服务 Delta 私有，无新展示接口）；非任务 CommsText 中现存剧情/数值文案原样保留，若配置改值后需全局同步文案应由其所有者协调。

## 收尾焦点路径复核

`Clear` 先隐藏旧子节点，再 `SetParent(null, false)` 从原查找根脱离，最后调用延迟 `Destroy`。因此同帧生成同名新节点后，`root.Find(selectedPath)` 不会命中待销毁旧节点；未使用 `DestroyImmediate`。代码已补充该顺序的原因注释，`git diff --check` 无输出。此为静态路径复核，真实键盘焦点仍待 INT 运行验证。
