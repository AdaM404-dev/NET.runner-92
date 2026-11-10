# Phase 3 quality assurance

## Hospital

The original four-floor fit-out remains in the upgraded project. All 3,225 original objects retain their transforms and collection assignments. The 3,020 original meshes retain their topology and vertex coordinates. The new architectural elements cleared 2,160 doorway rays, 36 concourse samples and 132 stair headroom samples. Five 1080p Cycles stills were reopened and reviewed. See [visual upgrade and validation](visual_upgrade.md).

## Blender source and picture

- Reopened the independent cinematic source. It has 10 animated picture cameras, a black plate camera, camera-bound cuts and a 1,440-frame relative stereo strip.
- Sampled 86 camera path segments against scene geometry. The solid geometry cleared those samples. The exterior path enters the intended rain volume.
- Checked the six checkpoint leaves open and closed, the surveillance pan toward the filming lens, the resuscitation display in standby and live states, and the final status before and after boot.
- Reviewed the entire edit as a 240-frame animatic and inspected final native-resolution images at cuts and events.
- Verified that the title asset opens with separate editable and baked collections. The baked main wordmark has 6,103 polygons. The main title and final monitor read **NET.runner-92** and **SYSTEM STATUS: ACTIVE**.
- The production rerender corrected an Eevee shadow-capacity overflow in reception. The larger pool remained stable in the replacement render. The first two chapters were retained, and three small animation corrections were rendered separately and integrated.

## Integration scope

The deliverables are Blender source assets and a rendered cinematic. Unity material baking, collision setup, runtime lighting, navigation and interactive access behavior remain engine tasks. Weather, shutter movement and automated equipment cues are authored for the film.

## Final master

The H.264 MP4 is 69,847,281 bytes and reports 60.00 s, 1920 × 1080 at 24 fps, and AAC stereo at 48 kHz. Every image in the 1,440-frame sequence passed PNG integrity and dimension checks, and every video frame decoded. Twenty-nine frames sampled across cuts and events averaged 1.712 levels per channel of absolute difference from their PNG sources on an 8-bit scale. All 36 blackout frames decoded at a maximum channel value of 2. The decoded audio matched the original master at 0.999786 correlation with no offset applied. The tail from 58.55 s decoded to exact zero.

MP4 SHA-256 `A36D0C225565C3DE1E205F847F655F2CBFFE860B59241D3A5EB939A471893BFD`.

All nine selected stills passed PNG integrity checks. Eight are 1920 × 1080 and the title still is 3840 × 2160.
