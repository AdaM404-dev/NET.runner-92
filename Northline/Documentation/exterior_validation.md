# Exterior validation

## Navigation and boundary

Clearance checks used Blender's evaluated geometry. The pedestrian envelope uses five lateral offsets spanning 0.68 m and four heights up to 2.08 m. The rolling vehicle probe uses a 2.35 m transverse envelope and samples three heights up to 2.40 m. These are discrete clearance probes along the listed paths.

| Sampled route | Body rays | Ground probes | Result |
| --- | ---: | ---: | --- |
| Inside public gate to reception threshold | 60 | 101 | Clear |
| West stair egress | 60 | 349 | Clear |
| East stair egress | 60 | 342 | Clear |
| Ambulance vestibule to resuscitation | 60 | 144 | Clear |
| Decontamination shelter aisle | 60 | 80 | Clear |
| Maintenance reader approach | 40 | 42 | Clear to the locked gate |
| Parked ambulance rear approach | 40 | 25 | Clear to the loading position |
| Road vehicle approach to ambulance canopy | 60 | 106 | Clear |

Ground samples found surfaces from −33 mm to +6 mm relative to the original landing grade. The perimeter proxies blocked **4,380** crossing rays, at three body heights and 0.25 m horizontal intervals around all four sides. Welded fence, concrete screening and closed gate leaves define the same visible boundary. A separate overlap check found no above-grade new scenery inside the three protected building and canopy footprints.

The first pass found an east-path conflict at the public PTZ mast and a low screening header. The mast moved onto a marked island and the shelter headers were raised. The rear-ambulance approach was resampled from the lane side to end clear of the step and bodywork. Added bearing pads bed the two crash blocks that straddle gravel grade.

## Scene and export checks

The exterior contains **3,638 objects**, including **3,608 mesh placements** backed by **846 unique mesh datablocks**. **2,981 placements** use shared data. The exterior has approximately **907,000 source triangles** across instanced placements before bevels and other evaluated geometry. Background, weather, props, fixtures and collision are separately selectable. The 11-mesh FBX sample preserved names and transformed bounds within the 35 mm comparison tolerance. It included pavement, fence infill, concrete screening, gate, typography, shelter, ambulance body and wheel, and two physics proxies.

Unity runtime controller tests, vehicle dynamics, lighting, material baking, access triggers and performance checks remain open. The closed perimeter gates are staged geometry. Starting inside them preserves the visible boundary without an invisible entry wall.

## Hospital preservation

Reopened the deliverable in Blender 5.0. The complete-building layer matched the baseline for **3,850 original objects**, including **3,626 original meshes**. The comparison covered world transforms, collection memberships, parents, custom properties, mesh coordinates and topology, and material slots. The saved file opens in `06 | Quarantine Grounds` with the main entrance camera. It has no linked Blender libraries or external image and font dependencies.

## Reviewed renders

Cycles OptiX rendered the views with up to 24 adaptive samples and denoising. The overhead view uses 1920 × 1540. The other views use 1920 × 1080.

| View | What was checked |
| --- | --- |
| [Establishing view](../Renders/Phase_04_Exterior/01_Establishing.png) | Scale of the hospital against the property, fence and screening court. The draft framing was widened. |
| [Main entrance and scanner](../Renders/Phase_04_Exterior/02_Main_Entrance.png) | Visible entrance, public crossing, portal and open landing. |
| [Ambulance staging](../Renders/Phase_04_Exterior/03_Ambulance_Staging.png) | Both vehicles on grade, wheel and door scale, distinct staging bays and unobstructed lane. Moved the wayfinding board between the bays and increased contrast on vehicle lettering. |
| [First-person screening court](../Renders/Phase_04_Exterior/04_Player_Screening.png) | Sightlines under shelter signs, supported frames, open aisle and localized decay. |
| [Restricted checkpoint](../Renders/Phase_04_Exterior/05_Restricted_Checkpoint.png) | Closed gate, visible placards and localized red lighting. Raised the main placard over its gate rail and moved the reader placard forward of its support. |
| [Night atmosphere](../Renders/Phase_04_Exterior/06_Night_Atmosphere.png) | Pooling light, dark fixtures and a legible main approach. |
| [Overhead site](../Renders/Phase_04_Exterior/07_Overhead.png) | Full perimeter, emergency loop, public plaza and visitor bays. |
| [Status monument](../Renders/Phase_04_Exterior/08_Status_Monument.png) | Full letterforms, frame, plinth, planter and status indicators. |

## Viewport follow-up

The initial saved exterior view exposed the 12 collision proxies in Solid mode. They were already omitted from the Cycles renders. The corrected file excludes the proxy collection in all view layers and sets its objects to wire display when enabled for editing. Reopening the corrected file confirmed the two visible ambulance meshes and fence infill remained visible with the proxies excluded. Use the Rendered viewport or a Cycles render to see scene lighting, fog and material response.
