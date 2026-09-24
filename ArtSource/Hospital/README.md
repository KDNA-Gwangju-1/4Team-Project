# Hospital architecture redesign

The editable Blender source is `HospitalArchitecture.blend`. Unity uses
`Assets/Art/Models/HospitalArchitecture/HospitalArchitecture.fbx` with materials
in `Assets/Materials/Hospital/Architecture`.

The original furniture, patients, door leaves, colliders, interactions and UI
remain in `HospitalRoom.unity`. Replaced blockout renderers are disabled rather
than deleted. The new `HospitalArchitecture` scene root owns the new visual
assemblies, bed-number signs and window fill light.

## Rebuild

1. Start the local Blender MCP addon.
2. From the project root, run:

   `uv run --python 3.11 --with mcp-for-blender==2.0.4 python tools/HospitalRedesign/blender_mcp_client.py tools/HospitalRedesign/build_architecture.py`

3. Allow Unity to import the FBX. Open HospitalRoom in Edit Mode and choose
   **Tools > Hospital > Apply Detailed Architecture**. Inspect and save the scene.

The baseline measurements are kept in `OriginalSceneInventory.json`.
The Blender script uses metre-scale geometry and projected metre-scale UVs.
The installer creates its own repeatable vinyl, oak and acoustic-ceiling textures.
It does not use the older full-scene rebuild command, which would replace manual
scene work.

## Validation

## Corridor extension, 2026-09-24

Both original ±9.075m end colliders remain enabled and invisible. Their visual
surfaces and the DarkEnd blockout renderers are hidden. Blender architecture
continues to ±49m, with matching wall protection, oak handrails interrupted at
doorways, vision panels, kick plates, door numbers, lights and slotted vents.
Sanitizer dispensers and rail brackets detail the playable corridor.
Linear atmospheric fog fades from 14m to 36m; no fullscreen blur is applied to
the nearby room or UI. Both end sightlines were rendered and reviewed.
`ExtensionValidation.json` records collider ray checks and missing-script checks.
The earlier `Validation.json` documents the initial architecture pass.

- Compared existing imported model positions/scales and visibility with the baseline.
- Checked the scene for missing scripts and Unity compile errors.
- Reviewed hospital interior and corridor renders, including window alignment,
  medical services, bed labels, ceiling equipment and lighting.
- Runtime validation and final previews are recorded alongside this source.
