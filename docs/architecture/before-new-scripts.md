# Before new scripts: what to change first

The game runs, but what exists was built to preview the art, not to grow a
game on. This page lists what should change before new gameplay scripts are
added, why, in which order, and who does it. Steps 1 to 4 are done (marked
below); the rest is still to do.

**Each step and each decision is now a GitHub issue** with the full
reasoning (why, what will change, what stays the same, risks, checks), linked
below. The pinned [Roadmap, issue #5](https://github.com/AdaM404-dev/NET.runner-92/issues/5)
keeps the order; [[decisions/README]] explains how the issues work.

Written 2026-09-30 from the inspection described in the `systems/` notes
(commit `df3a31d`).

## In short

- **The art is ahead of the code.** A detailed character, a 1-million-triangle
  warehouse and an enemy robot are ready; the code is two scripts, one of
  which does ten jobs.
- **Seven steps come first.** Most are small. They give every new script an
  obvious place to live, a way to be tested, and settings it can rely on.
- **The basic choices are sound** and stay: see "What is fine" below.

Sizes: **S** = under an hour, **M** = about one session, **L** = several
sessions. Tags: **YOU WRITE** (Samuel writes it, Claude reviews), **PAIR**
(Claude writes and explains, Samuel follows up), **CLAUDE** (Claude builds,
Samuel reviews).

## Must do first, in this order

### 1. Name the layers and stop using layer numbers in code

Issue: [#6](https://github.com/AdaM404-dev/NET.runner-92/issues/6). **Done 2026-10-04**: layers named, `NexusPlayer` uses two `LayerMask` fields.

| | |
| --- | --- |
| Now | Layers 8 (player) and 9 (the whole warehouse) are used but have no names. The code writes `1 << 9` and `~(1 << 8)`. |
| Why it blocks | Every new ray or overlap (interaction, enemy sight, hacking range) has to choose layers. Bare numbers are unreadable and break silently when someone reuses a layer. |
| Change | Name them `Player` and `World` in Tags and Layers; add `Enemy` and `Interactable` now. In code, use `[SerializeField] LayerMask` fields set in the Inspector. Give the player the existing `Player` tag. |
| Size, tag | S, CLAUDE |

### 2. Remove template leftovers and set the project's identity

Issue: [#7](https://github.com/AdaM404-dev/NET.runner-92/issues/7). **Done 2026-10-04**: template files and the four `NEXUS_FullBody_LOD*` prefabs deleted, company name `NET.runner`.

| | |
| --- | --- |
| Now | `Assets/TutorialInfo/` and `Assets/Readme.asset` from Unity's template; five prefabs nothing uses; company name `DefaultCompany`. |
| Why it matters | Leftovers show up in every search. The company name decides where save files go on players' computers, so it should be final before any save system exists. |
| Change | Delete the template files. Keep `NEXUS_FirstPerson_Arms.prefab` until [#14](https://github.com/AdaM404-dev/NET.runner-92/issues/14) is decided; delete the four `NEXUS_FullBody_LOD*` prefabs if AdaM404 agrees. Set the company name (decision D5). |
| Size, tag | S, CLAUDE |

### 3. A gameplay scene of our own, the warehouse as a prefab, one player prefab

Issue: [#8](https://github.com/AdaM404-dev/NET.runner-92/issues/8). **Done 2026-10-04**: `Warehouse_NearFuture.prefab`, gameplay scene `Assets/Scenes/Game/Warehouse.unity` first in the build list, `NEXUS_Player.prefab` tagged `Player`, `SampleScene` deleted. The preview scenes keep their player copies until Step 6.

| | |
| --- | --- |
| Now | The 119 door scripts, 704 colliders and 106 lights exist only inside `MainTest.unity` (2 MB), the teammate's preview scene. The player exists three times: in `MainTest`, in `CharacterPreview`, and as `NEXUS_Player.prefab`, which nothing uses. The build list starts with the empty `SampleScene`. |
| Why it blocks | Every gameplay object (a locked door, a terminal, an enemy spawn, the navigation mesh) would be added inside the teammate's scene, so both of us would edit the same 2 MB file and fight over merges; if he re-exports the level, our work detaches. Every player feature would have to be added to three copies. |
| Change | Save `Warehouse_NearFuture` (with its doors, colliders and lights) as a prefab under `Assets/Environment/Warehouse_NearFuture/` (art side). Create `Assets/Scenes/Game/Warehouse.unity` (our side) holding that prefab and an instance of `NEXUS_Player.prefab`. `MainTest` keeps previewing the same warehouse prefab. Put the new scene first in the build list and remove `SampleScene`. |
| Size, tag | M, CLAUDE. **Needs AdaM404's agreement** ([#18](https://github.com/AdaM404-dev/NET.runner-92/issues/18)), because it restructures his scene. If he has not answered yet, continue with steps 4 to 7 and come back to this one. |

### 4. Give the code a structure and a first test

Issue: [#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9). **Done 2026-10-04**, with feature folders instead of the folders below (Samuel's choice): `Assets/NETRunner/Core`, `World` and `Player`, one assembly and namespace each (`NetRunner.Core`, …), `VerticalMotion` in `Core` with 6 EditMode tests, 3 PlayMode tests that replace the auto-test, `.editorconfig`, both scripts reformatted. See [[architecture/overview]].

| | |
| --- | --- |
| Now | Two scripts in `Assets/Scripts/`, no namespaces, no assembly definitions, no tests. The K7 robot's scripts would land in the same code bucket under another namespace. |
| Why it blocks | A new script needs an obvious home, compile times grow with every script in one assembly, and without tests a refactor cannot show it kept behaviour. |
| Change | Folders `Core/` (plain C# rules with no scene objects, testable), `Gameplay/`, `UI/`, `Editor/`, `Tests/EditMode/`, `Tests/PlayMode/`; namespaces `NetRunner.Core` and so on; one assembly definition per folder; asset helper scripts (such as the K7's) in their own assembly next to their asset. Add an `.editorconfig` so new code is formatted one way. One real EditMode test and one PlayMode test that walks the player (the current auto-test, moved into Unity's test runner and saving inside the project). |
| Size, tag | M, PAIR |

### 5. One way to interact with anything

Issue: [#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10)

| | |
| --- | --- |
| Now | `Interact()` looks for a `NexusDoor` and nothing else. It casts from the camera, so in third person it reaches 0.3 m. The 2026-10-02 warehouse export closes all doors; the code still assumes any imported pose is closed. Every door says "Access granted". |
| Why it blocks | Terminals, access readers, pickups, people and hack points all need "look at it, press E". Without a shared contract, each one means editing the player script. |
| Change | An interface in `Core`: `IInteractable` with a prompt text, `CanInteract` and `Interact`. A `PlayerInteractor` component casts from the eyes (not from the camera) using a `LayerMask`, shows the prompt, and calls `Interact`. `NexusDoor` implements the interface, works out from its frame whether it is really open, and reports locked or unlocked. |
| Size, tag | M, **YOU WRITE**: a small, self-contained first script for Samuel that touches the core Unity ideas (raycasts, interfaces, `GetComponent`, the Inspector). Claude writes the spec and test names. |

### 6. Split `NexusPlayer` into small components

Issue: [#11](https://github.com/AdaM404-dev/NET.runner-92/issues/11)

| | |
| --- | --- |
| Now | One class handles input, movement, two camera modes, interaction, the HUD, cursor locking, debug keys, respawn and preview mode. (Step 4 removed the auto-test and moved the gravity and jump maths to `Core`.) |
| Why it blocks | Crouching, stamina, health, a hacking tool or aiming would all go into the same class. It would soon be impossible to change one part without breaking another. |
| Change | Same behaviour, new shape: `PlayerInput` (reads input), `PlayerMotor` (moves the CharacterController; its maths testable in `Core`), `PlayerCamera` (first- and third-person placement), `PlayerInteractor` (from step 5), `PlayerHud` (temporary), `DebugKeys` (Tab, F1, R, development builds only). Tunable numbers move into a settings asset (see "Should do soon"). The PlayMode test from step 4 must pass before and after. |
| Size, tag | L, PAIR. Depends on 4 and 5. |

### 7. Switch to the Input System

Issue: [#12](https://github.com/AdaM404-dev/NET.runner-92/issues/12)

| | |
| --- | --- |
| Now | The code reads keys with the old `Input` class. The project already has an Input System actions asset with Move, Look, Jump, Sprint, Interact, Crouch and Attack, and "Active Input Handling" is set to **Both**. |
| Why it blocks | Every new action (crouch, hack, open the map) would add another hard-coded key. The actions asset gives rebinding, gamepad support and menus for free. Running both systems invites bugs. |
| Change | `PlayerInput` reads the actions asset; then set Active Input Handling to the Input System only. |
| Size, tag | M, PAIR. Depends on 6. |

## Should do soon

Not blocking, but cheap if done early:

| # | Change | Why | Owner |
| --- | --- | --- | --- |
| S1 | Move tunable numbers (gravity, jump, eye height, camera offsets, fields of view, reach) into a settings asset (a ScriptableObject) | tuning without editing code; one place to look | Samuel, with step 6 |
| S2 | Replace the `OnGUI` text with a real UI screen (UI Toolkit): crosshair, interaction prompt | `OnGUI` is meant for debug tools; it cannot be styled | Samuel, after step 5 |
| S3 | Switch on post-processing and anti-aliasing on the player camera; add a Volume to the gameplay scene (exposure, tonemapping, bloom for the LEDs) | the dark, flat picture makes every test harder to judge | Samuel (camera), AdaM404 (look) |
| S4 | Doors: move with a kinematic Rigidbody, open away from the player, states (locked, unlocked, hackable, unpowered), an event other scripts can listen to, sounds | doors are central to a netrunner game | Samuel, after step 5 |
| S5 | Give `CharacterPreview` a small orbit-viewer script; remove `previewMode` and F1 from the player | the real player controller should not double as a model viewer | Samuel, with step 6 |
| S6 | Drive the Animator's `Speed` from real movement; decide how LOD1–3 animate; ask for missing clips (walk back and sideways, jump, crouch, interact, hack) | walking into walls plays the walk; distant characters would freeze | Samuel (code), AdaM404 (clips) |
| S7 | Remove `Application.runInBackground = true` from the player script; set it in Player Settings if wanted | a hidden global setting inside a gameplay script | Samuel |

## Can wait

| Change | When |
| --- | --- |
| Bake a navigation mesh on the warehouse (the AI Navigation package is installed) | before enemy AI |
| Import the K7 robot (open item V-07) | after step 4, so its scripts get their own assembly; after the navigation mesh |
| Lighting pass: shadows on key lights, reflection probes, baked or probe lighting, occlusion data | art, when the look is decided |
| Performance baseline in a built game, then budgets | once the gameplay scene exists |
| Git LFS ([#17](https://github.com/AdaM404-dev/NET.runner-92/issues/17)) | team decision, sooner is cheaper |
| Automatic compile and test run on every pull request | after step 4 |
| Remove unused packages (Visual Scripting, AI Inference, Version Control) | after the design kickoff |
| An editor command that checks incoming art (missing references, Built-in shaders, oversized textures, naming) | when art arrives regularly |

## What is fine and should stay

- **Movement through a CharacterController** with one `Simulate(input, dt)`
  step per frame: simple, predictable and testable. The new `PlayerMotor`
  keeps the idea.
- **Camera placement in `LateUpdate`**: the right moment.
- **The NEXUS model**: humanoid rig, LODs, clean import reports.
- **URP with the Forward+ renderer**: handles the 106 lights.
- **A separate, simple collision model** (40,680 triangles instead of 1.08
  million) and **static flags** on the building.
- **The naming scheme** (`DOOR_`, `FLOOR_`, `NF_`, …) and the object hierarchy.
- **The 52 URP materials.**

## Decisions needed

| # | Question | Recommendation | Who | Issue |
| --- | --- | --- | --- | --- |
| D1 | Third person: part of the game or a debug view? | Debug view only; the game is first-person. It keeps the camera work small. | Samuel | [#13](https://github.com/AdaM404-dev/NET.runner-92/issues/13) |
| D2 | First person: full body (today) or the arms-only model? | Keep the full body for now (it works and shows the cybernetic arm); decide again when aiming or weapons arrive. Keep the arms prefab until then. | Samuel, AdaM404 | [#14](https://github.com/AdaM404-dev/NET.runner-92/issues/14) |
| D3 | Who owns the warehouse prefab and the gameplay scene? | Prefab: AdaM404. Gameplay scene: Samuel. | both | [#8](https://github.com/AdaM404-dev/NET.runner-92/issues/8), [#18](https://github.com/AdaM404-dev/NET.runner-92/issues/18) |
| D4 | Target platforms | **Decided 2026-10-04: PC only (Windows and Linux); the Mobile quality level is removed.** | both | [#15](https://github.com/AdaM404-dev/NET.runner-92/issues/15) |
| D5 | Company name and code namespace | **Decided 2026-10-04: company name `NET.runner`; namespaces `NetRunner.<Feature>`.** | both | [#7](https://github.com/AdaM404-dev/NET.runner-92/issues/7), [#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9) |
| D6 | Doors that are exported open | The door script works out from the frame which pose is shut; later, export doors shut. | Samuel, AdaM404 | [#16](https://github.com/AdaM404-dev/NET.runner-92/issues/16) |

## After these steps

The design kickoff ([[design/README]]) decides which gameplay comes first:
netrunning, stealth against the K7, terminals. New scripts then go into the
structure from step 4, use the interaction contract from step 5 and the input
actions from step 7, and are placed in the gameplay scene from step 3.
