using System;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.LevelToolkit.Map.Editor
{
    public sealed class ChunkAiMergeWindow : EditorWindow
    {
        private GameObject proposal;
        private AiMergePlan plan;
        private Vector2 scroll;
        private bool showCells = true;
        private bool showFields = true;
        private bool showDecorations = true;

        [MenuItem("Echo/Level Toolkit/AI Chunk Merge")]
        public static void Open() => GetWindow<ChunkAiMergeWindow>("AI Chunk Merge");

        private void OnGUI()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var current = stage?.prefabContentsRoot;
            if (current == null || current.GetComponent<ChunkDefinition>() == null)
            {
                EditorGUILayout.HelpBox("Open the target Chunk prefab in Prefab Mode before merging.", MessageType.Info);
                return;
            }
            EditorGUILayout.ObjectField("Current Chunk", current, typeof(GameObject), true);
            var source = current.GetComponent<ChunkAiSource>();
            if (source == null)
            {
                EditorGUILayout.HelpBox("ChunkAiSource is missing. Existing content remains protected.", MessageType.Error);
                return;
            }
            if (source.Baseline == null)
            {
                EditorGUILayout.HelpBox("No AI baseline. Merge is disabled and current authored content cannot be overwritten.", MessageType.Warning);
                if (GUILayout.Button("Record current version as initial AI baseline (explicit)")
                    && EditorUtility.DisplayDialog("Record AI baseline",
                        "Only do this for a version that was authored by AI and reviewed. This records the current prefab as the baseline for later three-way merges.",
                        "Record", "Cancel"))
                    Try(() => ChunkAiMerge.RecordInitialAiBaseline(current));
                return;
            }
            EditorGUILayout.ObjectField("Last AI baseline", source.Baseline, typeof(ChunkAiBaseline), false);
            proposal = (GameObject)EditorGUILayout.ObjectField("New AI proposal prefab", proposal, typeof(GameObject), false);
            if (GUILayout.Button("Preview three-way changes"))
                Try(() => plan = ChunkAiMerge.Preview(current, proposal));
            if (plan == null) return;

            EditorGUILayout.LabelField($"Changed: {plan.Changes.Count}; human edits: {plan.Changes.Count(x => x.Status != AiMergeStatus.Safe)}");
            EditorGUILayout.HelpBox("Only safe changes are selected. A matching proposal still does not claim a manual edit as AI-owned. Review and select individual human edits explicitly. Undo restores both prefab edits and baseline updates.", MessageType.Info);
            showCells = EditorGUILayout.Toggle("Show cells", showCells);
            showFields = EditorGUILayout.Toggle("Show fields / ports", showFields);
            showDecorations = EditorGUILayout.Toggle("Show decorations", showDecorations);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var change in plan.Changes)
            {
                if (change.Kind == AiMergeKind.Cell && !showCells
                    || change.Kind == AiMergeKind.Field && !showFields
                    || change.Kind == AiMergeKind.Decoration && !showDecorations) continue;
                change.Apply = EditorGUILayout.ToggleLeft(change.Label, change.Apply);
                if (change.Status != AiMergeStatus.Safe)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Baseline: " + change.BaselineValue, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Current: " + change.CurrentValue, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Proposal: " + change.ProposalValue, EditorStyles.wordWrappedLabel);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Apply selected changes with Undo"))
                Try(() =>
                {
                    int applied = ChunkAiMerge.Apply(plan);
                    Debug.Log($"Applied {applied} AI Chunk changes. Save the prefab after review.");
                    plan = null;
                });
        }

        private static void Try(Action action)
        {
            try { action(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("AI Chunk merge", exception.Message, "OK");
            }
        }
    }
}
