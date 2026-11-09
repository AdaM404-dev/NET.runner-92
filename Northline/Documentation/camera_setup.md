# Camera setup

The picture edit is in the `NET.runner-92 | Intro` scene in `Cinematic/Intro_Cinematic.blend`. Camera-bound markers select the camera at each cut. Other timeline markers label cues for the mixer. Camera locations use the hospital's metric coordinates. +X is east, +Y is north and +Z is up. Locations are written as X, Y, Z in metres.

| Camera | Lens | Start | End | Focus |
| --- | ---: | --- | --- | --- |
| 01 World | 32 mm | (19, −91, 7.90) | (13.20, −80, 6.85) | Deep exterior |
| 02 Facility | 32 mm | (−8.30, −37.70, 3.03) | (−2.55, −27.45, 2.04) | 17 → 7.4 m at f/6.3 |
| 03A Reception | 26 mm | (−2.30, −14.12, 1.65) | (−1.50, −11.85, 1.56) | Deep clinical wide |
| 03B Emergency | 39 mm | (−20.82, 6.17, 1.68) | (−20.12, 6.41, 1.60) | 2.64 → 2.53 → 3.15 m at f/5.6 |
| 04A Optics | 51 mm | (5.32, −0.45, 15.02) | (5.83, −0.49, 15.17) | 1.52 → 1.29 m at f/5.6 |
| 04B Reader | 59 mm | (9.51, −0.76, 14.17) | (9.98, −0.85, 14.11) | 1.79 → 1.50 m at f/5.6 |
| 04C Gate | 27 mm | (8.03, −0.32, 14.16) | (9.06, −0.42, 13.99) | Deep across both jambs |
| 04D Lift | 38 mm | (−0.88, 2.77, 14.30) | (−0.46, 2.88, 14.27) | 2.48 → 2.25 m at f/6.3 |
| 05 Title | 40 mm | (153.46, −12.65, 3.31) | (156.94, −10.47, 2.69) | 12.7 → 10.91 → 10.49 m at f/5.6 |
| 06 Watchdog | 45 mm | (−23.98, −9.22, 14.40) | (−23.64, −8.94, 14.34) | 2.18 → 1.84 m at f/5.6 |
| Cut to black | 50 mm | (193, −4, 2) | Static | Physical black plate |

Location, aim and focus use editable F-curves. The cameras that span the longest distances have intermediate keys for controlled speed changes. The base hospital collections remain editable. Film-only geometry is under `INTRO | Sequence`. The title stage sits east of the hospital and its letterforms are editable Blender text with extrusion and bevel. The separate title library packages the same lockup at a local origin.

## Editing the scene

`Northline_Hospital` contains the furnished building and environment pass. Under `INTRO | Sequence`, the picture edit and lighting are separate from the arrival site, clinical action, security action, title stage, final monitor and weather effects. The audio master is a Sequencer sound strip. Keep the `Audio` directory beside the cinematic project to preserve its relative path. The picture render bypasses the Sequencer so each numbered frame comes from its bound 3D camera.
