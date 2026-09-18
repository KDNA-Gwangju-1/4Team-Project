# Arrival gait timing revision

Local pixel animation revision only. No generated footage or paid generation used.

- Left approach/lift: 1.25–2.95 seconds; right: 1.65–3.55 seconds.
- Each foot's lift is now driven by the same phase as its approach, instead of an extra late 0.52-second lift after the silhouette appears.
- Both feet stay planted after 3.55 seconds. Existing slight coat settling remains.
- Existing footstep track advanced by 0.5 seconds to follow the earlier plants.
- 0.35-second pixel/3D transition and graded 3D source retained from v2.
- This is still a 2D sprite deformation, not a newly simulated anatomical 3D walk. Complete hospital entry is deferred to a separate 3D scene task.
- Output: pixel-to-game3d-review-v3.mp4, 1920x1080, 60 fps, approximately 10.675 seconds. Original 3D motion remains native 24 fps.
- Validation: arrival contact sheet inspected; full output decoded without errors.

Run render-arrival.cjs with Node, then render-join.ps1 in PowerShell. Source dependencies remain in the sibling asset directories.
