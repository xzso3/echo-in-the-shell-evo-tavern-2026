using System.Collections.Generic;
using UnityEngine;
namespace PlagueSurvivor
{
    public sealed class CyberCityWorld : MonoBehaviour
    {
        public Rect[] obstacles = new Rect[0];
        public Vector2 halfSize = new Vector2(32,22);
        public Transform target;
        public Camera followCamera;
        public float viewHeight = 9;
        public bool overview;
        bool[] walkable;
        int[] distance;
        int width, height, lastCell = -1;
        readonly Queue<int> queue = new Queue<int>();
        void Awake(){BuildGrid();}
        public bool IsFree(Vector2 p, float radius=.38f)
        {
            if(Mathf.Abs(p.x)>halfSize.x-radius||Mathf.Abs(p.y)>halfSize.y-radius)return false;
            foreach(var r in obstacles) if(p.x>r.xMin-radius&&p.x<r.xMax+radius&&p.y>r.yMin-radius&&p.y<r.yMax+radius)return false;
            return true;
        }
        public Vector2 Move(Vector2 from, Vector2 delta)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.2f));
            delta/=steps;
            for(int i=0;i<steps;i++)
            {
                Vector2 x=from+new Vector2(delta.x,0);if(IsFree(x))from=x;
                Vector2 y=from+new Vector2(0,delta.y);if(IsFree(y))from=y;
            }
            return from;
        }
        static bool Slab(float origin,float delta,float min,float max,ref float a,ref float b)
        {
            if(Mathf.Abs(delta)<.00001f)return origin>=min&&origin<=max;
            float x=(min-origin)/delta,y=(max-origin)/delta;
            if(x>y){float z=x;x=y;y=z;}
            a=Mathf.Max(a,x);b=Mathf.Min(b,y);return a<=b;
        }
        public bool LineClear(Vector2 from,Vector2 to,float padding=0)
        {
            foreach(var r in obstacles)
            {
                float a=0,b=1;
                if(Slab(from.x,to.x-from.x,r.xMin-padding,r.xMax+padding,ref a,ref b)&&
                   Slab(from.y,to.y-from.y,r.yMin-padding,r.yMax+padding,ref a,ref b))return false;
            }
            return true;
        }
        int Cell(Vector2 p)
        {
            int x=Mathf.Clamp(Mathf.FloorToInt(p.x+halfSize.x),0,width-1);
            int y=Mathf.Clamp(Mathf.FloorToInt(p.y+halfSize.y),0,height-1);
            return y*width+x;
        }
        Vector2 Point(int cell){return new Vector2(cell%width+.5f-halfSize.x,cell/width+.5f-halfSize.y);}
        public void BuildGrid()
        {
            width=Mathf.RoundToInt(halfSize.x*2);height=Mathf.RoundToInt(halfSize.y*2);
            walkable=new bool[width*height];distance=new int[walkable.Length];lastCell=-1;
            for(int i=0;i<walkable.Length;i++)walkable[i]=IsFree(Point(i),.48f);
        }
        public void UpdateNavigation(Vector2 destination)
        {
            if(walkable==null)BuildGrid();
            int end=Cell(destination);if(end==lastCell)return;lastCell=end;
            for(int i=0;i<distance.Length;i++)distance[i]=int.MaxValue;
            if(!walkable[end]){float nearest=float.MaxValue;for(int i=0;i<walkable.Length;i++)if(walkable[i]){float d=(Point(i)-destination).sqrMagnitude;if(d<nearest){nearest=d;end=i;}}}
            queue.Clear();queue.Enqueue(end);distance[end]=0;
            while(queue.Count>0)
            {
                int at=queue.Dequeue();int x=at%width,y=at/width;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(Mathf.Abs(dx)+Mathf.Abs(dy)!=1)continue;
                    int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;
                    int n=ny*width+nx;if(!walkable[n]||distance[n]!=int.MaxValue)continue;
                    distance[n]=distance[at]+1;queue.Enqueue(n);
                }
            }
        }
        public Vector2 Chase(Vector2 from, Vector2 destination)
        {
            if(LineClear(from,destination,.48f))return (destination-from).normalized;
            int at=Cell(from),x=at%width,y=at/width;
            int best=at,bestCost=distance[at];float bestRange=float.MaxValue;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;
                int n=ny*width+nx;float range=(Point(n)-from).sqrMagnitude;
                if(!walkable[n]||!LineClear(from,Point(n),.4f))continue;
                if(distance[n]<bestCost||(distance[n]==bestCost&&range<bestRange)){best=n;bestCost=distance[n];bestRange=range;}
            }
            return (Point(best)-from).normalized;
        }
        public bool Reachable(Vector2 point)
        {
            return distance!=null&&distance[Cell(point)]!=int.MaxValue;
        }
        void LateUpdate()
        {
            if(!target||!followCamera)return;
            if(Input.GetKeyDown(KeyCode.Tab))overview=!overview;
            float size=overview?Mathf.Max(halfSize.y+8,(halfSize.x+3)/followCamera.aspect):Mathf.Max(viewHeight,13/followCamera.aspect);
            followCamera.orthographicSize=size;
            float x=overview?0:Mathf.Clamp(target.position.x,-Mathf.Max(0,halfSize.x-size*followCamera.aspect+1),Mathf.Max(0,halfSize.x-size*followCamera.aspect+1));
            float y=overview?1.5f:Mathf.Clamp(target.position.y,-Mathf.Max(0,halfSize.y-size+2.5f),Mathf.Max(0,halfSize.y-size+2.5f));
            followCamera.transform.position=new Vector3(x,y,-10);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(1,.5f,0,.65f);
            foreach(var r in obstacles)Gizmos.DrawWireCube(r.center,r.size);
        }
    }
}
