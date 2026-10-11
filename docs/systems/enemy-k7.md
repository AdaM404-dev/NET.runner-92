# Enemy: K7 industrial robot (not in the game yet)

**Purpose.** The first enemy model: a 2 m tall industrial humanoid robot with
armour plates, glowing sensor lights for four alert states and a hack port on
its back.

**Status.** Delivered as a source package on 2026-09-30 (pull request #3). It
is **outside `Assets/`**, so Unity does not import it: there is no K7 prefab,
nothing in any scene, and its two scripts are not compiled. Open item V-07
in `agent.md`.

Read from the package on 2026-09-30; nothing here was run in Unity.

## Where it lives

`ArtSource/K7_Industrial_Robot/` (150 MB). Start with its
[`README.md`](../../ArtSource/K7_Industrial_Robot/README.md).

| Folder | Contents |
| --- | --- |
| `FBX/` | `K7_Robot.fbx` (all four detail levels, one skeleton, sockets, 15 collision boxes): the file to import; also separate LOD files and an animation test |
| `Textures/2K`, `Textures/4K` | one shared texture set: colour, normal, metallic, roughness, occlusion, and Unity's packed metallic/smoothness |
| `Unity/Editor/K7AssetSetup.cs` | a menu command that builds the prefab |
| `Unity/Runtime/K7Robot.cs` | a helper component for the finished robot |
| `Blender/`, `Source/` | the Blender file and the scripts that generated it; keep them out of `Assets/` |
| `Previews/`, `Validation/` | renders and measurement reports |

## What the model offers

| Feature | Detail |
| --- | --- |
| Size | 2.000 m from floor to head; faces +Z |
| Detail levels | LOD0 128,304 triangles → LOD3 14,744 |
| Skeleton | 52 bones, **Generic** rig (not humanoid), rigid parts, working fingers |
| Sockets | named points on the skeleton: `VisionOrigin` (for sight rays), `HackPoint` (upper back), `AudioOrigin`, `HeadPoint`, `ChestPoint`, hands, feet |
| Armour | 10 plates (`Armor_Chest`, `Armor_Back`, shoulders, forearms, thighs, shins) that can be hidden, for damage |
| Sensor lights | four colour states: Patrol (cold white), Suspicious (amber), Hostile (red, default), Hacked (blue) |
| Hit boxes | 15 box triggers on the bones (head, torso, arms, legs) |
| Animation | only `K7_ArticulationCheck`, a 10 s joint test; **no walk, run or combat clips** |

## The two scripts

**`K7AssetSetup.cs`** (editor only) adds the menu **Tools > K7 > Build Prefab
from Selected FBX**. With `FBX/K7_Robot.fbx` selected, it sets the model's
import options, creates seven URP materials, turns the collision boxes into
triggers, builds the LOD group and saves
`Prefabs/K7_Industrial_Robot.prefab` next to the imported files.

**`K7Robot.cs`** (namespace `Hexacorp.K7`), put on the finished prefab:

| Member | Use |
| --- | --- |
| `SetState(SensorState)` | switch the sensor lights: `Patrol`, `Suspicious`, `Hostile`, `Hacked` |
| `visionOrigin`, `hackPoint`, `headPoint`, … | the socket transforms, found by name when the robot wakes up |
| `SetArmorVisible("Armor_Chest", false)` | hide or show an armour plate on every detail level |
| `emissionIntensity` | brightness of the sensor lights (5) |

It changes light colours per robot without touching the shared material,
so each robot can be in a different state. It contains no AI, movement,
health or hacking logic; the package says those belong to the game.

## How to bring it in (from the package README)

1. Copy `FBX/`, `Textures/` and `Unity/` into `Assets/Characters/K7_Industrial_Robot/`.
2. Let Unity import and compile.
3. Select `FBX/K7_Robot.fbx` and run **Tools > K7 > Build Prefab from Selected FBX**.
4. Place the prefab and check its size, facing, lights and LOD switching.

## What the game still has to build

Navigation (the warehouse has no NavMesh yet), AI states that drive
`SetState`, movement (a NavMeshAgent or CharacterController), perception using
`VisionOrigin`, health and damage using the hit boxes and armour plates,
hacking at `HackPoint`, and animations: a walk cycle at minimum.

## Decisions before importing

- Where its scripts live. Copied in as they are, they would compile into
  Unity's default assembly (`Assembly-CSharp`). Since Step 4 (2026-10-04) our
  code is in assembly definitions (`NetRunner.*`), and an assembly definition
  cannot reference the default assembly, so no `NetRunner` script could call
  `K7Robot.SetState()`. The K7 scripts therefore need their own assembly
  definition next to the asset (for example `Hexacorp.K7`), which our enemy
  code then references, plus an Editor-only one for `K7AssetSetup.cs`. See
  [[architecture/overview]].
- Which layer enemies use, so the player's rays and the camera treat them
  correctly.
- 4K or 2K textures: the 4K set is 35 MB of image files, the 2K set 14 MB.
  The setup command reads the 4K files (the path is written into
  `K7AssetSetup.cs`); Unity can still shrink them at import with the
  texture's Max Size setting.
- Who does the import: it is art work (AdaM404), but the prefab will carry
  gameplay components (Samuel).
