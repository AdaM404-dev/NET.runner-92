"""Read and render the saved Blender source without saving it again."""
from pathlib import Path
import bpy,json
ROOT=Path(__file__).resolve().parents[1]
results=[]
expected=['01-main-menu','02-gameplay-hud','03-pause','04-settings','05-hacking','06-mission-map','hud-overlay']
for name in expected:
    scene=bpy.data.scenes[name]
    texts=[obj for obj in scene.objects if obj.type=='FONT']
    results.append(dict(scene=name,camera=bool(scene.camera),orthographic=scene.camera.data.type=='ORTHO',
                        resolution=[scene.render.resolution_x,scene.render.resolution_y],
                        textObjects=len(texts),allTextEditable=all(obj.data.body for obj in texts)))
for name in expected[:2]:
    scene=bpy.data.scenes[name];bpy.context.window.scene=scene
    scene.render.resolution_percentage=50;scene.cycles.samples=16
    scene.render.filepath=str(ROOT/'review'/(name+'-blender-source.png'))
    bpy.ops.render.render(write_still=True)
report=dict(passed=all(r['camera'] and r['orthographic'] and r['allTextEditable'] and r['resolution']==[1920,1080] for r in results),scenes=results)
(ROOT/'review'/'blender-saved-checks.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
assert report['passed']
print('Saved Blender source: 7 editable orthographic scenes verified, 2 rendered.')
