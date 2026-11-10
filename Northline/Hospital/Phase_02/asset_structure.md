# Level 1 fit-out asset structure

The Phase 2 Blender file opens in `01 | Floor 01` for inspection. Switch to `00 | Complete Hospital` to review the enclosing slabs and first-person views. The active render camera shows reception. The other five `F01_CAM_` cameras cover the concourse, ambulance approach, triage, resuscitation and CT. All dimensions use metres.

## Collections

`Floor_01 → F01_Fitout` contains the entire ground-floor addition.

| Collection | Contents |
| --- | --- |
| `F01_Public_Reception` | Reception desk, lobby seats, kiosks, WC and entrance identity. |
| `F01_West_Emergency` | Triage, fast track, resuscitation, observation and decontamination. |
| `F01_Diagnostics` | CT, radiography, diagnostic support and riser cabinets. |
| `F01_East_Outpatient` | Admissions, consult, treatment, storage and staff support. |
| `F01_Circulation` | Concourse and lift wayfinding, wall seats and triage reader. |
| `F01_Clinical_Finishes` | Thin floor surfaces and route markings. |
| `F01_Practical_Fixtures` | Ceiling fixture meshes and visible diffusers. |
| `F01_Review_Lighting` and `F01_Review_Cameras` | Forty Blender area lights and six review viewpoints. |

The 387 added mesh objects use the prefix `F01_FF_`. Repeated furniture and equipment use linked `F01_Kit_` mesh datablocks. Moving an object changes that placement. To make a room-specific geometry edit, make its mesh data single-user first. Wayfinding and screen text are baked to meshes and have no font dependency.

Fit-out objects carry `room`, `module` and `collision_role` properties where applicable. `Static` and `Movable` identify floor-standing geometry for collision planning. `Surface overlay` marks finishes and lane graphics that sit just above the structural slab. `Review only` identifies labels, wall fixtures and other pieces that can be omitted from collision. These properties are authoring metadata.

## Unity preparation

Export mesh collections from the complete view with door leaves and movable furniture selected separately as needed. Use the original slab for collision beneath the floor finishes. Build and test simplified colliders for the counter, cabinets, beds and carts. Apply Unity-side lighting and material setup to the clinical finishes. The Blender noise and bump nodes serve as source material references and would need baking or recreation for engine parity.
