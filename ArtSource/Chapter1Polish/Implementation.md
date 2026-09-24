# Chapter 1 polish — 2026-09-25

Status: Ready with limitations (Unity Editor runtime validated; standalone build and full chapter playthrough not run).

## Scope

- Audited active mesh renderers, texture dimensions, object bounds, UV ranges and representative Unity camera views in `SD_BrightDream_Blockout_Rect`.
- Replaced 32 low-density perimeter meshes: 13 treelines, 12 hills, 4 half trees, 3 hedge rows. Their scene transforms, bounds and existing colliders remain intact.
- Rebuilt the enlarged boss arena in local Blender as individually bevelled paving, a padded moss border and embroidery. Replaced its visual and collider together; retained the original disabled mesh for reference.
- Replaced the 64-pixel surface of four small Deck objects with metre-scale woven ivory material.
- The continuous meadow already uses approximately one texture repeat per metre; it was not stretching one image across the entire map. Existing craft treehouse/greenhouse and ordinary higher-density props were retained.
- Rebuilt BossHand in local Blender with a voxel-fused palm and finger anatomy, curled claws, cloth seams and crimson cuff accents matching the Chapter 2 nightmare doll palette. Separate Arm and Hand assemblies retain telescoping animation.

## Grasp correction

The old sequence aimed at a cached Spine position before the defeat animation finished, then captured and preserved the resulting wrist/body offset. The new prefab contains a palm-centred `GripAnchor`. Reach tracks the current Spine, hold/drag contact is corrected in LateUpdate after Animator evaluation, and the hand scales with the shrinking unicorn. The animation driver now uses actual `HasGrabbedBoss` state instead of a guessed delay when an exit sequence exists.

## Validation

- Extended timing runtime test: 807 rendered contact frames, maximum point error 0.0000004915125 m; unicorn hidden and runtime hand removed on completion.
- Default timing runtime test: 170 rendered contact frames, same maximum error, normal completion and cleanup. Default hold 0.45 s and drag 1.3 s were retained in the saved scene.
- Rendered and inspected the arena before/after, final hand, first contact and mid-drag frames.
- Rebuilt boss navigation against the new collision surface. The pre-stage `Gate_UnicornPlaza` box covers the whole arena and must not participate in navigation baking; its NavMeshModifier now ignores baking while its gameplay collider remains unchanged.
- After rebuilding: boss start location on navigation mesh, all eight destinations on an 8 m circle found, all eight paths complete. Original `Assets/NavMesh/BossArenaNavMesh.asset` is preserved; this scene uses a new asset under Chapter1Polish.
- 32 arena collision rays hit the new surface.
- Unity C# compilation and final Console: no errors. New surface shader: no compiler errors. Scene: zero missing scripts; hand reference valid.
- Play Mode stopped. Diagnostic camera/callbacks were runtime-only; no test timing or camera changes saved.
- Unity's scene serializer adds its customary trailing spaces after empty YAML `value:` entries. `git diff --check` reports these serialization lines; script whitespace is clean.

## Source and regeneration

`Chapter1Polish.blend` is the editable Blender source. `tools/ChapterPolish/build_chapter1.py` regenerates only its own collection in a dedicated Blender scene. `blender_local.py` talks to the running local Blender MCP add-on on loopback; no external model-generation service is used.

FBX exports and Unity materials/meshes are under `Assets/BrightDream/Art/Chapter1Polish`. `Tools > Bright Dream > Apply Blender Chapter 1 Polish` reinstalls fitted scenery and the hand prefab. After changing the arena geometry, update its conformed collision mesh and bake BossNavMeshSurface again; keep the temporary chapter gate excluded. The four ivory Deck materials and final navigation asset are scene-authored refinements.

No new commit or push was performed for this change. Earlier checkpoint 39a70db was already pushed before this task; the previous Chapter 2 redundant-lantern fix remains an independent uncommitted change.
