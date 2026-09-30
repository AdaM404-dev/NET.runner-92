import bpy,bmesh,json,math
from pathlib import Path
BASE=Path(__file__).resolve().parents[2];W=BASE/'work'
bpy.ops.wm.open_mainfile(filepath=str(W/'k7_source.blend'))
patches=[]
for o in bpy.data.collections['LOD0'].objects:
    bm=bmesh.new();bm.from_mesh(o.data);uv=bm.loops.layers.uv.active
    candidates=set(v for f in bm.faces if f.calc_area()<1e-12 for v in f.verts)
    if candidates:bmesh.ops.remove_doubles(bm,verts=list(candidates),dist=.0000001)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001)
    border=[e for e in bm.edges if e.is_boundary]
    if border:
        newfaces=bmesh.ops.holes_fill(bm,edges=border,sides=16)['faces']
        newset=set(newfaces)
        for f in newfaces:
            neigh=next(n for e in f.edges for n in e.link_faces if n not in newset)
            sample=sum((l[uv].uv for l in neigh.loops),start=neigh.loops[0][uv].uv*0)/len(neigh.loops)
            idx=len(patches);cx=18+idx*28;cy=4036
            f.material_index=neigh.material_index;f.smooth=False
            for j,l in enumerate(f.loops):l[uv].uv=((cx+8*math.cos(j*math.tau/len(f.loops)))/4096,(cy+8*math.sin(j*math.tau/len(f.loops)))/4096)
            patches.append({'cx':cx,'cy':cy,'sample_uv':list(sample)})
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
bpy.ops.wm.save_as_mainfile(filepath=str(W/'k7_clean.blend'))
(W/'cap_patches.json').write_text(json.dumps(patches))
meta=json.loads((W/'build_metadata.json').read_text())
for item in meta['colliders']:
    if item[0]=='Head':item[2]=[0,-.012,1.918];item[3]=[.204,.24,.164]
(W/'build_metadata.json').write_text(json.dumps(meta,indent=2))
print('CLOSED_CAPS',len(patches))
