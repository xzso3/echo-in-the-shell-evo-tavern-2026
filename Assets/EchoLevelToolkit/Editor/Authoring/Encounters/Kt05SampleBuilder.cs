using System;
using System.IO;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Level;
using Echo.LevelToolkit.Map;
using Echo.LevelToolkit.Map.Doors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Level.Editor
{
    // Generates independent authoring samples. Never modifies NativeDemo or a shared scene.
    public static class Kt05SampleBuilder
    {
        private const string Root = "Assets/EchoLevelToolkit/Samples/KT05";
        private const string Author = "echo";
        private static string work;

        [MenuItem("Echo/Level Toolkit/Generate KT-05 Clear Slice")]
        private static void ClearSlice() { Generate(false); }

        [MenuItem("Echo/Level Toolkit/Generate KT-05 Full Sample")]
        private static void FullSample() { Generate(true); }

        private static void Generate(bool full)
        {
            work = full ? "kt05-full-sample" : "kt05-clear-slice";
            string folder = Root + "/" + work;
            EnsureFolders(folder);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(folder + "/" + work + ".unity")
                || AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/level.prefab"))
                throw new InvalidOperationException("Sample already exists; duplicate it to preserve manual edits.");
            Sprite sprite = EnsureSprite(folder);
            Tile tile = EnsureTile(folder, sprite);
            Tile wallTile = EnsureWallTile(folder, sprite);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                GameObject root = BuildScene(folder, sprite, tile, wallTile, full);
                string prefabPath = folder + "/level.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                SetObject(prefab.GetComponent<SandboxLevelHost>(), "restartPrefab", prefab);
                UnityEngine.Object.DestroyImmediate(root);
                PrefabUtility.InstantiatePrefab(prefab, scene);
                string path = folder + "/" + work + ".unity";
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("Could not save sample scene: " + path);
                AssetDatabase.SaveAssets();
                Debug.Log("Generated KT-05 sample: " + path);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject BuildScene(string folder, Sprite sprite, Tile tile, Tile wallTile, bool full)
        {
            var root = new GameObject("KT-05 independent test level");
            var mapObject = new GameObject("Chunk map"); mapObject.transform.SetParent(root.transform);
            var chunkMap = mapObject.AddComponent<ChunkMap>();
            for (int origin = -6; origin < 12; origin += 6)
            {
                string variant = origin == -6 ? "left" : origin == 6 ? "right" : "middle";
                string prefabPath = folder + "/chunk_" + variant + "_6.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (!prefab) prefab = CreateChunkPrefab(prefabPath, tile, variant);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
                instance.name = "Chunk at " + origin;
                instance.transform.SetParent(mapObject.transform);
                instance.transform.localPosition = chunkMap.CellToLocal(new Vector2Int(origin, 0));
                instance.GetComponent<ChunkMapInstance>().SetCellOrigin(new Vector2Int(origin, 0));
            }
            CreateBoundaryMap(mapObject.transform, wallTile);

            var worldObject = new GameObject("Combat world"); worldObject.transform.SetParent(root.transform);
            CombatWorld world = worldObject.AddComponent<CombatWorld>();
            CombatProjectile projectile = CreateProjectile(root.transform, sprite);
            CombatPlayer player = CreatePlayer(worldObject.transform, sprite, projectile);
            var spawn = new GameObject("Player spawn").transform;
            spawn.SetParent(root.transform); spawn.position = new Vector3(-5.5f, 3f, 0);

            var doorsObject = new GameObject("Map doors"); doorsObject.transform.SetParent(mapObject.transform);
            MapDoorSet doorSet = doorsObject.AddComponent<MapDoorSet>();
            MapDoor clearLeft = CreateDoor(doorsObject.transform, sprite, "clear_left", -3f);
            MapDoor clearRight = CreateDoor(doorsObject.transform, sprite, "clear_right", 5f);
            MapDoor bossLeft = full ? CreateDoor(doorsObject.transform, sprite, "boss_left", 6f) : null;
            MapDoor bossRight = full ? CreateDoor(doorsObject.transform, sprite, "boss_right", 11f) : null;
            SetObjects(doorSet, "doors", full
                ? new UnityEngine.Object[] { clearLeft, clearRight, bossLeft, bossRight }
                : new UnityEngine.Object[] { clearLeft, clearRight });

            var stageObject = new GameObject("Level stage"); stageObject.transform.SetParent(root.transform);
            LevelStage stage = stageObject.AddComponent<LevelStage>();
            var encounterRoot = new GameObject("Encounters"); encounterRoot.transform.SetParent(stageObject.transform);
            CombatEnemy clearTarget = CreateEnemy(worldObject.transform, sprite, "Clear target", 1.5f, 3f);
            LevelEncounter clear = CreateEncounter(encounterRoot.transform, "Clear encounter", "clear",
                EncounterMode.ClearEnemies, new Vector2(1f, 3f), new Vector2(6f, 5.5f), clearTarget, null,
                new[] { clearLeft.DoorId, clearRight.DoorId });
            LevelEncounter free = null, bossEncounter = null;
            if (full)
            {
                CombatEnemy freeTarget = CreateEnemy(worldObject.transform, sprite, "Free target", -4.3f, 3f);
                freeTarget.detectionRange = 2f;
                free = CreateEncounter(encounterRoot.transform, "Free combat", "free", EncounterMode.FreeCombat,
                    new Vector2(-4.5f, 3f), new Vector2(2.5f, 5.5f), freeTarget, null,
                    Array.Empty<ContentIdentity>());
                CombatBoss boss = CreateBoss(worldObject.transform, sprite, projectile);
                bossEncounter = CreateEncounter(encounterRoot.transform, "Boss encounter", "boss",
                    EncounterMode.Boss, new Vector2(8.5f, 3f), new Vector2(3.8f, 5.5f), null, boss,
                    new[] { bossLeft.DoorId, bossRight.DoorId });
            }
            LevelExit exit = CreateExit(stageObject.transform, sprite, full ? 11.55f : 9.5f);
            SetId(stage, "levelId", "level");
            SetObject(stage, "doorSet", doorSet);
            SetObject(stage, "playerSpawn", spawn);
            SetObjects(stage, "encounters", full
                ? new UnityEngine.Object[] { free, clear, bossEncounter }
                : new UnityEngine.Object[] { clear });
            SetObjects(stage, "exits", new UnityEngine.Object[] { exit });

            LevelEndpointCatalog catalog = CreateCatalog(folder, full);
            SandboxBindingDefinition binding = CreateBinding(folder, full);
            SandboxLevelHost host = root.AddComponent<SandboxLevelHost>();
            SetObject(host, "world", world); SetObject(host, "player", player);
            SetObject(host, "weapon", player.GetComponent<CombatWeapon>());
            SetObject(host, "stage", stage); SetObject(host, "catalog", catalog);
            SetObject(host, "bindingDefinition", binding);
            var cameraObject = new GameObject("Sample camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(3, 3, -10);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.05f, .08f, .12f);
            if (LevelStageAuthoring.Validate(stage).Count != 0)
                throw new InvalidOperationException(string.Join("; ", LevelStageAuthoring.Validate(stage)));
            return root;
        }

        private static GameObject CreateChunkPrefab(string path, Tile tile, string variant)
        {
            var chunk = new GameObject("6x6 road chunk");
            var definition = chunk.AddComponent<ChunkDefinition>();
            chunk.AddComponent<ChunkMapInstance>();
            SetId(definition, "contentId", "chunk_" + variant + "_6");
            SetInt(definition, "size", 6);
            SetString(definition, "themeId", "test-road");
            if (variant != "right") SetPort(definition, "east", 6);
            if (variant != "left") SetPort(definition, "west", 6);
            var grid = new GameObject("Ground grid").AddComponent<Grid>();
            grid.transform.SetParent(chunk.transform, false);
            var layer = new GameObject("Ground"); layer.transform.SetParent(grid.transform, false);
            Tilemap map = layer.AddComponent<Tilemap>();
            layer.AddComponent<TilemapRenderer>().sortingOrder = -100;
            for (int x = 0; x < 6; x++) for (int y = 0; y < 6; y++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(chunk, path);
            UnityEngine.Object.DestroyImmediate(chunk);
            return prefab;
        }

        private static void CreateBoundaryMap(Transform parent, Tile tile)
        {
            var grid = new GameObject("Boundary grid").AddComponent<Grid>();
            grid.transform.SetParent(parent, false);
            var layer = new GameObject("Wall tiles"); layer.transform.SetParent(grid.transform, false);
            Tilemap map = layer.AddComponent<Tilemap>();
            layer.AddComponent<TilemapRenderer>().sortingOrder = -50;
            for (int x = -7; x <= 12; x++)
            { map.SetTile(new Vector3Int(x, -1, 0), tile); map.SetTile(new Vector3Int(x, 6, 0), tile); }
            for (int y = 0; y < 6; y++)
            { map.SetTile(new Vector3Int(-7, y, 0), tile); map.SetTile(new Vector3Int(12, y, 0), tile); }
            layer.AddComponent<TilemapCollider2D>(); layer.AddComponent<CombatObstacle>();
        }

        private static CombatProjectile CreateProjectile(Transform parent, Sprite sprite)
        {
            var go = new GameObject("Projectile template"); go.transform.SetParent(parent);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            go.transform.localScale = new Vector3(.2f, .12f, 1);
            CombatProjectile projectile = go.AddComponent<CombatProjectile>();
            go.SetActive(false);
            return projectile;
        }

        private static CombatPlayer CreatePlayer(Transform parent, Sprite sprite, CombatProjectile projectile)
        {
            var go = new GameObject("Standard test player"); go.transform.SetParent(parent);
            go.transform.position = new Vector3(-5.5f, 3, 0);
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.color = Color.cyan;
            go.transform.localScale = Vector3.one * .64f;
            Rigidbody2D body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            go.AddComponent<CircleCollider2D>().radius = .5f;
            CombatPlayer player = go.AddComponent<CombatPlayer>(); player.view = view;
            CombatWeapon weapon = go.AddComponent<CombatWeapon>(); weapon.projectilePrefab = projectile;
            return player;
        }

        private static CombatEnemy CreateEnemy(Transform parent, Sprite sprite, string name, float x, float y)
        {
            var go = new GameObject(name); go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0); go.transform.localScale = Vector3.one * .7f;
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.color = new Color(1, .45f, .2f);
            Rigidbody2D body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            go.AddComponent<CircleCollider2D>().radius = .5f;
            var enemy = go.AddComponent<CombatEnemy>(); enemy.view = view;
            enemy.health = 24; enemy.detectionRange = 4.5f;
            return enemy;
        }

        private static CombatBoss CreateBoss(Transform parent, Sprite sprite, CombatProjectile projectile)
        {
            var go = new GameObject("Core Boss"); go.transform.SetParent(parent);
            go.transform.position = new Vector3(8.5f, 3, 0);
            go.AddComponent<CircleCollider2D>().radius = .9f;
            var boss = go.AddComponent<CombatBoss>();
            var baseObject = new GameObject("Base"); baseObject.transform.SetParent(go.transform, false);
            boss.baseView = baseObject.AddComponent<SpriteRenderer>(); boss.baseView.sprite = sprite;
            boss.baseView.color = new Color(.5f, .6f, .8f);
            baseObject.transform.localScale = new Vector3(1.8f, 1.4f, 1);
            var pivot = new GameObject("Turret"); pivot.transform.SetParent(go.transform, false);
            boss.turretPivot = pivot.transform;
            boss.turretView = pivot.AddComponent<SpriteRenderer>(); boss.turretView.sprite = sprite;
            boss.turretView.color = new Color(.9f, .7f, .3f);
            pivot.transform.localScale = new Vector3(.8f, .25f, 1);
            var core = new GameObject("Core"); core.transform.SetParent(go.transform, false);
            boss.coreView = core.AddComponent<SpriteRenderer>(); boss.coreView.sprite = sprite;
            core.transform.localScale = Vector3.one * .35f;
            boss.hostileProjectilePrefab = projectile;
            boss.maxArmor = 60;
            boss.armorDamageScale = 1;
            return boss;
        }

        private static MapDoor CreateDoor(Transform parent, Sprite sprite, string localId, float x)
        {
            var go = new GameObject(localId); go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, 3, 0);
            BoxCollider2D blocker = go.AddComponent<BoxCollider2D>(); blocker.size = new Vector2(.3f, 6f);
            var door = go.AddComponent<MapDoor>();
            var visual = new GameObject("Closed visual"); visual.transform.SetParent(go.transform, false);
            var view = visual.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.color = new Color(1, .2f, .2f, .8f);
            view.sortingOrder = 110;
            visual.transform.localScale = new Vector3(.3f, 6f, 1);
            SetId(door, "doorId", localId);
            SetId(door, "setOpenEndpoint", localId + "_set");
            SetObject(door, "blocker", blocker);
            SetObject(door, "closedVisual", visual);
            return door;
        }

        private static LevelEncounter CreateEncounter(Transform parent, string name, string id,
            EncounterMode mode, Vector2 center, Vector2 size, CombatEnemy enemy, CombatBoss boss,
            ContentIdentity[] sealDoors)
        {
            var go = new GameObject(name); go.transform.SetParent(parent);
            go.transform.position = center;
            var area = go.AddComponent<PolygonCollider2D>(); area.isTrigger = true;
            Vector2 half = size * .5f;
            area.SetPath(0, new[] { new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y),
                new Vector2(half.x, half.y), new Vector2(-half.x, half.y) });
            var encounter = go.AddComponent<LevelEncounter>();
            SetId(encounter, "encounterId", id);
            SetId(encounter, "regionEnteredEndpoint", id + "_enter");
            if (mode != EncounterMode.FreeCombat)
            { SetId(encounter, "activateEndpoint", id + "_activate");
              SetId(encounter, "completedEndpoint", id + "_done"); }
            SetEnum(encounter, "mode", (int)mode);
            SetObjects(encounter, "enemies", enemy
                ? new UnityEngine.Object[] { enemy } : Array.Empty<UnityEngine.Object>());
            SetObject(encounter, "boss", boss);
            SetIds(encounter, "sealDoors", sealDoors);
            return encounter;
        }

        private static LevelExit CreateExit(Transform parent, Sprite sprite, float x)
        {
            var go = new GameObject("Test exit"); go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, 3, 0);
            var trigger = go.AddComponent<BoxCollider2D>(); trigger.isTrigger = true;
            trigger.size = new Vector2(.8f, 2f);
            var marker = new GameObject("Locked marker"); marker.transform.SetParent(go.transform, false);
            var view = marker.AddComponent<SpriteRenderer>(); view.sprite = sprite;
            view.color = new Color(.3f, 1f, .4f, .7f);
            view.sortingOrder = 108;
            marker.transform.localScale = new Vector3(.8f, 2f, 1);
            LevelExit exit = go.AddComponent<LevelExit>();
            SetId(exit, "exitId", "test_exit"); SetId(exit, "reachedEndpoint", "exit_reached");
            SetId(exit, "unlockEndpoint", "exit_unlock"); SetObject(exit, "lockedVisual", marker);
            return exit;
        }

        private static LevelEndpointCatalog CreateCatalog(string folder, bool full)
        {
            string path = folder + "/endpoints.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelEndpointCatalog>(path);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<LevelEndpointCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            var serialized = new SerializedObject(catalog);
            SetId(serialized.FindProperty("levelIdentity"), "level");
            var endpoints = serialized.FindProperty("endpoints");
            int count = full ? 13 : 7;
            endpoints.arraySize = count;
            int i = 0;
            AddEndpoint(endpoints, i++, "clear_enter", LevelEndpointKind.RegionEntered, "Clear encounter");
            AddEndpoint(endpoints, i++, "clear_activate", LevelEndpointKind.ActivateEncounter, "Clear encounter");
            AddEndpoint(endpoints, i++, "clear_done", LevelEndpointKind.EncounterCompleted, "Clear encounter");
            foreach (string id in new[] { "clear_left", "clear_right" })
                AddEndpoint(endpoints, i++, id + "_set", LevelEndpointKind.SetDoorOpen, "Map doors/" + id);
            if (full)
            {
                AddEndpoint(endpoints, i++, "free_enter", LevelEndpointKind.RegionEntered, "Free combat");
                AddEndpoint(endpoints, i++, "boss_enter", LevelEndpointKind.RegionEntered, "Boss encounter");
                AddEndpoint(endpoints, i++, "boss_activate", LevelEndpointKind.ActivateEncounter, "Boss encounter");
                AddEndpoint(endpoints, i++, "boss_done", LevelEndpointKind.EncounterCompleted, "Boss encounter");
                foreach (string id in new[] { "boss_left", "boss_right" })
                    AddEndpoint(endpoints, i++, id + "_set", LevelEndpointKind.SetDoorOpen, "Map doors/" + id);
            }
            AddEndpoint(endpoints, i++, "exit_reached", LevelEndpointKind.ExitReached, "Test exit");
            AddEndpoint(endpoints, i++, "exit_unlock", LevelEndpointKind.UnlockExit, "Test exit");
            if (i != count) throw new InvalidOperationException("Endpoint count mismatch.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AddEndpoint(SerializedProperty array, int index, string localId,
            LevelEndpointKind kind, string path)
        {
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            SetId(entry.FindPropertyRelative("identity"), localId);
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind - 1;
            entry.FindPropertyRelative("objectPath").stringValue = path;
            entry.FindPropertyRelative("description").stringValue = localId;
            entry.FindPropertyRelative("requiredBinding").boolValue =
                kind == LevelEndpointKind.EncounterCompleted || kind == LevelEndpointKind.ExitReached;
            entry.FindPropertyRelative("allowedModes").intValue = (int)LevelBindingModes.Both;
        }

        private static SandboxBindingDefinition CreateBinding(string folder, bool full)
        {
            string path = folder + "/sandbox_binding.asset";
            var binding = AssetDatabase.LoadAssetAtPath<SandboxBindingDefinition>(path);
            if (!binding) { binding = ScriptableObject.CreateInstance<SandboxBindingDefinition>(); AssetDatabase.CreateAsset(binding, path); }
            var serialized = new SerializedObject(binding);
            var required = serialized.FindProperty("requiredEncounterEvents");
            required.arraySize = full ? 2 : 1;
            SetId(required.GetArrayElementAtIndex(0), "clear_done");
            if (full) SetId(required.GetArrayElementAtIndex(1), "boss_done");
            SetId(serialized.FindProperty("testExitEvent"), "exit_reached");
            SetId(serialized.FindProperty("unlockExitAction"), "exit_unlock");
            var triggers = serialized.FindProperty("encounterTriggers");
            triggers.arraySize = full ? 2 : 1;
            SetId(triggers.GetArrayElementAtIndex(0).FindPropertyRelative("regionEnteredEndpoint"), "clear_enter");
            SetId(triggers.GetArrayElementAtIndex(0).FindPropertyRelative("activateEncounterEndpoint"), "clear_activate");
            if (full)
            {
                SetId(triggers.GetArrayElementAtIndex(1).FindPropertyRelative("regionEnteredEndpoint"), "boss_enter");
                SetId(triggers.GetArrayElementAtIndex(1).FindPropertyRelative("activateEncounterEndpoint"), "boss_activate");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
            return binding;
        }

        private static void EnsureFolders(string folder)
        {
            string current = "Assets";
            foreach (string part in folder.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static Sprite EnsureSprite(string folder)
        {
            string path = folder + "/pixel.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Tile EnsureTile(string folder, Sprite sprite)
        {
            string path = folder + "/floor_tile.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (!tile) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.color = new Color(.12f, .18f, .25f);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tile EnsureWallTile(string folder, Sprite sprite)
        {
            string path = folder + "/wall_tile.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (!tile) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.color = new Color(.3f, .38f, .5f);
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void SetId(UnityEngine.Object target, string field, string local)
        {
            var serialized = new SerializedObject(target);
            SetId(serialized.FindProperty(field), local);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetId(SerializedProperty property, string local)
        {
            property.FindPropertyRelative("authorId").stringValue = Author;
            property.FindPropertyRelative("workId").stringValue = work;
            property.FindPropertyRelative("localId").stringValue = local;
        }
        private static void SetIds(UnityEngine.Object target, string field, ContentIdentity[] ids)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(field);
            array.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) SetId(array.GetArrayElementAtIndex(i), ids[i].LocalId);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetObjects(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetEnum(UnityEngine.Object target, string field, int index)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = index;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetPort(UnityEngine.Object target, string field, int width)
        {
            var serialized = new SerializedObject(target);
            var port = serialized.FindProperty(field);
            port.FindPropertyRelative("enabled").boolValue = true;
            port.FindPropertyRelative("offset").intValue = 0;
            port.FindPropertyRelative("width").intValue = width;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
