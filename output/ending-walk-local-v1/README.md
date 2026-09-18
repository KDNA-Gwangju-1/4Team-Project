# Local pixel corridor animation — blocking review v1

Delivery: corridor-exit-silent.mp4, 1920x1080, 30fps, 11 seconds, 330 output frames, silent. Not integrated into Unity or opening movie.

No Higgsfield tools, credits or video generation used. Built-in image_gen/imagegen skill prepared clean background, eight walking poses and four door-action poses. Local Node Canvas code assembles sprite animation, perspective movement, hinged door projection and doorway occlusion. FFmpeg encodes H.264. No optical flow, crossfade between poses, motion blur or accumulated frames.

Timing: 0–0.35 fade in; 0.35–5.35 approach; 5.35–6.1 stop/reach; 6.1–7.1 open door; 7.1–9.3 exit behind jamb; 9.3–10.4 empty corridor; 10.4–11 fade out.

This is a blocking draft, NOT finished character animation. Eight unique walking drawings are held across 30fps output; 30fps does not imply 30 unique drawings per second. Hand/handle contact, transition to standing, pose consistency and foot sliding need further review. Exit is 2D staging/occlusion rather than simulated 3D anatomy. No sound or preceding embrace added yet.

Re-run render.cjs using bundled Node. Paths to Canvas/FFmpeg are at script top. Clean background/door opening/pose sheets are preserved here; output poses and motion.json are regenerated. Source image: ../ending-v1/pixel/corridor-exit-keyframe-v1.png.

Prompt record (built-in image generation):
- Clean plate: remove only detective/shadow; retain corridor camera, closed door, lighting and pixel style.
- Walk: eight rear-view contact/down/passing/up poses with alternating legs and arms, consistent brown fedora, tan knee coat, dark trousers, brown shoes; transparent 4x2 sheet. First reference-bound sheet was rejected for repeated poses; current sheet generated with explicit alternating gait instructions.
- Door poses: transparent 2x2 sheet of same character stopped, reaching with right hand, pushing, stepping through; connected anatomy, stable scale.
- Opening plate: remove left glass door leaf, handle and bar only; show outdoor terrace/foliage; preserve right panel and architecture. Only the aperture crop is composited, so background outside it stays fixed.
