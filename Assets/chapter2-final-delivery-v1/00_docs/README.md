# Chapter 2 Final Delivery v1

챕터 2의 플레이어, 핵심 일반 몹, 보스, 배경, 기억 이미지와 전체 원본 보관본을 한 번에 전달하는 최종 패키지입니다.

## 실행용 핵심 에셋

- `01_player`: Idle 6F, Jump 6F, Walk v7 8F, 칼선 손전등 Walk v9 8F
- `02_enemies/core_scribble_enemy`: 확정 디자인 상태, Attack 8F, Dissolve 8F
- `03_boss`: 수정된 Boss Dissolve 20F와 보스 콘셉트
- `04_backgrounds`: 맵·충돌 지형이 없는 원경 배경 5종
- `05_supporting_assets`: 액자·기억, 콘셉트, 참고 목업
- `99_source_archive`: 분리 전후의 챕터 2 원본 자산 전체 보관본

## 투명·불투명 규칙

애니메이션 폴더마다 다음 두 변형을 함께 제공합니다.

- `transparent`: 실제 알파 채널이 있는 PNG 프레임, GIF, 스프라이트 시트
- `opaque_navy`: RGB 짙은 남색 배경으로 합성한 PNG 프레임, GIF, 스프라이트 시트

배경 5종은 원래부터 불투명 RGB 이미지입니다. `00_docs/asset-inventory.csv`의 `alpha_min`, `alpha_max`로 전체 파일의 알파 상태를 확인할 수 있습니다.

## 재생 권장값

| Animation | Frames | Duration | Loop |
|---|---:|---:|---|
| Player Idle | 6 | 160 ms | Yes |
| Player Jump | 6 | 120 ms | No |
| Player Walk v7 | 8 | 110 ms | Yes |
| Player Walk Flashlight v9 | 8 | 110 ms | Yes |
| Core Enemy Attack | 8 | 90 ms | No |
| Core Enemy Dissolve | 8 | 120 ms | No |
| Boss Dissolve | 20 | 200 ms | No |

모든 실행용 프레임은 512×512, bottom-center 피벗 기준입니다. 손전등 걷기는 칼선 버전만 포함했습니다.
