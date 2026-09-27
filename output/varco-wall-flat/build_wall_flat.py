"""
납작한 펠트 벽면 장식 20종을 한 번에 빌드한다.

felt_cutout_builder 로 각 이미지의 실루엣을 따서 얇은 판을 만들고,
격자로 늘어놓은 뒤 .blend / .glb 로 내보낸다.

Blender 에서:
    exec(open(r"C:\\Ondukong\\varco-wall-flat\\build_wall_flat.py").read())
"""

import os
import sys
import math

import bpy

ROOT = r"C:\Ondukong\varco-wall-flat"
SRC = os.path.join(ROOT, "src")
OUT = os.path.join(ROOT, "out")

if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
import felt_cutout_builder as fcb           # noqa: E402
import importlib                            # noqa: E402
importlib.reload(fcb)


# (파일 이름, 오브젝트 이름, 긴 변 길이(m))
# 크기는 실제 벽에 붙는다고 보고 잡았다. 나무는 사람 키보다 크고, 나비는 손바닥만하다.
ASSETS = [
    ("01_tree_apple",    2.00),
    ("02_tree_round",    1.80),
    ("03_tree_pine",     2.20),
    ("04_bush",          0.80),
    ("05_flower_group",  0.50),
    ("06_grass_tuft",    0.40),
    ("07_cloud_large",   2.50),
    ("08_cloud_small",   1.40),
    ("09_sun",           1.60),
    ("10_rainbow",       3.00),
    ("11_bird",          0.50),
    ("12_butterfly",     0.35),
    ("13_hill",          4.00),
    ("14_house",         1.60),
    ("15_fence",         2.00),
    ("16_mushroom",      0.50),
    ("17_star",          0.50),
    ("18_moon",          1.00),
    ("19_balloon",       0.80),
    ("20_bunting",       3.00),
]


def thickness_for(size):
    """두께는 크기에 살짝만 비례시킨다. 어디까지나 '펠트 한두 겹' 범위를 지킨다."""
    return min(max(size * 0.003, 0.003), 0.010)


def reset_scene():
    bpy.ops.wm.read_homefile()
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for blk in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(blk):
            if item.users == 0:
                blk.remove(item)


def build_all(erode_px=3, simplify=1.2, columns=5, gap=0.6):
    reset_scene()

    built = []
    missing = []
    x_cursor = 0.0
    row_top = 0.0
    row_max_h = 0.0
    col = 0

    for name, size in ASSETS:
        path = os.path.join(SRC, name + ".png")
        if not os.path.exists(path):
            missing.append(name)
            continue

        obj = fcb.build_cutout(
            name, path,
            size=size,
            thickness=thickness_for(size),
            bg="magenta",
            simplify=simplify,
            erode_px=erode_px,
        )
        obj.data.materials.append(fcb.make_material(name + "_mat", path))

        # 바닥에 세워 둔 것처럼 보이도록 원점을 아래쪽 가운데로 옮긴다
        bpy.context.view_layer.update()
        dep = bpy.context.evaluated_depsgraph_get()
        dims = obj.evaluated_get(dep).dimensions
        w, h = dims.x, dims.z

        obj.location = (x_cursor + w * 0.5, 0.0, row_top + h * 0.5)

        x_cursor += w + gap
        row_max_h = max(row_max_h, h)
        col += 1
        if col >= columns:
            col = 0
            x_cursor = 0.0
            row_top -= row_max_h + gap
            row_max_h = 0.0

        built.append((name, size, w, h, dims.y,
                      len(obj.evaluated_get(dep).data.vertices),
                      len(obj.evaluated_get(dep).data.polygons)))

    return built, missing


def finish(built):
    os.makedirs(OUT, exist_ok=True)

    # 모디파이어를 적용해 실제 두께를 가진 메시로 굳힌다
    for obj in bpy.data.objects:
        if obj.type != 'MESH':
            continue
        bpy.context.view_layer.objects.active = obj
        for m in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)

    blend = os.path.join(OUT, "wall_flat_felt.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend)

    glb = os.path.join(OUT, "wall_flat_felt.glb")
    bpy.ops.export_scene.gltf(filepath=glb, export_format='GLB',
                              export_animations=False, use_selection=False)

    return blend, glb


def report(built, missing):
    tri = sum(b[6] for b in built)
    vert = sum(b[5] for b in built)
    print()
    print(f"{'이름':<20s} {'가로':>6s} {'세로':>6s} {'두께':>7s} {'정점':>6s} {'면':>6s}")
    for name, size, w, h, t, v, f in built:
        print(f"{name:<20s} {w:6.2f} {h:6.2f} {t:7.4f} {v:6d} {f:6d}")
    print(f"{'합계':<20s} {'':>6s} {'':>6s} {'':>7s} {vert:6d} {tri:6d}")
    if missing:
        print("누락:", ", ".join(missing))
