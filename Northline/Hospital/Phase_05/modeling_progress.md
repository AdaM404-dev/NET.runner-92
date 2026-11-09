# Phase 5 modeling progress

[Open the complete hospital in Blender](NET-runner-92-hospital-phase-05.blend) · [Phase 4 surgery and intensive care](../Phase_04/NET-runner-92-hospital-phase-04.blend)

Phase 5 furnishes Level 4 at **12.60 m** and assembles the four-floor Northline Medical environment in one Blender file. The research floor uses subdued green equipment displays and amber access markings to distinguish working consoles from the shutdown clinical spaces below.

The file opens in the complete-building view. The stacked fit-out view shows the four furnished levels and connected stairs together.

## Completed

| Zone | Fit-out |
| --- | --- |
| West research | Logistics racks and transit cases, a sample-prep wet lab, a biosafety cabinet, analysis benches, microscopes, review stations and research operations. |
| Offices and storage | Facilities telemetry, data workstations, offices, briefing space, secured cold cabinets and a restricted workroom. |
| Access and services | Checkpoint A, a gowning and handwash anteroom, interlock B, controlled records, lab support, waste hold and restricted plant utilities. |
| Final lab | A glazed automated assay line with a visible internal gantry and specimen trays, backup archive cabinets, prep equipment and a data extraction console. |

## Mission route

**Level 1 reception → west stair → Level 2 wards → east stair → Level 3 surgery and ICU → west stair → Level 4 checkpoint A → restricted anteroom → interlock B → assay archive.**

The facilities log and controlled records consoles provide optional interaction points. The assay line interrupts sightlines across the final lab, with an alternate walking route behind it. The east stair remains reachable from the restricted concourse. The Blender scene shows the access doors open for spatial review. Mission logic can drive the existing leaves and readers in Unity.

## Final verification

- Reached **108 occupiable zones** from the concourses across all four floors using 62,558 geometry samples. Checked each leg of the intended mission route, all twelve stair flights and their landing connections.
- Walked the Level 4 approach through both thresholds to the archive console. Also checked the lab support door, rear path around the assay line, optional facilities and records approaches, utility links and east stair.
- Floor-standing Level 4 assets cleared the shell walls and columns. The assay line interrupted the sampled sightline between its two sides.
- All **2,846 Phase 4 objects** matched their baseline names, transforms and audited mesh, light and camera data. The final Blender scene contains **3,020 meshes**, 181 area lights and 24 review cameras.
- A full Blender FBX round trip preserved the 3,020 mesh names, overall bounds and face material sets. The temporary export copy used single-user mesh data.

## Production handoff

Unity work begins with collision, controller and stair tests, followed by lighting and material conversion and interaction scripting. The Blender route checks establish geometric clearance. Runtime navigation, stealth behavior and access states need engine testing. See the [asset and interaction notes](asset_structure.md) for door groups and binding identifiers.

