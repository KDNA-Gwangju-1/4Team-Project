# Hospital double-door entrance — English prompt

Status: NOT SUBMITTED. This is approved narrative direction, not a ready generation request. Required driving footage and matching 3D subject/scene reference are missing. No free generation was spent in this preparation.

## English prompt

Transfer the motion from the supplied driving video to the detective and hospital scene in the supplied 3D reference. Follow the driving video's camera position, timing, walking path, foot contacts and door interactions. Use restrained real-time 3D game rendering consistent with the reference, not photorealistic live action.

The low camera remains fixed outside the hospital, facing its central entrance. Begin with the detective standing still on the centerline, back toward the camera. Preserve his tan-brown trench coat, dark trousers, brown leather shoes and brown fedora as his upper body becomes visible with distance. Maintain one consistent adult character and outfit throughout.

The detective walks straight away from the camera toward the entrance, becoming smaller according to perspective. His feet alternate with grounded contacts and no sliding. The coat hem follows the movement with restrained secondary motion.

Both glass doors are initially closed. The detective pauses at the entrance, places his left and right hands on the corresponding door handles, and pushes both doors inward while stepping through. Each door rotates around its fixed side hinges. Keep each hand attached to its handle throughout the pushing phase. Release the handles only once his body and coat have fully cleared the doorway.

The detective continues into the hospital without turning around or manually closing either door. Both doors return smoothly under their door closers, with a slight timing offset. They decelerate near the closed position and finish fully shut against their frames without bouncing or slamming.

Preserve the hospital facade, window positions, door dimensions, architectural perspective and daylight. No lateral entrance, camera movement, zoom, orbit, cuts, duplicated characters, ghost trails, transparent limbs, foot sliding, changing anatomy, hand-handle separation, body-door intersections, shifting hinges, teleporting doors, glitches, captions or logos.

## Korean comparison

- 고정된 낮은 카메라, 중앙에 등을 보이고 정지한 탐정으로 시작.
- 실제 기준 영상의 보행·접지·동작 순서·타이밍을 따름.
- 갈색 코트·페도라, 짙은 바지, 갈색 신발과 동일한 체형 유지.
- 중앙선을 따라 입구로 이동하며 원근에 맞게 작아짐.
- 처음에는 양문 모두 닫힘. 양손으로 각각 손잡이를 잡고 안쪽으로 밀며 입장.
- 몸과 코트가 통과한 뒤 손을 놓고 안쪽으로 계속 이동.
- 두 문은 도어클로저로 시간차를 두고 돌아오며, 마지막에 감속해 완전히 닫힘.
- 인물 복제·잔상·관통·발 미끄러짐·건물 변형·카메라 움직임 금지.
- 픽셀에서 3D로 연결하는 전환과 효과음은 후반 편집 범위.

## Readiness gate

- Candidate model: hf_mult_motion_control. Recheck constraints and free grant immediately before execution.
- Proposed free-run resolution: 720p. One output only.
- Driving video: NOT READY. Current local 6.4-second video ends at the standing pose; it has no double-door opening, entrance, or closing motion.
- Matching 3D detective/hospital reference: NOT READY. User-supplied ward screenshot is style guidance only, not a matching subject/scene reference.
- Desired source framing: 16:9; proposed driving length 10–12 seconds, subject to actual motion timing.
- Current workspace Assets contains no .fbx/.FBX/.blend/.anim/.controller/.prefab files in the read-only search. Request the actual game asset project location or suitable driving footage.
- Do not send a still or unsuitable video as a substitute for the required door-action driving video.
