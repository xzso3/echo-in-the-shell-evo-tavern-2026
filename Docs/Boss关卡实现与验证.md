# WARDEN-01 独立 Boss 关卡：实现与验证

> 本文记录已实现的战斗测试关卡。根据 [ECHO IN THE SHELL 核心设计](ECHO_IN_THE_SHELL_GAME_DESIGN.md)第 16.5 节，WARDEN-01 不属于正式剧本角色；其测试胜败不能直接作为正式任务或结局事件。正式 Boss 与剧情衔接另行设计。

更新日期：2026-09-22。

首版已通过本机 Coplay Unity MCP 在 Unity 2021.3.27f1c2 中创建、编译和验证。独立入口为 `Assets/Scenes/BossArena.unity`。这是编辑器可玩版本，未打包独立程序；真人长期难度与性能调优仍需后续试玩。

## 运行与操作

打开 BossArena，点击 Play 并聚焦 Game 视图。出生瞬间背包与装备栏为空，地上的三件普通装备会按原吸附规则进入背包。按 E 穿戴后，向上进入青色启动台即可开战，也可以不穿装备挑战。

| 操作 | 行为 |
| --- | --- |
| WASD / 方向键 | 移动，沿场地边界和 Boss 圆形主体滑动 |
| 自动攻击 | 9 单位内攻击 Boss；超出距离时显示 OUT OF RANGE |
| E / 鼠标 | 背包、选装备、穿脱、删除；背包期间冻结关卡模拟 |
| ESC | 关闭背包，或暂停 / 继续；保留打开背包前的手动暂停 |
| R | 清空本局装备、危险物和补给，恢复整备 |
| Tab / F | 在此关卡禁用；CyberCity 保留原功能 |

## 已实现内容

- 独立 18×12 场地；原地面中央裁切为 576×384 像素，PPU 32、缩放 1，不压缩整座城市。四周能量边界与移动约束对齐；无城市障碍数据，无普通刷怪。
- Boss 固定 2600 HP；中央阻挡半径 0.9，受击半径 1.1，玩家半径 0.38；不产生接触伤害。
- 整备 → 2 秒启动 → 第一阶段 → 半血安全清场及约 2 秒转换 → 第二阶段 → 胜败 / 重开。转换等待已经排队的补给落地，不恢复 Boss 已失去的生命；直接致死跳过转换。
- 点射：0.6 秒追踪、0.3 秒方向锁定，3 发，间隔 0.15 秒、速度 6、伤害 16。瞄准线、锁定亮度、炮口和弹丸方向一致，弹丸离场清理。
- 轰炸：锁定玩家当时位置，半径 1.3，1.1 秒后单次伤害 20；第一阶段 2 次、第二阶段 3 次，间隔 0.65 秒。边缘预警线段裁切到场内，爆炸 SpriteMask 裁切到地面范围。
- 电网：条带中心 ±3.5、宽 2.2；中间安全通道宽 4.8。1.4 秒预警、1 秒通电，单次伤害 18，每次激活最多造成一次有效伤害。第二阶段横纵交替。实际碰撞包含玩家半径。
- 技能按点射 → 轰炸 → 电网循环，上一个技能的伤害对象完全结束才开始恢复计时。恢复为 1 / 1 / 1.5 秒，第二阶段乘 0.8；核心预警时长保持不变。
- 战前普通武器、鞋子、模块使用原 `EquipmentLoot`；75% / 50% / 25% 阈值依次投放医疗、稀有武器、医疗。跨阈值逐个排队，每局一次。
- 仅在安全恢复窗口或转换阶段投放，0.8 秒方形预告；落点避开已有装备和医疗。正在投放时延后下一个技能，避免新攻击覆盖落地预告。
- 医疗半径 0.8、回复 25，满血保留；装备满包保留并显示提示。胜敗和重开统一清场。
- 暂停冻结移动、攻击冷却、技能预警、弹丸、阶段计时和补给；换装保留现有攻击冷却进度。Boss 各伤害来源共用原 0.65 秒受伤无敌。
- 同一步内玩家死亡优先于 Boss 胜利；结果界面显示战斗秒数、总受伤次数及点射 / 轰炸 / 电网命中和医疗拾取数。

## 代码与配置

| 路径 | 职责 |
| --- | --- |
| `Assets/BossArena/BossArenaConfig.asset` | 场地、阶段、技能、补给数值和成品美术引用 |
| `Assets/BossArena/ArenaGround.asset` | 原地面纹理的独立 Sprite 子区域，原纹理不变 |
| `Assets/PlagueSurvivor/Scripts/BossEncounterController.cs` | 状态机、Boss 生命、中央阻挡、胜败、HUD |
| `Assets/PlagueSurvivor/Scripts/WardenBossController.cs` | 三技能、预警与判定、炮塔和过载表现 |
| `Assets/PlagueSurvivor/Scripts/BossSupplyController.cs` | 阈值队列、安全投放、医疗拾取 |
| `Assets/PlagueSurvivor/Scripts/BossArenaVisual.cs` | Sprite、精确预警线、边缘裁切、电网平铺 |
| `Assets/PlagueSurvivor/Scripts/BossArenaCamera.cs` | 固定全场镜头和 HUD 留白 |
| `Assets/PlagueSurvivor/Editor/BossArenaBuilder.cs` | 独立创建工具，已存在的 BossArena 拒绝重建 |
| `Assets/PlagueSurvivor/Editor/BossArenaVerification.cs` | 显式运行的 Play Mode 专项检查和自动走位战斗 |

`PlagueGame` 仅增加可选 Boss 接入、弹丸命中、医疗和结果接口。`EquipmentLoot` 增加落点占用查询，清理时立即隐藏对象；`EquipmentPanel` 在 Boss 场景使用补给说明文案。没有改变原装备数值、城市刷怪规则、城市碰撞或对话实现。

创建工具在已保存的 CyberCity 上执行 Save As Copy，随后只修改副本，不执行初始城市构建器。Build Settings 保留原顺序，将 BossArena 追加为启用场景。日常维护应直接编辑 BossArena 和配置资产，不重新运行创建工具。

## 美术接入

使用现有 `Assets/Art/Generated/Cyberpunk/BossArena/` 成品：分层 Boss、裂痕、残骸、边界、启动标记、点射弹丸、锁定端点、电网、爆炸帧、医疗、投放标记和血条框。成品按 Point、PPU 32、无压缩、无 mipmap、Full Rect 导入，母图与预览图未作为游戏素材。

精确轰炸边界和计时条采用程序线段，不以母图外晕推断伤害范围。血条填充使用原 HUD 的白色 Sprite，第一阶段青色、第二阶段橙色，避免有色填充贴图乘色变暗。核心蒙版按运行画面进行了局部位置校准。镜头和 UI 已检查 16:9 与 16:10；当前沿用 Point 采样和固定镜头，未承诺所有任意窗口尺寸都保持整数像素倍率。

## 验证结果

| 验证 | 结果与记录 |
| --- | --- |
| Unity 编译与场景 | 最终 Console 无错误或警告；缺失脚本 0、城市碰撞组件 0、EventSystem 1；`Logs/boss-final-scene-check.json` |
| Boss 专项 | 43 项通过，`Logs/boss-verification-result.json` |
| 裸装完整循环 | 自动走位、真实发射及命中；Victory，战斗 56.64 秒，119 发，观察到两阶段全部技能 |
| 普通三件装备循环 | Victory，战斗 34.10 秒，82 发，观察到两阶段全部技能；未自动换上战中稀有武器 |
| 完整战斗记录 | `Logs/boss-playable-loop.json`；以上为固定 0.02 秒步长的编辑器模拟，不是人工通关成绩 |
| 真实失败路径 | 静止承受实际 Boss 技能，16.26 秒、6 次受伤，进入 Defeat；点射 / 轰炸 / 电网各 2 次，危险物清零。`Logs/boss-standing-failure.json` |
| 全场可躲几何采样 | 242 个可站立点，含边缘、角落、中心附近；轰炸 / 点射各 242 点通过，横纵电网 484 项通过。`Logs/boss-geometry-result.json` |
| 采样边界 | 使用裸装速度、实际边界和中央圆形阻挡；点射横移 0.3 秒、轰炸及电网移动 0.8 秒内能找到安全方向。这是几何证据，不代替人类反应测试 |
| 实际键鼠 | E 开包、鼠标选中和穿戴武器、ESC 关包及战斗暂停、W 移动进入启动台、R 重开；Boss 场景 Tab / F 未切镜头或打开对话 |
| 暂停实测 | ESC 后相隔两次查询战斗计时均为 9.262774 秒，位置和状态冻结 |
| 城市装备回归 | 45 项通过，`Logs/boss-city-inventory-regression.json` |
| 城市对话回归 | 27 项通过，`Logs/boss-city-dialogue-regression.json`；首次切换视图后立即执行的射线断言未通过，等渲染稳定后独立射线检查和整套复验均通过，未修改对话代码 |
| 原城市实际输入 | Tab 切全城视图、F 打开对话并暂停，普通刷怪正常 |
| 原城市文件 | 开发前后 SHA-256 一致，`Logs/boss-city-hash-verification.json` |

专项检查包括一次跨三个阈值、致死跳阶段、同一步双方死亡、满血医疗、满包保留和恢复拾取、背包 / 手动暂停、转换清场以及连续五次重开。运行验证仅存在于 Editor 工具，不会给正式开局添加测试装备或无敌。

## 截图

- `Captures/boss-final-16x9.png`：最终 1920×1080 整备全景。
- `Captures/boss-locked-shot-16x9.png`：点射锁定方向、炮口与预警射线。
- `Captures/boss-preparation-16x10.png`：整备与全场视野。
- `Captures/boss-inventory-16x9.png`：真实鼠标穿戴后的背包界面。
- `Captures/boss-grid-warning-16x10.png` / `boss-grid-active-16x10.png`：电网预警和通电区分。
- `Captures/boss-transition-supply-16x10.png`：半血清场、核心变色与方形补给预告。
- `Captures/boss-bomb-warning-16x10.png`：固定轰炸圆、地面医疗与稀有武器。
- `Captures/boss-victory-16x10.png`：胜利界面布局检查，使用测试伤害触发，不是完整战斗成绩截图。
- `Captures/boss-defeat-16x10-1.png`：实际技能造成死亡的结果。

## 当前限制

- 裸装和普通装备的自动走位测试均无伤，说明学习后可以稳定躲避，也意味着当前固定技能循环可被熟练利用。普通装备持续命中仅 34.10 秒，短于计划中 45～75 秒的首轮目标；保持 2600 HP 基线，未为拉长时长加入强制锁血。
- 已做真实输入和画面检查，但未完成多人、多轮人工通关难度评估；补给拾取率、输出占比及紧张程度仍需真人试玩采样。
- 未完成长时间压力测试、任意超宽或窄屏布局验收、独立可执行程序打包。
- 动态实例仍使用 Instantiate/Destroy，暂未增加对象池。音效、完整逐帧动画、主菜单与跨场景装备继承不在首版范围。
