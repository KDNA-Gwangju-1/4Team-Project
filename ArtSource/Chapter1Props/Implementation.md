# Chapter 1 craft props — 2026-09-25

Replaced 11 visible props in SD_BrightDream_Blockout_Rect: seven crates, one bench, two signs and one bridge. Authored in the local Blender MCP session; editable source is Chapter1Props.blend, generator tools/ChapterPolish/build_props.py, exports Assets/BrightDream/Art/Chapter1Props.

The primitive-cube audit found no currently visible raw cubes. Existing cube walls, ceiling tiles and gates are hidden gameplay boundaries; they were preserved. Visible props were FBX models, and were redesigned with additional geometry rather than described as raw cubes.

New details include separate planks, rounded edges, corner bindings, studs, handles, hinges and clasps; bench slats, armrests and a plaque; framed two-sided direction signs; arched bridge beams, individual boards and rope-bound rails.

Scene positions, rotations, scales, colliders and gameplay scripts remain unchanged. New meshes are fitted to the original import coordinate system. After visual fitting, bridge vertices were lowered 0.14 world metres to align with the existing walking surface; sign depth was reduced to 45% for a thinner board. These refinements are stored in the fitted mesh assets, separately from the original FBX exports.

Validation: eleven replacements loaded in Play Mode; shader has no compile errors; Console has zero errors. Nine bridge deck probes hit the new visible surface, with maximum visual/collision height difference 0.042 m. Before/after bridge images and individual crate, bench and sign images were inspected. No full playthrough or standalone build was performed.

Play Mode was started externally during this work (before the planned play request, which returned Already in play mode); it was left running. The scene replacement was saved before this transition. Subsequent refinements only modified persistent mesh assets and were saved via AssetDatabase. An attempted redundant scene save during Play Mode was rejected without changing the scene. Temporary measurement colliders were removed.

No new commit or push in this turn.
