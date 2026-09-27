"""
보스를 3배로 키워도 괜찮은지 수치로 검증한다.

"커도 안 깨진다" 는 감이 아니라 두 가지 밀도로 판단할 수 있다.

  텍셀 밀도  = 텍스처 픽셀 수 / 모델의 실제 크기 (px/m)
      이게 낮으면 가까이 갔을 때 텍스처가 뭉개진다.
  폴리곤 밀도 = 실루엣 한 바퀴를 몇 개의 면으로 그리는가
      이게 낮으면 외곽선이 각져 보인다.

기준은 기존 몹이다. 몹과 같은 밀도가 나오면 나란히 놓아도 이질감이 없다.
"""

import json
import os
import struct


def glb_json(path):
    with open(path, "rb") as f:
        struct.unpack("<III", f.read(12))
        clen, _ = struct.unpack("<II", f.read(8))
        return json.loads(f.read(clen).decode("utf-8"))


def png_size(blob):
    # PNG 시그니처 뒤 IHDR 에서 폭/높이를 읽는다
    if blob[:8] == b"\x89PNG\r\n\x1a\n":
        w, h = struct.unpack(">II", blob[16:24])
        return w, h
    # JPEG : SOF 마커를 찾는다
    i = 2
    while i < len(blob) - 9:
        if blob[i] != 0xFF:
            i += 1
            continue
        marker = blob[i + 1]
        if 0xC0 <= marker <= 0xCF and marker not in (0xC4, 0xC8, 0xCC):
            h, w = struct.unpack(">HH", blob[i + 5:i + 9])
            return w, h
        seg = struct.unpack(">H", blob[i + 2:i + 4])[0]
        i += 2 + seg
    return None, None


def inspect(path):
    j = glb_json(path)

    tris = 0
    for m in j.get("meshes", []):
        for pr in m["primitives"]:
            if "indices" in pr:
                tris += j["accessors"][pr["indices"]]["count"] // 3

    # 바운딩 박스는 POSITION 접근자의 min/max 로 구한다
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for m in j.get("meshes", []):
        for pr in m["primitives"]:
            acc = j["accessors"][pr["attributes"]["POSITION"]]
            for k in range(3):
                lo[k] = min(lo[k], acc["min"][k])
                hi[k] = max(hi[k], acc["max"][k])
    dims = [hi[k] - lo[k] for k in range(3)]

    # 텍스처 크기 (가장 큰 것 하나)
    tex = (0, 0)
    with open(path, "rb") as f:
        data = f.read()
    # 바이너리 청크에서 이미지 바이트를 직접 떠 온다
    json_len = struct.unpack("<I", data[12:16])[0]
    bin_start = 12 + 8 + json_len + 8
    for img in j.get("images", []):
        bv = j["bufferViews"][img["bufferView"]]
        off = bin_start + bv["byteOffset"]
        blob = data[off:off + min(bv["byteLength"], 4096)]
        w, h = png_size(blob)
        if w:
            tex = max(tex, (w, h))

    return {
        "tris": tris,
        "dims": dims,
        "height": max(dims),
        "tex": tex,
        "joints": len(j["skins"][0]["joints"]) if j.get("skins") else 0,
        "anims": [a.get("name") for a in j.get("animations", [])],
    }


def compare(boss_path, ref_path, boss_scale=3.0, ref_scale=1.0):
    boss = inspect(boss_path)
    ref = inspect(ref_path)

    rows = []
    for tag, info, s in (("기존 몹", ref, ref_scale), ("보스", boss, boss_scale)):
        h = info["height"] * s
        texel = info["tex"][0] / h if h else 0
        # 실루엣 한 바퀴를 세는 대신, 삼각형 수의 제곱근을 선형 밀도의 대용으로 쓴다
        lin = (info["tris"] ** 0.5) / h if h else 0
        rows.append((tag, s, info["tris"], info["tex"][0], h, texel, lin, info["joints"]))

    print(f"{'':8s} {'배율':>4s} {'삼각형':>8s} {'텍스처':>6s} {'높이m':>7s} "
          f"{'텍셀/m':>8s} {'폴리선밀도':>10s} {'본':>4s}")
    for tag, s, t, tx, h, texel, lin, j in rows:
        print(f"{tag:8s} {s:4.1f} {t:8d} {tx:6d} {h:7.2f} {texel:8.0f} {lin:10.1f} {j:4d}")

    b = rows[1]
    r = rows[0]
    print()
    print(f"텍셀 밀도 비   보스/몹 = {b[5] / r[5]:.2f}  (1.0 이상이면 몹만큼 선명하다)")
    print(f"폴리 선밀도 비 보스/몹 = {b[6] / r[6]:.2f}  (1.0 이상이면 몹만큼 매끄럽다)")
