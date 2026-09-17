# Hospital pre-transition central review v2

Deliverable: `hospital-central-before-3d-1080p.mp4` — 1920×1080, 60fps, 6.4 seconds.

- Static hospital view with foliage-only reflection displacement. Glass frames, handles and highlights are excluded by inset polygons and foliage color mask. Mild sunlight intensity modulation does not transform geometry.
- Approaching temporary synthesized footsteps; lower-body foreground arrives from screen left and settles centrally. Only trouser ends, rear shoes and cropped coat hem appear.
- Last foot plants at 3.45 / 4.05 seconds; 2.35-second hold; small damped coat-hem settling motion.
- Local sprite choreography, not a full skeletal gait simulation. Upper leg strips stay attached beneath the coat; no alpha trails, optical-flow interpolation or motion blur.
- No Higgsfield call, no 3D transition, no opening integration, no hospital door movement.

`render-central.cjs` uses the coat sprite in this folder and the existing background and temporary foley from `../hospital-pre3d-v1/`. `reflections.cjs` defines the restricted movement mask. `central-manifest.json` contains timing and uncompressed reflection audit samples. All audited samples report zero changed pixels outside the foliage mask (before lighting and character layers). MP4 fully decoded with FFmpeg without errors.

Existing approved opening and earlier reviews remain unchanged. This is a separate review, not a final approved replacement.
