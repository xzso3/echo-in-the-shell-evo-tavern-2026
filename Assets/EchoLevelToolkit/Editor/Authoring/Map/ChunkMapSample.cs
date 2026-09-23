using System;
using System.IO;
using Echo.LevelToolkit.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map.Editor
{
    public static class ChunkMapSample
    {
        private const string SampleRoot = ChunkAuthoring.ContentRoot + "/Sample";

        // Run only in an isolated Editor slot. Creates a separate scene and never saves an open user scene.
        [MenuItem("Echo/Level Toolkit/Create Map Sample")]
        public static void Create()
        {
            ChunkAuthoring.EnsureFolder(SampleRoot);
            var floor = GetTile("SampleGround", new Color32(78, 92, 113, 255), Tile.ColliderType.None);
            var wall = GetTile("SampleWall", new Color32(39, 28, 70, 255), Tile.ColliderType.Grid);
            var walkerSprite = GetSprite("SampleWalker", new Color32(57, 223, 177, 255));
            ChunkAuthoring.CreateSharedPalette("MapSamplePalette", floor, wall);

            var chunks = new[]
            {
                GetChunk("Sample03", 3, -1, 1, floor, wall),
                GetChunk("Sample05", 5, 2, 2, floor, wall),
                GetChunk("Sample06", 6, 2, 2, floor, wall),
                GetChunk("Sample32", 32, 15, -1, floor, wall)
            };

            string scenePath = SampleRoot + "/ChunkMapSample.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                Debug.Log("Map sample already exists at " + scenePath + "; existing scene was preserved.");
                return;
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene sample = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(sample);
                var map = new GameObject("MixedSizeChunkMap").AddComponent<ChunkMap>();
                ChunkAuthoring.Place(map, chunks[0], new Vector2Int(0, 0));
                ChunkAuthoring.Place(map, chunks[1], new Vector2Int(3, -1));
                ChunkAuthoring.Place(map, chunks[2], new Vector2Int(8, -1));
                ChunkAuthoring.Place(map, chunks[3], new Vector2Int(14, -14));

                var walker = new GameObject("WASD physical walk probe");
                walker.transform.position = new Vector3(1.5f, 1.5f, 0f);
                walker.AddComponent<SpriteRenderer>().sprite = walkerSprite;
                walker.AddComponent<CircleCollider2D>().radius = 0.32f;
                var body = walker.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.freezeRotation = true;
                walker.AddComponent<ChunkMapPreviewWalker>();

                var cameraObject = new GameObject("Sample Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(23f, 2f, -10f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 21f;
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.13f);
                EditorSceneManager.SaveScene(sample, scenePath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(sample, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Created isolated map sample: " + scenePath + ". Open it and press Play for WASD probe.");
        }

        private static ChunkDefinition GetChunk(string localId, int size,
            int westOffset, int eastOffset, TileBase floor, TileBase wall)
        {
            string path = ChunkAuthoring.ChunkPath("echo", "map-sample", localId);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<ChunkDefinition>();
            if (existing != null) return existing;
            var west = westOffset >= 0 ? new EdgePort(true, westOffset, 1) : new EdgePort(false, 0, 0);
            var east = eastOffset >= 0 ? new EdgePort(true, eastOffset, 1) : new EdgePort(false, 0, 0);
            var created = ChunkAuthoring.CreateChunk("echo", "map-sample", localId, size,
                "sample", new EdgePort(false, 0, 0), east, new EdgePort(false, 0, 0), west);
            Paint(created, floor, wall);
            ChunkAiMerge.RecordGeneratedPrefabBaseline(path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ChunkDefinition>();
        }

        private static void Paint(ChunkDefinition prefab, TileBase floor, TileBase wall)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var definition = root.GetComponent<ChunkDefinition>();
                var layers = root.GetComponent<ChunkTileLayers>();
                int n = definition.Size;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    layers.Ground.SetTile(cell, floor);
                    bool boundary = x == 0 || y == 0 || x == n - 1 || y == n - 1;
                    bool opening = IsOpening(definition, x, y);
                    layers.Obstacles.SetTile(cell, boundary && !opening ? wall : null);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static bool IsOpening(ChunkDefinition definition, int x, int y)
        {
            int n = definition.Size;
            return (y == n - 1 && Covers(definition.Port(ChunkSide.North), x))
                || (x == n - 1 && Covers(definition.Port(ChunkSide.East), y))
                || (y == 0 && Covers(definition.Port(ChunkSide.South), x))
                || (x == 0 && Covers(definition.Port(ChunkSide.West), y));
        }

        private static bool Covers(EdgePort port, int offset) => port.Enabled
            && offset >= port.Offset && offset < port.EndExclusive;

        private static Tile GetTile(string name, Color32 color, Tile.ColliderType collider)
        {
            string path = SampleRoot + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null) return existing;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = GetSprite(name, color);
            tile.colliderType = collider;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Sprite GetSprite(string name, Color32 color)
        {
            string path = SampleRoot + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                try
                {
                    var pixels = new Color32[32 * 32];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
