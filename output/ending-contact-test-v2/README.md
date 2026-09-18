# Corridor contact study v2

Standalone review only. 1920x1080, 30 fps, 90 frames, 3 seconds, silent.
Not integrated into Unity or the ending cinematic.

Uses original corridor-exit-keyframe-v1.png character texture and the existing
corridor-clean.png plate. Connected triangular deformation replaces separately
normalized sprites. No frame blending, motion blur, or Higgsfield generation.

The two planted control anchors remain stationary during their stance phases
(validation.json: zero control-anchor displacement). This is not validation of
an anatomically correct walk: a single pose cannot redraw shoe soles, articulate
occluded knees, or provide correct clothing folds through a complete gait cycle.
The resulting movement remains a limited contact study, not final exit animation.

Contact sheet inspected; triangle raster seams corrected with overlapping clips.
Complete MP4 decoded successfully with FFmpeg. No audio or door action included.

Run render.cjs from repository root using Node and the configured local Canvas
and FFmpeg paths in the script. Inputs remain in their existing versioned folders.
