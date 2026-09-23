# UI-03 首页切片接线

本提交提供原生 uGUI/TMP 首页，基于 AI00 契约，不包含场景或 `NativePhone`/`NativeHud` 修改。平板主体由 UI-03B 独立交付，INT-AI 合并后在共享场景接线。UI-02 的图像在 `Assets/NativeGame/PhoneUI/Art/`，由 INT-AI 在最终平板 Image 上绑定。

## 首页入口

在启动场景 Canvas（带 `GraphicRaycaster`、`CanvasScaler`）下调用 `CommanderUiFactory.CreateHome(canvas.transform, chineseFont)`，场景中保留唯一 `EventSystem`。`chineseFont` 使用项目 FusionPixel TMP 字体并配置动态中文字形回退；所有可变内容仍是 TMP 文本。

随后调用 `view.Bind(new CommanderHomeBindings { ... })`：

| UI 委托 | 绑定来源 |
| --- | --- |
| `BaseUrl`, `ModelId`, `TimeoutSeconds`, `HasKey`, `EndpointUrl` | 同一程序级 `ICommanderSettings` 属性 |
| `SetEndpoint`, `SetApiKey`, `ClearKey` | 同一 `ICommanderSettings` 方法 |
| `SendAsync`, `CancelTransport`, `TransportBusy` | 游戏会话也使用的同一 `ICommanderTransport` 方法／属性 |
| `EnterGame(bool offline)` | INT-AI 首页启动回调；`true` 明确离线进入，`false` 使用已配置服务 |

首页测试发一条普通短文本，只检查 `CommanderTransportStatus.Success` 与非空响应；不解析游戏提案。更改表单会取消本页正在执行的连接检查，旧回调由代次挡住。Key 输入框是 TMP 密码模式；写入 `SetApiKey` 后立即清空，UI 只询问 `HasKey`，不读回或保存 Key。URL、模型、超时的持久化由 AI-01 设置对象负责。页面展示其规范化的 `EndpointUrl`，错误仅显示类别，不显示服务原文。

`EnterGame` 回调由 INT-AI 创建／进入实际 Run；首页自身不切场景。离线入口在服务失败时仍可用。INT-AI 负责共享场景与 Build Settings，不能将本切片直接设为唯一启动场景。

## 平板最终接线

合入 UI-03B 后，INT-AI 在 `NativeHud` 的真实开关入口装配平板，映射 AI-02 会话的 `Changed`、`Draft`、`Messages`、`Busy`、`Send`、`Cancel`，映射 AI-03 桥的提案状态与 `OpenContract`/`Accept`/`Reject`/`Cancel`。会话取消只发生于真正关闭手机、重开和配置变化；切换通讯／支援／记录页仅换视图。Esc 先关闭合同并调用桥的 `Cancel`，然后才关闭手机。AI-04 的暂停由 INT-AI 从 `NativeHud` 所有手机入口调用。

原 `NativePhone` 的手动医疗／弱点支援、记忆与档位、记录、保留异议、回声改写、最终写入／销毁、结局接续、返回与重开必须由 INT-AI 迁移到平板对应入口并继续调用现有真值；本切片没有重做这些逻辑。`NativePhone.SelectPage` 当前会取消旧网络请求，INT-AI 最终改动需拆开焦点退出与会话取消。UI-02 图片作为独立 Sprite/Image 覆盖外壳、返回键、头像与图标，装饰 Image 关闭 Raycast；合同、消息和所有动态中文保持 TMP。

未使用 Unity Editor；导入、编译与唯一一次人工校验由 INT-AI 协调。
