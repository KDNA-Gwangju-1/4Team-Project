"""Swaps chapter 1 of the finished submission video for a new playthrough recording.

    python replace_chapter1.py <new recording.mp4> [<submission video.mp4>]

The new chapter 1 is cut into captioned segments exactly like build_submission.py does
(same frame, badge and caption style), then placed between the untouched head and tail
of the submission video. Result: out/ReDream_게임소개영상_챕터1교체.mp4
"""
import os
import sys

import build_submission as bs
from vidlib import OUT, run

# Where chapter 1 sits in the finished video: from the end of the hospital segment (the green
# badge fades out) to the end of the first chapter 2 segment ("균열 속으로"), which still showed
# the old chapter 1 rift and the loading screen.
CUT_IN, CUT_OUT = 147.4, 319.4

# (start, end, speed, chapter, caption, feature tag) - times in the NEW recording
PLAY = [
    (23, 31, 1, "c1", "밝은 꿈에 도착하면 조작 안내가 나오고, 꿈탐정이 정원의 수상한 자국을 살핍니다.", "연출"),
    (31, 41, 1, "c1", "챕터 1 목표 — 정원에 숨은 '기억의 단서' 4개를 찾으세요. 오른쪽 위에 진행도가 표시됩니다.", None),
    (41, 69, 1, "c1", "단서에 다가가 E로 조사하면, 언니의 기억이 음성 나레이션과 함께 재생됩니다.", "주요 기능"),
    (69, 99, 1, "c1", "단서를 모두 모으고 다리 앞에 서면 괴물이 나타나는 컷씬으로 다음 단계가 이어집니다.", "컷씬"),
    (99, 105, 1, "c1", "화살표가 가리키는 곳에서 정화총을 얻습니다.", None),
    (105, 129, 1, "c1", "좌클릭으로 인형 몬스터를 정화하세요. 제한 시간(위쪽 타이머) 안에 목표를 채워야 합니다.", "주요 기능"),
    (129, 136, 1, "c1", "몬스터를 모두 정화하면 보스 스테이지로 가는 길이 열립니다.", None),
    (136, 166, 1, "c1b", "유니콘 보스전 — 몬스터를 정화해 보스의 약점을 드러내고, 드러난 약점을 공격합니다.", "보스 AI"),
    (166, 187, 1, "c1b", "보스 패턴: 뿔 레이저 · 별똥별 · 내려찍기. 바닥의 예고 표시를 보고 피하세요.", "3페이즈 기믹"),
    (187, 196, 1, "c1b", "정화를 6번 성공하면 보스가 정화되고, 다음 꿈으로 가는 균열이 열립니다.", None),
    (196, 202.9, 1, "c2", "균열 속으로 — 챕터 2 '동생의 꿈'은 2D 횡스크롤 액션입니다.", None),
]


def cut(src, out, start=None, end=None):
    """Re-encodes a span of the submission video with the segments' settings so everything concatenates."""
    if os.path.exists(out):
        return out
    args = []
    if start is not None:
        args += ["-ss", str(start)]
    args += ["-i", src]
    if end is not None:
        args += ["-t", str(end - (start or 0))]
    run(args + ["-map", "0:v", "-map", "0:a"] + bs.ENC + [out])
    return out


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    bs.SRC = os.path.abspath(sys.argv[1])
    video = os.path.abspath(sys.argv[2]) if len(sys.argv) > 2 else os.path.join(
        bs.ROOT, "..", "..", "docs", "presentation", "video", "ReDream_게임소개영상.mp4")
    bs.SEG = os.path.join(OUT, "ch1_seg")
    os.makedirs(bs.SEG, exist_ok=True)

    parts = [cut(video, os.path.join(bs.SEG, "head.mp4"), end=CUT_IN)]
    bg = bs.gameplay_bg()
    for i, row in enumerate(PLAY):
        parts.append(bs.play_segment(i, *row, bg))
        print("play", i, flush=True)
    parts.append(cut(video, os.path.join(bs.SEG, "tail.mp4"), start=CUT_OUT))

    lst = os.path.join(bs.SEG, "list.txt")
    with open(lst, "w", encoding="utf-8") as f:
        for p in parts:
            f.write("file '" + p.replace("\\", "/") + "'\n")
    final = os.path.join(OUT, "ReDream_게임소개영상_챕터1교체.mp4")
    run(["-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", "-movflags", "+faststart", final])
    print("done", final)


if __name__ == "__main__":
    main()
