import bpy,json,math
from pathlib import Path
from collections import deque
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'Warehouse_NearFuture.blend'))
verts=[];faces=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH' or o.name.startswith(('BLOCK_','COL_')) or o.get('interactive_type') or o.get('follows_operable_door'):continue
    if o.name.startswith(('LIGHT','DRAIN','GROUND_Dock','FLOOR_MainHall_Control','RAIL')):continue
    vs=[o.matrix_world@v.co for v in o.data.vertices]
    if min(v.z for v in vs)>6.4 or max(v.z for v in vs)<-.3:continue
    k=len(verts);verts.extend(vs);faces.extend([tuple(k+i for i in p.vertices) for p in o.data.polygons])
bvh=BVHTree.FromPolygons(verts,faces)
step=.4;nx=300;ny=200;x0=-59.8;y0=-39.8
walk=set()
def p(i,j,z):return Vector((x0+i*step,y0+j*step,z))
def hit(pos,dr,dist):return bvh.ray_cast(pos,Vector(dr),dist)[0]
for i in range(nx):
    for j in range(ny):
        q=p(i,j,.06)
        floor=hit(q,(0,0,-1),.1)
        if floor is None:continue
        clear=True
        for z in [.25,1.05,1.72]:
            for dr in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0)]:
                if hit(p(i,j,z),dr,.27) is not None:clear=False;break
            if not clear:break
        if clear:walk.add((i,j))
start=min(walk,key=lambda q:(x0+q[0]*step)**2+(y0+q[1]*step)**2)
seen={start};queue=deque([start])
while queue:
    i,j=queue.popleft()
    for di,dj in [(1,0),(-1,0),(0,1),(0,-1)]:
        k=(i+di,j+dj)
        if k not in walk or k in seen:continue
        if hit(p(i,j,1.05),(di,dj,0),step) is not None:continue
        seen.add(k);queue.append(k)
data=json.loads((root/'Asset_manifest.json').read_text())
rooms=[]
for r in data['rooms']:
    if r['floor_z']!=0:continue
    x,y=r['door'];nearest=min(walk,key=lambda q:(x0+q[0]*step-x)**2+(y0+q[1]*step-y)**2)
    rooms.append({'room':r['name'],'door_reachable':nearest in seen})
result={'method':'Ground-floor grid flood-fill, 0.4 m spacing, 0.54 m diameter clearance at 0.25 / 1.05 / 1.72 m; operable door leaves excluded. Base geometry only. Not a Unity NavMesh bake.','walkable_samples':len(walk),'reachable_samples':len(seen),'room_door_checks':rooms,'unreachable_room_doors':[r['room'] for r in rooms if not r['door_reachable']]}
upper=set()
for i in range(nx):
    for j in range(ny):
        if hit(p(i,j,3.66),(0,0,-1),.1) is None:continue
        if all(hit(p(i,j,z),dr,.27) is None for z in [3.85,4.65,5.32] for dr in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0)]):upper.add((i,j))
starts=[]
for x,y in [(-42.6,-8.5),(-23,-21.7),(42,25.8)]:starts.append(min(upper,key=lambda q:(x0+q[0]*step-x)**2+(y0+q[1]*step-y)**2))
seenup=set(starts);queue=deque(starts)
while queue:
    i,j=queue.popleft()
    for di,dj in [(1,0),(-1,0),(0,1),(0,-1)]:
        k=(i+di,j+dj)
        if k not in upper or k in seenup:continue
        if hit(p(i,j,4.65),(di,dj,0),step) is not None:continue
        seenup.add(k);queue.append(k)
result['upper_room_door_checks']=[]
for r in data['rooms']:
    if r['floor_z']!=3.6:continue
    x,y=r['door'];nearest=min(upper,key=lambda q:(x0+q[0]*step-x)**2+(y0+q[1]*step-y)**2)
    result['upper_room_door_checks'].append({'room':r['name'],'door_reachable_from_stair_landing':nearest in seenup})
result['stair_checks']=[]
for st in data['stairs']:
    top=Vector(st['top']);base=Vector(st['base']);delta=top-base;direction=Vector((delta.x,delta.y,0)).normalized()
    endpoint=top+direction*.20
    landing=hit(endpoint+Vector((0,0,.10)),(0,0,-1),.25)
    minhead=10
    for a in [.2,.4,.6,.8,.98]:
        q=base+delta*a+Vector((0,0,.25))
        hh=hit(q,(0,0,1),2)
        if hh is not None:minhead=min(minhead,(hh-q).length+.25)
    result['stair_checks'].append({'name':st['name'],'top_landing_present':landing is not None,'sampled_headroom_at_least_1_8m':minhead>=1.8,'minimum_hit_headroom_m':minhead if minhead<10 else None})
(root/'Route_validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result),flush=True)
