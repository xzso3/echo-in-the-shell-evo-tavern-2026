using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.NativeGame.Editor
{
    // One-time authoring for P2-03. Keeps the existing scene and main-line objects intact.
    public static class NativeChunkBuilder
    {
        const string PrefabPath = "Assets/NativeGame/Prefabs/NorthRouteChunk.prefab";
        const string ScenePath = "Assets/Scenes/NativeDemo.unity";
        static readonly Vector2 FirstCenter = new Vector2(-10, 14);
        static readonly Vector2 SecondCenter = new Vector2(-10, 24);
        static Sprite square;
        static Material material;
        static TMP_FontAsset font;

        [MenuItem("Echo/Native/Connect P2 North Route Chunks")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the north route.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!run || !run.map || !run.rules || !run.hud || run.map.northRouteChunks.Length != 0 ||
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
                throw new InvalidOperationException("North route requires the P2 baseline and may only be authored once.");

            square = Require<Sprite>("Assets/NativeGame/SolidMarker.asset");
            material = Require<Material>("Assets/CyberCity/Materials/Actors.mat");
            font = Require<TMP_FontAsset>("Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset");
            var world = run.map.transform;
            var oldNorthWall = world.Find("North perimeter");
            if (!oldNorthWall || !Mathf.Approximately(oldNorthWall.position.y, 9))
                throw new InvalidOperationException("Expected original north wall at y=9.");

            var prefab = CreateChunkPrefab();
            var first = Instantiate(prefab, FirstCenter, world);
            var second = Instantiate(prefab, SecondCenter, world);
            var entry = new GameObject("North route entry port").transform;
            entry.SetParent(world, false); entry.position = first.southPort.position;

            // The original wall stays in the scene as the west piece; a matching east piece
            // completes the old perimeter, leaving only the 3.5-unit south port open.
            oldNorthWall.position = new Vector3(-13.5f, 9, 0);
            oldNorthWall.localScale = new Vector3(3.5f, .5f, 1);
            Wall("North perimeter east of route", new Vector2(13.5f, 9), new Vector2(43.5f, .5f), world,
                new Color(.12f, .22f, .26f));
            var door = Wall("North route door - Map opens", new Vector2(-10, 19), new Vector2(3.5f, .45f), world,
                new Color(1, .42f, .22f));

            var switchView = Draw("North route switch", Require<Sprite>("Assets/CyberCity/Sprites/Maintenance plate.asset"),
                new Vector2(-10, 16.5f), world, -100, new Color(.16f, .94f, .83f));
            switchView.transform.localScale = Vector3.one * .8f;
            var routeSwitch = switchView.gameObject.AddComponent<NativeInteraction>();
            routeSwitch.run = run; routeSwitch.map = run.map; routeSwitch.indicator = switchView;
            routeSwitch.promptOverride = "E / 解除北侧区块门锁";

            Label("北侧区块 1", new Vector2(-10, 12.5f), world);
            Label("北侧区块 2", new Vector2(-10, 23), world);
            run.map.northRouteEntry = entry;
            run.map.northRouteChunks = new[] { first, second };
            run.map.northRouteDoor = door;
            run.rules.northRouteSwitch = routeSwitch;
            run.hud.interactables = run.hud.interactables.Concat(new[] { routeSwitch }).ToArray();
            var camera = UnityEngine.Object.FindObjectOfType<NativeCamera>();
            camera.worldCenter = new Vector2(10, 10);
            camera.worldHalfSize = new Vector2(25, 20);

            EditorUtility.SetDirty(run.map); EditorUtility.SetDirty(run.rules);
            EditorUtility.SetDirty(run.hud); EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("NATIVE_CHUNK_BUILD: two NorthRouteChunk prefab instances at (-10,14)/(-10,24); " +
                "entry (-10,9), joined ports/Map door (-10,19), E switch (-10,16.5); east main line retained.");
        }

        static GameObject CreateChunkPrefab()
        {
            var root = new GameObject("North route chunk");
            var chunk = root.AddComponent<NativeChunk>();
            var ground = Draw("Ground - reused city artwork", Require<Sprite>("Assets/NativeGame/GroundCrop.asset"),
                Vector2.zero, root.transform, -1000, Color.white);
            ground.transform.localScale = new Vector3(1f / 3f, 5f / 9f, 1);
            var wallColor = new Color(.12f, .22f, .26f);
            Wall("West wall", new Vector2(-5, 0), new Vector2(.45f, 10.45f), root.transform, wallColor);
            Wall("East wall", new Vector2(5, 0), new Vector2(.45f, 10.45f), root.transform, wallColor);
            foreach (float y in new[] { -5f, 5f })
            {
                Wall(y < 0 ? "South west wall" : "North west wall", new Vector2(-3.375f, y),
                    new Vector2(3.25f, .45f), root.transform, wallColor);
                Wall(y < 0 ? "South east wall" : "North east wall", new Vector2(3.375f, y),
                    new Vector2(3.25f, .45f), root.transform, wallColor);
            }
            chunk.southPort = Port("South port", new Vector2(0, -5), root.transform);
            chunk.northPort = Port("North port", new Vector2(0, 5), root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (!prefab) throw new InvalidOperationException("Could not save reusable north-route chunk prefab.");
            return prefab;
        }
        static NativeChunk Instantiate(GameObject prefab, Vector2 at, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = at;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance.GetComponent<NativeChunk>();
        }
        static Transform Port(string name, Vector2 local, Transform parent)
        {
            var port = new GameObject(name).transform;
            port.SetParent(parent, false); port.localPosition = local;
            return port;
        }
        static GameObject Wall(string name, Vector2 at, Vector2 size, Transform parent, Color color)
        {
            var view = Draw(name, square, at, parent, -50, color);
            view.transform.localScale = new Vector3(size.x, size.y, 1);
            view.gameObject.AddComponent<BoxCollider2D>();
            view.gameObject.AddComponent<NativeObstacle>();
            return view.gameObject;
        }
        static SpriteRenderer Draw(string name, Sprite sprite, Vector2 at, Transform parent, int order, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at;
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite;
            view.sharedMaterial = material; view.sortingOrder = order; view.color = color;
            return view;
        }
        static void Label(string text, Vector2 at, Transform parent)
        {
            var go = new GameObject(text).AddComponent<TextMeshPro>();
            go.transform.SetParent(parent, false); go.transform.position = at;
            go.font = font; go.fontSharedMaterial = font.material; go.fontSize = 3;
            go.text = text; go.color = new Color(.16f, .94f, .83f);
            go.alignment = TextAlignmentOptions.Center; go.rectTransform.sizeDelta = new Vector2(8, 1);
            go.GetComponent<MeshRenderer>().sortingOrder = 200;
        }
        static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) throw new InvalidOperationException("Missing NativeDemo asset: " + path);
            return asset;
        }
    }
}
