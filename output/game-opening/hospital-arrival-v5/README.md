# Hospital v5 — ghost-artifact audit

Approved opening untouched. Genjutsu not invoked; free generation unused.

Changes from v4:
- Clamp deformation to retain positive triangle orientation and at least 25% of original signed triangle area. Prevents local mesh foldover/double mapping.
- Explicitly clear every frame before drawing.
- Shade into the character's own sprite, then draw the character once, fully opaque.
- Integer pixel placement, nearest sampling, no temporal blend, no motion blur.

Validation: 124 generated walk/reach/release poses, binary alpha only, no inverted triangles. Both output MP4s decoded fully. Encoded gait/contact sheets inspected. This tests compositing defects, not anatomical correctness.

Remaining: procedural single-image deformation still gives unnatural feet/limited arm swing, and handle contact does not stay locked throughout the door rotation. NOT approved as a Genjutsu driving video or final animation. Do not claim a finished natural walk. Further work needs authored poses or a proper articulated animation source, not more crossfading.

Deliverables: hospital-entry-1080p.mp4 (11.6s/1080p/60fps), walk-smooth-review-1080p.mp4 (4.8s enlarged walk), deformation-audit.json, render-metadata.json, gait-check.png, entry-check.png.
