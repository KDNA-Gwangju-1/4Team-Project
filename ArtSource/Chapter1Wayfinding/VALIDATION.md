# Chapter 1 scene fixes — 2026-09-25

- Bench turned toward path; 2 sign arrows aligned with next route targets (dot product 1.0).
- Tree carving enlarged 1.9x; subdivided and projected onto trunk (5112/5112 surface hits) to prevent letters clipping into bark. Final visual: tree-carving-final.png.
- 12 perimeter hedge sections widened and fitted with grounded moss bases; gameplay colliders preserved.
- Path stones rebuilt in Blender with bottom-center pivots. 477 active stones, 454 overlapping originals disabled; minimum pivot spacing 0.50004 m.
- Purifier placed on a crate; removed automatic trigger pickup. Existing 4-clue gate retained; E interaction uses PlayerInteraction raycast.
- Boss floor references on spawner, containment, AI and attack indicators remapped from disabled old floor to enabled Blender arena collider. Invalid bounds rejected; spawn attempts cannot fall back to a shared point.

## Validation
Unity Editor play-mode checks (not a full build or manual end-to-end playthrough):
- Pickup before clues: false; after 4 clues, raycast target true, prompt active with E 정화총 획득하기. First TryInteract true, repeat false, weapon owned true. Physical keyboard input was not synthesized.
- Boss spawned 10 dolls at distinct positions, moving toward player. With runtime-only invulnerability and boss AI disabled, invoking the shot handler on 3 actual dark dolls changed weakpoint exposure false -> true; player health 100. See weakpoint-test.json and boss-weakpoint-validated.png. Test overrides were discarded by exiting Play Mode.
- Production Console: 0 errors. Scene saved, Play Mode stopped.
- Screenshots weapon-e-prompt*.png and boss-monsters-spread.png are earlier unsuccessful capture attempts (stale Game view / player death), not validation evidence. Use purifier-display.png for the final stand appearance and the explicit runtime checks above for interaction.
