# Hospital entry — locally rendered six-part rig

1920×1080, 60fps, 10.8 seconds. All video frames rendered locally using existing separated PNG assets and joint transforms. No Higgsfield, external video generation, free credits, pose crossfade or whole-body image warp used. Approved opening unchanged.

Fixed camera, distance-based character scale, hospital approach, right-hand door push, entry. Door remains open; cut ends before closing. Existing temporary footsteps/door foley reused, cropped before the old closing sound.

rig.cjs imports the six parts from ../detective-part-rig-v1. Leg joints use the previously authored gait. Right arm uses two-bone IK targeting the door handle. Numerical wrist-to-handle anchor error is recorded in manifest.json; this does not measure finger grip fidelity. Per-frame clear prevents temporal accumulation. Both body-part transparency at edges and ordinary part overlap are distinct from temporal ghost effects.

Validation: full MP4 decode passed; walking and contact/entry snapshots visually inspected. Remaining limitations: 2D cutout shoulders/elbows and foot-roll textures still stylized; this is not motion capture or anatomically validated human locomotion. Reaching is planar IK, no articulated fingers.

No integration into the approved opening. Previous versions retained.
