import bpy,sys
from pathlib import Path
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'Warehouse_NearFuture.blend'))
s=bpy.context.scene
preview_folder=root/'Previews'/'Release';preview_folder.mkdir(parents=True,exist_ok=True)
try:
    pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
    for device in pref.devices:device.use=device.type!='CPU'
    s.cycles.device='GPU'
except Exception:pass
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['Overview']
for key in args:
    layer='02_CUTAWAY' if key=='Overview' else '03_GROUND_PLAN' if key=='FloorPlan' else '01_COMPLETE'
    for v in s.view_layers:v.use=v.name==layer
    bpy.context.window.view_layer=s.view_layers[layer]
    s.camera=bpy.data.objects['CAM_'+('Overview' if key=='Enclosed' else key)]
    s.render.resolution_x=1600;s.render.resolution_y=1100 if key in ['Overview','FloorPlan','Enclosed'] else 1000
    s.cycles.samples=64
    s.render.filepath=str(preview_folder/(key+'.png'))
    bpy.ops.render.render(write_still=True,layer=layer)
    print('RENDER_DONE',key,flush=True)
