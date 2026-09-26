# 챕터 2 탐정 퇴장 컷씬

현재 검토본은 `detective-exit-continuous-v9.mp4`입니다. 12.4초, 1080p/60fps, 무음입니다. 원본 배경 크기인 1672×941에서 매 프레임 합성 후 확대합니다. Unity 씬에는 아직 연결하지 않았습니다.

`Comparison-v8-v9.mp4`는 왼쪽 v8 / 오른쪽 v9의 확대 비교 영상입니다. 원본 v8도 그대로 보관했습니다. v9의 자세 보간은 캐릭터에만 적용하며 배경·문·이동 위치는 60Hz로 직접 계산합니다. 큰 방향 전환에는 직접 지정한 손목·무릎·발목 대응점을 사용합니다. 상세 변경과 한계는 `Validation-v9.md`를 참고하세요.

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
