# Phase 3 modeling progress

[Open the Level 2 hospital model](NET-runner-92-hospital-phase-03.blend) · [Phase 2 ground floor](../Phase_02/NET-runner-92-hospital-phase-02.blend)

Phase 3 furnishes the patient ward level at **4.20 m**. The west and east pods use the same Northline Medical clinical kit, with warmer west-side markings and cooler east-side markings. Vacant bed bays in W203 and E205 and a mix of powered and dark fixtures convey the shutdown state.

## Completed

| Zone | Fit-out |
| --- | --- |
| West W201–W203 and east E204–E206 | Six patient rooms with ten beds, headwalls, bedside lockers, overbed tables and selective privacy screens. Beds face the side walls to keep the north glazing clear. |
| Six ensuites | Toilets, transfer rails, compact basins, shower fittings and flush drains. |
| Ward staff and utilities | Two staff counters, clean linen racks and trolleys, ward administration, riser cabinets and both rear utility rooms. |
| Patient support | Two day rooms, a patient lounge, family consult, gait-training rails and steps, therapy stations and a treatment bay. |
| Circulation | Ward thresholds and route markings, corridor wall protection, east stair wayfinding and a transfer console and reader for future progression. |

## Traversal and geometry checks

- Followed the Level 2 landing from the **west stair** across the concourse to the **east stair**, including the turn onto the ascending landing. Also checked both day-room-to-utility links and the east ward staff approach.
- Swept a **0.96 × 2.52 m** stretcher envelope from the concourse into all six patient rooms, including the turns at the two ward junctions. The model leaves the central corridor and those turning areas clear.
- Walked all six ensuite approaches. Each cleared a **1.26 m diameter** sample inside the room at three heights. These samples document modeled clearance for later controller and accessibility review.
- Reached all **34 occupiable zones** through 14,896 walkable geometry samples. Floor-standing fit-out meshes cleared the structural walls and columns. The W201 privacy screen interrupted the sampled lower sightline.
- Reopened the saved file in Blender 5.0.0. All **1,768 Phase 2 objects** matched their baseline names, transforms and audited data, including mesh topology, material assignments, light settings and camera optics.

- A temporary Blender FBX round trip preserved the names, bounds and face material sets of all **866 Level 2 meshes**. The export copy used single-user mesh data to avoid material-slot warnings from the FBX exporter.

## Next phase

Furnish **Level 3 surgery and intensive care**. Develop the theatre equipment and scrub or sterile support, then fit out the ICU, isolation and recovery bays. Verify the east-to-west stair route and patient transfers with equipment in place. Unity work includes simplified collision, controller tests and gameplay logic for the static ward transfer console and stair reader.

