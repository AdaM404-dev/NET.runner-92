"""Build the standalone Blender viewmodel from the existing operative arm topology."""
import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
from surfaces import make_materials, plain

V = Vector
Z = V((0, 0, 1))
Y = V((0, 1, 0))
OFFSET = V((0, 0, -1.55))
SIDES = ("Left", "Right")
FINGERS = ("Index", "Middle", "Ring", "Little", "Thumb")
JOINTS = ("Proximal", "Intermediate", "Distal")
W, H, GLASS = .150, .076, .0208
FPS = 60
CLIPS = {
    "Hack_Equip": (1, 31, False),
    "Hack_Idle": (61, 181, True),
    "Hack_Interact": (211, 259, False),
    "Hack_Success": (281, 329, False),
    "Hack_Failed": (351, 387, False),
    "Hack_Unequip": (411, 435, False),
    "Hack_Cancel": (461, 476, False),
}


def unit(v, fallback=None):
    return v.normalized() if v.length > 1e-7 else fallback.copy()


def basis(axis, dorsal):
    axis = axis.normalized()
    side = axis.cross(dorsal).normalized()
    return Matrix((side, axis, side.cross(axis))).transposed()


def source_data():
    arm, body = bpy.data.objects["Armature"], bpy.data.objects["Body"]
    bones = {}
    for b in arm.data.bones:
        if b.name.startswith(SIDES) and not any(t in b.name for t in ("Leg", "Foot", "Toes")):
            matrix = b.matrix_local.copy()
            matrix.translation += OFFSET
            bones[b.name] = (matrix, b.length, b.parent.name)
    points = [v.co+OFFSET for v in body.data.vertices]
    weights = [{body.vertex_groups[g.group].name:g.weight for g in v.groups} for v in body.data.vertices]
    uv = body.data.uv_layers["UV0"].data
    faces = [(list(p.vertices), [tuple(uv[i].uv) for i in p.loop_indices]) for p in body.data.polygons]
    return bones, points, weights, faces


def collection(name, parent):
    c = bpy.data.collections.new(name)
    parent.children.link(c)
    return c


def mesh(name, points, faces, coll, materials=(), uv=None, indices=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(points, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    coll.objects.link(ob)
    for material in materials:
        data.materials.append(material)
    layer = data.uv_layers.new(name="UV0")
    for p in data.polygons:
        p.use_smooth = len(p.vertices) <= 4
        if indices is not None: p.material_index = indices[p.index]
        if uv:
            for li, coord in zip(p.loop_indices, uv[p.index]):
                layer.data[li].uv = coord
        else:
            for li, vi in zip(p.loop_indices, p.vertices):
                v = data.vertices[vi].co
                layer.data[li].uv = (.5+v.x*28, .5+(v.y+v.z*.48)*28)
    return ob


def bind(ob, weights):
    groups = {}
    for i, w in enumerate(weights):
        for name, amount in w.items():
            if name not in groups: groups[name] = ob.vertex_groups.new(name=name)
            groups[name].add([i], amount, "REPLACE")
    arm = ob.modifiers.new("Armature deformation", "ARMATURE")
    arm.object = rig
    ob.parent = rig
    assets.append(ob)


def trim_weights(i, side):
    w = [(name,value) for name,value in source_weights[i].items() if name in rest and name.startswith(side)]
    w = sorted(w, key=lambda pair: -pair[1])[:4]
    if not w or sum(a for _,a in w) < 1e-5: return {side+"UpperArm":1.0}
    total = sum(a for _,a in w)
    return {n:a/total for n,a in w if a>0}


def cut_arm(side):
    matrix, length, _ = rest[side+"UpperArm"]
    origin = matrix.translation
    axis = matrix.to_3x3() @ Y
    wrist = rest[side+"Hand"][0].translation
    lower_axis = rest[side+"LowerArm"][0].to_3x3() @ Y
    sign = 1 if side=="Left" else -1
    usable = [(p-origin).dot(axis) > .034 and sign*p.x > .23 and
              sum(v for n,v in w.items() if n.startswith(side) and n in rest) > .62
              for p,w in zip(source_points,source_weights)]
    faces, uvs, indices, keep = [], [], [], {}
    for f, uv in source_faces:
        if not all(usable[i] for i in f): continue
        points = [source_points[i] for i in f]
        mid = sum(points,V())/len(f)
        distal = (mid-wrist).dot(lower_axis)
        grip = distal > .108 and sum(v.y for v in points)/len(f) < wrist.y-.003
        slot = (2 if grip else 1) if distal > .003 else 0
        faces.append([keep.setdefault(i,len(keep)) for i in f])
        scale = 13 if slot==0 else 8
        uvs.append([(u*scale,v*scale) for u,v in uv])
        indices.append(slot)
    positions = [source_points[i] for i in keep]
    ob = mesh("FP_"+side+"_Sleeve_and_Glove", positions, faces, collections[side+"Arm"],
              (mats["Fabric"],mats["Glove"],mats["Grip"]),uvs,indices)
    b = bmesh.new()
    b.from_mesh(ob.data)
    boundary = [e for e in b.edges if e.is_boundary]
    rim = {v for e in boundary for v in e.verts}
    cut = sum((v.co-origin).dot(axis) for v in rim)/len(rim)
    for v in rim: v.co += axis*(cut-(v.co-origin).dot(axis))
    caps = bmesh.ops.holes_fill(b,edges=boundary,sides=0)["faces"]
    cap_uv=b.loops.layers.uv.get("UV0")
    for face in caps:
        face.material_index=0
        face.smooth=False
        for loop in face.loops:
            v=loop.vert.co
            loop[cap_uv].uv=(v.x*18,v.y*18)
    bmesh.ops.recalc_face_normals(b,faces=list(b.faces))
    b.to_mesh(ob.data)
    b.free()
    weights = [trim_weights(i,side) for i in keep]
    bind(ob,weights)
    ob["source"] = "Phase 14 Body quad cage, trimmed proximal to the biceps"
    tree = BVHTree.FromPolygons(positions, faces, all_triangles=False)
    return ob, tree, positions, weights


def surface_point(tree, center, direction):
    point,_,_,_ = tree.ray_cast(center+direction*.11,-direction,.16)
    return point if point is not None else center+direction*.036


def deform_overlay(side, tree, name, sections, angles, material, offset, coll=None):
    # All contours are sampled on the operative mesh in the undeformed arm frame.
    samples, weights, faces, uv = [], [], [], []
    for part,t in sections:
        bone = side+part
        m,l,_ = rest[bone]
        axis = m.to_3x3()@Y
        center = m.translation + axis*l*t
        dorsal = (V((0,1,0))-axis*axis.y).normalized()
        cross = axis.cross(dorsal).normalized()
        for angle in angles:
            normal = math.cos(angle)*dorsal+math.sin(angle)*cross
            samples.append(surface_point(tree,center,normal)+normal*offset)
            if part=="UpperArm" and t>.76:
                other,blend=side+"LowerArm",(t-.76)*1.85
            elif part=="LowerArm" and t<.20:
                other,blend=side+"UpperArm",(.20-t)*2.15
            else:
                other,blend=bone,0
            weights.append({bone:1-blend,other:blend} if blend else {bone:1.0})
    n = len(angles)
    for i in range(len(sections)-1):
        for j in range(n-1):
            a=i*n+j
            faces.append((a,a+1,a+1+n,a+n))
            uv.append(((i*.22,j*.09),(i*.22,(j+1)*.09),((i+1)*.22,(j+1)*.09),((i+1)*.22,j*.09)))
    ob=mesh("FP_"+side+"_"+name,samples,faces,coll or collections["Clothing"],(material,),uv)
    # A two-sided cloth strip receives a thin physical edge on export.
    solid=ob.modifiers.new("Fabric edge thickness","SOLIDIFY")
    solid.thickness=.00042
    bind(ob,weights)
    return ob


def decorate_sleeve(side, tree):
    angles = [i*math.tau/36 for i in range(37)]
    deform_overlay(side,tree,"Wrist_Cuff",[("LowerArm",t) for t in (.885,.916,.951,.977)],angles,mats["Grip"],.0013)
    deform_overlay(side,tree,"Cuff_Topstitch",[("LowerArm",.923),("LowerArm",.932)],angles,mats["Thread"],.0020)
    deform_overlay(side,tree,"Elbow_Gusset",[("UpperArm",t) for t in (.85,.90,.95,.99)]+[("LowerArm",t) for t in (.015,.055,.10,.15)],
                   [i*.15 for i in range(-8,9)],mats["Grip"],.0021)
    for i,t in enumerate((.025,.060,.095,.130)):
        deform_overlay(side,tree,f"Elbow_Welt_{i+1:02}",[("LowerArm",t),("LowerArm",t+.012)],
                       [i*.16 for i in range(-6,7)],mats["Thread"],.0030)
    for i,phi in enumerate((-1.0,1.0)):
        segments=[("LowerArm", t) for t in (.20,.30,.43,.58,.70,.82,.90)]
        deform_overlay(side,tree,f"Bonded_Seam_{i+1}",segments,[phi,phi+.035,phi+.073],mats["Thread"],.0015)
    deform_overlay(side,tree,"Dorsal_Glove_Panel",[("Hand",t) for t in (.26,.34,.46,.57,.66,.73)],
                   [i*.14 for i in range(-5,6)],mats["Grip"],.0012)
    for i,t in enumerate((.40,.61)):
        deform_overlay(side,tree,f"Glove_Panel_Stitch_{i+1}",[("Hand",t),("Hand",t+.016)],
                       [i*.135 for i in range(-5,6)],mats["Thread"],.0019)


def poly_box(name, footprint, bottom, top, coll, material, bevel, frame=None, bone=None, uv_display=False):
    n=len(footprint)
    local=[V((x,y,z)) for z in (bottom,top) for x,y in footprint]
    coords=[frame@v for v in local] if frame is not None else local
    faces=[tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    uv = [[(.5+local[i].x/W,.5+local[i].y/H) if uv_display else (.5+local[i].x*27,.5+local[i].y*27)
           for i in f] for f in faces]
    ob=mesh(name,coords,faces,coll,(material,),uv)
    if bevel:
        m=ob.modifiers.new("Machined edge radii","BEVEL")
        m.width=bevel
        m.segments=3
        m.harden_normals=True
        nrm=ob.modifiers.new("Weighted face normals","WEIGHTED_NORMAL")
        nrm.keep_sharp=True
    if bone: bind(ob,[{bone:1.0} for _ in coords])
    return ob


def outline(w,h,cut):
    x,y=w*.5,h*.5
    return [(-x+cut,-y),(x-cut,-y),(x,-y+cut),(x,y-cut),(x-cut,y),(-x+cut,y),(-x,y-cut),(-x,-y+cut)]


def rectangle(cx,cy,w,h):
    return [(cx-w/2,cy-h/2),(cx+w/2,cy-h/2),(cx+w/2,cy+h/2),(cx-w/2,cy+h/2)]


def tablet():
    mount,screen,mech,lights=(collections[k] for k in ("MainHousing","Screen","MechanicalParts","IndicatorLights"))
    shell=[(-.098,-.030),(-.080,-.052),(.064,-.052),(.095,-.036),(.101,-.019),(.101,.030),
           (.079,.051),(-.070,.051),(-.098,.032)]
    draw=lambda name,f,z0,z1,c,mat,bv=.001,b="Device_Mount": poly_box(name,f,z0,z1,c,mats[mat],bv,device_m,b)
    draw("LATCH_07_Chassis",shell,-.004,.014,mount,"Titanium",.0033)
    draw("LATCH_07_Isolation_Gasket",outline(.183,.094,.012),-.008,-.004,mount,"Grip",.0016)
    draw("Display_Carrier",outline(.161,.086,.006),.014,.0204,screen,"Ceramic",.0018,"Device_Display")
    draw("Display_Matte_Black_Backplate",outline(.154,.080,.003),.0194,.0206,screen,"Ceramic",.00055,"Device_Display")
    glass_points=[device_m@V((x,y,GLASS)) for x,y in rectangle(0,0,W,H)]
    ob=mesh("NR92_Display_Surface",glass_points,[(0,1,2,3)],screen,(mats["Screen"],),[[(0,0),(1,0),(1,1),(0,1)]])
    bind(ob,[{"Device_Display":1.0}]*4)
    ob["uv_contract"]="UV0. Positive local X toward wrist; positive local Y toward screen header."
    draw("Guard_Port", [(-.082,-.051),(.065,-.052),(.083,-.045),(-.083,-.0445),(-.091,-.046)], .012,.024,mech,"Ceramic",.0012)
    draw("Guard_Starboard", [(-.087,.0445),(.078,.0445),(.075,.050),(-.070,.051),(-.092,.047)], .012,.023,mech,"Ceramic",.0012)
    for side in (-1,1):
        x=side*.087
        draw("Slide_"+str(side),rectangle(x,0,.010,.070),.012,.022,mech,"Edge",.0011)
        for v in (-.028,.026):
            six=[(x+math.cos(i*math.tau/6)*.0031,v+math.sin(i*math.tau/6)*.0031) for i in range(6)]
            draw("Recessed_Torx",six,.021,.0227,mech,"Titanium",.00025)
            draw("Torx_Slot",rectangle(x,v,.0035,.00085),.02265,.0228,mech,"Grip",.00009)
    draw("Radio_Endcap",rectangle(.091,.006,.012,.040),.013,.024,mech,"Ceramic",.0015)
    for i in range(5):
        draw("Radio_Cooling_Slot",rectangle(.092,-.008+i*.0054,.0065,.0016),.02385,.0241,mech,"Grip",.00018)
    draw("Secure_Radio_Aperture",rectangle(-.089,.002,.012,.022),.013,.022,mech,"Ceramic",.0007)
    draw("Radio_Status_Lens",rectangle(-.089,.002,.005,.015),.0219,.0223,lights,"LED",.00022)
    draw("Radio_Standby_Lens",rectangle(-.088,-.015,.004,.004),.018,.0202,lights,"Amber",.0005)
    draw("Service_Release",rectangle(.086,-.029,.012,.008),.017,.020,mech,"Accent",.0014)
    for side,t in enumerate((.39,.72)):
        m,l,_=rest["LeftLowerArm"]
        axis=m.to_3x3()@Y
        c=m.translation+axis*l*t
        dorsal=device_m.to_3x3()@Z
        cross=axis.cross(dorsal)
        verts=[]
        for axial in (-.009,.009):
            for i in range(49):
                angle=math.tau*i/48
                n=math.cos(angle)*dorsal+math.sin(angle)*cross
                p=surface_point(arms["Left"][1],c+axis*axial,n)
                verts.append(p+n*.0048)
        faces=[(i,i+1,i+50,i+49) for i in range(48)]
        strap=mesh(f"Retention_Webbing_{side+1}",verts,faces,mech,(mats["Grip"],))
        thick=strap.modifiers.new("Webbing thickness","SOLIDIFY")
        thick.thickness=.00155
        bind(strap,[{"LeftLowerArm":1} for _ in verts])
        x=(device_m.inverted()@c).x
        pad=[(u+x,v) for u,v in outline(.027,.070,.007)]
        draw("Curved_Saddle_Pad",pad,-.0035,.0011,mech,"Ceramic",.00125)
        angles=[i*.086 for i in range(-12,13)]
        # The saddle's fabric-facing surface follows the original forearm contour.
        deform_overlay("Left",arms["Left"][1],f"Curved_Mount_Liner_{side+1}",[("LowerArm",t-.046),("LowerArm",t+.046)],
                       angles,mats["Grip"],.0035,mech)
        # Visible attachment wings bridge the curved webbing to the chassis.
        for flank in (-1,1):
            draw("Strap_Adjuster",rectangle(x,flank*.041,.023,.013),-.009,-.003,mech,"Titanium",.002)
            draw("Webbing_Lock",rectangle(x,flank*.047,.020,.009),-.025,-.011,mech,"Ceramic",.0016)


def make_rig():
    global rig
    data=bpy.data.armatures.new("NR92_FP_Skeleton")
    rig=bpy.data.objects.new("NR92_FP_Rig",data)
    collections["Rig"].objects.link(rig)
    rig.show_in_front=True
    data.display_type="STICK"
    bpy.context.view_layer.objects.active=rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones=data.edit_bones
    root=bones.new("FP_Root")
    root.head=(0,0,0)
    root.tail=(0,.11,0)
    for name,(m,length,parent) in rest.items():
        b=bones.new(name)
        b.head=m.translation
        b.tail=m@V((0,length,0))
        b.align_roll(m.to_3x3()@Z)
        b.parent=bones.get(parent) or root
    for name,parent in (("Device_Mount","LeftLowerArm"),("Device_Display","Device_Mount")):
        b=bones.new(name)
        b.head=device_m.translation
        b.tail=device_m@V((0,.036,0))
        b.align_roll(device_m.to_3x3()@Z)
        b.parent=bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.select_set(False)
    for p in rig.pose.bones: p.rotation_mode="QUATERNION"
    for name in ("FP_Root", "Device_Mount", "Device_Display"):
        rig.data.bones[name].use_deform=True
    for side in SIDES:
        color="THEME04" if side=="Left" else "THEME03"
        for p in rig.pose.bones:
            if p.name.startswith(side): p.color.palette=color


def open_new():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global scene, collections, mats
    scene=bpy.context.scene
    scene.name="NR92 / First Person Arms"
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1
    scene.render.fps=FPS
    scene.frame_start,scene.frame_end=1,476
    scene.render.resolution_x,scene.render.resolution_y,scene.render.resolution_percentage=1440,810,100
    asset=collection("NETRunner_FP_Arms",scene.collection)
    collections={"NETRunner_FP_Arms":asset}
    for name,children in {
        "Character":("LeftArm","RightArm","Clothing"),
        "HackingDevice":("MainHousing","Screen","MechanicalParts","IndicatorLights"),
        "Rig":(), "Animation":(), "Materials":(), "PreviewCamera":(), "PreviewEnvironment":()
    }.items():
        c=collections[name]=collection(name,asset)
        for child in children: collections[child]=collection(child,c)
    mats=make_materials(ROOT/"Textures")


if __name__=="__main__":
    rest,source_points,source_weights,source_faces=source_data()
    open_new()
    forearm,length,_=rest["LeftLowerArm"]
    axis=forearm.to_3x3()@Y
    normal=(V((0,1,0))-axis*axis.y).normalized()
    direction=normal.cross(axis).normalized()
    device_m=Matrix((axis,direction,normal)).transposed().to_4x4()
    device_m.translation=forearm.translation+axis*(length*.51)+normal*.055
    assets=[]
    make_rig()
    arms={side:cut_arm(side) for side in SIDES}
    for side,(_,tree,_,_) in arms.items(): decorate_sleeve(side,tree)
    tablet()
    sys.modules["assembly"]=sys.modules[__name__]
    from presentation import finish
    finish()
