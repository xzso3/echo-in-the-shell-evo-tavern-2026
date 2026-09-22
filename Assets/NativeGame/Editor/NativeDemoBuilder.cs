using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace Echo.NativeGame.Editor
{
    // One concrete scene authoring command. It does not alter build settings or historical scenes.
    public static class NativeDemoBuilder
    {
        const string Root = "Assets/NativeGame";
        public const string ScenePath = "Assets/Scenes/NativeDemo.unity";
        static Material material;
        static TMP_FontAsset font;
        static Sprite square;
        static Color Cyan => new Color(.16f, .94f, .83f);
        static Color Dark => new Color(.025f, .048f, .065f, .96f);
        static T Asset<T>(string path) where T : UnityEngine.Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (!asset) throw new InvalidOperationException("NativeDemo missing asset: " + path); return asset; }
        static string DetectPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline == null ? "BuiltIn" : pipeline.GetType().FullName;
        }
        [MenuItem("Echo/Native/Create First Playable")]
        public static void Create()
        {
            if (Application.isPlaying || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException("Stop Play Mode; existing NativeDemo is never overwritten by this builder.");
            Debug.Log("NativeDemo render pipeline: " + DetectPipeline());
            material = Asset<Material>("Assets/CyberCity/Materials/Actors.mat");
            font = Asset<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("World").transform;
            var actors = new GameObject("Actors").transform;
            var run = new GameObject("Run").AddComponent<NativeRunController>();
            run.map = world.gameObject.AddComponent<NativeMap>();
            run.quest = new GameObject("Quest").AddComponent<NativeQuest>();
            run.combat = new GameObject("Combat").AddComponent<NativeCombat>();
            run.rules = new GameObject("ECA - scene rules").AddComponent<NativeEcaRules>();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Native solid marker" };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
            AssetDatabase.CreateAsset(texture, Root + "/SolidMarkerTexture.asset");
            square = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2);
            AssetDatabase.CreateAsset(square, Root + "/SolidMarker.asset");
            var groundTexture = Asset<Texture2D>("Assets/Art/Generated/Cyberpunk/cyber_ground_64x44_32px.png");
            var ground = Sprite.Create(groundTexture, new Rect((groundTexture.width - 960) / 2, (groundTexture.height - 576) / 2, 960, 576), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
            AssetDatabase.CreateAsset(ground, Root + "/GroundCrop.asset");
            Draw("Ground - reused city artwork", ground, Vector2.zero, world, -1000);
            Wall("North perimeter", new Vector2(0, 9), new Vector2(30.5f, .5f), world);
            Wall("South perimeter", new Vector2(0, -9), new Vector2(30.5f, .5f), world);
            Wall("West perimeter", new Vector2(-15, 0), new Vector2(.5f, 18), world);
            Wall("East perimeter", new Vector2(15, 0), new Vector2(.5f, 18), world);
            Wall("Central machinery - routes above and below", Vector2.zero, new Vector2(3, 8), world);
            Wall("Exit partition north", new Vector2(10, 5.5f), new Vector2(.55f, 7), world);
            Wall("Exit partition south", new Vector2(10, -5.5f), new Vector2(.55f, 7), world);
            run.map.exitGate = Wall("Gate - relay opens this collider", new Vector2(10, 0), new Vector2(.55f, 4), world);
            run.map.exitGate.GetComponent<SpriteRenderer>().color = new Color(1, .32f, .2f);
            for (int y = -3; y <= 3; y += 2) Prop("Power cabinet", new Vector2(0, y), world);
            for (int x = -12; x < 10; x += 3)
            {
                Draw("Upper bypass lane", Asset<Sprite>("Assets/CyberCity/Sprites/Yellow lane.asset"), new Vector2(x, 6), world, -900).transform.localScale = new Vector3(.55f, .55f, 1);
                Draw("Lower bypass lane", Asset<Sprite>("Assets/CyberCity/Sprites/Yellow lane.asset"), new Vector2(x, -6), world, -900).transform.localScale = new Vector3(.55f, .55f, 1);
            }
            var boltObject = Draw("Pulse projectile", Asset<Sprite>("Assets/CyberCity/Sprites/Smart round.asset"), Vector2.zero, null, 250).gameObject;
            boltObject.transform.localScale = Vector3.one * .65f;
            boltObject.AddComponent<NativeProjectile>();
            var boltPrefab = PrefabUtility.SaveAsPrefabAsset(boltObject, Root + "/Prefabs/Pulse.prefab").GetComponent<NativeProjectile>();
            UnityEngine.Object.DestroyImmediate(boltObject);
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<NativePlayer>();
            player.view = Draw("Visual", Asset<Sprite>("Assets/CyberCity/Sprites/Cyber operative.asset"), Vector2.zero, playerObject.transform, 100);
            player.view.transform.localPosition = new Vector3(0, -.2f, 0);
            run.combat.projectilePrefab = boltPrefab; Body(playerObject, .32f);
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObject, Root + "/Prefabs/Player.prefab");
            UnityEngine.Object.DestroyImmediate(playerObject);
            run.player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, actors)).GetComponent<NativePlayer>();
            run.player.transform.position = new Vector3(-11, 0, 0); run.player.run = run;
            var enemyObject = new GameObject("Security drone - development enemy");
            var enemy = enemyObject.AddComponent<NativeEnemy>();
            enemy.view = Draw("Visual", Asset<Sprite>("Assets/CyberCity/Sprites/Security robot v3.asset"), Vector2.zero, enemyObject.transform, 100);
            enemy.view.transform.localPosition = new Vector3(0, -.2f, 0); Body(enemyObject, .4f);
            var enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, Root + "/Prefabs/SecurityDrone.prefab");
            UnityEngine.Object.DestroyImmediate(enemyObject);
            foreach (var point in new[] { new Vector2(-5, 1), new Vector2(-2, 5.8f), new Vector2(4, -5), new Vector2(7, 3) })
            {
                var instance = ((GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, actors)).GetComponent<NativeEnemy>();
                instance.transform.position = point; instance.run = run;
            }
            var relay = Interaction("Relay terminal", new Vector2(6, 0), false, world, run);
            Prop("Data beacon", new Vector2(6, 0), world);
            var exit = Interaction("Exit uplink", new Vector2(13, 0), true, world, run);
            WorldText("TERMINAL", new Vector2(6, 2), world, Cyan);
            WorldText("EXIT", new Vector2(13, 2), world, Cyan);
            WorldText("BYPASS", new Vector2(-6, 7.5f), world, new Color(.9f, .8f, .4f));
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 7.1f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.014f, .025f, .04f);
            camera.allowHDR = false; camera.allowMSAA = false; camera.allowDynamicResolution = false;
            camera.transform.position = new Vector3(-3.4f, 0, -10);
            cameraObject.AddComponent<NativeCamera>().target = run.player.transform;
            BuildUI(run);
            WireModules(run, relay, exit);
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("NativeDemo created: 1 player, 4 enemies, 2 interactions, native Physics2D, Canvas HUD and phone. Build settings unchanged.");
        }
        [MenuItem("Echo/Native/Open Playable")]
        public static void OpenPlayable()
        {
            if (Application.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        static void WireModules(NativeRunController run, NativeInteraction relay, NativeInteraction exit)
        {
            run.player.run = run;
            run.combat.level = run; run.combat.actor = run.player;
            run.hud.level = run; run.hud.interactables = new[] { relay, exit };
            relay.run = exit.run = run; relay.map = exit.map = run.map;
            run.rules.level = run; run.rules.terminal = relay; run.rules.exit = exit;
            run.rules.quest = run.quest; run.rules.map = run.map; run.rules.dialogue = run.dialogue;
        }
        static void BuildDialogue(NativeRunController run, Transform canvas)
        {
            run.dialogue = canvas.gameObject.AddComponent<NativeDialogue>();
            var panel = Panel("Dialogue", canvas, new Vector2(.5f, 0), new Vector2(0, 90), new Vector2(690, 150), Dark, true);
            run.dialogue.panel = panel.gameObject;
            run.dialogue.message = Text("Transmission", "", panel.transform, new Vector2(0, 1), new Vector2(24, -20), new Vector2(642, 72), 21, Cyan);
            run.dialogue.closeButton = Button("Acknowledge", "ACKNOWLEDGE  /  E", panel.transform, new Vector2(0, 14), new Vector2(300, 40));
            panel.gameObject.SetActive(false);
        }
        // One-time in-place migration of the U1 scene. Existing artwork, geometry and Prefab identities stay intact.
        [MenuItem("Echo/Native/Connect Scene ECA")]
        public static void ConnectSceneEca()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (run.rules) throw new InvalidOperationException("Scene ECA is already connected.");
            run.map = GameObject.Find("World").AddComponent<NativeMap>();
            run.map.exitGate = GameObject.Find("Gate - relay opens this collider");
            run.quest = new GameObject("Quest").AddComponent<NativeQuest>();
            run.combat = new GameObject("Combat").AddComponent<NativeCombat>();
            run.combat.projectilePrefab = Asset<GameObject>(Root + "/Prefabs/Pulse.prefab").GetComponent<NativeProjectile>();
            run.rules = new GameObject("ECA - scene rules").AddComponent<NativeEcaRules>();
            var canvas = GameObject.Find("UI"); run.hud = canvas.AddComponent<NativeHud>();
            var hud = run.hud;
            hud.healthLabel = canvas.transform.Find("Health").GetComponent<TMP_Text>();
            hud.fireLabel = canvas.transform.Find("Fire mode").GetComponent<TMP_Text>();
            hud.objectiveLabel = canvas.transform.Find("Objective").GetComponent<TMP_Text>();
            hud.promptLabel = canvas.transform.Find("Input and proximity prompt").GetComponent<TMP_Text>();
            hud.counterLabel = canvas.transform.Find("Run status").GetComponent<TMP_Text>();
            hud.phonePanel = canvas.transform.Find("Phone - Tab does not pause").gameObject;
            hud.phoneCloseButton = hud.phonePanel.transform.Find("Close phone").GetComponent<UnityEngine.UI.Button>();
            hud.resultPanel = canvas.transform.Find("Result overlay").gameObject;
            var card = hud.resultPanel.transform.Find("Result card");
            hud.resultTitle = card.Find("Result title").GetComponent<TMP_Text>();
            hud.resultBody = card.Find("Result body").GetComponent<TMP_Text>();
            hud.restartButton = card.Find("Restart").GetComponent<UnityEngine.UI.Button>();
            BuildDialogue(run, canvas.transform);
            WireModules(run, GameObject.Find("Relay terminal").GetComponent<NativeInteraction>(), GameObject.Find("Exit uplink").GetComponent<NativeInteraction>());
            EditorUtility.SetDirty(run);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("NativeDemo ECA connected in place: Interaction -> Quest -> Map -> Dialogue; exit -> Quest -> Level.");
        }
        [MenuItem("Echo/Native/Connect U3 Main Line")]
        public static void ConnectMainLine()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (run.narrative) throw new InvalidOperationException("U3 main line already connected.");
            material = Asset<Material>("Assets/CyberCity/Materials/Actors.mat");
            font = Asset<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            square = Asset<Sprite>(Root + "/SolidMarker.asset");
            var world = run.map.transform;
            run.narrative = new GameObject("Narrative - recovered memories and ending").AddComponent<NativeNarrative>();
            run.quest.narrative = run.narrative; run.rules.narrative = run.narrative;
            var memories = new[] {
                Memory("Private memory", NativeMemoryKind.Private, new Vector2(-10, -5), "A hand at the window",
                    "PRIVATE MEMORY / Unverified personal fragment\nRain on warm glass. Someone holds your hand and says: if they ask what you remember, tell them the light. You remember the hand instead.\nCOMMANDER: The record carries no name. Keep it anyway.", world, run),
                Memory("System record", NativeMemoryKind.System, new Vector2(5, 6), "A transfer without consent",
                    "SYSTEM RECORD / Institutional archive\nShell transfer accepted. Personal attachments marked as noise. The subject's objection was removed from the summary.\nCOMMANDER: The log says the transfer succeeded. It does not say who agreed.", world, run),
                Memory("Initial echo", NativeMemoryKind.InitialEcho, new Vector2(6, -6), "You may keep the contradiction",
                    "SYSTEM INITIAL ECHO / Offline seed\nYou do not need a consistent past to choose what you carry forward.\nThis is a system-provided starting message. It is not a live message or a record left by another player.", world, run)
            };
            run.rules.memoryNodes = memories;
            run.hud.interactables = new[] { memories[0].interaction, memories[1].interaction, memories[2].interaction, run.rules.terminal, run.rules.exit };
            run.rules.terminal.promptOverride = "E  /  RECONNECT WITH THREE MEMORIES";
            run.rules.exit.transform.position = new Vector2(33, 0);
            run.rules.exit.promptOverride = "E  /  KEEP THE THREE MEMORIES";
            world.Find("EXIT").position = new Vector2(33, 2);
            world.Find("EXIT").GetComponent<TMP_Text>().text = "FINAL ARCHIVE";
            world.Find("BYPASS").GetComponent<TMP_Text>().text = "SERVICE PATH";
            foreach (var name in new[] { "North perimeter", "South perimeter" })
            {
                var wall = world.Find(name); wall.position = new Vector3(10, wall.position.y, 0); wall.localScale = new Vector3(50.5f, .5f, 1);
            }
            world.Find("East perimeter").position = new Vector2(35, 0);
            var ground = Draw("Ground - encounter extension", Asset<Sprite>(Root + "/GroundCrop.asset"), new Vector2(25, 0), world, -1000);
            ground.transform.localScale = new Vector3(2f / 3f, 1, 1);
            foreach (float x in new[] { 14f, 31f })
            {
                Wall("Encounter partition north " + x, new Vector2(x, 5.5f), new Vector2(.55f, 7), world);
                Wall("Encounter partition south " + x, new Vector2(x, -5.5f), new Vector2(.55f, 7), world);
            }
            run.map.arenaEntryGate = Wall("Arena entry - closes during encounter", new Vector2(14, 0), new Vector2(.55f, 4), world);
            run.map.arenaEntryGate.SetActive(false);
            run.map.finalGate = Wall("Final gate - real Boss defeat required", new Vector2(31, 0), new Vector2(.55f, 4), world);
            run.map.finalGate.GetComponent<SpriteRenderer>().color = new Color(1, .32f, .2f);
            var anchor = new GameObject("BossSpawn - real component required"); anchor.transform.SetParent(world, false); anchor.transform.position = new Vector2(23, 0);
            WorldText("DEVELOPMENT ENCOUNTER", new Vector2(23, 7), world, Cyan);
            WorldText("GUARDED CHECKPOINT", new Vector2(-4, -7.7f), world, new Color(1, .55f, .3f));
            var enemies = UnityEngine.Object.FindObjectsOfType<NativeEnemy>();
            Array.Sort(enemies, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            var positions = new[] { new Vector2(-5, -4), new Vector2(-1, -6), new Vector2(4, -4), new Vector2(6, 2) };
            for (int i = 0; i < enemies.Length; i++) enemies[i].transform.position = positions[i % positions.Length];
            run.rules.regions = new[] {
                Region("Service path trigger", NativeRegion.Purpose.ServiceBypass, new Vector2(0, 6), new Vector2(3, 3), world),
                Region("Encounter entrance trigger", NativeRegion.Purpose.BossEntrance, new Vector2(17, 0), new Vector2(3, 5), world)
            };
            var camera = UnityEngine.Object.FindObjectOfType<NativeCamera>(); camera.worldCenter = new Vector2(10, 0); camera.worldHalfSize = new Vector2(25, 9);
            var dialogue = run.dialogue.panel.GetComponent<RectTransform>(); dialogue.sizeDelta = new Vector2(780, 250);
            run.dialogue.message.rectTransform.sizeDelta = new Vector2(732, 174); run.dialogue.message.fontSize = 20;
            var hud = run.hud;
            hud.phonePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 470);
            hud.phoneArchive = hud.phonePanel.transform.Find("Phone placeholder").GetComponent<TMP_Text>();
            hud.phoneArchive.rectTransform.sizeDelta = new Vector2(302, 316); hud.phoneArchive.fontSize = 16;
            hud.phoneArchive.text = "MEMORY ARCHIVE / 0 OF 3\nRecover the marked memory nodes.";
            var card = hud.resultTitle.transform.parent.GetComponent<RectTransform>(); card.sizeDelta = new Vector2(840, 570);
            hud.resultTitle.rectTransform.sizeDelta = new Vector2(780, 58); hud.resultTitle.fontSize = 29;
            hud.resultBody.rectTransform.sizeDelta = new Vector2(760, 370); hud.resultBody.fontSize = 21;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("U3 main line connected: three memories, physical service route, relay, real Boss interface pending, final archive and one ending.");
        }
        static NativeMemoryNode Memory(string name, NativeMemoryKind kind, Vector2 at, string title, string body, Transform parent, NativeRunController run)
        {
            var node = Interaction(name, at, false, parent, run); node.map = run.map; node.promptOverride = "E  /  RECOVER " + name.ToUpperInvariant();
            var memory = node.gameObject.AddComponent<NativeMemoryNode>(); memory.kind = kind; memory.interaction = node; memory.title = title; memory.body = body;
            WorldText(name.ToUpperInvariant(), at + new Vector2(0, 1.6f), parent, Cyan); return memory;
        }
        static NativeRegion Region(string name, NativeRegion.Purpose purpose, Vector2 at, Vector2 size, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at;
            var collider = go.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = size;
            var region = go.AddComponent<NativeRegion>(); region.purpose = purpose; return region;
        }
        static SpriteRenderer Draw(string name, Sprite sprite, Vector2 point, Transform parent, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = point;
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.sharedMaterial = material; view.sortingOrder = order;
            return view;
        }
        static GameObject Wall(string name, Vector2 point, Vector2 size, Transform parent)
        {
            var view = Draw(name, square, point, parent, -50); view.transform.localScale = new Vector3(size.x, size.y, 1);
            view.color = new Color(.12f, .22f, .26f);
            view.gameObject.AddComponent<BoxCollider2D>(); view.gameObject.AddComponent<NativeObstacle>();
            return view.gameObject;
        }
        static void Body(GameObject go, float radius)
        {
            var body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.GetComponent<CircleCollider2D>().radius = radius;
        }
        static void Prop(string name, Vector2 point, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = point;
            go.AddComponent<MeshFilter>().sharedMesh = Asset<Mesh>("Assets/CyberCity/Meshes/" + name + ".asset");
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = Asset<Material>("Assets/CyberCity/Materials/CityAtlas.mat");
            renderer.sortingOrder = 100 - Mathf.RoundToInt(point.y * 10);
        }
        static NativeInteraction Interaction(string name, Vector2 at, bool isExit, Transform parent, NativeRunController run)
        {
            var view = Draw(name, Asset<Sprite>("Assets/CyberCity/Sprites/Maintenance plate.asset"), at, parent, -100);
            view.color = Cyan; view.transform.localScale = Vector3.one * .8f;
            var node = view.gameObject.AddComponent<NativeInteraction>(); node.isExit = isExit; node.indicator = view; node.run = run; return node;
        }
        static void WorldText(string text, Vector2 at, Transform parent, Color color)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.position = at;
            var label = go.AddComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = 3;
            label.color = color; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(4, 1);
        }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 point, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = point; rect.sizeDelta = size; }
        static UnityEngine.UI.Image Panel(string name, Transform parent, Vector2 anchor, Vector2 point, Vector2 size, Color color, bool raycast = false)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            panel.transform.SetParent(parent, false); panel.color = color; panel.raycastTarget = raycast;
            Place(panel.rectTransform, anchor, anchor, point, size); return panel;
        }
        static TMP_Text Text(string name, string text, Transform parent, Vector2 anchor, Vector2 point, Vector2 size, int fontSize = 20, Color? color = null)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(parent, false);
            label.font = font; label.text = text; label.fontSize = fontSize; label.color = color ?? Color.white; label.raycastTarget = false;
            label.enableWordWrapping = true; Place(label.rectTransform, anchor, anchor, point, size); return label;
        }
        static UnityEngine.UI.Button Button(string name, string label, Transform parent, Vector2 point, Vector2 size)
        {
            var image = Panel(name, parent, new Vector2(.5f, 0), point, size, Cyan, true);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var text = Text("Label", label, image.transform, new Vector2(.5f, .5f), Vector2.zero, size, 21, Dark); text.alignment = TextAlignmentOptions.Center;
            return button;
        }
        static void BuildUI(NativeRunController run)
        {
            var canvasObject = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            run.hud = canvasObject.AddComponent<NativeHud>();
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
            eventSystem.sendNavigationEvents = false; // Space is gameplay only, never a hidden UI Submit.
            var top = Panel("Header", canvasObject.transform, new Vector2(.5f, 1), Vector2.zero, new Vector2(2600, 118), Dark);
            Text("Title", "ECHO IN THE SHELL  /  SIGNAL 01", canvasObject.transform, new Vector2(0, 1), new Vector2(24, -16), new Vector2(600, 28), 23, Cyan);
            run.hud.objectiveLabel = Text("Objective", run.quest.initialObjective, canvasObject.transform, new Vector2(0, 1), new Vector2(24, -53), new Vector2(730, 56), 19);
            run.hud.healthLabel = Text("Health", "SHELL 100 / 100", canvasObject.transform, new Vector2(1, 1), new Vector2(-24, -18), new Vector2(350, 30), 24);
            run.hud.healthLabel.alignment = TextAlignmentOptions.TopRight;
            run.hud.fireLabel = Text("Fire mode", "AUTO FIRE", canvasObject.transform, new Vector2(1, 1), new Vector2(-24, -57), new Vector2(380, 30), 20, Cyan);
            run.hud.fireLabel.alignment = TextAlignmentOptions.TopRight;
            Panel("Footer", canvasObject.transform, new Vector2(.5f, 0), Vector2.zero, new Vector2(2600, 74), Dark);
            run.hud.promptLabel = Text("Input and proximity prompt", "WASD  MOVE     SPACE  FIRE / HOLD     E  INTERACT     TAB  PHONE", canvasObject.transform, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(1160, 32), 21, Cyan);
            run.hud.promptLabel.alignment = TextAlignmentOptions.Center;
            run.hud.counterLabel = Text("Run status", "00:00", canvasObject.transform, new Vector2(.5f, 0), new Vector2(0, 7), new Vector2(1160, 25), 16, new Color(.65f, .75f, .79f));
            run.hud.counterLabel.alignment = TextAlignmentOptions.Center;
            var phone = Panel("Phone - Tab does not pause", canvasObject.transform, new Vector2(1, .5f), new Vector2(-24, -12), new Vector2(350, 440), Dark, true);
            run.hud.phonePanel = phone.gameObject;
            Text("Phone title", "GHOST LINK\nLOCAL CONNECTION", phone.transform, new Vector2(0, 1), new Vector2(24, -24), new Vector2(302, 75), 26, Cyan);
            Text("Phone placeholder", "COMMANDER\nReconnect the cyan terminal, then reach the eastern exit. Side paths remain open.\n\nNETWORK\nOffline. No live network service.\n\nSupport and memory features arrive in the next checkpoint.\n\nCombat continues while this panel is open.", phone.transform, new Vector2(0, 1), new Vector2(24, -110), new Vector2(302, 260), 18);
            run.hud.phoneCloseButton = Button("Close phone", "CLOSE  /  TAB", phone.transform, new Vector2(0, 20), new Vector2(302, 42));
            phone.gameObject.SetActive(false);
            var overlay = Panel("Result overlay", canvasObject.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(3000, 2000), new Color(.01f, .02f, .03f, .92f), true);
            run.hud.resultPanel = overlay.gameObject;
            var card = Panel("Result card", overlay.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(640, 400), Dark, true);
            run.hud.resultTitle = Text("Result title", "SHELL OFFLINE", card.transform, new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(590, 58), 30, Cyan); run.hud.resultTitle.alignment = TextAlignmentOptions.Center;
            run.hud.resultBody = Text("Result body", "", card.transform, new Vector2(.5f, 1), new Vector2(0, -120), new Vector2(550, 190), 21); run.hud.resultBody.alignment = TextAlignmentOptions.Center;
            run.hud.restartButton = Button("Restart", "RESTART RUN", card.transform, new Vector2(0, 32), new Vector2(300, 52));
            overlay.gameObject.SetActive(false);
            BuildDialogue(run, canvasObject.transform);
        }
    }
}
