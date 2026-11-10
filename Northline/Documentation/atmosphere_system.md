# Real-time atmosphere

The scene uses HDRP Lit surfaces, a low-luminance overcast sky, a cool directional fill and short-range practical fixtures. Emissive materials mark powered displays. Selected practicals use the Inspector-configurable `PracticalPulse` component. The global profile uses ACES, restrained bloom, screen-space ambient occlusion and reflections. The player camera uses TAA.

`EnvironmentalZone` defines a spatially weighted volume, an interior blend and a dust target. The exterior has a global fog and lighting profile. Reception has its own subtle local volumetric fog and public and ambulance intake transitions. Wards have reduced practical output and more dust. Surgery has cooler task light and lower dust. Research is cleaner, with more consistent light and less haze. Local HDRP profiles blend exposure and mean free path at the zone edge.

Wind-driven rain emits in a bounded area around the player. Downward and wind-direction physics probes clip each streak to roofs and other high surfaces. Local impact particles appear on exposed ground. Rain sampling pauses on all four floors while the player has an interior blend above 0.92 and is within the central footprint (`abs(x) < 29 m`, `abs(z) < 9.5 m`). The probes still run along the facade and entrance transitions. Dust follows the local zone. Steam is limited to a damaged service valve and emits only near the player.

The five included PCM clips are original procedural sound effects created for this project. Rain and ventilation crossfade by zone. Thunder, medical monitor signals and fence creaks use localized AudioSources. The Northline AudioMixer has Exterior, Interior and Mechanical groups. The phase does not reuse the cinematic music.
