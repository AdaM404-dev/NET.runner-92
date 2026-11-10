# Complete hospital asset structure

[Open the Blender source](NET-runner-92-hospital-phase-05.blend)

The file opens in `00 | Complete Hospital` with all four furnished levels stacked at their original elevations and linked by the two stair cores. The `Northline_Hospital` collection contains the exterior and all four floor collections. Choose `05 | Stacked Fit-Out` to see the furniture on all four levels with the roof, walls and window assemblies hidden in that view. The individual floor views isolate each level for editing. The six `F04_CAM_` viewpoints cover the final lab, archive console, checkpoint, anteroom, sample preparation and facilities control.

## Level 4 collections

`Northline_Hospital → Floor_04 → F04_Fitout` holds 336 added meshes, 37 area lights and six review cameras.

| Collection | Contents |
| --- | --- |
| `F04_West_Research` | Research logistics, sample prep, analysis, review and operations. |
| `F04_Restricted_Suite` | Controlled records, gowning anteroom, lab support, automated assay line and archive console. |
| `F04_Research_Offices_and_Stores` | Facilities control, data analysis, offices, meeting room, secure sample storage and restricted workroom. |
| `F04_Plant_and_Utilities` | Waste hold, restricted plant and utility equipment, and riser access. |
| `F04_Circulation` and `F04_Clinical_Finishes` | Checkpoint reader, route markings, floor finishes and stair and department wayfinding. |
| `F04_Practical_Fixtures`, `F04_Review_Lighting` and `F04_Review_Cameras` | Fixture meshes, area lights and review viewpoints. |

Objects use the `F04_FF_` prefix. Repeated geometry uses `F04_Kit_` or one of eight linked mesh modules from earlier fit-outs. Text is baked to mesh. The final scene has no external texture or font dependencies.

## Interaction bindings

The following meshes carry an `interaction_id` custom property for downstream binding.

| Identifier | Object or associated door geometry |
| --- | --- |
| `F04_CHECKPOINT_A` | `F04_FF_112_Checkpoint_A_Reader`. The six moving leaves share the `F04_D03_Restricted_Concourse_` prefix. |
| `F04_INTERLOCK_B` | `F04_FF_128_Interlock_B_Reader`. The two moving leaves share the `F04_D25_Final_Research_Lab_` prefix. |
| `F04_ASSAY_ARCHIVE` | `F04_FF_139_Assay_Data_Terminal`, the proposed final extraction target. |
| `F04_RECORDS_REVIEW` | `F04_FF_169_Records_Console`, an optional review target. |
| `F04_FACILITIES_LOG` | `F04_FF_221_Facilities_Telemetry_Console`, an optional service log target. |

## Export and integration

Use the complete view and select mesh collections for export. Make shared mesh data single-user in a temporary export copy to avoid Blender 5.0 FBX material-slot warnings. The full round trip used −Z forward and Y up. Verify the 4.20 m floor spacing and 175 mm stair rise on import. Keep door leaves and movable equipment separate from static room geometry. Use original slabs for collision beneath the finish overlays, and preserve the stair and lift openings. Translate the Blender lighting and procedural materials to the target Unity render pipeline.

