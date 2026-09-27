# Integrated opening — 2026-09-17

User authorized integration after review. Previous videos and variants are preserved.

- 1920x1080 / 30fps / 999 frames / 33.3 seconds.
- Scene 1 uses original daytime lobby and birds, NOT cyberpunk concept.
- TV-to-news uses one monotonic camera zoom and stable news framing.
- Expert caption is a single 48px line; existing interview animation retained.
- Foreground tree version of window pose selected by user. Upper-left facade filled behind leaves; no ivy. Both hands touch window.
- Camera environment extended from corrected plate; 2D crop pan follows upper-left gaze, then sky-only blend. Cloud drift and call continue on existing schedule.
- Existing complete sound mix is stream-copied: no new voice/music or timing changes.
- Hospital arrival is excluded.

## Files

- opening-final-1080p.mp4: master.
- opening-review-under10MB.mp4: sharing/review copy.
- window-approved.png: corrected reference shot.
- window-environment.png: extended camera plate.
- review-sheet.png: scene overview.
- render.cjs: local renderer; --stills regenerates review frames only.
- finish.cjs: full decode checks, identical audio hash check, two-pass sharing encode.
- manifest.json: hashes, sizes and validation results.

The video-editing skill was inspected; the existing local Canvas/FFmpeg timeline was retained to preserve all timing and audio. Artwork was edited with built-in image_gen; see art-prompts.md.
