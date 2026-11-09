# Level 3 asset structure

[Open the Blender source](NET-runner-92-hospital-phase-04.blend)

The file opens in `03 | Floor 03`. Switch to `00 | Complete Hospital` for the enclosed rooms and connected stairs. Six `F03_CAM_` views cover an operating theatre, the hybrid theatre, ICU, recovery, the surgical hall and the concourse. Operating Theatre 01 is the active render camera.

## Collections

`Floor_03 → F03_Fitout` contains the Level 3 additions.

| Collection | Contents |
| --- | --- |
| `F03_West_Surgery` | Theatre tables, ceiling lamps and diffusers, anaesthesia and imaging equipment, scrub fixtures and sterile supply. |
| `F03_East_Intensive_Care` | Critical-care beds, pumps, ventilators, isolation fixtures, observation and clean equipment. |
| `F03_Recovery_and_Transfer` | Recovery, pre-op, patient transfer, family consult and step-down. |
| `F03_Clinical_Utilities` | Surgical service, soiled hold, clean utility, anaesthesia support, critical-care support and riser equipment. |
| `F03_Circulation` and `F03_Clinical_Finishes` | Wayfinding, access reader, seating, floor finishes and route markings. |
| `F03_Practical_Fixtures`, `F03_Review_Lighting` and `F03_Review_Cameras` | Ceiling fixture meshes, 51 area lights and six review cameras. |

The fit-out adds **447 mesh objects** under `F03_FF_` and links **26 earlier mesh modules** from Levels 1 and 2. New repeated modules use linked `F03_Kit_` mesh datablocks. Room, module and collision intent are recorded on each fit-out object where applicable. Text is baked to mesh.

## Unity handoff

Use the complete view for selected mesh export. Make linked mesh data single-user in a temporary copy before FBX export to avoid Blender 5.0 material-slot warnings. Keep beds, carts, pumps and the C-arm separate where gameplay may move them. Use the original slab for collision below the finish overlays and preserve the stair and lift voids. Blender lighting and procedural finishes serve as visual references for the Unity setup.
