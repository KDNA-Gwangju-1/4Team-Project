# Hospital arrival and transition review v2

- Local edit only; no generation credits used.
- Output: `pixel-to-game3d-review-v2.mp4`, 1920x1080, 60 fps, approximately 10.675 seconds.
- Pixel arrival starts at 1.25 seconds instead of 2.55. Left/right approach durations extend to 2.20/2.80 seconds, preserving foot plant times and final framing.
- Original fixed architecture and animated reflections retained. No temporal frame blending added to the walking animation.
- Pixel-to-3D dissolve lasts 0.35 seconds from 4.45 seconds; the 3D first frame is held during the dissolve. Some contour overlap remains because the two source images differ.
- Uses v1 graded 3D source. Its native 24 fps frames are repeated in the 60 fps output; this does not create new gait poses.
- Footsteps retained from the pixel sequence. The generated 3D footage has no audio.
- Full doorway entry was NOT added. The generated clip stops short, with inconsistent character/door scale; a credible extension requires new motion and reliable occluded background, not merely shrinking/sliding the last frame.
- Validation: complete FFmpeg decode succeeded; arrival and transition contact sheets inspected.

Reproduce with `render-arrival.cjs`, followed by `render-join.ps1`. These reuse the retained v3 renderer/assets and v1 graded source.
