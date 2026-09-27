# Rigged nightmare grasp — 2026-09-25

## Requested revisions
- Restore the previous full-body purple weakpoint tint. Removed the small floating-core presentation from the active controller; damage rules and purification counts are unchanged.
- Replace the static curled mesh with a Blender-authored, skinned hand: palm bone plus three joints for each of five digits (16 bones, 27,326 vertices).
- Dorsum faces upward; four fingers flex beneath the torso and thumb opposes them. The 1-second authored OpenToGrip clip is sampled by the existing exit sequence over 0.85 seconds before attachment.
- Added rounded anatomy, metacarpal tendons, joint creases, nails, stitched dorsal patch, finger seams, crimson cuff and fine fabric surface shading. The material uses stable UV coordinates during deformation.
- Correct wrist aim for the contact point below the palm; preserve torso contact through animation and shrink. Reach duration 1.4 seconds, drag duration 2.2 seconds.

## Validation in Unity Editor
- Blender MCP local add-on generated the rigged FBX and source .blend successfully.
- Imported 16 weighted bones and a 1-second animation clip. Baked mesh vertices change between open and closed poses.
- Actual boss-stage exposure: IsExposed true and material RGBA(0.25,0.05,0.35,1); screenshot weakpoint-restored.png.
- Actual defeat sequence: HasGrabbedBoss true, Closure 1, grip-anchor-to-Spine distance 0, hand up vector approximately (0.01,0.96,-0.28). Screenshots grasp-runtime-side.png and grasp-runtime-fingers.png.
- At the end of drag, Closure remained 1 and anchor error was 0. The editor pause/resume skipped the intermediate capture; drag-contact-errors.txt contains only one sample, not a complete per-frame sweep.
- Sequence completed, IsPlaying false, HasGrabbedBoss false, runtime hand destroyed. Runtime invulnerability and disabled boss AI were used solely for observation and discarded on exiting Play Mode.
- Preview screenshots from edit mode do not reliably refresh skinned deformation; use runtime grasp screenshots as visual evidence.

No full standalone build or end-to-end manual playthrough was performed.
