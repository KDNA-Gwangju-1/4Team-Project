"""Assembles the Re:Dream submission video (game introduction) from the playthrough recording."""
import os
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw

from vidlib import *
import cards
import logo_anim

SEG = os.path.join(OUT, "seg")
os.makedirs(SEG, exist_ok=True)
MUSIC = r"D:/Ondukong/4Team-Project/Assets/Resources/Audio/Music/"
ENC = ["-c:v", "libx264", "-preset", "veryfast", "-crf", "19", "-pix_fmt", "yuv420p", "-r", "30",
       "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2"]

VX, VY, VW, VH = 128, 24, 1664, 936          # gameplay window inside the frame

CH = {  # badge text, colour
    "start": ("시작 화면", cards.YEL), "open": ("오프닝", cards.YEL), "hosp": ("현실 · 병원", cards.GRN),
    "c1": ("챕터 1 · 밝은 꿈", cards.RED), "c1b": ("챕터 1 · 보스전", cards.RED),
    "c2": ("챕터 2 · 어두운 꿈", cards.BLU), "c2b": ("챕터 2 · 보스전", cards.BLU), "end": ("엔딩", cards.YEL),
}

# (start, end, speed, chapter, caption, feature tag)
PLAY = [
    (0, 8, 1, "start", "메인 메뉴 — 게임 시작 · 설정 · 종료. '게임 시작'을 누르면 오프닝이 재생됩니다.", None),
    (8, 22, 1, "open", "원인을 알 수 없는 '꿈병'이 퍼집니다. 꿈병에 걸린 사람은 깊은 잠에서 깨어나지 못합니다.", None),
    (22, 36, 1, "open", "꿈탐정에게 잠든 쌍둥이 자매의 꿈을 부숴 달라는 의뢰가 들어옵니다.", None),
    (36, 52, 1, "open", "탐정은 자매가 입원한 병원으로 향합니다.", "오프닝 시네마틱"),
    (52, 64, 1, "hosp", "1인칭(WASD · 마우스)으로 병동 복도를 걸으며 301호 병실을 찾습니다.", None),
    (64, 68, 1, "hosp", "로딩 화면 — 로딩할 때마다 10가지 팁 중 하나가 무작위로 나옵니다.", "주요 기능"),
    (68, 88, 1, "hosp", "병실에서 대화를 나눈 뒤, E로 잠든 아이의 손을 잡으면 꿈속으로 들어갑니다.", "상호작용"),
    (88, 100, 1, "c1", "챕터 1 목표 — 정원에 숨은 '기억의 단서' 4개를 찾으세요. 오른쪽 위에 진행도가 표시됩니다.", None),
    (100, 112, 1, "c1", "단서에 다가가 E로 조사하면, 언니의 기억이 음성 나레이션과 함께 재생됩니다.", "주요 기능"),
    (112, 134, 1, "c1", "단서를 모두 모으고 다리 앞에 서면 괴물이 나타나는 컷씬으로 다음 단계가 이어집니다.", "컷씬"),
    (134, 146, 1, "c1", "화살표가 가리키는 곳에서 정화총을 얻습니다.", None),
    (146, 172, 1, "c1", "좌클릭으로 인형 몬스터를 정화하세요. 제한 시간(위쪽 타이머) 안에 목표를 채워야 합니다.", "주요 기능"),
    (172, 188, 1, "c1", "몬스터를 모두 정화하면 보스 스테이지로 가는 균열이 열립니다.", None),
    (188, 218, 1, "c1b", "유니콘 보스전 — 몬스터를 정화해 보스의 약점을 드러내고, 드러난 약점을 공격합니다.", "보스 AI"),
    (218, 236, 1, "c1b", "보스 패턴: 뿔 레이저 · 별똥별 · 내려찍기. 바닥의 예고 표시를 보고 피하세요.", "3페이즈 기믹"),
    (236, 248, 1, "c1b", "정화를 6번 성공하면 보스가 정화되고, 다음 꿈으로 가는 균열이 열립니다.", None),
    (248, 260, 1, "c2", "균열 속으로 — 챕터 2 '동생의 꿈'은 2D 횡스크롤 액션입니다.", None),
    (260, 272, 1, "c2", "하늘이 갈라지며 꿈탐정이 떨어지고, 챕터 제목이 나타납니다.", "연출"),
    (272, 288, 1, "c2", "손전등 옆의 쪽지를 E로 펼치면 조작 도움말을 읽을 수 있습니다.", None),
    (288, 300, 1, "c2", "A/D 이동 · Space 점프(벽차기) · Shift 대시 · 우클릭 손전등 · 좌클릭 광탄", "조작"),
    (300, 330, 1, "c2", "목표 — 손전등으로 어둠을 비춰 숨은 발판을 찾고, 출구의 문까지 가세요.", "주요 기능"),
    (330, 348, 1, "c2", "손전등은 과열 게이지, 대시는 충전 칸이 있어 아껴 쓰며 전진해야 합니다.", None),
    (348, 384, 1, "c2b", "보스 등장 — 언니의 모습을 한 악몽이 나타납니다.", "컷씬"),
    (384, 420, 1, "c2b", "1페이즈 — 촉수를 빛으로 비춘 상태에서 광탄을 맞혀야 피해가 들어갑니다.", "주요 기능"),
    (420, 444, 1, "c2b", "2페이즈로 넘어가며 보스가 본모습을 드러냅니다.", None),
    (444, 516, 1.5, "c2b", "2페이즈 — 탄막을 대시로 피하고, 보스를 빛으로 비춘 뒤 공격하세요.", "보스 AI"),
    (516, 536, 1, "c2b", "보스를 쓰러뜨리면 악몽이 걷히고, 웅크린 동생이 모습을 드러냅니다.", None),
    (536, 560, 1, "end", "두 개의 악몽을 모두 부수자, 쌍둥이 자매가 깨어납니다.", "엔딩"),
    (560, 577.4, 1, "end", "에필로그 — 탐정은 병원을 떠나고 게임이 끝납니다.", None),
]


# The boundaries above were read off a 4-second contact sheet whose labels run ~2 s ahead of
# the footage (checked on the loading screen and the ending); shift every inner cut to match.
PLAY = [(s + (2 if s > 0 else 0), e + (2 if e < 577 else 0), *rest) for s, e, *rest in PLAY]


# ---------------------------------------------------------------- images
def gameplay_bg():
    path = os.path.join(SEG, "play_bg.png")
    y = np.linspace(0, 1, H)[:, None, None]
    top, bot = np.array([16, 14, 30]), np.array([8, 7, 16])
    arr = (top * (1 - y) + bot * y).repeat(W, 1).astype(np.uint8)
    im = Image.fromarray(arr, "RGB")
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((VX - 4, VY - 4, VX + VW + 3, VY + VH + 3), 8, outline=(60, 56, 90), width=2)
    im.save(path)
    return path


def caption_png(i, chapter, text, tag, speed):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    label, col = CH[chapter]
    y0, y1 = 976, 1060
    fb = font(24, True)
    bw = d.textlength(label, font=fb) + 44
    d.rounded_rectangle((VX, y0 + 14, VX + bw, y1 - 14), 28, fill=col + (255,))
    d.text((VX + bw / 2, (y0 + y1) / 2), label, font=fb, fill=(20, 18, 34), anchor="mm")
    x = VX + bw + 24
    if tag:
        ft = font(22, True)
        tw = d.textlength(tag, font=ft) + 28
        d.rounded_rectangle((x, y0 + 18, x + tw, y1 - 18), 10, outline=col + (255,), width=2)
        d.text((x + tw / 2, (y0 + y1) / 2), tag, font=ft, fill=col + (255,), anchor="mm")
        x += tw + 20
    f = font(32, True)
    right = VX + VW - (150 if speed != 1 else 0)
    while d.textlength(text, font=f) > right - x and f.size > 24:
        f = font(f.size - 1, True)
    d.text((x, (y0 + y1) / 2), text, font=f, fill=(240, 238, 248, 255), anchor="lm")
    if speed != 1:
        s = f"{speed:g}배속"
        d.rounded_rectangle((VX + VW - 130, y0 + 18, VX + VW, y1 - 18), 10, fill=(255, 255, 255, 40))
        d.text((VX + VW - 65, (y0 + y1) / 2), "▶▶ " + s, font=font(22, True), fill=(240, 238, 248, 255), anchor="mm")
    path = os.path.join(SEG, f"cap_{i:02d}.png")
    im.save(path)
    return path


# ---------------------------------------------------------------- segments
def play_segment(i, s, e, speed, chapter, text, tag, bg):
    out = os.path.join(SEG, f"play_{i:02d}.mp4")
    if os.path.exists(out):
        return out
    cap = caption_png(i, chapter, text, tag, speed)
    dur = (e - s) / speed
    fo = max(0.0, dur - .25)
    vf = (f"[0:v]setpts=(PTS-STARTPTS)/{speed},fps=30,scale={VW}:{VH}:flags=lanczos[v];"
          f"[1:v][v]overlay={VX}:{VY}:shortest=1[b];"
          f"[2:v]format=rgba,fade=in:st=0:d=0.25:alpha=1,fade=out:st={fo:.3f}:d=0.25:alpha=1[c];"
          f"[b][c]overlay=0:0:shortest=1,format=yuv420p[out];"
          f"[0:a]{'atempo=%g,' % speed if speed != 1 else ''}aresample=48000,asetpts=PTS-STARTPTS[a]")
    run(["-ss", str(s), "-t", str(e - s), "-i", SRC, "-loop", "1", "-i", bg, "-loop", "1", "-i", cap,
         "-filter_complex", vf, "-map", "[out]", "-map", "[a]", "-t", f"{dur:.3f}"] + ENC + [out])
    return out


def card_section(name, items, music, music_start=0.0, volume=.55):
    """items: [(png, seconds)] -> one clip with fades between cards and a music bed."""
    out = os.path.join(SEG, name + ".mp4")
    if os.path.exists(out):
        return out
    args, parts = [], []
    total = 0.0
    for k, (png, secs) in enumerate(items):
        args += ["-loop", "1", "-t", str(secs), "-i", png]
        parts.append(f"[{k}:v]fps=30,format=yuv420p,fade=in:st=0:d=0.45,fade=out:st={secs - .45:.2f}:d=0.45[v{k}]")
        total += secs
    n = len(items)
    args += ["-stream_loop", "-1", "-ss", str(music_start), "-i", music]
    fc = ";".join(parts) + ";" + "".join(f"[v{k}]" for k in range(n)) + f"concat=n={n}:v=1:a=0[vout];" \
        f"[{n}:a]atrim=0:{total},asetpts=PTS-STARTPTS,volume={volume},afade=in:st=0:d=1," \
        f"afade=out:st={total - 1.5}:d=1.5,aresample=48000[aout]"
    run(args + ["-filter_complex", fc, "-map", "[vout]", "-map", "[aout]", "-t", str(total)] + ENC + [out])
    return out


def logo_audio(path, total, blob_t0=.35, blob_dt=.22, letter_t0=1.55, letter_dt=.11):
    sr = 48000
    n = int(total * sr)
    buf = np.zeros(n)
    t = np.arange(int(.25 * sr)) / sr

    def add(at, sig):
        i = int(at * sr)
        buf[i:i + len(sig)] += sig[:max(0, n - i)]

    for k in range(4):   # soft "pop" per blob, rising in pitch
        f0 = 330 * (1.26 ** k)
        sweep = np.sin(2 * np.pi * (f0 * 1.8 * t - f0 * .8 * t * t / .25 * .5))
        add(blob_t0 + k * blob_dt + .05, .35 * sweep * np.exp(-t * 22))
    for k in range(5):   # small ticks for the letters
        f0 = 880 * (1.12 ** k)
        add(letter_t0 + k * letter_dt + .06, .16 * np.sin(2 * np.pi * f0 * t) * np.exp(-t * 40))
    tc = np.arange(int(2.6 * sr)) / sr   # warm chord once the logo is complete
    chord = sum(np.sin(2 * np.pi * f * tc) for f in (392, 493.9, 587.3, 784)) / 4
    add(2.4, .22 * chord * np.exp(-tc * 1.4) * np.minimum(1, tc * 30))
    fade = np.ones(n); fl = int(.6 * sr); fade[-fl:] = np.linspace(1, 0, fl)
    buf = np.clip(buf * fade, -1, 1)
    st = (np.stack([buf, buf], 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr); w.writeframes(st.tobytes())


def logo_segment(name, caption, total=5.4):
    out = os.path.join(SEG, name + ".mp4")
    if os.path.exists(out):
        return out
    silent = os.path.join(SEG, name + "_v.mp4")
    logo_anim.render(silent, caption=caption, total=total)
    wav = os.path.join(SEG, name + ".wav")
    logo_audio(wav, total)
    run(["-i", silent, "-i", wav, "-map", "0:v", "-map", "1:a", "-shortest"] + ENC + [out])
    return out


def main():
    cd = os.path.join(OUT, "cards")
    c = lambda n: os.path.join(cd, n + ".png")
    bg = gameplay_bg()
    parts = [logo_segment("00_logo", "presents")]
    parts.append(card_section("01_intro_cards", [(c("title"), 6), (c("team"), 11), (c("overview"), 13),
                                                  (c("controls"), 14), (c("flow"), 8)],
                              MUSIC + "BGM_Garden.wav", 0, .5))
    for i, row in enumerate(PLAY):
        parts.append(play_segment(i, *row, bg))
        print("play", i, flush=True)
    parts.append(card_section("03_dev_cards", [(c("features"), 13), (c("tools"), 11), (c("roles0"), 16),
                                                (c("roles1"), 14), (c("problems"), 17), (c("wrapup"), 15)],
                              MUSIC + "BGM_Ending.wav", 0, .5))
    parts.append(logo_segment("04_outro", "감사합니다", total=6.0))

    lst = os.path.join(SEG, "list.txt")
    with open(lst, "w", encoding="utf-8") as f:
        for p in parts:
            f.write("file '" + p.replace("\\", "/") + "'\n")
    final = os.path.join(OUT, "ReDream_게임소개영상.mp4")
    run(["-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", "-movflags", "+faststart", final])
    print("done", final)


if __name__ == "__main__":
    main()
