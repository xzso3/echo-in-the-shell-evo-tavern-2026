# P2-05 脉冲线圈接线

场景接线已由 `dd0350b` 完成：拾取件在第一块北区 `(-12,14)`，不要再次运行一次性菜单。下文保留独立代码切片的作者步骤；源场景任务短 Play Mode 已核对 E/F/Q 与效果；实际人工战斗和 Windows 仍待验证。

本切片基于 `4d64c4c794a030ed0dc1a8ce440a58d2947df73d`，只增加装备、拾取、限时效果、状态显示与两个 Prefab。未修改 `NativeDemo.unity`、`NativeMap.cs`、敌人或 Combat 文件。**合入代码本身不等于 B5 已交付**；必须完成下述场景接线并在实际战斗中确认伤害与射速变化。

## 场景接线（由当前 Unity Editor / 场景持有人执行）

1. 在 P2-03 地图和 P2-04 战斗代码落地后导入本提交，打开 `Assets/Scenes/NativeDemo.unity`。
2. 选择 `Echo/Native/Connect Pulse Coil Equipment`。菜单默认把拾取件放在 `(-7, -2)`，靠近起点；若该点在新地图中不可达，可在编辑器脚本中调用 `NativeEquipmentBuilder.ConnectAt(Vector2 pickupPosition)`，传入经过确认的可达位置。连接方法会打开并保存 `NativeDemo`，所以只由场景持有人运行一次。
3. 确认场景中只有一个 `PulseCoilEquipment`、一个 `PulseCoilPickup`。拾取件的 `NativeInteraction.run/map` 与 `NativeEquipmentPickup.equipment` 已指向当前场景；拾取交互已加入 `NativeHud.interactables`。状态条在原 UI Canvas 顶部右侧下方，引用同一个装备组件。
4. Play Mode：靠近金色拾取件按 E，状态条显示“已拾取”；按 F 装备后，`NativeCombat.damage` 默认从 24 变为 36；按 Q 后，`shotInterval` 默认从 0.38 变为 0.228，持续 8 秒；状态条倒计时，结束恢复原射击间隔。按 F 卸下，伤害恢复。实际战斗应观察同一敌人受击次数或射击密度变化。完整 Windows 试玩仍由用户进行。

## 状态归属与边界

- `NativeEquipment` 唯一持有持有、装备和效果截止时间；`NativeEquipmentHud` 只读取并显示。`NativeCombat` 的两个公开数值是射击输出，不再另存物品状态。
- `NativeEquipmentPickup` 通过已有 `NativeInteraction.Confirmed` 接收 E，成功拾取后消耗并隐藏自己；未接入 HUD 交互列表时不能被玩家拾取。
- 装备卸下或组件禁用会恢复伤害；效果到时、玩家离开运行状态、卸下或组件禁用会恢复射击间隔。
- 这一切片按当前 Combat 公开字段工作。若 P2-04 改为其他伤害/发射间隔 API，集成时应只调整 `NativeEquipment` 的写入点，并保持装备状态仍由该组件持有。

## 本独立切片检查

- 静态核对脚本 GUID、Prefab 引用、`NativeCombat.damage/shotInterval` 字段、E/F/Q 输入占用及 `NativeHud.interactables` 接线入口。
- 未占用 Unity Editor；本工作树没有运行 Unity 编译、Play Mode 或 Windows 构建。场景实例和玩家可见结果待集成者接线、编译与聚焦冒烟后确认。
