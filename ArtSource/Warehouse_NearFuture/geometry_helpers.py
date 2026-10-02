import bpy, math
from mathutils import Vector, Matrix
class Mesh:

    def __init__(self):
        self.v = []
        self.f = []
        self.mi = []

    def box(self, c, d, ma='Concrete', rotation=None):
        x, y, z = c
        a, b, h = [q / 2 for q in d]
        k = len(self.v)
        vv = [(-a, -b, -h), (a, -b, -h), (a, b, -h), (-a, b, -h), (-a, -b, h), (a, -b, h), (a, b, h), (-a, b, h)]
        for v in vv:
            p = Vector(v)
            p = rotation @ p if rotation else p
            self.v.append((x + p.x, y + p.y, z + p.z))
        for face in [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)]:
            self.f.append(tuple((k + i for i in face)))
            self.mi.append(ma)

    def beam(self, a, b, width=0.08, depth=None, ma='Steel'):
        a, b = (Vector(a), Vector(b))
        v = b - a
        self.box((a + b) / 2, (width, depth or width, v.length), ma, v.to_track_quat('Z', 'Y').to_matrix())

    def build(self, name, col, bevel=0.005, origin=(0, 0, 0)):
        origin = Vector(origin)
        me = bpy.data.meshes.new(name + '_Mesh')
        me.from_pydata([Vector(v) - origin for v in self.v], [], self.f)
        me.update()
        mats = list(dict.fromkeys(self.mi))
        for m in mats:
            me.materials.append(M[m])
        uv = me.uv_layers.new(name='UV0_Tile_512px_m')
        for p, m in zip(me.polygons, self.mi):
            p.material_index = mats.index(m)
            n = p.normal
            axis = max(range(3), key=lambda i: abs(n[i]))
            axes = [i for i in range(3) if i != axis]
            for li in p.loop_indices:
                v = Vector(self.v[me.loops[li].vertex_index])
                uv.data[li].uv = (v[axes[0]] / 2, v[axes[1]] / 2)
        ob = bpy.data.objects.new(name, me)
        cols[col].objects.link(ob)
        ob.location = origin
        if bevel:
            mod = ob.modifiers.new('Real edge radius', 'BEVEL')
            mod.width = bevel
            mod.segments = 2
            mod.affect = 'EDGES'
            mod = ob.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
            mod.keep_sharp = True
            mod.weight = 50
        ob['units'] = 'metres'
        ob['collision_role'] = 'architecture'
        return ob

def box(name, c, d, ma, col, bevel=0.008):
    b = Mesh()
    b.box(c, d, ma)
    return b.build(name, col, bevel, c)

def label(name, body, loc, size=0.25, rot=(math.pi / 2, 0, 0), col='02_EXTERIOR', material='Plaster'):
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = body
    cu.size = size
    cu.extrude = 0.001
    cu.align_x = 'CENTER'
    ob = bpy.data.objects.new(name, cu)
    cols[col].objects.link(ob)
    ob.location = loc
    ob.rotation_euler = rot
    cu.materials.append(M[material])
    return ob

def area(name, loc, target, power, size, color=(1, 0.78, 0.53), col='13_LIGHTING', shape='DISK', size_y=None):
    dat = bpy.data.lights.new(name, 'AREA')
    dat.energy = power
    dat.color = color
    dat.shape = shape
    dat.size = size
    if size_y and shape == 'RECTANGLE':
        dat.size_y = size_y
    ob = bpy.data.objects.new(name, dat)
    cols[col].objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return ob