"""
GLB 를 4방향에서 렌더한다. Blender CLI 로 헤드리스 실행.
  blender -b -P render_turnaround.py -- <in.glb> <out_dir> <basename>
"""
import sys, math, os
import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, base = argv[0], argv[1], argv[2]
os.makedirs(out_dir, exist_ok=True)

# 씬 비우기
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene

bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in sc.objects if o.type == "MESH"]
if not meshes:
    raise SystemExit("메시 없음")

# 바운딩 박스 -> 원점 정렬, 1m 정규화
lo = [min((o.matrix_world @ v.co)[i] for o in meshes for v in o.data.vertices) for i in range(3)]
hi = [max((o.matrix_world @ v.co)[i] for o in meshes for v in o.data.vertices) for i in range(3)]
ctr = [(lo[i] + hi[i]) / 2 for i in range(3)]
span = max(hi[i] - lo[i] for i in range(3)) or 1.0

root = bpy.data.objects.new("root", None)
sc.collection.objects.link(root)
for o in meshes:
    if o.parent is None:
        o.parent = root
root.location = (-ctr[0], -ctr[1], -ctr[2])

# 라이팅 — 균일한 스튜디오
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [
    i.identifier for i in sc.render.bl_rna.properties["engine"].enum_items] else sc.render.engine
world = bpy.data.worlds.new("w"); sc.world = world
world.use_nodes = True
bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
bg.inputs[0].default_value = (1, 1, 1, 1)
bg.inputs[1].default_value = 1.6

for ang, energy in ((45, 400), (-120, 200)):
    d = bpy.data.lights.new(f"L{ang}", type="AREA"); d.energy = energy; d.size = 6
    ob = bpy.data.objects.new(f"L{ang}", d); sc.collection.objects.link(ob)
    a = math.radians(ang)
    ob.location = (math.cos(a) * 4, math.sin(a) * 4, 3)
    ob.rotation_euler = (math.radians(55), 0, a + math.pi / 2)

cam_d = bpy.data.cameras.new("cam"); cam = bpy.data.objects.new("cam", cam_d)
sc.collection.objects.link(cam); sc.camera = cam
cam_d.lens = 70

sc.render.resolution_x = sc.render.resolution_y = 512
sc.render.film_transparent = False
sc.render.image_settings.file_format = "PNG"

dist = span * 3.1
for name, deg in (("front", 0), ("side", 90), ("back", 180), ("top34", 45)):
    a = math.radians(deg - 90)
    z = span * (1.6 if name == "top34" else 0.55)
    cam.location = (math.cos(a) * dist, math.sin(a) * dist, z)
    # 원점을 바라보게
    dvec = cam.location
    cam.rotation_euler = (
        math.atan2(math.hypot(dvec[0], dvec[1]), dvec[2]),
        0,
        math.atan2(dvec[1], dvec[0]) + math.pi / 2,
    )
    sc.render.filepath = os.path.join(out_dir, f"{base}_{name}.png")
    bpy.ops.render.render(write_still=True)
print("DONE", base)
