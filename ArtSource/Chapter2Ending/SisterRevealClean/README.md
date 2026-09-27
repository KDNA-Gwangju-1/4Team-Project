# Clean boss-to-sister reveal — 2026-09-27

## Delivery
- Asset: Assets/Resources/UI/ChapterSkins/SisterRevealClean.png, 1774 × 887 RGBA, eight named sprites in its Unity importer metadata.
- Built-in image_gen used for the cleaned nightmare source, eight-frame sheet, and one scale-correction edit. Original generated PNG alpha is preserved byte for byte. No image-processing cleanup or frame blending was applied.
- Measured sprite rectangles and foot pivots are recorded in FrameRegistration.json. The sheet is not an exact equal-cell grid; use the supplied per-sprite rectangles instead of automatic grid slicing.
- Runtime loads sprites by numbered name. All frames use one pixel-to-world scale, calculated from the last frame's 113px height. With current scene settings the final height is 0.78 world units, also capped at 40% of detective sprite bounds height. The intact shell is approximately 2.76 units tall at that scale.
- Existing 1.4-second first-pose hold is retained. Seven changes use the existing 0.19-second frame interval; the final pose is reached 1.14 seconds after the first change. The same final sprite remains during dialogue. The old shader reveal is a fallback only when the eight clean sprites are unavailable.
- The generated poses depict a shadow collapsing downward and clearing around a crouched child. They are eight discrete key drawings, without optical-flow smoothing. Hand/toy details are authored per frame and are not guaranteed pixel-identical.

## Prompt specifications
1. Edit the original intact nightmare sprite into one transparent clean pixel-art sprite. Preserve low twin ponytails, red ties, eerie pale eyes, long triangular charcoal/purple shadow dress and grey unicorn with pink mane. Replace scratchy texture with contiguous deliberate pixel clusters. Remove gold rim, dust, debris, glow and fuzzy ground puddle. No text or extra objects.
2. Generate an eight-frame 4-column/2-row transparent sheet from that cleaned shell and SisterUnified. Progress from the intact tall shadow through downward collapse to the small seated child, head in crossed arms, knees together, blue-grey pajamas, brown twin ponytails, unicorn on her right. Keep the body centred and feet grounded. No scattered particles, gold dust, blur, crossfade or ghosts. Final child should be small relative to the shell.
3. Correct the oversized child in the first sheet: preserve upper-row designs and shrink the child consistently in all four lower cells, keeping the same floor and pose, with progressively less shadow around her. The corrected sheet is the delivered version; the first sheet is not integrated.

## Validation and limits
- Verified 8 non-overlapping, in-bounds sprite rectangles, unique sprite names and IDs, valid grounded pivots, genuine alpha transparency, and unchanged PNG bytes on copying into the project.
- Inspected generated source and sheet visually. Final three frame bounds heights are 112, 113, 113px; fixed runtime scaling prevents per-frame normalization from changing apparent body size.
- Reviewed the code path: final frame remains through dialogue; no independent SisterUnified replacement or shell shader is used for these frames.
- Unity importer, C# compilation and actual scene playback are not verified here because no Unity tool is connected and local process execution is unavailable. Do not label this a fully approved or perfect runtime animation.
- Manual check: open BadDream_Stage3, let the introduction finish and defeat the boss (or use PlayNow). Check foot grounding and head/toy continuity across all eight frames, final scale beside the detective, no swap at the dialogue boundary, no Console errors, then finish the hospital epilogue.
