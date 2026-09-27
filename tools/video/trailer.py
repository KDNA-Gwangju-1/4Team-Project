"""Cinematic trailer: logo build -> three acts cut to music -> title and credits."""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from vidlib import *
import cards
import logo_anim
import build_submission as bs

TR = os.path.join(OUT, "trailer")
os.makedirs(TR, exist_ok=True)
MUSIC_CALM = r"D:/Ondukong/4Team-Project/Assets/Resources/Audio/Music/BGM_Ending.wav"
MUSIC_BOSS = r"D:/Ondukong/4Team-Project/Assets/Audio/BGM_Boss_Phase2.mp3"
BAR = 130                       # cinemascope bars
BEAT = .625                     # BGM_Boss_Phase2 ~96 bpm

# ("clip", source time, seconds) | ("text", kind, seconds, lines...)
ACT1 = [
    ("text", "line", 3.5, "꿈병에 걸린 사람은", "깊은 잠에 빠진다."),
    ("clip", 10.0, 3.0), ("clip", 22.0, 3.0),
    ("text", "line", 3.0, "꿈을 부숴야만", "다시 깨어날 수 있다."),
    ("clip", 44.0, 2.5), ("clip", 80.0, 3.0),
    ("text", "line", 3.0, "이번 의뢰", "잠든 쌍둥이 자매"),
]
B2 = BEAT * 4
ACT2 = [("text", "chapter1", B2, "CHAPTER 1", "언니의 꿈")] + [
    ("clip", t, B2) for t in (90.0, 101.0, 118.0, 158.0, 205.0, 222.0, 238.0)]
ACT3 = [("text", "chapter2", B2, "CHAPTER 2", "동생의 꿈")] + [
    ("clip", t, B2) for t in (263.0, 301.0, 333.0, 352.0, 392.0, 432.0)]
CLIMAX = [("clip", t, BEAT) for t in (480.0, 205.5, 495.0, 238.5, 500.0, 226.0, 510.0, 518.0)]
RESOLVE = [("clip", 544.0, 3.0), ("clip", 551.0, 3.0), ("clip", 555.0, 2.0), ("clip", 559.0, 3.0),
           ("text", "line", 3.2, "두 개의 꿈,", "하나의 이야기")]
END_SECS = 6.5


def clip(i, t, secs, flash_in=False):
    out = os.path.join(TR, f"c{i:02d}.mp4")
    if os.path.exists(out):
        return out
    pan = "(iw-ow)/2*(1-0.9*t/%.3f)" % secs if i % 2 else "(iw-ow)/2*(0.1+0.9*t/%.3f)" % secs
    vf = (f"fps=30,scale=2016:1134:flags=lanczos,crop=1920:{H - 2 * BAR}:x='{pan}':y=(ih-oh)/2,"
          f"eq=contrast=1.08:saturation=1.12,vignette=PI/5,pad=1920:1080:0:{BAR}:black")
    if flash_in:
        vf += ",fade=in:st=0:d=0.6:color=white"
    run(["-ss", str(t), "-t", str(secs), "-i", SRC, "-vf", vf, "-an", "-t", str(secs),
         "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p", out])
    return out


def bars(im):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, BAR), fill=(0, 0, 0))
    d.rectangle((0, H - BAR, W, H), fill=(0, 0, 0))
    return im


def text_card(i, kind, secs, lines):
    out = os.path.join(TR, f"c{i:02d}.mp4")
    if os.path.exists(out):
        return out
    wr = FrameWriter(out)
    n = int(round(secs * FPS))
    accent = {"chapter1": cards.RED, "chapter2": cards.BLU}.get(kind, (236, 232, 246))
    for f in range(n):
        t = f / FPS
        a = ease_out_cubic(t / .6) * (1 - ease_in_out((t - (secs - .45)) / .45))
        im = Image.new("RGB", (W, H), (0, 0, 0))
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        if kind == "line":
            for k, s in enumerate(lines):
                ak = ease_out_cubic((t - k * .35) / .7) * (1 - ease_in_out((t - (secs - .45)) / .45))
                y = H / 2 - 40 + k * 84 - (len(lines) - 1) * 10 + (1 - ak) * 16
                d.text((W / 2, y), s, font=font(62, k == len(lines) - 1), fill=(236, 232, 246, int(255 * ak)), anchor="mm")
        else:
            # chapter card: small spaced label, big title, accent rule drawing out
            d.text((W / 2, H / 2 - 70), "  ".join(lines[0]), font=font(28, True),
                   fill=accent + (int(255 * a),), anchor="mm")
            d.text((W / 2, H / 2 + 10 + (1 - a) * 14), lines[1], font=font(96, True),
                   fill=(245, 242, 252, int(255 * a)), anchor="mm")
            rw = 160 * ease_out_cubic((t - .2) / .6)
            d.rectangle((W / 2 - rw, H / 2 + 88, W / 2 + rw, H / 2 + 92), fill=accent + (int(255 * a),))
        glow = layer.filter(ImageFilter.GaussianBlur(14))
        im = im.convert("RGBA"); im.alpha_composite(glow); im.alpha_composite(layer)
        wr.write(bars(im.convert("RGB")))
    wr.close()
    return out


def end_card(i, secs):
    out = os.path.join(TR, f"c{i:02d}.mp4")
    if os.path.exists(out):
        return out
    lg = cards.logo_image(110)
    bg = logo_anim.background().convert("RGB")
    wr = FrameWriter(out)
    for f in range(int(secs * FPS)):
        t = f / FPS
        fade_out = 1 - ease_in_out((t - (secs - 1.0)) / 1.0)
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        a = ease_out_cubic(t / .9) * fade_out
        s = 1.04 - .04 * ease_out_cubic(t / 2.5)
        title_f = font(int(170 * s), True)
        d.text((W / 2, 440), "Re:Dream", font=title_f, fill=(246, 243, 252, int(255 * a)), anchor="mm")
        a2 = ease_out_cubic((t - .7) / .8) * fade_out
        d.text((W / 2, 570), "두 개의 꿈, 하나의 이야기", font=font(36), fill=(190, 184, 214, int(255 * a2)), anchor="mm")
        a3 = ease_out_cubic((t - 1.4) / .8) * fade_out
        l = lg.copy(); l.putalpha(l.split()[-1].point(lambda v: int(v * a3)))
        layer.alpha_composite(l, (W // 2 - l.width // 2, 660))
        d.text((W / 2, 820), "이원주 · 주서연 · 임현규 · 윤남기 · 김슬빈 · 김명주", font=font(28, True),
               fill=(206, 200, 228, int(255 * a3)), anchor="mm")
        d.text((W / 2, 866), "Made with Unity 6", font=font(22), fill=(140, 134, 164, int(255 * a3)), anchor="mm")
        im = bg.convert("RGBA")
        im.alpha_composite(layer.filter(ImageFilter.GaussianBlur(16)))
        im.alpha_composite(layer)
        if fade_out < 1:
            im = Image.blend(Image.new("RGBA", (W, H), (0, 0, 0, 255)), im, fade_out)
        wr.write(bars(im.convert("RGB")))
    wr.close()
    return out


def build():
    parts, marks = [], {}
    logo = os.path.join(TR, "c00.mp4")
    if not os.path.exists(logo):
        logo_anim.render(logo, caption=None, total=5.4)
    parts.append((logo, 5.4))
    i = 1
    for name, act in (("act1", ACT1), ("act2", ACT2), ("act3", ACT3), ("climax", CLIMAX), ("resolve", RESOLVE)):
        marks[name] = sum(p[1] for p in parts)
        for k, item in enumerate(act):
            if item[0] == "clip":
                path = clip(i, item[1], item[2], flash_in=(name == "resolve" and k == 0))
                parts.append((path, item[2]))
            else:
                parts.append((text_card(i, item[1], item[2], item[3:]), item[2]))
            i += 1
    marks["end"] = sum(p[1] for p in parts)
    parts.append((end_card(i, END_SECS), END_SECS))
    total = sum(p[1] for p in parts)

    lst = os.path.join(TR, "list.txt")
    with open(lst, "w", encoding="utf-8") as f:
        for p, _ in parts:
            f.write("file '" + p.replace("\\", "/") + "'\n")
    video = os.path.join(TR, "video.mp4")
    if not os.path.exists(video):
        run(["-f", "concat", "-safe", "0", "-i", lst, "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", "-r", "30", video])

    # music: logo pops -> calm bed for act 1 -> boss theme for acts 2-3 and the climax -> calm again
    logo_wav = os.path.join(TR, "logo.wav")
    bs.logo_audio(logo_wav, 5.4)
    a1, a2, a3 = marks["act1"], marks["act2"], marks["resolve"]
    ms = lambda s: int(s * 1000)
    fc = (f"[1:a]volume=1.0[l];"
          f"[2:a]atrim=0:{a2 - a1 + .8},asetpts=PTS-STARTPTS,volume=.8,afade=in:st=0:d=1.2,"
          f"afade=out:st={a2 - a1 - .9}:d=1.6,adelay={ms(a1)}|{ms(a1)}[c1];"
          f"[3:a]atrim=40:{40 + a3 - a2},asetpts=PTS-STARTPTS,volume=.95,afade=in:st=0:d=0.25,"
          f"afade=out:st={a3 - a2 - .5}:d=0.5,adelay={ms(a2)}|{ms(a2)}[b];"
          f"[4:a]atrim=18:{18 + total - a3},asetpts=PTS-STARTPTS,volume=.85,afade=in:st=0:d=1.8,"
          f"afade=out:st={total - a3 - 2.2}:d=2.2,adelay={ms(a3)}|{ms(a3)}[c2];"
          f"[l][c1][b][c2]amix=inputs=4:normalize=0:duration=longest,atrim=0:{total},aresample=48000[a]")
    final = os.path.join(OUT, "ReDream_시네마틱트레일러.mp4")
    run(["-i", video, "-i", logo_wav, "-i", MUSIC_CALM, "-i", MUSIC_BOSS, "-i", MUSIC_CALM,
         "-filter_complex", fc, "-map", "0:v", "-map", "[a]",
         "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-t", str(total), final])
    print("done", final, "%.1fs" % total, marks)


if __name__ == "__main__":
    build()
