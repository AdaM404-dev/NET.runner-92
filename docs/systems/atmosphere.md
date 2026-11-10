# Atmosphere: weather, ambience and pulsing lights

## Purpose

The hospital's mood: rain and lightning outside, the sound changing as the
player goes in and out, dust in the air, a leaking valve, distant sounds,
and lights that pulse or flicker. No gameplay depends on it yet.

## Status

From AdaM404's Northline Phase 07 (#30), in the root project since #31.
Since #42 (2026-11-10) it follows the coding standard: the rules are plain
C# in `Core` with EditMode tests, and the three components only read the
scene, probe it for roofs, emit particles, set volumes and play sounds.
Behaviour is unchanged (see "Tests").

## Where it lives

| What | Where |
| --- | --- |
| Components (`NetRunner.Atmosphere`) | `Assets/NETRunner/Atmosphere/Scripts/`: `EnvironmentalZone`, `HospitalAtmosphere`, `PracticalPulse` |
| Rules (`NetRunner.Core`) | `Assets/NETRunner/Core/Scripts/`: `ZoneBlend`, `Ambience`, `RainField`, `AmbientEvents`, `LightPulse`, `RandomRange` |
| Fog profiles, audio clips and mixer, particle materials (AdaM404's) | `Assets/NETRunner92/Atmosphere/` |
| Set-up (editor) | `Assets/NETRunner/World/Editor/HospitalAtmosphereBuilder.cs`: zones, fixtures, particle systems, audio sources, `Configure` calls |
| In the scene | under `Atmosphere`: the `HospitalAtmosphere` component, the `ZONE_…` objects (HDRP volume + `EnvironmentalZone`), the practicals with `PracticalPulse` |

## How it works

| Part | Where | What it decides |
| --- | --- | --- |
| `ZoneBlend` | `Core` | how much a point counts as inside a zone's box |
| `Ambience` | `Core` | how fast the interior blend follows the zones; the volumes, dust and steam from it |
| `RainField` | `Core` | how many drops fall, where, where each streak starts and how long it falls, which are left out, which splash, where no rain is worked out |
| `AmbientEvents` | `Core` | the lightning flash and when flashes, thunder, monitor beeps and creaks happen |
| `LightPulse` | `Core` | the emergency and intermittent light levels, and the light's intensity |
| `EnvironmentalZone` | `Atmosphere` | holds a zone's box, floor and dust; turns a world point into the zone's frame |
| `HospitalAtmosphere` | `Atmosphere` | every frame: reads the zones, sets volumes and particle rates, probes for roofs, emits rain and splashes, sets the lightning light, plays the one-shots |
| `PracticalPulse` | `Atmosphere` | samples Perlin noise and sets its light's intensity |

Every frame `HospitalAtmosphere` does this, for the listener (the player's
eyes):

1. **Cover.** Each zone on floors 1–4 gives a weight at the listener; the
   highest counts. The dust is the highest weight × zone dust. A floor-0
   zone counts for nothing (the builder makes none).
2. **Interior blend.** It follows the cover at a steady rate, so walking
   through a door crossfades the sound instead of switching it.
3. **Loops, dust, steam** from the blend, the dust and the distance to the
   valve.
4. **Rain**, unless the listener is deep inside: the drops due this frame
   each get a random column. A probe straight down finds the column's
   highest surface; the streak starts above both the listener and that
   surface, upwind so the wind carries it onto the column, and a second
   probe along its path shortens it if something is in the way.
5. **Events**: the lightning light, thunder, a monitor beep, a creak.

| Value | Number | Where |
| --- | --- | --- |
| zone weight | 1 inside the box; an S-curve (3t² − 2t³) down to 0 at the blend distance outside, measured to the nearest point of the box | `ZoneBlend` |
| hospital zones | floors 1–4 (dust 0.53, 0.78, 0.26, 0.04) and two entrance wings on floor 1 (0.47, 0.45); blend distance 1.35 m, as the HDRP volumes | builder |
| interior blend | follows the cover at 1.4 a second: outdoors to fully inside in 0.71 s | `Ambience.BlendRate` |
| rain and wind loop | 0.48 outdoors → 0.034 fully inside, in a straight line | `Ambience` |
| ventilation loop | 0.012 → 0.27 | `Ambience` |
| thunder | 0.72 → 0.26 | `Ambience` |
| creak of fence and structure | 0.34 → 0.15 | `Ambience` |
| monitor beep | 0.22 × the blend, heard only above blend 0.45 | `Ambience`, `AmbientEvents` |
| dust | 29 motes a second × the dust, emitted 2.2 m in front of the listener | `Ambience`, `HospitalAtmosphere` |
| valve steam | 6 puffs a second while the listener is within 11 m | `Ambience` |
| rainfall | 730 drops a second; fractions carry over; at most 90 in one frame | `rainfall` on the component, `RainField` |
| where drops land | within 14 m of the listener along x and along z | `RainField.Reach` |
| roof probe | straight down from 27 m to −2 m, on the `weatherGeometry` layers (Default) | `HospitalAtmosphere` |
| streak start | the higher of 2.7–9.5 m above the listener and 1.6–5.6 m above the roof | `RainField` |
| streak fall | at 16 m/s to 0.18 m above the roof; wind (1.8, 0.6) m/s along x and z | `RainField`; `wind` on the component |
| left out under cover | roof more than 0.6 m above the listener and streak start more than 12 m above the listener; counted in `RejectedByCover` | `RainField` |
| something in the way | the probe reaches 0.24 m past the streak's end; the streak ends 0.07 m short of the hit, after at least 0.02 s | `HospitalAtmosphere`, `RainField` |
| drop thickness | 0.013–0.026 m | `RainField` |
| splashes | every 7th drop, on ground below 0.35 m, while the blend is below 0.9; up 0.35–0.75 m/s, ±0.3 m/s sideways, 0.028–0.067 m, 0.14–0.22 s, 0.026 m above the ground | `RainField`, `HospitalAtmosphere` |
| no rain worked out | blend above 0.92, within 29 m east–west and 9.5 m north–south of the centre (the central block); along the facades and at the entrances it still rains | `RainField.IsSheltered` |
| first lightning | 15 s after the start | `thunderDelay` on the component |
| one flash | 0.7 s; intensity 34·e^(−22t) + 17·e^(−65·\|t − 0.19\|), t in seconds since the strike | `AmbientEvents` |
| between flashes | 20–39 s from the end of one to the next | `AmbientEvents` |
| thunder | once per flash, 0.65–1.5 s after the strike | `AmbientEvents` |
| monitor | first 4.5 s after the start, then every 7–16 s; 3–6.5 m to the right, 3 m ahead | `AmbientEvents` |
| creak | first 8 s after the start, then every 10–23 s | `AmbientEvents` |
| emergency light | level 0.5 + 0.5·sin(2π × cycles); the lockout fixtures: depth 0.5, 0.64 cycles a second | `LightPulse`; builder |
| intermittent light | dimmed while its Perlin noise is below 0.27, full from 0.27 + 1/3.5 ≈ 0.56, an S-curve between; every 13th fixture on each floor below the top one: depth 0.61, 0.9 cycles a second | `LightPulse`; builder |
| light intensity | its own intensity at level 1, × (1 − depth) at level 0, a straight line between | `LightPulse` |

- **Randomness.** The rain and the events draw from one seeded stream
  (`System.Random(2087)`, the Northline scene's seed), passed to `RainField`
  and `AmbientEvents`, and the component calls them in the Northline
  order: rain, then events. In the game the draws still depend on frame
  timing (the number of drops per frame does), so two runs differ; the
  shared stream is what let the old and new code be compared draw for
  draw.
- **Perlin noise** is the engine's (`Mathf.PerlinNoise`), which `Core`
  cannot call: `PracticalPulse` samples it at (cycles, phase) and
  `LightPulse.Intermittent` shapes the sample. The phase also picks the
  noise row, so neighbouring lights flicker differently.
- `LightPulse.Emergency` and `AmbientEvents.FlashIntensity` round their
  angle or power to a float before the double-precision sine or
  exponential, as `Mathf.Sin` and `Mathf.Exp` do; without that, the
  results differed from the Northline code by a few thousandths.
- The flickering "residual power" lights are `World`'s `ResidualPower`
  ([[systems/level-hospital]]), not part of this system.
- Read by others: the hospital checks read `InteriorBlend`, `EmittedRain`
  and `RejectedByCover`; the route benchmark logs the blend and the two
  loops' volumes ([[systems/performance]]).

More: Northline's own notes, [atmosphere system](../../Northline/Documentation/atmosphere_system.md).

## Tests

| Test | Kind | What it checks |
| --- | --- | --- |
| `Core/Tests/EditMode/ZoneBlendTests.cs` | 5 EditMode | inside, the S-curve outside a face, beyond the blend, every side alike, past a corner |
| `Core/Tests/EditMode/AmbienceTests.cs` | 10 EditMode | blend rate and stop, outdoors to inside in 0.71 s, every volume, dust, steam |
| `Core/Tests/EditMode/RainFieldTests.cs` | 20 EditMode | drops per frame and the 90 cap, spread, streak start and fall, left out under cover, something in the way, splashes, shelter, ranges, same seed same rain |
| `Core/Tests/EditMode/AmbientEventsTests.cs` | 11 EditMode | the flash curve, first flash after the delay, flash length and spacing, thunder once per flash, monitor timing, side and threshold, creak timing |
| `Core/Tests/EditMode/LightPulseTests.cs` | 9 EditMode | cycles, the emergency swell, intermittent dim, half and full, intensity |
| `Core/Tests/EditMode/EasingTests.cs` | 2 of 6 EditMode | `Lerp`, used for the volumes and the light intensity |
| `Atmosphere/Tests/PlayMode/EnvironmentalZoneTests.cs` | 5 PlayMode | a turned zone: inside, half and zero weight, a corner, the box turning with it |
| `Atmosphere/Tests/PlayMode/HospitalAtmosphereTests.cs` | 8 PlayMode | on a stand-in site the test builds: rain outdoors, loud rain and wind outside, nothing drawn under a roof 12 m up, the blend rising at 1.4 a second and the crossfade, dust ahead, the valve steaming near it, the monitor beeping inside only |
| `Atmosphere/Tests/PlayMode/PracticalPulseTests.cs` | 2 PlayMode | an emergency light swinging over one cycle, an intermittent one within its depth |
| `HospitalChecksTests` | 3 of the 95 checks | rain emitted outdoors, streaks left out under the roof, interior blend above 0.80 in reception |

The PlayMode tests were written first and passed on the Northline code.
Before the pull request, a comparison ran the Northline code and the new
code side by side on a test site (open ground, a building with a roof, a
roof 12 m up, four zones including a floor-0 one, three pulsing lights)
for 3,781 frames of uneven length, four of them 0.25 s long, 110 s in all.
A listener walked up to the building, through it, out, and under the high
roof. Every compared value was identical: zone weights, the interior blend,
both loops' volumes, the lightning, dust and steam, the monitor's position,
the three lights, and every rain and splash particle (position, velocity,
lifetime and size; 36,937 streaks, 16,886 left out under cover, 1,229
frames with no rain worked out). Thunder and creaks cannot be watched
directly, but they draw from the same random stream as the rain, which
stayed identical after each of them.

The hospital checks' counts (drops emitted, streaks left out) change from
run to run with the frame timing: 748 and 129 before #42, 741 and 134 after.

## Known problems

- `HospitalAtmosphere` still does several jobs in one component (ambience,
  rain, events). Splitting it means changing the scene through the
  builders, which #44 reworks.
- The central block's footprint (29 × 9.5 m) and the 27 m probe height are
  the hospital's numbers in code; a second map would need them as data.

## Open questions

- Which of these numbers become data (a weather or map ScriptableObject)
  once there is a second map or the weather changes during play.
