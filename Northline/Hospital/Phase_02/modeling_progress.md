# Phase 2 modeling progress

[Open the furnished hospital in Blender](NET-runner-92-hospital-phase-02.blend) · [Phase 1 shell](../NET-runner-92-hospital.blend)

Phase 2 covers Level 1, from the public and ambulance approaches through reception, emergency, diagnostics and outpatient care. The original structural model is the reference for room boundaries, openings and floor levels. The fit-out uses the fictional **Northline Medical** identity and English wayfinding.

## Completed

| Zone | Fit-out |
| --- | --- |
| Public intake | Reception counter with a lower service section and knee recess, paired waiting seats, check-in kiosks, parked wheelchair, lift wayfinding and accessible WC fixtures. |
| West emergency | Triage interview and exam stations, a credential reader and local terminal, two fast-track exam stations, three resuscitation bays, three observation bays and a decontamination station. |
| Imaging and diagnostics | CT gantry and patient table, digital radiography system, operator desks, diagnostic casework and riser cabinets. |
| East outpatient | Admissions desks, consult and treatment stations, staff workstations, clean storage and support casework. |
| Circulation and appearance | Department signs, floor route markings, protective wall rails and selective practical lighting. Closed-intake signage, dark equipment screens and displaced furniture establish the shutdown state. |

## Route and geometry checks

- Walked the public approach to reception and the concourse, then sampled the turn into triage and both end stair landings against the saved geometry. Also sampled triage to decontamination and outpatient consult to its support room.
- Swept a **0.96 × 2.52 m** stretcher envelope from the north apron through the ambulance vestibule and resuscitation to the concourse. A second sweep checked the turn from resuscitation into observation.
- Reached all **22 occupiable ground-floor zones** from the concourse using 17,508 walkable geometry samples. No floor-standing fit-out mesh intersected a shell wall or column.
- Sampled two stealth sightlines. The reception counter hid a crouched target from the entrance-side viewpoint while exposing a standing target. The privacy screen interrupted a standing sightline from the triage doorway.
- Reopened the finished file in Blender 5.0.0 and compared it with the baseline. All **1,335 original shell meshes** retained their names, transforms, topology and material slots.

## Open items and next phase

The triage reader and standby terminal are visual interaction targets. Door state, access rules and mission scripting are Unity work. The clearance results are Blender geometry samples. Runtime colliders, player-controller handling, stair movement, performance and lighting still need an engine check.

Continue with **Level 2 patient wards**. Reuse the beds, headwalls, carts and privacy screens in the west and east ward pods. Develop the ensuite fixtures and staff stations, then verify the west-to-east stair route with furniture in place.
