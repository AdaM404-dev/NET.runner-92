import bpy,json
from mathutils import Matrix
from pathlib import Path
root=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Warehouse_NearFuture_Geometry.fbx'))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
tri=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in obs)
result={'fbx_roundtrip_import':'passed','mesh_objects':len(obs),'triangles':tri,'all_meshes_have_uv0':all(len(o.data.uv_layers)>0 for o in obs),'all_meshes_have_materials':all(len(o.data.materials)>0 for o in obs),'interactive_doors':sum(bool(o.get('interactive_type')) for o in obs),'missing_texture_files':[],'unity_runtime_tested':False}
skins=[o for o in obs if o.get('follows_operable_door')]
result['parented_door_skins']=len(skins)
result['all_door_skins_follow_operable_parent']=all(o.parent and o.parent.get('interactive_type') for o in skins)
closure=json.loads((root/'Door_closure_validation.json').read_text())
expected={d['name']:d for d in closure['closed_pose_checks']}
actual={o.name:o for o in obs if o.get('interactive_type')}
assert set(actual)==set(expected), 'Exported door names changed'
errors={}
for name,o in actual.items():
    matrix=Matrix(expected[name]['matrix_world'])
    error=max(abs(matrix[i][j]-o.matrix_world[i][j]) for i in range(4) for j in range(4))
    if error>1e-4 or o.get('initial_state')!='closed': errors[name]=error
result['all_119_exported_doors_closed']=not errors
result['door_pose_errors']=errors
result['loading_shutters_closed']=sum(o.get('interactive_type')=='sectional overhead' and o.get('initial_state')=='closed' for o in actual.values())
for im in bpy.data.images:
    if im.source=='FILE' and not Path(bpy.path.abspath(im.filepath)).exists():result['missing_texture_files'].append(im.filepath)
(root/'Export_validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result),flush=True)
assert len(obs)==1827 and tri==1086728
assert result['all_meshes_have_uv0'] and result['all_meshes_have_materials']
assert result['all_door_skins_follow_operable_parent'] and len(skins)==119
assert not result['missing_texture_files'] and not errors
