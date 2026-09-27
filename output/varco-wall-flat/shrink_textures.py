"""
원본 이미지를 에셋용 텍스처 크기로 줄인다.

VARCO 가 뽑아 준 1024 원본은 가장자리 여백(마젠타 배경)이 화면의 절반을 넘는다.
UV 가 이미지 전체를 덮으므로, 그대로 쓰면 텍스처 용량의 절반 이상을 배경이 먹는다.

  1. 마젠타 배경을 걷어 내고 실루엣의 바운딩 박스를 구한다
  2. 여백을 조금만 남기고 잘라낸다
  3. 목표 크기로 줄인다

벽에 붙는 납작한 장식이고 색이 거의 평면이라 512 면 충분하다.
"""

import os

import bpy
import numpy as np


def _mask_bbox(px, w, h, margin=0.04):
    rgb = px[..., :3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    is_bg = (r > g + 0.20) & (b > g + 0.15) & (g < 0.50)
    fg = ~is_bg

    rows = np.nonzero(fg.any(axis=1))[0]
    cols = np.nonzero(fg.any(axis=0))[0]
    if len(rows) == 0 or len(cols) == 0:
        return 0, 0, w, h

    r0, r1 = int(rows[0]), int(rows[-1]) + 1
    c0, c1 = int(cols[0]), int(cols[-1]) + 1

    pad = int(round(max(r1 - r0, c1 - c0) * margin))
    r0 = max(0, r0 - pad); r1 = min(h, r1 + pad)
    c0 = max(0, c0 - pad); c1 = min(w, c1 + pad)
    return r0, c0, r1, c1


def shrink(src_dir, dst_dir, target=512):
    os.makedirs(dst_dir, exist_ok=True)
    rows = []

    for fname in sorted(os.listdir(src_dir)):
        if not fname.lower().endswith(".png"):
            continue
        src = os.path.join(src_dir, fname)

        img = bpy.data.images.load(src, check_existing=False)
        w, h = img.size
        buf = np.empty(w * h * 4, dtype=np.float32)
        img.pixels.foreach_get(buf)
        px = buf.reshape(h, w, 4)[::-1]          # 위가 0행

        r0, c0, r1, c1 = _mask_bbox(px, w, h)
        crop = px[r0:r1, c0:c1]
        ch, cw = crop.shape[:2]

        out = bpy.data.images.new(fname, cw, ch, alpha=True)
        out.pixels.foreach_set(crop[::-1].ravel())

        # 긴 변을 target 에 맞춘다
        if max(cw, ch) > target:
            s = target / max(cw, ch)
            out.scale(max(1, int(round(cw * s))), max(1, int(round(ch * s))))

        dst = os.path.join(dst_dir, fname)
        out.filepath_raw = dst
        out.file_format = 'PNG'
        out.save()

        rows.append((fname, f"{w}x{h}", f"{cw}x{ch}", f"{out.size[0]}x{out.size[1]}",
                     os.path.getsize(src) / 1024, os.path.getsize(dst) / 1024))

        bpy.data.images.remove(img)
        bpy.data.images.remove(out)

    return rows
