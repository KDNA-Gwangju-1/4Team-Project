# Chapter 2 Final Integration v1

이 폴더를 Chapter 2 통합 기준본으로 사용합니다. 기존 원본 폴더는 비교 및 롤백을 위해 수정하지 않았습니다.

## 확정 교체 규칙

- `Walk`: `01_player/walk`의 v7 8프레임 워크사이클을 사용합니다.
- `Walk-Flashlight`: `01_player/walk_flashlight_outlined`의 v9 8프레임을 사용합니다.
- 손전등: 칼선이 있는 v9 에셋만 허용합니다. 이전 무칼선 손전등 에셋은 이 통합 팩에 포함하지 않았습니다.
- 배경: `02_backgrounds`의 순수 원경 배경 5종을 사용합니다. 기존 소스 시트에서 분리한 3종과 이번에 새로 제작한 2종이며, 맵·발판·충돌 지형은 포함하지 않습니다.
- 보스 소멸: `03_boss/dissolve_20f`의 수정된 20프레임 버전으로 교체합니다. 특히 17~20프레임은 동생이 과도하게 지워지지 않도록 보정된 최종본입니다.

## 런타임 권장값

| 상태 | 프레임 | 반복 | 기준 속도 |
|---|---:|---|---:|
| Walk | 8 | Loop | 100~120 ms/frame |
| Walk-Flashlight | 8 | Loop | 100~120 ms/frame |
| Boss Dissolve | 20 | Once | 200 ms/frame |

게임의 실제 이동 속도와 프레임 이벤트에 맞춰 walk 계열 재생 속도만 미세 조정하고, 프레임 순서는 변경하지 않습니다.

## 폴더 구성

- `01_player/walk`: 새 일반 걷기 GIF, 시트, 개별 프레임
- `01_player/walk_flashlight_outlined`: 새 칼선 손전등 걷기 GIF, 시트, 개별 프레임
- `02_backgrounds`: 기존 원경 3종 + 신규 원경 2종. 모두 맵과 분리해 뒤에 배치하는 배경 플레이트입니다.
- `03_boss/dissolve_20f`: 새 보스 소멸 GIF, 시트, 개별 투명 프레임, 후반 검수 이미지
- `04_supporting`: 기존 확정 idle, jump, enemy, memory frame, concept 자산

`04_supporting/scene_mockups_reference`의 장면 이미지는 구성 참고용이며 배경 5종에 포함되지 않습니다.

세부 출처와 적용 상태는 `integration-manifest.csv`를 확인합니다.
