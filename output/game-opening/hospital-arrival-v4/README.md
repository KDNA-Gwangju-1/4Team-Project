# Hospital arrival v4 — single opaque silhouette

Existing approved opening is preserved, not integrated.

- 1920×1080, 60fps, 11.6s, 72 samples per 1.2s gait cycle.
- Removed all pose crossfades. A single source character is deformed to animated joint targets; no second semi-transparent character is drawn.
- Rasterized the deformation mesh directly with nearest sampling and binary character alpha, eliminating antialiased triangle seams and semi-transparent doubled contours.
- The walk, reach and release share that one source silhouette. This is procedural 2D mesh animation, not newly drawn anatomical frames or 3D animation. Texture deformation can still be apparent; treat this as a ghost-removal review, not final character animation approval.
- Continued the same walking phase across the doorway instead of resetting at 8.25 seconds.
- Existing sound timing and building art preserved.

Separate requested Higgsfield 3D-style experiment is blocked by the current free plan/zero credits. No 3D video has been generated and no purchase was made. See ../hospital-style-transition-v1/STATUS.md.
