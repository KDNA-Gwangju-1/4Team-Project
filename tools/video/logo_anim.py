"""Team logo build animation: four blobs pop in, letters drop and bounce, soft glow."""
import math
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from vidlib import *

LOGO = os.path.join(ROOT, "assets", "4team_logo.webp")
COLORS = {"Y": (252, 181, 43), "R": (252, 88, 71), "B": (44, 146, 248), "G": (50, 190, 128)}
LETTER_Y = 775


def extract_pieces():
    im = np.asarray(Image.open(LOGO).convert("RGB")).astype(np.float32)
    h, w, _ = im.shape
    names = list(COLORS)
    cols = np.array([COLORS[n] for n in names], np.float32)
    # nearest colour by chroma (remove brightness so anti-aliased edges keep their hue)
    chroma = im - im.mean(2, keepdims=True)
    cchroma = cols - cols.mean(1, keepdims=True)
    d = ((chroma[:, :, None, :] - cchroma[None, None]) ** 2).sum(3)
    label = d.argmin(2)
    csum = cols.sum(1)[label]
    alpha = np.clip((765 - im.sum(2)) / (765 - csum), 0, 1)
    alpha[:110, :130] = 0          # the "07" sheet number in the corner
    alpha[alpha < .04] = 0

    pieces = []
    # blobs: one piece per colour above the lettering
    for i, n in enumerate(names):
        m = (label == i) & (alpha > 0)
        m[LETTER_Y:] = False
        pieces.append(("blob", n, m))
    # letters: split the lettering band at empty columns
    band = alpha.copy(); band[:LETTER_Y] = 0
    cols_used = (band > .15).any(0)
    x, runs = 0, []
    while x < w:
        if cols_used[x]:
            s = x
            while x < w and cols_used[x]:
                x += 1
            runs.append((s, x))
        x += 1
    for s, e in runs:
        m = np.zeros_like(alpha, bool); m[LETTER_Y:, max(0, s - 2):min(w, e + 2)] = True
        m &= alpha > 0
        n = names[np.bincount(label[m], minlength=4).argmax()]
        pieces.append(("letter", n, m))

    out = []
    for kind, n, m in pieces:
        ys, xs = np.where(m)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        rgba = np.zeros((y1 - y0, x1 - x0, 4), np.uint8)
        rgba[..., :3] = COLORS[n]
        rgba[..., 3] = (alpha[y0:y1, x0:x1] * m[y0:y1, x0:x1] * 255).astype(np.uint8)
        out.append(dict(kind=kind, color=n, img=Image.fromarray(rgba, "RGBA"), box=(x0, y0, x1, y1)))
    return out


def background():
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    r = np.sqrt(((x - W / 2) / W) ** 2 + ((y - H / 2) / H) ** 2)
    t = np.clip(r * 1.6, 0, 1)[..., None]
    inner, outer = np.array([30, 26, 54], np.float32), np.array([8, 7, 16], np.float32)
    return Image.fromarray((inner * (1 - t) + outer * t).astype(np.uint8), "RGB").convert("RGBA")


def render(path, caption=None, total=5.4, fade_out=True, light=False):
    pieces = extract_pieces()
    bg = background() if not light else Image.new("RGBA", (W, H), (250, 249, 246, 255))
    # logo area in source px
    x0 = min(p["box"][0] for p in pieces); x1 = max(p["box"][2] for p in pieces)
    y0 = min(p["box"][1] for p in pieces); y1 = max(p["box"][3] for p in pieces)
    S = 600 / (y1 - y0)
    ox = W / 2 - (x0 + x1) / 2 * S
    oy = H / 2 - (y0 + y1) / 2 * S - (30 if caption else 0)

    blobs = [p for p in pieces if p["kind"] == "blob"]
    letters = sorted([p for p in pieces if p["kind"] == "letter"], key=lambda p: p["box"][0])
    order = {"Y": 0, "R": 1, "G": 2, "B": 3}
    blobs.sort(key=lambda p: order[p["color"]])
    for i, p in enumerate(blobs):
        p["t0"] = .35 + i * .22
    for i, p in enumerate(letters):
        p["t0"] = 1.55 + i * .11

    rnd = random.Random(4)
    for p in blobs:
        p["sparks"] = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(140, 260), rnd.uniform(5, 11)) for _ in range(9)]

    # pre-scaled piece images
    for p in pieces:
        bw, bh = p["img"].size
        p["base"] = p["img"].resize((max(1, int(bw * S)), max(1, int(bh * S))), Image.LANCZOS)
        cx = (p["box"][0] + p["box"][2]) / 2 * S + ox
        cy = (p["box"][1] + p["box"][3]) / 2 * S + oy
        p["c"] = (cx, cy)

    full = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for p in pieces:
        b = p["base"]; full.alpha_composite(b, (int(p["c"][0] - b.width / 2), int(p["c"][1] - b.height / 2)))
    glow = full.filter(ImageFilter.GaussianBlur(28))

    wr = FrameWriter(path)
    n = int(total * FPS)
    for f in range(n):
        t = f / FPS
        frame = bg.copy()
        # glow after assembly
        g = ease_in_out((t - 2.35) / .8)
        if g > 0:
            ga = glow.copy(); ga.putalpha(ga.split()[-1].point(lambda v: int(v * .55 * g)))
            frame.alpha_composite(ga)
        # settle squash of the whole cluster once the last blob lands
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        sd = ImageDraw.Draw(layer)
        for p in blobs:
            k = (t - p["t0"]) / .55
            if k <= 0:
                continue
            s = ease_out_elastic(k) if k < 1 else 1
            rot = (1 - ease_out_cubic(k)) * (-18 if p["color"] in "YG" else 18)
            b = p["base"]
            img = b.resize((max(1, int(b.width * s)), max(1, int(b.height * s))), Image.BICUBIC)
            if abs(rot) > .3:
                img = img.rotate(rot, Image.BICUBIC, expand=True)
            layer.alpha_composite(img, (int(p["c"][0] - img.width / 2), int(p["c"][1] - img.height / 2)))
            # sparks
            if 0 < k < 1.1:
                for ang, dist, rad in p["sparks"]:
                    q = ease_out_cubic(k / 1.1)
                    px = p["c"][0] + math.cos(ang) * dist * q * 1.2
                    py = p["c"][1] + math.sin(ang) * dist * q * 1.2
                    rr = rad * (1 - q)
                    if rr > .5:
                        sd.ellipse((px - rr, py - rr, px + rr, py + rr), fill=COLORS[p["color"]] + (230,))
        for p in letters:
            k = (t - p["t0"]) / .55
            if k <= 0:
                continue
            # pops up from just below its seat with a small overshoot
            rise = (1 - ease_out_back(k, 2.4)) * 70
            s = .6 + .4 * ease_out_back(k, 2.0)
            a = ease_out_cubic(k * 2.5)
            b = p["base"]
            img = b.resize((max(1, int(b.width * s)), max(1, int(b.height * s))), Image.BICUBIC) if s != 1 else b.copy()
            if a < 1:
                img.putalpha(img.split()[-1].point(lambda v: int(v * a)))
            layer.alpha_composite(img, (int(p["c"][0] - img.width / 2), int(p["c"][1] - img.height / 2 + rise)))
        frame = shadowed(frame, layer, offset=(0, 12), blur=16, alpha=90 if not light else 50)

        if caption:
            c = ease_out_cubic((t - 2.7) / .7)
            if c > 0:
                d = ImageDraw.Draw(frame)
                col = (235, 232, 245) if not light else (60, 60, 70)
                d.text((W / 2, oy + y1 * S + 70 + (1 - c) * 20), caption, font=font(34, True),
                       fill=col + (int(255 * c),), anchor="mm")
        if fade_out:
            fo = clamp01((t - (total - .6)) / .6)
            if fo > 0:
                frame = Image.blend(frame, Image.new("RGBA", (W, H), (0, 0, 0, 255)), fo)
        fi = clamp01(t / .3)
        if fi < 1:
            frame = Image.blend(Image.new("RGBA", (W, H), (0, 0, 0, 255)), frame, fi)
        wr.write(frame)
    wr.close()


if __name__ == "__main__":
    render(os.path.join(OUT, "logo_intro.mp4"), caption="presents")
    print("ok")
