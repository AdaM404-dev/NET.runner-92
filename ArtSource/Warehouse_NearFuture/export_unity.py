import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Warehouse_NearFuture.blend'))
s=bpy.context.scene;bpy.context.window.view_layer=s.view_layers['01_COMPLETE']
for l in s.view_layers:l.use=l.name=='01_COMPLETE'
c=bpy.data.collections['14_COLLISION'];c.hide_viewport=False
def find(lc,name):
    if lc.name==name:return lc
    for ch in lc.children:
        v=find(ch,name)
        if v:return v
for l in s.view_layers:find(l.layer_collection,'14_COLLISION').exclude=False
for ob in list(c.objects):
    if ob.name.startswith('COL_NF_'):bpy.data.objects.remove(ob,do_unlink=True)
for ob in list(s.objects):
    if ob.type!='MESH' or not ob.name.startswith(('NF_PANEL_','NF_DOCK_Automation','NF_DOCK_AlignmentScanner','NF_COLUMN_')):continue
    copy=bpy.data.objects.new('COL_'+ob.name,ob.data.copy());c.objects.link(copy);copy.matrix_world=ob.matrix_world.copy();copy.data.materials.clear();copy.hide_render=True;copy.display_type='WIRE';copy['source_visual']=ob.name;copy['collision_type']='Static non-convex MeshCollider'
visual=[o for o in s.objects if o.type=='MESH' and not o.name.startswith(('BLOCK_','PRESENTATION_','COL_'))]
for ob in s.objects:ob.select_set(False)
for ob in visual:ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Warehouse_NearFuture_Geometry.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,mesh_smooth_type='FACE',use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
for ob in s.objects:ob.select_set(False)
for ob in c.objects:ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Warehouse_NearFuture_Collision.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
for ob in s.objects:ob.select_set(False)
c.hide_viewport=True
for l in s.view_layers:find(l.layer_collection,'14_COLLISION').exclude=True
deps=bpy.context.evaluated_depsgraph_get();tris=0
for ob in visual:
    ev=ob.evaluated_get(deps);me=ev.to_mesh();tris+=sum(len(p.vertices)-2 for p in me.polygons);ev.to_mesh_clear()
data=json.loads((ROOT/'Asset_manifest.json').read_text());data.update(export_visual_meshes=len(visual),evaluated_visual_triangles=tris,collision_meshes=len(c.objects));(ROOT/'Asset_manifest.json').write_text(json.dumps(data,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Warehouse_NearFuture.blend'))
print('EXPORT_DONE',len(visual),tris,len(c.objects),flush=True)
