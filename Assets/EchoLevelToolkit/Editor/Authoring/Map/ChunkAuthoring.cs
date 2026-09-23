using System;
using System.IO;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map.Editor
{
    public static class ChunkAuthoring
    {
        public const string ContentRoot = "Assets/EchoLevelToolkit/Content/Map";

        public static ChunkDefinition CreateChunk(string authorId, string workId, string localId,
            int size, string themeId, EdgePort north, EdgePort east, EdgePort south, EdgePort west)
        {
            if (size < 3 || size > 32 || !north.IsValidFor(size) || !east.IsValidFor(size)
                || !south.IsValidFor(size) || !west.IsValidFor(size))
                throw new ArgumentException("Chunk size or port interval is invalid.");
            if (!new ContentIdentity(authorId, workId, localId).IsComplete)
                throw new ArgumentException("Author, work and local IDs are required.");

            string path = ChunkPath(authorId, workId, localId);
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("A Chunk already exists at " + path + "; existing author work content is preserved.");
            string safeName = Sanitize(localId);
            var root = new GameObject(safeName);
            try
            {
                var definition = root.AddComponent<ChunkDefinition>();
                var serialized = new SerializedObject(definition);
                var id = serialized.FindProperty("contentId");
                id.FindPropertyRelative("authorId").stringValue = authorId;
                id.FindPropertyRelative("workId").stringValue = workId;
                id.FindPropertyRelative("localId").stringValue = localId;
                serialized.FindProperty("size").intValue = size;
                serialized.FindProperty("themeId").stringValue = themeId ?? string.Empty;
                SetPort(serialized.FindProperty("north"), north);
                SetPort(serialized.FindProperty("east"), east);
                SetPort(serialized.FindProperty("south"), south);
                SetPort(serialized.FindProperty("west"), west);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var gridObject = new GameObject("Grid");
                gridObject.transform.SetParent(root.transform, false);
                var grid = gridObject.AddComponent<Grid>();
                grid.cellSize = Vector3.one;
                var ground = AddLayer(gridObject.transform, "Ground", 0, false);
                var obstacles = AddLayer(gridObject.transform, "Obstacles", 10, true);
                var decoration = AddLayer(gridObject.transform, "Decoration", 20, false);
                var overhead = AddLayer(gridObject.transform, "Overhead", 30, false);
                var details = new GameObject("Details");
                details.transform.SetParent(root.transform, false);

                var layers = root.AddComponent<ChunkTileLayers>();
                var layerData = new SerializedObject(layers);
                layerData.FindProperty("grid").objectReferenceValue = grid;
                layerData.FindProperty("ground").objectReferenceValue = ground;
                layerData.FindProperty("obstacles").objectReferenceValue = obstacles;
                layerData.FindProperty("decoration").objectReferenceValue = decoration;
                layerData.FindProperty("overhead").objectReferenceValue = overhead;
                layerData.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<ChunkAiSource>();

                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                return saved.GetComponent<ChunkDefinition>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static ChunkMapInstance Place(ChunkMap map, ChunkDefinition prefab, Vector2Int origin)
        {
            if (map == null || prefab == null || !prefab.HasValidMetadata)
                throw new ArgumentException("A map and valid Chunk prefab are required.");
            if (PrefabUtility.GetPrefabAssetType(prefab.gameObject) == PrefabAssetType.NotAPrefab)
                throw new ArgumentException("Place a saved Chunk prefab, not a scene object.");
            var placements = map.GetPlacements();
            placements.Add(new ChunkPlacement(prefab, origin));
            if (ChunkMapGeometry.Inspect(placements).Issues.Any(x => x.Code == "MAP_OVERLAP"))
                throw new InvalidOperationException("Placement overlaps an existing Chunk.");
            if (!Mathf.Approximately(map.transform.lossyScale.x, 1f)
                || !Mathf.Approximately(map.transform.lossyScale.y, 1f)
                || map.transform.rotation != Quaternion.identity || map.CellWorldSize <= 0f)
                throw new InvalidOperationException("Map must have identity rotation and scale, and positive cell size.");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, map.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Place Chunk");
            var marker = Undo.AddComponent<ChunkMapInstance>(instance);
            Undo.RecordObject(marker, "Set Chunk origin");
            marker.SetCellOrigin(origin);
            instance.transform.localPosition = map.CellToLocal(origin);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            var layers = instance.GetComponent<ChunkTileLayers>();
            if (layers != null && layers.Grid != null)
            {
                Undo.RecordObject(layers.Grid, "Set map cell size");
                layers.Grid.cellSize = new Vector3(map.CellWorldSize, map.CellWorldSize, 1f);
            }
            EditorUtility.SetDirty(map);
            return marker;
        }

        public static GameObject CreateSharedPalette(string name, TileBase ground, TileBase obstacle)
        {
            EnsureFolder(ContentRoot + "/Palette");
            string path = ContentRoot + "/Palette/" + Sanitize(name) + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var palette = GridPaletteUtility.CreateNewPalette(ContentRoot + "/Palette", Sanitize(name),
                GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic,
                new Vector3(1f, 1f, 0f), GridLayout.CellSwizzle.XYZ);
            if (palette == null) throw new InvalidOperationException("Unity did not create the Tile Palette.");
            var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(palette));
            try
            {
                var tilemap = contents.GetComponentInChildren<Tilemap>();
                if (ground != null) tilemap.SetTile(new Vector3Int(0, 0, 0), ground);
                if (obstacle != null) tilemap.SetTile(new Vector3Int(1, 0, 0), obstacle);
                PrefabUtility.SaveAsPrefabAsset(contents, AssetDatabase.GetAssetPath(palette));
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return palette;
        }

        public static string ChunkPath(string authorId, string workId, string localId)
            => ContentRoot + "/Works/" + Sanitize(authorId) + "/" + Sanitize(workId)
                + "/Chunks/" + Sanitize(localId) + ".prefab";

        public static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static Tilemap AddLayer(Transform grid, string name, int order, bool obstacle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            if (obstacle) go.AddComponent<TilemapCollider2D>();
            return tilemap;
        }

        private static void SetPort(SerializedProperty property, EdgePort value)
        {
            property.FindPropertyRelative("enabled").boolValue = value.Enabled;
            property.FindPropertyRelative("offset").intValue = value.Offset;
            property.FindPropertyRelative("width").intValue = value.Width;
        }

        private static string Sanitize(string name)
        {
            string result = new string((name ?? string.Empty)
                .Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
            return string.IsNullOrWhiteSpace(result) ? "Chunk" : result;
        }
    }
}
