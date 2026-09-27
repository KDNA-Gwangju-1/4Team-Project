# RE:Dream title revision

Changes only the title treatment at 30.3–33.3 seconds in the existing integrated opening/hospital movie. Previous video is retained locally as original.mp4. Delivery: OpeningAndHospital-REDream.mp4, also copied to Assets/StreamingAssets/Cinematics/OpeningAndHospital.mp4.

Old lettering is covered by a neighboring background patch; centered RE:Dream uses Consolas Bold, pixel-stepped 2x text rasterization and a 0.35-second fade-in. No scene timing or audio edits. Video is re-encoded H.264 Baseline, no B-frames, BT.709, audio stream copied. Full decode passed; title still inspected. Existing game menu title is not changed.

video-editing skill reviewed; native Higgsedit unavailable locally, existing FFmpeg/Canvas pipeline used instead. Reproduction script: replace-title.cjs. Requires bundled Node, @napi-rs/canvas and FFmpeg at paths declared in the script.
