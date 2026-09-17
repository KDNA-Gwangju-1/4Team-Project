# TV fit and visible cloud drift — 2026-09-17

Versioned correction of opening-approved-20260917-v1; previous exports untouched.

- 1920 x 1080, 30 fps, 999 frames, 33.3 seconds.
- CRT glass measured from the original lobby: x=1520..1790, y=598..758, rounded corners. The 16:9 programme is inset vertically, with extended edge scanlines filling the small remaining glass area.
- One monotonic camera transform throughout the TV approach. The same programme transform continues through the bezel-to-fullscreen handoff; no scale reset.
- Existing sky artwork translated with overscan, no generated replacement art and no wrapping seams. Approximately 287 screen pixels of horizontal cloud travel over 12.4 seconds (23.1 px/s), including ringing and dialogue. This is translation of the existing sky plate, not simulated volumetric cloud deformation.
- Existing dialogue, music, sound effects and timing retained; master AAC packet hash checked against the restored original.
- Hospital segment is not included.

Run render.cjs using the bundled Node runtime, then finish.cjs for full decode validation and an under-10-MB review encode. Source paths are relative to the parent game-opening folder. window-environment.png is copied unchanged from the approved prior version.
