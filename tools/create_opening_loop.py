from __future__ import annotations

import math
import sys
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter, ImageOps


def vertical_feather(width: int, height: int, solid_until: int, fade_until: int) -> Image.Image:
    mask = Image.new("L", (width, height), 0)
    px = mask.load()
    for y in range(height):
        if y <= solid_until:
            alpha = 210
        elif y >= fade_until:
            alpha = 0
        else:
            alpha = round(210 * (fade_until - y) / (fade_until - solid_until))
        for x in range(width):
            px[x, y] = alpha
    return mask.filter(ImageFilter.GaussianBlur(2.0))


def animate(source: Path, output: Path) -> None:
    base = Image.open(source).convert("RGB")
    width, height = base.size
    if width != 1672 or height < 600:
        raise ValueError(f"Unexpected source dimensions: {width}x{height}")

    # Interior areas of the five window panes. Frames and all desk objects remain static.
    panes = [(35, 0, 330, 405), (379, 0, 694, 405), (761, 0, 1028, 405),
             (1073, 0, 1378, 405), (1418, 0, 1672, 405)]
    pane_sources: list[tuple[tuple[int, int, int, int], Image.Image, Image.Image]] = []
    for box in panes:
        pane = base.crop(box)
        enlarged = pane.resize((pane.width + 28, pane.height), Image.Resampling.LANCZOS)
        mask = vertical_feather(pane.width, pane.height, 245, 395)
        pane_sources.append((box, enlarged, mask))

    # Inner glass only; the television casing is intentionally untouched.
    tv_box = (1360, 477, 1530, 563)
    tv_source = base.crop(tv_box)

    frames: list[Image.Image] = []
    frame_count = 40
    for i in range(frame_count):
        phase = 2.0 * math.pi * i / frame_count
        frame = base.copy()

        # Slow, seamless cloud drift. Different pane offsets preserve the sense of layered distance.
        for index, (box, enlarged, mask) in enumerate(pane_sources):
            drift = math.sin(phase + index * 0.16)
            offset = 14 + round(8 * drift)
            moving = enlarged.crop((offset, 0, offset + mask.width, mask.height))
            frame.paste(moving, (box[0], box[1]), mask)

        # Very subdued broadcast movement: tiny vertical instability, faint pulse, and a rolling scan band.
        tv = tv_source.copy()
        shift_y = round(math.sin(phase * 2.0) * 1.0)
        if shift_y:
            shifted = Image.new("RGB", tv.size, (18, 25, 38))
            shifted.paste(tv, (0, shift_y))
            tv = shifted
        tv = ImageEnhance.Brightness(tv).enhance(0.88 + 0.025 * math.sin(phase * 3.0))
        tv = ImageEnhance.Contrast(tv).enhance(0.90)

        scan_y = int(((i / frame_count) * (tv.height + 28)) - 14)
        overlay = Image.new("L", tv.size, 0)
        opx = overlay.load()
        for y in range(max(0, scan_y - 8), min(tv.height, scan_y + 9)):
            strength = max(0, 22 - abs(y - scan_y) * 3)
            for x in range(tv.width):
                opx[x, y] = strength
        cool = Image.new("RGB", tv.size, (70, 92, 116))
        tv = Image.composite(cool, tv, overlay.filter(ImageFilter.GaussianBlur(3)))

        # Alternating scanlines are visible only on close inspection.
        line_layer = Image.new("L", tv.size, 0)
        lpx = line_layer.load()
        parity = i % 2
        for y in range(parity, tv.height, 4):
            for x in range(tv.width):
                lpx[x, y] = 12
        tv = Image.composite(ImageOps.colorize(tv.convert("L"), (5, 10, 18), (100, 122, 145)), tv, line_layer)
        frame.paste(tv, (tv_box[0], tv_box[1]))
        frames.append(frame)

    output.parent.mkdir(parents=True, exist_ok=True)
    if output.suffix.lower() == ".gif":
        target_width = 1280
        target_height = round(height * target_width / width)
        gif_frames = [
            frame.resize((target_width, target_height), Image.Resampling.LANCZOS)
            .quantize(colors=192, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
            for frame in frames
        ]
        gif_frames[0].save(
            output,
            save_all=True,
            append_images=gif_frames[1:],
            duration=120,
            loop=0,
            optimize=False,
            disposal=2,
            format="GIF",
        )
    else:
        frames[0].save(
            output,
            save_all=True,
            append_images=frames[1:],
            duration=120,
            loop=0,
            format="WEBP",
            quality=88,
            method=6,
        )


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: create_opening_loop.py SOURCE.png OUTPUT.webp")
    animate(Path(sys.argv[1]), Path(sys.argv[2]))
