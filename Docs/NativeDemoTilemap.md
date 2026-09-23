# NativeDemo Tilemap 试玩关卡

交付日期：2026-09-23。Unity 2021.3.27f1c2 / URP 12.1.12。

## 入口与范围

- 场景：`Assets/Scenes/NativeDemoTilemap.unity`，菜单 **Echo → Native → Open Tilemap Playable**。
- Build Settings 首项为新版，所有原有场景条目保留；旧 `NativeDemo.unity` 未修改。
- 基于工具包 `ChunkMapSample` 的作者流程与地图模型扩展布局，未直接把 Sandbox 完整关卡的终点当作 Native 结局。
- Native 自己持有任务、叙事、门、Boss、支援与重开；没有增加第二套业务状态。这不代替工具包台账中的 S3/S4 验收。

## 地图

| Chunk Prefab | 左下角整数坐标 | 尺寸 | 用途 |
| --- | --- | --- | --- |
| Street18 | (-16, -9) | 18×18 | 出生、私人记忆、NPC、安全通讯、上下绕行 |
| Archive18 | (2, -9) | 18×18 | 系统记录、初始回声、中继、Boss 入口 |
| Arena18 | (20, -9) | 18×18 | Boss 主战场与最终档案 |
| Workshop10 | (-15, 9) | 10×10 | 装备线圈、北门开关 |
| Uplink10 | (-15, 19) | 10×10 | ArcSentry 与定向脉冲终端 |

一格为 1 Unity 单位。主街接缝宽 16 格，北侧通路宽 4 格，端口采用工具包半开区间。地图保留中央设备岛，上方检修通道与下方守卫街道均可通行。主线位置沿用 NativeDemo，门中心随格子墙对齐为 x=10.5 / 14.5 / 31.5，北门为 (-10, 19)。

所有 Chunk 都有 Ground / Obstacles / Decoration / Overhead 图层。地面、墙体为可编辑 Tilemap，墙体使用 TilemapCollider2D + NativeObstacle，运行时共享 CombatObstacle 识别，因此角色碰撞、视线和弹丸能识别同一墙体。设备装饰不代替碰撞层。

## 资源与编辑

- Chunk：`Assets/EchoLevelToolkit/Content/Map/Works/echo/native-demo-tilemap/Chunks/`。
- Tile：`Assets/NativeGame/TilemapDemo/`；Palette：`Assets/EchoLevelToolkit/Content/Map/Palette/NativeDemoCity.prefab`，含七种 Tile。
- 素材来源：现有 `cyber_city_tileset_atlas.png`、CyberCity Sprite / Mesh / Material、原 Native 角色和 Boss。
- Tile 使用原图集中 96×96 的正方形内部裁片、PPU 96；原图集已为 Point、无 mipmap、无压缩。没有改动源图集或全局渲染设置。
- `NativeTilemapDemoBuilder.cs` 是一次性新场景作者工具，拒绝覆盖已存在场景。后续美术修改直接编辑已保存的 Chunk / Tilemap，勿重新运行旧场景迁移菜单。各 Chunk 记录工具包生成基线。
- 本轮运行时 TMP 补入了原 UI 文本所需的动态字形，字体图集缓存随工作区改动保留。

## 已执行验证

1. 当前 Unity 编辑器编译成功；新场景 Missing Script 为 0。
2. 5 个 Chunk 的 `ChunkMapGeometry.Inspect` 报告为 0 问题；共 1420 个非空 Tile（地面与墙体合计）。
3. 在 Play Mode 使用半径 0.33 的圆形碰撞查询及连续 CircleCast 做可达性检查：初始能到三段记忆、中继、北区开关；不能绕过中继门或北门。打开相应门后可到脉冲终端与 Boss 入口，最终门关闭时仍不能进入最终档案；最终门开启后可达。
4. 真实 `NativeInteraction.Use` 检查：缺记忆时中继拒绝；收集三段记忆后中继开门；北侧开关打开北门。
5. 通过 Rigidbody2D.MovePosition + Physics2D.Simulate 进行聚焦实体移动：(-2,6)→(2.98,6)、(-10,17)→(-10,20.96)、(13,0)→(20.98,0)。检修与 Boss 区域由实际碰撞触发；Boss 激活并关闭入口门。
6. 通过 NativeCombat.HitBoss 施加加速测试伤害击破外壳，再调用经过距离/状态校验的核心互动入口。真实 Defeated 事件完成任务并打开最终门；从 (30,0) 实体移动至 (32.94,0)，最终节点选择 Upload 后进入 Completed / 归档。
7. NativeRunController.Restart 实际重新加载新版场景；待异步加载完成后确认 Playing、新 RunId、0 记忆、门复位、Boss 未启动。
8. 检查主街、全图、Boss 区截图；Console 未出现 C# 或场景运行错误。图形后端出现两条 memoryless depth load/store 警告。

以上使用定位、宿主调用与加速伤害，不是完整键鼠通关。未重测四结局矩阵、所有 NPC / 装备 / 支援分支，未构建 Windows。相关引用保留，完整操作手感、战斗节奏与 Windows 运行仍待人工试玩。

截图保留在本地 `Captures/NativeTilemap/`，未纳入 Git：`native-tilemap-street.png`、`native-tilemap-overview-1.png`、`native-tilemap-arena.png`。
