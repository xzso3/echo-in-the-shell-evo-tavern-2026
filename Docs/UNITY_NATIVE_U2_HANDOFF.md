# U2 可打包源码检查点 · ECA 增量版

工作区：`/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。分支：`codex/unity-native-u2-candidate`。首次源码 `988b163` 单独导入为 `3c6edd5`；本次仅将其直接子提交 `91ca8556cbe7239bee3a3e9d668a95293de18d53` cherry-pick 为 `df120194a4f8e3d739b1a6984a66094d3e5741e5`，未合入旧 P1 祖先。最终交接提交见本文件所在提交，原生集成分支同步到该点。旧 world 分支仍保留 be40cd3，不在当前候选中。

使用现有 Unity 2021.3.27f1c2 打开上述工作区，入口 `Assets/Scenes/NativeDemo.unity`。Build Settings 保留 NativeDemo 首位且唯一勾选；四个历史场景保留未勾选，文件未删除。用户手动选择 Windows x64 / Mono 并构建。此次仅源码集成和文档交接，不启动 Unity、不构建、不安装模块，不修改用户主工作区。

操作：WASD 移动；Space 切换自动开火/停火；靠近青色终端按 E，触发 Quest 完成目标→Map 开门→Dialogue 提示；再次 E 或确认按钮关闭对白；到东侧出口按 E，由 ECA 查询 Quest 后交 Level 完成检查点。Tab 开关手机占位，Escape 关闭，手机不暂停战斗；死亡或结果界面点击 Restart Run 重开。

当前已接入本关实际 ECA：Level.Started→Quest.Activate；Interaction.Confirmed→只读条件→Quest/Map/Dialogue；出口确认→任务条件→Level.Complete。Actor 持有生命和移动，Combat 管开火/索敌/冷却/伤害调用与击杀，Map 管门，Quest 管目标，Dialogue 管会话，Run 收敛为 Level，Hud 采样输入并读取各系统；ECA 只协调规则，不持有业务副本。具体分工见 `Assets/NativeGame/README.md`。

仍是最小操作检查点：主线、正式 Boss、记忆、支援、结局和新生演出未完成，不代表最终架构或完整游戏完成。英文 UI、静态角色帧与部分素材占位保留。

验证边界：U1 报告其工作区 Unity 编译及 Play Mode 组件/ECA链冒烟通过，日志 `/private/tmp/native-u1-eca-smoke.log`；移除临时探针后最终编译 `/private/tmp/native-u1-final-compile.log` 退出0。终端/出口通过显式定位玩家和组件调用检查，不能算真实玩家路线。最后补的出口条件失败对白仅编译，未额外运行。真实键鼠、鼠标点击、Windows 构建/启动、分辨率适配及完整通关未测。本轮没有重复运行验证，也不把 U1 原工作区报告转记为当前候选运行通过。上轮33个外部GUID检查属于早期检查点，不能当作本次新增组件的运行证据。

工作区备注：已跟踪源码无未提交修改；旧 .NET bin/obj 产物在 Assets 外原样保留且不暂存，不运行旧测试、不删除旧产物。原始场景资源差量保持交付字节，候选独有 Build Settings 未受本次导入影响。后续等待开发交付再做精确源码集成。
