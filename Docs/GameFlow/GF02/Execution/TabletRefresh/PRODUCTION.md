# 战术平板批准方案实施批次

## 授权和角色

用户在确认任务列表/详情、支援、记录四张视觉预览后，明确要求由本任务担任制作人，组织 UIUX 美术 agent 与程序 agent 完成实施。设计阶段的禁止修改代码边界至此解除；仍保持原剧情、授权规则、SDK、引擎和文件所有权。

本轮采用当前任务内两个协作角色，不新建用户任务。旧 GF02 批次独立执行 task 的调度说明不作为本轮调度要求。既有 GF02-INT 继续负责集成工作树及唯一 Unity 导入编译。

| 角色 | 本轮所有权 | 输出 |
| --- | --- | --- |
| 制作人 / root | 批次文档、范围、基线、提交与 INT 协调 | 交付核对及验收边界 |
| UIUX 美术 / uiux_art | 本目录 UIUX_SPEC.md、美术清单；确需新资源时仅独立 TabletRefresh 资源目录 | 精确布局/文字/状态规格、现有素材复用、实现静态复核 |
| 程序 / programmer | CommanderTabletView、CommanderUiFactory、CommanderTabletRunAdapter、必要局部 UI DTO；本目录 PROGRAM_HANDOFF.md | 三页真实 uGUI 实现、功能映射、静态检查 |
| 既有 GF02-INT | codex/gf02-integration、共享接线、唯一 Unity 导入编译 | 集成提交与真实编译证据 |

## 基线

- 输入 `d989a632ff01b801117d7d616750981f3c5e79a6`。
- 实施分支 `codex/gf02-tablet-approved-ui`。
- 实施工作树 `/Users/const/.codex/worktrees/f30c/echo-in-the-shell-evo-tavern-2026`。
- 唯一设计输入 `Docs/GameFlow/GF02/TABLET_DESIGN_APPROVED.md` 及其四张预览。
- 集成树已有 `ProjectSettings/PackageManagerSettings.asset` 未提交变更，禁止覆盖或混入本批提交。

## 交付门槛

1. 离线/未配置通讯无输入发送入口；本地主题、任务列表详情来自真实状态与既有剧情。
2. 支援分组选项、真实限制、合同确认与一次性执行保留。
3. 记录目录详情、异议/回声、终局选择与新生接续完整保留。
4. 列表返回位置、固定导航、正文滚动、明确行高、16:9/16:10 等比安全区域落地。
5. 在线已配置/请求/成功/失败/共享通道忙/安全节点限制不混淆；生命周期保持。
6. 不改传输 SDK、玩法数值、场景和共享核心逻辑；需要跨归属接线交 INT。
7. 规格与代码静态复核，INT 必要导入编译；不运行自动测试、mock 或截图巡检。最终人工画面/功能验收尚需真实操作，不能以静态检查或编译代替。

## 进度（2026-09-24）

- UIUX 规格与三页代码已完成，程序只修改 TabletView 与 TabletRunAdapter；无新增运行资源。
- 美术静态复核完成；任务行高、固定返回、标题省略与合同提示空间已闭环。制作人补充审查了模式返回、新Session清理、合同同帧/重入及键盘焦点恢复。
- 静态色值计算：Pale/Ink 14.06:1，Pale/Selected 5.16:1，Muted/Ink 6.71:1。此为基础色理论计算，不等于真实字体/缩放验收。
- INT 只读确认集成 HEAD 仍为输入基线；Unity PID 75893 占用4e44集成工程。已请求用户保存并正常关闭或说明继续占用，收到明确答复前不强关、不并开批处理、不改动该集成树。
- 正在整理实施提交。Unity 编译、最终人工验收尚未执行；用户工作树设置改动仍保留。
