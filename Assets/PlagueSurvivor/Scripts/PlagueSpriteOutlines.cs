using System.Collections.Generic;
using UnityEngine;
namespace PlagueSurvivor
{
    [RequireComponent(typeof(PlagueGame))]
    public sealed class PlagueSpriteOutlines : MonoBehaviour
    {
        public bool cyberArt;
        static readonly float[][] CyberRows = {
            new float[]{0,52,75,20,26,94,48,17,104,83,1,116,112,1,113,148,6,105,175,33,92,197,41,57,209,42,55},
            new float[]{0,48,69,18,26,83,45,16,87,70,4,98,90,9,98,104,18,116,114,26,113,120,26,82,147,27,50,157,28,43},
            new float[]{0,37,53,15,27,67,32,14,79,58,1,94,79,8,86,100,14,83,119,24,74,133,43,68}
        };
        static readonly float[][] Rows = {
            new float[]{0,65,71,12,53,85,42,35,97,68,17,108,90,7,123,111,12,124,130,27,113,158,36,114,181,79,112,186,80,106},
            new float[]{0,57,77,13,45,93,35,25,112,61,9,125,93,1,137,123,35,137,135,39,107,153,40,70,160,43,64},
            new float[]{0,80,101,14,62,112,39,34,132,65,19,151,100,10,164,120,27,150,143,47,131,162,48,121,168,54,112}
        };
        void Awake() { Attach(GetComponent<PlagueGame>().player, 0); }
        public void Attach(SpriteRenderer source, int kind)
        {
            var go = new GameObject("Art silhouette");
            go.transform.SetParent(source.transform, false);
            var sprite = source.sprite;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var rows = cyberArt ? CyberRows[kind] : Rows[kind];
            for (int i=0;i<rows.Length;i+=3)
            {
                for(int side=1;side<=2;side++)
                {
                    vertices.Add(new Vector3((rows[i+side]-sprite.pivot.x)/sprite.pixelsPerUnit,
                        (sprite.rect.height-rows[i]-sprite.pivot.y)/sprite.pixelsPerUnit,0));
                    uv.Add(new Vector2((sprite.rect.x+rows[i+side])/sprite.texture.width,
                        (sprite.rect.y+sprite.rect.height-rows[i])/sprite.texture.height));
                }
                if(i>0){int n=vertices.Count-4;triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
            var mesh = new Mesh { name="Actor silhouette" };
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            var colors=new Color[vertices.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;
            mesh.colors=colors;mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=source.sharedMaterial;
            var visual=go.AddComponent<PlagueActorMesh>();
            visual.source=source;visual.mesh=mesh;visual.target=renderer;
            source.enabled=false;
        }
    }
    public sealed class PlagueActorMesh : MonoBehaviour
    {
        public SpriteRenderer source;
        public MeshRenderer target;
        public Mesh mesh;
        MaterialPropertyBlock properties;
        void LateUpdate()
        {
            if(!source)return;
            if(properties==null)properties=new MaterialPropertyBlock();
            properties.SetTexture("_MainTex",source.sprite.texture);
            properties.SetColor("_Color",source.color);
            properties.SetColor("_RendererColor",Color.white);
            target.SetPropertyBlock(properties);
            target.sortingLayerID=source.sortingLayerID;
            target.sortingOrder=source.sortingOrder;
            transform.localScale=new Vector3(source.flipX ? -1 : 1,1,1);
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
