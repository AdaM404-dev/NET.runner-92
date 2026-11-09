"""Render the deliverable views and a Blender-only seven-action preview reel."""
import math
from pathlib import Path
import sys

import bpy
from mathutils import Matrix, Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"Renders"
OUT.mkdir(exist_ok=True)
V=Vector
s=bpy.context.scene
s.render.use_persistent_data=True
s.render.image_settings.file_format="PNG"
s.render.image_settings.color_mode="RGB"
s.render.resolution_percentage=100
s.cycles.adaptive_threshold=.024

try:
    prefs=bpy.context.preferences.addons["cycles"].preferences
    prefs.compute_device_type="OPTIX"
    prefs.refresh_devices()
    for d in prefs.devices: d.use=d.type=="OPTIX"
    if any(d.use for d in prefs.devices): s.cycles.device="GPU"
except (RuntimeError,TypeError):
    pass


def shot(name,frame):
    s.frame_set(frame)
    s.render.filepath=str(OUT/(name+".png"))
    bpy.ops.render.render(write_still=True)


def new_camera(name,at,target,ortho=None,up_axis=None,lens=61):
    cam=bpy.data.cameras.new(name)
    obj=bpy.data.objects.new(name,cam)
    s.collection.objects.link(obj)
    obj.location=at
    f=(V(target)-V(at)).normalized()
    if up_axis is None: obj.rotation_euler=f.to_track_quat("-Z","Y").to_euler()
    else:
        right=f.cross(up_axis).normalized()
        obj.rotation_euler=Matrix((right,right.cross(f),-f)).transposed().to_euler()
    cam.clip_start=.012
    cam.lens=lens
    if ortho:
        cam.type="ORTHO"
        cam.ortho_scale=ortho
    s.camera=obj
    return obj


def area(name,position,target,power,size,color):
    data=bpy.data.lights.new(name,"AREA")
    data.shape="DISK"
    data.energy=power
    data.size=size
    data.color=color
    ob=bpy.data.objects.new(name,data)
    s.collection.objects.link(ob)
    ob.location=position
    ob.rotation_euler=(V(target)-V(position)).to_track_quat("-Z","Y").to_euler()


def stills():
    s.render.resolution_x,s.render.resolution_y=1600,900
    s.cycles.samples=60
    shot("01_FirstPerson_Ready",121)
    shot("02_FirstPerson_Touch",233)
    shot("03_FirstPerson_Stowed",1)

    rig=bpy.data.objects["NR92_FP_Rig"]
    env=bpy.data.collections["Temporary hospital corridor and K-7 silhouette"]
    env.hide_render=True
    s.frame_set(121)
    dg=bpy.context.evaluated_depsgraph_get()
    display=bpy.data.objects["NR92_Display_Surface"].evaluated_get(dg)
    verts=[display.matrix_world@v.co for v in display.data.vertices]
    center=sum(verts,V())*.25
    u,v=(verts[1]-verts[0]).normalized(),(verts[3]-verts[0]).normalized()
    n=u.cross(v)
    new_camera("Tablet optical inspection",center+n*.397-u*.119+v*.087,center-n*.004,up_axis=v,lens=47)
    area("Macro edge",center+n*.25-u*.13+v*.27,center,12,.22,(.97,.97,1))
    area("Macro kicker",center-n*.01+u*.12-v*.22,center,8,.21,(.43,.74,1))
    shot("04_LATCH07_Closeup",121)
    env.hide_render=False

    bpy.data.collections["PreviewEnvironment"].hide_render=True
    rig.animation_data.use_nla=False
    rig.animation_data.action=None
    for pb in rig.pose.bones: pb.matrix_basis=Matrix.Identity(4)
    s.frame_set(1)
    s.world.node_tree.nodes["Background"].inputs[0].default_value=(.081,.103,.124,1)
    s.world.node_tree.nodes["Background"].inputs[1].default_value=.46
    s.render.resolution_x,s.render.resolution_y=1440,960
    look=(0,0,-.398)
    area("Model review key",(-.65,1.1,.50),look,125,1.03,(.86,.93,1))
    area("Model review contour",(.84,-.52,.24),look,122,.94,(.64,.78,1))
    area("Model review fill",(.44,1.01,-.38),look,60,.79,(1,.76,.55))
    new_camera("Front orthographic",(0,-2.2,-.398),look,ortho=1.41)
    shot("05_Model_Front",1)
    bpy.data.collections["RightArm"].hide_render=True
    for ob in bpy.data.collections["Clothing"].objects:
        if ob.name.startswith("FP_Right_"): ob.hide_render=True
    new_camera("Left profile",(2.1,.0,-.408),(0,0,-.408),ortho=1.18)
    shot("06_Model_Side",1)


def overlay(label,at,size,material):
    font=bpy.data.curves.new("Preview reel caption","FONT")
    font.body=label
    font.size=size
    font.space_character=1.14
    o=bpy.data.objects.new("Preview reel caption",font)
    s.collection.objects.link(o)
    o.rotation_euler.x=math.pi/2
    o.location=at
    font.materials.append(material)
    return font


def reel():
    rig=bpy.data.objects["NR92_FP_Rig"]
    anim=rig.animation_data
    for track in list(anim.nla_tracks): anim.nla_tracks.remove(track)
    sequence=("Hack_Equip","Hack_Idle","Hack_Interact","Hack_Success","Hack_Unequip",
              "Hack_Equip","Hack_Interact","Hack_Failed","Hack_Cancel")
    cuts=[]
    at=1
    for name in sequence:
        action=bpy.data.actions[name]
        track=anim.nla_tracks.new()
        strip=track.strips.new(name,at,action)
        if action.slots: strip.action_slot=action.slots[0]
        strip.extrapolation="NOTHING"
        cuts.append((at,name))
        at+=round(action.frame_range[1]-action.frame_range[0])
    overlay("NET.RUNNER-92   /   LATCH-07",(-.044,.062,.0238),.0016,bpy.data.materials["Preview_Phosphor"])
    caption=overlay("",(-.044,.062,.0207),.00138,bpy.data.materials["Preview_Phosphor"])

    def label(scene,*_):
        name=next(name for frame,name in reversed(cuts) if frame<=scene.frame_current)
        caption.body=name.upper().replace("_"," / ")

    bpy.app.handlers.frame_change_pre.append(label)
    s.frame_start,s.frame_end,s.frame_step=1,at,3
    s.render.fps=20
    s.render.resolution_x,s.render.resolution_y=640,360
    s.cycles.samples=9
    s.cycles.adaptive_threshold=.08
    s.render.image_settings.media_type="VIDEO"
    s.render.ffmpeg.format="MPEG4"
    s.render.ffmpeg.codec="H264"
    s.render.ffmpeg.constant_rate_factor="HIGH"
    s.render.ffmpeg.audio_codec="NONE"
    s.render.filepath=str(OUT/"07_Animation_Preview_")
    s.frame_set(1)
    bpy.ops.render.render(animation=True)
    path=next(OUT.glob("07_Animation_Preview_*.mp4"))
    path.replace(OUT/"07_Animation_Preview.mp4")


if sys.argv[-1]=="reel":
    reel()
else:
    stills()
