# Phase 3 final delivery

## Master and editable projects

| Deliverable | Contents |
| --- | --- |
| [Final introduction](../Renders/NET_runner_92_Intro.mp4) | 60.00 s at 1920 × 1080 and 24 fps. H.264 picture and 48 kHz stereo AAC. The MP4 carries the music credit in its metadata. |
| [Upgraded Northline Medical hospital](../Environment/Hospital_Final.blend) | Four connected furnished floors, revised finishes and practical lighting, exterior branding and architectural service details. |
| [Editable cinematic](../Cinematic/Intro_Cinematic.blend) | Camera edit, animated mechanisms and displays, rain and haze, real 3D title, lighting and a timed relative audio strip. |
| [Reusable 3D title](../Cinematic/Title_Assets/NET_runner_92_3D_Title.blend) | Two collections at a local origin, one with editable beveled typography and one with baked meshes. Append the collection that fits the next use. |
| [Pre-upgrade hospital backup](../Environment/Hospital_Pre_Visual_Upgrade.blend) | Byte-identical copy of the assembled four-floor Phase 5 source. |
| [Animatic](../Renders/Previews/NET_runner_92_Animatic.mp4) | Earlier motion preview with the full stereo mix, retained as a production checkpoint. |

The [three frame archives](../Builds/README.md#cinematic-assets) restore `Renders/Final_Frames` with `f_0001.png` through `f_1440.png`. Keep the `Audio` folder next to the cinematic Blender file. Use the [stereo master](../Cinematic/Audio/Intro_Master.wav) alone, or the [music](../Cinematic/Audio/Intro_Music.wav) and [effects](../Cinematic/Audio/Intro_Effects.wav) stems together.

## Stills

- [4K title](../Renders/Stills/Intro_Title.png)
- [Storm approach](../Renders/Stills/Intro_Storm_Approach.png), [entrance](../Renders/Stills/Intro_Entrance.png) and [resuscitation](../Renders/Stills/Intro_Emergency.png)
- [Hospital exterior](../Renders/Stills/Hospital_Exterior.png), [reception](../Renders/Stills/Hospital_Reception.png), [wards](../Renders/Stills/Hospital_Wards.png), [surgery](../Renders/Stills/Hospital_Surgery.png) and [research checkpoint](../Renders/Stills/Hospital_Research.png)

## Verification and handoff

All 1,440 source PNGs passed integrity checks, and the full MP4 decoded at the required frame rate and dimensions. The audio round trip matched the original stereo master, with silence after the final cut. The movie uses 1080p to make the full-rate workstation render practical. The hospital stills and 4K title use Cycles. See the [QA report](qa_report.md), [render progress](render_progress.md) and [camera setup](camera_setup.md).

The four-floor route and original mesh geometry remain in the environment file. Unity material conversion, collision, navigation and interactive access logic remain open. The film keeps the final lab and archive out of view.

Music is **Signal to Noise** by Scott Buckley, edited and mixed under CC BY 4.0. Keep the credit and license link with any distributed copy. [Music license and attribution](music_license.md)
