"""
GLB 의 실제 분리 덩어리 개수를 센다.

주의: VARCO 출력은 정점이 용접되어 있지 않다. 그대로 loose parts 를 세면
표면 패치 수백 개가 잡혀 아무 의미가 없다. 반드시 거리 병합을 먼저 한다.

  blender -b -P count_loose_parts.py -- <in.glb>
"""
import sys
import bpy

src = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]

# 1) 하나로 합치고
bpy.ops.object.select_all(action="DESELECT")
for o in objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = objs[0]
if len(objs) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active

pre = len(obj.data.vertices)

# 2) 정점 병합 (용접)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=1e-4)
bpy.ops.object.mode_set(mode="OBJECT")
post = len(obj.data.vertices)

# 3) 그제서야 loose parts
before = {x.name for x in bpy.context.scene.objects}
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.mesh.separate(type="LOOSE")
parts = [x for x in bpy.context.scene.objects if x.type == "MESH"]

info = []
for p in parts:
    vs = [p.matrix_world @ v.co for v in p.data.vertices]
    if not vs:
        continue
    span = max(max(v[i] for v in vs) - min(v[i] for v in vs) for i in range(3))
    info.append((span, len(p.data.polygons)))
info.sort(reverse=True)

print(f"RESULT verts {pre} -> {post} (병합 {pre-post})  parts={len(info)}")
for i, (span, tris) in enumerate(info[:8]):
    print(f"  part{i:02d}  최장변 {span:.3f} m  면 {tris}")
if len(info) > 8:
    tiny = sum(1 for s, _ in info if s < 0.05)
    print(f"  ... 외 {len(info)-8}개 (그 중 5cm 미만 부스러기 {tiny}개)")
