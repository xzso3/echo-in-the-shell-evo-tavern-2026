# 原生源码交接 · U3 主线与真实 Boss

工作区：`/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。分支：`codex/unity-native-u2-candidate`；本文件所在提交为当前交接点，`codex/unity-native-integration`同步至该点。

使用 Unity 2021.3.27f1c2 打开工作区，入口 `Assets/Scenes/NativeDemo.unity`，无需重建场景。Build Settings 仍为 NativeDemo 首位唯一勾选；历史四场景保留未勾选。Windows x64 / Mono 构建由用户手动执行；本轮未启动 Unity、构建、安装或测试，未修改用户主工作区内容。

## 当前操作与路径

WASD 移动，Space 切换自动开火；E 近距离互动，打开对白后下一次 E 确认关闭。Tab 查看记忆目录，Escape/按钮关闭；手机、对白均不暂停战斗。死亡或结局点击 Restart Run。

收集西南 Private Memory、东北 System Record、东南 System Initial Echo 三段记忆；上方 service path 记录真实绕行，下方有守卫。回青色 relay 按 E，进入东侧 Boss 区域触发封门和真实战斗；破壳后靠近暴露核心按 E，错过窗口可等待再次开放。真实 Defeated 事件开最终门，最终节点 E 提交一种结局和本局摘要，随后可重开。Boss 为开发占位，系统初始回声不冒充真实玩家内容。

ECA 实际协调 Interaction/区域事件→Quest/Narrative→Map/Dialogue→Boss→Level；状态由各系统持有，移动/战斗使用 Unity 原生组件。具体职责和场景位置见 Assets/NativeGame/README.md。U4 支援、四象限结局及新生演出尚未完成。

## 提交与验证边界

在已有 ECA 集成 `66ea557` 上先接入两份状态文档 `597162e`（本地 `cc890f2`），再精确按序导入：

- 接口 `4191c87` → `a28b9e8`。
- 主线 `69d2df9` → `f7e51d5`。
- Boss `da4b5f4` → `694967b`，与原 `9def779`等价，不重复合入。
- 最终场景接线 `9e2a787` → `a23ee89`。

没有合入旧 P1 祖先或 world WIP；旧分支与用户改动保留。此次交付 Assets 字节与 U1 最终源提交一致，Build Settings 保持原集成设置。

用户已确认旧 U2 `66ea557` 的 Windows 启动、移动、开火切换、终端及重开无错误。此反馈不能外推到新增 U3。执行者报告 U3 Unity 编译及必要组件冒烟通过：三记忆/绕行、真实 Boss 两类攻击与破壳/核心窗口、一次 Defeated、最终节点单结局及重开。检查使用显式定位、脚本移动与组件调用，不是玩家人工路线。U3 真实键鼠完整通关、节奏及 Windows 新版本仍未测；本轮不重复验证或将原工作区报告转记为当前候选运行通过。日志范围见 Assets/NativeGame/README.md。

旧 .NET bin/obj 产物位于 Assets 外，保留且不暂存；本轮不运行旧验收、不清理历史产物。
