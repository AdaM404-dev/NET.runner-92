"""Pose authoring, preview stage and FBX handoff for the LATCH-07 viewmodel."""
import json
import math

import bpy
from mathutils import Matrix, Quaternion

import assembly as a
from surfaces import plain

V, Y, Z = a.V, a.Y, a.Z


def smooth(x):
    t = min(max(x, 0), 1)
    return t*t*(3-2*t)


def rise(t, start, end):
    return smooth((t-start)/(end-start))


def transform(position, rotation):
    m = rotation.to_4x4()
    m.translation = position
    return m


def local_pose(name, desired, pose):
    p = a.rig.pose.bones[name]
    if p.parent:
        parent = p.parent.name
        m = p.bone.matrix_local.inverted() @ p.parent.bone.matrix_local @ pose[parent].inverted() @ desired
    else:
        m = p.bone.matrix_local.inverted() @ desired
    p.location, p.rotation_quaternion, _ = m.decompose()
    pose[name] = desired


def child_pose(name, matrix, pose):
    p = a.rig.pose.bones[name]
    parent = p.parent.name
    rest = p.parent.bone.matrix_local.inverted() @ p.bone.matrix_local
    local_pose(name, pose[parent] @ rest @ matrix, pose)


def align_bone(name, position, axis, dorsal):
    original = a.rest[name][0].to_3x3()
    original_axis = original @ Y
    r = a.basis(axis,dorsal) @ a.basis(original_axis,V((0,1,0))).inverted()
    return transform(position, r @ original)


def arm_pose(side, shoulder, wrist, hint, hand_direction, dorsal, curl, pose):
    clav, upper, lower, hand = [side+s for s in ("Shoulder","UpperArm","LowerArm","Hand")]
    upper_length, lower_length = a.rest[upper][1], a.rest[lower][1]
    d = wrist-shoulder
    direction = d.normalized()
    length = min(d.length,upper_length+lower_length-.001)
    wrist = shoulder + direction*length
    forward = (length*length+upper_length*upper_length-lower_length*lower_length)/(2*length)
    height = math.sqrt(max(0.,upper_length**2-forward**2))
    pole = hint-shoulder
    pole = (pole-direction*pole.dot(direction)).normalized()
    elbow = shoulder + direction*forward + pole*height
    clav_matrix = a.rest[clav][0].copy()
    clav_matrix.translation += shoulder-a.rest[upper][0].translation
    local_pose(clav,clav_matrix,pose)
    local_pose(upper,align_bone(upper,shoulder,elbow-shoulder,V((0,-.83,.55))),pose)
    local_pose(lower,align_bone(lower,elbow,wrist-elbow,dorsal),pose)
    local_pose(hand,align_bone(hand,wrist,hand_direction,dorsal),pose)
    for i,finger in enumerate(a.FINGERS):
        for j,joint in enumerate(a.JOINTS):
            name=side+finger+joint
            m=a.rest[name][0].to_3x3()
            axis=(m@Y).cross(V((0,-1,0))).normalized()
            local=m.inverted()@axis
            child_pose(name,Quaternion(local,math.radians(curl[i][j])).to_matrix().to_4x4(),pose)


def skin_samples(points, pose, side):
    bind = {name:m@a.rig.data.bones[name].matrix_local.inverted() for name,m in pose.items() if name.startswith(side)}
    return [sum((bind[n]@co*w for n,w in weights.items()),V()) for co,weights in points]


def gesture(clip,t):
    equip, deploy, activity, accent, side = 1, 1, 0, 0, 0
    if clip=="Hack_Equip":
        equip = rise(t,.0,.91)
        deploy = rise(t,.59,.97)
    elif clip=="Hack_Unequip":
        equip = 1-rise(t,.05,1)
        deploy = 1-rise(t,.01,.44)
    elif clip=="Hack_Cancel":
        equip = 1-rise(t,.00,1)
        deploy = 1-rise(t,.00,.60)
        accent = -math.sin(math.pi*t)*.25
    elif clip=="Hack_Interact":
        activity = rise(t,.03,.40)-rise(t,.52,.96)
    elif clip=="Hack_Success":
        accent = math.sin(math.pi*t)**2*.90
        side = math.sin(math.pi*t)**2*.018
    elif clip=="Hack_Failed":
        accent = -(rise(t,.06,.26)-rise(t,.42,.95))*.90
        side = -math.sin(math.pi*t)**2*.015
        deploy = 1-(rise(t,.07,.24)-rise(t,.33,.72))*.53
    breathe = math.sin(t*math.tau) if clip=="Hack_Idle" else math.sin(t*math.pi)**2*.25
    return equip, deploy, activity, accent, side, breathe


def author_pose(clip,t,target=(-.018,-.011)):
    e, deployment, tap, accent, drift, breath = gesture(clip,t)
    pose = {"FP_Root":a.rig.data.bones["FP_Root"].matrix_local.copy()}
    left_shoulder, right_shoulder = V((-.181,-.078,-.205)),V((.195,-.078,-.226))
    left_rest=V((-.33,.263,-.603))
    left_ready=V((.047+drift,.387+breath*.0018,-.061+breath*.0019+accent*.009))
    down,raised=left_rest-left_shoulder,left_ready-left_shoulder
    left_wrist=left_shoulder+down.normalized().slerp(raised.normalized(),e)*(down.length*(1-e)+raised.length*e)
    left_hint=V((-.27,.12,-.53)).lerp(V((-.23,.261,-.118)),e)
    dorsal=V((-.94,-.22,-.27)).lerp(V((.02,-.80,.60)),e)
    left_axis=V((.88,.37,-.08))
    relaxed=((23,22,12),(26,27,14),(31,30,17),(37,31,17),(16,15,12))
    arm_pose("Left",left_shoulder,left_wrist,left_hint,
             V((-.20,.21,-.82)).lerp(left_axis,e),dorsal,relaxed,pose)
    child_pose("Device_Mount",Matrix.Identity(4),pose)
    child_pose("Device_Display",Matrix.Translation(Z*(deployment*.0046)),pose)
    screen=pose["Device_Display"]
    n=screen.to_3x3()@Z
    ready_d=V((-.92,.10,-.38)).normalized()
    waiting_d=V((-.28,.57,-.46)).normalized()
    d=V((.27,.18,-.82)).lerp(waiting_d.lerp(ready_d,tap),e).normalized()
    right_dorsal=Quaternion(d,math.radians(8*tap))@V((0,-.68,.74)).lerp(n,e)
    stowed=V((.31,.183,-.49))
    hint=V((.40,.14,-.52)).lerp(V((.38,.19,-.36)),e)
    waiting=V((.261+.026*abs(accent),.328+breath*.001,-.312+accent*.018))
    desired = screen@V((.045*(1-tap)+target[0]*tap, -.025*(1-tap)+target[1]*tap, a.GLASS+.001+.079*(1-tap)))
    wrist=stowed.lerp(waiting.lerp(desired-d*.168,tap),e)
    curl=((22-12*tap,25-10*tap,12-5*tap),(48,45,31),(53,50,34),(56,49,37),(23,29,18))
    arm_pose("Right",right_shoulder,wrist,hint,d,right_dorsal,curl,pose)
    if tap>.001:
        pad=min(skin_samples(pad_samples,pose,"Right"),key=lambda q:(q-desired).dot(n))
        wrist += (desired-pad)*tap
        arm_pose("Right",right_shoulder,wrist,hint,d,right_dorsal,curl,pose)
        # Keep non-index glove geometry out of the panel's rectangle while preserving fingertip travel.
        inv=screen.inverted()
        near=[inv@q for q in skin_samples(right_hand_samples,pose,"Right")]
        in_plane=[p.z for p in near if abs(p.x)<a.W*.5 and abs(p.y)<a.H*.5]
        if in_plane and min(in_plane)<a.GLASS+.0006:
            wrist+=n*(a.GLASS+.0006-min(in_plane))
            arm_pose("Right",right_shoulder,wrist,hint,d,right_dorsal,curl,pose)
    return pose


def actions():
    global pad_samples,right_hand_samples
    _,_,coords,weights=a.arms["Right"]
    bone=a.rest["RightIndexDistal"][0]
    axis=bone.to_3x3()@Y
    right_hand_samples=[(p,w) for p,w in zip(coords,weights) if any("Hand" in k or "Arm" not in k and "Shoulder" not in k for k in w)]
    pad_samples=[(p,w) for p,w in right_hand_samples if w.get("RightIndexDistal",0)>.54 and
                 (p-bone.translation).dot(axis)>a.rest["RightIndexDistal"][1]*.55]
    if len(pad_samples)<4: raise RuntimeError("Index finger contact sample not found")
    rig=a.rig
    anim=rig.animation_data_create()
    for name,(start,end,loop) in a.CLIPS.items():
        action=bpy.data.actions.new(name)
        action.use_fake_user=True
        action["loop"]=loop
        anim.action=action
        previous={}
        for frame in range(end-start+1):
            author_pose(name,frame/(end-start))
            for p in rig.pose.bones:
                q=p.rotation_quaternion.copy()
                if p.name in previous: q.make_compatible(previous[p.name])
                p.rotation_quaternion=q
                previous[p.name]=q
                p.keyframe_insert("rotation_quaternion",frame=frame+1,group=p.name)
                p.keyframe_insert("location",frame=frame+1,group=p.name)
        strip_slot=anim.action_slot
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        for key in curve.keyframe_points: key.interpolation="LINEAR"
        anim.action=None
        track=anim.nla_tracks.new()
        track.name=name
        strip=track.strips.new(name,start,action)
        strip.action_slot=strip_slot
        strip.extrapolation="NOTHING"
        a.scene.timeline_markers.new(name,frame=start)
    for name in ("FP_Root","Device_Mount","Device_Display"):
        a.rig.pose.bones[name]["purpose"] = {
            "FP_Root":"Mount at first-person camera origin",
            "Device_Mount":"Rigid on left forearm",
            "Device_Display":"Display, carrier, and preview interface. Translation only."
        }[name]


def reach_samples():
    rows=[]
    for target in ((-.050,-.014),(-.018,-.011),(.039,-.014)):
        pose=author_pose("Hack_Interact",.46,target)
        inv=pose["Device_Display"].inverted()
        near=[inv@q for q in skin_samples(pad_samples,pose,"Right")]
        pad=min(near,key=lambda p:abs(p.x-target[0])+abs(p.y-target[1])+abs(p.z-a.GLASS))
        rows.append({"target_uv":[round(target[0]/a.W+.5,5),round(target[1]/a.H+.5,5)],
                     "sampled_pad_uv":[round(pad.x/a.W+.5,5),round(pad.y/a.H+.5,5)],
                     "sampled_pad_gap_mm":round((pad.z-a.GLASS)*1000,3)})
    folder=a.ROOT/"Validation"
    folder.mkdir(exist_ok=True)
    (folder/"reach_targets.json").write_text(json.dumps(rows,indent=2),encoding="utf-8")


def camera(name, location, target, coll, lens=None, ortho=None):
    data=bpy.data.cameras.new(name)
    ob=bpy.data.objects.new(name,data)
    coll.objects.link(ob)
    ob.location=location
    ob.rotation_euler=(V(target)-V(location)).to_track_quat("-Z","Y").to_euler()
    data.clip_start=.016
    data.clip_end=35
    if ortho:
        data.type="ORTHO"
        data.ortho_scale=ortho
    else:
        data.sensor_fit="HORIZONTAL"
        data.sensor_width=36
        data.lens=lens or 36/(2*math.tan(math.radians(80)/2))
    return ob


def simple_box(name, at, dimensions, material, coll, bevel=.009):
    w,d,h=dimensions
    return a.poly_box(name,a.rectangle(at[0],at[1],w,d),at[2]-h/2,at[2]+h/2,coll,material,bevel)


def area(name, at, target, energy, size, color, coll):
    d=bpy.data.lights.new(name,"AREA")
    d.energy=energy
    d.shape="DISK"
    d.size=size
    d.color=color
    ob=bpy.data.objects.new(name,d)
    coll.objects.link(ob)
    ob.location=at
    ob.rotation_euler=(V(target)-V(at)).to_track_quat("-Z","Y").to_euler()
    return ob


def text_device(name, label, at, size, mat, coll, bone, export=False):
    font=bpy.data.curves.new(name,"FONT")
    font.body=label
    font.size=size
    font.space_character=1.12
    ob=bpy.data.objects.new(name,font)
    coll.objects.link(ob)
    font.materials.append(mat)
    ob.matrix_world=a.device_m@Matrix.Translation(V(at))
    for x in bpy.context.selected_objects: x.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.convert(target="MESH")
    ob=bpy.context.object
    ob.data.transform(ob.matrix_world)
    ob.matrix_world=Matrix.Identity(4)
    a.bind(ob,[{bone:1.0} for _ in ob.data.vertices])
    if not export: a.assets.remove(ob)
    ob.select_set(False)
    return ob


def interface(preview):
    text_device("Engraved_Product_Mark","LATCH // 07",(-.032,.0423,.0236),.0047,a.mats["Label"],
                a.collections["MainHousing"],"Device_Mount",True)
    text_device("Engraved_Service_Mark","SECURE BUS",(.027,-.0434,.0219),.0033,a.mats["Label"],
                a.collections["MainHousing"],"Device_Mount",True)
    printouts = (
        ("Header","NORTHLINE  /  SERVICE LINK",-.067,.0285,.0053,"UI_Dim"),
        ("Title","NET.runner-92",-.067,.009,.0130,"UI"),
        ("Subtitle","SYSTEM READY",-.066,-.006,.0063,"UI"),
        ("Footer","SIGNAL INTERFACE ONLINE",-.066,-.0307,.0050,"UI_Dim"),
        ("Index","07:42",.045,-.0307,.0045,"UI_Dim"),
    )
    for key,label,x,y,size,mat in printouts:
        text_device("Preview_"+key,label,(x,y,a.GLASS+.00025),size,a.mats[mat],preview,"Device_Display")
    for i in range(11):
        height=.003+(.008*math.sin(i*.92))**2*110
        o=a.poly_box("Preview_Signal",a.rectangle(.013+i*.0047,-.014+height*.5,.0023,height),a.GLASS+.0002,a.GLASS+.00035,
                     preview,a.mats["UI_Dim"],0,a.device_m,"Device_Display")
        a.assets.remove(o)
    for y in (-.020,.023):
        o=a.poly_box("Preview_Rule",a.rectangle(0,y,.132,.00056),a.GLASS+.0002,a.GLASS+.00035,preview,
                     a.mats["UI_Dim"],0,a.device_m,"Device_Display")
        a.assets.remove(o)


def corridor(parent):
    env=a.collection("Temporary hospital corridor and K-7 silhouette",parent)
    metal=plain("Preview Hospital Graphite",(.055,.077,.087),.46,.36)
    wall=plain("Preview Hospital Panels",(.115,.152,.164),.53,.08)
    edge=plain("Preview Hospital Trims",(.026,.044,.049),.37,.46)
    lit=plain("Preview Panel Lamp",(.58,.84,1),.42,0,3.4)
    hazard=plain("Preview Amber Guide",(.63,.18,.035),.49,.1,.27)
    simple_box("Corridor Floor",(0,5.2,-1.68),(4.35,14,.12),metal,env)
    simple_box("Corridor Ceiling",(0,5.2,1.29),(4.35,14,.10),edge,env)
    for sign in (-1,1):
        for i in range(8):
            y=-.2+i*1.40
            simple_box("Wall Infill",(sign*2.07,y,-.20),(.19,1.38,2.84),wall,env)
            simple_box("Wall Wainscot",(sign*1.95,y,-1.19),(.080,1.37,.79),metal,env)
            simple_box("Column Rib",(sign*1.93,y-.67,-.22),(.18,.10,2.87),edge,env)
            simple_box("Guide Strip",(sign*1.895,y,-.75),(.013,1.19,.022),hazard,env,.003)
    for i in range(5):
        y=.95+i*2.0
        simple_box("Ceiling Light",(0,y,1.205),(1.03,.085,.034),lit,env,.012)
        area("Ceiling Bounce",(0,y,1.15),(0,y,-1.2),72,1.0,(.53,.71,1),env)
    for sign in (-1,1):
        simple_box("Far Portal Upright",(sign*.90,8.2,-.20),(.20,.28,2.9),edge,env,.03)
        simple_box("Far Portal Marker",(sign*.79,8.13,-.13),(.022,.075,2.25),lit,env,.008)
    simple_box("Far Door",(0,9.2,-.12),(3.8,.11,2.94),edge,env)
    area("Far threshold backlight",(0,8.85,.35),(0,7.4,-.48),190,1.5,(.53,.84,1),env)
    # Human-scale temporary silhouette. Asset exports select only the rig and skinned parts.
    bot=plain("Preview K-7 silhouette",(.023,.034,.039),.53,.44)
    x,y=-1.38,4.25
    for name,at,size in (
        ("Optic head",(0,0,.257),(.28,.20,.24)),
        ("Thorax",(0,0,-.085),(.48,.23,.40)),
        ("Pelvis",(0,0,-.49),(.31,.21,.24)),
        ("Spine",(0,0,-.36),(.16,.16,.22)),
    ):
        simple_box("K-7 "+name,(x+at[0],y+at[1],at[2]),size,bot,env,.036)
    for sign in (-1,1):
        for name,dx,z,w,h in (
            ("Shoulder",.30,-.04,.18,.20),("Forearm",.37,-.39,.114,.40),("Thigh",.116,-.82,.14,.49),
            ("Shin",.12,-1.26,.12,.44),("Foot",.12,-1.55,.15,.12)
        ):
            simple_box("K-7 "+name,(x+sign*dx,y,z),(w,.16,h),bot,env,.025)
        simple_box("K-7 sensor",(x+sign*.062,y-.105,.273),(.086,.013,.014),hazard,env,.004)
    return env


def setup_preview():
    c=a.collections["PreviewCamera"]
    envroot=a.collections["PreviewEnvironment"]
    fp=camera("FP_Camera_80deg",(0,0,0),(0,1,0),c)
    a.scene.camera=fp
    ui=a.collection("Blender-only boot screen",envroot)
    interface(ui)
    env=corridor(envroot)
    lamps=a.collection("Viewmodel lighting",envroot)
    area("Key / neutral softbox",(-.58,.26,.57),(-.05,.29,-.17),31,.46,(.89,.95,1.0),lamps)
    area("Fill / overhead practical",(.59,.58,.32),(0,.30,-.20),21,.53,(.58,.75,1.0),lamps)
    area("Rim / warm reflection",(-.45,.78,.04),(-.1,.25,-.16),13,.42,(1.0,.65,.39),lamps)
    area("Bounce / fabric recovery",(.04,.10,-.55),(0,.27,-.15),3.5,.39,(.67,.81,1),lamps)
    world=bpy.data.worlds.new("NR92 Studio Ambient")
    world.use_nodes=True
    world.node_tree.nodes["Background"].inputs[0].default_value=(.105,.153,.182,1)
    world.node_tree.nodes["Background"].inputs[1].default_value=.23
    a.scene.world=world
    a.scene.render.engine="CYCLES"
    a.scene.cycles.samples=32
    a.scene.cycles.use_denoising=True
    a.scene.cycles.max_bounces=5
    a.scene.view_settings.view_transform="AgX"
    a.scene.view_settings.look="AgX - Medium High Contrast"
    return fp,env,ui


def export_assembly(parts,name,coll):
    copies=[]
    for o in bpy.context.selected_objects: o.select_set(False)
    for part in parts:
        ob=part.copy()
        ob.data=part.data.copy()
        coll.objects.link(ob)
        for mod in list(ob.modifiers):
            if mod.type=="ARMATURE": ob.modifiers.remove(mod)
        ob.select_set(True)
        copies.append(ob)
    bpy.context.view_layer.objects.active=copies[0]
    bpy.ops.object.convert(target="MESH")
    if len(copies)>1: bpy.ops.object.join()
    result=bpy.context.object
    result.name=name
    arm=result.modifiers.new("Armature deformation","ARMATURE")
    arm.object=a.rig
    return result


def selected_export():
    stage=a.collection("Transient FBX mesh consolidation",a.scene.collection)
    screen=bpy.data.objects["NR92_Display_Surface"]
    assemblies=[]
    for label,parts in (
        ("FP_Clothing_Left",[o for o in a.collections["Clothing"].objects if o.name.startswith("FP_Left_")]),
        ("FP_Clothing_Right",[o for o in a.collections["Clothing"].objects if o.name.startswith("FP_Right_")]),
        ("LATCH_07_Housing",list(a.collections["MainHousing"].objects)),
        ("LATCH_07_Trim",list(a.collections["MechanicalParts"].objects)),
        ("LATCH_07_Indicators",list(a.collections["IndicatorLights"].objects)),
        ("LATCH_07_Carrier",[o for o in a.collections["Screen"].objects if o != screen]),
    ):
        assemblies.append(export_assembly(parts,label,stage))
    for o in bpy.context.selected_objects: o.select_set(False)
    for o in [a.rig,screen,*[value[0] for value in a.arms.values()],*assemblies]: o.select_set(True)
    bpy.context.view_layer.objects.active=a.rig
    a.scene.frame_set(1)
    bpy.ops.export_scene.fbx(filepath=str(a.ROOT/"NETRunner_FP_Arms.fbx"),use_selection=True,
        object_types={"ARMATURE","MESH"},axis_forward="-Z",axis_up="Y",global_scale=1,
        apply_unit_scale=True,apply_scale_options="FBX_SCALE_UNITS",add_leaf_bones=False,
        use_armature_deform_only=True,bake_anim=True,bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,
        bake_anim_step=1,bake_anim_simplify_factor=0.0,use_mesh_modifiers=True,path_mode="COPY",embed_textures=True)
    for o in list(stage.objects): bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(stage)


def finish():
    actions()
    reach_samples()
    fp,env,ui=setup_preview()
    # Scene-level metadata keeps the player, rig, and UI anchor independent.
    a.scene["delivery"]="NET.runner-92 / LATCH-07 first-person viewmodel"
    a.scene["camera_contract"]="80 degree horizontal, +Y forward, +Z up, metres"
    selected_export()
    a.scene.frame_set(121)
    for ob in bpy.context.selected_objects: ob.select_set(False)
    for screen in bpy.data.screens:
        for area_ in screen.areas:
            if area_.type=="VIEW_3D":
                space=area_.spaces.active
                space.region_3d.view_perspective="CAMERA"
                space.overlay.show_overlays=False
                space.shading.type="MATERIAL"
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(a.ROOT/"NETRunner_FP_Arms.blend"),compress=True)
