using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace PlagueSurvivor.Editor
{
    public static class BossArenaBuilder
    {
        const string Art = "Assets/Art/Generated/Cyberpunk/BossArena/";
        const string Root = "Assets/BossArena";
        public const string ScenePath = "Assets/Scenes/BossArena.unity";
        static Sprite Import(string relative)
        {
            string path = Art + relative + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing Boss artwork: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        [MenuItem("Neural Lockdown/Create Independent Boss Arena")]
        public static void CreateFromMenu() { Debug.Log(Create()); }
        public static string Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new InvalidOperationException("BossArena already exists; open it without rebuilding.");
            var source = EditorSceneManager.GetActiveScene();
            if (source.path != "Assets/Scenes/CyberCity.unity" || source.isDirty) throw new InvalidOperationException("Open saved CyberCity first. Unsaved work is never overwritten.");
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "BossArena");
            var config = ScriptableObject.CreateInstance<BossArenaConfig>();
            config.material = UnityEngine.Object.FindObjectOfType<PlagueGame>().player.sharedMaterial;
            config.baseSprite = Import("Boss/warden_base"); config.turret = Import("Boss/warden_turret");
            config.core = Import("Boss/warden_core_mask"); config.damageOverlay = Import("Boss/warden_damage_overlay"); config.wreck = Import("Boss/warden_wreck");
            config.projectile = Import("Telegraphs/boss_burst_projectile"); config.bombRing = Import("Telegraphs/bomb_warning_ring");
            config.gridWarningSprite = Import("Telegraphs/electric_warning_tile"); config.gridActiveSprite = Import("Telegraphs/electric_active_tile");
            config.aimTracking = Import("Telegraphs/aim_tracking_endpoint"); config.aimLocked = Import("Telegraphs/aim_locked_endpoint");
            config.explosions = new Sprite[4]; for (int i = 0; i < 4; i++) config.explosions[i] = Import("Telegraphs/bomb_explosion_" + (i + 1));
            config.medkit = Import("Supplies/supply_medkit"); config.supplyMarker = Import("Supplies/supply_drop_marker");
            config.startMarker = Import("Arena/arena_start_marker"); config.boundary = Import("Arena/arena_boundary_straight"); config.corner = Import("Arena/arena_boundary_corner");
            config.healthFrame = Import("UI/boss_healthbar_frame"); config.healthFill = Import("UI/boss_healthbar_fill");
            AssetDatabase.CreateAsset(config, Root + "/BossArenaConfig.asset");
            // Save-as-copy preserves every reference in the existing player and UI; source scene is untouched.
            EditorSceneManager.SaveScene(source, ScenePath, true);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindObjectOfType<PlagueGame>();
            var environment = GameObject.Find("CYBER CITY - Modular atlas construction");
            if (environment) UnityEngine.Object.DestroyImmediate(environment);
            UnityEngine.Object.DestroyImmediate(game.city); game.city = null;
            game.name = "Boss Encounter - Tune Config Asset"; game.enemyLimit = 0; game.arenaHalfSize = config.halfSize;
            game.testDialogue = null; game.player.transform.position = config.playerSpawn;
            var encounter = game.gameObject.AddComponent<BossEncounterController>(); encounter.config = config; game.bossEncounter = encounter;
            var camera = Camera.main; camera.gameObject.AddComponent<BossArenaCamera>();
            camera.allowHDR = false; camera.allowMSAA = false; camera.allowDynamicResolution = false;
            camera.backgroundColor = new Color(.014f, .022f, .035f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographicSize = 8.4f; camera.transform.position = new Vector3(0, .5f, -10);
            var arena = new GameObject("Arena - 18x12 - No Interior Obstacles").transform;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/Cyberpunk/cyber_ground_64x44_32px.png");
            var ground = Sprite.Create(texture, new Rect((texture.width - 576) / 2, (texture.height - 384) / 2, 576, 384), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
            ground.name = "Ground crop 18x12 at native PPU32"; AssetDatabase.CreateAsset(ground, Root + "/ArenaGround.asset");
            BossArenaVisual.Sprite("Native scale ground crop", ground, Vector2.zero, arena, config, -1000);
            var mask = new GameObject("Clip effects to actual arena").AddComponent<SpriteMask>(); mask.transform.SetParent(arena, false); mask.sprite = ground;
            var bounds = new GameObject("Energy perimeter").transform; bounds.SetParent(arena, false);
            for (int x = -8; x <= 8; x++) foreach (int side in new[] { -1, 1 })
                BossArenaVisual.Sprite("Horizontal boundary", config.boundary, new Vector2(x, side * 6), bounds, config, 50);
            for (int y = -5; y <= 5; y++) foreach (int side in new[] { -1, 1 })
                BossArenaVisual.Sprite("Vertical boundary", config.boundary, new Vector2(side * 9, y), bounds, config, 50).transform.rotation = Quaternion.Euler(0, 0, 90);
            foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 })
            {
                var corner = BossArenaVisual.Sprite("Boundary corner", config.corner, new Vector2(x * 9, y * 6), bounds, config, 51);
                corner.flipX = x > 0; corner.flipY = y < 0;
            }
            encounter.startMarker = BossArenaVisual.Sprite("Start pad - enter to activate", config.startMarker, config.startPosition, arena, config, 20);
            var canvas = game.status.canvas;
            var title = canvas.transform.Find("Title").GetComponent<TMP_Text>(); title.text = "NEURAL LOCKDOWN  /  WARDEN SECTOR";
            title.fontSize = 18;
            game.status.rectTransform.sizeDelta = new Vector2(300, 24);
            var label = UnityEngine.Object.Instantiate(title, canvas.transform); label.name = "Boss Name";
            Layout(label.rectTransform, new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(630, 30));
            label.alignment = TextAlignmentOptions.Center; label.fontSize = 20; encounter.bossLabel = label;
            Layout(title.rectTransform, new Vector2(0, 1), new Vector2(22, -14), new Vector2(290, 25)); title.fontSize = 13;
            var track = Image("Boss health track", canvas.transform, config.healthFill, new Color(.08f, .15f, .2f));
            Layout(track.rectTransform, new Vector2(.5f, 1), new Vector2(0, -50), new Vector2(480, 27));
            var fill = Image("Boss health", canvas.transform, game.healthFill.sprite, Color.cyan);
            Layout(fill.rectTransform, new Vector2(.5f, 1), new Vector2(0, -60), new Vector2(418, 8));
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; encounter.bossHealthFill = fill;
            var frame = Image("Boss health frame", canvas.transform, config.healthFrame, Color.white);
            Layout(frame.rectTransform, new Vector2(.5f, 1), new Vector2(0, -50), new Vector2(480, 27));
            var cue = UnityEngine.Object.Instantiate(label, canvas.transform); cue.name = "Boss Skill Cue"; cue.fontSize = 17;
            Layout(cue.rectTransform, new Vector2(.5f, 1), new Vector2(0, -89), new Vector2(1190, 30)); encounter.skillLabel = cue;
            var header = canvas.transform.Find("Header").GetComponent<RectTransform>(); header.sizeDelta = new Vector2(header.sizeDelta.x, 126);
            game.overlay.transform.SetAsLastSibling();
            game.message.fontSize = 17;
            encounter.bossLabel.text = "WARDEN-01    /    DISTRICT WARDEN    2600 / 2600";
            encounter.skillLabel.text = "COLLECT GEAR  /  E TO EQUIP  /  ENTER THE CYAN START PAD";
            game.message.text = "WASD Move    AUTO FIRE    E Bag    ESC Pause    R Restart";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var builds = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); builds.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets(); Selection.activeGameObject = game.gameObject;
            return "Created independent BossArena, native 18x12 crop, zero city obstacles, reused player/inventory and Boss art. CyberCity is unchanged.";
        }
        static UnityEngine.UI.Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            image.transform.SetParent(parent, false); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image;
        }
        static void Layout(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
    }
}
