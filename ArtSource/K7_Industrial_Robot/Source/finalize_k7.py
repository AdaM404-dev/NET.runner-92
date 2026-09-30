import bpy, math, json, os, shutil
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
BASE=Path(__file__).resolve().parents[2];OUT=BASE/'outputs'/'K7_Industrial_Robot';WORK=BASE/'work'
bpy.ops.wm.open_mainfile(filepath=str(WORK/'k7_clean.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['K7_Rig'];meta=json.loads((WORK/'build_metadata.json').read_text())
scene.cycles.device='GPU';scene.cycles.samples=48
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'

images={}
for name in ['BaseColor','Normal','Metallic','Roughness','AO']:
    img=bpy.data.images.load(str(OUT/'Textures'/'4K'/('K7_'+name+'.png')),check_existing=True)
    if name!='BaseColor':img.colorspace_settings.name='Non-Color'
    images[name]=img
mats={}
for kind in ['Armor','Internal','Metal','Rubber','SensorGlass','Cables','StatusLight']:
    m=bpy.data.materials.new('MAT_K7_'+kind);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    out=n.new('ShaderNodeOutputMaterial');out.location=(640,80)
    bs=n.new('ShaderNodeBsdfPrincipled');bs.location=(350,80);l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    if kind=='StatusLight':
        bs.inputs['Base Color'].default_value=(.8,.8,.8,1);bs.inputs['Roughness'].default_value=.25;bs.inputs['Metallic'].default_value=.05
        bs.inputs['Emission Color'].default_value=(1,.012,.007,1);bs.inputs['Emission Strength'].default_value=5
        m['Patrol']=[.65,.82,1];m['Suspicious']=[1,.30,.015];m['Hostile']=[1,.012,.007];m['Hacked']=[.015,.26,1]
    else:
        nodes={}
        for i,(name,img) in enumerate(images.items()):
            tex=n.new('ShaderNodeTexImage');tex.image=img;tex.name='K7_'+name;tex.label=name;tex.location=(-600,400-i*230);nodes[name]=tex
        mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.7;mix.location=(0,320)
        l.new(nodes['BaseColor'].outputs['Color'],mix.inputs[1]);l.new(nodes['AO'].outputs['Color'],mix.inputs[2]);l.new(mix.outputs[0],bs.inputs['Base Color'])
        l.new(nodes['Metallic'].outputs['Color'],bs.inputs['Metallic']);l.new(nodes['Roughness'].outputs['Color'],bs.inputs['Roughness'])
        normal=n.new('ShaderNodeNormalMap');normal.location=(70,-90);normal.inputs['Strength'].default_value=.6;l.new(nodes['Normal'].outputs['Color'],normal.inputs['Color']);l.new(normal.outputs['Normal'],bs.inputs['Normal'])
        if kind=='SensorGlass':bs.inputs['Coat Weight'].default_value=.25
    m['texture_set']='K7_shared_atlas';mats[kind]=m

base=list(bpy.data.collections['LOD0'].objects)
def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
for o in base:
    old=[mat['final_kind'] for mat in o.data.materials];kinds=list(dict.fromkeys(old));idx=[kinds.index(old[p.material_index]) for p in o.data.polygons]
    o.data.materials.clear()
    for k in kinds:o.data.materials.append(mats[k])
    for p,i in zip(o.data.polygons,idx):p.material_index=i
    # Source-specific bookkeeping is in the build report, not on the final object.
    for k in ['part_group','bone','kind','decal','uv_tile','area']:
        if k in o:del o[k]
total=sum(tris(o) for o in base)
if total>136000:
    body=bpy.data.objects['K7_LOD0'];other=total-tris(body);ratio=(128000-other)/tris(body);active(body)
    arm=body.modifiers.get('K7_RigidSkin');body.modifiers.remove(arm)
    dec=body.modifiers.new('LOD0_optimization','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
    arm=body.modifiers.new('K7_RigidSkin','ARMATURE');arm.object=rig

lods=[base]
for level,ratio in [(1,.53),(2,.255),(3,.115)]:
    meshes=[];col=bpy.data.collections['LOD'+str(level)]
    for src in base:
        o=src.copy();o.data=src.data.copy();col.objects.link(o)
        o.name='K7_LOD'+str(level) if src.name=='K7_LOD0' else src.name+'_LOD'+str(level)
        o['LOD']=level;o['base_part']=src.name
        o.modifiers.clear();active(o)
        dec=o.modifiers.new('Silhouette_reduction','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=dec.name)
        a=o.modifiers.new('K7_RigidSkin','ARMATURE');a.object=rig;meshes.append(o)
    lods.append(meshes)
for level,meshes in enumerate(lods):
    for o in meshes:
        active(o);tri=o.modifiers.new('Stable_export_triangulation','TRIANGULATE');tri.quad_method='FIXED';tri.ngon_method='BEAUTY'
        bpy.ops.object.modifier_apply(modifier=tri.name)
        o['LOD']=level;o['rigid_mechanical_skin']=True

# Replace display-only collider empties with closed, low-poly box proxies.
col=bpy.data.collections['K7_COLLIDERS']
for o in list(col.objects):bpy.data.objects.remove(o,do_unlink=True)
for name,bone,pos,dims in meta['colliders']:
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name='COL_'+name;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);o.display_type='WIRE';o.hide_render=True
    o['collider_shape']='Box';o['bone']=bone;o['dimensions_m']=dims
    world=Matrix.Translation(Vector(pos));o.parent=rig;o.parent_type='BONE';o.parent_bone=bone;bpy.context.view_layer.update();o.matrix_world=world

# Rigid articulation proof, separate from the rest-pose FBX.
scene.render.fps=30;scene.frame_start=1;scene.frame_end=300
if rig.animation_data:rig.animation_data.action=None
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
frames=[1,30,60,90,120,150,180,210,240,270,300]
def rot(name,axis,degrees):
    pb=rig.pose.bones[name];local=pb.bone.matrix_local.to_3x3().inverted()@Vector(axis)
    pb.rotation_quaternion=Quaternion(local,math.radians(degrees))
for f in frames:
    for pb in rig.pose.bones:pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
    if f in (30,60):rot('Head',(0,0,1),-55 if f==30 else 55);rot('Neck',(1,0,0),-10 if f==30 else 12)
    if f==90:
        # Local pelvis Y is world Z in the bind pose.
        pb=rig.pose.bones['Pelvis'];pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-.105))
        for side in ['L','R']:
            rot('Thigh_'+side,(1,0,0),-28);rot('Shin_'+side,(1,0,0),56);rot('Foot_'+side,(1,0,0),-28)
            rot('UpperArm_'+side,(1,0,0),-40);rot('LowerArm_'+side,(1,0,0),-35)
    if f==150:
        for side in ['L','R']:
            rot('UpperArm_'+side,(1,0,0),-60);rot('LowerArm_'+side,(1,0,0),-25)
            for finger in ['Index','Middle','Ring','Little']:
                for i in range(1,4):rot(f'{finger}_{i}_{side}',(1,0,0),-65 if i!=1 else -50)
            for i in range(1,4):rot(f'Thumb_{i}_{side}',(0,0,1),35 if side=='L' else -35)
    if f in (210,270):
        side='L' if f==210 else 'R';other='R' if side=='L' else 'L'
        rot('Thigh_'+side,(1,0,0),-28);rot('Shin_'+side,(1,0,0),45);rot('Foot_'+side,(1,0,0),-17);rot('Toe_'+side,(1,0,0),15)
        rot('UpperArm_'+other,(1,0,0),-22);rot('UpperArm_'+side,(1,0,0),18)
    for pb in rig.pose.bones:
        pb.keyframe_insert(data_path='rotation_quaternion',frame=f,group=pb.name);pb.keyframe_insert(data_path='location',frame=f,group=pb.name)
action=rig.animation_data.action;action.name='K7_ArticulationCheck';action.use_fake_user=True
scene.frame_set(1)

# Export excludes render stage, authoring controls and the reference image.
common=[rig]+list(bpy.data.collections['K7_GAMEPLAY_POINTS'].objects)+list(col.objects)
def export(path,meshes,animation=False):
    bpy.ops.object.select_all(action='DESELECT')
    for o in common+meshes:o.hide_set(False);o.hide_viewport=False;o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_space_transform=True,bake_space_transform=False,object_types={'ARMATURE','MESH','EMPTY'},use_mesh_modifiers=True,mesh_smooth_type='OFF',use_tspace=True,use_custom_props=True,add_leaf_bones=False,primary_bone_axis='Y',secondary_bone_axis='X',use_armature_deform_only=True,armature_nodetype='NULL',bake_anim=animation,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False,axis_forward='-Z',axis_up='Y')

rig.animation_data.action=None
export(OUT/'FBX'/'K7_Robot.fbx',[o for L in lods for o in L])
for level,meshes in enumerate(lods):export(OUT/'FBX'/('K7_LOD'+str(level)+'.fbx'),meshes)
rig.animation_data.action=action;scene.frame_set(1)
export(OUT/'FBX'/'K7_ArticulationCheck.fbx',[],True)

# Validation of geometry, skinning, scale, UV range, proxies and IK reach.
report={'blender':bpy.app.version_string,'lods':[],'skeleton_bones':len(rig.data.bones),'materials':list(m.name for m in mats.values()),'sockets':{},'IK':{}}
for level,meshes in enumerate(lods):
    badweights=badtransforms=uvbad=degenerate=boundary=nonmanifold=0
    import bmesh
    for o in meshes:
        badtransforms+=int(o.location.length>1e-6 or any(abs(v-1)>1e-6 for v in o.scale) or any(abs(a)>1e-6 for a in o.rotation_euler))
        for v in o.data.vertices:
            weights=[g.weight for g in v.groups if g.weight>1e-5]
            if len(weights)!=1 or abs(sum(weights)-1)>1e-5:badweights+=1
        for uv in o.data.uv_layers.active.data:
            if any(v<0 or v>1 for v in uv.uv):uvbad+=1
        degenerate+=sum(p.area<1e-12 for p in o.data.polygons)
        bm=bmesh.new();bm.from_mesh(o.data);boundary+=sum(e.is_boundary for e in bm.edges);nonmanifold+=sum(not e.is_manifold for e in bm.edges);bm.free()
    verts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    report['lods'].append({'level':level,'triangles':sum(tris(o) for o in meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'renderers':len(meshes),'height_m':max(v.z for v in verts)-min(v.z for v in verts),'ground_z_m':min(v.z for v in verts),'bounds_m':[[min(v[i] for v in verts),max(v[i] for v in verts)] for i in range(3)],'nonrigid_vertices':badweights,'bad_object_transforms':badtransforms,'uv_out_of_range':uvbad,'degenerate_triangles':degenerate,'boundary_edges':boundary,'nonmanifold_edges':nonmanifold})
for o in bpy.data.collections['K7_GAMEPLAY_POINTS'].objects:report['sockets'][o.name]={'blender_world_xyz':list(o.matrix_world.translation),'bone':o.parent_bone}
rig.animation_data.action=None
for side in ['L','R']:
    for limb,bn in [('Foot','Shin_'+side),('Hand','LowerArm_'+side)]:
        target=bpy.data.objects['CTRL_'+limb+'IK_'+side];start=target.location.copy();target.location+=Vector((0,-.055,.055));rig[limb+'IK_'+side]=1.0
        rig.update_tag();scene.frame_set(2);bpy.context.view_layer.update();dep=bpy.context.evaluated_depsgraph_get();er=rig.evaluated_get(dep)
        tail=er.matrix_world@er.pose.bones[bn].tail;error=(tail-target.matrix_world.translation).length
        report['IK'][limb+'_'+side]={'target_error_m':error,'pass':error<.006}
        target.location=start;rig[limb+'IK_'+side]=0.0;rig.update_tag();scene.frame_set(1);bpy.context.view_layer.update()
rig.animation_data.action=action;scene.frame_set(1)
report['unity_editor_tested']=False
report['notes']=['LOD0 UV components occupy exclusive atlas tiles. Each tile is smart-unwrapped; detailed raster overlap audit is in uv_validation.json.','Closed parts overlap intentionally at mechanical assemblies. Full swept-volume collision certification is not claimed.','Animation is an articulation diagnostic, not a finished locomotion library.','Generic Unity import is the supported default. Humanoid mapping needs manual Avatar configuration.']
(OUT/'Validation'/'asset_report.json').write_text(json.dumps(report,indent=2))
(OUT/'Validation'/'skeleton.json').write_text(json.dumps(meta['bones'],indent=2))

for level,meshes in enumerate(lods):
    for o in meshes:o.hide_render=level!=0;o.hide_set(level!=0)
for c in ['K7_COLLIDERS','K7_ANIMATION_CONTROLS','K7_GAMEPLAY_POINTS']:
    for o in bpy.data.collections[c].objects:o.hide_set(True)

# Reference stays packed in the authoring file and is never exported.
refpath=OUT/'Reference'/'K7_Reference.jpg'
if refpath.exists():
    img=bpy.data.images.load(str(refpath));img.pack();ref=bpy.data.objects.new('K7_Primary_Reference',None);bpy.data.collections['K7_REFERENCE'].objects.link(ref);ref.empty_display_type='IMAGE';ref.data=img;ref.empty_display_size=2.2;ref.location=(2,1,1);ref.hide_set(True)
for img in images.values():
    img.pack();img.filepath=bpy.path.relpath(img.filepath,start=str(OUT/'Blender'))

scene['Asset']='K7 Industrial Humanoid';scene['Units']='meters';scene['Forward']='Blender -Y; FBX -Z forward / Y up; intended Unity +Z';scene['Status']='See Validation and README for tested scope'
scene.frame_set(1)
active(rig)
for area in bpy.context.screen.areas if bpy.context.screen else []:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_distance=3.2;area.spaces.active.region_3d.view_location=(0,0,1.05);area.spaces.active.shading.type='MATERIAL'
scene.render.image_settings.file_format='PNG'
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Blender'/'K7_Industrial_Robot.blend'),compress=True,relative_remap=False)

cam=scene.camera
def render(name,loc,target,scale,res=(1000,1300),frame=1):
    scene.frame_set(frame);cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale
    scene.render.resolution_x=res[0];scene.render.resolution_y=res[1];scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/'Previews'/name)
    bpy.ops.render.render(write_still=True)
render('K7_Hero.png',(2.4,-4.5,2.4),(0,0,1.05),2.34,(1200,1600))
render('K7_Front.png',(0,-5,1.03),(0,0,1.03),2.2)
render('K7_Side.png',(5,0,1.03),(0,0,1.03),2.2)
render('K7_Rear.png',(0,5,1.03),(0,0,1.03),2.2)
render('K7_HackInterface.png',(1.1,3,2.01),(0,.08,1.54),.79,(1200,1100))
render('K7_Grasp_Check.png',(2,-4,2.2),(0,-.17,1.4),1.70,(1300,1000),150)
render('K7_Crouch_Check.png',(2.4,-4.5,2.0),(0,-.06,.98),2.2,(1100,1400),90)
render('K7_Face_Hostile.png',(.38,-1.8,2.1),(0,-.02,1.904),.43,(1400,1100))
bs=next(n for n in mats['StatusLight'].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
for state,color in [('Patrol',(.65,.82,1,1)),('Suspicious',(1,.30,.015,1)),('Hostile',(1,.012,.007,1)),('Hacked',(.015,.26,1,1))]:
    bs.inputs['Emission Color'].default_value=color
    render('K7_State_'+state+'.png',(.20,-2,2.02),(0,-.02,1.92),.31,(700,620))
bs.inputs['Emission Color'].default_value=(1,.012,.007,1)
for level in [1,2,3]:
    for i,meshes in enumerate(lods):
        for o in meshes:o.hide_render=i!=level
    render('K7_LOD'+str(level)+'.png',(2.4,-4.5,2.4),(0,0,1.05),2.34,(700,950))
print('FINALIZE_COMPLETE',json.dumps(report['lods']),flush=True)

