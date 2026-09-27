# Chapter 2 rabbit surprise cue

The Stage 2 tutorial's plain yellow `!?` TextMesh is replaced by a point-filtered
41 × 35 pixel speech badge. Its ink, lavender rim and ivory question mark match
the existing Stage 3 boss reaction badge; the exclamation mark uses coral.

Runtime source: `Assets/Scripts/2d_scripts/Stage2IntroCutscene.cs`,
`GetSurpriseBadgeSprite` and `ShowMarkOver`.

- The tail is the bottom anchor, placed 0.18 world units above the rabbit's
  SpriteRenderer bounds. World positioning avoids inherited negative scale.
- The cue follows the rabbit, uses its sorting layer with a higher order, and
  ignores the flashlight sprite mask.
- Entry is 0.16 seconds with a 0.12-second settle. The configured surprise hold
  remains unchanged; a 0.12-second fade finishes it.
- Target destruction, component disabling and normal completion remove the cue.
- No scene/prefab references or serialized fields were changed.

`RabbitSurpriseBadge.png` and `RabbitSurprisePreview.png` are offline design
previews of the pixel layout, not Unity gameplay captures. The runtime generates
the cached sprite directly, following the existing boss cue implementation.

Validation: compiled the full Assembly-CSharp source set with the project's
existing Unity 6000.3.21f1 response file and bundled Roslyn compiler, redirecting
outputs to `output/rabbit-reaction`. Exit code 0, no compilation errors. Existing
obsolete API/unused-field warnings remain. Pixel preview inspected and diff
whitespace check passed. Unity MCP reported no connected Editor instance;
Play Mode positioning, pacing and retry behavior still require in-editor review.
