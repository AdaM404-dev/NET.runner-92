import math

import bpy
import numpy as np


def save_map(directory, name, data, linear):
    height, width, channels = data.shape
    rgba = np.ones((height, width, 4), dtype=np.float32)
    rgba[:, :, :channels] = np.clip(data, 0, 1)
    image = bpy.data.images.new(name, width, height, alpha=False)
    image.colorspace_settings.name = "Non-Color" if linear else "sRGB"
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(directory / (name + ".png"))
    image.file_format = "PNG"
    image.save()
    image.pack()
    image.filepath = "//Textures/" + name + ".png"
    return image


def plain(name, color, rough, metal=0, emission=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    p = mat.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    if emission:
        p.inputs["Emission Color"].default_value = (*color, 1)
        p.inputs["Emission Strength"].default_value = emission
    return mat


def make_materials(directory):
    directory.mkdir(exist_ok=True, parents=True)
    size = 512
    y, x = np.mgrid[0:size, 0:size] / size * math.tau
    rng = np.random.default_rng(2087)
    grain = rng.normal(0, 1, (size, size))
    cloud = np.sin(5*x+4*y+.5*np.sin(3*y))*.44 + np.sin(11*x-7*y)*.31 + np.cos(3*x-4*y)*.25
    weave = np.sin(54*x+1.1*np.cos(54*y)) * np.sin(54*y+.75*np.sin(54*x))
    diagonal = np.sin(66*x+66*y)*.65+np.cos(65*x-65*y)*.35
    brushing = np.cos(91*y+.18*np.sin(4*x))*.48 + np.cos(121*y)*.28 + np.cos(37*y-.1*np.cos(3*x))*.24
    mats = {}
    variants = {
        "Fabric": ((.151, .171, .192), .77, 0., weave, .013),
        "Glove": ((.172, .188, .203), .64, 0., diagonal, .011),
        "Grip": ((.106, .119, .130), .87, 0., diagonal, .014),
        "Titanium": ((.274, .301, .319), .43, .74, brushing, .024),
        "Ceramic": ((.135, .159, .170), .57, .10, grain*.23, .012),
    }
    for kind, (color, rough, metal, pattern, amplitude) in variants.items():
        mat = plain("NR92_" + kind, tuple(v**2.2 for v in color), rough, metal)
        nt = mat.node_tree
        shader = nt.nodes.get("Principled BSDF")
        shade = cloud*.012 + pattern*amplitude + grain*.004
        color_image = save_map(directory, kind+"_BaseColor", np.asarray(color)[None,None,:]+shade[:,:,None], False)
        rough_image = save_map(directory, kind+"_Roughness", (rough+cloud*.035+pattern*.026)[:,:,None], True)
        metallic_image = save_map(directory, kind+"_Metallic", np.full((size,size,1), metal), True)
        height = (pattern*.065+cloud*.025+grain*.008) * (.56 if kind in ("Titanium", "Ceramic") else 1)
        dx = (np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*1.25
        dy = (np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*1.25
        normal = np.stack((-dx,-dy,np.ones_like(dx)),axis=-1)
        normal /= np.linalg.norm(normal, axis=2, keepdims=True)
        normal_image = save_map(directory, kind+"_Normal", normal*.5+.5, True)
        for index, (image, pin) in enumerate(((color_image,"Base Color"),(rough_image,"Roughness"),(metallic_image,"Metallic"),(normal_image,"Normal"))):
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = image
            tex.location = (-550, 340-index*250)
            if pin == "Normal":
                n = nt.nodes.new("ShaderNodeNormalMap")
                n.location = (-190, -380)
                n.inputs["Strength"].default_value = .34 if kind == "Titanium" else .43
                nt.links.new(tex.outputs["Color"],n.inputs["Color"])
                nt.links.new(n.outputs["Normal"],shader.inputs[pin])
            else:
                nt.links.new(tex.outputs["Color"],shader.inputs[pin])
        if kind in ("Fabric","Glove","Grip"):
            shader.inputs["Sheen Weight"].default_value = .12
        mats[kind] = mat
    mats.update({
        "Thread": plain("NR92_Thread", (.045,.057,.065), .82),
        "Edge": plain("NR92_Satin_Steel", (.16,.21,.24), .33,.68),
        "Label": plain("NR92_Etched_Label", (.36,.45,.48), .61,.2),
        "Accent": plain("NR92_Ceramic_Orange", (.48,.122,.024), .51,.15),
        "Screen": plain("NR92_Display_Surface", (.006,.012,.016), .48,.04),
        "LED": plain("NR92_Status_Cyan", (.075,.66,.68), .31,.08,2.0),
        "UI": plain("Preview_Phosphor", (.19,.87,.81), .72,0,1.25),
        "UI_Dim": plain("Preview_Faded_Phosphor", (.06,.34,.33), .75,0,.65),
        "Amber": plain("NR92_Standby_Amber", (.67,.23,.04), .48,.1,1.35),
    })
    return mats
