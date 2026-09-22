# WARDEN-01 Boss Arena Art Pack

本目录包含独立 Boss 关卡首轮像素美术资源。风格沿用 `Neural Lockdown` 的俯视赛博都市视觉；源生成图保存在 `Sources/`，游戏用成品均为独立 PNG RGBA。

## Unity 导入设置

- 世界资源：`Sprite (2D and UI)`，`Pixels Per Unit = 32`，`Filter Mode = Point`，关闭 Mip Maps，Compression 设为 `None`。
- Boss、场地、预警、弹丸和补给默认 Pivot 均为 Center。
- UI 血条使用 Canvas 像素尺寸导入，不以 PPU 推断屏幕尺寸。
- 所有游戏用资源已清理透明背景；`Previews/` 是说明图，不应作为 Sprite 接入。

## Boss（B01-B03）

| 文件 | 尺寸 | 用途 |
| --- | ---: | --- |
| `Boss/warden_base.png` | 128×128 | 固定部署底座 |
| `Boss/warden_turret.png` | 128×128 | 上机身与朝右默认炮口 |
| `Boss/warden_core_mask.png` | 128×128 | 独立核心发光蒙版 |
| `Boss/warden_damage_overlay.png` | 128×128 | 第二阶段橙红裂痕覆盖层 |
| `Boss/warden_wreck.png` | 128×128 | 失效残骸 |
| `Boss/warden_assembled_reference.png` | 128×128 | 第一阶段组合外观参考 |
| `Boss/warden_phase2_reference.png` | 128×128 | 第二阶段组合外观参考 |

所有 Boss 层使用同一 128×128 画布和共同 Pivot `(64, 64)`，不要自动裁紧。首轮挂点建议（像素坐标，原点为左上）：

- 视觉底座中心：`(64, 66)`
- 核心中心：`(57, 52)`
- 默认朝右炮口挂点：`(108, 55)`

接入旋转炮塔时，以视觉底座中心作为旋转校准基准，并在 Unity 中用实际弹丸出生位置复核炮口挂点。核心蒙版可由材质或代码分别染为第一阶段青色、第二阶段橙红色。

## 竞技场（A01-A02）

| 文件 | 尺寸 | 用途 |
| --- | ---: | --- |
| `Arena/arena_boundary_straight.png` | 32×32 | 可旋转复用的直边界块 |
| `Arena/arena_boundary_corner.png` | 32×32 | 可旋转复用的转角块 |
| `Arena/arena_start_marker.png` | 64×64 | 战斗启动标记；战斗开始后熄灭 |

`Previews/arena_scale_preview_18x12.png` 使用现有 `cyber_ground_64x44_32px.png` 截取 18×12 单位区域，展示 PPU 32 下的实际比例。

## 攻击预警与特效（T01-T04、F01-F02）

| 文件 | 尺寸 | 用途 |
| --- | ---: | --- |
| `Telegraphs/aim_tracking_endpoint.png` | 16×16 | 琥珀色未锁定追踪端点 |
| `Telegraphs/aim_locked_endpoint.png` | 16×16 | 高亮完整锁定端点 |
| `Telegraphs/bomb_warning_ring.png` | 128×128 | 轰炸预警环；计时填充由程序控制 |
| `Telegraphs/electric_warning_tile.png` | 32×32 | 可平铺斜纹预警 |
| `Telegraphs/electric_active_tile.png` | 32×32 | 可平铺通电状态 |
| `Telegraphs/boss_burst_projectile.png` | 16×16 | 橙红敌方点射弹丸 |
| `Telegraphs/bomb_explosion_1.png` ～ `bomb_explosion_4.png` | 128×128 | 独立爆炸帧 |
| `Telegraphs/bomb_explosion_4x128.png` | 512×128 | 4 列×1 行横向图集，帧序从左至右 |

爆炸建议 10–12 FPS、单次播放、不循环。轰炸环的发光外晕不是伤害边界；按配置把有效直径校准到约 83 像素（半径 1.3 世界单位）。

## 补给与辅助反馈（S01-S02、F03）

| 文件 | 尺寸 | 用途 |
| --- | ---: | --- |
| `Supplies/supply_medkit.png` | 32×32 | 医疗包 |
| `Supplies/supply_drop_marker.png` | 64×64 | 方形四角补给投放标记 |
| `Supplies/supply_crate.png` | 32×32 | 补给箱 |
| `Supplies/fx_core_overload.png` | 64×64 | 核心过载单帧辅助效果 |
| `Supplies/fx_supply_landing.png` | 64×64 | 补给落地单帧辅助效果 |
| `Supplies/fx_medical_pickup.png` | 32×32 | 医疗拾取单帧辅助效果 |

F03 当前以单帧程序缩放、闪烁或淡出使用；若试玩后需要更强反馈，再扩展为 3–4 帧动画。

## Boss 血条（U01）

| 文件 | 尺寸 | 用途 |
| --- | ---: | --- |
| `UI/boss_healthbar_frame.png` | 320×24 | 空框与 50% 阶段分界 |
| `UI/boss_healthbar_fill.png` | 320×24 | 可裁切/缩放生命填充 |

名称、生命数值、倒计时和阶段文字由 UI 绘制，不烘焙进贴图。建议让填充图使用 `Image.Type = Filled`，框体始终覆盖在填充之上。

## 预览与源文件

- `Previews/transparency_preview.png`：棋盘格透明度检查图。
- `Previews/arena_scale_preview_18x12.png`：正式地面上的 18×12 单位比例检查图。
- `Sources/`：生成母图，仅用于追溯与后续重切，不直接接入游戏。

## 接入注意

- Sorting 建议：地面 < 预警 < 物资/角色/Boss < 瞬时命中特效 < HUD。
- `warden_damage_overlay.png` 是从第二阶段参考图中提取的首轮覆盖层，接入前应与运行时炮塔旋转方案一并检查裂痕位置。
- 预警贴图必须按照玩法判定缩放，不能用外晕范围替代碰撞范围。
- 受击闪白、后坐、核心脉冲、阶段闪动和提示文字优先由程序完成。
