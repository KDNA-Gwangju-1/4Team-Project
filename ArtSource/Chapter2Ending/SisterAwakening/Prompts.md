# Image generation record

Tool: built-in `image_gen`, imagegen skill. No CLI/API-key fallback.
All project images were copied from the tool's generated-image directory into `assets/`.

## Closed-eye keyframe

References: existing seated-embrace pixel artwork (character and style) and hospital interior capture (bed/environment layout).

Final prompt specification: one 16:9 cinematic pixel-art close-up of the younger 12-year-old Korean twin from the right side of the embrace reference, lying supine on a white pillow in the right occupied hospital bed. Short dark chestnut bob with bangs, modest pale blue-gray hospital pajamas, blanket covering lower chest. Eyes gently closed, peaceful neutral mouth. Slightly elevated bedside camera, nearly frontal with a slight three-quarter angle, complete head and both eyelids visible, face large enough for animation. White pillow, pale wood hospital headboard and bed-rail edges. Warm afternoon light from screen left. Preserve reference identity and detailed crisp pixel clusters. No other people, text, watermark, photorealism or 3D render.

Output: `assets/eyes-closed.png`.

## Open-eye endpoint

Edit target: closed-eye keyframe.

Final prompt specification: change only the closed eyelids to naturally open relaxed eyes, warm dark-brown irises and small daylight highlights. Preserve the exact composition, crop, pillow, bed, sunlight, hair, head silhouette, eyebrows, nose, mouth, cheeks, clothes and blanket. Calm recovering consciousness, not startled, no smile or tears. Keep eye-corner positions and head tilt. Upper lid opens upward toward the brow; lower lid remains near the original lash curve. Preserve pixel-art style and proportions.

Output: `assets/eyes-open.png`.

## Half-open in-between

Edit target: open-eye endpoint.

Final prompt specification: change only eyelids to half-open, drowsy eyes. Upper lids occlude the upper half of the existing brown irises; do not shrink or squash irises. Keep iris/pupil position and lower eyelids. Preserve exact crop, face, head position, nose, mouth, hair, pillow, bedding, light and pixel texture. The girl is softly regaining consciousness, not angry; do not close eyes completely. Single image, not a sheet.

Output: `assets/eyes-half.png`.

The video renderer uses only eye patches from the edited keyframes so incidental changes elsewhere in generated edits cannot cause whole-face or background drift.
