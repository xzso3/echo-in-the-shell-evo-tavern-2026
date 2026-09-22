using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.NativeGame.Editor
{
    // One-time P2-07 authoring: configure only the two passage rules already present in NativeDemo.
    public static class NativePassageEcaConnector
    {
        const string ScenePath = "Assets/Scenes/NativeDemo.unity";

        // Batch-mode entry point for the isolated P2-07 worktree after the integration base is updated.
        public static void ConnectBatch()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring passage ECA.");
            EditorSceneManager.OpenScene(ScenePath);
            Connect();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Echo/Native/Connect P2-07 Passage ECA")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring passage ECA.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open NativeDemo before configuring passage ECA; this command will not replace the open scene.");
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!run || run.gameObject.scene.path != scene.path || !run.rules || !run.quest || !run.map || !run.dialogue ||
                !run.rules.terminal || !run.rules.northRouteSwitch || !run.narrative || !run.map.northRouteDoor)
                throw new InvalidOperationException("NativeDemo needs its existing relay terminal, north switch, Quest, Map, Narrative and Dialogue references.");

            var rules = run.rules;
            if (rules.level != run || rules.quest != run.quest || rules.map != run.map ||
                rules.dialogue != run.dialogue || rules.narrative != run.narrative)
                throw new InvalidOperationException("The scene ECA references must point to this run's Quest, Map, Narrative and Dialogue.");
            if (rules.passageRules != null && rules.passageRules.Length != 0)
                throw new InvalidOperationException("Passage rules are already configured. Inspect them instead of overwriting scene edits.");

            Undo.RecordObject(rules, "Connect P2-07 passage ECA");
            rules.passageRules = new[]
            {
                new NativeEcaRules.PassageRule
                {
                    kind = NativeEcaRules.PassageKind.RelayTerminal,
                    source = rules.terminal,
                    successMessage = rules.terminalMessage
                },
                new NativeEcaRules.PassageRule
                {
                    kind = NativeEcaRules.PassageKind.NorthRoute,
                    source = rules.northRouteSwitch,
                    successMessage = "北侧通路 / 门锁已解除。\n沿连接的两块区段向北继续，原东侧主线仍可返回。"
                }
            };
            EditorUtility.SetDirty(rules);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("P2-07 passage ECA configured on the existing terminal and north switch. Inspect ECA - scene rules, then save NativeDemo.", rules);
        }
    }
}
