"""
저폴리에서 리깅한 결과를 고폴리 원본에 옮겨 붙인다.

주의: 이 스크립트는 아직 실제로 쓰인 적이 없다.
유니콘 보스 리깅이 끝내 성공하지 못해 입력(리깅된 저폴리)이 없기 때문이다.
리깅이 뚫리면 바로 쓸 수 있도록 미리 만들어 둔 것이다.

배경.
VARCO 오토리거가 유니콘 보스(80,000 트라이앵글)를 리깅하지 못했다.
폴리곤 수를 의심해 30,000 으로 리메시하고 다시 걸었지만 4족 모드는 여전히 실패했다.
(휴머노이드 모드는 태스크만 통과하고 본 23개 중 16개가 한 점에 뭉쳤다.)

만약 폴리곤 수를 낮춰야 리깅이 통과하는 상황이 온다면, 3배 크기 보스를
30,000 폴리로 깎아 버리면 애초에 고폴리로 뽑은 의미가 없어진다. 그때 이렇게 한다.

  1. 리깅된 저폴리(아마추어 + 스킨 웨이트)를 가져온다
  2. 텍스처가 살아 있는 고폴리 원본을 가져온다
  3. 저폴리의 버텍스 그룹(본 웨이트)을 고폴리로 보간 전송한다
  4. 고폴리를 같은 아마추어에 묶는다

애니메이션은 저폴리에서 계산된 웨이트를 쓰되, 화면에 보이는 메시는
80,000 폴리 + 4096 텍스처 그대로 남는다.
"""

import bpy


def ctx():
    wm = bpy.data.window_managers[0]
    win = wm.windows[0]
    scr = win.screen
    area = next(a for a in scr.areas if a.type == 'VIEW_3D')
    region = next(r for r in area.regions if r.type == 'WINDOW')
    return win, scr, area, region


def import_glb(path):
    """임포트하고 이번에 새로 생긴 오브젝트만 돌려준다."""
    before = set(bpy.data.objects.keys())
    win, scr, area, region = ctx()
    with bpy.context.temp_override(window=win, screen=scr, area=area, region=region):
        bpy.ops.import_scene.gltf(filepath=path)
    return [bpy.data.objects[n] for n in set(bpy.data.objects.keys()) - before]


def biggest_mesh(objs):
    return max((o for o in objs if o.type == 'MESH'),
               key=lambda o: len(o.data.polygons))


def transfer(rigged_glb, highpoly_glb, out_name="boss_highpoly"):
    win, scr, area, region = ctx()

    rig_objs = import_glb(rigged_glb)
    arm = next(o for o in rig_objs if o.type == 'ARMATURE')
    low = biggest_mesh(rig_objs)

    high_objs = import_glb(highpoly_glb)
    high = biggest_mesh(high_objs)
    high.name = out_name

    # 임포트로 딸려 온 나머지(본 셰이프용 아이코스피어 등)는 치운다
    for o in high_objs:
        if o is not high:
            bpy.data.objects.remove(o, do_unlink=True)

    # 저폴리와 고폴리는 같은 원본에서 나왔으므로 좌표계가 일치한다.
    # 혹시 스케일이 다르면 전송이 엉키므로 맞춰 둔다.
    high.matrix_world = low.matrix_world.copy()

    with bpy.context.temp_override(window=win, screen=scr, area=area, region=region):
        # 1) 버텍스 그룹(본 웨이트)을 저폴리 -> 고폴리 로 보간 전송
        bpy.ops.object.select_all(action='DESELECT')
        high.select_set(True)
        low.select_set(True)
        bpy.context.view_layer.objects.active = low      # 활성 = 보내는 쪽

        bpy.ops.object.data_transfer(
            use_reverse_transfer=False,
            data_type='VGROUP_WEIGHTS',
            vert_mapping='POLYINTERP_NEAREST',   # 가장 가까운 면 위에서 보간 — 표면이 밀착돼 있어 정확하다
            layers_select_src='ALL',
            layers_select_dst='NAME',
            mix_mode='REPLACE',
        )

        # 2) 고폴리를 같은 아마추어에 묶는다 (웨이트는 이미 있으므로 새로 계산하지 않는다)
        bpy.ops.object.select_all(action='DESELECT')
        high.select_set(True)
        bpy.context.view_layer.objects.active = high
        mod = high.modifiers.new("Armature", 'ARMATURE')
        mod.object = arm
        high.parent = arm

        # 3) 저폴리는 더 이상 필요 없다
        bpy.data.objects.remove(low, do_unlink=True)

    groups = len(high.vertex_groups)
    bones = len(arm.data.bones)
    print(f"전송 완료: 고폴리 {len(high.data.polygons)} 면, "
          f"버텍스 그룹 {groups} 개, 아마추어 본 {bones} 개")
    if groups < bones * 0.5:
        print("경고: 전송된 버텍스 그룹이 본 수에 비해 너무 적다. 좌표계가 어긋났을 수 있다.")
    return high, arm
