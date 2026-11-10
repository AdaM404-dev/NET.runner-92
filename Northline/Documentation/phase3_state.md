# Phase 3 state

The visual upgrade and cinematic delivery are complete. Open the [delivery index](final_delivery.md) for the MP4, the editable projects, title library, stills and audio. [QA results](qa_report.md) records the checks for the upgraded building and final media.

## Source checkpoints

- The fully furnished source is `Hospital/Phase_05/NET-runner-92-hospital-phase-05.blend`. All four floor fit-outs are present. The backup in `Environment/Hospital_Pre_Visual_Upgrade.blend` matches its SHA-256, `F3467AE73613D90F6F6CE6C43A39EBF20F2F17C46931752EDBF60F60FA583242`.
- `Environment/Hospital_Final.blend` is the upgraded building source. See [visual upgrade](visual_upgrade.md) for geometry and circulation validation.
- `Cinematic/Intro_Cinematic.blend` is the 1,440-frame camera edit. It opens on the resolved title. The `Audio` directory holds the relative stereo master and stems. The last shot cuts to black at frame 1405.
- `Renders/Final_Frames` holds the complete numbered PNG sequence. To revise a shot, use the original frame range from the [storyboard](cinematic_storyboard.md), leave picture rendering independent of the Sequencer and replace the affected frames before re-encoding.
- `Renders/NET_runner_92_Intro.mp4` is the verified master. The music credit is in the metadata and [licensing record](music_license.md).

## Next development stage

Unity work includes colliders and player movement, material conversion, level streaming and lighting, then access and interaction scripting. The film-only weather and automation are in the separate cinematic source.
