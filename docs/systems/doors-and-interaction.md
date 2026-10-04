# Doors and interaction

**Purpose.** Pressing E near a door toggles it. This is the only interaction
in the game so far.

**Status.** All 119 doors are exported closed in the 2026-10-02 art revision.
The 71 previously open swing leaves now use their authored closed poses;
the six loading shutters are fully lowered. There are no locks, sounds or
events, and nothing except a door can be interacted with.

Verified 2026-09-30 on commit `df3a31d` with `bin/unity-inspect scene doors`
and scripted Play-mode checks (reach, timing, collisions).

## Where it lives

| What | Where |
| --- | --- |
| Door script | [`Assets/NETRunner/World/Scripts/NexusDoor.cs`](../../Assets/NETRunner/World/Scripts/NexusDoor.cs), `NetRunner.World.NexusDoor` |
| Interaction code | `NexusPlayer.Interact()` in [`NexusPlayer.cs`](../../Assets/NETRunner/Player/Scripts/NexusPlayer.cs); the E key is read in `Update()`, the notice text is drawn in `OnGUI()` |
| Door objects | `Warehouse_NearFuture.prefab` → `Warehouse_NearFuture_Geometry/DOOR_*` (119 of them), placed in the gameplay scene and in `MainTest` |
| Setup | stored in `Warehouse_NearFuture.prefab` (since 2026-10-04) as additions to the imported model; there is no door prefab |
| Test | `ADoorNearTheStartOpensWhenToggled` in `Assets/NETRunner/Player/Tests/PlayMode/`: `DOOR_Hall_X-18_4` turns 95° in one second |

To find doors in the editor: type `t:NexusDoor` in the Hierarchy search box,
or run `unity command find_gameobjects --type NexusDoor`.

## One door, as built

```
DOOR_Hall_X-18_4          layer 9 World, not static
│  MeshFilter + MeshRenderer   the door leaf you see
│  NexusDoor                   the script
│  BoxCollider                 solid (not a trigger), blocks the player
└─ NF_DOOR_Skin_DOOR_Hall_X-18_4   decorative panel; moves with the leaf
```

Next to the doors stand more objects without scripts: a frame for every
swing door (`DOOR_…_Frame`, static, 113 in total) and 81 wall-mounted access
readers (`NF_PANEL_ACCESS_###`). The readers are decoration only, like the 44
other wall panels (`NF_PANEL_ADMIN…`, `DOCK`, `HALL`, `HUB`, `POWER`,
`RELAY`, `SERVICE`, `SHAFT`, `ENTRY`), which look like natural hacking
targets later.

## The door script, line by line

```csharp
public bool loadingDoor;                 // false: swings. true: slides up (loading bay)
public float angle = 95, lift = 3.8f;    // how far it swings (degrees) or lifts (metres)
public bool IsOpen { get; private set; } // flipped by Toggle()

void Awake()   // remember the pose from the model file as the "closed" pose
void Toggle()  // IsOpen = !IsOpen; nothing moves yet
void Update()  // every frame, move 'progress' toward 1 (open) or 0 (closed)
               //   swing: 1.3 per second  → 0.77 s to open
               //   lift:  0.45 per second → 2.2 s to open
               // eased = progress² × (3 − 2 × progress)   (slow start, slow end)
               // swing: turn the leaf angle × eased around the world's up axis
               // lift:  raise the leaf lift × eased metres
```

The door does not decide anything on its own: it only animates toward
whatever `IsOpen` says.

## How pressing E reaches a door

```
E pressed (read in Update)
 └─ Interact()
      ray from the camera, straight ahead, 3.5 m long,
      hitting every layer except 8 (the player)
      └─ first thing hit → look for a NexusDoor on it or on its parents
           found     → door.Toggle()
                        notice "Access granted" (now open) or "Door closed"
                        shown for 2 s at the bottom of the screen
           not found → nothing happens, no message
```

Measured reach:

| View | Works at | Fails at | Why |
| --- | --- | --- | --- |
| First person | 3.6 m from the door | 3.9 m | the ray starts at the eyes, 0.17 m in front of the body |
| Third person | 0.37 m (touching the door) | 0.6 m | the ray starts at the camera, 3.17 m behind the body, so only about 0.3 m of it reaches past the player |

## The 119 doors

| Kind | Count | Behaviour |
| --- | ---: | --- |
| Swing doors | 113 | turn 95° around their hinge |
| Loading doors `DOOR_Loading_01` … `06` | 6 | slide up 3.8 m; at the loading docks on the south side |
| on the ground level | 82 | |
| on the upper level | 37 | |

**Exported pose.** Each swing door starts in its authored closed pose, and
the script remembers that pose as "closed". All 113 hinged leaves align with
their frames; all six shutters start unraised. The 71 leaves that were
standing open in the earlier export were corrected in the Blender source
and re-exported without changing geometry, names, hierarchy or Unity GUIDs.
The first toggle is intended to open each door. The source and FBX checks
are in `ArtSource/Warehouse_NearFuture/Door_closure_validation.json` and
`Export_validation.json`.

The code still assumes an imported starting pose is closed. General state
detection for future incorrectly posed assets remains part of issue #16
and the gameplay work in #10; neither issue is completed by this art edit.

Run `bin/unity-inspect doors` for the exact list per group. The floor plan
in [[systems/level-warehouse]] shows where every door is.

## Physics

The door's BoxCollider has no Rigidbody, so the collider is teleported a
little every frame. Measured: a door swinging into a player who stands in its
way pushed the player 1.4 m aside; the player ended up next to the leaf, not
inside it. Every door turns the same way (+95°), whichever side the player
stands on.

## Known problems

Each is explained, with a proposed fix, in [[architecture/before-new-scripts]].

- State detection relies on the exported pose. The current warehouse is
  exported closed; another asset exported open would still be misidentified.
- Interaction knows only doors, through a hard-coded `GetComponentInParent<NexusDoor>()`.
  Terminals, pickups, access readers or hack points would each need new code
  inside the player script.
- Third-person reach is about 0.3 m.
- No lock, access rule, key card, power state or hacking hook; every door
  says "Access granted".
- No sound, no event other scripts can listen to, no saved state.
- Moving colliders without a Rigidbody; doors can shove the player.
- The 119 door setups are 119 separate additions inside the warehouse
  prefab; there is no door prefab to change them all at once.

## Open questions

- Which doors should start open, which locked, which hackable? (Design.)
- Should doors open automatically, on E, or both?

2026-10-02 Unity verification: a headless MainTest check confirmed all 119
doors start closed in Play mode. DOOR_Hall_X-18_4 opened 95 degrees on its
first scripted Toggle call and returned closed on the second. Door scripts,
colliders, skin parents and material assignments were retained; no scene
was saved. No manual key input or Unity rendering was tested. Results are
in ArtSource/Warehouse_NearFuture/Unity_validation.json.
