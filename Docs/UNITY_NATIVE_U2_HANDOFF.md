# U2 首次可打包源码检查点

工作区：`/Users/const/.codex/worktrees/de32/echo-in-the-shell-evo-tavern-2026`。分支：`codex/unity-native-u2-candidate`；源码导入提交 `3c6edd52d3dc5a8aaba161e12475709c38bb4a57`，仅从 U1 精确 cherry-pick `988b163d7445e7c64f8ceccd136d507910a3ebe5`，不包含其旧 P1 祖先。最终入口/说明提交见本文件所在提交，原生集成分支同步到该点。旧 world 分支仍保留 be40cd3，不在当前候选中。

使用现有 Unity 2021.3.27f1c2 打开上述工作区，入口 `Assets/Scenes/NativeDemo.unity`。候选 Build Settings 已把 NativeDemo 放在首位并唯一勾选；四个历史场景仍保留但未勾选，场景文件未删除。用户手动选择 Windows x64 / Mono 并执行构建；本任务不运行 Unity、构建或安装模块，未修改主工作区。

操作：WASD 移动；Space 切换自动开火/停火；靠近青色终端按 E 开门，再到东侧出口按 E；Tab 开关手机占位，Escape 关闭，手机不暂停战斗；死亡或检查点结果界面点击 Restart Run 重开。

这是最小操作检查点：ECA 尚未接入，Run 尚待按系统归属收敛；主线、正式 Boss、记忆、支援及结局未完成，不代表最终架构或游戏完成。Windows 构建/启动、真实键鼠通关与重开尚未验证；英文 UI 与占位表现保留。

本轮仅检查交付路径、场景/Prefab/资源外部 GUID 引用及 Git 工作区：33 个外部 GUID 均在当前 Assets 或声明依赖包中可解析；未发现缺失外部 GUID，这不替代 Unity 导入/编译或运行。U1 自述的原工作区编译/临时组件冒烟及其限制见 Assets/NativeGame/README.md，不转记为本候选运行通过。候选资源文件保持精确交付字节；仅另改 Build Settings 和本说明。等待 U1 后续最小 ECA 版再做源码集成。

工作区备注：切换候选前旧分支状态干净；切换后显露旧 .NET bin/obj 产物（旧分支忽略规则不同），均在 Assets 外，原样保留且不暂存。本轮没有运行旧测试，也没有清理这些历史产物。
