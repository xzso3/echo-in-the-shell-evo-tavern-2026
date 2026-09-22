using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace PlagueSurvivor.Editor
{
    public static class PlagueSceneBuilder
    {
        const string Root = "Assets/PlagueSurvivor";
        static Material material;
        static TMP_FontAsset font;
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            Folder(path.Substring(0, i));
            AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
        }
        static Texture2D Texture(string name)
        {
            string path = "Assets/Art/Generated/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        // Actor outlines are applied by PlagueSpriteOutlines at runtime.
        static Sprite Crop(Texture2D texture, string name, Rect topRect, float[] rows = null)
        {
            var sprite = Sprite.Create(texture, new Rect(topRect.x, texture.height-topRect.y-topRect.height, topRect.width, topRect.height),
                new Vector2(.5f, rows == null ? .5f : .08f), 128, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, Root + "/Sprites/" + name + ".asset");
            return sprite;
        }
        static SpriteRenderer View(string name, Sprite sprite, Vector2 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sharedMaterial = material;
            return sr;
        }
        static SpriteRenderer Prefab(string name, Sprite sprite, Vector2 scale)
        {
            var sr = View(name, sprite, Vector2.zero);
            sr.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var prefab = PrefabUtility.SaveAsPrefabAsset(sr.gameObject, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(sr.gameObject);
            return prefab.GetComponent<SpriteRenderer>();
        }
        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        static TMP_Text Label(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAlignmentOptions align)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = fontSize;
            label.alignment = align; label.color = new Color(.89f,.84f,.70f);
            label.raycastTarget = false;
            return label;
        }
        static UnityEngine.UI.Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var image = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        public static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/PlagueCourtyard.unity") != null)
                throw new InvalidOperationException("Scene already exists. Open it instead of rebuilding.");
            Folder(Root + "/Sprites"); Folder(Root + "/Materials"); Folder(Root + "/Prefabs");
            var previous = EditorSceneManager.GetActiveScene();
            if (previous.isDirty && !string.IsNullOrEmpty(previous.path)) EditorSceneManager.SaveScene(previous);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, Root + "/Materials/PixelSprites.mat");
            var survivor = Texture("medieval_survivor_crossbow_sheet");
            var zombies = Texture("medieval_zombies_sheet");
            var town = Texture("plague_town_environment_atlas");
            var hero = Crop(survivor, "Survivor", new Rect(299,40,130,187),
                new float[]{0,65,71, 12,53,85, 42,35,97, 68,17,108, 90,7,123, 111,12,124, 130,27,113, 158,36,114, 181,79,112, 186,80,106});
            var walker = Crop(zombies, "Walker", new Rect(201,19,143,161),
                new float[]{0,57,77, 13,45,93, 35,25,112, 61,9,125, 93,1,137, 123,35,137, 135,39,107, 153,40,70, 160,43,64});
            var spitter = Crop(zombies, "Spitter", new Rect(947,11,172,169),
                new float[]{0,80,101, 14,62,112, 39,34,132, 65,19,151, 100,10,164, 120,27,150, 143,47,131, 162,48,121, 168,54,112});
            var ground = Crop(town, "Cobblestone", new Rect(18,18,112,112));
            var dirt = Crop(town, "Dirt", new Rect(151,18,112,112));
            var stone = Crop(town, "StoneFloor", new Rect(417,18,112,112));
            var wall = Crop(town, "Wall", new Rect(57,756,116,80));
            var arrow = Crop(survivor, "CrossbowBolt", new Rect(723,916,107,26));
            var acid = Crop(zombies, "AcidGlob", new Rect(904,973,39,31));
            var map = new GameObject("Courtyard - 28 x 16").transform;
            for (int y=0;y<8;y++) for(int x=0;x<14;x++)
            {
                bool path = x == 6 || x == 7 || y == 3 || y == 4;
                var tile = View("Ground_"+x+"_"+y, path ? stone : ((x+y)%7==0 ? dirt : ground), new Vector2(-13+x*2,-7+y*2), map);
                tile.transform.localScale = Vector3.one * (256f/112f);
                tile.color = path ? new Color(.75f,.76f,.7f) : new Color(.66f,.69f,.60f);
                tile.sortingOrder = -1000;
            }
            var border = new GameObject("Impassable boundary").transform;
            for(int x=0;x<15;x++)
            {
                foreach(float y in new float[]{-8.35f,8.35f})
                {
                    var v=View("Wall",wall,new Vector2(-14+x*2,y),border);
                    v.transform.localScale = new Vector3(256f/116f,1.5f,1);
                    v.sortingOrder = y < 0 ? 400 : -200;
                }
            }
            for(int y=0;y<8;y++) foreach(float x in new float[]{-14.5f,14.5f})
            {
                var v=View("Side wall",wall,new Vector2(x,-7+y*2),border);
                v.transform.localScale=new Vector3(1,3.2f,1); v.sortingOrder=400;
            }
            var cameraGo = new GameObject("Main Camera", typeof(Camera),typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.transform.position = new Vector3(0,0,-10);
            camera.orthographic = true; camera.orthographicSize = 10;
            camera.backgroundColor = new Color(.025f,.033f,.032f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false; camera.allowMSAA = false;
            var pixel = cameraGo.AddComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>();
            pixel.assetsPPU=16; pixel.refResolutionX=512; pixel.refResolutionY=384;
            pixel.cropFrame = UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera.CropFrame.StretchFill;
            var light = new GameObject("Main Directional Light").AddComponent<Light>();
            light.type=LightType.Directional; light.intensity=1;
            var game = new GameObject("Game - Tune parameters here").AddComponent<PlagueGame>();
            game.player = View("Survivor", hero, Vector2.zero);
            game.meleePrefab = Prefab("PlagueWalker",walker,Vector2.one);
            game.rangedPrefab = Prefab("AcidSpitter",spitter,Vector2.one);
            game.boltPrefab = Prefab("CrossbowBolt",arrow,new Vector2(.7f,.7f));
            game.acidPrefab = Prefab("AcidGlob",acid,Vector2.one);
            game.gameObject.AddComponent<PlagueSpriteOutlines>();
            return Finish(game);
        }
        public static string Finish(PlagueGame game)
        {
            var scene = EditorSceneManager.GetActiveScene();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!font) throw new InvalidOperationException("Import TMP essentials before finishing the scene.");
            var canvas = new GameObject("HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,800); scaler.matchWidthOrHeight=.5f;
            Panel("Header",canvas.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(1280,87),new Color(.035f,.05f,.045f,.96f));
            Label("Title",canvas.transform,"PLAGUE  /  THE LAST BOLTS",new Vector2(0,1),new Vector2(28,-12),new Vector2(800,25),22,TextAlignmentOptions.Left);
            game.status=Label("Vitals",canvas.transform,"",new Vector2(0,1),new Vector2(28,-43),new Vector2(900,24),17,TextAlignmentOptions.Left);
            game.timer=Label("Time",canvas.transform,"00:00",new Vector2(1,1),new Vector2(-30,-14),new Vector2(130,48),36,TextAlignmentOptions.Right);
            Panel("Health track",canvas.transform,new Vector2(0,1),new Vector2(28,-73),new Vector2(300,5),new Color(.18f,.22f,.18f));
            game.healthFill=Panel("Health",canvas.transform,new Vector2(0,1),new Vector2(28,-73),new Vector2(300,5),new Color(.7f,.28f,.2f));
            // Filled UI requires a sprite; use a generated white UI sprite, independent of art.
            var white = new Texture2D(2,2); white.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white}); white.Apply();
            AssetDatabase.CreateAsset(white,Root+"/Sprites/UIWhite.asset");
            var whiteSprite=Sprite.Create(white,new Rect(0,0,2,2),new Vector2(.5f,.5f));
            AssetDatabase.AddObjectToAsset(whiteSprite,white);
            game.healthFill.sprite=whiteSprite;
            game.healthFill.type=UnityEngine.UI.Image.Type.Filled; game.healthFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            Panel("Footer",canvas.transform,new Vector2(.5f,0),Vector2.zero,new Vector2(1280,42),new Color(.035f,.05f,.045f,.96f));
            game.message=Label("Controls",canvas.transform,"",new Vector2(.5f,0),new Vector2(0,8),new Vector2(1200,26),18,TextAlignmentOptions.Center);
            var overlay=Panel("Pause and defeat",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1280,800),new Color(.015f,.025f,.02f,.88f));
            game.overlay=overlay.gameObject;
            game.overlayText=Label("Message",overlay.transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1000,220),46,TextAlignmentOptions.Center);
            game.overlay.SetActive(false);
            new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/PlagueCourtyard.unity");
            var builds=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene("Assets/Scenes/PlagueCourtyard.unity",true)};
            foreach(var s in EditorBuildSettings.scenes) if(s.path!="Assets/Scenes/PlagueCourtyard.unity") builds.Add(s);
            EditorBuildSettings.scenes=builds.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=game.gameObject;
            return "Created PlagueCourtyard with original art, 112 floor tiles, 4 reusable prefabs, HUD and combat.";
        }
    }
}
