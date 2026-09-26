# 챕터 2 탐정 퇴장 컷씬

최신 검토본은 **`detective-exit-stepped-v13.mp4`**입니다. 자세뿐 아니라 **위치·크기도 초당 6번만 갱신**합니다. 8프레임 동안 유지한 뒤 2프레임의 이동 방향 블러로 다음 위치에 넘어갑니다. 카메라·배경은 고정했습니다. `Comparison-v12-v13.mp4`는 왼쪽 연속 위치 이동 / 오른쪽 이번 수정본입니다. 제작·검사·한계는 `Validation-v13.md`를 참고하세요. Unity에는 아직 연결하지 않았습니다.

## 이전 v12

최신 검토본은 **`detective-exit-keyposes-v12.mp4`**입니다. 후면 보행을 핵심 4개 자세로 줄이고, 자세가 바뀌는 전후 4프레임에만 팔·다리 방향성 블러를 적용했습니다. 배경·머리는 블러 없이 유지합니다. `Comparison-v11-v12.mp4`는 왼쪽 이전 / 오른쪽 수정 보행 비교, `Walk-isolated-v12.mp4`는 수정 보행 확대 영상입니다. 재렌더는 `render_v12.py`, 검사와 한계는 `Validation-v12.md`를 참고하세요. 게임에는 아직 연결하지 않은 검토용 영상입니다.

## 이전 v11

현재 수정 검토본은 `detective-exit-rigid-v11.mp4`입니다. v10은 공간이 일렁이는 문제로 사용자가 반려했습니다. v11은 배경의 깊이별 재투영을 제거하고, 전체 합성 화면에 동일한 확대만 적용합니다. 걷는 다리의 TPS 변형도 제거하고 기존 보행 셀 원화를 거리 기준으로 교체합니다. 13.4초, 1080p/60fps, 무음입니다. Unity 씬에는 아직 연결하지 않았습니다.

`Walk-isolated-v11.mp4`는 고정 배경·고정 크기에서 처음 5초의 캐릭터 동작만 확인하는 영상입니다. 60fps 출력이지만 보행 원화는 후면 12장 / 측면 6장을 사용하므로 60개의 고유 자세가 생성되는 것은 아닙니다. 기존 원화의 자세 수와 중간 자세 부족은 남아 있으며, 자연스러운 보행의 최종 승인본으로 표기하지 않습니다. `render_v11.py`로 재렌더합니다. 수정·검수 범위는 `Validation-v11.md`에 기록합니다.

v10의 연구·수치 검사는 `Research-and-Validation-v10.md`에 보관합니다. 그 수치 검사 통과는 영상의 자연스러움을 보증하지 않았습니다.

`Comparison-v8-v9.mp4`는 이전 v8 / v9의 확대 비교 영상입니다. 원본 v8과 v9도 그대로 보관했습니다. v9의 상세 변경과 한계는 `Validation-v9.md`를 참고하세요.

`Comparison-v9-v10.mp4`는 왼쪽 v9 / 오른쪽 v10의 전체 화면 비교 영상입니다.

## v9 재렌더

Python 3.12, numpy 2.5.3, opencv-python-headless 5.0.0.93으로 검증했습니다. 프로젝트 루트에서:

```sh
python -m pip install --target output/cinematic-tools numpy==2.5.3 opencv-python-headless==5.0.0.93
python ArtSource/Chapter2Ending/DetectiveExit/render_v9.py
```

FFmpeg는 기존 `output/cinematic-tools/imageio_ffmpeg/binaries/*.exe`를 사용합니다. `--preview`를 붙이면 주요 장면만 렌더합니다. 중간 검수 이미지는 `output/ending-v9`에 저장합니다.

`versions/`에는 v5–v7 비교 영상을 보관했습니다. `assets/`에는 원화·배경과 관절 가이드를, `Validation-v8.md`에는 보정 내용과 검수 한계를 기록했습니다. 기존 셀 원화를 사용하므로 일부 동작 전환의 잔상과 발 접지 한계가 남아 있습니다.

## 이전 v8 재렌더

Node.js와 FFmpeg(minterpolate/libx264 포함)가 필요합니다. 이 폴더에서 실행합니다.

```sh
npm install
node render.cjs
node render-side.cjs
ffmpeg -y -framerate 60/7 -i keys/%04d.png -filter_complex_script main.filter -map "[out]" -t 9.4 -an -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p main.mp4
ffmpeg -y -framerate 20/3 -i sidekeys/%04d.png -filter_complex_script side.filter -map "[out]" -t 3.166667 -an -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p side.mp4
ffmpeg -y -i main.mp4 -i side.mp4 -filter_complex_script join.filter -map "[out]" -t 12.416667 -an -c:v libx264 -preset slow -crf 17 -pix_fmt yuv420p -movflags +faststart detective-exit-polish-v8.mp4
```

렌더 중간 프레임은 Git에서 제외합니다. 이미지와 영상은 저장소의 Git LFS 설정을 따릅니다. 원래 로컬 `output/` 작업 기록은 그대로 유지했습니다.
