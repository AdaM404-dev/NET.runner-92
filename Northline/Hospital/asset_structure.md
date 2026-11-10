# Blender asset structure

The native file is [NET-runner-92-hospital.blend](NET-runner-92-hospital.blend). It contains one scene, eleven simple working materials and 1,335 mesh objects. Geometry is in metres at unit scale 1.0, with +X east, +Y north and +Z up. The origin is at ground level near the centre of the main concourse.

| Root collection | Contents |
| --- | --- |
| `Hospital_Exterior` | `Exterior_Entry_Structures` holds both entrance wing roofs, canopies, posts and grade landings. `Exterior_Roof` holds the main roof, perimeter parapets and the setback roof parapet. |
| `Floor_01` | Ground slab and reception/emergency envelope and partitions. Includes the two shaft pits and the flights departing from Level 1. |
| `Floor_02` | Ward slab and architecture, including the ward hall partitions and ensuites, with stair flights departing from Level 2. |
| `Floor_03` | Surgery/ICU slab and architecture, the south setback transfer beams below Level 4 and flights departing from Level 3. |
| `Floor_04` | Research slab, setback envelope, anteroom, checkpoint and top-floor stair guards. |

Each floor has its own `F##_Structure`, `F##_Envelope`, `F##_Partitions`, `F##_Stairs`, `F##_Doors`, `F##_Windows`, `F##_Columns` and `F##_Elevator_Shafts` collections. Ceiling beams are nested under `F##_Structure` and hidden by default in the individual floor view layers. Use the complete view layer for connected stairs and entry-wing roofs. The floor layer shows its slab and the flights rising from it. Numpad 7 switches the viewport to top view.

## Naming and modular edits

- Floor meshes begin with `F01_` through `F04_`. Shared exterior structures use `EXT_`.
- Walls carry the names of their adjacent spaces. Wall, door and window objects include `side_negative`, `side_positive` and `floor` custom properties. The side names are relative to increasing X or Y across the wall.
- Door assemblies have separate `_Frame` and leaf meshes. Hinged leaves have local origins on their hinge axes. Door frames carry nominal clear width, clear height and opening style. Each window has separate frame and glazing meshes with a common origin. Wall openings are cut into the wall topology, so changing an opening size also requires editing the corresponding wall mesh.
- Floor slabs are native meshes with physical voids. Stair flights, half-landings, bearing beams and guards are separately editable. Columns are individually named and carry their grid coordinates.
- Working materials distinguish concrete, clinical partitions, door leaves, framing and glass. The scene has no external texture dependencies.

## Unity handoff

Keep this Blender file as the editable source and export from the complete view layer. For a floor export, select the intended collections from the complete view so the ceiling beams are included. Keep movable door leaves separate, group static geometry by floor and verify metric scale and axis conversion on import. The Blender FBX smoke test used −Z forward and Y up. A 4.20 m floor interval and the 175 mm stair rise provide quick scale references.

Prepare simplified collision for floors, openings and stairs before testing the first-person controller. Preserve stair and shaft voids in collision, and decide where to use separate glass colliders and door triggers. The current file preserves the architectural parts as source meshes for that work.
