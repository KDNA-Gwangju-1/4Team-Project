# Continuous garden path and grounded boss terrace

Replaced the scattered stepping stones with a two-column, curved limestone path, narrow sage border and continuous mortar bed. Authored separate routes on both sides of the bridge and a stage-2 entrance branch. Hidden the old path root; kept it available for rollback. The new path stops at the terrace perimeter instead of leaving tiles under the boss arena.

Rebuilt the terrace in Blender as a flat, solid elliptical plinth. The former rising outer ring over a rounded cushion left air gaps near the perimeter. The new base starts at local/world Y=0, with paving at Y=0.16 and a 0.165 outer trim. Its pivot is bottom-center, at (8.07, 0, 67.61). Ivory stone dominates, with a sage border and a small rose/sage center medallion.

The existing arena GameObject and MeshCollider component were retained. All five combat references still point to that enabled collider. Renderer and collision use the same mesh. Boss navigation was rebuilt while preserving its existing NavMeshData asset GUID.

Validation:
- Mesh minimum Y = 0, bottom pivot at ground; renderer/collider mesh identity confirmed.
- 16 surface probes all hit Y=0.16 and all 16 navigation paths completed. Navmesh approximation differs from the rendered surface by at most 0.056 m at those probes.
- In Play Mode, CharacterController.Move traversed 5.20 m from the approach across the arch/terrace edge; final position (19.67, 0.24, 56.65), grounded=true, stage=3. This was scripted movement, not physical keyboard input.
- Live boss AI and doll spawning checked after entry; see runtime-validation.json.
- Final approach, garden walk and terrace overview screenshots inspected. No full standalone build performed.

Source: tools/ChapterPolish/build_garden_paving.py and ArtSource/GardenPaving/GardenPaving.blend.
Installer: Assets/Editor/GardenPavingInstaller.cs (explicit authoring only).
