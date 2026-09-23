# AI 指挥官阶段集成入口

状态：开发代码与资源已接线；**待人工校验**。本阶段没有编写或运行自动测试。Unity 2021.3.27f1c2 已完成一次正常导入、C# 编译和场景保存；这不代表 Play Mode、真实联网或人工操作已通过。

## 启动与配置

- 隔离集成版 Build Settings 首场景为 `Assets/Scenes/CommanderHome.unity`，首页“开始游戏”和“离线试玩”均进入已提交的 `Assets/Scenes/NativeDemo.unity`。主目录尚未提交的 `NativeDemoTilemap.unity` 和 Build Settings 是用户现有改动，最终合流时必须保留，不能以隔离版设置覆盖。
- 首页填写兼容 `/v1/chat/completions` 的 API Base URL、模型 ID、API Key，可改 1–120 秒超时（默认 15 秒）。测试连接只验证回复有非空文本，不进入本局聊天或提案。模型 ID 由玩家填写，没有写死暂定供应商的未核验名称。
- URL、模型、超时按 AI-01 保存到本机 PlayerPrefs；Key 仅在当前程序内存中，重开本局保留，退出程序清除。“离线试玩”禁用本局在线发送，不清除现有配置或内存 Key；手动支援和记录仍可用。

## 游戏内入口

- 所有手机显示/关闭由 `NativeHud.OpenPhone/ClosePhone` 统一处理暂停；Tab 或实体返回键关闭，Esc 有合同则先关合同。手机打开时世界移动、开火和 E 互动输入被屏蔽；通讯、支援、记录由原生 uGUI/TMP 绘制。
- 在安全通讯节点可发送当前状态与最近六轮成功问答；节点内敌人不再阻止输入。请求可取消，切换页签不断线，关闭/重开隔离迟到响应。非安全节点仍可阅读、查看本局记录和使用手动支援。
- AI 只返回对白及可选医疗/弱点解析提案；合同由现有 `NativeSupportController` 生成，查看和接受分别重查条件。实际效果与同步变化由 Unity 回写为本地事实，不自动再问模型。手动/AI 合同通过待确认版本和一次性执行状态隔离。
- 既有任务、记忆、保留异议、改写回声、最终写入/销毁及结局接续入口保留在记录页。Toolkit 焦点玩家与 Boss 仍由原支援、快照和输入路由读取。
- `Assets/Scenes/NativeDemo.unity` 的 `CommanderTabletView` 序列化绑定了 `Assets/NativeGame/PhoneUI/Art/` 的七张 ImageGen Sprite；文本、数值、合同和点击区仍由 TMP/uGUI 负责。

## 最后人工校验

仅使用 [EXECUTION.md](EXECUTION.md) 的五项清单：首页配置与离线进入、安全节点通讯与暂停、合同真实效果、取消/关闭/重开隔离、平板文字和原入口。尚未执行时保持“待人工校验”；真实联网由操作者在首页输入自己的 Key，不在文档或聊天收集凭据。
