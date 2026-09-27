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

## 2026-09-27 — smaller seated child and cleaner dissolve

- User feedback: the seated child still reads too large beside the player; the disappearance looks dirty.
- Replaced the 1.08 world-unit height with a 0.78 upper limit (27.8% smaller), also capped at 40% of the ending detective renderer bounds height. Scale remains uniform and the feet pivot remains at the authored ground. BadDream_Stage3 explicitly stores both settings.
- Removed the shader’s two-dimensional random erosion and added gold edge. A narrow, stepped top-to-bottom boundary now removes the original shell without spawning isolated mask fragments. Original sprite artwork, including any detail painted into it, is unchanged.
- Static validation: sampled the shader mask on a 512 x 512 grid at 101 progress values. Initial visibility is 1 everywhere, final visibility is 0 everywhere, and no pixel becomes visible again. This checks the mask math, not GPU compilation or visual quality.
- Scene validation: only the two size settings were added; all existing references, dialogue and timings were preserved. Pre-edit copies are in output/sister-reveal-cleanup-20260927/.
- Unity runtime and shader compilation are unverified for this revision: no connected Unity tool is available, and local process execution failed. Earlier runtime results above apply only to the previous revision.
- Manual acceptance: in BadDream_Stage3 play the actual boss defeat (or existing PlayNow shortcut with the introduction finished). Inspect the intact shell, 25/50/75% removal, and final dialogue two-shot. Check smaller seated proportions, fixed feet, no added gold outline, no scattered mask islands, and no sprite/size jump when dialogue starts. Check the Console for C# and shader errors and complete the ending transition.

## 2026-09-27 — clean authored keyframes

The eight-frame SisterRevealClean sheet now takes precedence over the shader reveal above. See SisterRevealClean/README.md for prompts, sprite registration, timings and unverified Unity playback checks. The same final sprite stays through dialogue.
