# Chapter 2 phase 2 boss polish

## Image

- Built-in imagegen refined the original eight-pose sheet. Original and generated source files are preserved here.
- `tools/ChapterPolish/pack_boss_pixels.ps1` packs the generated art into the original eight-frame order and silhouette extents, removes disconnected cell spill, snaps to a 256px logical grid, and expands each pixel to an exact 4×4 block.
- Final game atlas: `Assets/Sprites/2d_sprites/Boss/boss-phase2-claw-8f.png`, 8192×1024, eight 1024px frames, binary alpha. Point filtering, no mipmaps or compression.
- Sprite GUID, internal IDs, names and pivots are preserved. Rectangles and pixels-per-unit both doubled (234 → 468), preserving world dimensions and existing animation references.
- `pixel-contact-sheet.png` is the review preview. `pixel-validation.json` confirms the integer grid and alpha.

## Retry correction

The authored scene's phase 2 dialogue uses transformation beat 2 but no ascent/burst beat 3. Normal transformation scales the phase 1 reference to `phase2Width = 9` before switching sprites, producing a phase 2 frame width of 13.31792 world units. Retry incorrectly used the unused burst width of 30.

Both paths now share combat sizing. Retry selects the appropriate endpoint from the authored timeline; normal scenes retain their existing visual size. The optional burst still uses its own authored width. Retry also disables the background parallax/bob components, as the normal transition does.

Validation: Unity compilation and Console error checks passed. The real transformation coroutine was advanced with shortened waits; three calls to the retry endpoint remained at 13.31792. Reloading the scene with the same flags used by the retry UI reached phase 2 with the same size. All eight sprite references resolve. This is targeted transition/reload validation, not a full boss gameplay playthrough.

## Imagegen prompt

Edit the provided game boss sprite sheet for clean pixel-perfect production use. Preserve the exact character identity: creepy small dark-haired girl with red hair ties, cream outline, black shadow dress, clutching a small grey plush rabbit, growing a long black claw during attack. Preserve EXACTLY all 8 original animation poses and their chronological order, same character proportions, anchored feet and head size across all frames. Remove ALL red/black background rectangles; background must be fully transparent alpha, including between claws and limbs. Clean intentional crisp pixel-art edges, limited coherent cream, charcoal, muted brown and red palette, no blur/no antialiasing/no glow, sharper readable fine details. Deliver eight equal square frames arranged in a 4 columns by 2 rows grid, order left-to-right then top-to-bottom: neutral, windup left, claw overhead, swipe forward, fully extended swipe, downward follow-through, recoil, neutral. Same scale in every cell, each figure entirely within its cell with transparent padding, feet baseline at 84% of cell height. Largest high-resolution output possible, suitable for exact integer nearest-neighbor upscale. No text, no labels, no grid, no extra characters. This is a refinement of source art, do not redesign.
