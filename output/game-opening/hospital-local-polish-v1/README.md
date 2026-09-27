# Local brightness and pixel-to-3D join review

No generation or Higgsfield credits used. Sources retained unchanged.

- 3d-brightness-corrected.mp4: progressive gamma lift with highlight attenuation, 1920x1080, source 24fps. Source duration is 5.875s, despite nominal six-second generation.
- pixel-to-game3d-review.mp4: pixel arrival trimmed to 4.65s; 0.20s stationary dissolve starts at 4.45s. The 3D opening frame is held for the dissolve before its original movement starts. Output 60fps duplicates source frames; no optical-flow interpolation. Original pixel footsteps retained, faded before the silent 3D section. Total approximately 10.525s.
- render.ps1: reproducible local FFmpeg edit.
- grade-review.png / join-review.png: sampled visual checks.

Measured upper-left background mean Y declined from 126 at the first sampled second to 51 at the last in the generated source. Grading lifts midtones progressively, attenuating highlights. This cannot restore lost character texture. Some residual lighting variation/haze remains.

Known limitations: shoe/coat shape and closed-to-open doorway differ across source images; the short stationary dissolve reduces the abrupt change but is not a true geometry morph. Complete hospital entry is still absent/unverified; no missing motion fabricated. Not integrated with the approved full opening.
