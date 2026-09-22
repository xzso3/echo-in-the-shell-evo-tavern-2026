# P2-07：两处现有通路交互的 ECA 接线

本切片只复用 `NativeDemo` 已存在的中继终端与北侧区块开关。`NativeEcaRules.PassageRule` 在 Inspector 中配置事件来源、规则种类和成功对白。共同处理器订阅 `NativeInteraction.Confirmed`，只读 `Level`、`Interaction`、`Quest`、`Map` 的条件，然后让 `Quest`、`Map`、`Narrative`、`Dialogue` 各自执行动作。ECA 不保存门、任务或叙事状态。

## 在最新集成场景接线

1. 先合入 P2-07 代码提交，确认 `Assets/Scenes/NativeDemo.unity` 已包含 P2-04/05/06 的最新接线；只在持有 Unity Editor 槽时打开该场景。
2. 停止 Play Mode，执行菜单 **Echo → Native → Connect P2-07 Passage ECA**。命令只给现有 `ECA - scene rules` 的 `passageRules` 写两个 Inspector 条目，不创建场景物件，不替换当前场景，也不自动保存。已有条目时会拒绝覆盖。
3. 检查条目 0：`RelayTerminal`，Source 为原 **Relay terminal**；Success Message 与该组件原 `terminalMessage` 完全相同。条目 1：`NorthRoute`，Source 为 **North route switch**；Success Message 是原北侧门解锁对白。保存场景。
4. 原 `terminal`、`northRouteSwitch`、`exit`、记忆节点、NPC、HUD 可交互数组和 Map 门引用保留。`passageRules` 非空时仅新规则订阅这两个 `Confirmed` 事件，不会与旧处理器重复结算。若尚未执行一次性菜单，空数组会用上述两个现有引用构造同样规则，现有场景仍可运行。

## 两个实际结果

| 来源 | 只读条件 | 动作 |
| --- | --- | --- |
| 中继终端 | Level 运行、玩家可达、交互未消费、`Quest.CanRecordTerminal()`；未收齐记忆时仍显示原进度提示 | `Quest.RecordTerminal()` → 消费交互 → `Map.OpenExit()` → `Narrative.RecordRelay()` → `Dialogue.Show(原场景成功文本)` |
| 北侧开关 | Level 运行、玩家可达、交互未消费、`!Map.NorthRouteOpen` 且门存在 | `Map.OpenNorthRoute()` 成功后消费交互 → `Dialogue.Show(北侧解锁文本)` |

本切片不改变三记忆、Boss、最终节点、NPC 支线或结局判定。Windows 构建和键鼠试玩由用户最终执行；本分支的离线编译不能证明实际场景已经接线或可玩。
