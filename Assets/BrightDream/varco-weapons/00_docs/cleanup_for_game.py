"""
VARCO 원본 GLB 를 게임용으로 정리한다.

  1. 메시 전부 합치기
  2. 정점 용접        VARCO 출력은 용접되어 있지 않다
  3. 폴리곤 감축      20,000 -> 목표치
  4. 텍스처 축소      2048 -> 1024
  5. 크기 정규화      최장축 1.0m -> 0.22m (손에 드는 장난감 총)
  6. 원점 이동        바운딩 박스 바닥 중앙 (손잡이 아래) — 몹의 발밑 원점과 같은 규칙
  7. GLB 내보내기

  blender -b -P cleanup_for_game.py -- <in.glb> <out.glb> [목표폴리곤] [텍스처크기] [목표크기m]
"""
import sys
import bpy

a = sys.argv[sys.argv.index("--") + 1:]
src, dst = a[0], a[1]
TARGET_TRIS = int(a[2]) if len(a) > 2 else 3000
TEX = int(a[3]) if len(a) > 3 else 1024
SIZE_M = float(a[4]) if len(a) > 4 else 0.22

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not objs:
    raise SystemExit("메시 없음")

# 1) 합치기
bpy.ops.object.select_all(action="DESELECT")
for o in objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = objs[0]
if len(objs) > 1:
    bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
ob.name = "weapon"

tris_before = len(ob.data.polygons)
verts_before = len(ob.data.vertices)

# 2) 용접
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=1e-4)
bpy.ops.object.mode_set(mode="OBJECT")

# 3) 감축 — UV 를 유지하려고 COLLAPSE 를 쓴다
cur = len(ob.data.polygons)
if cur > TARGET_TRIS:
    m = ob.modifiers.new("dec", "DECIMATE")
    m.decimate_type = "COLLAPSE"
    m.ratio = TARGET_TRIS / cur
    m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)

# 4) 텍스처 축소
for img in bpy.data.images:
    if img.size[0] > TEX or img.size[1] > TEX:
        img.scale(min(img.size[0], TEX), min(img.size[1], TEX))

# 5) 크기 정규화
vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
lo = [min(v[i] for v in vs) for i in range(3)]
hi = [max(v[i] for v in vs) for i in range(3)]
longest = max(hi[i] - lo[i] for i in range(3))
ob.scale = [SIZE_M / longest] * 3
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# 6) 원점 — 바닥 중앙
vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
lo = [min(v[i] for v in vs) for i in range(3)]
hi = [max(v[i] for v in vs) for i in range(3)]
pivot = ((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2])
bpy.context.scene.cursor.location = pivot
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
ob.location = (0, 0, 0)

bpy.ops.object.shade_smooth()

# 7) 내보내기
bpy.ops.export_scene.gltf(
    filepath=dst,
    export_format="GLB",
    export_apply=True,
    export_yup=True,
)

tris_after = len(ob.data.polygons)
print(f"RESULT {tris_before}->{tris_after} tris  {verts_before}->{len(ob.data.vertices)} verts  "
      f"size {SIZE_M}m  tex {TEX}")
