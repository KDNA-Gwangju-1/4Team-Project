# Ending v1 — key art, not finished animation

## Separate segments

- 3D: twins wake in their existing hospital beds, rise and look at each other.
- Cut on mutual eye contact. No blended 3D/pixel morph.
- Pixel (revised): continue matched seated pose, lean toward each other and embrace while remaining on their adjacent beds. No bed exit, standing or walking.
- Do not connect to gameplay until motion and art are reviewed.

## Implemented in this revision

- Inspected GirlSleeping.fbx and GirlSleeping.glb in Unity: each has 1 transform, 1 MeshFilter, 0 SkinnedMeshRenderers. Existing models cannot perform articulated motion as-is.
- Checked patient data: Seo Harin (elder twin) and Seo Hayun, both 12 years old.
- Generated pixel endpoint concept: pixel/embrace-keyframe.png. This is a review keyframe, not a sprite sequence or a finished 1920x1080 animation.
- Hospital reference: ../ending-reference/hospital-current.png.
- Existing scenes, opening video, and gameplay remain unchanged.

## Remaining production requirements

- Rigged matching 3D character meshes (or rigging existing meshes), with eyelid/head/spine controls and blanket handling.
- Camera-matched 3D endpoint and pixel starting frame.
- Pixel intermediate seated poses for turning and embracing; hips supported on mattresses, coherent shoulders/elbows, no frame blending/ghost trails.
- Sound, video export and runtime integration only after review.

## Image generation record

Tool: built-in image_gen, imagegen skill; no Higgsfield generation.
Reference: current hospital screenshot, layout/appearance reference only.
Prompt: 16:9 pixel-art hospital ending keyframe; preserve broad left window, pale wooden wall panels, two adjacent beds and a third empty bed behind a privacy curtain. Two same-height 12-year-old Korean twin girls with short dark-brown bob hair and modest pale blue-gray long-sleeved hospital pajamas embrace naturally on the clear floor in front of their now-empty beds. Connected anatomy, grounded feet, plausible arms around upper backs, restrained relief, crisp pixel clusters, coherent perspective, no text/UI/watermark. Endpoint artwork only, not storyboard grid.

## Revision 2 — seated embrace

Current review image: pixel/embrace-in-bed-v2.png. Previous standing version retained as superseded history.
Built-in image_gen edit using imagegen skill. Prompt: preserve character identity, pajamas, pixel style and hospital identity; change embrace to seated on two flush adjacent beds, one sister supported by each mattress, inner rails lowered, legs remain on beds with loose blankets on laps. Natural connected arms and shoulders. Medium-wide framing with distinct paired footboards and third empty bed to the right. No standing figures, text or UI. This remains a still keyframe, not completed animation.
