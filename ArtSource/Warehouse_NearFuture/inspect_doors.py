"""Read-only inventory of the warehouse's operable doors."""
import bpy, json
from pathlib import Path

root = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root / 'Warehouse_NearFuture.blend'))
doors = [o for o in bpy.context.scene.objects if o.get('interactive_type')]
data = []
for o in sorted(doors, key=lambda o: o.name):
    data.append({'name': o.name, 'properties': dict(o.items()),
                 'location': list(o.location), 'rotation': list(o.rotation_euler),
                 'bounds': [list(v) for v in o.bound_box],
                 'children': [c.name for c in o.children]})
(root / 'Door_inventory_before.json').write_text(json.dumps(data, indent=2))
print('DOOR_INVENTORY', len(data), json.dumps(data[:2]), flush=True)
print('LOADING', json.dumps([d for d in data if 'Loading' in d['name']]), flush=True)
