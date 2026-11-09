"""Inspect evaluated Blender poses and independently reimport the delivered FBX."""
import json
import math
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
CLIPS={
    "Hack_Equip":(1,31), "Hack_Idle":(61,181), "Hack_Interact":(211,259),
    "Hack_Success":(281,329), "Hack_Failed":(351,387), "Hack_Unequip":(411,435),
    "Hack_Cancel":(461,476),
}


def points(ob, deps):
    item=ob.evaluated_get(deps)
    arr=np.empty((len(item.data.vertices),3),dtype=np.float32)
    item.data.vertices.foreach_get("co", arr.ravel())
    mat=np.asarray(item.matrix_world)
    return arr@mat[:3,:3].T+mat[:3,3]


def skinned(scene):
    return [o for o in scene.objects if o.type=="MESH" and
            any(m.type=="ARMATURE" for m in o.modifiers) and not o.name.startswith("Preview_")]


def screen_frame(points_):
    p,x,_,y=points_
    u=x-p
    v=y-p
    u,v=u/np.linalg.norm(u),v/np.linalg.norm(v)
    return p, np.stack((u,v,np.cross(u,v)), axis=1)


def projection(coords):
    h=math.tan(math.radians(40))
    depth=coords[:,1]
    with np.errstate(divide="ignore",invalid="ignore"):
        x=coords[:,0]/depth/h
        z=coords[:,2]/depth/h*16/9
    visible=(depth>.016)&(np.abs(x)<=1)&(np.abs(z)<=1)
    return np.stack((x,z), axis=1)*.5+.5,visible


def normal_area(q):
    return abs(sum(q[i,0]*q[(i+1)%4,1]-q[(i+1)%4,0]*q[i,1] for i in range(4))*.5)


def triangles(ob,deps):
    data=ob.evaluated_get(deps).data
    data.calc_loop_triangles()
    return [tuple(t.vertices) for t in data.loop_triangles]


def main_blend():
    s=bpy.context.scene
    armature=bpy.data.objects["NR92_FP_Rig"]
    objects=skinned(s)
    arms=[bpy.data.objects["FP_"+side+"_Sleeve_and_Glove"] for side in ("Left","Right")]
    right=arms[1]
    slot=right.vertex_groups["RightIndexDistal"].index
    tips=np.array([v.index for v in right.data.vertices if any(g.group==slot and g.weight>.63 for g in v.groups)])
    screen=bpy.data.objects["NR92_Display_Surface"]
    hard_parts=[o for o in objects if o.name.startswith(("LATCH_07_Chassis","Curved_Saddle_Pad",
                 "Strap_Adjuster","Webbing_Lock","Display_Carrier","Guard_","Slide_"))]
    guards=[o for o in hard_parts if o.name.startswith(("Guard_","Slide_"))]
    soft_parts=[o for o in objects if o.name.startswith(("Retention_Webbing","FP_Left_Curved_Mount_Liner","LATCH_07_Isolation_Gasket"))]
    mounts,soft_contacts,carrier_guard,glass_crossings,arm_crossings=[],[],[],[],[]
    missing_norms=[]
    max_influences=0
    for obj in objects:
        for v in obj.data.vertices:
            active=[g.weight for g in v.groups if g.weight>1e-5]
            if not active or abs(sum(active)-1)>1e-4: missing_norms.append((obj.name,v.index))
            max_influences=max(max_influences,len(active))
    trim_vertices=[]
    for ob in arms:
        rim={i for face in ob.data.polygons if not face.use_smooth for i in face.vertices}
        if not rim: raise RuntimeError("Shoulder cap missing: "+ob.name)
        trim_vertices.append(np.array(sorted(rim),dtype=int))
    rows={}
    matrices={}
    for clip,(start,end) in CLIPS.items():
        result=[]
        for frame in range(start,end+1):
            s.frame_set(frame)
            dg=bpy.context.evaluated_depsgraph_get()
            gauntlet=points(screen,dg)
            origin,axes=screen_frame(gauntlet)
            l,r=(points(o,dg) for o in arms)
            q,gauntlet_visible=projection(gauntlet)
            right_local=(r-origin)@axes
            active=(right_local[:,0]>0)&(right_local[:,0]<.150)&(right_local[:,1]>0)&(right_local[:,1]<.076)
            lowest=float(right_local[active,2].min()) if active.any() else None
            active_tip=active[tips]
            tip_depth=float(right_local[tips[active_tip],2].min()) if active_tip.any() else None
            near_eye=min(float(np.linalg.norm(l,axis=1).min()),float(np.linalg.norm(r,axis=1).min()))
            shoulder_visible=any(projection(coords[idx])[1].any() for idx,coords in zip(trim_vertices,(l,r)))
            visible=np.count_nonzero(projection(l)[1])+np.count_nonzero(projection(r)[1])
            result.append({
                "frame":frame, "near_eye_m":round(near_eye,5), "shoulder_trim_visible":bool(shoulder_visible),
                "arm_vertices_in_camera":int(visible), "screen_corners_in_camera":int(np.count_nonzero(gauntlet_visible)),
                "screen_area_fraction":round(normal_area(q),5),
                "screen_projection":np.round(q,5).tolist(),
                "right_nearest_in_panel_bounds_m":None if lowest is None else round(lowest,5),
                "right_index_pad_to_display_m":None if tip_depth is None else round(tip_depth,5),
            })
            if frame in (start,end):
                matrices[clip+("_start" if frame==start else "_end")]=np.array(
                    [[value for row in bone.matrix for value in row] for bone in armature.pose.bones])
            left_bvh=BVHTree.FromPolygons(l.tolist(),triangles(arms[0],dg),all_triangles=True)
            right_bvh=BVHTree.FromPolygons(r.tolist(),triangles(arms[1],dg),all_triangles=True)
            pane_bvh=BVHTree.FromPolygons(gauntlet.tolist(),[(0,1,2),(0,2,3)],all_triangles=True)
            if left_bvh.overlap(right_bvh): arm_crossings.append(frame)
            if pane_bvh.overlap(right_bvh): glass_crossings.append(frame)
            trees={o.name:BVHTree.FromPolygons(points(o,dg).tolist(),triangles(o,dg),all_triangles=True)
                   for o in hard_parts+soft_parts}
            for ob in hard_parts:
                if left_bvh.overlap(trees[ob.name]): mounts.append((frame,ob.name))
            for ob in soft_parts:
                if left_bvh.overlap(trees[ob.name]): soft_contacts.append((frame,ob.name))
            for ob in guards:
                if trees["Display_Carrier"].overlap(trees[ob.name]): carrier_guard.append((frame,ob.name))
        rows[clip]=result
    joins={}
    for first,second in (
        ("Hack_Equip","Hack_Idle"),("Hack_Idle","Hack_Idle"),("Hack_Idle","Hack_Interact"),
        ("Hack_Interact","Hack_Success"),("Hack_Interact","Hack_Failed"),
        ("Hack_Success","Hack_Unequip"),("Hack_Failed","Hack_Cancel"),("Hack_Unequip","Hack_Equip"),("Hack_Cancel","Hack_Equip")
    ):
        joins[first+" → "+second]=float(np.abs(matrices[first+"_end"]-matrices[second+"_start"]).max())
    s.frame_set(121)
    dg=bpy.context.evaluated_depsgraph_get()
    evaluated_tris={}
    for ob in objects:
        data=ob.evaluated_get(dg).data
        data.calc_loop_triangles()
        evaluated_tris[ob.name]=len(data.loop_triangles)
    stowed_meshes={}
    for f in (1,435,476):
        s.frame_set(f)
        dg=bpy.context.evaluated_depsgraph_get()
        stowed_meshes[str(f)]=sum(int(projection(points(o,dg))[1].sum()) for o in objects)
    idle=rows["Hack_Idle"]
    closest=min((row for row in rows["Hack_Interact"] if row["right_index_pad_to_display_m"] is not None),
                key=lambda row:abs(row["right_index_pad_to_display_m"]),default=None)
    penetration=[(clip,row["frame"],row["right_nearest_in_panel_bounds_m"]) for clip,values in rows.items() for row in values
                 if row["right_nearest_in_panel_bounds_m"] is not None and row["right_nearest_in_panel_bounds_m"]<-.0015
                 and row["screen_corners_in_camera"]==4]
    return {
        "format":"Blend pose audit",
        "bone_count":len(armature.data.bones), "arm_vertices":[len(o.data.vertices) for o in arms],
        "skinned_mesh_objects":len(objects), "evaluated_triangles":sum(evaluated_tris.values()),
        "triangles_by_object":evaluated_tris, "max_vertex_influences":max_influences,
        "unweighted_or_unnormalized_count":len(missing_norms),
        "frames_sampled":sum(map(len,rows.values())), "stowed_visible_asset_vertices":stowed_meshes,
        "shoulder_trim_visible_frames":[row["frame"] for values in rows.values() for row in values if row["shoulder_trim_visible"]],
        "min_distance_to_eye_m":min(row["near_eye_m"] for values in rows.values() for row in values),
        "idle_screen_area_percent":list(np.round(np.array([min(row["screen_area_fraction"] for row in idle),
                                                        max(row["screen_area_fraction"] for row in idle)])*100,2)),
        "idle_screen_inside_frame_every_sample":all(row["screen_corners_in_camera"]==4 for row in idle),
        "closest_index_contact":closest, "panel_penetration_samples":penetration,
        "glass_triangle_crossings":glass_crossings,
        "arm_to_arm_triangle_crossings":arm_crossings,
        "rigid_mount_triangle_crossings":mounts,
        "carrier_to_guard_triangle_crossings":carrier_guard,
        "flexible_liner_contact_frames":len(set(f for f,_ in soft_contacts)),
        "pose_join_max_delta":joins, "frames":rows,
    }


def roundtrip():
    original=bpy.data.objects["NR92_FP_Rig"]
    bones={b.name:b.length for b in original.data.bones}
    original_s=bpy.context.scene
    def capture(rig, scene, action, frame):
        rig.animation_data_create()
        rig.animation_data.action=action
        scene.frame_set(round(frame))
        return {b.name:b.head.copy() for b in rig.pose.bones}
    source_actions={name:bpy.data.actions[name] for name in CLIPS}
    original.animation_data.use_nla=False
    original_poses={}
    for name,action in source_actions.items():
        original_poses[name]=[capture(original,original_s,action,f) for f in (
            action.frame_range[0],round(sum(action.frame_range)*.5),action.frame_range[1])]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s=bpy.context.scene
    s.render.fps=60
    bpy.ops.import_scene.fbx(filepath=str(ROOT/"NETRunner_FP_Arms.fbx"),use_anim=True,anim_offset=1)
    rigs=[o for o in s.objects if o.type=="ARMATURE"]
    rig=rigs[0]
    exported=[]
    names=[]
    for action in bpy.data.actions:
        name=next((clip for clip in CLIPS if clip in action.name),None)
        if not name: continue
        start,end=action.frame_range
        rig.animation_data.action=action
        errors=[]
        for f,expected in zip((start,round((start+end)*.5),end),original_poses[name]):
            s.frame_set(round(f))
            diffs=[]
            for key,position in expected.items():
                bone=rig.pose.bones.get(key)
                if bone: diffs.append(((rig.matrix_world@bone.head)-position).length)
            errors.append(max(diffs,default=0))
        names.append(name)
        exported.append({"clip":name,"fbx_action":action.name,"frames":round(end-start),
                         "seconds":round((end-start)/60,4),"max_bone_head_delta_m":round(max(errors),6)})
    screen=s.objects.get("NR92_Display_Surface")
    meshes=[o for o in s.objects if o.type=="MESH"]
    geometry=[]
    for ob in meshes:
        ob.data.calc_loop_triangles()
        geometry.append(len(ob.data.loop_triangles))
    audit={
        "format":"Fresh Blender FBX round-trip",
        "imported_armatures":len(rigs),
        "imported_bones":len(rig.data.bones),
        "mesh_count":len(meshes),
        "triangles":sum(geometry),
        "clips":exported,
        "matching_clip_count":len(set(names)),
        "missing_or_unexpected_bone_names":sorted(set(bones)^set(rig.data.bones.keys())),
        "screen_mesh":screen.name if screen else None,
        "screen_uv_loops":[tuple(v.uv) for v in screen.data.uv_layers.active.data] if screen and screen.data.uv_layers.active else None,
        "preview_objects": [o.name for o in s.objects if o.type in {"LIGHT","CAMERA","FONT"} or o.name.startswith(("Preview_", "K-7", "Corridor"))],
        "material_count":len({mat.name for o in meshes for mat in o.data.materials if mat}),
        "imported_image_count":len(bpy.data.images),
    }
    return audit


if __name__=="__main__":
    dest=ROOT/"Validation"
    dest.mkdir(exist_ok=True)
    mode=sys.argv[-1]
    if mode=="fbx":
        result=roundtrip()
        file="fbx_roundtrip.json"
    else:
        result=main_blend()
        file="pose_audit.json"
    (dest/file).write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding="utf-8")
    print(json.dumps({k:v for k,v in result.items() if k not in ("frames","triangles_by_object")},ensure_ascii=False))
