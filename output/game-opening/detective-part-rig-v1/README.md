# Detective six-part rig — review only

Six independent transparent PNG assets: head, torso, left-arm, right-arm, left-leg, right-leg. Newly prepared through built-in imagegen using the existing detective as identity reference; prompt.txt stores the prompt. Atlas is preserved. Cropping only extracts the six separate generated components.

Arms have shoulder/elbow chains. Legs have hip/knee/ankle chains. Fixed 125-unit thigh and shin lengths are solved in sagittal space and projected into the existing back view. This calculation does NOT create a 3D asset. Gait cycle 1.2s, 60% stance / 40% swing design baseline, alternating limbs. Stance foot world-depth position cancels root advance; swing includes foot clearance. Opposing arm movement and small root bob are independently controlled. No crossfades, previous-frame accumulation or full-body warping.

Outputs: parts-and-rig.png, six-part-walk-review.mp4 (6s,1920x1080,60fps, silent), six part PNGs, parts.json, joint-audit.json, render.cjs.

Limitations: this is a first cutout-rig gait study, not approved final locomotion. Rear-view source textures do not yet include lifted-heel/sole variants, coat tails are not independently animated, shoulder/elbow seams and full foot rollover need visual refinement. No door action, hospital integration or Genjutsu generation in this version. Preserve the previously approved opening.

Biomechanics references consulted:
- Collins et al., Dynamic arm swinging in human walking: https://pmc.ncbi.nlm.nih.gov/articles/PMC2817299/
- Park, Synthesis of natural arm swing motion in human bipedal walking: https://ai.stanford.edu/~park73/papers/Jaeheung-JBM.pdf
- Clinical gait phase overview: https://pmc.ncbi.nlm.nih.gov/articles/PMC5318488/

Research informs coordination, not a claim that this stylized rig reproduces validated human gait data.
