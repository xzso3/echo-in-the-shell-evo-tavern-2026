# P102 待派最小返修（尚未启动）

真实缺陷：首次感知 null→target、丢失 target→null 时，actor.target_changed 的 Schema 允许 old_target/new_target=null，但 Core Runtime/Services.cs 的 ValueJson.Encode 直接读取 value.Kind，EventBus 无法提交合法事实。P105原任务已报告，主控核实实际代码。不阻塞无敌人玩家移动/终端路线；不可用假目标绕过。

原 owner P102；两并发名额释放后继续原任务，以届时 accepted base 安全合并保留已有 Core 成果。允许 Core/Runtime、Events 及对应 Core 测试最小修改；不改冻结公共契约或 Actor 实现。由 owner 选择最小正确修复，使框架 nullable payload 符合 Schema；不要默认放宽变量、动作参数或任意事件字段的 null。

必要验证：真实事件注册与总线的首次获取/丢失目标成功，非 nullable 字段及变量/动作仍拒绝；读取P105固定最小复现，最终由INT在真实Actor感知接点验证。不扩展畸形矩阵。固定提交、命令、结果、输入摘要一次记录；交付后INT候选验收。
