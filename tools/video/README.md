# 소개 영상 · 트레일러 제작 스크립트

`Recordings/`의 플레이 녹화(Unity Recorder 자동 녹화)로 제출용 영상을 만듭니다. 결과물은 `out/`에 생깁니다.

| 스크립트 | 만드는 것 |
|---|---|
| `logo_anim.py` | 4TEAM 로고 조립 애니메이션 (`assets/4team_logo.webp`) |
| `cards.py` | 팀 정보 · 게임 개요 · 조작 방법 · 팀원별 분담 등 정보 카드 |
| `build_submission.py` | 게임 소개 영상 (카드 + 자막 달린 전체 플레이 + 마무리) |
| `trailer.py` | 시네마틱 트레일러 (약 1분 30초) |

## 준비

```
python -m pip install --user pillow numpy imageio-ffmpeg
```

`vidlib.py`의 `SRC`를 녹화 파일 경로로 바꾼 뒤 실행합니다.

```
python build_submission.py
python trailer.py
```

- 자막 · 구간은 `build_submission.py`의 `PLAY`, 카드 문구는 `cards.py`(`MEMBERS`, `ROLES`, `PROBLEMS` 등)에서 고칩니다.
- 만든 구간은 `out/seg/`, `out/trailer/`에 캐시되므로, 문구를 고친 카드나 구간의 파일만 지우고 다시 실행하면 됩니다.
