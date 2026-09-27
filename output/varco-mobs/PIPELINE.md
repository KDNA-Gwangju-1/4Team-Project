# VARCO 3D 몹 파이프라인 (검증 완료 2026-09-18)

## 검증된 전체 경로
```
TextInput ─▶ GenerateImage ─▶ Generate3D ─▶ Rig ─┐
                                                 └▶ (taskId 복사)
                              Rigged3DInput ─┬─▶ Animate (공격)
                                             └─▶ Animate (사망)
```

## 함정 1 — run_workflow 의 스코프 파라미터는 `scope` (배열)
`nodeId` 라는 파라미터는 **없다**. 잘못 넘기면 조용히 무시되고 `scope=[]` 가 되어
**워크플로 안의 모든 액션 노드가 재실행된다.** (이미지 20 + 메시 200 크레딧이 그냥 날아감)

```
run_workflow(workflowId=..., scope=["<node-id>", ...])
```

## 함정 2 — Animate 를 Rig 에 직접 물리면 매번 메시부터 다시 만든다
`Rigged3DInput` 노드를 만들고 intrinsic `value` 에 **Rig 태스크의 taskId** 를 넣으면
입력 노드(액션 아님)라서 재실행되지 않는다. 여기에 Animate 여러 개를 붙인다.
모션을 추가·교체할 때 메시를 다시 뽑지 않아도 된다.

```
create_node(nodeType="Rigged3DInput", intrinsic={"value":[{"type":"3d","taskId":"<RIG_TASK_ID>"}]})
```

## 함정 3 — 포트는 max 1, 끊는 툴이 없다
`connect_edge` 가 `Port "mesh" is full (max 1)` 로 막히면
`delete_node` 로 그 노드를 지우고 다시 만드는 수밖에 없다.

## 함정 4 — intrinsic 은 반드시 배열 + 타입 래핑
`{"animation": "boxing_punch"}` ✗
`{"animation": [{"type":"string","value":"boxing_punch"}]}` ✓

## 함정 5 — Blender glTF 임포트가 MCP 컨텍스트에서 터진다
`bpy.context.object` / `bpy.context.window` 가 None 이라 임포터가 죽는다.
윈도우·스크린·에어리어·리전을 모두 채운 `temp_override` 안에서 호출해야 한다.

```python
wm   = bpy.data.window_managers[0]; win = wm.windows[0]; scr = win.screen
area = next(a for a in scr.areas if a.type == 'VIEW_3D')
region = next(r for r in area.regions if r.type == 'WINDOW')
with bpy.context.temp_override(window=win, screen=scr, area=area, region=region):
    bpy.ops.import_scene.gltf(filepath=path)
```

## 크레딧 (tier GAME_AI_CREATOR)
| 작업 | 크레딧 |
|---|---|
| GenerateImage (V2) | 20 |
| Generate3D (텍스처 포함) | 200 |
| Rig / Animate / Remesh / PBR | **0** |

→ 몹 1종당 220 크레딧. 모션은 몇 개를 붙이든 공짜.

## 노드 설정값
- **Generate3D**: polygonCount 20000 / topology `tri` / **tPose 1** / usePbrTexture 1 / textureSize 1024
  `tPose 1` 을 빼면 리깅이 엉킨다.
- **Rig**: riggingMode `humanoid` (다른 값: `humanoid-fingers`, `quadruped`)
- **Animate**: `inPlace 1` (제자리 재생 — 게임에서는 이동을 코드가 처리하므로 반드시 1)

## 애니메이션 카탈로그
총 599개 중 **무료는 17개뿐**이고 전부 걷기/달리기/대기 계열이다.
전투·사망 모션은 전부 `isPlus: true` 지만 **현재 등급에서 정상 실행된다** (0 크레딧).

선택한 키:
- 공격 `boxing_punch`
- 사망 `convulsion_death`

다른 후보: `two_hand_attack`, `punch_and_kick`, `gorilla_pound_attack`, `sword_slash`,
`magic_ground_attack_1`, `staff_spin_attack` / 사망계 `gorilla_fall_down`

## 결과물 (01_cotton_soldier 기준)
- 본 23개, **Mixamo 표준 이름** (Hips / Spine~Spine2 / Neck / Head /
  Left·RightShoulder·Arm·ForeArm·Hand / Left·RightUpLeg·Leg·Foot·ToeBase)
  → Unity Humanoid 아바타에 자동 매핑된다.
- 메시 20,000 폴리곤, 텍스처 3장(베이스컬러 / 노멀 / ORM), 머티리얼 1개
- 애니메이션 채널 21개, 프레임 1.6 ~ 89.6
- GLB 약 6MB

## "소멸 모션" 처리 방침
VARCO 카탈로그에는 디졸브 같은 소멸 연출이 없다. 표준 방식대로
**사망 모션(convulsion_death)으로 쓰러뜨리고, 소멸은 Unity 디졸브 셰이더**로 처리한다.
셰이더가 노이즈 텍스처의 클립 임계값을 올리며 깎아 내는 방식이라 모션과 독립적으로 동작한다.
