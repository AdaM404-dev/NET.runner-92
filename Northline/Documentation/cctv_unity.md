# Northline reception CCTV

## Play

Enter the hospital and go around the reception counter's east end. From the staff side, aim at the monitor with the amber strip and press **E**. Match the three cells of the local route to the intercepted signature.

[Phase 07 Windows archive](../Builds/README.md#current-windows-build)

| Control | At the terminal | On LATCH-07 |
| --- | --- | --- |
| Left / Right | Select a route cell | Previous / next feed |
| Up / Down | Change the selected cell | Previous / next feed |
| Enter | Verify the route | |
| E | Open or exit the terminal | Existing tablet interaction |
| 1 | | Open or close CCTV once unlocked |
| R | | Cancel and lower the tablet |

An incorrect signature allows another attempt. The correct route unlocks CCTV for the current play session and turns the monitor's indicator cyan. Pressing **1** before the breach points the player to reception. After unlocking, **1** raises the tablet with the CCTV page. Feed selection wraps at both ends. The player can move while watching. The terminal puzzle holds movement and mouse look while it is open.

## Coverage

The two existing reception monitor faces were identified in the counter mesh. The terminal uses the east monitor, with a dedicated interaction volume and a status strip. The hospital had no Unity cameras for interior surveillance. Its exterior scanner and vehicle camera meshes are decorative scene geometry.

| Channel | View |
| --- | --- |
| 01 | Public entrance and canopy |
| 02 | Reception and waiting area |
| 03 | Emergency concourse |
| 04 | Patient wards |
| 05 | Surgery concourse |
| 06 | Restricted research access |

Six editable viewpoints drive one HDRP camera. It renders the selected view into a 576 × 288 texture at up to 15 updates per second while the CCTV page is open. The arms and tablet are excluded from the feed to prevent the screen filming itself. The live image is shown on the existing tablet UV surface through its UI Toolkit panel. Camera settings disable several screen-space effects for this secondary view. [Unity camera render-target reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-targetTexture.html)

## Maintenance

Use `NET.runner-92 → CCTV → Install in Hospital` to install the system. Full hospital regeneration restores it when the LATCH-07 prefab is present. The six poses live under `Northline CCTV network / Virtual security viewpoints` in the saved hospital scene. The camera is inactive until a feed is selected. The unlock is scene-session state and resets on a fresh load.

Build the Windows player with `NETRunner92.Editor.HospitalSceneBuilder.BuildCctvWindows`. Run the Editor verification with `NETRunner92.Editor.PlayModeValidation.InstallCctvAndRun`. [Play Mode checks](../Renders/Phase_07_CCTV/playmode_checks.txt)

## Verified

The final Play Mode run passed all 95 checks. It walked around the desk, targeted the monitor, rejected an incorrect signature, unlocked all six distinct feeds and verified that closing CCTV stops the extra camera. The ward image changed while that channel stayed selected. The existing 108 hospital zones remained reachable. The standalone Windows replay passed all 31 focused input checks, including **E**, **Enter**, **1** and all four arrow keys. [Windows input log](../Renders/Phase_07_CCTV/focused_input_checks.txt) · [Live tablet capture](../Renders/Phase_07_CCTV/09_LATCH07_Live_CCTV.png) · [Terminal capture](../Renders/Phase_07_CCTV/10_Reception_Puzzle.png)
