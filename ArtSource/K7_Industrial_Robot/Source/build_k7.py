import bpy, math, json, os, sys, random, time
from mathutils import Vector, Matrix
from pathlib import Path

BASE = Path(__file__).resolve().parents[2]
OUT = BASE / 'outputs' / 'K7_Industrial_Robot'
WORK = BASE / 'work'
random.seed(71)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
    if c.name != 'Collection': bpy.data.collections.remove(c)
bpy.data.collections['Collection'].name = 'K7_ROBOT'
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
prefs = bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type = 'OPTIX'
    prefs.get_devices()
    for d in prefs.devices: d.use = d.type == 'OPTIX'
    scene.cycles.device = 'GPU'
    print('DEVICES', [(d.name,d.type,d.use) for d in prefs.devices], flush=True)
except Exception as e: print(e)

def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or scene.collection).children.link(c)
    return c

robot = bpy.data.collections['K7_ROBOT']
lodcols = [collection('LOD'+str(i),robot) for i in range(4)]
rigcol = collection('K7_RIG')
ctlcol = collection('K7_ANIMATION_CONTROLS')
collcol = collection('K7_COLLIDERS')
socketcol = collection('K7_GAMEPLAY_POINTS')
refcol = collection('K7_REFERENCE')
stagecol = collection('K7_PRESENTATION')

def move_to(obj,col):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    col.objects.link(obj)

PALETTE = {
    'Armor': ((.026,.032,.038,1),.62,.43),
    'Internal': ((.008,.011,.014,1),.73,.40),
    'Metal': ((.20,.24,.27,1),.92,.29),
    'Rubber': ((.013,.019,.022,1),.0,.78),
    'SensorGlass': ((.005,.014,.023,1),.38,.16),
    'Cables': ((.085,.037,.012,1),.28,.48),
    'StatusLight': ((.8,.8,.8,1),.05,.24),
}
source_mats={}

def source_mat(kind, variant=0):
    key=(kind,variant)
    if key in source_mats: return source_mats[key]
    color,metal,rough=PALETTE[kind]
    if variant and kind=='Armor':
        color=(.047,.055,.064,1) if variant==1 else (.017,.022,.029,1)
    if variant and kind=='Cables': color=(.22,.12,.025,1)
    m=bpy.data.materials.new('SOURCE_'+kind+'_'+str(variant)); m.use_nodes=True
    m['final_kind']=kind
    nt=m.node_tree; n=nt.nodes; l=nt.links
    n.clear(); out=n.new('ShaderNodeOutputMaterial'); bs=n.new('ShaderNodeBsdfPrincipled'); l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    geo=n.new('ShaderNodeNewGeometry')
    noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=180; noise.inputs['Detail'].default_value=3
    l.new(geo.outputs['Position'],noise.inputs['Vector'])
    mix=n.new('ShaderNodeMixRGB'); mix.blend_type='MIX'
    mix.inputs[1].default_value=tuple(c*.72 for c in color[:3])+(1,)
    mix.inputs[2].default_value=tuple(min(c*1.3,1) for c in color[:3])+(1,)
    l.new(noise.outputs['Fac'],mix.inputs[0])
    # Geometry pointiness supplies restrained polished edge wear.
    ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.53; ramp.color_ramp.elements[1].position=.62
    ramp.color_ramp.elements[0].color=(0,0,0,1); ramp.color_ramp.elements[1].color=(.16,.16,.16,1)
    l.new(geo.outputs['Pointiness'],ramp.inputs[0])
    wear=n.new('ShaderNodeMixRGB'); l.new(ramp.outputs['Color'],wear.inputs[0]); l.new(mix.outputs[0],wear.inputs[1]); wear.inputs[2].default_value=(.28,.30,.31,1)
    l.new((wear if kind in ('Armor','Metal') else mix).outputs[0],bs.inputs['Base Color'])
    bs.inputs['Metallic'].default_value=metal
    rr=n.new('ShaderNodeMapRange'); rr.inputs['From Min'].default_value=0; rr.inputs['From Max'].default_value=1; rr.inputs['To Min'].default_value=rough-.07; rr.inputs['To Max'].default_value=rough+.10
    l.new(noise.outputs['Fac'],rr.inputs['Value']); l.new(rr.outputs[0],bs.inputs['Roughness'])
    fine=n.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value=2500; fine.inputs['Detail'].default_value=2
    l.new(geo.outputs['Position'],fine.inputs['Vector'])
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.17 if kind!='SensorGlass' else .015; bump.inputs['Distance'].default_value=.00032
    l.new(fine.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs['Normal'],bs.inputs['Normal'])
    if kind=='StatusLight':
        bs.inputs['Emission Color'].default_value=(1,.012,.007,1); bs.inputs['Emission Strength'].default_value=5
    m.diffuse_color=color
    source_mats[key]=m
    return m

parts=[]
GROUP='K7_LOD0'

def finish(obj,name,kind,bone,bevel=0,variant=0):
    obj.name=name; move_to(obj,lodcols[0])
    bpy.context.view_layer.objects.active=obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    if bevel:
        mod=obj.modifiers.new('Machined_edge_radius','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Keep flat panels flat and machine-turned surfaces smooth.
    for p in obj.data.polygons: p.use_smooth = len(p.vertices)==4 and not (p.area>.003 and bevel)
    if bevel:
        mod=obj.modifiers.new('Face_weighted_normals','WEIGHTED_NORMAL'); mod.keep_sharp=True; mod.weight=40
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(source_mat(kind,variant))
    obj['part_group']=GROUP; obj['bone']=bone; obj['kind']=kind
    vg=obj.vertex_groups.new(name=bone); vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    parts.append(obj)
    obj.select_set(False)
    return obj

def box(name,loc,dims,kind,bone,bevel=.005,rot=None,variant=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object; o.dimensions=dims
    if rot: o.rotation_euler=rot
    return finish(o,name,kind,bone,bevel,variant)

def cylinder(name,a,b,r,kind,bone,vertices=24,r2=None):
    a,b=Vector(a),Vector(b); v=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r if r2 is None else r2,depth=v.length,end_fill_type='NGON',location=(a+b)/2)
    o=bpy.context.object; o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
    return finish(o,name,kind,bone,.0009 if r>.007 else 0)

def ring(name,center,major,minor,axis,kind,bone):
    bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=8,location=center,major_radius=major,minor_radius=minor)
    o=bpy.context.object; o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    o=finish(o,name,kind,bone)
    for p in o.data.polygons:p.use_smooth=True
    return o

def beam(name,a,b,width,depth,kind,bone,bevel=.003):
    a,b=Vector(a),Vector(b); d=b-a
    return box(name,(a+b)/2,(width,depth,d.length),kind,bone,bevel,d.to_track_quat('Z','Y').to_euler())

def panel(name,outline,y,thick,kind,bone,bevel=.004,variant=0):
    # outline is counter-clockwise in the X/Z plane, front is -Y.
    coords=[(x,y-thick/2,z) for x,z in outline]+[(x,y+thick/2,z) for x,z in outline]
    N=len(outline); faces=[tuple(range(N)),tuple(range(2*N-1,N-1,-1))]
    faces += [(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
    mesh=bpy.data.meshes.new(name+'_mesh');mesh.from_pydata(coords,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);lodcols[0].objects.link(o)
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    return finish(o,name,kind,bone,bevel,variant)

def cable(name,points,r,kind,bone):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=8;cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
    for p,co in zip(sp.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,cu);lodcols[0].objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
    return finish(o,name,kind,bone)

def joint(name,x,y,z,r,depth,bone):
    cylinder(name+'_motor',(x-depth/2,y,z),(x+depth/2,y,z),r,'Internal',bone,32)
    for s in [-1,1]:
        xx=x+s*depth/2
        cylinder(name+'_endcap'+str(s),(xx,y,z),(xx+s*.008,y,z),r*.83,'Metal',bone,32)
        ring(name+'_seal'+str(s),(xx+s*.009,y,z),r*.68,.0045,(1,0,0),'Rubber',bone)
        cylinder(name+'_hub'+str(s),(xx+s*.010,y,z),(xx+s*.013,y,z),r*.42,'Internal',bone,24)
        # Six large structural drive dogs on the rotary flange.
        if r>.05:
            for j in range(6):
                a=j*math.tau/6
                cylinder(name+'_drive_lug_'+str(s)+'_'+str(j),(xx+s*.009,y+math.cos(a)*r*.89,z+math.sin(a)*r*.89),(xx+s*.014,y+math.cos(a)*r*.89,z+math.sin(a)*r*.89),.0045,'Metal',bone,6)

# Chest and serviceable internal torso.
box('Thoracic_computing_cage',(0,.018,1.565),(.29,.19,.28),'Internal','Chest',.022)
for s in [-1,1]:
    beam('Thorax_load_rail_'+str(s),(s*.128,.012,1.40),(s*.188,.012,1.685),.035,.08,'Metal','Chest',.007)
    cylinder('Scapular_bearing_'+str(s),(s*.125,0,1.644),(s*.265,0,1.644),.065,'Internal','Chest',40)
    for j in range(5):
        box('Side_cooling_louvre_'+str(s)+'_'+str(j),(s*.153,.027,1.49+j*.03),(.044,.135,.012),'Metal','Chest',.002)
    cable('Shoulder_power_feed_'+str(s),[(s*.105,.06,1.73),(s*.198,.066,1.73),(s*.247,.03,1.65)],.007,'Cables','Chest')
    cable('Torso_service_harness_'+str(s),[(s*.125,.122,1.66),(s*.16,.127,1.56),(s*.12,.113,1.43)],.006,'Rubber','Chest')
box('Compute_core',(0,-.09,1.57),(.13,.049,.19),'Metal','Chest',.008)
box('Compute_core_face',(0,-.122,1.57),(.098,.016,.137),'Internal','Chest',.006)
for j in range(8):box('Heatsink_fin_'+str(j),(-.043+j*.012,-.115,1.572),(.006,.015,.095),'Metal','Chest',.001)
box('Lower_power_bus',(0,-.036,1.41),(.175,.085,.049),'Metal','Chest',.006)

GROUP='Armor_Chest'
chest=panel('Chest_primary_shell',[(-.17,1.712),(-.205,1.68),(-.172,1.505),(-.104,1.458),(.105,1.458),(.173,1.505),(.205,1.68),(.166,1.712)],-.128,.030,'Armor','Chest',.009)
chest['decal']='chest'
for s in [-1,1]:
    panel('Chest_upper_flange_'+str(s),[(s*.035,1.716),(s*.151,1.74),(s*.192,1.711),(s*.153,1.657),(s*.07,1.667)],-.096,.025,'Armor','Chest',.003,1)
    box('Chest_corner_bracket_'+str(s),(s*.164,-.148,1.559),(.022,.012,.074),'Metal','Chest',.003)
    panel('Chest_inset_cheek_'+str(s),[(s*.135,1.664),(s*.178,1.651),(s*.159,1.540),(s*.124,1.506),(s*.112,1.573)],-.15,.018,'Armor','Chest',.003,2)
box('Chest_lower_lock',(0,-.139,1.461),(.099,.04,.032),'Internal','Chest',.004)
for s in [-1,1]:box('Chest_lock_catch_'+str(s),(s*.049,-.16,1.463),(.014,.008,.022),'Metal','Chest',.002)

GROUP='Armor_Back'
back=panel('Dorsal_access_shield',[(-.153,1.715),(-.183,1.681),(-.145,1.467),(.145,1.467),(.183,1.681),(.153,1.715)],.137,.025,'Armor','Chest',.007)
back['decal']='back'
GROUP='K7_HackInterface'
box('Maintenance_module_base',(0,.174,1.602),(.197,.068,.204),'Internal','Chest',.012)
hack=box('Maintenance_access_panel',(0,.211,1.635),(.155,.021,.12),'Armor','Chest',.009,variant=1);hack['decal']='service'
for s in [-1,1]:
    box('Network_module_'+str(s),(s*.052,.206,1.519),(.062,.028,.074),'Metal','Chest',.004)
for j in range(3):
    cylinder('Service_socket_'+str(j),(.045,.225,1.50+j*.022),(.045,.235,1.50+j*.022),.008,'Internal','Chest',24)
    ring('Service_socket_rim_'+str(j),(.045,.236,1.50+j*.022),.007,.0018,(0,1,0),'Metal','Chest')
for j in range(3):box('Diagnostic_indicator_'+str(j),(-.051+j*.026,.226,1.572),(.015,.006,.006),'StatusLight','Chest',.001)
box('Data_port_recess',(-.05,.226,1.525),(.039,.012,.016),'Internal','Chest',.002)
for i in range(6):box('Data_port_contact_'+str(i),(-.064+i*.005,.234,1.525),(.002,.006,.008),'Metal','Chest',.0005)
cylinder('Antenna_mount',(-.087,.13,1.716),(-.087,.13,1.758),.017,'Metal','Chest')
cylinder('Communications_antenna',(-.087,.13,1.756),(-.087,.13,1.844),.006,'Rubber','Chest',24)
cylinder('Network_aerial',(.069,.14,1.72),(.069,.14,1.786),.009,'Internal','Chest',24)

GROUP='K7_LOD0'
# Segmented waist, protected column and paired hydraulic pistons.
cylinder('Spinal_column',(0,.024,1.125),(0,.024,1.456),.042,'Internal','Spine',40)
for i in range(6):
    z=1.19+i*.036
    ring('Spinal_flex_bellows_'+str(i),(0,.024,z),.044,.009,(0,0,1),'Rubber','Spine')
for s in [-1,1]:
    cylinder('Abdominal_ram_'+str(s),(s*.085,.035,1.19),(s*.097,.04,1.355),.019,'Internal','Spine')
    cylinder('Abdominal_piston_'+str(s),(s*.097,.04,1.30),(s*.10,.04,1.433),.009,'Metal','Spine')
    cable('Abdominal_cable_'+str(s),[(s*.105,.075,1.43),(s*.09,.086,1.32),(s*.089,.07,1.18)],.007,'Cables','Spine')
for i in range(3):
    panel('Abdominal_overlap_plate_'+str(i),[(-.073,1.25+i*.049),(-.089,1.29+i*.049),(.089,1.29+i*.049),(.073,1.25+i*.049)],-.06,.034,'Armor','Spine',.004)
for s in [-1,1]:
    box('Abdomen_controller_'+str(s),(s*.064,-.087,1.350),(.037,.035,.051),'Internal','Spine',.004)
    for j in range(3):box('Controller_cooling_'+str(s)+'_'+str(j),(s*.064,-.108,1.335+j*.012),(.029,.010,.004),'Metal','Spine',.0008)
    cylinder('Abdominal_fore_ram_'+str(s),(s*.101,-.047,1.204),(s*.115,-.063,1.369),.012,'Metal','Spine')
box('Pelvis_core',(0,.014,1.10),(.205,.15,.125),'Internal','Pelvis',.018)
cylinder('Hip_cross_axle',(-.172,.016,1.081),(.172,.016,1.081),.056,'Metal','Pelvis',48)
panel('Pelvis_front_shield',[(-.114,1.16),(-.078,1.012),(.078,1.012),(.114,1.16)],-.099,.038,'Armor','Pelvis',.008,1)
box('Pelvis_top_cover',(0,-.057,1.178),(.19,.112,.037),'Armor','Pelvis',.006)
box('Pelvis_rear_cover',(0,.115,1.092),(.17,.03,.106),'Armor','Pelvis',.007)

# Neck: turntable at its base, yoke and independent sensor housing.
cylinder('Neck_rotation_base',(0,.01,1.718),(0,.01,1.753),.063,'Internal','Neck',48)
ring('Neck_turntable',(0,.01,1.75),.048,.008,(0,0,1),'Metal','Neck')
cylinder('Neck_column',(0,.005,1.75),(0,.005,1.835),.027,'Internal','Neck')
for s in [-1,1]:
    beam('Neck_gimbal_yoke_'+str(s),(s*.037,.005,1.763),(s*.037,.005,1.842),.018,.035,'Metal','Neck',.003)
    cable('Neck_flex_'+str(s),[(s*.02,.03,1.765),(s*.022,.046,1.80),(s*.02,.03,1.839)],.004,'Rubber','Head')
cylinder('Head_pitch_axle',(-.052,.005,1.844),(.052,.005,1.844),.021,'Metal','Head')
head=panel('Sensor_head_housing',[(-.069,2.0),(-.094,1.976),(-.091,1.872),(-.061,1.835),(.061,1.835),(.091,1.872),(.094,1.976),(.069,2.0)],.002,.204,'Armor','Head',.004,2);head['decal']='head'
panel('Recessed_optical_visor',[(-.077,1.965),(-.079,1.886),(-.053,1.854),(.053,1.854),(.079,1.886),(.077,1.965)],-.104,.009,'SensorGlass','Head',.003)
# The brow is a functional armored sunshade: overhangs the recessed optics.
for s in [-1,1]:
    panel('Angular_brow_'+str(s),[(s*.094,1.982),(s*.032,1.982),(0,1.956),(s*.021,1.943),(s*.077,1.960),(s*.095,1.952)],-.117,.030,'Armor','Head',.0025,2)
    panel('Optic_recess_'+str(s),[(s*.073,1.954),(s*.010,1.934),(s*.011,1.916),(s*.070,1.933)],-.114,.013,'Internal','Head',.0018)
    panel('Threat_sensor_slit_'+str(s),[(s*.067,1.946),(s*.016,1.930),(s*.016,1.923),(s*.063,1.937)],-.125,.003,'StatusLight','Head',.0006)
    panel('Lower_sensor_guard_'+str(s),[(s*.084,1.925),(s*.038,1.913),(s*.014,1.889),(s*.024,1.850),(s*.060,1.848),(s*.087,1.878)],-.112,.023,'Armor','Head',.0025,2)
    box('Head_side_service_cover_'+str(s),(s*.094,.022,1.919),(.012,.124,.096),'Armor','Head',.003,variant=2)
    for j in range(5):box('Head_side_vent_'+str(s)+'_'+str(j),(s*.101,.023+j*.011,1.928),(.003,.005,.037),'Internal','Head',.0008)
panel('Central_optics_keystone',[(-.014,1.958),(-.017,1.909),(0,1.882),(.017,1.909),(.014,1.958)],-.120,.029,'Armor','Head',.002,2)
box('Head_crown_panel',(0,.009,1.997),(.10,.13,.006),'Armor','Head',.0015,variant=2)
# Asymmetric lower ranging camera; no mouth-like light or circular eye grid.
cylinder('Range_camera_socket',(-.047,-.119,1.885),(-.047,-.128,1.885),.011,'Internal','Head',24)
ring('Range_camera_rim',(-.047,-.130,1.885),.008,.0018,(0,1,0),'Metal','Head')
cylinder('Range_camera_glass',(-.047,-.130,1.885),(-.047,-.132,1.885),.0065,'SensorGlass','Head',24)
for j in range(3):box('Lower_optic_vent_'+str(j),(.044,-.126,1.879+j*.010),(.021,.004,.003),'Internal','Head',.0005)

bones=[('Root',(0,0,0),(0,0,.15),None),('Pelvis',(0,.01,1.075),(0,.01,1.21),'Root'),('Spine',(0,.01,1.21),(0,.01,1.445),'Pelvis'),('Chest',(0,.01,1.445),(0,.01,1.729),'Spine'),('Neck',(0,.01,1.729),(0,.005,1.84),'Chest'),('Head',(0,.005,1.84),(0,.005,1.985),'Neck')]
colliders=[('Torso','Chest',(0,0,1.58),(.37,.31,.28)),('Pelvis','Pelvis',(0,0,1.09),(.25,.22,.16)),('Head','Head',(0,0,1.918),(.18,.21,.164))]
socket_defs=[('HeadPoint','Head',(0,0,2.0)),('VisionOrigin','Head',(0,-.139,1.930)),('AudioOrigin','Chest',(0,-.065,1.738)),('HackPoint','Chest',(0,.242,1.630)),('ChestPoint','Chest',(0,-.16,1.60))]

for s,side in [(1,'L'),(-1,'R')]:
    shoulder=Vector((s*.273,.005,1.637)); elbow=Vector((s*.331,-.002,1.338)); wrist=Vector((s*.373,-.015,1.078))
    ua='UpperArm_'+side;la='LowerArm_'+side;hand='Hand_'+side
    bones += [('Clavicle_'+side,(s*.09,.006,1.694),shoulder,'Chest'),(ua,shoulder,elbow,'Clavicle_'+side),(la,elbow,wrist,ua),(hand,wrist,(s*.38,-.02,.977),la)]
    GROUP='K7_LOD0'
    joint('Shoulder_'+side,*shoulder,.080,.103,ua)
    beam('Humerus_load_member_'+side,shoulder+(Vector((s*.014,0,-.06))),elbow+Vector((0,0,.03)),.058,.073,'Internal',ua,.009)
    for off in [-.035,.035]:
        cylinder('Upper_arm_tie_rod_'+side+str(off),(shoulder.x,off,1.570),(elbow.x,off,1.371),.010,'Metal',ua)
    beam('Upper_arm_outer_rail_'+side,(s*.322,.01,1.560),(s*.361,.012,1.383),.026,.08,'Armor',ua,.004)
    box('Upper_arm_collar_'+side,(s*.306,0,1.515),(.092,.098,.035),'Internal',ua,.005)
    cylinder('Biceps_hydraulic_body_'+side,(s*.288,-.040,1.573),(s*.311,-.042,1.433),.022,'Internal',ua)
    cylinder('Biceps_chrome_rod_'+side,(s*.309,-.04,1.450),(s*.325,-.04,1.369),.009,'Metal',ua)
    cable('Upper_arm_power_loop_'+side,[(s*.296,.07,1.59),(s*.334,.068,1.486),(s*.343,.034,1.378)],.008,'Rubber',ua)
    # Floating shoulder shroud is clavicle-bound, separate from the upper arm.
    GROUP='Armor_Shoulder_'+side
    panel('Shoulder_armor_'+side,[(s*.265,1.72),(s*.315,1.735),(s*.363,1.68),(s*.380,1.579),(s*.337,1.562),(s*.289,1.623)],-.006,.131,'Armor','Clavicle_'+side,.009,1)
    GROUP='K7_LOD0'
    panel('Upper_arm_front_guard_'+side,[(s*.272,1.574),(s*.338,1.572),(s*.367,1.410),(s*.304,1.396)],-.057,.024,'Armor',ua,.004)
    joint('Elbow_'+side,*elbow,.044,.100,la)
    beam('Forearm_frame_'+side,elbow+Vector((0,.005,-.03)),wrist+Vector((0,.005,.022)),.063,.071,'Internal',la,.006)
    for off in [-.027,.027]:
        cylinder('Forearm_cylinder_'+side+str(off),(s*.342,off,1.28),(s*.364,off,1.132),.012,'Metal',la)
        cylinder('Forearm_rod_'+side+str(off),(s*.359,off,1.17),(s*.37,off,1.088),.006,'Metal',la)
    cable('Forearm_utility_line_'+side,[(s*.347,.052,1.302),(s*.394,.05,1.19),(s*.383,.025,1.10)],.005,'Cables',la)
    for z,xx in [(1.255,.347),(1.145,.365)]:
        box('Forearm_band_'+side+str(z),(s*xx,.003,z),(.093,.089,.021),'Internal',la,.003)
    GROUP='Armor_Forearm_'+side
    p=panel('Forearm_shield_'+side,[(s*.313,1.302),(s*.353,1.312),(s*.403,1.169),(s*.397,1.122),(s*.353,1.123),(s*.327,1.199)],-.057,.028,'Armor',la,.007,1);p['decal']='forearm'
    GROUP='K7_LOD0'
    cylinder('Wrist_rotor_'+side,(s*.373,-.015,1.059),(s*.370,-.013,1.105),.036,'Internal',hand)
    ring('Wrist_cuff_'+side,(s*.372,-.014,1.074),.031,.004,(0,0,1),'Metal',hand)
    box('Manipulator_palm_'+side,(s*.381,-.018,1.019),(.079,.047,.085),'Internal',hand,.011)
    box('Hand_dorsal_plate_'+side,(s*.381,.010,1.023),(.067,.017,.065),'Armor',hand,.008,variant=1)
    for fi,(fname,length) in enumerate([('Index',.098),('Middle',.109),('Ring',.099),('Little',.083)]):
        x=s*(.349+fi*.022)
        zs=[.981,.981-length*.38,.981-length*.72,.981-length]
        ys=[-.018,-.022,-.030,-.039]
        parent=hand
        for seg in range(3):
            bname=f'{fname}_{seg+1}_{side}';a=(x,ys[seg],zs[seg]);b=(x,ys[seg+1],zs[seg+1])
            bones.append((bname,a,b,parent));parent=bname
            cylinder(f'{fname}_knuckle_{seg}_{side}',(x-.009,ys[seg],zs[seg]),(x+.009,ys[seg],zs[seg]),.011 if seg==0 else .008,'Metal',bname,24)
            beam(f'{fname}_phalanx_{seg}_{side}',Vector(a)+(Vector(b)-Vector(a))*.15,Vector(a)+(Vector(b)-Vector(a))*.90,.016,.020,'Internal',bname,.003)
            beam(f'{fname}_guard_{seg}_{side}',Vector(a)+Vector((0,.009,-.006)),Vector(b)+Vector((0,.009,.006)),.014,.008,'Armor',bname,.002)
            if seg==2:box(fname+'_grip_'+side,(x,ys[3]-.008,zs[3]+.01),(.016,.009,.021),'Rubber',bname,.003)
    thumbpts=[(s*.342,-.012,1.028),(s*.319,-.025,1.004),(s*.303,-.044,.977),(s*.306,-.056,.952)]
    par=hand
    for i in range(3):
        bn=f'Thumb_{i+1}_{side}';bones.append((bn,thumbpts[i],thumbpts[i+1],par));par=bn
        beam('Thumb_segment_'+str(i)+'_'+side,thumbpts[i],thumbpts[i+1],.022,.024,'Internal',bn,.004)
        cylinder('Thumb_pivot_'+str(i)+'_'+side,Vector(thumbpts[i])+Vector((0,-.012,0)),Vector(thumbpts[i])+Vector((0,.012,0)),.012,'Metal',bn,24)
    socket_defs += [('Hand_'+side+'_Point',hand,(s*.38,-.05,1.006))]
    colliders += [('UpperArm_'+side,ua,tuple((shoulder+elbow)/2),(.105,.12,.24)),('Forearm_'+side,la,tuple((elbow+wrist)/2),(.09,.12,.23)),('Hand_'+side,hand,(s*.38,-.02,1.012),(.083,.062,.115))]

    hip=Vector((s*.124,.014,1.065));knee=Vector((s*.145,-.018,.616));ankle=Vector((s*.155,.008,.173))
    th='Thigh_'+side;sh='Shin_'+side;ft='Foot_'+side;toe='Toe_'+side
    bones += [(th,hip,knee,'Pelvis'),(sh,knee,ankle,th),(ft,ankle,(s*.155,-.115,.075),sh),(toe,(s*.155,-.115,.075),(s*.155,-.218,.068),ft)]
    joint('Hip_'+side,*hip,.071,.118,th)
    beam('Femur_core_'+side,hip+Vector((0,0,-.043)),knee+Vector((0,0,.025)),.069,.095,'Internal',th,.009)
    for off in [-.042,.042]:
        cylinder('Thigh_load_rod_'+side+str(off),(s*.141,off,.990),(s*.151,off,.687),.013,'Metal',th)
    cylinder('Thigh_hydraulic_body_'+side,(s*.168,.065,1.010),(s*.176,.065,.819),.022,'Internal',th)
    cylinder('Thigh_piston_'+side,(s*.176,.065,.85),(s*.174,.065,.663),.011,'Metal',th)
    cable('Thigh_power_'+side,[(s*.096,.07,1.025),(s*.084,.085,.863),(s*.114,.065,.689)],.008,'Cables',th)
    GROUP='Armor_Thigh_'+side
    p=panel('Thigh_front_plate_'+side,[(s*.086,1.009),(s*.169,1.027),(s*.207,.950),(s*.193,.744),(s*.158,.705),(s*.105,.723)],-.063,.045,'Armor',th,.009,1);p['decal']='thigh'
    box('Thigh_outer_rib_'+side,(s*.199,-.004,.866),(.025,.099,.183),'Armor',th,.006)
    GROUP='K7_LOD0'
    for z in [.94,.80]:
        box('Thigh_actuator_band_'+side+str(z),(s*.177,.065,z),(.045,.049,.018),'Internal',th,.002)
    cylinder('Thigh_cable_connector_'+side,(s*.114,.065,.692),(s*.119,.063,.660),.012,'Metal',th,16)
    joint('Knee_'+side,*knee,.058,.129,sh)
    panel('Knee_patella_'+side,[(s*.10,.656),(s*.191,.654),(s*.18,.587),(s*.117,.586)],-.076,.04,'Armor',sh,.006)
    beam('Tibia_load_frame_'+side,knee+Vector((0,.032,-.043)),ankle+Vector((0,.022,.03)),.061,.058,'Internal',sh,.007)
    for xx in [s*.124,s*.182]:
        cylinder('Shin_shock_body_'+side+str(xx),(xx,.029,.54),(xx,.029,.361),.016,'Internal',sh)
        cylinder('Shin_shock_rod_'+side+str(xx),(xx,.029,.393),(xx,.026,.215),.008,'Metal',sh)
        ring('Shock_seal_'+side+str(xx),(xx,.029,.356),.015,.003,(0,0,1),'Metal',sh)
    cable('Shin_sensor_harness_'+side,[(s*.177,.065,.556),(s*.189,.070,.374),(s*.178,.057,.207)],.005,'Rubber',sh)
    GROUP='Armor_Shin_'+side
    p=panel('Shin_front_guard_'+side,[(s*.093,.557),(s*.193,.553),(s*.190,.458),(s*.167,.324),(s*.143,.297),(s*.109,.392)],-.054,.043,'Armor',sh,.008);p['decal']='shin'
    panel('Shin_lateral_guard_'+side,[(s*.191,.493),(s*.215,.468),(s*.189,.309),(s*.179,.302)],.005,.052,'Armor',sh,.003,1)
    GROUP='K7_LOD0'
    for xx in [s*.126,s*.18]:
        cylinder('Shin_front_exposed_rod_'+side+str(xx),(xx,-.027,.376),(xx,-.024,.208),.007,'Metal',sh)
        cylinder('Shin_lower_coupler_'+side+str(xx),(xx,-.027,.232),(xx,-.024,.208),.012,'Internal',sh)
    joint('Ankle_'+side,*ankle,.039,.107,ft)
    cylinder('Ankle_boot_'+side,(s*.155,.008,.107),(s*.155,.008,.172),.035,'Rubber',ft,32)
    box('Foot_heel_chassis_'+side,(s*.155,.012,.077),(.13,.165,.118),'Internal',ft,.017)
    box('Foot_heel_armor_'+side,(s*.155,.035,.104),(.119,.118,.06),'Armor',ft,.013)
    box('Foot_main_sole_'+side,(s*.155,-.014,.018),(.144,.211,.036),'Rubber',ft,.005)
    box('Foot_instep_'+side,(s*.155,-.079,.092),(.133,.104,.096),'Armor',ft,.013,variant=1)
    for xx in [s*.119,s*.191]:
        beam('Instep_rib_'+side+str(xx),(xx,-.12,.10),(xx,-.046,.135),.014,.014,'Metal',ft,.002)
    for t in [-1,1]:
        box('Foot_toe_segment_'+side+str(t),(s*.155+t*.036,-.173,.057),(.065,.127,.078),'Armor',toe,.010)
        box('Toe_rubber_pad_'+side+str(t),(s*.155+t*.036,-.177,.016),(.065,.123,.032),'Rubber',toe,.004)
        for j in range(3):box('Toe_tread_'+side+str(t)+'_'+str(j),(s*.155+t*.036,-.149-j*.031,.008),(.066,.010,.016),'Rubber',toe,.002)
    cylinder('Toe_hinge_'+side,(s*.094,-.116,.071),(s*.216,-.116,.071),.014,'Metal',toe,32)
    colliders += [('Thigh_'+side,th,tuple((hip+knee)/2),(.138,.17,.35)),('Shin_'+side,sh,tuple((knee+ankle)/2),(.117,.138,.36)),('Foot_'+side,ft,(s*.155,-.065,.072),(.145,.29,.143))]
    socket_defs += [('Foot_'+side+'_Point',ft,(s*.155,-.065,0))]

print('MODELED',len(parts),'parts',flush=True)
# Each closed mechanical component gets a unique UV tile; all LODs inherit this atlas.
# Allocate by surface area, then shelf-pack to preserve useful texel density.
for o in parts:
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.025,area_weight=.15,correct_aspect=True,scale_to_bounds=True)
    bpy.ops.object.mode_set(mode='OBJECT')
    o['area']=sum(p.area for p in o.data.polygons)

def pack_tiles(density):
    items=sorted([(max(20,math.ceil(math.sqrt(o['area'])*density)+8),o) for o in parts],key=lambda x:-x[0])
    x=y=row=0;alloc=[]
    for size,o in items:
        if x+size>4096:x=0;y+=row;row=0
        if y+size>3960:return None
        alloc.append((o,x,y,size));x+=size;row=max(row,size)
    return alloc
density=1250
while not (alloc:=pack_tiles(density)):density*=.95
decals=[]
for o,x,y,size in alloc:
    uv=o.data.uv_layers.active
    for p in uv.data:p.uv=((x+4+p.uv.x*(size-8))/4096,(y+4+p.uv.y*(size-8))/4096)
    o['uv_tile']=[x,y,size]
    if 'decal' in o:
        o.data.calc_loop_triangles()
        faces=[]
        for t in o.data.loop_triangles:
            norm=o.data.polygons[t.polygon_index].normal
            # Only outward faces: chest front or maintenance/rear back.
            back=o['decal'] in ('back','service')
            if (norm.y>.9 if back else norm.y<-.9):
                faces.append({'uv':[list(uv.data[l].uv) for l in t.loops],'pos':[list(o.matrix_world@o.data.vertices[v].co) for v in t.vertices]})
        verts=[o.matrix_world@v.co for v in o.data.vertices]
        decals.append({'type':o['decal'],'name':o.name,'bounds':[[min(v[i] for v in verts),max(v[i] for v in verts)] for i in range(3)],'faces':faces,'back':back})

(WORK/'decals.json').write_text(json.dumps(decals))
(WORK/'tiles.json').write_text(json.dumps([{'name':o.name,'tile':[x,y,size],'kind':o['kind']} for o,x,y,size in alloc]))
print('UV_ATLAS',density,len(alloc),flush=True)

# Skeleton with rigid weights. Helpers stay out of the FBX deform skeleton.
arm=bpy.data.armatures.new('K7_MechanicalSkeleton');rig=bpy.data.objects.new('K7_Rig',arm);rigcol.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail,parent in bones:
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]
    b.use_connect=False
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True;arm.display_type='OCTAHEDRAL'
for pb in rig.pose.bones:pb.rotation_mode='XYZ'
for o in parts:
    mod=o.modifiers.new('K7_RigidSkin','ARMATURE');mod.object=rig
    o.parent=rig

def empty(name,pos,col,parentbone=None,display='PLAIN_AXES',size=.04):
    o=bpy.data.objects.new(name,None);col.objects.link(o);o.empty_display_type=display;o.empty_display_size=size;o.location=pos
    if parentbone:
        world=o.matrix_world.copy();o.parent=rig;o.parent_type='BONE';o.parent_bone=parentbone
        bpy.context.view_layer.update();o.matrix_world=world
    return o

for name,bone,pos in socket_defs:
    e=empty(name,pos,socketcol,bone);e['purpose']='Gameplay attachment; local +Z is forward after FBX axis conversion'
    # Blender local -Y is robot forward; local +Z at the socket points forward.
    q=Vector((0,1 if name=='HackPoint' else -1,0)).to_track_quat('Z','Y')
    world=e.matrix_world.copy();world=Matrix.Translation(Vector(pos))@q.to_matrix().to_4x4();e.matrix_world=world

for name,bone,pos,dims in colliders:
    e=empty('COL_'+name,pos,collcol,bone,'CUBE',1);e.scale=Vector(dims)/2;e['collider_shape']='Box';e['dimensions_m']=list(dims);e['bone']=bone

for side in ['L','R']:
    s=1 if side=='L' else -1
    for limb,bn,pos,polepos in [('Foot','Shin_'+side,(s*.155,.008,.173),(s*.15,-.7,.62)),('Hand','LowerArm_'+side,(s*.373,-.015,1.078),(s*.55,.5,1.33))]:
        target=empty('CTRL_'+limb+'IK_'+side,pos,ctlcol,None,'CUBE',.045)
        pole=empty('CTRL_'+limb+'Pole_'+side,polepos,ctlcol,None,'SPHERE',.025)
        con=rig.pose.bones[bn].constraints.new('IK');con.name=limb+'_IK_optional';con.target=target;con.pole_target=pole;con.chain_count=2;con.influence=0
        rig[limb+'IK_'+side]=0.0
        f=con.driver_add('influence');drv=f.driver;drv.type='AVERAGE';var=drv.variables.new();var.name='enabled';var.targets[0].id=rig;var.targets[0].data_path='["'+limb+'IK_'+side+'"]'
headcon=rig.pose.bones['Head'].constraints.new('LIMIT_ROTATION');headcon.name='Mechanical_head_travel';headcon.owner_space='LOCAL'
for axis,mn,mx in [('x',-30,35),('y',-75,75),('z',-20,20)]:
    setattr(headcon,'use_limit_'+axis,True);setattr(headcon,'min_'+axis,math.radians(mn));setattr(headcon,'max_'+axis,math.radians(mx))
look=empty('CTRL_HeadLook',(0,-1,1.91),ctlcol,None,'SPHERE',.045)
track=rig.pose.bones['Head'].constraints.new('DAMPED_TRACK');track.name='Optional_head_tracking';track.target=look;track.track_axis='TRACK_NEGATIVE_Z';track.influence=0
ctlcol.hide_render=True;collcol.hide_render=True;refcol.hide_render=True

# Group visual components for useful draw call and gameplay boundaries.
groups={}
for o in parts:groups.setdefault(o['part_group'],[]).append(o)
meshes=[]
for name,objs in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    o['LOD']=0;meshes.append(o)
print('GROUPS',len(meshes),flush=True)

scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'
scene.render.resolution_x=1100;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('K7_StudioWorld');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.22,1);world.node_tree.nodes['Background'].inputs[1].default_value=.3

def light(name,loc,power,color,size,target=(0,0,1)):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);stagecol.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
light('Studio_Key',(2,-3,4),550,(.80,.88,1),3)
light('Studio_Fill',(-2,-1,2.5),330,(.68,.80,1),2)
light('Studio_Rim',(1,2.2,3),700,(1,.77,.53),2)
camd=bpy.data.cameras.new('K7_Camera');cam=bpy.data.objects.new('K7_Camera',camd);stagecol.objects.link(cam);scene.camera=cam
cam.location=(2.5,-4.4,2.4);cam.rotation_euler=(Vector((0,0,1.05))-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=2.40

# Floor is presentation-only and never selected for export.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.006));floor=bpy.context.object;floor.name='STUDIO_Floor';move_to(floor,stagecol)
fm=bpy.data.materials.new('STUDIO_Floor');fm.diffuse_color=(.045,.056,.065,1);fm.use_nodes=True;fm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=fm.diffuse_color;fm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.62;floor.data.materials.append(fm)

scene.render.filepath=str(WORK/'first_preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'k7_source.blend'))
scene.cycles.samples=32
bpy.ops.render.render(write_still=True)

metadata={'bones':[{'name':n,'head':list(h),'tail':list(t),'parent':p} for n,h,t,p in bones], 'colliders':colliders,'sockets':socket_defs,'groups':list(groups),'uv_tiles':len(alloc),'blender_version':bpy.app.version_string}
(WORK/'build_metadata.json').write_text(json.dumps(metadata,indent=2))
print('BUILD_COMPLETE',flush=True)

