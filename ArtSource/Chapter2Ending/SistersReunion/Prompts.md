# Image generation record

Tool: built-in `image_gen`, imagegen skill; no external video-generation service or CLI fallback.

## 01-eye-contact.png

References: `SisterAwakening/assets/embrace-style-reference.png` for hospital layout and both twins; `SisterAwakening/assets/eyes-open.png` for the newly awakened younger sister.

Final prompt specification: a single 16:9 medium two-shot from the foot of two adjacent touching hospital beds. Two 12-year-old Korean twin sisters sit supported on their own mattresses, legs under white lap blankets and inner rails lowered. Elder left, younger right, both short dark-brown bob hair and pale blue-gray long-sleeved pajamas. Gentle relieved eye contact, faces separated, shoulders not touching, inner hands resting nearby about 10 cm apart. Coherent anatomy, pale wooden wall panels, paired pillows, left warm afternoon window light, cool lavender shadows, detailed crisp pixel clusters. No embrace yet, no text, grids or extra people.

## 03-hand-contact.png

Edit target: eye-contact keyframe.

Final prompt specification: preserve exact camera, faces, room, bed, blankets and light. Only change inner forearms/hands so the younger sister on the right gently places her left hand over the elder sister's stationary right hand at the seam between blankets. Simple reassuring contact, not interlaced fingers; exactly two visible anatomically coherent hands, sleeves connected to wrists, elbows supported. No full embrace yet. Single same-size 16:9 frame.

## 02-hand-reaching.png

Edit target: hand-contact keyframe.

Final prompt specification: edit only the younger sister's left forearm/hand to the moment just before contact, about 60 pixels to the right and slightly raised. Relaxed hand hovers above the blanket, fingertips down-left toward the stationary elder hand; no contact yet. Keep older hand fixed, coherent wrist/sleeve/elbow, no extra fingers. Exact same framing, expressions, bedding and light.

## 04-embrace.png

References: eye-contact keyframe for exact camera/environment/identities, original seated-embrace reference for gesture.

Final prompt specification: both sisters remain seated securely on their respective touching mattresses with lap blankets. Lean toward each other, temples/cheeks gently touch, eyes close, small relieved smiles. Arms wrap around upper backs/shoulders with coherent shoulder-elbow-wrist anatomy, two arms each, no extra hands. Hips remain supported, no standing. Preserve paired beds, pillow context, hospital architecture, camera, left afternoon light and detailed crisp pixel art. Single 16:9 frame, no captions, grid or extra people.
