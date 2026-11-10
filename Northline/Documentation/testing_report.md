# Unity Play Mode testing

The Editor opened `Hospital_Exploration` with Direct3D 11 and an RTX 3060 Ti. The play-mode runner drove the actual scene's CharacterController, then captured six 1920 × 1080 HDRP views. [Raw Play Mode results](../Renders/Phase_05_Unity/playmode_checks.txt)

## Executed checks

| Test | Result |
| --- | --- |
| Player representation | No renderers on the player or its children. Camera and capsule present. |
| Normal and sprint travel | 3.24 m and 4.84 m from rest over one second in the sampled route. |
| Public entry and emergency | Walked from the quarantine forecourt through reception and from the ambulance apron to the emergency department. |
| Floors and gravity | Slabs aligned at 0.0, 4.2, 8.4 and 12.6 m. The capsule settled on the ground slab after a drop. |
| Stairs | Continuous L1→L4→L1 runs for west/east/west and east/west/east. All twelve floor-to-floor legs passed with grounded arrival. No jump or floor teleport between legs. The largest vertical change in a 1/90 s movement substep was 26 mm. |
| Walls and furniture | Physics probes hit a concourse wall and the reception counter. Capsule sprint tests stopped at the closed vehicle gate and at the east perimeter. |
| Rooms | 108 of 108 occupiable zones reached in the 0.32 m PhysX capsule-clearance grid. Per floor 22/22, 34/34, 28/28, 24/24. This is a collision graph sample in Play Mode. |
| Weather and zones | Rain emitted outdoors, roof probes rejected covered paths, and the reception ambience reached full interior blend. Three distinct transparent material types were identified. |

The first test path crossed each stair landing too close to the door wall and one floor probe hit the test player's own capsule. The final sweep uses the documented landing axis and moves the capsule clear of the floor test. The first interior previews were dark. Local fill and exposure were adjusted, and the final passes captured the brighter interior.

The focused keyboard, mouse and cursor checks are separate from the no-focus batch test. The first batch window could not hold the cursor lock, so those input observations were excluded from the final automated result. Unity also logged an Editor Search indexing exception during batch startup, before the scene tests. The scene continued to Play Mode.

## Game camera captures

[Player spawn](../Renders/Phase_05_Unity/01_PlayerSpawn.png) · [Public canopy](../Renders/Phase_05_Unity/02_ExteriorWeather.png) · [Reception](../Renders/Phase_05_Unity/03_Reception.png) · [Wards](../Renders/Phase_05_Unity/04_Patient_Ward.png) · [Surgery](../Renders/Phase_05_Unity/05_Surgery.png) · [Research](../Renders/Phase_05_Unity/06_Research.png)

The optimized scene retains the same level positions and collision shapes. Its rendering change limits far fine detail and shadow workload. The gameplay path checks passed on that scene before its Windows build.

## Focused Windows player input

The optimized Windows development player opened a focused Direct3D 11 window. Its Input System replay advanced the player 1.51 m with W, changed the view with mouse delta, released the cursor with Escape and restored the lock with left click. [Focused player log](../Renders/Phase_05_Unity/focused_input_checks.txt)

Desktop capture of the first Windows player timed out with `window capture timed out: timed out waiting on channel`. The focused replay and six Play Mode image captures provide the test evidence for that build.

## Traversal build validation

The updated Windows player passed 11 focused Input System replay checks covering W/A/S/D, normalized diagonal movement, sprint, mouse look, Escape and left-click recapture. In the timed replay, W moved 1.57 m, W+D moved 1.51 m and Shift+W moved 2.34 m. [Focused input results](../Renders/Phase_05_Unity/Traversal/focused_input_checks.txt)

The rendered 640.8 m route took 184.91 s with the actual CharacterController. It started at the south forecourt, crossed the intake and reception, used the west/east/west stair sequence to Level 4 and returned outside on foot. All six stair legs ended grounded at the expected floor height. A separate Editor Play Mode regression repeated both stair circuits and reached all 108 planned zones in the PhysX clearance grid. [Standalone route results](../Renders/Phase_05_Unity/Traversal/route_checks.txt) · [Play Mode results](../Renders/Phase_05_Unity/playmode_checks.txt)

The route sampled 826 digital AudioListener buffers and logged the two ambience loop gains. Both indoor and outdoor mixes produced signal, the entrance transition produced intermediate mix values and all sampled buffers stayed below clipping. The Play Mode regression emitted 769 outdoor drops and rejected 106 covered rain paths. [Audio trace](../Renders/Phase_05_Unity/Traversal/route_audio.csv)

A human playthrough for mouse comfort, readability while moving and the sound mix by ear remains open. [Performance report](performance_report.md)

## Interactive build validation

The Play Mode suite found 108/108 closed door assemblies with a collider across the opening center. The 100 ordinary door assemblies opened, and the PhysX clearance graph reached 108/108 planned zones. Entrance and ambulance approaches still worked. The six approved stair legs passed with the actual CharacterController. Six sprint probes against the isolated legs were stopped, covering both ends of each closed leg. [Interactive Play Mode results](../Renders/Phase_05_Unity/Interactive/playmode_checks.txt)

The west lift rejected undiscovered floors at startup. The player targeted the lobby call control, boarded under its own collision, targeted all four cabin controls and rode L1→L2→L3→L4→L1. It walked back out onto the original lobby floor. The landing doors remained interlocked with the aligned cabin.

All four K-7 units moved and changed the original rig's leg pose. The four authored routes passed a sampled capsule-clearance check. The patrols have the source model's four detail levels and no interaction target. The ward camera capture shows the K-7 with normal automatic LOD selection and the doors in their starting state. [Ward patrol capture](../Renders/Phase_05_Unity/Interactive/07_K7_Ward_Patrol.png) · [Interaction and maintenance notes](interactive_systems.md)

The four selected practical lights each fell below 10 percent output and recovered during a 12.6-second Play Mode sample. The scene also contains five independent display power loops.

The focused standalone Windows replay passed all 15 checks. It opened and closed the public entrance with E and opened the aligned west lift landing with E on the call button. [Windows input results](../Renders/Phase_05_Unity/Interactive/focused_input_checks.txt)

## LATCH-07 build validation

The isolated import passed 11 checks. The final hospital Play Mode run passed 75 checks, including the relay flow and all 108 reachable zones. The Windows build passed 21 focused checks. It used Q to raise and stow the tablet, E to link and R to cancel. Tablet input held the aimed-at public door closed during the E test. [LATCH-07 test evidence](latch07_unity.md#verification-and-continuation)

## Reception CCTV validation

The Play Mode suite passed 95 checks, including a physical walk to the terminal and six distinct rendered feeds. The ward feed changed without a channel switch. The Windows build passed 31 focused checks covering the terminal, signature verification, tablet CCTV and camera selection. The hospital regression again reached all 108 planned zones. [CCTV verification](cctv_unity.md#verified)
