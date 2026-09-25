# 챕터 2 탐정 퇴장 컷씬

현재 검토본은 `detective-exit-polish-v8.mp4`입니다. 12.4초, 1080p/60fps, 무음이며 720p 합성에서 확대했습니다. Unity 씬에는 아직 연결하지 않았습니다.

`versions/`에는 v5–v7 비교 영상을 보관했습니다. `assets/`에는 원화·배경과 관절 가이드를, `Validation-v8.md`에는 보정 내용과 검수 한계를 기록했습니다. 기존 셀 원화를 사용하므로 일부 동작 전환의 잔상과 발 접지 한계가 남아 있습니다.

## 재렌더

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
