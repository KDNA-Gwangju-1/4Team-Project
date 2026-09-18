# 2026-09-17 — separate review cuts, NOT integrated

User requested review before integration. Previous final MP4 remains unchanged.

- `tv-zoom-review-6s.mp4`: 1920x1080, 30fps, 6-second transition review. Single camera zoom, fixed news composition, existing mouth frames and audio reused. Last 1.2 seconds show the existing interview cut for context.
- `check-270.png`: expert caption review, one line at 48px.
- `check-429.png`: window composition review, 1920x1080.
- `window-environment.png`: reference-guided environment artwork, built-in image generation. See art-prompt.md.
- `render.cjs --stills`: regenerate review images.
- `render.cjs --transition`: regenerate the 6-second zoom review only.

No full revised opening has been rendered. No hospital scene included. Integration requires user approval. Video passed full FFmpeg decode. Zoom geometry is monotonic and ends at exactly 1920x1080; image cuts were visually inspected. The window's final art direction remains subject to user review.
