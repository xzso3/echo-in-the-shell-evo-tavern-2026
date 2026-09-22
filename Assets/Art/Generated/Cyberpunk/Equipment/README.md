# 赛博装备像素图集

生成方式：Codex 内置 `imagegen`。玩家与弹幕图集仅作为像素密度、配色和材质参考；装备设计均为原创。

## 成品文件

- `weapon_rarities_5x256.png`：武器，主属性为攻击力。
- `boots_rarities_5x256.png`：鞋子，主属性为移动速度。
- `accessory_rarities_5x256.png`：神经火控配件，主属性为攻击速度。

每张图片尺寸均为 1280×256，由 5 个 256×256 单元格组成。

## 品质顺序

从左到右固定为：

1. 劣质：暗灰破损边框，磨损或拼装设备。
2. 普通：银灰边框，标准工业设备。
3. 稀有：蓝青边框，加入智能传感器与辅助机构。
4. 史诗：紫色边框，高级电磁或预测控制组件。
5. 传说：金橙边框，实验级核心和精密执行机构。

## Unity 导入与切片

- Texture Type：Sprite (2D and UI)
- Sprite Mode：Multiple
- Filter Mode：Point (no filter)
- Compression：None
- Generate Mip Maps：关闭
- Alpha Is Transparency：开启
- Sprite Editor → Slice：Grid By Cell Size
- Pixel Size：256×256
- Pivot：Center

## 生成规格摘要

- 武器：从损坏的拼装枪械逐级升级为标准冲锋枪、智能卡宾枪、电磁步枪和实验型线圈卡宾枪，通过结构复杂度表现攻击力提升。
- 鞋子：从破损工作靴逐级升级为战术靴、伺服跑靴、矢量稳定机动靴和动力外骨骼靴，通过关节、缓冲器和推进机构表现移动速度提升。
- 配件：从简陋的扳机计时模块逐级升级为腕式火控器、神经反射协处理器、预测战斗加速器和同步反射核心，用于表现攻击速度提升。

图片不包含品质文字、数值或属性箭头，品质通过固定位置、边框颜色和装备复杂度表达。
