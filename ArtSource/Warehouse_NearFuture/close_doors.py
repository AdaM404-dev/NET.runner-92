"""Close all operable warehouse doors without changing their geometry.

Run before export_unity.py, including after rebuilding the retrofit.
The untouched original building is kept in Source/Original_Warehouse.blend.
"""
import bpy, hashlib, json, math, struct
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Warehouse_NearFuture.blend'))
scene = bpy.context.scene
doors = sorted((o for o in scene.objects if o.get('interactive_type')), key=lambda o: o.name)
assert len(doors) == 119, f'Expected 119 doors, found {len(doors)}'

def geometry_digest(o):
    h = hashlib.sha256()
    for v in o.data.vertices:
        h.update(struct.pack('<fff', *v.co))
    for p in o.data.polygons:
        h.update(struct.pack('<I', len(p.vertices)))
        h.update(struct.pack('<' + 'I' * len(p.vertices), *p.vertices))
    return h.hexdigest()

def matrix_delta(a, b):
    return max(abs(a[i][j] - b[i][j]) for i in range(4) for j in range(4))

before_geometry = {o.name: geometry_digest(o) for o in scene.objects if o.type == 'MESH'}
before_world = {o.name: o.matrix_world.copy() for o in scene.objects}
before_basis = {o.name: o.matrix_basis.copy() for o in scene.objects}
before_parent_inverse = {o.name: o.matrix_parent_inverse.copy() for o in scene.objects}
door_names = {o.name for o in doors}
skin_names = {c.name for o in doors for c in o.children if c.get('follows_operable_door')}
changed = []
for o in doors:
    assert not o.animation_data and not o.constraints, f'Door is driven: {o.name}'
    assert not o.parent, f'Unexpected parent on door: {o.name}'
    prior = o.rotation_euler.z
    if o.get('interactive_type') == 'hinged door':
        assert 'closed_rotation_z' in o, f'No closed pose: {o.name}'
        o.rotation_euler.z = o['closed_rotation_z']
        delta = (prior - o.rotation_euler.z + math.pi) % (2 * math.pi) - math.pi
        if abs(delta) > 1e-5:
            changed.append({'name': o.name, 'previous_angle_from_closed_degrees': math.degrees(delta)})
    elif o.get('interactive_type') == 'sectional overhead':
        # These meshes are built with their lower edge at Z=0, and unraised origins.
        assert o.name.startswith('DOOR_Loading_') and abs(o.location.z) < 1e-5
        o.location.z = 0.0
    else:
        raise AssertionError(f'Unknown door type: {o.name}')
    o['initial_state'] = 'closed'

bpy.context.view_layer.update()
assert before_geometry == {o.name: geometry_digest(o) for o in scene.objects if o.type == 'MESH'}
unchanged_objects = [o for o in scene.objects if o.name not in door_names | skin_names]
assert all(matrix_delta(before_world[o.name], o.matrix_world) < 1e-6 for o in unchanged_objects)
assert all(matrix_delta(before_basis[n], bpy.data.objects[n].matrix_basis) < 1e-6
           and matrix_delta(before_parent_inverse[n], bpy.data.objects[n].matrix_parent_inverse) < 1e-6
           for n in skin_names)

checks = []
for o in doors:
    corners = [o.matrix_world @ Vector(v) for v in o.bound_box]
    if o.get('interactive_type') == 'hinged door':
        frame = bpy.data.objects[o.name + '_Frame']
        frame_pts = [frame.matrix_world @ Vector(v) for v in frame.bound_box]
        # The leaf's horizontal local X axis must align with the frame's long axis.
        width_x = max(p.x for p in frame_pts) - min(p.x for p in frame_pts)
        width_y = max(p.y for p in frame_pts) - min(p.y for p in frame_pts)
        along_x = width_x >= width_y
        axis = o.matrix_world.to_3x3() @ Vector((1, 0, 0))
        off_wall = abs(axis.y if along_x else axis.x)
        assert off_wall < 1e-5, f'Leaf not aligned with frame: {o.name}'
        index = 0 if along_x else 1
        lo, hi = min(p[index] for p in corners), max(p[index] for p in corners)
        flo, fhi = min(p[index] for p in frame_pts), max(p[index] for p in frame_pts)
        assert flo - .02 <= lo <= hi <= fhi + .02, f'Leaf outside frame: {o.name}'
    else:
        assert abs(min(p.z for p in corners)) < .02, f'Raised shutter: {o.name}'
    assert len([c for c in o.children if c.get('follows_operable_door')]) == 1
    checks.append({'name': o.name, 'type': o['interactive_type'], 'closed': True,
                   'matrix_world': [list(row) for row in o.matrix_world]})

report = {'all_doors_closed': True, 'doors': len(doors), 'hinged_doors': 113,
          'loading_shutters': 6, 'previously_open_doors_corrected': len(changed),
          'changed_doors': changed, 'all_mesh_geometry_unchanged': True,
          'non_door_world_transforms_unchanged': True, 'door_skin_local_transforms_unchanged': True,
          'door_skin_parents_preserved': len(skin_names), 'closed_pose_checks': checks,
          'blender_version': bpy.app.version_string, 'unity_runtime_tested': False}
(ROOT / 'Door_closure_validation.json').write_text(json.dumps(report, indent=2))
manifest = json.loads((ROOT / 'Asset_manifest.json').read_text())
manifest['door_initial_state'] = 'closed'
manifest['door_closure'] = {k: v for k, v in report.items() if k not in ('closed_pose_checks', 'changed_doors')}
(ROOT / 'Asset_manifest.json').write_text(json.dumps(manifest, indent=2))
scene['door_initial_state'] = 'All 119 doors closed; see Door_closure_validation.json'
scene['door_closure_revision'] = '2026-10-02'
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Warehouse_NearFuture.blend'))
print('DOORS_CLOSED', len(doors), 'CORRECTED', len(changed), flush=True)
