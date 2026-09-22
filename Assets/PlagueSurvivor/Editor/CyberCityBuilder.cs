using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace PlagueSurvivor.Editor
{
    public static class CyberCityBuilder
    {
        const string Root="Assets/CyberCity";
        const string Art="Assets/Art/Generated/Cyberpunk/";
        static Material material;
        static Texture2D atlas;
        static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        static readonly List<Rect> blocks=new List<Rect>();
        static int tileCount,propCount;
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int index=path.LastIndexOf('/');Folder(path.Substring(0,index));
            AssetDatabase.CreateFolder(path.Substring(0,index),path.Substring(index+1));
        }
        static Texture2D Texture(string file)
        {
            string path=Art+file+".png";
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Sprite Sprite(Texture2D texture,string name,Rect top,float ppu=64,bool feet=false)
        {
            var result=UnityEngine.Sprite.Create(texture,new Rect(top.x,texture.height-top.y-top.height,top.width,top.height),new Vector2(.5f,feet?.08f:.5f),ppu,0,SpriteMeshType.FullRect);
            result.name=name;AssetDatabase.CreateAsset(result,Root+"/Sprites/"+name+".asset");return result;
        }
        static SpriteRenderer Draw(string name,Sprite sprite,Vector2 at,Transform parent,int order=-1000)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=at;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=order;return sr;
        }
        static SpriteRenderer Prefab(string name,Sprite sprite,float scale=1)
        {
            var sr=Draw(name,sprite,Vector2.zero,null);
            sr.transform.localScale=Vector3.one*scale;
            var result=PrefabUtility.SaveAsPrefabAsset(sr.gameObject,Root+"/Prefabs/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(sr.gameObject);return result.GetComponent<SpriteRenderer>();
        }
        static void PropType(string name,Rect rect,float[] profile=null)
        {
            if(profile==null)profile=new float[]{0,.2f,.85f,.13f,.03f,.98f,.86f,.03f,.98f,1,.16f,.9f};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<profile.Length;i+=3)
            {
                for(int side=1;side<=2;side++)
                {
                    vertices.Add(new Vector3((profile[i+side]-.5f)*rect.width/64,(1-profile[i]-.06f)*rect.height/64,0));
                    uv.Add(new Vector2((rect.x+profile[i+side]*rect.width)/atlas.width,(atlas.height-rect.y-profile[i]*rect.height)/atlas.height));
                }
                if(i>0){int n=vertices.Count-4;triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            var colors=new Color[vertices.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+".asset");meshes[name]=mesh;
        }
        static void Prop(string name,Vector2 point,Transform parent,float scale=1,bool block=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=point;go.transform.localScale=Vector3.one*scale;
            go.AddComponent<MeshFilter>().sharedMesh=meshes[name];
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=100-Mathf.RoundToInt(point.y*10);
            var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",atlas);properties.SetColor("_Color",Color.white);properties.SetColor("_RendererColor",Color.white);
            renderer.SetPropertyBlock(properties);
            // Property blocks are transient; a material asset keeps the atlas bound after reopening.
            propCount++;
            if(block)blocks.Add(new Rect(point.x-.65f*scale,point.y-.2f,1.3f*scale,.7f*scale));
        }
        static void Tile(Sprite sprite,Vector2 point,Transform parent,int order=-1000)
        {
            var sr=Draw(sprite.name,sprite,point,parent,order);
            sr.transform.localScale=new Vector3(128/sprite.rect.width,128/sprite.rect.height,1);tileCount++;
        }
        static void Label(string title,Vector2 position,Transform parent,Color color)
        {
            var go=new GameObject(title);go.transform.SetParent(parent);go.transform.position=position;
            var text=go.AddComponent<TextMeshPro>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            text.text=title;text.fontSize=5;text.alignment=TextAlignmentOptions.Center;text.color=color;
            text.rectTransform.sizeDelta=new Vector2(14,1);text.GetComponent<MeshRenderer>().sortingOrder=-500;
        }
        public static string Build()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/CyberCity.unity"))throw new InvalidOperationException("CyberCity already exists.");
            Folder(Root+"/Sprites");Folder(Root+"/Meshes");Folder(Root+"/Materials");Folder(Root+"/Prefabs");
            meshes.Clear();blocks.Clear();tileCount=0;propCount=0;
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/PlagueCourtyard.unity")throw new InvalidOperationException("Open PlagueCourtyard first.");
            if(scene.isDirty)EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/CyberCity.unity");
            foreach(var root in scene.GetRootGameObjects())
                if(root.name=="Courtyard - 28 x 16"||root.name=="Impassable boundary")UnityEngine.Object.DestroyImmediate(root);
            atlas=Texture("cyber_city_tileset_atlas");
            material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            material.SetTexture("_MainTex",atlas);AssetDatabase.CreateAsset(material,Root+"/Materials/CityAtlas.mat");
            var actorMaterial=new Material(material);actorMaterial.SetTexture("_MainTex",null);AssetDatabase.CreateAsset(actorMaterial,Root+"/Materials/Actors.mat");
            var floor=Sprite(atlas,"Wet asphalt",new Rect(144,24,100,104));
            var puddle=Sprite(atlas,"Rain puddle",new Rect(506,268,100,103));
            var yellow=Sprite(atlas,"Yellow lane",new Rect(266,24,98,104));
            var white=Sprite(atlas,"White lane",new Rect(24,146,100,101));
            var concrete=Sprite(atlas,"Concrete paving",new Rect(627,25,99,103));
            var curb=Sprite(atlas,"Curb",new Rect(868,149,61,100));
            var metal=Sprite(atlas,"Maintenance plate",new Rect(144,524,101,103));
            var grate=Sprite(atlas,"Drain grate",new Rect(266,524,100,103));
            var hazard=Sprite(atlas,"Hazard floor",new Rect(266,268,100,103));
            var drain=Sprite(atlas,"Storm drain",new Rect(628,275,96,94));
            PropType("Barrier",new Rect(954,19,137,96));
            PropType("Hazard barrier",new Rect(1099,20,135,100));
            PropType("Broken barrier",new Rect(1244,20,137,101));
            PropType("Cone",new Rect(963,132,55,68),new float[]{0,.45f,.55f,.15f,.38f,.63f,.76f,.19f,.81f,.9f,.01f,.99f,1,.28f,.72f});
            PropType("Bollard",new Rect(1143,135,44,84),new float[]{0,.4f,.68f,.1f,.3f,.77f,.85f,.29f,.77f,1,.05f,.98f});
            PropType("Rail",new Rect(1300,135,142,83));
            PropType("Vending machine",new Rect(950,225,149,162));
            PropType("Air conditioner",new Rect(1110,267,113,119));
            PropType("Vent",new Rect(1237,271,56,98));
            PropType("Cable cabinet",new Rect(1309,244,132,188));
            PropType("Surveillance camera",new Rect(1449,245,70,179),new float[]{0,.5f,.9f,.13f,0,1,.27f,.32f,.8f,.47f,.45f,.9f,1,.5f,.8f});
            PropType("Refuse bags",new Rect(950,413,92,81));
            PropType("Trash bin",new Rect(1049,412,67,85));
            PropType("Dumpster",new Rect(1115,398,117,113));
            PropType("Planter",new Rect(1308,432,130,104));
            PropType("Small planter",new Rect(1446,427,70,109));
            PropType("Fan",new Rect(761,523,96,94));
            PropType("Turbine",new Rect(1090,525,109,102));
            PropType("Data beacon",new Rect(1227,560,66,137),new float[]{0,.45f,.59f,.05f,.15f,.85f,.43f,.15f,.85f,.6f,.26f,.74f,.92f,.25f,.75f,1,0,1});
            PropType("Power cabinet",new Rect(746,617,140,124));
            PropType("Transformer",new Rect(1017,638,159,104));
            PropType("Wall",new Rect(16,761,136,187));
            PropType("Service door",new Rect(350,776,104,180));
            PropType("Security door",new Rect(256,775,86,177));
            PropType("Pipe wall",new Rect(459,776,112,173));
            PropType("Fence",new Rect(751,778,129,183));
            PropType("Fence gate",new Rect(872,780,144,177));
            PropType("Guard booth",new Rect(1164,746,186,236));
            var cityRoot=new GameObject("CYBER CITY - Modular atlas construction").transform;
            var ground=new GameObject("01 - Roads and paving").transform;ground.SetParent(cityRoot);
            var furniture=new GameObject("02 - Street furniture").transform;furniture.SetParent(cityRoot);
            var perimeter=new GameObject("03 - Perimeter and checkpoints").transform;perimeter.SetParent(cityRoot);
            var random=new System.Random(617);
            for(int y=0;y<22;y++)for(int x=0;x<32;x++)
            {
                Vector2 point=new Vector2(-31+x*2,-21+y*2);
                bool road=Mathf.Abs(point.x)<6||Mathf.Abs(point.y)<4||Mathf.Abs(point.x)>27||Mathf.Abs(point.y)>18;
                Sprite tile=road ? (random.Next(7)==0?puddle:floor) : concrete;
                if(road&&Mathf.Abs(point.x)==1&&Mathf.Abs(point.y)>5)tile=yellow;
                if(road&&Mathf.Abs(point.y)==1&&Mathf.Abs(point.x)>7)tile=white;
                if(!road&&random.Next(20)==0)tile=drain;
                Tile(tile,point,ground);
            }
            string[] districts={"01 / POWER EXCHANGE","02 / SECURITY NODE","03 / SERVICE MARKET","04 / COOLING YARD"};
            int district=0;
            foreach(float cy in new[]{11f,-11f})foreach(float cx in new[]{-17f,17f})
            {
                var zone=new GameObject(districts[district]).transform;zone.SetParent(cityRoot);
                for(int iy=0;iy<3;iy++)for(int ix=0;ix<4;ix++)Tile((ix+iy)%3==0?grate:metal,new Vector2(cx-3+ix*2,cy-2+iy*2),zone,-900);
                blocks.Add(new Rect(cx-4,cy-3,8,6));
                for(int i=0;i<4;i++)
                {
                    Prop(i==1?"Service door":i==2?"Pipe wall":"Wall",new Vector2(cx-3+i*2,cy-3),zone,.92f);
                    Prop(i%2==0?"Power cabinet":"Air conditioner",new Vector2(cx-3+i*2,cy+1.8f),zone,.8f);
                }
                Prop("Transformer",new Vector2(cx-1.5f,cy),zone,.9f);
                Prop("Turbine",new Vector2(cx+1.7f,cy),zone,.9f);
                for(int side=-1;side<=1;side+=2)
                {
                    for(int i=0;i<4;i++)Prop(i%2==0?"Fence":"Rail",new Vector2(cx+side*8,cy-4+i*2.4f),zone,.75f);
                    for(int i=0;i<3;i++)Tile(hazard,new Vector2(cx-2+i*2,cy+5),zone,-800);
                    Prop("Data beacon",new Vector2(cx+side*5.5f,cy-4.4f),zone,.8f,true);
                    Prop("Planter",new Vector2(cx+side*6,cy+5),zone,.9f,true);
                    Prop("Surveillance camera",new Vector2(cx+side*7.5f,cy+5),zone,1);
                }
                for(int i=0;i<3;i++)
                {
                    Prop(district%2==0?"Vending machine":"Cable cabinet",new Vector2(cx-3+i*2.5f,cy-6.5f),zone,.85f,true);
                    Prop(i%2==0?"Refuse bags":"Trash bin",new Vector2(cx-5.6f,cy-5.5f+i*2),zone,.8f);
                    Prop("Fan",new Vector2(cx+5.5f,cy-1.5f+i*1.8f),zone,.8f);
                }
                Prop("Dumpster",new Vector2(cx+5,cy-6.5f),zone,.9f,true);
                Label(districts[district],new Vector2(cx,cy+7),zone,new Color(.4f,.72f,.76f,.8f));
                district++;
            }
            // Crosswalks and central intersection: preserve a clear spawn pocket.
            foreach(float x in new[]{-7f,7f})foreach(float y in new[]{-5f,5f})
            {
                Prop("Planter",new Vector2(x,y),furniture,.9f,true);
                Prop("Bollard",new Vector2(x-Mathf.Sign(x)*1.3f,y),furniture,.85f);
                Prop("Cone",new Vector2(x,y-Mathf.Sign(y)*1.2f),furniture,.65f);
            }
            for(int i=0;i<5;i++)foreach(float y in new[]{-5f,5f})Tile(white,new Vector2(-4+i*2,y),ground,-850);
            for(int x=-30;x<=30;x+=2)
            {
                Prop(x%6==0?"Pipe wall":"Wall",new Vector2(x,21.1f),perimeter,.95f);
                Prop(x%8==0?"Fence gate":"Fence",new Vector2(x,-21.4f),perimeter,.92f);
            }
            for(int y=-19;y<=19;y+=2)
            {
                Prop("Fence",new Vector2(-31,y),perimeter,.85f);
                Prop("Pipe wall",new Vector2(31,y),perimeter,.85f);
            }
            foreach(float x in new[]{-27f,27f})
            {
                Prop("Guard booth",new Vector2(x,3.8f),perimeter,1,true);
                Prop("Security door",new Vector2(x,-4.5f),perimeter,1);
                for(int i=0;i<3;i++)
                {
                    Prop(i==1?"Broken barrier":"Hazard barrier",new Vector2(x-2+i*2,-5.5f),perimeter,.8f,true);
                    Prop("Cone",new Vector2(x-2+i*2,-6.6f),perimeter,.7f);
                }
                Prop("Data beacon",new Vector2(x,7.5f),perimeter,1,true);
            }
            // Distributed drain and utility runs enrich the outer circulation lanes.
            for(int i=0;i<12;i++)foreach(float y in new[]{-18f,18f})
            {
                float x=-26+i*4.7f;
                Prop(i%3==0?"Cable cabinet":i%3==1?"Air conditioner":"Dumpster",new Vector2(x,y),furniture,.8f,true);
                Prop("Bollard",new Vector2(x+1.4f,y),furniture,.8f);
            }
            var game=UnityEngine.Object.FindObjectOfType<PlagueGame>();
            var playerTexture=Texture("cyber_operative_player_sheet");
            var enemies=Texture("cyber_security_enemies_sheet_v3");
            var effects=Texture("cyber_projectiles_effects_atlas");
            var hero=Sprite(playerTexture,"Cyber operative",new Rect(132,22,119,210),140,true);
            var melee=Sprite(enemies,"Augmented enforcer",new Rect(228,9,118,158),105,true);
            var robot=Sprite(enemies,"Security robot",new Rect(224,506,98,134),90,true);
            var bolt=Sprite(effects,"Smart round",new Rect(819,95,68,28),110);
            var pulse=Sprite(effects,"Pulse round",new Rect(823,402,39,40),105);
            game.player.sprite=hero;game.player.sharedMaterial=actorMaterial;
            game.player.name="Cyber Operative";game.player.transform.position=Vector3.zero;
            game.meleePrefab=Prefab("AugmentedEnforcer",melee);game.rangedPrefab=Prefab("SecurityRobot",robot);
            game.boltPrefab=Prefab("SmartRound",bolt);game.acidPrefab=Prefab("PulseRound",pulse);
            foreach(var prefab in new[]{game.meleePrefab,game.rangedPrefab,game.boltPrefab,game.acidPrefab})
            {prefab.sharedMaterial=actorMaterial;EditorUtility.SetDirty(prefab);}
            game.GetComponent<PlagueSpriteOutlines>().cyberArt=true;
            game.arenaHalfSize=new Vector2(32,22);game.spawnSafeDistance=8;game.spawnInterval=.8f;game.enemyLimit=85;
            game.gameObject.name="Cyber Game - Tune parameters here";
            var world=game.gameObject.AddComponent<CyberCityWorld>();world.halfSize=game.arenaHalfSize;
            world.obstacles=blocks.ToArray();world.target=game.player.transform;world.followCamera=Camera.main;game.city=world;
            var pixel=Camera.main.GetComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>();
            if(pixel)UnityEngine.Object.DestroyImmediate(pixel);
            Camera.main.orthographicSize=9;Camera.main.backgroundColor=new Color(.016f,.023f,.035f);
            foreach(var label in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
            {
                if(label.gameObject.name=="Title")label.text="NEURAL LOCKDOWN  /  DISTRICT 07";
                if(label.gameObject.name=="Controls")label.text="WASD  Move    AUTO FIRE    TAB  City map    ESC  Pause    R  Restart";
                if(label.gameObject.name=="Vitals")label.text="INTEGRITY   100 / 100     |     NEUTRALIZED   0";
                if(label.canvas)label.color=new Color(.65f,.87f,.9f);
            }
            game.healthFill.color=new Color(.96f,.55f,.18f);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var builds=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene("Assets/Scenes/CyberCity.unity",true)};
            foreach(var old in EditorBuildSettings.scenes)if(old.path!="Assets/Scenes/CyberCity.unity")builds.Add(old);
            EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
            Selection.activeGameObject=game.gameObject;
            return "CyberCity saved: "+tileCount+" tiles, "+propCount+" props, "+blocks.Count+" collision footprints.";
        }
    }
}
