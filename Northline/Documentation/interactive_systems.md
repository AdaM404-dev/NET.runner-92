# Phase 5 hospital interaction

## Controls

Aim the center reticle at an architectural door and press **E**. The prompt shows whether E opens or closes it. Doors start closed each time the scene loads. The 100 regular doors include swing leaves, sliding pairs and the telescopic research barrier. Their colliders follow the moving panels. An ordinary door reopens if the player enters its clearance while it closes.

The west lift is the working car. Aim at the call button to the right of the west landing doors and press **E**, then walk into the car. Inside, aim at one of the four buttons and press **E**. The prompt names the destination. Lift landings open only when the car is aligned. The east shaft is marked offline. The separate quarantine perimeter and vehicle gate remain sealed.

## Stair progression and lift access

| First ascent | Required core | Isolated core |
| --- | --- | --- |
| L1 reception → L2 wards | West | East |
| L2 wards → L3 surgery | East | West |
| L3 surgery → L4 research | West | East |

Each isolated leg has physical closures at both flight ends and a bulkhead at the half landing. The concourse doors to the stair vestibules can open normally. The open stair legs also work in reverse. Crossing a required stair exit into the new concourse unlocks that stop in the lift. The lift can return to any unlocked level for the rest of the session. It starts with only L1 enabled on a fresh load.

The cabin has a moving floor, side and rear walls, a ceiling and an interlocked cabin door. The CharacterController rides the cabin with its viewing direction preserved. The lift aborts a closing sequence if the passenger is in the doorway.

## Residual power and patrols

Four concourse practicals, one per floor, occasionally lose output and recover. The matching emissive geometry follows the same timing where the fixture has an emissive material slot. Four concourse index readouts and the L2 transfer console display have separate power loops. They use their original materials with per-renderer overrides.

Four K-7 units patrol the main concourse, one per floor. Each walks between two authored points, turns at the ends and scans with its head. The existing model has an articulation check but no locomotion clip. The patrol code drives the original leg, foot, arm, pelvis and head bones. The units have no interaction prompt, perception, pursuit or combat.

The imported source is `ArtSource/K7_Industrial_Robot` from the local checkout of the game's GitHub repository, introduced in commit `05e2e86`. The FBX is copied byte for byte. This project remaps its seven materials to HDRP Lit, uses the supplied 2K maps with an HDRP-packed mask, and configures all four original LODs. The imported visor points toward local −Z, so the patrol rotates the visual model 180° inside its movement root. [K-7 source commit](https://github.com/AdaM404-dev/NET.runner-92/commit/05e2e86f512e1266dabc57a36038fc40d26f5257)

## Scene and maintenance

`Hospital systems — circulation and patrols` contains the interactive doors, stairs, lift, patrols and residual power controllers. Architectural door frames remain in their original import collections. Moving leaves and their BoxColliders are grouped by opening under `Doors`. `HospitalInteraction` is on the scene player.

`NET.runner-92 → Install Hospital Systems` applies the setup to the saved scene. Full scene regeneration calls the same installer. It reads the copied K-7 sources under `Assets/NETRunner92/Environment/K7`.
