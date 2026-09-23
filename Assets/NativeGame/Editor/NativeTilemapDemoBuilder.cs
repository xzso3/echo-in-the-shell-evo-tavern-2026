using System;
using System.Linq;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Map;
using Echo.LevelToolkit.Map.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.NativeGame.Editor
{
    // Adapts ChunkMapSample's four-layer prefab authoring to the existing Native story.
    // Creates a separate scene; never reruns the legacy scene migrations.
    public static class NativeTilemapDemoBuilder
    {
        public const string ScenePath = "Assets/Scenes/NativeDemoTilemap.unity";
        const string ArtRoot = "Assets/NativeGame/TilemapDemo";
        static Tile asphalt, paving, metal, hazard, lane, grate, wall;
        static Material material;

        [MenuItem("Echo/Native/Create Tilemap Demo")]
        public static void Create()
        {
            if (Application.isPlaying || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException("Stop Play Mode. The saved Tilemap demo is never regenerated.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before creating a new demo.");
            ChunkAuthoring.EnsureFolder(ArtRoot);
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberCity/Materials/Actors.mat");
            asphalt = Tile("Asphalt", "Wet asphalt", false);
            paving = Tile("Paving", "Concrete paving", false);
            metal = Tile("Metal", "Maintenance plate", false);
            hazard = Tile("Hazard", "Hazard floor", false);
            lane = Tile("Lane", "Yellow lane", false);
            grate = Tile("Grate", "Drain grate", false);
            wall = Tile("Wall", "Maintenance plate", true);
            var palette = ChunkAuthoring.CreateSharedPalette("NativeDemoCity", asphalt, wall);
            var paletteRoot = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(palette));
            try
            {
                var paletteMap = paletteRoot.GetComponentInChildren<Tilemap>();
                var tiles = new[] { asphalt, wall, paving, metal, hazard, lane, grate };
                for (int i = 0; i < tiles.Length; i++) paletteMap.SetTile(new Vector3Int(i, 0, 0), tiles[i]);
                PrefabUtility.SaveAsPrefabAsset(paletteRoot, AssetDatabase.GetAssetPath(palette));
            }
            finally { PrefabUtility.UnloadPrefabContents(paletteRoot); }

            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            // Save under a new identity before making any scene changes.
            EditorSceneManager.SaveScene(scene, ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            var world = run.map.transform;
            var gates = new[] { run.map.exitGate, run.map.arenaEntryGate, run.map.finalGate, run.map.northRouteDoor };
            foreach (var chunk in run.map.northRouteChunks)
                if (chunk) UnityEngine.Object.DestroyImmediate(chunk.gameObject);
            run.map.northRouteChunks = new NativeChunk[0];
            foreach (var obstacle in world.GetComponentsInChildren<NativeObstacle>(true))
                if (!gates.Contains(obstacle.gameObject)) UnityEngine.Object.DestroyImmediate(obstacle.gameObject);
            foreach (var t in world.GetComponentsInChildren<Transform>(true).ToArray())
                if (t && (t.name.StartsWith("Ground -") || t.name.Contains("bypass lane")))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);

            var map = new GameObject("City — five Tilemap chunks").AddComponent<ChunkMap>();
            map.transform.SetParent(world, false);
            var closed = new EdgePort(false, 0, 0);
            var wide = new EdgePort(true, 1, 16);
            var north = new EdgePort(true, 4, 4);
            var branch = new EdgePort(true, 3, 4);
            Place(map, "Street18", 18, new Vector2Int(-16, -9), north, wide, closed, closed);
            Place(map, "Archive18", 18, new Vector2Int(2, -9), closed, wide, closed, wide);
            Place(map, "Arena18", 18, new Vector2Int(20, -9), closed, closed, closed, wide);
            Place(map, "Workshop10", 10, new Vector2Int(-15, 9), branch, closed, branch, closed);
            Place(map, "Uplink10", 10, new Vector2Int(-15, 19), closed, closed, branch, closed);
            var report = ChunkMapGeometry.Inspect(map.GetPlacements());
            if (report.Issues.Count != 0) throw new InvalidOperationException(string.Join("; ", report.Issues.Select(i => i.Code)));

            Gate(run.map.exitGate, new Vector2(10.5f, 0), false);
            Gate(run.map.arenaEntryGate, new Vector2(14.5f, 0), false);
            Gate(run.map.finalGate, new Vector2(31.5f, 0), false);
            Gate(run.map.northRouteDoor, new Vector2(-10, 19), true);
            run.map.arenaEntryGate.SetActive(false);
            run.combatIntegration.workId = "native-demo-tilemap";
            var camera = UnityEngine.Object.FindObjectOfType<NativeCamera>();
            camera.worldCenter = new Vector2(11, 10);
            camera.worldHalfSize = new Vector2(27, 19);
            camera.GetComponent<Camera>().orthographicSize = 6;
            camera.GetComponent<Camera>().allowMSAA = false;
            camera.GetComponent<Camera>().allowHDR = false;
            camera.transform.position = new Vector3(-8, 0, -10);

            Label(world, "BYPASS", "检修通道  →", new Vector2(-6, 7));
            Label(world, "GUARDED CHECKPOINT", "警戒街道  →", new Vector2(-4, -7));
            Label(world, "DEVELOPMENT ENCOUNTER", "封锁区 / 核心机体", new Vector2(23, 7));
            Label(world, "北侧区块 1", "北区 / 线圈工坊", new Vector2(-10, 12.5f));
            Label(world, "北侧区块 2", "北区 / 脉冲上行站", new Vector2(-10, 26.5f));
            AddProps(world);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ConfigureBuildEntry();
            AssetDatabase.SaveAssets();
            Debug.Log("NATIVE_TILEMAP_CREATED: 5 chunks, 0 topology issues; original story references retained.");
        }

        public static void ConfigureBuildEntry()
        {
            // Keep every existing entry; the new playable becomes the build startup scene.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
        }

        [MenuItem("Echo/Native/Open Tilemap Playable")]
        public static void Open()
        {
            if (!Application.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        static Tile Tile(string name, string source, bool blocks)
        {
            string path = ArtRoot + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing) return existing;
            var original = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberCity/Sprites/" + source + ".asset");
            // A square interior crop removes atlas gutters; one tile is exactly one world unit.
            var rect = original.rect;
            rect = new Rect(Mathf.Floor(rect.center.x - 48), Mathf.Floor(rect.center.y - 48), 96, 96);
            var sprite = Sprite.Create(original.texture, rect, Vector2.one * .5f, 96, 0, SpriteMeshType.FullRect);
            sprite.name = name + "96";
            var result = ScriptableObject.CreateInstance<Tile>();
            result.sprite = sprite;
            result.colliderType = blocks ? UnityEngine.Tilemaps.Tile.ColliderType.Grid : UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(result, path);
            AssetDatabase.AddObjectToAsset(sprite, result);
            return result;
        }

        static void Place(ChunkMap map, string id, int size, Vector2Int origin,
            EdgePort n, EdgePort e, EdgePort s, EdgePort w)
        {
            string path = ChunkAuthoring.ChunkPath("echo", "native-demo-tilemap", id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab)
            {
                ChunkAuthoring.CreateChunk("echo", "native-demo-tilemap", id, size, "cyber-city", n, e, s, w);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var layers = root.GetComponent<ChunkTileLayers>();
                    layers.Obstacles.gameObject.AddComponent<NativeObstacle>();
                    foreach (var renderer in root.GetComponentsInChildren<TilemapRenderer>()) renderer.sharedMaterial = material;
                    layers.Ground.GetComponent<TilemapRenderer>().sortingOrder = -1000;
                    layers.Decoration.GetComponent<TilemapRenderer>().sortingOrder = -900;
                    layers.Obstacles.GetComponent<TilemapRenderer>().sortingOrder = -50;
                    for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int wx = origin.x + x, wy = origin.y + y;
                        bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                        bool opening = y == size - 1 && Covers(n, x) || x == size - 1 && Covers(e, y)
                            || y == 0 && Covers(s, x) || x == 0 && Covers(w, y);
                        bool block = edge && !opening;
                        // Static partitions flank the same four-unit dynamic story gates.
                        if (wy < 8 && wy >= -8 && (wx == 10 || wx == 14 || wx == 31) && (wy < -2 || wy >= 2)) block = true;
                        if (wx >= -1 && wx <= 0 && wy >= -4 && wy <= 3) block = true;
                        var cell = new Vector3Int(x, y, 0);
                        Tile ground = wy >= 9 ? metal : wx >= 15 ? metal : asphalt;
                        if (wy < 9 && (Mathf.Abs(wy) >= 5 || wx <= -12 || wx >= 33)) ground = paving;
                        if (wx > 15 && wx < 31 && (wy == -6 || wy == 5)) ground = hazard;
                        if (wy < 9 && wx < 10 && (wy == -5 || wy == 5)) ground = lane;
                        if (wy >= 9 && (x == 2 || x == 7)) ground = lane;
                        if ((wx * 7 + wy * 3) % 29 == 0 && !block) ground = grate;
                        layers.Ground.SetTile(cell, ground);
                        if (block)
                        {
                            layers.Obstacles.SetTile(cell, wall);
                            layers.Obstacles.SetTileFlags(cell, TileFlags.None);
                            layers.Obstacles.SetColor(cell, new Color(.40f, .58f, .65f));
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                ChunkAiMerge.RecordGeneratedPrefabBaseline(path);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            ChunkAuthoring.Place(map, prefab.GetComponent<ChunkDefinition>(), origin);
        }

        static bool Covers(EdgePort port, int value) => port.Enabled && value >= port.Offset && value < port.EndExclusive;
        static void Gate(GameObject gate, Vector2 position, bool horizontal)
        {
            gate.transform.position = position;
            gate.transform.localScale = horizontal ? new Vector3(4, .45f, 1) : new Vector3(.45f, 4, 1);
            gate.GetComponent<SpriteRenderer>().sprite = hazard.sprite;
            gate.GetComponent<SpriteRenderer>().color = new Color(1, .65f, .35f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gate.transform);
        }

        static void Label(Transform parent, string name, string text, Vector2 position)
        {
            var label = parent.Find(name);
            if (!label) return;
            label.position = position;
            label.GetComponent<TMP_Text>().text = text;
        }

        static void AddProps(Transform world)
        {
            var root = new GameObject("City dressing — on blocked cells").transform;
            root.SetParent(world, false);
            string[] types = { "Wall", "Pipe wall", "Service door", "Cable cabinet" };
            for (int x = -14; x < 37; x += 3)
            {
                if (x >= -12 && x < -8) continue;
                Prop(types[(x + 14) / 3 % types.Length], new Vector2(x + .5f, 8.5f), root);
            }
            Prop("Vending machine", new Vector2(-15.5f, 2), root);
            Prop("Transformer", new Vector2(-.5f, -1), root);
            Prop("Fan", new Vector2(-.5f, 2), root);
            Prop("Guard booth", new Vector2(14.5f, 5), root);
            Prop("Cable cabinet", new Vector2(31.5f, 5), root);
            Prop("Data beacon", new Vector2(36.5f, 6), root);
            for (int y = 11; y < 29; y += 4) Prop("Power cabinet", new Vector2(-14.5f, y), root);
        }

        static void Prop(string name, Vector2 position, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/CyberCity/Meshes/" + name + ".asset");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberCity/Materials/CityAtlas.mat");
            renderer.sortingOrder = 30;
        }
    }
}
