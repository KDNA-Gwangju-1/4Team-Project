"""Shared helpers for the Re:Dream submission video and trailer."""
import math
import os
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

try:
    import imageio_ffmpeg
    FF = imageio_ffmpeg.get_ffmpeg_exe()
except ImportError:  # no pip package: use the ffmpeg named by FFMPEG, else the one on PATH
    FF = os.environ.get("FFMPEG", "ffmpeg")
W, H, FPS = 1920, 1080, 30
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "out")
os.makedirs(OUT, exist_ok=True)
SRC = r"D:/Ondukong/4Team-Project/Recordings/20260928_033842_MainMenu.mp4"
FONT_DIR = os.path.join(ROOT, "..", "..", "Assets", "Resources", "UI", "Fonts") + os.sep
FONT_R = FONT_DIR + "Pretendard-Regular.ttf"
FONT_B = FONT_DIR + "Pretendard-SemiBold.ttf"

_font_cache = {}


def font(size, bold=False):
    key = (size, bold)
    if key not in _font_cache:
        _font_cache[key] = ImageFont.truetype(FONT_B if bold else FONT_R, size)
    return _font_cache[key]


# ---------------------------------------------------------------- easing
def clamp01(t):
    return max(0.0, min(1.0, t))


def ease_out_cubic(t):
    t = clamp01(t)
    return 1 - (1 - t) ** 3


def ease_in_out(t):
    t = clamp01(t)
    return t * t * (3 - 2 * t)


def ease_out_back(t, s=1.9):
    t = clamp01(t)
    t -= 1
    return t * t * ((s + 1) * t + s) + 1


def ease_out_elastic(t):
    t = clamp01(t)
    if t in (0, 1):
        return t
    return 2 ** (-10 * t) * math.sin((t * 10 - .75) * (2 * math.pi) / 3) + 1


def bounce_out(t):
    t = clamp01(t)
    n1, d1 = 7.5625, 2.75
    if t < 1 / d1:
        return n1 * t * t
    if t < 2 / d1:
        t -= 1.5 / d1
        return n1 * t * t + .75
    if t < 2.5 / d1:
        t -= 2.25 / d1
        return n1 * t * t + .9375
    t -= 2.625 / d1
    return n1 * t * t + .984375


# ---------------------------------------------------------------- ffmpeg
class FrameWriter:
    """Pipes RGB frames into an H.264 file (video only)."""

    def __init__(self, path, fps=FPS):
        self.path = path
        self.p = subprocess.Popen(
            [FF, "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24",
             "-s", f"{W}x{H}", "-r", str(fps), "-i", "-", "-c:v", "libx264", "-preset", "medium",
             "-crf", "18", "-pix_fmt", "yuv420p", path],
            stdin=subprocess.PIPE)

    def write(self, img):
        if img.mode != "RGB":
            img = img.convert("RGB")
        self.p.stdin.write(img.tobytes())

    def close(self):
        self.p.stdin.close()
        self.p.wait()
        if self.p.returncode != 0:
            raise RuntimeError("ffmpeg failed for " + self.path)


def run(args):
    r = subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y"] + args)
    if r.returncode != 0:
        raise RuntimeError("ffmpeg failed: " + " ".join(args))


# ---------------------------------------------------------------- drawing
def text_size(draw, s, f):
    b = draw.textbbox((0, 0), s, font=f)
    return b[2] - b[0], b[3] - b[1]


def draw_text(draw, xy, s, f, fill, anchor="la", spacing=10):
    draw.multiline_text(xy, s, font=f, fill=fill, anchor=anchor, spacing=spacing)


def rounded(draw, box, r, fill=None, outline=None, width=1):
    draw.rounded_rectangle(box, r, fill=fill, outline=outline, width=width)


def shadowed(base, layer, offset=(0, 10), blur=18, alpha=110):
    """Composite RGBA layer onto base with a soft drop shadow."""
    a = layer.split()[-1]
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    sh.putalpha(a.point(lambda v: v * alpha // 255))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(sh, offset)
    base.alpha_composite(layer)
    return base
