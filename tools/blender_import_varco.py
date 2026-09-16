"""
VARCO 3D에서 받은 OBJ 에셋을 Blender로 가져와 배치 가능한 상태로 다듬는 스크립트.

Blender의 Scripting 탭에서 이 파일을 열고 실행하면 된다.

VARCO OBJ를 그냥 임포트하면 다섯 가지가 어긋난다. 실제로 37개를 넣어 보고
확인한 내용이며, 이 스크립트가 전부 처리한다.

1. 버텍스가 UV 시임마다 쪼개져 있어 메시가 수백 개 조각으로 보인다
   (유니콘 기준 16,538버텍스 / 셸 574개 / 논매니폴드 엣지 12,002개)
   -> Merge by Distance 후 9,844버텍스 / 셸 1개 / 논매니폴드 0개
   -> UV는 루프 단위 데이터라 병합해도 보존된다 (퇴화 UV 삼각형 0개 확인)

2. 90도 X 회전이 적용되지 않은 채로 남는다 (OBJ의 Y-up을 Z-up으로 바꾸느라)
   -> 회전을 메시에 구워야 배치할 때 축이 어긋나지 않는다

3. 원점이 메시 중앙에 있어 바닥에 놓으면 절반이 파묻힌다
   -> 원점을 바닥 중앙으로 옮긴다

4. ORM/노멀 텍스처가 sRGB로 임포트된다
   -> 데이터 맵이므로 Non-Color여야 한다. 아니면 거칠기와 굴곡이 틀어진다

5. ORM 텍스처가 Metallic/Roughness에 통째로 직결된다
   -> ORM은 R=차폐, G=거칠기, B=메탈릭으로 채널이 나뉘어 있다.
      Separate Color로 갈라서 연결해야 한다

여기에 더해 각도 기반 스무스 셰이딩과 실제 비례 크기를 적용한다.


─────────────────────────────────────────────────────────────
VARCO에서 얇은 바닥 에셋을 만들 때의 주의사항
─────────────────────────────────────────────────────────────

02_path_straight를 네 번 만들어서야 제대로 나왔다. 원인은 프롬프트 문구가
아니라 소스 이미지의 시점이었다.

  1차  "a straight path segment that tiles end to end"
       -> 다리 달린 나무 통로. 두께비 0.745
  2차  "completely FLAT, thickness less than one tenth of its width"
       -> 평평해졌으나 버섯 3개가 얹혀 나옴. 두께비 0.286
  3차  위 문구 + "no mushrooms, no decorations of any kind"
       -> 이미지는 완벽했으나 메시가 정육면체 벽돌. 두께비 1.004
  4차  "LOW three-quarter angle close to ground level,
        so that its thin side edge is clearly visible"
       -> 성공. 두께비 0.059

핵심: 위에서 내려다본 이미지에는 두께 정보가 없다. 프롬프트에서 flat이나
thin을 아무리 강조해도 3D 변환기는 두께를 추측할 수밖에 없고, 매번 다르게
틀린다. 이미지 자체에 옆면이 찍혀 있어야 한다.

연못, 잔디 타일, 보스 플랫폼이 처음부터 잘 나온 것은 통통한 형태라
위에서 봐도 두께가 드러났기 때문이다. 얇은 것만 이 문제를 겪는다.

교훈: 얇고 납작한 에셋은 프롬프트에 카메라 각도를 명시할 것.
      "photographed from a LOW three-quarter angle close to ground level,
       so that its thin side edge and shallow profile are clearly visible"
"""

import bpy
import bmesh
import math
import os

# ─────────────────────────────────────────────────────────────
# 설정
# ─────────────────────────────────────────────────────────────

ASSET_ROOT = r"C:/Ondukong/4Team-Project/output/varco-obj"

# 가져올 에셋. 빈 리스트면 ASSET_ROOT 안의 전부.
# 텍스처가 에셋당 2048 3장이라, 한 번에 너무 많이 올리면 Blender가 죽는다.
# 5~8개씩 끊어서 쓰는 것을 권장한다. (37개 전부 + 머티리얼 프리뷰에서 실제로 크래시했다)
ASSETS = []

# 제외할 에셋 (몹은 별도 작업 예정이라 건드리지 않는다)
EXCLUDE = {"33_monster_blob", "34_unicorn_boss"}

MERGE_VERTICES = True
MERGE_DISTANCE = 1e-5

# 이 각도보다 완만하면 부드럽게, 급하면 각지게.
# 전부 smooth로 두면 상자 모서리까지 뭉개져 물렁해 보인다.
SMOOTH_ANGLE = math.radians(35)

# 실제 비례 크기를 적용할지. 원본은 전부 1x1x1로 정규화되어 있다.
APPLY_REAL_SCALE = True

# (기준값 m, 기준축) - 세로로 세우는 것은 z, 바닥에 깔리거나 가로로 긴 것은 x
SCALE_TABLE = {
    "01_path_curved":   (2.0, 'x'),
    "02_path_straight": (2.0, 'x'),
    "03_grass_tile":    (2.0, 'x'),
    "04_dirt_arena":    (7.0, 'x'),
    "05_boss_platform": (9.0, 'x'),

    "06_arch_welcome":  (3.0, 'z'),
    "07_fence_picket":  (1.1, 'z'),
    "08_fence_corner":  (1.1, 'z'),
    "09_fence_wood":    (1.2, 'z'),
    "10_greenhouse":    (3.2, 'z'),
    "11_treehouse":     (6.5, 'z'),
    "12_cottage":       (4.2, 'z'),
    "13_bridge":        (2.6, 'y'),
    "14_swing":         (2.4, 'z'),

    "15_boss_tree":    (12.0, 'z'),
    "16_tree_large":    (6.0, 'z'),
    "17_tree_medium":   (4.0, 'z'),
    "18_tree_conifer":  (5.0, 'z'),
    "19_bush":          (0.8, 'z'),
    "20_daisy_cluster": (0.35, 'z'),
    "21_flower_large":  (1.6, 'z'),
    "22_pond":          (4.5, 'x'),
    "23_lilypad":       (0.5, 'x'),
    "24_rocks":         (0.9, 'z'),

    "25_lamp_post":     (3.0, 'z'),
    "26_bench":         (0.9, 'z'),
    "27_crate":         (0.8, 'z'),
    "28_barrel":        (0.9, 'z'),
    "29_photo_album":   (0.32, 'x'),
    "30_bunting":       (3.0, 'x'),
    "31_flower_pot":    (0.6, 'z'),
    "32_signpost":      (1.8, 'z'),

    "35_cloud":         (4.0, 'x'),
    "36_sun":           (3.0, 'x'),
    "37_star":          (1.0, 'z'),
}

AXIS_INDEX = {'x': 0, 'y': 1, 'z': 2}


# ─────────────────────────────────────────────────────────────
# 텍스처 종류 판별
#
# 주의: 단순히 "orm" in name 으로 검사하면 "normal" 에도 걸린다.
#       n-orm-al 이기 때문. 실제로 이것 때문에 노멀맵 노드를 지워 먹었다.
#       반드시 접미사로 판별할 것.
# ─────────────────────────────────────────────────────────────

def is_orm_map(image_name):
    n = image_name.lower()
    return n.endswith("-orm.png") or n.endswith("_orm.png")


def is_normal_map(image_name):
    n = image_name.lower()
    return n.endswith("-normal.png") or n.endswith("_normal.png")


def is_data_map(image_name):
    return is_orm_map(image_name) or is_normal_map(image_name)


# ─────────────────────────────────────────────────────────────
# 메시 정리
# ─────────────────────────────────────────────────────────────

def weld_vertices(obj):
    """UV 시임 때문에 쪼개진 버텍스를 붙인다."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    before = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=MERGE_DISTANCE)
    after = len(bm.verts)
    bm.to_mesh(obj.data)
    obj.data.update()
    bm.free()
    return before - after


def recalc_normals(obj):
    """노멀을 전부 바깥쪽으로. 뒤집힌 면이 있으면 검은 얼룩으로 보인다."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    obj.data.update()
    bm.free()


def origin_to_bottom_center(obj):
    """원점을 바닥 중앙으로. 바닥에 놓으면 z=0에 딱 붙는다."""
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    cx = (min(xs) + max(xs)) / 2
    cy = (min(ys) + max(ys)) / 2
    zmin = min(zs)
    for v in obj.data.vertices:
        v.co.x -= cx
        v.co.y -= cy
        v.co.z -= zmin
    obj.data.update()


def mesh_report(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold = sum(1 for e in bm.edges if not e.is_manifold)

    seen = set()
    shells = 0
    for f in bm.faces:
        if f.index in seen:
            continue
        shells += 1
        stack = [f]
        seen.add(f.index)
        while stack:
            cur = stack.pop()
            for e in cur.edges:
                for lf in e.link_faces:
                    if lf.index not in seen:
                        seen.add(lf.index)
                        stack.append(lf)

    info = {
        "verts": len(bm.verts),
        "faces": len(bm.faces),
        "non_manifold_edges": non_manifold,
        "shells": shells,
    }
    bm.free()
    return info


# ─────────────────────────────────────────────────────────────
# 머티리얼 배선
# ─────────────────────────────────────────────────────────────

def fix_material(mat, asset_name):
    if not mat or not mat.use_nodes:
        return []

    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        return []

    changes = []

    # 데이터 맵은 Non-Color 로
    for n in nt.nodes:
        if n.type == 'TEX_IMAGE' and n.image:
            want = 'Non-Color' if is_data_map(n.image.name) else 'sRGB'
            if n.image.colorspace_settings.name != want:
                n.image.colorspace_settings.name = want
                changes.append("%s -> %s" % (n.image.name, want))

    # ORM 직결 링크 제거.
    # 링크를 지우기 전에 이름을 먼저 읽어야 한다. 지운 뒤 접근하면 StructRNA 에러.
    doomed = []
    for link in nt.links:
        src = link.from_node
        if (src.type == 'TEX_IMAGE' and src.image
                and is_orm_map(src.image.name) and link.to_node == bsdf):
            doomed.append((link, link.to_socket.name))
    for link, socket_name in doomed:
        nt.links.remove(link)
        changes.append("직결 해제: ORM -> " + socket_name)

    # 임포터가 만든 중복 ORM 텍스처 노드 정리
    orm_nodes = [n for n in nt.nodes
                 if n.type == 'TEX_IMAGE' and n.image and is_orm_map(n.image.name)]
    for extra in orm_nodes[1:]:
        nt.nodes.remove(extra)
        changes.append("중복 ORM 노드 제거")

    # Separate Color 로 채널을 갈라 연결
    if orm_nodes:
        orm = orm_nodes[0]
        sep = next((n for n in nt.nodes if n.type == 'SEPARATE_COLOR'), None)
        if sep is None:
            sep = nt.nodes.new('ShaderNodeSeparateColor')
            sep.location = (orm.location.x + 280, orm.location.y)
            changes.append("Separate Color 추가")
        nt.links.new(orm.outputs['Color'], sep.inputs['Color'])
        nt.links.new(sep.outputs['Green'], bsdf.inputs['Roughness'])
        nt.links.new(sep.outputs['Blue'], bsdf.inputs['Metallic'])
        changes.append("ORM G->거칠기, B->메탈릭")

    mat.name = asset_name + "_mat"
    return changes


# ─────────────────────────────────────────────────────────────
# 임포트
# ─────────────────────────────────────────────────────────────

def import_asset(asset_name):
    obj_path = os.path.join(ASSET_ROOT, asset_name, asset_name + ".obj")
    if not os.path.exists(obj_path):
        print("  건너뜀 - 파일 없음:", obj_path)
        return None

    before = set(bpy.data.objects.keys())
    bpy.ops.wm.obj_import(filepath=obj_path)
    created = [bpy.data.objects[n] for n in set(bpy.data.objects.keys()) - before]
    obj = next((o for o in created if o.type == 'MESH'), None)
    if obj is None:
        return None

    obj.name = asset_name

    if MERGE_VERTICES:
        merged = weld_vertices(obj)
        print("  버텍스 %d개 병합" % merged)

    # 회전/스케일을 메시에 굽는다
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    origin_to_bottom_center(obj)
    recalc_normals(obj)

    # 각도 기반 스무스 셰이딩. 샤프 엣지를 메시에 직접 굽는다
    bpy.ops.object.shade_smooth_by_angle(angle=SMOOTH_ANGLE)

    for mat in obj.data.materials:
        for line in fix_material(mat, asset_name):
            print("  " + line)

    # 실제 비례 크기 적용
    if APPLY_REAL_SCALE and asset_name in SCALE_TABLE:
        target, axis = SCALE_TABLE[asset_name]
        current = obj.dimensions[AXIS_INDEX[axis]]
        if current > 0:
            factor = target / current
            obj.scale = (factor, factor, factor)
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            print("  크기 %s=%.2fm 적용" % (axis, target))

    sharp = sum(1 for e in obj.data.edges if e.use_edge_sharp)
    print("  결과:", mesh_report(obj), "샤프엣지:", sharp,
          "크기:", [round(d, 2) for d in obj.dimensions])
    return obj


def main():
    if not os.path.isdir(ASSET_ROOT):
        print("에셋 폴더를 찾을 수 없습니다:", ASSET_ROOT)
        return

    names = ASSETS or sorted(
        d for d in os.listdir(ASSET_ROOT)
        if os.path.isdir(os.path.join(ASSET_ROOT, d))
    )
    names = [n for n in names if n not in EXCLUDE]

    print("=" * 60)
    print("VARCO 3D 에셋 임포트 - %d개" % len(names))
    if len(names) > 10:
        print("경고: 한 번에 10개 넘게 올리면 텍스처 메모리로 Blender가 죽을 수 있습니다.")
        print("      ASSETS 리스트로 5~8개씩 끊어서 실행하세요.")
    print("=" * 60)

    for name in names:
        print("\n[%s]" % name)
        import_asset(name)

    print("\n완료.")


if __name__ == "__main__":
    main()
