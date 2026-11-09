# First-person arms and LATCH-07

This standalone Blender 5.0 scene contains the operative's two arm and hand meshes, thin technical gloves, fitted sleeves, a left forearm interface, a finger-articulated rig, seven actions and a dedicated first-person preview.

The arm meshes retain the anatomy and normalized weights of the existing Phase 14 operative. They were cropped above the visible biceps, closed at the shoulder cut and reassigned to an arm-only skeleton. Fabric and glove regions share each arm's underlying mesh so the wrist does not have a geometric seam. Separate geometry defines cuff binding, elbow gussets, seam welts and the dorsal glove panels. The palette uses matte graphite textiles, satin dark titanium, insulating ceramic and cyan and amber indicators.

## Blender collections

| Collection | Contents |
| --- | --- |
| Character / LeftArm and RightArm | Continuous sleeve and glove surface on each side |
| Character / Clothing | Editable gussets, cuffs, seams and dorsal glove details |
| HackingDevice / MainHousing | LATCH-07 chassis, isolation gasket and permanent marks |
| HackingDevice / Screen | Display carrier, backplate and dedicated UV0 screen |
| HackingDevice / MechanicalParts | Protective guards, slides, fasteners, sensor housing and two webbing assemblies |
| HackingDevice / IndicatorLights | Cyan radio and amber standby lenses |
| Rig | 41-bone armature with 30 finger joints and dedicated device bones |
| Animation | Collection for animation helpers. The seven actions are on the rig's named NLA tracks. |
| Materials | Collection for material review objects. The actual materials are named datablocks. |
| PreviewCamera and PreviewEnvironment | 80° horizontal FP camera, lights, corridor, temporary robot silhouette and removable boot display |

The **Textures** folder contains twenty 512 × 512 PNGs, four maps per surface for Fabric, Glove, Grip, Titanium and Ceramic. Base Color is sRGB. Roughness, Metallic and tangent-space +Y Normal are linear. The Blend packs those maps. The FBX also embeds the images. Glass, small hardware finishes, labels and status lights use constant material values.

The FBX consolidates related rigid details and garment overlays into nine named mesh assemblies. Blender retains the editable parts individually. The export contains no corridor, robot silhouette, camera or preview lettering.

[Blender project](../NETRunner_FP_Arms.blend) · [FP ready pose](../Renders/01_FirstPerson_Ready.png) · [Front model view](../Renders/05_Model_Front.png) · [Side model view](../Renders/06_Model_Side.png)
