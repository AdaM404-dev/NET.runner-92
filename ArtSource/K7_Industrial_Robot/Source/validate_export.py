import bpy,json,math,numpy as np
from pathlib import Path
from mathutils import Vector
B=Path(__file__).resolve().parents[2];O=B/'outputs'/'K7_Industrial_Robot';W=B/'work'
bpy.ops.wm.open_mainfile(filepath=str(O/'Blender'/'K7_Industrial_Robot.blend'))
r=bpy.data.objects['K7_Rig'];s=bpy.context.scene;s.frame_set(1)
meshes=list(bpy.data.collections['LOD0'].objects)
uvs=[]
for o in meshes:
    o.data.calc_loop_triangles();uv=o.data.uv_layers.active
    uvs.extend([[tuple(uv.data[l].uv) for l in t.loops] for t in o.data.loop_triangles])
np.save(W/'lod0_uv_triangles.npy',np.array(uvs,dtype=np.float32))
report={'source_blend_opens':True,'textures_packed':all(i.packed_file is not None for i in bpy.data.images if i.name.startswith('K7_') and not i.name.startswith('K7_Primary')),'deform_bones':len(r.data.bones)}
report['external_texture_paths_exist']=all(Path(bpy.path.abspath(i.filepath)).exists() for i in bpy.data.images if i.name.startswith('K7_') and i.filepath)
# Edge lengths are invariant under every sampled diagnostic pose.
rigid={}
for f in [30,60,90,150,210,270]:
    s.frame_set(f);dep=bpy.context.evaluated_depsgraph_get();maxerr=0
    for o in meshes:
        ev=o.evaluated_get(dep);em=ev.to_mesh()
        for e in o.data.edges:
            a,b=e.vertices
            rest=(o.data.vertices[a].co-o.data.vertices[b].co).length
            moved=(em.vertices[a].co-em.vertices[b].co).length
            maxerr=max(maxerr,abs(rest-moved))
        ev.to_mesh_clear()
    rigid[str(f)]=maxerr
report['rigid_edge_length_max_error_m']=rigid
s.frame_set(1)
sourcecounts={o.name:len(o.data.polygons) for col in ['LOD0','LOD1','LOD2','LOD3'] for o in bpy.data.collections[col].objects}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(O/'FBX'/'K7_Robot.fbx'),use_anim=False)
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];r=rigs[0]
visual=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.name.startswith('COL_')]
verts=[o.matrix_world@v.co for o in visual if o.name=='K7_LOD0' or o.name.startswith('Armor_') and '_LOD' not in o.name or o.name=='K7_HackInterface' for v in o.data.vertices]
report['roundtrip']={'visual_mesh_count':len(visual),'bone_count':len(r.data.bones),'height_m':max(v.z for v in verts)-min(v.z for v in verts),'ground_z_m':min(v.z for v in verts),'collider_count':len([o for o in bpy.context.scene.objects if o.name.startswith('COL_')]),'triangle_counts_match':all(o.name in sourcecounts and len(o.data.polygons)==sourcecounts[o.name] for o in visual),'sockets':{n:list(bpy.data.objects[n].matrix_world.translation) for n in ['HackPoint','VisionOrigin','Hand_L_Point','Foot_R_Point']},'root_position':list(r.matrix_world.translation),'armature_scale':list(r.scale)}
expected=json.loads((O/'Validation'/'asset_report.json').read_text())
report['roundtrip']['socket_position_errors_m']={n:(Vector(expected['sockets'][n]['blender_world_xyz'])-Vector(pos)).length for n,pos in report['roundtrip']['sockets'].items()}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(O/'FBX'/'K7_ArticulationCheck.fbx'),use_anim=True)
r=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
report['animation_roundtrip']={'actions':[a.name for a in bpy.data.actions],'frames':list(r.animation_data.action.frame_range),'bones':len(r.data.bones)}
bpy.context.scene.frame_set(150)
report['animation_roundtrip']['finger_rotation_degrees']=math.degrees(r.pose.bones['Index_2_L'].rotation_quaternion.angle)
(O/'Validation'/'fbx_roundtrip.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
