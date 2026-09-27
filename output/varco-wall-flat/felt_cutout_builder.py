"""
납작한 펠트 컷아웃 메시 빌더.

평면 이미지 한 장에서 실루엣을 따서, 그 모양 그대로 아주 얇은 판을 만든다.
VARCO 의 Generate3D 를 쓰지 않는 이유는 간단하다 —
평면 이미지를 3D 로 변환하면 두께 정보가 없어서 큐브처럼 부풀어 버린다.
(01_wall_sky_panel / 02_wall_corner 에서 이미 겪었다.)
여기서는 두께를 우리가 직접 지정하므로 "얇게"가 100% 보장된다.

동작 순서
  1. 이미지를 읽어 배경을 걷어 내고 이진 마스크를 만든다
  2. 마스크의 경계를 픽셀 사이 틈(crack)을 따라가며 폴리곤 루프로 뽑는다
     - 바깥 윤곽과 구멍이 부호 있는 면적으로 자동 구분된다
  3. 루프를 단순화(RDP)해 정점 수를 줄인다
  4. 구멍을 포함해 삼각분할하고, 지정한 두께만큼 밀어낸다
  5. 원본 이미지를 알파 클립 머티리얼로 입힌다

Blender 5.2 / bpy 기준. numpy 외 외부 의존성 없음.
"""

import math

import bpy
import bmesh
import numpy as np
from mathutils import Vector
from mathutils.geometry import tessellate_polygon


# ---------------------------------------------------------------- 마스크

def load_rgba(path, max_side=512):
    """이미지를 (H, W, 4) float 배열로 읽는다. 위가 0행이 되도록 뒤집어 둔다."""
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    arr = buf.reshape(h, w, 4)[::-1]          # Blender 는 아래에서 위로 저장한다
    bpy.data.images.remove(img)

    # 경계 추적은 512 정도면 충분하다. 크면 느리기만 하다.
    step = max(1, int(math.ceil(max(h, w) / max_side)))
    if step > 1:
        arr = arr[::step, ::step]
    return arr


def build_mask(arr, bg="white", thresh=0.90, alpha_thresh=0.5):
    """전경 픽셀이 True 인 이진 마스크.

    bg="alpha"   : 알파 채널을 그대로 쓴다 (배경이 이미 투명한 경우)
    bg="white"   : 흰 배경. 밝고 채도 낮은 픽셀을 배경으로 본다
    bg="magenta" : 마젠타 크로마키 배경
    """
    rgb = arr[..., :3]

    if bg == "alpha":
        return arr[..., 3] > alpha_thresh

    if bg == "magenta":
        # 절대 밝기로 자르면 배경 마젠타의 톤이 조금만 달라져도 통째로 실패한다.
        # (01_tree_apple 의 배경은 b=0.544 라서 b>0.55 기준을 아슬아슬하게 빠져나갔다)
        # 마젠타의 정의 그대로 "빨강과 파랑이 초록보다 뚜렷하게 높다" 로 판정한다.
        r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
        is_bg = (r > g + 0.20) & (b > g + 0.15) & (g < 0.50)
        return ~is_bg

    # 흰 배경: 밝기가 높고 채도가 낮으면 배경
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    sat = mx - mn
    is_bg = (mn > thresh) & (sat < 0.06)
    return ~is_bg


def erode(mask, iterations=1):
    """마스크를 안쪽으로 깎는다.

    배경과 물체 사이의 안티에일리어싱 픽셀이 실루엣에 섞이면
    가장자리에 배경색 테두리가 남는다. 한두 픽셀 깎으면 깔끔해진다.
    """
    for _ in range(iterations):
        m = mask
        up = np.vstack([m[1:], np.zeros((1, m.shape[1]), dtype=bool)])
        dn = np.vstack([np.zeros((1, m.shape[1]), dtype=bool), m[:-1]])
        lf = np.hstack([m[:, 1:], np.zeros((m.shape[0], 1), dtype=bool)])
        rt = np.hstack([np.zeros((m.shape[0], 1), dtype=bool), m[:, :-1]])
        mask = m & up & dn & lf & rt
    return mask


def largest_blobs(mask, min_area_ratio=0.002):
    """너무 작은 부스러기를 털어 낸다. 4-연결 성분 중 면적 비율이 기준 이상인 것만 남긴다."""
    h, w = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    keep = np.zeros_like(mask, dtype=bool)
    min_area = int(h * w * min_area_ratio)

    for sr in range(h):
        row = mask[sr]
        for sc in range(w):
            if not row[sc] or seen[sr, sc]:
                continue
            stack = [(sr, sc)]
            seen[sr, sc] = True
            comp = []
            while stack:
                r, c = stack.pop()
                comp.append((r, c))
                for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nr, nc = r + dr, c + dc
                    if 0 <= nr < h and 0 <= nc < w and mask[nr, nc] and not seen[nr, nc]:
                        seen[nr, nc] = True
                        stack.append((nr, nc))
            if len(comp) >= min_area:
                for r, c in comp:
                    keep[r, c] = True
    return keep


# ---------------------------------------------------------------- 경계 추적

def trace_loops(mask):
    """픽셀 사이 틈을 따라 닫힌 폴리곤 루프들을 뽑는다.

    전경을 왼쪽에 두고 도는 방향으로 방향성 간선을 만들면
    바깥 윤곽과 구멍이 서로 반대 방향으로 나와서 부호 있는 면적으로 구분된다.
    격자 틈을 따라가므로 대각선 애매함이 없고 실루엣이 정확하다.
    """
    h, w = mask.shape
    padded = np.zeros((h + 2, w + 2), dtype=bool)
    padded[1:-1, 1:-1] = mask

    edges = {}          # 시작 코너 -> [끝 코너, ...]
    fg = padded
    rs, cs = np.nonzero(fg)
    for r, c in zip(rs.tolist(), cs.tolist()):
        if not fg[r - 1, c]:
            edges.setdefault((r, c), []).append((r, c + 1))          # 위쪽 틈 : 오른쪽으로
        if not fg[r, c + 1]:
            edges.setdefault((r, c + 1), []).append((r + 1, c + 1))  # 오른쪽 틈 : 아래로
        if not fg[r + 1, c]:
            edges.setdefault((r + 1, c + 1), []).append((r + 1, c))  # 아래쪽 틈 : 왼쪽으로
        if not fg[r, c - 1]:
            edges.setdefault((r + 1, c), []).append((r, c))          # 왼쪽 틈 : 위로

    loops = []
    while edges:
        start = next(iter(edges))
        loop = []
        cur = start
        while True:
            outs = edges.get(cur)
            if not outs:
                break
            nxt = outs.pop()
            if not outs:
                del edges[cur]
            loop.append(cur)
            cur = nxt
            if cur == start:
                break
        if len(loop) >= 4:
            loops.append(loop)
    return loops


def signed_area(loop):
    a = 0.0
    n = len(loop)
    for i in range(n):
        y0, x0 = loop[i]
        y1, x1 = loop[(i + 1) % n]
        a += x0 * y1 - x1 * y0
    return a * 0.5


def rdp(points, eps):
    """Ramer-Douglas-Peucker. 계단 모양 경계를 매끈한 폴리라인으로 줄인다."""
    if len(points) < 3:
        return points

    def _rdp(pts):
        if len(pts) < 3:
            return pts
        p0 = np.array(pts[0], dtype=float)
        p1 = np.array(pts[-1], dtype=float)
        seg = p1 - p0
        seg_len = np.hypot(*seg)
        best_i, best_d = 0, -1.0
        for i in range(1, len(pts) - 1):
            p = np.array(pts[i], dtype=float)
            if seg_len < 1e-9:
                d = np.hypot(*(p - p0))
            else:
                d = abs(seg[0] * (p0[1] - p[1]) - (p0[0] - p[0]) * seg[1]) / seg_len
            if d > best_d:
                best_i, best_d = i, d
        if best_d <= eps:
            return [pts[0], pts[-1]]
        return _rdp(pts[:best_i + 1])[:-1] + _rdp(pts[best_i:])

    closed = list(points) + [points[0]]
    out = _rdp(closed)
    return out[:-1]


def point_in_loop(pt, loop):
    y, x = pt
    inside = False
    n = len(loop)
    for i in range(n):
        y0, x0 = loop[i]
        y1, x1 = loop[(i + 1) % n]
        if (y0 > y) != (y1 > y):
            xin = (x1 - x0) * (y - y0) / (y1 - y0) + x0
            if x < xin:
                inside = not inside
    return inside


# ---------------------------------------------------------------- 메시 생성

def build_cutout(name, image_path, size=1.0, thickness=0.004,
                 bg="white", simplify=1.2, min_area_ratio=0.002,
                 erode_px=2):
    """이미지 한 장을 얇은 펠트 조각 메시로 만든다.

    size       : 완성 메시의 긴 변 길이 (미터)
    thickness  : 판 두께 (미터). 펠트 한두 겹이면 0.003 ~ 0.005 가 적당하다
    simplify   : RDP 허용 오차 (마스크 픽셀 단위). 키우면 정점이 줄고 윤곽이 뭉툭해진다
    erode_px   : 실루엣을 안쪽으로 깎을 픽셀 수. 가장자리 배경색 테두리를 없앤다
    """
    arr = load_rgba(image_path)
    mask = build_mask(arr, bg=bg)

    # 키잉이 실패하면 화면 전체가 전경이 되어 "그냥 사각형"이 나온다.
    # 조용히 넘어가면 20개 중 하나만 사각형인 걸 눈으로 찾아야 하므로 여기서 막는다.
    ratio = mask.mean()
    if ratio > 0.80:
        raise RuntimeError(
            f"{name}: 배경 키잉 실패로 보인다 (전경 비율 {ratio:.2f}). "
            f"bg={bg} 판정 기준과 원본 배경색을 확인할 것")
    if ratio < 0.005:
        raise RuntimeError(f"{name}: 전경을 찾지 못했다 (전경 비율 {ratio:.3f})")

    if erode_px:
        mask = erode(mask, erode_px)
    mask = largest_blobs(mask, min_area_ratio)
    if not mask.any():
        raise RuntimeError(f"{name}: 침식 후 남은 영역이 없다")

    loops = trace_loops(mask)
    if not loops:
        raise RuntimeError(f"{name}: 경계 루프가 없다")

    # 1~2 픽셀짜리 잡티 루프는 버린다. 삼각분할이 깨진다.
    min_loop_area = max(24.0, mask.size * 0.00005)

    simplified = []
    for lp in loops:
        if abs(signed_area(lp)) < min_loop_area:
            continue
        s = rdp(lp, simplify)
        if len(s) >= 3:
            simplified.append((signed_area(s), s))

    outers = [s for a, s in simplified if a > 0]
    holes = [s for a, s in simplified if a < 0]
    if not outers:                       # 방향이 반대로 나온 경우
        outers = [s for a, s in simplified if a < 0]
        holes = []

    # 실제 실루엣의 바운딩 박스를 기준으로 스케일을 잡는다.
    # (이미지 여백까지 포함해 재면 에셋마다 크기가 제멋대로가 된다)
    all_pts = [p for _, s in simplified for p in s]
    rr = [p[0] for p in all_pts]
    cc = [p[1] for p in all_pts]
    bbox_px = max(max(cc) - min(cc), max(rr) - min(rr))

    h, w = mask.shape
    scale = size / max(bbox_px, 1)
    ox = (min(cc) + max(cc)) * 0.5
    oy = (min(rr) + max(rr)) * 0.5

    def to_vec(pt):
        r, c = pt
        return Vector(((c - ox) * scale, 0.0, (oy - r) * scale))

    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)

    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")

    for outer in outers:
        mine = [hl for hl in holes if point_in_loop(hl[0], outer)]
        contours = [[to_vec(p) for p in outer]] + [[to_vec(p) for p in hl] for hl in mine]
        tris = tessellate_polygon(contours)

        flat = [p for contour in contours for p in contour]
        verts = [bm.verts.new(v) for v in flat]
        for a, b, c in tris:
            try:
                bm.faces.new((verts[a], verts[b], verts[c]))
            except ValueError:
                pass                      # 중복 면은 건너뛴다

    bm.verts.ensure_lookup_table()
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=scale * 0.25)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    # 평면 투영 UV — 월드 좌표를 원본 이미지 픽셀 좌표로 되돌려 매핑한다
    for face in bm.faces:
        for loop in face.loops:
            co = loop.vert.co
            c = co.x / scale + ox
            r = oy - co.z / scale
            loop[uv_layer].uv = (c / w, 1.0 - r / h)

    bm.to_mesh(mesh)
    bm.free()

    # 두께 — 여기서 "얇게"가 결정된다
    solid = obj.modifiers.new("Thickness", 'SOLIDIFY')
    solid.thickness = thickness
    solid.offset = 0.0
    # use_even_offset 은 반드시 꺼 둔다.
    # 평평한 판의 테두리가 톱니처럼 잘게 꺾여 있으면 인접 면이 거의 마주 보게 되고,
    # even offset 은 그 사이각으로 나누기 때문에 두께가 수 미터로 발산한다.
    # (01_tree_apple 이 4mm 대신 23m 로 부풀어 올랐던 원인)
    solid.use_even_offset = False
    solid.use_rim = True

    return obj


def make_material(name, image_path, alpha_clip=True):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')

    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(image_path, check_existing=True)
    tex.interpolation = 'Smart'
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])

    # 펠트는 거의 완전 무광이다
    bsdf.inputs['Roughness'].default_value = 0.95
    if 'Metallic' in bsdf.inputs:
        bsdf.inputs['Metallic'].default_value = 0.0
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.15

    if alpha_clip:
        mat.blend_method = 'CLIP' if hasattr(mat, 'blend_method') else mat.blend_method
    return mat
