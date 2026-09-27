# Chapter 2 sister reveal revision

## Problem and implementation

The 20-frame dissolve used a stretched scene anchor (2.15, 2.85), then switched to a separately drawn SisterResolved sprite and recalculated its width to 2.4 world units. That switch changed style, silhouette, scale and position at the dialogue boundary.

The replacement uses one `SisterUnified` sprite for the entire reveal and dialogue. Its seated height is 1.08 world units (previous final sprite: approximately 1.845), with a bottom pivot at the girl's feet, not the centre of the girl-and-toy group. Anchor X still stages the scene; Y uses the authored ending floor, -2.63. Scaling is uniform. The old serialized fields and source frames are retained for compatibility, but intermediate drawings and the separate final sprite no longer drive this ending.

The original intact nightmare image is a separate foreground shell, uniformly scaled. A resource shader erodes fixed pixel clusters from head to feet over the existing dissolve duration. The same child drawing is revealed underneath; there is no final art swap. The shell and its material are removed after the dissolve and on destruction. Unsupported shader platforms use a simple shell fade.

## Artwork

Built-in image_gen, identity-preserving reference edit. Output: `Assets/Resources/UI/ChapterSkins/SisterUnified.png`. The image retains its generated transparent alpha. Unity imports a sprite rectangle (289,147,784,578), pivot (0.32,0), point filtering and uncompressed texture. No image manipulation script was used.

Prompt: Create one transparent production sprite of the existing seated elementary-school girl, knees together, head buried in crossed forearms, brown low twin ponytails with red ties, blue-grey pajamas and dark slippers. Retain the original dissolve-final-frame proportions and coarse pixel style; use the separate resolved image only to clarify the objects. Keep the grey unicorn plush with dusty pink mane and gold horn on her right, on the same baseline. Discrete square pixel clusters, subdued palette and dark outline; no painterly smoothing, ground island, glowing rubble, monster, particles, text or border. Use this exact fixed pose throughout reveal and dialogue.

## Validation

- Unity imported exactly one sprite with the requested rect and pivot.
- Shader supported; ShaderUtil reported no shader errors. Unity Console reported no errors after compilation and the dissolve test.
- Play-mode test on BadDream_Stage3, using the existing PlayNow shortcut; the boss introduction was stopped only in transient test state to avoid two cutscenes competing for the camera.
- Captured intact shell, 60% erosion and final two-shot. Ran the actual DissolveFrames coroutine and verified shell cleanup.
- Before and after reveal: child bounds height 1.08; feet Y -2.63; sprite SisterUnified. No final scale or sprite assignment occurs.
- Final full ending retested with production timings through the dialogue portion. This does not represent a full boss-fight playthrough or a packaged-build test.

Review screenshots are in ignored `output/sister-reveal-review/`.
