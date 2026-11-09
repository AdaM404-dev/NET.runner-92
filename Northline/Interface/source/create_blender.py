"""Rebuild a native editable Blender 2D source from layouts.json. No Unity access."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
DATA=json.loads((ROOT/'source'/'layouts.json').read_text(encoding='utf-8'))
METRICS=json.loads((ROOT/'source'/'text-metrics.json').read_text(encoding='utf-8')) if (ROOT/'source'/'text-metrics.json').exists() else {}
S=.01
MATERIALS={}
FONTS={}
for key,path in [('regular','C:/Windows/Fonts/segoeui.ttf'),('medium','C:/Windows/Fonts/seguisb.ttf'),
                 ('light','C:/Windows/Fonts/segoeuil.ttf'),('mono','C:/Windows/Fonts/consola.ttf')]:
    if Path(path).exists():FONTS[key]=bpy.data.fonts.load(path)


def linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
def rgba(hexcolor,alpha=1):return tuple(linear(int(hexcolor[i:i+2],16)/255) for i in (1,3,5))+(alpha,)


def material(color,opacity=1):
    key=(color,opacity)
    if key not in MATERIALS:
        m=bpy.data.materials.new(f'UI {color} / {opacity:g}')
        m.diffuse_color=rgba(color,opacity)
        m.use_nodes=True
        n=m.node_tree.nodes;n.clear()
        output=n.new('ShaderNodeOutputMaterial');emission=n.new('ShaderNodeEmission')
        emission.inputs['Color'].default_value=rgba(color)
        emission.inputs['Strength'].default_value=1
        if opacity<1:
            transparent=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader')
            mix.inputs[0].default_value=opacity
            m.node_tree.links.new(transparent.outputs[0],mix.inputs[1])
            m.node_tree.links.new(emission.outputs[0],mix.inputs[2])
            m.node_tree.links.new(mix.outputs[0],output.inputs[0])
            m.surface_render_method='DITHERED'
        else:m.node_tree.links.new(emission.outputs[0],output.inputs[0])
        MATERIALS[key]=m
    return MATERIALS[key]


def mesh(name,points,faces,color,collection,z,opacity=1):
    data=bpy.data.meshes.new(name)
    data.from_pydata([(x*S,(HEIGHT-y)*S,z) for x,y in points],[],faces)
    data.update()
    obj=bpy.data.objects.new(name,data);collection.objects.link(obj)
    obj.data.materials.append(material(color,opacity))
    return obj


def segment(name,a,b,width,color,collection,z,opacity=1):
    dx,dy=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dy)
    if length<1e-8:return
    nx,ny=-dy/length*width/2,dx/length*width/2
    return mesh(name,[(a[0]+nx,a[1]+ny),(b[0]+nx,b[1]+ny),(b[0]-nx,b[1]-ny),(a[0]-nx,a[1]-ny)],[(0,1,2,3)],color,collection,z,opacity)


def stroke(name,points,width,color,collection,z,opacity=1):
    for index,(a,b) in enumerate(zip(points,points[1:])):segment(name+f' / edge {index:02}',a,b,width,color,collection,z,opacity)


# Fresh source output only. Never opens or modifies the existing game's assets.
for scene in list(bpy.data.scenes):
    if len(bpy.data.scenes)>1:bpy.data.scenes.remove(scene)
scene=bpy.data.scenes[0]
for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)

screen_names=[n for n in DATA if n[:2].isdigit()]+['hud-overlay']
report=[]
for index,name in enumerate(screen_names):
    board=DATA[name];HEIGHT=board['height'];WIDTH=board['width']
    scene=bpy.data.scenes[0] if index==0 else bpy.data.scenes.new(name)
    scene.name=name
    bpy.context.window.scene=scene
    scene.render.engine='CYCLES';scene.cycles.samples=32
    scene.render.resolution_x=WIDTH;scene.render.resolution_y=HEIGHT;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.render.film_transparent=True
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    scene.world=bpy.data.worlds.new(name+' world');scene.world.color=(0,0,0)
    layers={};action='';counter=0;text_index=0
    for i in board['elements']:
        k=i['kind'];counter+=1;z=counter*.00006
        layer=i['layer']
        if layer not in layers:
            layers[layer]=bpy.data.collections.new(layer);scene.collection.children.link(layers[layer])
        col=layers[layer];title=i.get('id') or str(i.get('value',''))[:42] or k
        title=(action+' / ' if action else '')+title
        opacity=i.get('opacity',1)
        if k=='begin':action=i['action'];continue
        if k=='end':action='';continue
        if opacity==0:continue
        obj=None
        if k=='rect':
            x,y,w,h=i['x'],i['y'],i['w'],i['h'];points=[(x,y),(x+w,y),(x+w,y+h),(x,y+h),(x,y)]
            if i['fill']!='none':obj=mesh(title,points[:4],[(0,1,2,3)],i['fill'],col,z,opacity)
            if i.get('stroke'):stroke(title+' outline',points,1,i['stroke'],col,z+.00002,opacity)
        elif k=='line':segment(title,(i['x1'],i['y1']),(i['x2'],i['y2']),i['width'],i['stroke'],col,z,opacity)
        elif k=='poly':
            points=i['points']
            if i['fill']!='none':obj=mesh(title,points,[tuple(range(len(points)))],i['fill'],col,z,opacity)
            if i.get('stroke'):stroke(title+' outline',points,i['width'],i['stroke'],col,z+.00002,opacity)
        elif k=='circle':
            points=[(i['x']+i['r']*math.cos(a*math.tau/64),i['y']+i['r']*math.sin(a*math.tau/64)) for a in range(64)]
            if i['fill']!='none':obj=mesh(title,points,[tuple(range(64))],i['fill'],col,z,opacity)
            if i.get('stroke'):stroke(title+' outline',points+[points[0]],i['width'],i['stroke'],col,z+.00002,opacity)
        elif k=='text':
            data=bpy.data.curves.new(title,'FONT');data.body=i['value'];data.size=i['size']*S
            data.align_y='BOTTOM_BASELINE';data.align_x={'start':'LEFT','middle':'CENTER','end':'RIGHT'}[i.get('anchor','start')]
            key='mono' if i['mono'] else 'medium' if i['weight']>=500 else 'light' if i['weight']<400 else 'regular'
            if key in FONTS:data.font=FONTS[key]
            data.space_character=1+i.get('spacing',0)/max(i['size']*.5,1)
            obj=bpy.data.objects.new(title,data);col.objects.link(obj)
            obj.location=(i['x']*S,(HEIGHT-i['y'])*S,z)
            data.materials.append(material(i['fill'],opacity))
            # TrueType em sizing differs between browser SVG and Blender FONT curves.
            # Match measured ink height/width while retaining native editable text.
            metrics=METRICS.get(name,[])
            if text_index<len(metrics):
                measured=metrics[text_index]
                bpy.context.view_layer.update()
                local_top=max(v[1] for v in obj.bound_box)
                if local_top>1e-6 and measured['ascent']>0:
                    data.size*=measured['ascent']*S/local_top
                    bpy.context.view_layer.update()
                if obj.dimensions.x>1e-6 and measured['width']>0:
                    obj.scale.x*=measured['width']*S/obj.dimensions.x
            text_index+=1
        if obj:
            obj['source_layer']=layer
            if action:obj['prototype_action']=action
            if i.get('id'):obj['binding_id']=i['id']
    camera_data=bpy.data.cameras.new(name+' orthographic');camera_data.type='ORTHO';camera_data.ortho_scale=WIDTH*S
    camera=bpy.data.objects.new('UI CAMERA / 1920 × 1080',camera_data);scene.collection.objects.link(camera)
    camera.location=(WIDTH*S/2,HEIGHT*S/2,30);scene.camera=camera
    camera_data.lens=50;camera_data.clip_end=100
    scene['artboard_pixels']=f'{WIDTH} × {HEIGHT}'
    scene['purpose']='Editable standalone interface artwork. No Unity integration or game runtime.'
    scene.render.filepath=str(ROOT/'review'/f'{name}-blender.png')
    report.append(dict(scene=name,objects=len(scene.objects),layers=len(layers)))

bpy.context.window.scene=bpy.data.scenes['02-gameplay-hud']
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
# System fonts remain referenced instead of redistributing proprietary font binaries.
# SVG and JSON remain portable editable source if the target system uses other fonts.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'blender'/'NET-runner-92-interface.blend'))
(ROOT/'review'/'blender-source-audit.json').write_text(json.dumps(dict(scenes=report,fonts=list(FONTS),source='source/layouts.json'),indent=2),encoding='utf-8')
print(json.dumps(report))
