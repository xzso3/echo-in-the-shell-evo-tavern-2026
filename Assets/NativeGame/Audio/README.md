# P2-06 音频切片（待 NativeDemo 接线）

本目录提供一段 16 秒可循环的背景音乐和三类实际事件音效。四段 WAV 都是 **2026-09-23 为本项目用 `generate_placeholders.py` 自制的程序合成占位音频**：纯数学波形与固定种子噪声，没有采样、录音、外部曲目或第三方音频素材。项目所有者可在本项目及其发布版本中使用、修改和再分发这些音频，无需额外署名或外部授权；此说明不改变其他资源的许可。运行 `python3 Assets/NativeGame/Audio/generate_placeholders.py` 可重生同样的 PCM WAV。未来替换为正式音频时，应单独记录新素材来源和许可。

| 资源 | 类别 | 触发 |
| --- | --- | --- |
| `BGM_SignalLoop.wav` | 背景音乐 | 本局开始播放，循环；死亡或普通结局停止 |
| `SFX_Interaction.wav` | 互动 | `NativeInteraction.Confirmed`，如记忆、终端、NPC、出口 |
| `SFX_Hurt.wav` | 受伤 | 玩家生命值实际下降，包括致命一击 |
| `SFX_Defeat.wav` | 击败 | `NativeCombat.Kills` 实际增加 |

## 场景接线（Unity 槽空闲后）

1. 把 `Assets/NativeGame/Audio/NativeAudioDirector.prefab` **放入 `Assets/Scenes/NativeDemo.unity` 根级一次**，保存场景。组件的四个 AudioClip 在 Prefab 上已绑定；`run` 留空时会自动找到现有 `NativeRunController`。Prefab 在运行时创建 2D 音乐和音效 AudioSource。不要再放第二个实例。
2. 确认场景原有 Main Camera 仍有唯一 AudioListener，Level 上 `player`、`combat`、`hud` 已接好，`hud.interactables` 包含需要发声的互动对象。若新增动态互动对象未列入该数组，其确认声不会播放；可将其补入该数组。
3. Play 后先听启动音乐；靠近记忆节点或 NPC 按 E 听互动声；被敌人实际命中听受伤声；击败普通敌人听击败声。打开 Console 确认没有 `Native audio: assign a complete run and all four clips.` 报错。此处尚未实做场景接线或 Play/Windows 听感检查，不能把资源已存在视为 B6 通过。

## 音量操作

- `F5` / `F6`：背景音乐减小 / 增大，每次 10%。
- `F7` / `F8`：音效减小 / 增大，每次 10%。
- 取值 0–100%，默认音乐 45%、音效 70%。两个值分别保存到 Unity PlayerPrefs，在重开后恢复。组件也提供 `SetMusicVolume(float)`、`SetEffectsVolume(float)`，供后续设置 UI 直接调用。

音频系统只读取现有事件与状态，不修改 Quest、Combat、Map 或 Narrative 的权威状态。当前切片需由持有 Unity 槽的任务接入场景并做必要的编译/聚焦 Play 检查；Windows 构建和人工听感由用户验收。
