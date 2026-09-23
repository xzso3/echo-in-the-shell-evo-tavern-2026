using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Echo.LevelToolkit.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map.Editor
{
    public enum AiMergeKind { Cell, Field, Decoration }
    public enum AiMergeStatus { Safe, AlreadyPresent, Conflict }

    public sealed class AiMergeChange
    {
        public AiMergeKind Kind { get; internal set; }
        public string Key { get; internal set; }
        public AiMergeStatus Status { get; internal set; }
        public bool Apply { get; set; }
        public string BaselineValue { get; internal set; }
        public string CurrentValue { get; internal set; }
        public string ProposalValue { get; internal set; }
        public string Label => Kind + " " + Key + " — " + Status;
    }

    public sealed class AiMergePlan
    {
        internal GameObject CurrentRoot;
        internal GameObject ProposalRoot;
        internal ChunkAiBaseline Baseline;
        internal string CurrentFingerprint;
        internal string ProposalFingerprint;
        internal List<AiMergeChange> Items = new List<AiMergeChange>();
        public IReadOnlyList<AiMergeChange> Changes => Items;
    }

    // Three-way merge is deliberately scoped to Chunk cells, port/theme fields and marked decorations.
    // It never infers a baseline for a pre-existing asset.
    public static class ChunkAiMerge
    {
        private sealed class State
        {
            public readonly Dictionary<string, AiCellSnapshot> Cells = new Dictionary<string, AiCellSnapshot>();
            public readonly Dictionary<string, AiFieldSnapshot> Fields = new Dictionary<string, AiFieldSnapshot>();
            public readonly Dictionary<string, AiDecorationSnapshot> Decorations = new Dictionary<string, AiDecorationSnapshot>();
            public readonly Dictionary<string, ChunkDecoration> Objects = new Dictionary<string, ChunkDecoration>();
        }

        public static ChunkAiBaseline RecordInitialAiBaseline(GameObject prefabStageRoot)
        {
            RequirePrefabStage(prefabStageRoot);
            var source = prefabStageRoot.GetComponent<ChunkAiSource>();
            if (source == null) throw new InvalidOperationException("ChunkAiSource is required on the prefab root.");
            if (source.Baseline != null) throw new InvalidOperationException("A baseline already exists; it cannot be silently replaced.");
            string path = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
            string baselinePath = Path.ChangeExtension(path, ".ai-baseline.asset");
            if (AssetDatabase.LoadMainAssetAtPath(baselinePath) != null)
                throw new InvalidOperationException("A baseline asset already exists at " + baselinePath);
            var state = Capture(prefabStageRoot);
            var baseline = ScriptableObject.CreateInstance<ChunkAiBaseline>();
            baseline.Replace(Fingerprint(state), state.Cells.Values.ToList(), state.Fields.Values.ToList(),
                state.Decorations.Values.ToList());
            AssetDatabase.CreateAsset(baseline, baselinePath);
            Undo.RecordObject(source, "Link AI baseline");
            var data = new SerializedObject(source);
            data.FindProperty("baseline").objectReferenceValue = baseline;
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return baseline;
        }

        // Only the sample generator calls this immediately after creating a brand-new prefab.
        public static void RecordGeneratedPrefabBaseline(string prefabPath)
        {
            string baselinePath = Path.ChangeExtension(prefabPath, ".ai-baseline.asset");
            if (AssetDatabase.LoadMainAssetAtPath(baselinePath) != null)
                throw new InvalidOperationException("Generated baseline already exists: " + baselinePath);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var source = root.GetComponent<ChunkAiSource>();
                if (source == null || source.Baseline != null)
                    throw new InvalidOperationException("Generated prefab already has an AI source or is missing its marker.");
                var state = Capture(root);
                var baseline = ScriptableObject.CreateInstance<ChunkAiBaseline>();
                baseline.Replace(Fingerprint(state), state.Cells.Values.ToList(), state.Fields.Values.ToList(),
                    state.Decorations.Values.ToList());
                AssetDatabase.CreateAsset(baseline, baselinePath);
                var data = new SerializedObject(source);
                data.FindProperty("baseline").objectReferenceValue = baseline;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static AiMergePlan Preview(GameObject currentRoot, GameObject proposalRoot)
        {
            RequirePrefabStage(currentRoot);
            if (proposalRoot == null || proposalRoot.GetComponent<ChunkDefinition>() == null)
                throw new ArgumentException("Select a proposal Chunk prefab.");
            var currentDefinition = currentRoot.GetComponent<ChunkDefinition>();
            var proposalDefinition = proposalRoot.GetComponent<ChunkDefinition>();
            if (!currentDefinition.ContentId.Equals(proposalDefinition.ContentId))
                throw new InvalidOperationException("Proposal ContentIdentity differs from current Chunk.");
            if (currentDefinition.Size != proposalDefinition.Size)
                throw new InvalidOperationException("Changing Chunk size requires an explicit new asset; the merge keeps footprint stable.");
            var baseline = currentRoot.GetComponent<ChunkAiSource>()?.Baseline;
            if (baseline == null) throw new InvalidOperationException("No AI baseline: existing content is protected. Record a baseline only for an explicitly AI-authored version.");
            var original = FromBaseline(baseline);
            if (baseline.SourceFingerprint != Fingerprint(original))
                throw new InvalidOperationException("AI baseline fingerprint is inconsistent; no changes were applied.");
            if (!original.Fields.TryGetValue("size", out var originalSize)
                || originalSize.value != currentDefinition.Size.ToString(CultureInfo.InvariantCulture)
                || !original.Fields.TryGetValue("contentId", out var originalId)
                || originalId.value != currentDefinition.ContentId.ToString())
                throw new InvalidOperationException("Size or ContentIdentity differs from the AI baseline. Create a new Chunk asset or restore identity before merging.");
            var current = Capture(currentRoot);
            var proposed = Capture(proposalRoot);
            var plan = new AiMergePlan
            {
                CurrentRoot = currentRoot, ProposalRoot = proposalRoot, Baseline = baseline,
                CurrentFingerprint = Fingerprint(current), ProposalFingerprint = Fingerprint(proposed)
            };
            AddChanges(plan, AiMergeKind.Cell, original.Cells, current.Cells, proposed.Cells, CellToken, CellSummary);
            AddChanges(plan, AiMergeKind.Field, original.Fields, current.Fields, proposed.Fields, x => x.value, x => x.value);
            AddChanges(plan, AiMergeKind.Decoration, original.Decorations, current.Decorations,
                proposed.Decorations, x => x.fingerprint,
                x => x.summary + " [" + (x.fingerprint ?? string.Empty).Substring(0, Math.Min(12, (x.fingerprint ?? string.Empty).Length)) + "]");
            return plan;
        }

        public static int Apply(AiMergePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            RequirePrefabStage(plan.CurrentRoot);
            var current = Capture(plan.CurrentRoot);
            var proposed = Capture(plan.ProposalRoot);
            if (Fingerprint(current) != plan.CurrentFingerprint || Fingerprint(proposed) != plan.ProposalFingerprint)
                throw new InvalidOperationException("Chunk changed after preview. Refresh the preview before applying.");
            var baseline = FromBaseline(plan.Baseline);
            if (Fingerprint(baseline) != plan.Baseline.SourceFingerprint)
                throw new InvalidOperationException("AI baseline changed after preview.");

            var selected = plan.Items.Where(x => x.Apply).ToList();
            if (selected.Count == 0) return 0;
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply AI Chunk changes");
            var definition = plan.CurrentRoot.GetComponent<ChunkDefinition>();
            var layers = plan.CurrentRoot.GetComponent<ChunkTileLayers>();
            Undo.RecordObject(definition, "Apply AI Chunk fields");
            Undo.RecordObject(plan.Baseline, "Update AI baseline");
            foreach (ChunkTileLayer layer in Enum.GetValues(typeof(ChunkTileLayer)))
                Undo.RegisterCompleteObjectUndo(layers.Get(layer), "Apply AI Chunk cells");

            foreach (var change in selected)
            {
                if (change.Status != AiMergeStatus.AlreadyPresent)
                {
                    switch (change.Kind)
                    {
                        case AiMergeKind.Cell:
                            WriteCell(layers, proposed.Cells[change.Key]);
                            break;
                        case AiMergeKind.Field:
                            WriteField(definition, proposed.Fields[change.Key]);
                            break;
                        case AiMergeKind.Decoration:
                            ReplaceDecoration(plan.CurrentRoot, current.Objects, proposed.Objects, change.Key);
                            break;
                    }
                }
                switch (change.Kind)
                {
                    case AiMergeKind.Cell: baseline.Cells[change.Key] = proposed.Cells[change.Key]; break;
                    case AiMergeKind.Field: baseline.Fields[change.Key] = proposed.Fields[change.Key]; break;
                    case AiMergeKind.Decoration:
                        if (proposed.Decorations.TryGetValue(change.Key, out var decoration))
                            baseline.Decorations[change.Key] = decoration;
                        else baseline.Decorations.Remove(change.Key);
                        break;
                }
            }
            plan.Baseline.Replace(Fingerprint(baseline), baseline.Cells.Values.ToList(),
                baseline.Fields.Values.ToList(), baseline.Decorations.Values.ToList());
            EditorUtility.SetDirty(plan.Baseline);
            Undo.CollapseUndoOperations(group);
            return selected.Count;
        }

        private static void AddChanges<T>(AiMergePlan plan, AiMergeKind kind,
            IDictionary<string, T> before, IDictionary<string, T> current, IDictionary<string, T> proposal,
            Func<T, string> token, Func<T, string> summary)
        {
            var keys = new SortedSet<string>(before.Keys);
            keys.UnionWith(current.Keys);
            keys.UnionWith(proposal.Keys);
            foreach (string key in keys)
            {
                string b = before.TryGetValue(key, out var bv) ? token(bv) : null;
                string c = current.TryGetValue(key, out var cv) ? token(cv) : null;
                string p = proposal.TryGetValue(key, out var pv) ? token(pv) : null;
                if (p == b) continue;
                AiMergeStatus status = c == b ? AiMergeStatus.Safe
                    : c == p ? AiMergeStatus.AlreadyPresent : AiMergeStatus.Conflict;
                plan.Items.Add(new AiMergeChange { Kind = kind, Key = key, Status = status,
                    Apply = status == AiMergeStatus.Safe,
                    BaselineValue = before.TryGetValue(key, out bv) ? summary(bv) : "<none>",
                    CurrentValue = current.TryGetValue(key, out cv) ? summary(cv) : "<none>",
                    ProposalValue = proposal.TryGetValue(key, out pv) ? summary(pv) : "<none>" });
            }
        }

        private static State Capture(GameObject root)
        {
            var definition = root.GetComponent<ChunkDefinition>();
            var layers = root.GetComponent<ChunkTileLayers>();
            if (definition == null || !definition.HasValidMetadata || layers == null || !layers.IsComplete)
                throw new InvalidOperationException("Chunk metadata and all four Tilemap layers are required.");
            var details = root.transform.Find("Details");
            if (details == null) throw new InvalidOperationException("Chunk Details child is required.");
            var state = new State();
            int n = definition.Size;
            foreach (ChunkTileLayer layer in Enum.GetValues(typeof(ChunkTileLayer)))
            {
                var tilemap = layers.Get(layer);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var snapshot = new AiCellSnapshot { layer = layer, cell = new Vector2Int(x, y),
                        tile = tilemap.GetTile(cell), color = tilemap.GetColor(cell),
                        transform = tilemap.GetTransformMatrix(cell), flags = tilemap.GetTileFlags(cell) };
                    state.Cells[CellKey(snapshot)] = snapshot;
                }
            }
            AddField(state, "themeId", definition.ThemeId ?? string.Empty);
            AddField(state, "size", definition.Size.ToString(CultureInfo.InvariantCulture));
            AddField(state, "contentId", definition.ContentId.ToString());
            foreach (ChunkSide side in Enum.GetValues(typeof(ChunkSide)))
            {
                var port = definition.Port(side);
                AddField(state, side.ToString().ToLowerInvariant(),
                    (port.Enabled ? "1" : "0") + ":" + port.Offset + ":" + port.Width);
            }
            foreach (var item in root.GetComponentsInChildren<ChunkDecoration>(true))
            {
                if (string.IsNullOrWhiteSpace(item.LocalId) || state.Decorations.ContainsKey(item.LocalId))
                    throw new InvalidOperationException("Decorations need unique nonempty local IDs.");
                if (item.transform.parent != details)
                    throw new InvalidOperationException("Each marked decoration must be a direct child of Details.");
                state.Decorations.Add(item.LocalId, new AiDecorationSnapshot
                { localId = item.LocalId, fingerprint = FingerprintDecoration(item.gameObject),
                    summary = item.name + " at " + item.transform.localPosition + " active=" + item.gameObject.activeSelf });
                state.Objects.Add(item.LocalId, item);
            }
            return state;
        }

        private static State FromBaseline(ChunkAiBaseline baseline)
        {
            var state = new State();
            foreach (var cell in baseline.Cells) state.Cells.Add(CellKey(cell), cell);
            foreach (var field in baseline.Fields) state.Fields.Add(field.path, field);
            foreach (var decoration in baseline.Decorations) state.Decorations.Add(decoration.localId, decoration);
            return state;
        }

        private static void AddField(State state, string path, string value)
            => state.Fields.Add(path, new AiFieldSnapshot { path = path, value = value });

        private static string CellKey(AiCellSnapshot cell) => cell.layer + ":" + cell.cell.x + ":" + cell.cell.y;

        private static string CellToken(AiCellSnapshot cell)
        {
            var builder = new StringBuilder();
            builder.Append(cell.tile == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(cell.tile).ToString());
            builder.Append('|').Append((int)cell.flags);
            AppendFloat(builder, cell.color.r); AppendFloat(builder, cell.color.g);
            AppendFloat(builder, cell.color.b); AppendFloat(builder, cell.color.a);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++) AppendFloat(builder, cell.transform[row, col]);
            return builder.ToString();
        }

        private static string CellSummary(AiCellSnapshot cell)
            => (cell.tile == null ? "<empty>" : cell.tile.name) + " color=" + cell.color
                + " flags=" + cell.flags + " transform=" + cell.transform;

        private static void AppendFloat(StringBuilder builder, float value)
            => builder.Append('|').Append(value.ToString("R", CultureInfo.InvariantCulture));

        private static string Fingerprint(State state)
        {
            var builder = new StringBuilder();
            foreach (var pair in state.Cells.OrderBy(x => x.Key)) builder.Append("C").Append(pair.Key).Append(CellToken(pair.Value)).Append('\n');
            foreach (var pair in state.Fields.OrderBy(x => x.Key)) builder.Append("F").Append(pair.Key).Append(pair.Value.value).Append('\n');
            foreach (var pair in state.Decorations.OrderBy(x => x.Key)) builder.Append("D").Append(pair.Key).Append(pair.Value.fingerprint).Append('\n');
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        private static string FingerprintDecoration(GameObject root)
        {
            var builder = new StringBuilder();
            AppendObject(builder, root.transform);
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        private static void AppendObject(StringBuilder builder, Transform item)
        {
            builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf);
            AppendFloat(builder, item.localPosition.x); AppendFloat(builder, item.localPosition.y); AppendFloat(builder, item.localPosition.z);
            AppendFloat(builder, item.localRotation.x); AppendFloat(builder, item.localRotation.y);
            AppendFloat(builder, item.localRotation.z); AppendFloat(builder, item.localRotation.w);
            AppendFloat(builder, item.localScale.x); AppendFloat(builder, item.localScale.y); AppendFloat(builder, item.localScale.z);
            foreach (var component in item.GetComponents<Component>())
            {
                if (component == null || component is Transform) continue;
                builder.Append(component.GetType().FullName).Append(EditorJsonUtility.ToJson(component));
            }
            for (int i = 0; i < item.childCount; i++) AppendObject(builder, item.GetChild(i));
        }

        private static void WriteCell(ChunkTileLayers layers, AiCellSnapshot snapshot)
        {
            var tilemap = layers.Get(snapshot.layer);
            var cell = new Vector3Int(snapshot.cell.x, snapshot.cell.y, 0);
            tilemap.SetTile(cell, snapshot.tile);
            tilemap.SetTileFlags(cell, TileFlags.None);
            tilemap.SetColor(cell, snapshot.color);
            tilemap.SetTransformMatrix(cell, snapshot.transform);
            tilemap.SetTileFlags(cell, snapshot.flags);
            tilemap.RefreshTile(cell);
        }

        private static void WriteField(ChunkDefinition definition, AiFieldSnapshot field)
        {
            if (field.path == "size" || field.path == "contentId")
                throw new InvalidOperationException("Chunk size and ContentIdentity are immutable in AI merge.");
            var data = new SerializedObject(definition);
            if (field.path == "themeId") data.FindProperty("themeId").stringValue = field.value;
            else
            {
                var parts = field.value.Split(':');
                var port = data.FindProperty(field.path);
                if (port == null || parts.Length != 3) throw new InvalidOperationException("Invalid port field " + field.path);
                port.FindPropertyRelative("enabled").boolValue = parts[0] == "1";
                port.FindPropertyRelative("offset").intValue = int.Parse(parts[1], CultureInfo.InvariantCulture);
                port.FindPropertyRelative("width").intValue = int.Parse(parts[2], CultureInfo.InvariantCulture);
            }
            data.ApplyModifiedProperties();
        }

        private static void ReplaceDecoration(GameObject currentRoot,
            IDictionary<string, ChunkDecoration> current, IDictionary<string, ChunkDecoration> proposal, string id)
        {
            if (current.TryGetValue(id, out var old)) Undo.DestroyObjectImmediate(old.gameObject);
            if (!proposal.TryGetValue(id, out var replacement)) return;
            var details = currentRoot.transform.Find("Details");
            if (details == null) throw new InvalidOperationException("Chunk Details child is missing.");
            var clone = UnityEngine.Object.Instantiate(replacement.gameObject, details);
            clone.name = replacement.gameObject.name;
            Undo.RegisterCreatedObjectUndo(clone, "Apply AI decoration");
        }

        private static void RequirePrefabStage(GameObject root)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || root == null || stage.prefabContentsRoot != root)
                throw new InvalidOperationException("Open the target Chunk prefab in Prefab Mode; select its root.");
        }
    }
}
