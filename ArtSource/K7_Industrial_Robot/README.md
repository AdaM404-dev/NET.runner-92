# K-7 Industrial Humanoid — enemy face revision

An articulated industrial enemy robot based on the supplied K-7 character sheet. The revised head has a heavy angular brow, narrow down-angled sensor slits, an asymmetric ranging camera, and a red hostile default. All four gameplay light states remain controllable.

This is a procedurally authored, functional game-asset iteration. Its surface complexity and visual fidelity are an approximation of the reference, not a hand-authored AA/AAA reproduction. Blender geometry, rig and FBX round-trip checks were completed. The Unity Editor is not available in this environment, so the included Unity integration has not been compiled or play-tested in Unity. Final engine acceptance and art approval remain open.

## Start here

- `Blender/K7_Industrial_Robot.blend`: complete Blender 5.0 project. Image maps and the source reference are packed. Default pose is frame 1; lower LOD collections and authoring helpers are hidden.
- `FBX/K7_Robot.fbx`: one skeleton, all four visual LODs, gameplay sockets and 15 collider proxies. This is the file used by the Unity setup menu.
- `FBX/K7_LOD0.fbx` through `K7_LOD3.fbx`: optional independent exports. Each is a complete alternative with its own copy of the skeleton; do not put all four independent files in one prefab.
- `FBX/K7_ArticulationCheck.fbx`: skeleton/attachment animation diagnostic. No finished walk/run or combat animation library is implied.
- `Textures/4K` and `Textures/2K`: shared image atlas at both resolutions.
- `Unity/Editor/K7AssetSetup.cs`: explicit prefab/material/LOD/collider setup action.
- `Unity/Runtime/K7Robot.cs`: light-state and armor-visibility helpers.
- `Previews/K7_Overview.jpg`: turnaround, revised face, back interface and all light states.
- `Validation`: measured geometry, UV and FBX import results.
- `Source`: reproducible authoring scripts and rebuild instructions.

## Geometry and LODs

| Level | Triangles | Vertices | Relative to LOD0 | Visual objects |
|---|---:|---:|---:|---:|
| LOD0 | 128,304 | 65,080 | 100.0% | 12 |
| LOD1 | 67,992 | 34,924 | 53.0% | 12 |
| LOD2 | 32,712 | 17,284 | 25.5% | 12 |
| LOD3 | 14,744 | 8,300 | 11.5% | 12 |

Counts exclude collision proxies and presentation objects. All meshes are triangulated. There are 7 visual materials and 12 visual objects at each LOD. LODs reuse the same atlas and 52-bone skeleton. Blender displays LOD0 by default; the other LODs are deliberate distance variants.

Start with screen-relative Unity transition heights `0.55`, `0.28`, `0.12`, `0.035`; below the last threshold the asset is culled. Profile these in the target camera and game. Cross-fading is off by default, avoiding extra shader variant requirements. Near-camera close-ups should use LOD0. LOD2/3 retain small submillimeter height changes from reduction.

The ten detachable/replaceable armor objects are `Armor_Chest`, `Armor_Back`, and `Armor_Shoulder_L/R`, `Armor_Forearm_L/R`, `Armor_Thigh_L/R`, `Armor_Shin_L/R`. Lower variants append `_LOD1`, `_LOD2`, `_LOD3`. The underlying frame remains visible after hiding these plates. `K7_HackInterface` is an independent skinned object on the Chest bone at every LOD.

## Units, axes and sockets

LOD0 measures **2.000 m** from the floor to the head. Bind-pose floor is Blender Z = 0. Root is (0, 0, 0). Visual mesh and rig object scales are (1, 1, 1), with baked mesh transforms. Blender is Z-up, facing -Y. FBX is exported with -Z forward / Y up for the intended Unity +Z-forward convention. The setup enables Unity `bakeAxisConversion` and file units at importer scale 1. Check the prefab facing and a 2 m measuring object in the target Unity project.

Sockets are bone-parented transforms, preserved by keeping Optimize Game Objects disabled. Their local +Z points outward along the intended interaction/look direction; the back HackPoint faces rearward.

| Socket | Parent | Blender bind position, meters |
|---|---|---|
| `HeadPoint` | `Head` | (0.000, 0.000, 2.000) |
| `VisionOrigin` | `Head` | (0.000, -0.139, 1.930) |
| `AudioOrigin` | `Chest` | (0.000, -0.065, 1.738) |
| `HackPoint` | `Chest` | (0.000, 0.242, 1.630) |
| `ChestPoint` | `Chest` | (0.000, -0.160, 1.600) |
| `Hand_L_Point` | `Hand_L` | (0.380, -0.050, 1.006) |
| `Foot_L_Point` | `Foot_L` | (0.155, -0.065, 0.000) |
| `Hand_R_Point` | `Hand_R` | (-0.380, -0.050, 1.006) |
| `Foot_R_Point` | `Foot_R` | (-0.155, -0.065, 0.000) |

Use `VisionOrigin.forward` for sensor rays after engine validation. `HackPoint` is just outside the upper-back service panel. `AudioOrigin` is near the collar. Hand sockets mark palm interaction points; foot sockets mark contact positions in the bind pose. `Root` is the skeleton's ground-origin bone.

## PBR textures and materials

Every supplied map is 4096×4096 in `4K`, with a 2048×2048 copy in `2K`.

| File | Interpretation |
|---|---|
| `K7_BaseColor.png` | sRGB color; muted paint, industrial markings and restrained wear |
| `K7_Normal.png` | Linear tangent-space +Y normal map; import as Normal Map |
| `K7_Metallic.png` | Linear metallic scalar |
| `K7_Roughness.png` | Linear roughness; 0 smooth, 1 rough |
| `K7_AO.png` | Linear ambient occlusion; occlusion strength 0.7 suggested |
| `K7_MetallicSmoothness.png` | Unity packed texture: **R = metallic, A = 1 − roughness**, G/B unused |

Materials: `MAT_K7_Armor`, `MAT_K7_Internal`, `MAT_K7_Metal`, `MAT_K7_Rubber`, `MAT_K7_SensorGlass`, `MAT_K7_Cables`, `MAT_K7_StatusLight`.

All final visible surface shading uses standard image textures and Principled PBR values. The packed Blender reference is presentation-only. Source procedural shaders are not needed by the exported model. Sensor glass is opaque dark optical glass for predictable real-time rendering; no transparent sorting is required. Emission is a separately controlled material, not a baked state color in the body albedo. Its atlas texels are unused by the final status-light shader.

The sensor defaults to **Hostile** in the Blender scene and Unity helper. `SetState` supports cold white Patrol, amber Suspicious, red Hostile and blue Hacked. Intensity defaults to 5. Runtime changes use a per-material `MaterialPropertyBlock` on all LODs and do not mutate shared materials. Bloom, if desired, is a camera/render-pipeline effect configured by the game.

## Unity setup (URP or Built-in)

1. Extract the separate `K7_Unity_Import.zip` into a Unity project, retaining its `Assets/K7_Industrial_Robot` path. Alternatively copy only this package's `FBX`, `Textures`, and `Unity` folders beneath one directory in `Assets`. Keep the `.blend` and authoring sources outside `Assets` to avoid Unity launching Blender for automatic conversion.
2. Allow scripts to compile, then select `FBX/K7_Robot.fbx` in the Project window.
3. Run **Tools → K7 → Build Prefab from Selected FBX**. This configures Generic import, file units, axis conversion, imported normals/Mikk tangents, material texture channels, LODGroup membership, collision proxies and gameplay socket references. It creates `Materials` and `Prefabs/K7_Industrial_Robot.prefab` beneath the selected package.
4. Place the prefab at identity transform. Verify its 2 m height, +Z facing, normal-map orientation, socket directions, light colors and LOD transitions under the actual game lighting.
5. Preview the diagnostic clip from `K7_ArticulationCheck.fbx`, or assign your Generic AnimatorController. Root motion is not authored in the diagnostic. The setup keeps the default prefab free of an automatically playing test controller.
6. Add the game's navigation, AI, health, hacking logic, movement capsule and Rigidbody/CharacterController as appropriate. These gameplay systems are outside this asset package.

The setup detects URP Lit or Built-in Standard. HDRP material creation is not automated. For HDRP, create corresponding Lit materials and repack channels as required by the project. The Editor menu is explicit and operates on the selected K7 asset only. It intentionally retains bones and sockets instead of optimizing them out.

Each `COL_*` object is a closed box proxy parented to the corresponding skeleton bone. The setup replaces its visible mesh components with a trigger BoxCollider. There are head, torso, pelvis, upper-arm, forearm, hand, thigh, shin and foot proxies. Trigger callbacks still require a suitable project physics configuration. These are hit-detection preparation, not a configured ragdoll or detailed mesh colliders.

Example runtime use:

```csharp
using Hexacorp.K7;
K7Robot robot = enemy.GetComponent<K7Robot>();
robot.SetState(K7Robot.SensorState.Hacked);
Transform target = robot.hackPoint;
robot.SetArmorVisible("Armor_Chest", false); // applies to all LODs
```

## Rig and animation

The 52-bone skeleton uses 100% rigid weights. Each mechanical part follows one bone. Fingers have three articulating segments each, including opposing thumbs. Armor does not stretch. The bind pose is a relaxed A-pose; Generic is the supplied default. Humanoid retargeting requires manual Avatar mapping and a proper T-pose conversion in Unity; it has not been certified in this build.

In Blender, select `K7_Rig` and set the custom properties `FootIK_L/R` or `HandIK_L/R` from 0 to 1 to enable the optional two-bone IK constraints. Unhide `K7_ANIMATION_CONTROLS` for targets and poles. FK is the default. Independent Head and Neck joints support scanning. Head local travel limits are approximately yaw ±75°, pitch −30°/+35°, roll ±20°. The optional head tracking constraint is off by default. Blender authoring controls are excluded from FBX; animate/bake the deform skeleton for export or use Unity Animation Rigging on the supplied bones and sockets.

`K7_ArticulationCheck` is a 300-frame, 30 fps proof sequence: head scans at frames 30/60, a shallow crouch and reach at 90, closed-hand reach at 150, left/right leg and toe lifts at 210/270, rest resets between them. It is intended for inspection, not finished movement, planted-foot locomotion or gameplay timing. FBX round-trip import may offset the first sample from frame 1 to frame 2; duration is unchanged.

```text
Root
  Pelvis
    Spine
      Chest
        Neck
          Head
        Clavicle_L
          UpperArm_L
            LowerArm_L
              Hand_L
                Index_1_L
                  Index_2_L
                    Index_3_L
                Middle_1_L
                  Middle_2_L
                    Middle_3_L
                Ring_1_L
                  Ring_2_L
                    Ring_3_L
                Little_1_L
                  Little_2_L
                    Little_3_L
                Thumb_1_L
                  Thumb_2_L
                    Thumb_3_L
        Clavicle_R
          UpperArm_R
            LowerArm_R
              Hand_R
                Index_1_R
                  Index_2_R
                    Index_3_R
                Middle_1_R
                  Middle_2_R
                    Middle_3_R
                Ring_1_R
                  Ring_2_R
                    Ring_3_R
                Little_1_R
                  Little_2_R
                    Little_3_R
                Thumb_1_R
                  Thumb_2_R
                    Thumb_3_R
    Thigh_L
      Shin_L
        Foot_L
          Toe_L
    Thigh_R
      Shin_R
        Foot_R
          Toe_R
```

## Verification and limits

- Blender file opens; textures are packed and external maps are supplied.
- All four LODs have zero boundary/non-manifold edges, zero degenerate triangles, normalized single-bone weights and clean visual-object transforms.
- LOD0 UV raster audit at 4K found **0 overlapping interior texels**. Shared edges are excluded; subpixel overlaps are below this test's resolution. Cross-LOD atlas reuse is intentional.
- Rigid edge-length error across six sampled poses is at most **0.00000130 m**.
- Both hand and foot IK solvers reached their test targets within **0.02 mm**.
- FBX round-trip preserved all 48 visual objects, 52 bones, 15 collision proxies, triangle counts and socket positions. Measured imported height is **2.000000 m**.
- The diagnostic animation round-tripped with finger motion.
- Front, side, rear, face, service interface, grasp, crouch and distance-LOD renders were reviewed.

Assemblies intentionally overlap at bearings, fasteners and armor mounting interfaces. Rigid hoses are attached to their owning segment; they are not physically simulated. Extreme motions, deep crouches, retargeted clips and production hand interactions still need motion-specific clearance review. Full swept-volume collision testing, polished animation, physics simulation, in-engine shader compilation and target-hardware performance testing have not been completed. No claim of AA/AAA visual equivalence or production-engine certification is made.

Unity API references used for the helper:

- [Rig import settings](https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Rig.html)
- [LODGroup](https://docs.unity3d.com/6000.0/Documentation/Manual/class-LODGroup.html)
- [Axis conversion](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter-bakeAxisConversion.html)
- [Per-material property blocks](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer.SetPropertyBlock.html)
