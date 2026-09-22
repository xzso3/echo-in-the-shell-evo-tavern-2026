using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PlagueSurvivor.Editor
{
    public static class CyberCityPolish
    {
        const string Root="Assets/CyberCity/";
        static List<Rect> blocks;
        static Material material;
        static void Prop(Transform parent,string meshName,Vector2 point,float scale=1,bool solid=false)
        {
            var go=new GameObject(meshName);go.transform.SetParent(parent);go.transform.position=point;go.transform.localScale=Vector3.one*scale;
            go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Meshes/"+meshName+".asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=100-Mathf.RoundToInt(point.y*10);
            if(solid)blocks.Add(new Rect(point.x-.55f*scale,point.y-.25f,1.1f*scale,.8f*scale));
        }
        public static string Apply()
        {
            if(GameObject.Find("04 - District landmarks"))return "Already applied";
            var game=Object.FindObjectOfType<PlagueGame>();
            if(!game||!game.city)throw new System.InvalidOperationException("Open CyberCity first");
            blocks=new List<Rect>(game.city.obstacles);
            material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/CityAtlas.mat");
            var landmarks=new GameObject("04 - District landmarks").transform;
            landmarks.SetParent(GameObject.Find("CYBER CITY - Modular atlas construction").transform);
            // Power exchange: offset banks and cable infrastructure.
            for(int i=0;i<4;i++){Prop(landmarks,"Power cabinet",new Vector2(-23+i*1.8f,17),.8f,true);Prop(landmarks,"Cable cabinet",new Vector2(-22,7+i*1.8f),.7f,true);}
            Prop(landmarks,"Transformer",new Vector2(-12,16),1.15f,true);
            // Security: guard station, gates and a narrow inspection approach.
            Prop(landmarks,"Guard booth",new Vector2(22,16),1.05f,true);
            Prop(landmarks,"Fence gate",new Vector2(13,17),1);
            for(int i=0;i<4;i++){Prop(landmarks,"Hazard barrier",new Vector2(11+i*1.7f,6),.65f,true);Prop(landmarks,"Surveillance camera",new Vector2(11+i*3.5f,17),.85f);}
            // Service market: vending arcade with small planters and refuse lane.
            for(int i=0;i<5;i++){Prop(landmarks,"Vending machine",new Vector2(-22+i*2.2f,-6),.7f,true);Prop(landmarks,"Small planter",new Vector2(-23,-15+i*1.7f),.75f);}
            Prop(landmarks,"Dumpster",new Vector2(-11,-16),1,true);
            // Cooling: exposed turbines and grouped ventilation plant.
            for(int i=0;i<4;i++){Prop(landmarks,"Turbine",new Vector2(11+i*2.8f,-6),.8f,true);Prop(landmarks,"Vent",new Vector2(22,-14+i*1.8f),1,true);}
            Prop(landmarks,"Transformer",new Vector2(12,-16),1,true);
            foreach(float cy in new[]{11f,-11f})foreach(float cx in new[]{-17f,17f})foreach(float side in new[]{-1f,1f})
                blocks.Add(new Rect(cx+side*8-.25f,cy-4,.5f,7.2f));
            blocks.Add(new Rect(-32,-22,1.25f,44));blocks.Add(new Rect(30.75f,-22,1.25f,44));
            blocks.Add(new Rect(-32,-22,64,.8f));blocks.Add(new Rect(-32,21,64,1));
            game.city.obstacles=blocks.ToArray();
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/Cyberpunk/cyber_city_tileset_atlas.png");
            var variants=new Sprite[3];
            Rect[] source={new Rect(384,25,100,103),new Rect(505,25,99,103),new Rect(384,147,100,101)};
            for(int i=0;i<3;i++){var r=source[i];variants[i]=Sprite.Create(atlas,new Rect(r.x,atlas.height-r.y-r.height,r.width,r.height),new Vector2(.5f,.5f),64);AssetDatabase.CreateAsset(variants[i],Root+"Sprites/AsphaltVariation"+i+".asset");}
            var random=new System.Random(817);int changed=0;
            foreach(var sr in Object.FindObjectsOfType<SpriteRenderer>())
                if(sr.name=="Wet asphalt"&&random.Next(4)!=0){var sprite=variants[random.Next(variants.Length)];sr.sprite=sprite;sr.transform.localScale=new Vector3(128/sprite.rect.width,128/sprite.rect.height,1);changed++;}
            var canvas=Object.FindObjectOfType<Canvas>();
            foreach(string name in new[]{"Header","Footer"})
            {
                var rt=canvas.transform.Find(name).GetComponent<RectTransform>();
                rt.anchorMin=new Vector2(0,name=="Header"?1:0);rt.anchorMax=new Vector2(1,name=="Header"?1:0);
                rt.sizeDelta=new Vector2(0,rt.sizeDelta.y);rt.anchoredPosition=Vector2.zero;
            }
            var overlay=game.overlay.GetComponent<RectTransform>();
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=Vector2.zero;overlay.offsetMax=Vector2.zero;
            EditorUtility.SetDirty(game.city);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            return "Added "+landmarks.childCount+" district props, "+blocks.Count+" total obstacles, "+changed+" road variations; responsive HUD.";
        }
    }
}