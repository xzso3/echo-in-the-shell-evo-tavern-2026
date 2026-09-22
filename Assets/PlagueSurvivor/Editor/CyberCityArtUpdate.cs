using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PlagueSurvivor.Editor
{
    public static class CyberCityArtUpdate
    {
        const string Art="Assets/Art/Generated/Cyberpunk/";
        static Sprite EnemySprite(Texture2D texture,string name,Rect top,float ppu)
        {
            string path="Assets/CyberCity/Sprites/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(existing)return existing;
            var sprite=Sprite.Create(texture,new Rect(top.x,texture.height-top.y-top.height,top.width,top.height),new Vector2(.5f,.08f),ppu,0,SpriteMeshType.FullRect);
            sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        static void UpdatePrefab(SpriteRenderer reference,Sprite sprite)
        {
            string path=AssetDatabase.GetAssetPath(reference);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<SpriteRenderer>().sprite=sprite;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static string Apply()
        {
            var game=Object.FindObjectOfType<PlagueGame>();
            if(!game||!game.city||game.gameObject.scene.path!="Assets/Scenes/CyberCity.unity")
                throw new System.InvalidOperationException("Open CyberCity in Edit Mode first.");
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            string groundPath=Art+"cyber_ground_64x44_32px.png";
            AssetDatabase.ImportAsset(groundPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(groundPath);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.spritePivot=new Vector2(.5f,.5f);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var groundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(groundPath);
            if(!groundSprite||Vector2.Distance(groundSprite.bounds.size,new Vector2(64,44))>.001f)
                throw new System.InvalidOperationException("Ground import must be exactly 64 x 44.");
            var tagManager=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tagManager.FindProperty("m_SortingLayers");
            int groundIndex=-1;
            for(int i=0;i<layers.arraySize;i++)if(layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue=="Ground")groundIndex=i;
            if(groundIndex<0)
            {
                layers.InsertArrayElementAtIndex(0);
                var element=layers.GetArrayElementAtIndex(0);
                element.FindPropertyRelative("name").stringValue="Ground";
                element.FindPropertyRelative("uniqueID").intValue=184825231;
                element.FindPropertyRelative("locked").boolValue=false;
            }
            else if(groundIndex>0)layers.MoveArrayElement(groundIndex,0);
            tagManager.ApplyModifiedProperties();
            var root=GameObject.Find("CYBER CITY - Modular atlas construction").transform;
            var archive=root.Find("Legacy tiled ground - disabled");
            if(!archive){archive=new GameObject("Legacy tiled ground - disabled").transform;archive.SetParent(root,false);}
            int retired=0;
            foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if(sr.sortingOrder>-800||sr.gameObject.name=="Ground")continue;
                sr.transform.SetParent(archive,true);retired++;
            }
            archive.gameObject.SetActive(false);
            var ground=root.Find("Ground");
            if(!ground){ground=new GameObject("Ground").transform;ground.SetParent(root,false);}
            ground.localPosition=Vector3.zero;ground.localScale=Vector3.one;ground.localRotation=Quaternion.identity;
            var renderer=ground.GetComponent<SpriteRenderer>();if(!renderer)renderer=ground.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=groundSprite;renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberCity/Materials/Actors.mat");
            renderer.color=Color.white;renderer.sortingLayerName="Ground";renderer.sortingOrder=0;
            string enemyPath=Art+"cyber_security_enemies_sheet_v3.png";AssetDatabase.ImportAsset(enemyPath);
            var enemyImporter=(TextureImporter)AssetImporter.GetAtPath(enemyPath);
            enemyImporter.filterMode=FilterMode.Point;enemyImporter.mipmapEnabled=false;
            enemyImporter.textureCompression=TextureImporterCompression.Uncompressed;
            enemyImporter.maxTextureSize=2048;enemyImporter.npotScale=TextureImporterNPOTScale.None;enemyImporter.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(enemyPath);
            UpdatePrefab(game.meleePrefab,EnemySprite(texture,"Baton enforcer v3",new Rect(228,9,118,158),105));
            UpdatePrefab(game.rangedPrefab,EnemySprite(texture,"Security robot v3",new Rect(224,506,98,134),90));
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            return "Ground: Single, Center, PPU 32, 64x44, Ground sorting layer. Retired "+retired+" floor tiles; enemy prefabs updated to v3.";
        }
    }
}