# P2-07：两处现有通路交互的 ECA 接线

本切片只复用 `NativeDemo` 已存在的中继终端与北侧区块开关。`NativeEcaRules.PassageRule` 在 Inspector 中配置事件来源、规则种类和成功对白。共同处理器订阅 `NativeInteraction.Confirmed`，只读 `Level`、`Interaction`、`Quest`、`Map` 的条件，然后让 `Quest`、`Map`、`Narrative`、`Dialogue` 各自执行动作。ECA 不保存门、任务或叙事状态。

## 最新集成场景的接线

1. 本分支已在包含 P2-04/05/06 的集成基线 `f8661ff` 上，用 Unity 2021 执行一次性接线器并保存 `Assets/Scenes/NativeDemo.unity`。集成时先导入规则代码提交，再导入场景接线提交，无需再次运行菜单。
2. 检查现有 `ECA - scene rules` 的 Inspector 条目 0：`RelayTerminal`，Source 为原 **Relay terminal**；Success Message 与该组件原 `terminalMessage` 完全相同。条目 1：`NorthRoute`，Source 为 **North route switch**；Success Message 是原北侧门解锁对白。
3. 仅在只导入代码而没有导入场景接线提交时，停止 Play Mode 并执行菜单 **Echo → Native → Connect P2-07 Passage ECA**，检查两项后保存场景。命令不创建场景物件，不替换当前场景，也不自动保存；已有条目时会拒绝覆盖。
4. 原 `terminal`、`northRouteSwitch`、`exit`、记忆节点、NPC、HUD 可交互数组和 Map 门引用保留。`passageRules` 非空时仅新规则订阅这两个 `Confirmed` 事件，不会与旧处理器重复结算。若尚未执行一次性菜单，空数组会用上述两个现有引用构造同样规则，现有场景仍可运行。

## 两个实际结果

| 来源 | 只读条件 | 动作 |
| --- | --- | --- |
| 中继终端 | Level 运行、玩家可达、交互未消费、`Quest.CanRecordTerminal()`；未收齐记忆时仍显示原进度提示 | `Quest.RecordTerminal()` → 消费交互 → `Map.OpenExit()` → `Narrative.RecordRelay()` → `Dialogue.Show(原场景成功文本)` |
| 北侧开关 | Level 运行、玩家可达、交互未消费、`!Map.NorthRouteOpen` 且门存在 | `Map.OpenNorthRoute()` 成功后消费交互 → `Dialogue.Show(北侧解锁文本)` |

本切片不改变三记忆、Boss、最终节点、NPC 支线或结局判定。

## 已检查与未测

- 全 `NativeGame` 源码及接线器离线编译：0 错误、0 警告。
- Unity 2021.3.27f1c2 batch mode 编译并执行 `ConnectBatch`：退出 0；场景保存了两个 Source 和成功对白引用。
- 同版本聚焦 Editor 冒烟：直接调用两处 `NativeInteraction.Use`，先确认终端 0/3 时阻止提交，再补齐三段记忆确认 Quest 中继、东侧 Map 门和原场景对白；随后确认北侧 Map 门与对白。退出 0。探针已移除。这是程序设置局状态及交互调用，不是键鼠通关。
- Windows 构建、人工移动/按 E、Boss/最终节点及整局主线仍待用户最终试玩。
