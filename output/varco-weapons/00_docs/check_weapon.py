"""
장난감 총 GLB 검증.

varco-mobs/check_scale.py 의 프롭 버전이다. 차이는 두 가지.
  - 스켈레톤을 기대하지 않는다 (리깅 안 하는 프롭)
  - 납작해졌는지(두께 붕괴)를 본다. 정측면 이미지로 생성한 02 / 05 가
    실제로 판때기가 되었는지 여기서 걸러낸다.

기준:
  길이 0.20 ~ 0.25 m  (몹 0.900 m 기준 손에 드는 크기)
  최단축 / 최장축 >= 0.18   이보다 작으면 납작하다고 본다
"""

import io
import json
import struct
import sys
from pathlib import Path

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

TARGET_LEN = (0.20, 0.25)
FLAT_RATIO = 0.18


def read_glb(path):
    data = Path(path).read_bytes()
    magic, version, _ = struct.unpack_from("<III", data, 0)
    if magic != 0x46546C67:
        raise ValueError(f"{path}: glTF 바이너리가 아니다")
    off, gltf, bin_chunk = 12, None, None
    while off < len(data):
        clen, ctype = struct.unpack_from("<II", data, off)
        chunk = data[off + 8: off + 8 + clen]
        if ctype == 0x4E4F534A:
            gltf = json.loads(chunk)
        elif ctype == 0x004E4942:
            bin_chunk = chunk
        off += 8 + clen + (-clen % 4)
    return gltf, bin_chunk


def bounds(gltf):
    """POSITION 액세서의 min/max 로 바운딩 박스를 낸다."""
    lo = [float("inf")] * 3
    hi = [float("-inf")] * 3
    for mesh in gltf.get("meshes", []):
        for prim in mesh.get("primitives", []):
            idx = prim.get("attributes", {}).get("POSITION")
            if idx is None:
                continue
            acc = gltf["accessors"][idx]
            if "min" not in acc or "max" not in acc:
                continue
            for i in range(3):
                lo[i] = min(lo[i], acc["min"][i])
                hi[i] = max(hi[i], acc["max"][i])
    return lo, hi


def tri_count(gltf):
    n = 0
    for mesh in gltf.get("meshes", []):
        for prim in mesh.get("primitives", []):
            if "indices" in prim:
                n += gltf["accessors"][prim["indices"]]["count"] // 3
            else:
                idx = prim.get("attributes", {}).get("POSITION")
                if idx is not None:
                    n += gltf["accessors"][idx]["count"] // 3
    return n


def check(path):
    gltf, _ = read_glb(path)
    lo, hi = bounds(gltf)
    size = [hi[i] - lo[i] for i in range(3)]
    longest, shortest = max(size), min(size)
    ratio = shortest / longest if longest else 0.0

    print(f"\n{Path(path).name}")
    print(f"  크기      {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} m")
    print(f"  최장축    {longest:.3f} m")
    print(f"  두께비    {ratio:.3f}  (최단축/최장축)")
    print(f"  폴리곤    {tri_count(gltf):,}")
    print(f"  텍스처    {len(gltf.get('images', []))}장")
    print(f"  스켈레톤  {'있음 (프롭인데 이상하다)' if gltf.get('skins') else '없음 (정상)'}")

    problems = []
    if ratio < FLAT_RATIO:
        problems.append(f"납작하다 — 두께비 {ratio:.3f} < {FLAT_RATIO}. 각도 다시 뽑아야 한다")
    if not gltf.get("images"):
        problems.append("텍스처가 없다")

    # 스케일은 임포트 시 맞추면 되므로 경고만
    if not (TARGET_LEN[0] <= longest <= TARGET_LEN[1]):
        print(f"  참고      최장축이 목표 {TARGET_LEN[0]}~{TARGET_LEN[1]} m 밖이다. "
              f"임포트 시 {TARGET_LEN[1] / longest:.3f} 배로 맞출 것")

    if problems:
        for p in problems:
            print(f"  FAIL {p}")
        return False
    print("  OK 통과")
    return True


if __name__ == "__main__":
    targets = sys.argv[1:] or sorted(str(p) for p in Path(".").rglob("*.glb"))
    if not targets:
        print("검사할 GLB 가 없다")
        sys.exit(1)
    ok = all([check(t) for t in targets])
    sys.exit(0 if ok else 1)
