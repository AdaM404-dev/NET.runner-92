import bpy, json, math, time
from pathlib import Path
BASE=Path(__file__).resolve().parents[2]
OUT=BASE/'outputs'/'K7_Industrial_Robot';WORK=BASE/'work'
bpy.ops.wm.open_mainfile(filepath=str(WORK/'k7_source.blend'))
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.device='GPU'
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
originals=list(bpy.data.collections['LOD0'].objects)
bpy.ops.object.select_all(action='DESELECT')
copies=[]
for o in originals:
    c=o.copy();c.data=o.data.copy();scene.collection.objects.link(c);c.modifiers.clear();c.parent=None;c.select_set(True);copies.append(c);o.hide_render=True
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();bake=bpy.context.object;bake.name='BAKE_Surface'
tri=sum(len(p.vertices)-2 for p in bake.data.polygons)
print('BAKE_TRIANGLES',tri,flush=True)
mats=list(set(bake.data.materials))
scene.render.bake.use_clear=True;scene.render.bake.margin=10;scene.render.bake.margin_type='EXTEND';scene.render.bake.use_selected_to_active=False
results={}
for mapname,channel in [('BaseColor','Base Color'),('Metallic','Metallic'),('Roughness','Roughness'),('Normal','Normal'),('AO','AO')]:
    start=time.time();scene.cycles.samples=1 if mapname=='AO' else 8
    img=bpy.data.images.new('K7_'+mapname+'_4K',width=4096,height=4096,alpha=False,float_buffer=False)
    if mapname!='BaseColor':img.colorspace_settings.name='Non-Color'
    saved=[]
    for mat in mats:
        nt=mat.node_tree;n=nt.nodes;l=nt.links
        target=n.new('ShaderNodeTexImage');target.name='BAKE_TARGET';target.image=img;n.active=target
        out=next(x for x in n if x.type=='OUTPUT_MATERIAL')
        bs=next(x for x in n if x.type=='BSDF_PRINCIPLED')
        saved.append((mat,bs,out,target))
        if mapname in ('BaseColor','Metallic','Roughness'):
            emit=n.new('ShaderNodeEmission');emit.name='BAKE_EMISSION'
            inp=bs.inputs[channel]
            if inp.is_linked:l.new(inp.links[0].from_socket,emit.inputs[0])
            else:
                val=inp.default_value
                emit.inputs[0].default_value=(val,val,val,1) if isinstance(val,(int,float)) else val
            l.new(emit.outputs[0],out.inputs['Surface'])
        elif mapname=='AO':
            ao=n.new('ShaderNodeAmbientOcclusion');ao.name='BAKE_AO';ao.inputs['Distance'].default_value=.12;ao.samples=4
            emit=n.new('ShaderNodeEmission');emit.name='BAKE_EMISSION';l.new(ao.outputs['Color'],emit.inputs[0]);l.new(emit.outputs[0],out.inputs['Surface'])
    print('BAKING',mapname,flush=True)
    bpy.ops.object.bake(type='NORMAL' if mapname=='Normal' else 'EMIT')
    img.filepath_raw=str(OUT/'Textures'/'4K'/('K7_'+mapname+'.png'));img.file_format='PNG';img.save()
    results[mapname]={'seconds':round(time.time()-start,1),'path':img.filepath_raw}
    for mat,bs,out,target in saved:
        nt=mat.node_tree;nt.links.new(bs.outputs['BSDF'],out.inputs['Surface']);nt.nodes.remove(target)
        for name in ['BAKE_EMISSION','BAKE_AO']:
            if name in nt.nodes:nt.nodes.remove(nt.nodes[name])
    bpy.data.images.remove(img)
    print('BAKED',mapname,results[mapname]['seconds'],flush=True)
(WORK/'bake_results.json').write_text(json.dumps(results,indent=2))
print('BAKE_COMPLETE',flush=True)

