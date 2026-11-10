# Level 2 asset structure

[Open the Blender source](NET-runner-92-hospital-phase-03.blend)

The file opens in `02 | Floor 02`. Switch to `00 | Complete Hospital` for the enclosed first-person views and stair connections. The six `F02_CAM_` cameras cover the west and east patient rooms, west ward hall, main concourse, patient lounge and rehabilitation. The west patient room is the active render camera.

## Collections

`Floor_02 → F02_Fitout` contains the Level 2 additions.

| Collection | Contents |
| --- | --- |
| `F02_West_Ward_Pod` and `F02_East_Ward_Pod` | The six patient rooms and ensuites, local staff counters, clean utility stations and corridor wall protection. |
| `F02_Patient_Support` | Day rooms, family consult, lounge, rehabilitation, therapy and treatment. |
| `F02_Administration_and_Utilities` | Ward administration, utility hub, rear utility rooms and riser equipment. |
| `F02_Circulation` | Concourse and stair signs, wall seating, lift identification and east stair reader. |
| `F02_Clinical_Finishes` | Thin floor finishes, bed bay zones and route markings. |
| `F02_Practical_Fixtures` | Ceiling light housings and diffusers. |
| `F02_Review_Lighting` and `F02_Review_Cameras` | 53 area lights and six first-person review views. |

The fit-out adds **515 mesh objects** under the `F02_FF_` prefix and links **17 mesh modules** from the Level 1 kit, including beds, headwalls, privacy screens, carts, chairs and WC fittings. New repeated assets use shared `F02_Kit_` mesh datablocks. Make a mesh single-user before changing its geometry for one room. Text is baked to meshes.

The `room`, `module` and `collision_role` properties describe the intended room and use. `Static` and `Movable` identify objects for collision planning. `Surface overlay` marks thin finish geometry above the original slab. `Review only` marks visual elements such as signage, fixtures and wall protection. Include wall protection in gameplay collision where its projection matters.

For a Blender FBX export, select mesh collections in the complete view and make linked mesh data single-user in a temporary export copy. Direct FBX export of shared mesh datablocks reported material-slot warnings in Blender 5.0.0. The single-user copy round-tripped all 866 Level 2 mesh names, bounds and face material sets. Use the structural slab for walkable collision and keep beds, carts and other movable equipment separate. The Blender area lights and procedural finishes provide the visual reference for the engine setup.

