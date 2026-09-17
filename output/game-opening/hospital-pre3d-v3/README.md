# Hospital depth entrance review v3

`hospital-depth-before-3d-1080p.mp4`: 1920×1080, 60fps, 6.4 seconds, temporary approach footsteps.

Replaces the rejected left-to-center movement with a fixed-camera, centerline depth entrance. World lateral position remains zero throughout. Each foot advances in depth and stops at 3.45 / 4.05 seconds. Perspective projection makes the near-camera legs initially cropped, then reveals the rear shoes, trousers and coat hem as the detective advances away and stops. The final pose holds until 6.4 seconds. Small coat settling is retained.

This is a local 2D depth-projected sprite review, not a true 3D skeletal animation. No Higgsfield generation, 3D transition, door movement, or integration with the approved opening.

Dependencies: coat sprite and reflections module in `../hospital-pre3d-v2/`; background and temporary foley in `../hospital-pre3d-v1/`. The renderer uses the bundled Node Canvas and project FFmpeg paths declared at its top.

QA: sampled arrival/movement/stop frames reviewed. Projected strip heights corrected to avoid horizontal gaps. Full MP4 decoded with FFmpeg without reported errors. Manifest includes per-frame world lateral position and depths, plus reflection-only mask audits. Earlier versions preserved.
