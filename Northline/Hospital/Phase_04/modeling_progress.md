# Phase 4 modeling progress

[Open the Level 3 hospital model](NET-runner-92-hospital-phase-04.blend) · [Phase 3 patient wards](../Phase_03/NET-runner-92-hospital-phase-03.blend)

Phase 4 furnishes surgery and intensive care at **8.40 m**. Amber wayfinding identifies the surgical wing and blue identifies critical care. Selective equipment displays and ceiling fixtures remain powered in the shutdown scene.

## Completed

| Zone | Fit-out |
| --- | --- |
| West surgical wing | Two operating theatres and one hybrid theatre with surgical tables, dual lamp assemblies, ceiling diffusers, anaesthesia workstations and instrument tables. A mobile C-arm is parked in the hybrid theatre. The wing also has sterile supply and a dual-station scrub room. |
| East ICU wing | Two critical-care rooms and an isolation room with ensuite. Bed spaces have headwalls, ventilators and infusion pumps. An observation counter and a clean equipment room support the wing. |
| Recovery and transfer | Four post-anaesthesia recovery bays, pre-op assessment, a stretcher and wheelchair staging room, family consult and two step-down beds. |
| Clinical utilities | Surgical service, soiled hold, clean utility, anaesthesia support, critical-care support and riser equipment. |
| Circulation | Department signs, floor markings, impact rails and the west stair approach. The surgical service terminal and west stair reader provide visual targets for future access logic. |

## Verification

- Walked from the **east stair arrival** across the concourse to the **west ascending landing**. Also checked the approaches to surgical scrub, ICU observation, soiled hold and ICU clean utility.
- Swept a **0.96 × 2.52 m** stretcher envelope from the concourse into all three theatres, all three ICU rooms and the recovery aisle. The isolation ensuite also passed the foot approach and a 1.26 m diameter clearance sample at three heights.
- Reached all **28 occupiable Level 3 zones** through 15,097 walkable geometry samples. Floor-standing assets cleared the shell walls and columns. The lowest points of the surgical lamps are above 2.50 m from the floor. The ICU privacy screen interrupted the sampled lower sightline.
- Reopened the saved file in Blender 5.0.0. All **2,342 Phase 3 objects** matched the baseline names, transforms and audited mesh, light and camera data.
- A temporary Blender FBX round trip preserved the names, bounds and face material sets of all **769 Level 3 meshes** using single-user mesh data in the export copy.

## Next phase

Furnish **Level 4 restricted research**. Begin at the west stair, then develop the research rooms, checkpoint, restricted anteroom and final lab at the east end. The access readers and terminals are static assets for later Unity interaction. Runtime collision, controller handling and lighting still need engine testing.
