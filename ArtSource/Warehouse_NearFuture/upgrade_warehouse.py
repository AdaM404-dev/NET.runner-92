"""Additive near-future retrofit. Run in Blender 5.0+ with --background --python.
Loads the completed sibling Warehouse_Complex asset; never rebuilds its architecture.
"""
import bpy,math,json,hashlib,struct,sys,random
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parent
BASE=ROOT/'Source'/'Original_Warehouse.blend'
if '--' in sys.argv:
    args=sys.argv[sys.argv.index('--')+1:]
    if args:BASE=Path(args[0]).resolve()
bpy.ops.wm.open_mainfile(filepath=str(BASE))
scene=bpy.context.scene
def digest(o):
    h=hashlib.sha256()
    if o.type=='MESH':
        for v in o.data.vertices:h.update(struct.pack('fff',*v.co))
        for p in o.data.polygons:h.update(struct.pack('I',len(p.vertices)));h.update(struct.pack('I'*len(p.vertices),*p.vertices))
    for row in o.matrix_world:h.update(struct.pack('ffff',*row))
    return h.hexdigest()
baseline={o.name:digest(o) for o in scene.objects if o.type=='MESH'}
original_names=set(o.name for o in scene.objects)
M={m.name:m for m in bpy.data.materials};cols={c.name:c for c in bpy.data.collections}
exec(compile((ROOT/'geometry_helpers.py').read_text(),str(ROOT/'geometry_helpers.py'),'exec'))
def collection(name,parent=None):
    c=bpy.data.collections.new(name);(cols[parent] if parent else scene.collection).children.link(c);cols[name]=c
collection('17_NEAR_FUTURE_RETROFIT')
for name in ['17A_Door_Systems','17B_Facade_Panels','17C_Security_Interfaces','17D_Circulation_Lighting','17E_Service_Infrastructure','17F_Ceiling_Systems','17G_Roof_Communications','17H_Signage']:
    collection(name,'17_NEAR_FUTURE_RETROFIT')
def material(name,color,rough=.5,metal=0,emit=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if emit:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
    M[name]=m;return m
material('NF_Graphite',(.042,.060,.074),.48,.55)
material('NF_Composite',(.10,.14,.17),.63,.25)
material('NF_Alloy',(.31,.38,.43),.36,.82)
material('NF_CeramicGrey',(.34,.40,.42),.62,.12)
material('NF_Grip',(.055,.070,.078),.90,0)
material('NF_LED_White',(.68,.82,.92),.45,0,3)
material('NF_LED_Cyan',(.15,.53,.63),.5,0,2)
material('NF_LED_Amber',(.8,.41,.10),.5,0,1.8)
material('NF_Screen',(.032,.105,.135),.4,0,.8)
material('NF_Text',(.48,.74,.81),.5,0,1)
material('NF_Warning',(.63,.40,.12),.65,0)
for kind in ['Control','Access']:
    mat=material('NF_UI_'+kind,(.06,.13,.16),.27,0,.4)
    img=bpy.data.images.load(str(ROOT/'Textures'/('NF_UI_'+kind+'.png')),check_existing=True)
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img
    p=mat.node_tree.nodes.get('Principled BSDF');mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Emission Color'])

# Replace only material images in this copy; the existing architectural meshes stay intact.
for im in bpy.data.images:
    candidate=ROOT/'Textures'/Path(im.filepath).name
    if candidate.is_file():
        if im.packed_file:im.unpack(method='REMOVE')
        im.filepath=str(candidate);im.reload();im.pack()
for name in ['Steel','Cladding','Door','Flooring','Plaster','Galvanized']:
    M[name]['retrofit']='Renewed coating using original UV and surface grain'
p=M['DirtyGlass'].node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.09,.19,.23,1);p.inputs['Roughness'].default_value=.22;p.inputs['Transmission Weight'].default_value=.50
M['DirtyGlass']['system']='Electrochromic glazing; visual state only'
p=M['WarmLamp'].node_tree.nodes.get('Principled BSDF');p.inputs['Emission Color'].default_value=(.75,.86,1,1);p.inputs['Emission Strength'].default_value=2.7
for o in scene.objects:
    if o.type=='LIGHT' and o.name.startswith('LIGHT_') and not any(x in o.name for x in ['Moon','Bounce','Cutaway']):o.data.color=(.74,.85,1);o.data.energy*=.87

def textplate(name,body,loc,size=.12,rot=0,material='NF_Text',parent=None):
    o=label('NF_ID_'+name,body,loc,size,(math.pi/2,0,rot),'17H_Signage',material)
    o.data.resolution_u=3;o.data.extrude=.0003
    if parent:o.parent=parent
    return o
def panel(name,loc,width=1.1,height=1.6,rot=0,kind='diagnostic'):
    m=Mesh();m.box((0,0,height/2),(width,.085,height),'NF_Graphite')
    m.box((0,-.049,height/2),(width-.055,.020,height-.07),'NF_Composite')
    m.box((-width/2+.06,-.064,height/2),(.018,.014,height-.14),'NF_Alloy')
    if kind!='blank':
        ui='NF_UI_Access' if 'reader' in kind or 'lock' in kind else 'NF_UI_Control'
        m.box((0,-.067,height*.64),(width*.76,.016,height*.36),ui)
        for z in [.17,.22,.27]:m.box((0,-.062,z),(width*.62,.006,.009),'NF_Grip')
        m.box((width*.31,-.073,height*.85),(.04,.008,.035),'NF_LED_Cyan')
    o=m.build('NF_PANEL_'+name,'17C_Security_Interfaces',.008);o.location=loc;o.rotation_euler.z=rot
    for poly in o.data.polygons:
        if o.data.materials[poly.material_index].name.startswith('NF_UI_'):
            for li in poly.loop_indices:
                v=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(v.x/(width*.76)+.5,(v.z-height*.64)/(height*.36)+.5)
    o['purpose']=kind;o['interaction']='Visual interface; implement gameplay in engine'
    if kind!='blank':textplate(name,name,(0,-.062,height*.87),min(.08,width*.10,width/(max(1,len(name))*.65)),parent=o)
    return o
def sensor(name,loc,rot=0):
    m=Mesh();m.box((0,0,0),(.26,.10,.40),'NF_Graphite');m.box((0,-.11,.03),(.20,.19,.16),'NF_Alloy');m.box((0,-.211,.03),(.14,.018,.085),'NF_Grip');m.box((.055,-.224,.02),(.022,.009,.022),'NF_LED_Cyan');m.box((0,-.054,-.12),(.14,.012,.03),'NF_LED_Amber')
    ob=m.build('NF_SENSOR_'+name,'17C_Security_Interfaces',.006);ob.location=loc;ob.rotation_euler.z=rot;ob['purpose']='Surveillance / occupancy sensor housing';return ob
def strip(name,a,b,ma='NF_LED_White',width=.018,col='17D_Circulation_Lighting'):
    m=Mesh();m.beam(a,b,width,width,ma);return m.build('NF_STRIP_'+name,col,.002)

# Reinforced original hinged leaves: children follow the existing hinge and door pose.
manifest=json.loads((ROOT/'Asset_manifest.json').read_text())
for i,item in enumerate(manifest['portals']):
    leaf=bpy.data.objects.get(item['name'])
    if not leaf:continue
    w=item['width'];h=item['height'];m=Mesh()
    for face in [-1,1]:
        m.box((w/2,face*.041,h/2),(w-.16,.017,h-.25),'NF_Composite')
        m.box((w*.30,face*.053,h*.63),(.035,.009,h*.35),'NF_Alloy')
        m.box((w*.72,face*.056,h-.31),(min(.23,w*.20),.01,.025),'NF_LED_Cyan')
        m.box((w/2,face*.055,.22),(w-.24,.012,.12),'NF_Graphite')
    child=m.build('NF_DOOR_Skin_'+item['name'],'17A_Door_Systems',.004)
    child.parent=leaf;child['follows_operable_door']=True
    # Magnet housing sits above the opening; status is duplicated on both wall faces.
    rz=leaf.get('closed_rotation_z',0);p=Vector(item['position']);R=Matrix.Rotation(rz,3,'Z');m=Mesh()
    for side in [-1,1]:
        m.box((0,side*.192,h+.10),(min(.70,w),.07,.13),'NF_Graphite')
        m.box((0,side*.232,h+.10),(min(.44,w*.65),.012,.022),'NF_LED_Cyan')
    ob=m.build('NF_MAGLOCK_'+item['name'],'17A_Door_Systems',.004);ob.location=p;ob.rotation_euler.z=rz
    if i%2==0 or any(s in item['name'] for s in ['Exterior','Hall','Tech','Shaft']):
        q=p+R@Vector((w/2+.23,-.225,.97))
        rd=panel('ACCESS_'+str(i+1).zfill(3),q,.23,.43,rz,'RFID / biometric reader')
        rd['controls_door']=leaf.name

# Exterior recladding respects all door/window positions and the existing massing.
for zone,xs,h in [('WEST',[-56.3,-49,-41,-33.3],6.3),('EAST',[13.3,20.5,27.5,34.6],7.6)]:
    for i,x in enumerate(xs):
        m=Mesh();m.box((x,-38.21,2.45),(.38,.09,4.85),'NF_Graphite');m.box((x,-38.264,2.45),(.25,.025,4.72),'NF_Composite');m.box((x,-38.285,3.5),(.025,.012,.80),'NF_LED_White');m.build(f'NF_FACADE_Pilaster_{zone}_{i}','17B_Facade_Panels',.006)
for x in range(-31,12,3):
    box('NF_FACADE_AdminBand_'+str(x),(x,-38.235,3.07),(2.90,.11,.62),'NF_Composite','17B_Facade_Panels',.007)
for x in range(-34,36,6):
    box('NF_FACADE_HallCrown_'+str(x),(x,-18.21,10.32),(5.85,.11,.82),'NF_Graphite','17F_Ceiling_Systems',.007)
    strip('Crown_'+str(x),(x-1.4,-18.28,10.30),(x+1.4,-18.28,10.30),'NF_LED_White',.022,'17F_Ceiling_Systems')
textplate('Facility','NORTHLINE  /  LOGISTICS SYSTEMS',(-9,-38.31,3.06),.30)
textplate('FacilitySmall','SECURE INDUSTRIAL SITE     /     SECTOR 07',(-9,-38.31,2.79),.105)

# Smart loading-bay retrofit: outer frame, motor header, status board, sensor posts.
for i,x in enumerate([-53,-45,-37,17,24,31],1):
    m=Mesh()
    for side in [-1,1]:
        m.box((x+side*2.32,-38.30,2.45),(.23,.35,4.95),'NF_Graphite')
        m.box((x+side*2.325,-38.487,3.10),(.045,.018,1.7),'NF_LED_Cyan')
        m.box((x+side*2.05,-39.97,-.15),(.1,.065,.10),'NF_LED_Amber')
    m.box((x,-38.31,4.99),(4.88,.40,.35),'NF_Graphite')
    m.box((x,-38.535,5.05),(1.60,.065,.48),'NF_Screen')
    m.build(f'NF_DOCK_Automation_{i:02}','17A_Door_Systems',.007)
    m=Mesh()
    for z in [1.2,2.5,3.8]:m.box((0,-.024,z),(3.70,.025,.08),'NF_Alloy')
    skin=m.build(f'NF_LOADING_LeafDetail_{i:02}','17A_Door_Systems',.003);skin.parent=bpy.data.objects[f'DOOR_Loading_{i:02}'];skin['follows_operable_door']=True
    textplate(f'Dock_{i}',f'{i:02}  /  READY',(x,-38.58,4.99),.20)
    panel(f'DOCK-{i:02}',(x+2.69,-38.3,1.1),.43,.70,0,'Dock controller')
    for side in [-1,1]:
        px=x+side*2.52;m=Mesh();m.box((px,-42.2,-.39),(.20,.22,1.05),'NF_Graphite');m.box((px,-42.33,-.17),(.12,.04,.31),'NF_Alloy');m.box((px,-42.355,-.13),(.064,.018,.13),'NF_LED_Cyan');m.build(f'NF_DOCK_AlignmentScanner_{i}_{side}','17C_Security_Interfaces',.007)
        strip(f'DockGuide_{i}_{side}',(x+side*2.0,-41.4,-.918),(x+side*2.0,-47.3,-.918),'NF_LED_White',.019)
    sensor(f'DOCK-{i:02}',(x-2.35,-38.6,4.7))

# Selected hall panels and built-in stations avoid existing door apertures.
for side in [-1,1]:
    x=side*35.78;rot=-math.pi/2 if side>0 else math.pi/2
    for i,y in enumerate([-3,12]):
        panel(f'HALL-{side+2}{i}',(x,y,.45),3.6,2.3,rot,'Integrated logistics / maintenance station')
        sensor(f'HALL-{side+2}{i}',(side*35.7,y,5.0),rot)
for x in [-30,-13,10,29]:
    panel('HUB-'+str(x),(x,25.76,.45),2.4,2.2,0,'Environmental / network control')
    textplate('HallZone'+str(x),'HALL  /  '+str((x+30)//13+1).zfill(2),(x,25.67,3.5),.42)
# Four corner collars articulate existing concrete columns without replacing them.
for x in [-27,-9,9,27]:
    for y in [-7,15]:
        m=Mesh()
        for side in [-1,1]:
            m.box((x+side*.348,y,1.05),(.047,.63,1.55),'NF_Graphite')
            m.box((x,y+side*.348,1.05),(.63,.047,1.55),'NF_Composite')
        m.box((x+.16,y-.378,1.62),(.16,.025,.13),'NF_Alloy');m.box((x+.16,y-.394,1.64),(.085,.006,.018),'NF_LED_Cyan');m.build(f'NF_COLUMN_ServiceCollar_{x}_{y}','17E_Service_Infrastructure',.006)

# Broad painted logistics lanes; only short intervals are illuminated.
for x in [-20,20]:
    m=Mesh()
    for ya,yb in [(-14,0),(5,21)]:m.box((x,(ya+yb)/2,.006),(.075,yb-ya,.007),'NF_CeramicGrey')
    for y in [-12,-2,10,20]:
        for dx in [-.20,.20]:m.beam((x+dx,y-.30,.013),(x,y,.013),.035,.025,'NF_CeramicGrey')
    m.build('NF_FLOOR_LogisticsLane_'+str(x),'17D_Circulation_Lighting',.001)
    for y in [-12,2,17]:strip(f'LaneMarker_{x}_{y}',(x,y,.013),(x,y+.65,.013),'NF_LED_White',.018)
for x,y in [(-14,-5),(14,7)]:
    m=Mesh()
    for dx,dy in [(-2,-2),(-2,2),(2,-2),(2,2)]:
        m.box((x+dx,y+dy,.009),(.8,.065,.008),'NF_Warning');m.box((x+dx,y+dy,.009),(.065,.8,.008),'NF_Warning')
    m.build('NF_FLOOR_ServiceBoundary_'+str(x),'17D_Circulation_Lighting',.001)

# Rail-mounted markers reuse actual rail spans, preserving every access gap.
for ob in list(scene.objects):
    if ob.type!='MESH' or not ob.name.startswith('RAIL_'):continue
    verts=ob.data.vertices;m=Mesh();found=False
    for k in range(0,len(verts)-7,8):
        vs=[ob.matrix_world@verts[k+j].co for j in range(8)];center=sum(vs,Vector())/8
        edges=[vs[1]-vs[0],vs[3]-vs[0],vs[4]-vs[0]];edge=max(edges,key=lambda v:v.length)
        if abs(edge.z)>.05 or edge.length<.7:continue
        # Top rail is highest among the assembly's horizontal pieces.
        if center.z<max((ob.matrix_world@v.co).z for v in verts)-.10:continue
        unit=edge.normalized();n=max(1,int(edge.length/4))
        for j in range(n):
            c=center+unit*((j+.5)*edge.length/n-edge.length/2)+Vector((0,0,-.015))
            length=min(.65,edge.length*.6);m.beam(c-unit*length/2,c+unit*length/2,.014,.018,'NF_LED_White');found=True
    if found:m.build('NF_RAIL_Markers_'+ob.name,'17D_Circulation_Lighting',.001)
for st in manifest['stairs']:
    a=Vector(st['base']);b=Vector(st['top']);dr=Vector((b.x-a.x,b.y-a.y,0)).normalized();cross=Vector((-dr.y,dr.x,0));m=Mesh()
    for j in range(0,st['steps'],4):
        c=a+dr*(j*st['tread']+.035)+Vector((0,0,(j+1)*st['riser']+.006));m.beam(c-cross*(st['width']/2-.09),c+cross*(st['width']/2-.09),.012,.012,'NF_LED_White')
    o=m.build('NF_STAIR_Nosing_'+st['name'],'17D_Circulation_Lighting',.001);o['purpose']='Flush illuminated nosing; original stair unchanged'

# Wall-mounted office and maintenance technology stays out of corridor centre-lines.
for x,rot in [(-46.86,math.pi/2),(-44.14,-math.pi/2)]:
    for z in [0,3.6]:
        for i,y in enumerate([-6,10,25]):
            panel(f'ADMIN-{int(z*10)}-{abs(x)}-{i}',(x,y,z+.86),.62,.98,rot,'Room directory / secured administration')
            strip(f'OfficeLight_{x}_{y}_{z}',(x,y-1.3,z+2.95),(x,y+1.3,z+2.95),'NF_LED_White',.025)
for z in [0,3.6]:
    for x in [-24,-9,5]:panel(f'ADMIN-S-{x}-{z}',(x,-26.87,z+.94),.65,.92,math.pi,'Administration / room control')
for i,y in enumerate([-13,-4,7]):
    panel('SERVICE-E'+str(i),(38.025,y,.65),.72,1.40,-math.pi/2,'Network relay / diagnostics')
    sensor('SERVICE-E'+str(i),(38.03,y+.6,2.25),-math.pi/2)
    strip('ServiceRoute_'+str(i),(36.27,y-.8,.12),(36.27,y+.8,.12),'NF_LED_Cyan',.017)
for y in [-14,-7,1]:
    # Recess-like thin housings mounted on the technical-room perimeter, not loose machines.
    panel('POWER-E'+str(y),(57.75,y,.20),1.8,2.15,-math.pi/2,'Power distribution / robotic service interface')
for y in [-12,-3,5]:
    m=Mesh();m.box((37.99,y,2.32),(.11,5,.14),'NF_Graphite')
    for dz in [-.03,.03]:m.box((37.922,y,2.32+dz),(.018,4.96,.027),'NF_Alloy')
    m.build('NF_SERVICE_CableTrunk_'+str(y),'17E_Service_Infrastructure',.004)
for x in [41,47,54]:
    m=Mesh();m.box((x,9.84,2.3),(5,.10,.13),'NF_Graphite');m.box((x,9.774,2.31),(4.9,.018,.02),'NF_Alloy');m.build('NF_SERVICE_BendTrunk_'+str(x),'17E_Service_Infrastructure',.004)
    panel('RELAY-'+str(x),(x,9.82,.9),.5,.8,0,'Maintenance diagnostic node')
for i,(x,y) in enumerate([(-33,20),(31,19)]):
    panel('SHAFT-'+str(i),(x+.69,y-1.19,1.06),.30,.55,0,'Shaft lock / ventilation state')
    strip('ShaftIndicator_'+str(i),(x-.68,y-1.185,2.35),(x+.68,y-1.185,2.35),'NF_LED_Cyan',.018)

# Suspended technological systems attach to the existing roof load-bearing grid.
for x in [-22.5,-4.5,13.5]:
    m=Mesh();m.box((x,4,9.70),(.18,40,.16),'NF_Graphite')
    for y in [-14,-6,4,14,22]:
        m.box((x,y,9.61),(.23,2.75,.08),'NF_Alloy');m.box((x,y,9.56),(.17,2.60,.025),'NF_LED_White')
        roof_purlin_z=11.30+1.35*(1-abs(y-4)/22)
        m.beam((x,y,9.78),(x,y,roof_purlin_z-.08),.022,.022,'NF_Alloy')
        area(f'NF_LIGHT_Linear_{x}_{y}',(x,y,9.51),(x,y,0),220,2.6,(.76,.87,1),'17F_Ceiling_Systems','RECTANGLE',.18)
    m.build('NF_CEILING_PowerLightingRail_'+str(x),'17F_Ceiling_Systems',.004)
for x in [-27,9,27]:
    for y in [-7,15]:
        px,py=x+.60,y+.60
        m=Mesh();m.box((px,py,9.35),(.50,.50,.18),'NF_Graphite');m.box((px,py,9.23),(.25,.25,.08),'NF_Alloy');m.box((px,py,9.18),(.15,.15,.025),'NF_Screen');m.beam((x,y,9.45),(px,py,9.45),.065,.065,'NF_Alloy');m.build(f'NF_CEILING_Monitor_{x}_{y}','17F_Ceiling_Systems',.004)
# Small roof communication / environmental units are fixed infrastructure.
for i,(x,y,z) in enumerate([(-49,28,7.40),(48,34,7.40)]):
    m=Mesh();m.box((x,y,z+.30),(1.1,.8,.6),'NF_Graphite');m.box((x,y,z+.63),(.9,.65,.10),'NF_Alloy');m.beam((x,y,z+.65),(x,y,z+2.1),.045,.045,'NF_Alloy');m.box((x,y,z+1.55),(.28,.12,.72),'NF_Composite')
    for yy in [-.23,0,.23]:m.box((x-.56,y+yy,z+.3),(.012,.06,.35),'NF_Grip')
    m.build('NF_ROOF_Comms_'+str(i),'17G_Roof_Communications',.007)
# A compact entry checkpoint is bolted beside the existing landing, never across it.
panel('ENTRY-07',(-3.83,-40.22,.75),.60,1.15,0,'Entry biometric / security directory')
sensor('ENTRY-07',(-5,-40.25,2.80))
textplate('Entry','AUTHORIZED ACCESS',(-5,-40.24,2.5),.16)

# Display texts become actual meshes so their export is renderer-independent.
bpy.context.window.view_layer=scene.view_layers['01_COMPLETE']
for ob in list(scene.objects):
    if ob.type=='FONT' and ob.name.startswith('NF_ID_'):
        bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH')
        if not ob.data.uv_layers:
            uv=ob.data.uv_layers.new(name='UV0_Label')
            for poly in ob.data.polygons:
                for li in poly.loop_indices:
                    v=ob.data.vertices[ob.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x/2,v.y/2)
        ob['collision_role']='non-colliding label'
def lc(layer,name):
    def walk(c):
        if c.name==name:return c
        for ch in c.children:
            q=walk(ch)
            if q:return q
    return walk(layer.layer_collection)
for key in ['02_CUTAWAY','03_GROUND_PLAN']:
    for c in ['17F_Ceiling_Systems','17G_Roof_Communications']:lc(scene.view_layers[key],c).exclude=True
# Original signatures must still match exactly: topology, vertex coordinates, world transform.
changed=[n for n,h in baseline.items() if n not in bpy.data.objects or digest(bpy.data.objects[n])!=h]
assert not changed,changed
new=[o for o in scene.objects if o.name not in original_names]
audit={'source_file':BASE.name,'source_mesh_objects_checked':len(baseline),'changed_original_meshes_or_transforms':changed,'preservation_passed':not changed,'new_mesh_objects':sum(o.type=='MESH' for o in new),'new_lights':sum(o.type=='LIGHT' for o in new),'unchanged_footprint_m':[118,80],'unchanged_hall_m':[72,44],'method':'SHA-256 of original mesh vertex coordinates, polygon indices and full world transforms before and after the retrofit. Existing materials and light properties may change.'}
(ROOT/'Preservation_validation.json').write_text(json.dumps(audit,indent=2))
manifest['scene']='Warehouse_NearFuture.blend';manifest['theme']='Grounded near-future industrial retrofit';manifest['preservation']=audit
(ROOT/'Asset_manifest.json').write_text(json.dumps(manifest,indent=2))
scene.name='WAREHOUSE | Near-future retrofit';scene['retrofit']='Additive upgrade; original mesh geometry, world transforms and routes preserved'
scene['base_asset']=BASE.name
for layer in scene.view_layers:layer.use=layer.name=='01_COMPLETE'
scene.camera=bpy.data.objects['CAM_Overview']
scene.view_settings.exposure=.30
for im in bpy.data.images:
    if im.source=='FILE':
        if not im.packed_file:im.pack()
        candidate=ROOT/'Textures'/Path(im.filepath).name
        im.filepath='//Textures/'+candidate.name if candidate.is_file() else '//Reference.png'
for o in scene.objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Warehouse_NearFuture.blend'))
exec(compile((ROOT/'close_doors.py').read_text(),str(ROOT/'close_doors.py'),'exec'))
print('UPGRADE_DONE',json.dumps(audit),flush=True)
