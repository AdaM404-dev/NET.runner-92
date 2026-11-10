# Render settings

The intro renders at 1920 × 1080 and 24 fps with 1,440 numbered picture frames. The edit uses real Blender camera cuts and animated geometry. Color is AgX with the Medium High Contrast look. Depth of field and motion blur are evaluated from the Blender cameras and moving objects.

Eevee rendered the motion sequence. The 1080p Cycles OptiX test took roughly 20 seconds for a frame after warm-up, making it unsuitable for the full 1,440-frame sequence on this workstation. Cycles is reserved for representative stills. Rendering at 1080p prioritizes completing the sequence at the requested native 24 fps. See [render progress](render_progress.md) for the render checkpoint.

The animatic uses 480 × 270 at four sampled frames per second with the full-duration master. The final render writes the full-rate PNG sequence before H.264 encoding in the Blender Sequencer and muxing with the 48 kHz stereo AAC master. The frame files let production resume from the first absent frame and permit re-encoding without rerendering geometry.

The Eevee picture uses 12 temporal samples, one shadow ray, five shadow steps, a 0.24-frame motion-blur shutter and selective camera depth of field. The 1 GB shadow pool accommodates the interior practicals. The exterior was completed before the pool expansion without a shadow-capacity error. The isolated title still uses Cycles OptiX, up to 40 adaptive samples and denoising at 3840 × 2160.

