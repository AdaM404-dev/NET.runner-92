# Character model and animation (NEXUS)

**Purpose.** The visible body of the player: a human character, NEXUS, with
a cybernetic left arm, in four levels of detail, animated by one Animator.

**Status.** Idle, walk and jog forward work, plus a raised-arms pose for first
person. Only the most detailed model is animated. There are no clips for
jumping, walking backwards or sideways, crouching or interacting.

Verified 2026-09-30 on commit `df3a31d` with `bin/unity-inspect player animation`
and a Play-mode check of the lower detail levels.

## Where it lives

| What | Where |
| --- | --- |
| Models | `Assets/Characters/NEXUS/FBX/`: `NEXUS_FullBody_LOD0.fbx` … `LOD3.fbx`, `NEXUS_FirstPerson_Arms.fbx` |
| Textures | `Assets/Characters/NEXUS/Textures/` (89 images) |
| Materials | `Assets/Materials/NEXUS/` (24 materials, URP Lit) |
| Prefab used by the scenes | `Assets/Prefabs/NEXUS_Character.prefab` |
| Animator Controller | `Assets/Animations/NEXUS_Locomotion.controller` |
| Arm mask | `Assets/Animations/NEXUS_FirstPerson_UpperBody.mask` |
| Import reports | `Assets/Characters/NEXUS/UnityValidation.txt`, `TechnicalReport.json`, `OptimizationReport.json` |
| Licences | `Assets/Documentation/Character_Licenses/` (the base body comes from MakeHuman, CC0) |

The art folders belong to AdaM404 (see [[assets/handoff]]).

## Levels of detail (LOD)

`NEXUS_Character.prefab` holds a **LODGroup** and four complete copies of the
model, each with its own skeleton. Unity draws only one copy at a time,
chosen by how tall the character appears on screen.

| Level | Object | Triangles | Parts (renderers) | Drawn while the character fills more than | Animated |
| --- | --- | ---: | ---: | --- | --- |
| LOD0 | `LOD0` | 188,001 | 42 | 22 % of the screen height | **yes** (has the Animator) |
| LOD1 | `LOD1` | 100,227 | 42 | 11 % | no |
| LOD2 | `LOD2` | 48,083 | 39 | 4.5 % | no |
| LOD3 | `LOD3` | 19,499 | 39 | 0.8 %, then hidden | no |

The PC quality level doubles these distances (`LOD Bias` 2). By calculation,
with the third-person camera's field of view the switch to LOD1 happens at
about 17 m. The player's own camera is always closer, so today you only ever
see LOD0; in first person the script forces LOD0 anyway.

**Checked in Play mode:** with LOD1 forced, its leg bone did not move while
LOD0's did. LOD1–3 would appear frozen in their export pose. This matters as
soon as NEXUS, or any character built the same way, is seen from further away.

## The Animator

The **Animator** component sits on `NEXUS_Character/LOD0`:

| Setting | Value |
| --- | --- |
| Controller | `NEXUS_Locomotion` |
| Avatar | `NEXUS_FullBody_LOD0Avatar`, humanoid, valid |
| Apply Root Motion | off: the animation does not move the character; `CharacterController.Move` does |
| Culling Mode | Always Animate |

### The controller `NEXUS_Locomotion`

One parameter: **`Speed`** (a number). Two layers, each with a single state
and no transitions:

```
Layer 0  "Base Layer"         weight 1 (always)
  state "Locomotion" = blend tree on Speed
      Speed 0     → NEXUS_Rig|Idle_Ready     (4 s loop)
      Speed 1.65  → NEXUS_Rig|Walk_Forward   (1 s loop)
      Speed 3.25  → NEXUS_Rig|Jog_Forward    (0.8 s loop)
      values in between blend two neighbouring clips

Layer 1  "First Person Arms"  weight 0 by default, Override
  mask: arms and fingers only
  state "Ready" = NEXUS_Rig|FPS_Ready        (2 s loop)
```

A **blend tree** mixes clips by a number instead of switching between them,
so the legs speed up smoothly from standing to jogging. An **avatar mask**
limits a layer to some bones: layer 1 only moves the arms, so the legs keep
walking underneath.

### How the script drives it

| Code (`NexusPlayer.cs`) | Effect |
| --- | --- |
| `animator.SetFloat("Speed", speed, 0.15f, dt)`, line 74 | `speed` = walk or run speed × how far the stick or keys are pushed, smoothed over 0.15 s |
| `animator.SetLayerWeight(1, value ? 1 : 0)`, line 42 | Tab raises (first person) or lowers (third person) the arms layer |

The thresholds 1.65 and 3.25 in the blend tree are the same numbers as
`Walk Speed` and `Run Speed` in the Inspector. Changing a speed in one place
and not the other makes the feet slide.

## Animation clips in the model

All clips live inside `NEXUS_FullBody_LOD0.fbx`. Their full names start with
`NEXUS_Rig|` (the name of the skeleton in Blender), which is left out here.

| Clip | Length | Loops | Used by |
| --- | ---: | --- | --- |
| `Idle_Ready` | 4.0 s | yes | blend tree |
| `Walk_Forward` | 1.0 s | yes | blend tree |
| `Jog_Forward` | 0.8 s | yes | blend tree |
| `FPS_Ready` | 2.0 s | yes | arms layer |
| `Preview_Interface_Deploy` | 2.0 s | no | nothing |
| `Preview_Grip_Open_Close` | 2.0 s | no | nothing |
| `Preview_Contact_Index` | 2.0 s | no | nothing |
| `Cinematic_ClosedGarment_Intro_20s` | 19.9 s | no | nothing |

The four unused clips were made for previews. Judging by their names,
`Interface_Deploy` and `Contact_Index` show the cybernetic arm's contact,
which may suit hacking later; nobody has checked them in the game yet.

## Unused character assets

| Asset | What it is |
| --- | --- |
| `NEXUS_FullBody_LOD0.prefab` … `LOD3.prefab` | one prefab per detail level, each a variant of its model file |
| `NEXUS_FirstPerson_Arms.prefab` | a separate arms-only model (13 parts, 90,816 triangles, its own rig `NEXUS_FP_Rig`, one 2 s clip) meant for an arms-only first-person view |

None of them is referenced by any scene or other prefab.

## Import settings (all five models)

Scale 1, rig **Humanoid** for the full body and **Generic** for the arms,
avatar created from each model, animations imported, **Read/Write enabled**
(the mesh is kept in normal memory as well as on the graphics card, which
doubles its memory use), materials remapped to `Assets/Materials/NEXUS/`.

## Known problems

- Only LOD0 animates; LOD1–3 would appear frozen.
- Locomotion covers idle, walk and jog forward only. Backwards and sideways
  movement play the forward clips.
- The Animator's `Speed` comes from the keys, not from how fast the body
  really moves, so walking into a wall still plays the walk.
- The blend-tree thresholds repeat the Inspector speeds.
- Read/Write is enabled on every character model; nothing reads the meshes
  from code.

## Open questions

- Full body in first person (today) or the arms-only model?
- Which animations does the game need (crouch, interact, hack, hit, death),
  and who makes them?
