# GF01-00 输入基线与最小接口冻结

冻结于 2026-09-23。隔离工作树：`/Users/const/.codex/worktrees/70ec/echo-in-the-shell-evo-tavern-2026`；实际输入 HEAD：`3a7390f7258e73eec38a42098d33cb9a17dc3cab`，创建本文前工作树干净。主目录 `AGENTS.md`、`Docs/GameFlow/DEVELOPMENT.md`／`TASKS.md`、`Docs/AICommander/FINAL_HANDOFF.md`／`STATUS.md` 是只读的未提交输入，并不在此 HEAD 中；本契约不替它们登记新完成项。前 AI 批次代码、首页场景、平板 Sprite 与绑定已交付，Unity 导入及 C# 编译已报告完成，人工 Play、真实联网和最终人工校验仍未做；UI-01 已撤销，不恢复。

**场景基线。** 菜单为已提交的 `CommanderHome`；`CommanderHomeBootstrap.gameSceneName` 首选 `NativeDemoTilemap`。该 Tilemap 场景及 Build Settings 条目在主目录为用户未提交内容，本工作树没有它；干净检出以已提交的 `NativeDemo` 为游戏场景回退。GF01-INT 在唯一 Unity 时段确认最终可玩场景和菜单首项；其他任务不保存场景、改地图或运行一次性生成菜单。

## 冻结的调用语义

| 入口／拥有者 | 消费者与不可变条件 |
| --- | --- |
| `NativeRunController.BeginAction(): bool`（GF01-01 提接线，INT 写共享 Run） | 场景启动仅做 `PrepareRun` 等价准备，`Phase=Starting`、`Elapsed=0`、无战斗／世界互动／`Started`。每局“开始行动”首次成功才进入 `Playing` 并恰好触发一次既有 `Started`；未准备好或重复点击返回 `false`。当前 `NativeCombatIntegration.Initialize` 要求 `run.Running`，故由 `BeginAction` 在同一调用内先置 `Playing`、初始化 Combat、成功后发 `Started`，失败恢复 `Starting`；两步之间不可让世界推进一帧。`NativeLevelHost`、ECA、音频等既有订阅仍以 `Started` 启动。 |
| `FlowPauseReason { Intro, Phone, PauseMenu, ReturnConfirm }`、`SetPaused(reason, bool)`、`IsCombatAdvancing`、`ReleaseAllForRunEnd()`（GF01-02 定义，INT 接现有 PhonePause） | 每局只有一个暂停拥有者和一份 `Time.timeScale` 恢复值；重复置位／释放幂等，任一原因仍在即不恢复。设置窗继承菜单暂停，无独立原因。`IsRunActive` 可在手机暂停中为真以允许合法支援，战斗推进只看 `IsCombatAdvancing`。Intro 保持 0；结局演出解除时间冻结但禁止战斗，避免 `NativeEndingSequence` 的 `WaitForSeconds` 停住。保留既有 `IPhonePauseController.OpenPhone/ClosePhone/ReleaseForRunEnd` 兼容调用并路由同一拥有者，禁止第二处私存时标。 |
| `RunResultSnapshot`（GF01-03 在 `GameFlow/Results` 定义） | 终局成功判定或死亡时仅捕获一次、仅本局内存持有：本局 `Guid` 身份、`bool Success`、可空 `NativeEnding`、标题／一句总结、秒数、击杀、Sync、Difference、至多三条**真实**行为。击杀读取当前 Toolkit 武器或 NativeCombat 的实际计数。成功结局仍由 `NativeNarrative` 决定；失败标题严格“意识涣散”，不宣称四结局。时间戳在判定时冻结，不计演出／浏览。`NativeNarrative` 的记录目前是私有列表，只读事实入口由 GF01-03 提补丁、INT 接，不解析 HUD 字符串或虚构记录。 |
| `FlowNavigation.TryLoad(FlowDestination destination): bool`，`destination` 仅 `Game`／`Menu`（GF01-01） | 所有开始、重试、再来一局、返回菜单走同一去重导航；从菜单优先可加载的 `NativeDemoTilemap`，否则 `NativeDemo`，局内重试重载本局实际游戏场景；菜单为 `CommanderHome`。锁导航后按“使请求代次失效并取消 HTTP → 取消待确认合同／未执行提案 → 清理本局会话及订阅 → 释放暂停原因 → 加载场景”离局。载入失败须解锁并给可操作错误／返回入口；保留进程级 `CommanderSettings` 和内存 Key。退出应用由菜单单独处理。 |
| `ApplySettings(draft): bool changed`（GF01-02 的设置适配入口） | 菜单及局内共用既有 `CommanderSettings.Instance`、`CommanderHttpClient` 和同一设置表单逻辑；实际差异才提交并触发一次生命周期失效：取消在途请求及旧响应、取消待确认的手动／AI 合同、将未执行 AI 提案标失效，保留聊天与已执行效果。取消编辑或无变化返回 `false` 且不清合同。空 Key 输入默认保留旧内存 Key，清 Key 为明确操作；新请求使用新配置。现有 `Changed` 已取消会话请求，但尚不失效所有旧提案，GF01-02 须补这一接线；不调用 `Session.Reset()` 作为配置变化处理。 |

**输入层级。** GF01-02 给出单帧最上层消费规则，INT 在 `NativeHud`／`NativePhone` 落线：合同 Esc 只关合同；普通平板 Esc 只关平板；设置关闭回父菜单且继续暂停；返回确认取消回暂停；暂停 Esc 恢复；Intro Esc 不开始；新生拳击／黑屏及结局／结算不打开暂停或恢复战斗。终局手机返回键交 GF01-06 显示“查看本局结果”，由 GF01-03 的结果阶段路由；原新生第一拳、自主终拳、黑屏和平板接续保留。普通成功先呈现结局短文本，再由玩家主动看结算；死亡直接失败结算。

## 文件归属与交接

| 文件／目录 | 唯一写入者 |
| --- | --- |
| `GameFlow/Navigation`、`GameFlow/RunLifecycle` | GF01-01 |
| `GameFlow/Pause`、`GameFlow/Settings`，PhonePause／设置桥接**补丁建议** | GF01-02；共享原文件由 INT 落地 |
| `GameFlow/Results`、`GameFlow/Ending`，Narrative／EndingSequence **补丁建议** | GF01-03；共享原文件由 INT 落地 |
| `GameUI/Art`、`GameUI/Design` | GF01-04；复用已有 `PhoneUI/Art`，不重做平板 |
| `GameUI/Menu`、`GameUI/Intro`、`GameUI/Pause` 及各自新 Prefab | GF01-05 |
| `GameUI/Hud`、`GameUI/Results`、`GameUI/Ending` 及各自新 Prefab | GF01-06 |
| `NativeRunController`、`NativeCombatIntegration`、`NativeHud`、`NativePhone`、`CommanderHomeBootstrap`、既有 PhonePause、共享场景、Build Settings、字体引用与最终 Unity 保存 | GF01-INT 独占；其他任务提交新组件及明确接线点 |

GF01-01 依赖 `BeginAction` 与导航语义；GF01-02 交唯一暂停与设置变化入口；GF01-03 使用 01 导航、02 暂停并提供快照；GF01-05 只绑定 01／02；GF01-06 只绑定 03 与结果路由；GF01-INT 负责共享入口、当前 Tilemap/回退场景、去除旧结果 UI 重叠并串行合入。GF01-04 可并行做布局与资产。本契约仅冻结接口，无新 C#、场景改动或自动测试；正常编译由 INT 处理，最终人工校验仍待做。
