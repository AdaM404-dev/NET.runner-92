#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NetRunner.World
{
    public static class RoomReachability
    {
        const float Pitch=.32f;
        const int X=113;
        const int Z=61;
        static readonly int[] DX={1,0,-1,0};
        static readonly int[] DZ={0,1,0,-1};

        public static string Probe()
        {
            string file=Path.Combine(Application.dataPath,"NETRunner92/Environment/Hospital/RoomSchedule.json");
            var plan=JsonUtility.FromJson<HospitalRoomSchedule>(File.ReadAllText(file));
            var missed=new List<string>();
            var totals=new List<string>();
            for (int level=1;level<=4;level++)
            {
                float height=(level-1)*4.2f;
                var checkedGrid=new byte[2*X+1,2*Z+1];
                var open=new Queue<Vector2Int>();
                var visited=new List<Vector2>();
                open.Enqueue(Vector2Int.zero);
                checkedGrid[X,Z]=1;
                while(open.Count>0)
                {
                    var p=open.Dequeue();
                    float x=p.x*Pitch,z=p.y*Pitch;
                    if (!Physics.Raycast(new Vector3(x,height+.10f,z),Vector3.down,out var floor,.20f,1,QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(floor.point.y-height)>.065f) continue;
                    if (Physics.CheckCapsule(new Vector3(x,floor.point.y+.31f,z),new Vector3(x,floor.point.y+1.49f,z),.275f,1,QueryTriggerInteraction.Ignore))
                        continue;
                    visited.Add(new Vector2(x,z));
                    for(int i=0;i<4;i++)
                    {
                        int xx=p.x+DX[i],zz=p.y+DZ[i];
                        if(Mathf.Abs(xx)>X||Mathf.Abs(zz)>Z||checkedGrid[xx+X,zz+Z]!=0)continue;
                        checkedGrid[xx+X,zz+Z]=1;
                        open.Enqueue(new Vector2Int(xx,zz));
                    }
                }
                int count=0,found=0;
                foreach(var room in plan.rooms)
                {
                    if(room.floor!=level)continue;
                    count++;
                    bool hit=visited.Exists(p=>Array.Exists(room.tiles,t=>p.x>t.x0+.28f&&p.x<t.x1-.28f&&p.y>t.z0+.28f&&p.y<t.z1-.28f));
                    if(hit)found++;
                    else missed.Add($"L{level} {room.name}");
                }
                totals.Add($"L{level} {found}/{count} ({visited.Count} accessible samples)");
            }
            return string.Join("; ",totals)+(missed.Count==0?"":" | Missing: "+string.Join(", ",missed));
        }
    }
}
#endif

