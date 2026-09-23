using System;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map.Editor
{
    public sealed class ChunkMapWindow : EditorWindow
    {
        private string authorId = "sample";
        private string workId = "map";
        private string localId = "chunk";
        private string themeId = "default";
        private int size = 3;
        private PortInput north, east, south, west;
        private ChunkMap map;
        private ChunkDefinition prefab;
        private Vector2Int origin;
        private Vector2 scroll;
        private string paletteName = "SharedMapPalette";
        private TileBase paletteGround;
        private TileBase paletteObstacle;

        [MenuItem("Echo/Level Toolkit/Map Authoring")]
        public static void Open() => GetWindow<ChunkMapWindow>("Chunk Map");

        private void OnEnable() => SceneView.duringSceneGui += DrawScene;
        private void OnDisable() => SceneView.duringSceneGui -= DrawScene;

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Create Chunk prefab", EditorStyles.boldLabel);
            authorId = EditorGUILayout.TextField("Author ID", authorId);
            workId = EditorGUILayout.TextField("Work ID", workId);
            localId = EditorGUILayout.TextField("Local ID", localId);
            themeId = EditorGUILayout.TextField("Theme ID", themeId);
            size = EditorGUILayout.IntSlider("Square size", size, 3, 32);
            north.Draw("North (left → right)");
            east.Draw("East (bottom → top)");
            south.Draw("South (left → right)");
            west.Draw("West (bottom → top)");
            if (GUILayout.Button("Create independent Tilemap Chunk prefab"))
                Try(() => { prefab = ChunkAuthoring.CreateChunk(authorId, workId, localId, size,
                    themeId, north.Value, east.Value, south.Value, west.Value); Selection.activeObject = prefab; });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shared Unity Tile Palette", EditorStyles.boldLabel);
            paletteName = EditorGUILayout.TextField("Palette name", paletteName);
            paletteGround = (TileBase)EditorGUILayout.ObjectField("Ground Tile / RuleTile", paletteGround, typeof(TileBase), false);
            paletteObstacle = (TileBase)EditorGUILayout.ObjectField("Obstacle Tile / RuleTile", paletteObstacle, typeof(TileBase), false);
            if (GUILayout.Button("Create rectangular Palette"))
                Try(() => Selection.activeObject = ChunkAuthoring.CreateSharedPalette(paletteName, paletteGround, paletteObstacle));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assemble map", EditorStyles.boldLabel);
            map = (ChunkMap)EditorGUILayout.ObjectField("Scene map", map, typeof(ChunkMap), true);
            if (GUILayout.Button("Create map root in current scene"))
                Try(() => { var go = new GameObject("ChunkMap"); Undo.RegisterCreatedObjectUndo(go, "Create Chunk map"); map = go.AddComponent<ChunkMap>(); });
            EditorGUILayout.HelpBox("Set Cell World Size on the map component. 1 is a candidate until physical clearance is checked.", MessageType.Info);
            prefab = (ChunkDefinition)EditorGUILayout.ObjectField("Chunk prefab", prefab, typeof(ChunkDefinition), false);
            origin = EditorGUILayout.Vector2IntField("Integer cell origin", origin);
            using (new EditorGUI.DisabledScope(map == null || prefab == null))
                if (GUILayout.Button("Place prefab instance")) Try(() => Selection.activeGameObject = ChunkAuthoring.Place(map, prefab, origin).gameObject);

            if (map != null)
            {
                var report = ChunkMapGeometry.Inspect(map.GetPlacements());
                EditorGUILayout.LabelField($"Placements: {map.GetPlacements().Count}; map geometry issues: {report.Issues.Count}");
                foreach (var instance in map.GetComponentsInChildren<ChunkMapInstance>(true))
                {
                    if (instance.transform.parent != map.transform
                        || (instance.transform.localPosition - map.CellToLocal(instance.CellOrigin)).sqrMagnitude > 0.0001f
                        || instance.transform.localRotation != Quaternion.identity
                        || (instance.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
                        EditorGUILayout.HelpBox(instance.name + ": transform differs from its integer placement; snap or restore the prefab instance.", MessageType.Error);
                    var layers = instance.GetComponent<ChunkTileLayers>();
                    if (layers == null || !layers.IsComplete || layers.Obstacles.GetComponent<TilemapCollider2D>() == null)
                        EditorGUILayout.HelpBox(instance.name + ": Tilemap layers or obstacle collider missing.", MessageType.Error);
                    else if ((layers.Grid.cellSize - new Vector3(map.CellWorldSize, map.CellWorldSize, 1f)).sqrMagnitude > 0.0001f)
                        EditorGUILayout.HelpBox(instance.name + ": Grid cell size differs from map Cell World Size.", MessageType.Error);
                }
                foreach (var issue in report.Issues.Take(20))
                    EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message} {issue.Cell}",
                        issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning);
                if (report.Issues.Count > 20) EditorGUILayout.LabelField("Showing first 20 issues.");
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate isolated 3 / 5 / 6 / 32 sample")) Try(ChunkMapSample.Create);
            EditorGUILayout.EndScrollView();
        }

        private void DrawScene(SceneView view)
        {
            if (map == null || map.CellWorldSize <= 0f) return;
            foreach (var placement in map.GetPlacements())
            {
                var chunk = placement.Chunk;
                if (chunk == null) continue;
                int n = chunk.Size;
                var min = map.transform.TransformPoint(map.CellToLocal(placement.CellOrigin));
                var center = min + new Vector3(n, n, 0f) * map.CellWorldSize * 0.5f;
                Handles.color = Color.cyan;
                Handles.DrawWireCube(center, new Vector3(n, n, 0f) * map.CellWorldSize);
                Handles.Label(center, $"{chunk.ContentId.LocalId} {n}×{n}");
                foreach (ChunkSide side in Enum.GetValues(typeof(ChunkSide)))
                {
                    var port = chunk.Port(side);
                    if (!port.Enabled) continue;
                    var normal = ChunkMapGeometry.Normal(side);
                    Vector3 a, b;
                    if (side == ChunkSide.North || side == ChunkSide.South)
                    {
                        float y = side == ChunkSide.North ? n : 0;
                        a = min + new Vector3(port.Offset, y, 0) * map.CellWorldSize;
                        b = min + new Vector3(port.EndExclusive, y, 0) * map.CellWorldSize;
                    }
                    else
                    {
                        float x = side == ChunkSide.East ? n : 0;
                        a = min + new Vector3(x, port.Offset, 0) * map.CellWorldSize;
                        b = min + new Vector3(x, port.EndExclusive, 0) * map.CellWorldSize;
                    }
                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(5f, a, b);
                    Handles.Label((a + b) * 0.5f + new Vector3(normal.x, normal.y, 0f) * 0.2f,
                        $"{side} [{port.Offset},{port.EndExclusive})");
                }
            }
        }

        private static void Try(Action action)
        {
            try { action(); }
            catch (Exception exception) { Debug.LogException(exception); EditorUtility.DisplayDialog("Map authoring", exception.Message, "OK"); }
        }

        private struct PortInput
        {
            private bool enabled;
            private int offset;
            private int width;
            public EdgePort Value => enabled ? new EdgePort(true, offset, width) : new EdgePort(false, 0, 0);
            public void Draw(string label)
            {
                enabled = EditorGUILayout.Toggle(label, enabled);
                if (!enabled) return;
                EditorGUI.indentLevel++;
                offset = EditorGUILayout.IntField("Offset", offset);
                width = EditorGUILayout.IntField("Width", width);
                EditorGUI.indentLevel--;
            }
        }
    }
}
