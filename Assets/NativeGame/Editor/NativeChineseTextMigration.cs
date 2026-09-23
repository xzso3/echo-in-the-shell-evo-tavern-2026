using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echo.NativeGame.Editor
{
    // Explicit, repeatable migration for the existing NativeDemo only. Never rebuilds the world.
    // Run after merging the Chinese font assets. This changes text fields only, including inactive UI.
    public static class NativeChineseTextMigration
    {
        const string ScenePath = "Assets/Scenes/NativeDemo.unity";
        const string BossPath = "Assets/NativeGame/Boss/DevelopmentBoss.prefab";

        [MenuItem("Echo/Native/Apply Simplified Chinese Text (Once)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before migrating text.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (!openedHere && scene.isDirty)
                throw new InvalidOperationException("Save NativeDemo before migrating text.");
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            GameObject prefab = null;
            try
            {
                // Gather and validate every target before changing any field.
                var roots = scene.GetRootGameObjects();
                var run = roots.Single(x => x.name == "Run").GetComponent<NativeRunController>();
                var world = roots.Single(x => x.name == "World").transform;
                Require(run); Require(run.hud); Require(run.quest); Require(run.rules);
                Require(run.dialogue); Require(run.endingSequence); Require(run.hud.phone);
                var hud = run.hud;
                var phone = hud.phone;
                var labels = new Dictionary<TMP_Text, string>();
                Action<TMP_Text, string> label = (target, value) => { Require(target); labels.Add(target, value); };
                Action<Button, string> button = (target, value) => { Require(target); label(target.GetComponentInChildren<TMP_Text>(true), value); };
                label(hud.transform.Find("Title").GetComponent<TMP_Text>(), "壳中回响 / 信号 01");
                label(hud.healthLabel, "生命 100 / 100");
                label(hud.fireLabel, "自动开火 / Space 停火");
                label(hud.objectiveLabel, "01 / 找回信号\n收集这片区域中的三段记忆。");
                label(hud.promptLabel, "WASD 移动    Space 自动开火/停火    E 互动    Tab 手机");
                label(hud.resultTitle, "躯壳失联");
                button(hud.restartButton, "重新开始");
                button(hud.phoneCloseButton, "关闭 / Tab");
                button(run.dialogue.closeButton, "收到 / E");
                label(phone.heading, "心智连接 / 通讯 / 滚动阅读");
                if (phone.tabs.Length != 3 || phone.actions.Length != 8)
                    throw new InvalidOperationException("Unexpected NativeDemo phone layout.");
                for (int i = 0; i < phone.tabs.Length; i++) button(phone.tabs[i], NativePhone.PageLabel((NativePhone.Page)i));
                for (int i = 0; i < phone.actions.Length; i++) button(phone.actions[i], "操作 " + i);
                button(phone.restart, "重新开始");
                label(run.endingSequence.caption, "世界的指令消失了。\n\nE / 挥出第一拳");
                foreach (var name in new[] { "TERMINAL", "EXIT", "BYPASS", "PRIVATE MEMORY", "SYSTEM RECORD", "INITIAL ECHO", "DEVELOPMENT ENCOUNTER", "GUARDED CHECKPOINT" })
                {
                    var target = world.Find(name); Require(target);
                    label(target.GetComponent<TMP_Text>(), name == "EXIT" ? "最终档案" : name == "BYPASS" ? "检修通道" : NativeDemoBuilder.WorldLabel(name));
                }
                Require(run.rules.terminal); Require(run.rules.exit); Require(run.rules.bossComponent);
                var memories = run.rules.memoryNodes;
                if (memories.Length != 3 || memories.Any(x => !x || !x.interaction) ||
                    memories.Select(x => x.kind).Distinct().Count() != 3 ||
                    memories.Any(x => !Enum.IsDefined(typeof(NativeMemoryKind), x.kind)))
                    throw new InvalidOperationException("Expected the three original NativeDemo memories.");
                var sceneBoss = run.rules.bossComponent.GetComponentInChildren<NativeBossDisplay>(true);
                Require(sceneBoss);
                label(sceneBoss.title, "战斗机体"); label(sceneBoss.cue, "击破外壳");
                prefab = PrefabUtility.LoadPrefabContents(BossPath);
                var prefabBoss = prefab.GetComponentInChildren<NativeBossDisplay>(true);
                Require(prefabBoss); Require(prefabBoss.title); Require(prefabBoss.cue);

                foreach (var pair in labels) { Undo.RecordObject(pair.Key, "Chinese text migration"); pair.Key.text = pair.Value; Dirty(pair.Key); }
                Undo.RecordObject(run.quest, "Chinese text migration");
                run.quest.initialObjective = "01 / 找回信号\n收集这片区域中的三段记忆。";
                run.quest.completedObjective = "05 / 最终节点\n前往东侧档案节点，按 E 作出最后的选择。";
                Dirty(run.quest);
                Undo.RecordObject(run.rules, "Chinese text migration");
                run.rules.terminalMessage = "指挥官 / 三段记忆都保留下来了。\n东侧通路已开启。支援能帮上忙，但请先看清授权范围。按 Tab 打开手机。前方机体仍为测试形象，正式身份尚未确定。";
                Dirty(run.rules);
                Prompt(run.rules.terminal, "E / 重新连接三段记忆");
                Prompt(run.rules.exit, "E / 选择写入或销毁");
                foreach (var memory in memories)
                {
                    Undo.RecordObject(memory, "Chinese text migration");
                    memory.title = memory.kind == NativeMemoryKind.Private ? NativeMemoryNode.PrivateTitle : memory.kind == NativeMemoryKind.System ? NativeMemoryNode.SystemTitle : NativeMemoryNode.EchoTitle;
                    memory.body = memory.kind == NativeMemoryKind.Private ? NativeMemoryNode.PrivateBody : memory.kind == NativeMemoryKind.System ? NativeMemoryNode.SystemBody : NativeMemoryNode.EchoBody;
                    Dirty(memory); Prompt(memory.interaction, "E / 找回" + NativeMemoryNode.KindLabel(memory.kind));
                }
                prefabBoss.title.text = "战斗机体"; prefabBoss.cue.text = "击破外壳";
                PrefabUtility.SaveAsPrefabAsset(prefab, BossPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save translated Boss prefab.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save translated NativeDemo.");
                Debug.Log("NativeDemo Chinese text migration complete. Verify font coverage and layout in Unity.");
            }
            finally
            {
                if (prefab) PrefabUtility.UnloadPrefabContents(prefab);
                // Keep a failed, modified scene available for review instead of discarding edits.
                if (openedHere && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }
        static void Require(UnityEngine.Object value)
        { if (!value) throw new InvalidOperationException("Missing expected NativeDemo text reference; migration aborted."); }
        static void Prompt(NativeInteraction target, string text)
        { Undo.RecordObject(target, "Chinese text migration"); target.promptOverride = text; Dirty(target); }
        static void Dirty(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
