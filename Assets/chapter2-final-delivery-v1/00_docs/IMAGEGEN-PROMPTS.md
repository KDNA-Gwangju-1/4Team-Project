# ImageGen Production Prompts

## Tool

- Built-in `imagegen`
- Workflow: `imagegen` skill

## Core enemy attack — final prompt

> Use case: stylized-concept. Asset type: production 2D game enemy attack animation sprite sheet, revised for strict cell safety. Preserve the exact same scribble monster identity from the supplied references: dense black crayon oval body, irregular purple looping outline strokes, exactly two rough ivory circular eyes, two thin dark-red diagonal scratches, small cream/purple debris specks, handmade pixel-crayon edges. Create exactly 8 sequential frames in a strict 4 columns by 2 rows layout, read left-to-right on the top row, then left-to-right on the bottom row. Motion: neutral; compress backward; coil; begin rightward lunge; maximum horizontal stretch RIGHT; overshoot; recoil; neutral settle. CRITICAL LAYOUT: each frame must be an isolated sprite centered inside its own imaginary square cell; monster and every trail/debris particle must fit inside only the central 60 percent of that cell's width and height, with at least 20 percent completely empty transparent padding on all four sides. No pixel from any frame may cross or touch a neighboring cell. Make the whole monster smaller rather than clipping the lunge. Keep bottom pivot consistent. Genuine transparent background with alpha. No navy backdrop, no floor, no shadow, no borders, no visible grid, no labels, no text, no other characters, no watermark. Crisp game-ready sheet.

References: `final-enemy-11.png`, `final-enemy-06.png`, and the first attack draft for motion continuity.

## Core enemy dissolve — final prompt

> Use case: stylized-concept. Asset type: production 2D game enemy defeat/dissolve animation sprite sheet. Use the supplied images as strict identity references for the SAME single scribble monster: dense black crayon oval body, irregular purple looping outline strokes, two rough ivory circular eyes, two thin dark-red diagonal scratch marks, small cream/purple debris specks, handmade pixel-crayon edges. Create exactly 8 sequential frames in a clean 4 columns by 2 rows sprite sheet, read left-to-right across the top row then left-to-right across the bottom row. Defeat motion: frame 1 intact neutral monster; frame 2 outer purple loops loosen and small edge flecks lift; frame 3 black scribble body begins breaking into short crayon strokes; frame 4 eyes fade and separate into ivory dust; frame 5 body is half dispersed into black/purple/red fragments; frame 6 only a loose cloud of fragments remains; frame 7 a few sparse dust motes and short red/purple strokes remain; frame 8 completely empty transparent cell. The dissolve must be gradual, monotonic, and clearly readable with no regrowth. Preserve scale and lock the bottom pivot/baseline until the form vanishes. Every cell equal size with safe padding. Genuine transparent background with alpha; empty space must be transparent. No navy backdrop, no floor, no shadow plate, no border, no grid lines, no labels, no numbers, no text, no UI, no other characters, no gore, no watermark. Do not merge adjacent frames. Crisp game-ready sprite sheet.

References: `final-enemy-11.png`, `final-enemy-08.png`, and `final-enemy-09.png`.

Background-generation prompts are stored separately in `BACKGROUND-GENERATION-NOTES.md`.
