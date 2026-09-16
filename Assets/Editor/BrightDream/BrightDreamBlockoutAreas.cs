using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrightDreamBlockoutBuilder 의 구간별 배치 담당 부분.
///
/// 모든 좌표는 (s, lateral) 로 잡는다.
///   s       = START 에서부터 걸어간 거리(m)
///   lateral = 그 지점 길 중심선 기준 좌우(+가 오른쪽)
/// 중심선이 휘어도 배치가 알아서 따라오기 때문에, 맵 모양을 바꿔도 이 파일은 거의 손대지 않아도 된다.
///
/// InArea("CombatArena", 0.5f) 처럼 쓰면 그 구간의 한가운데 s 를 얻는다.
/// </summary>
public static partial class BrightDreamBlockoutBuilder
{
    // ==========================================================
    // 1. 시작 정원
    // ==========================================================
    private static void BuildStartGarden(Transform parent, Transform markers)
    {
        var rng = new System.Random(101);
        MakeMarker(markers, "Marker_PlayerStart", SpinePoint(PlayerStartS), MatMarkerClue, 0.3f);

        // WELCOME 아치
        float archS = InArea("StartGarden", 0.32f);
        var arch = Child(parent, "WelcomeArch");
        float archHalf = RibbonHalfWidth(archS) + 0.7f;
        Vector3 right = SpineRight(archS);
        Quaternion facing = Facing(archS);

        Prim(PrimitiveType.Cube, "Post_L", arch, At(archS, -archHalf) + Vector3.up * 1.45f,
             new Vector3(0.26f, 2.9f, 0.26f), MatWood, collider: true, rot: facing);
        Prim(PrimitiveType.Cube, "Post_R", arch, At(archS, archHalf) + Vector3.up * 1.45f,
             new Vector3(0.26f, 2.9f, 0.26f), MatWood, collider: true, rot: facing);
        Prim(PrimitiveType.Cube, "Beam", arch, SpinePoint(archS) + Vector3.up * 2.95f,
             new Vector3(archHalf * 2f + 0.4f, 0.3f, 0.3f), MatWood, rot: facing);
        Prim(PrimitiveType.Cube, "Sign", arch, SpinePoint(archS) + Vector3.up * 2.55f,
             new Vector3(1.9f, 0.5f, 0.12f), MatYellow, rot: facing);

        MakeFenceRun(parent, "Fence_Left", InArea("StartGarden", 0.45f), InArea("StartGarden", 0.95f), -1f, 1.0f);
        MakeFenceRun(parent, "Fence_Right", InArea("StartGarden", 0.45f), InArea("StartGarden", 0.95f), 1f, 1.0f);

        MakeTree(parent, "Tree_A", At(InArea("StartGarden", 0.25f), -4.0f), 3.6f, 1.2f, MatLeaf, rng);
        MakeTree(parent, "Tree_B", At(InArea("StartGarden", 0.55f), 4.1f), 3.2f, 1.1f, MatLeafAlt, rng);
        MakeTree(parent, "Tree_C", At(InArea("StartGarden", 0.85f), 4.0f), 4.0f, 1.35f, MatLeaf, rng);

        MakeFlowerBed(parent, "FlowerBed_L", At(InArea("StartGarden", 0.7f), -3.4f), 1.8f, 8, rng);
        MakeFlowerBed(parent, "FlowerBed_R", At(InArea("StartGarden", 0.4f), 3.2f), 1.5f, 6, rng);

        // START 뒤쪽 마개를 가리는 수풀 (길 위는 비워 둔다)
        for (int i = -1; i <= 1; i += 2)
        {
            MakeBush(parent, $"BackBush_{i}", At(0.6f, i * 3.4f), 1.8f, MatLeafDeep);
        }
    }

    // ==========================================================
    // 2. 단서 산책길 - 좁게
    // ==========================================================
    private static void BuildClueWalk(Transform parent, Transform markers)
    {
        var rng = new System.Random(202);

        // 그네
        float swingS = InArea("ClueWalk", 0.3f);
        var swing = Child(parent, "Swing");
        Vector3 swingBase = At(swingS, 2.7f);
        Quaternion swingRot = Facing(swingS);
        Vector3 SR(Vector3 local) => swingBase + swingRot * local;

        Prim(PrimitiveType.Cube, "Post_L", swing, SR(new Vector3(-1.4f, 1.3f, 0f)),
             new Vector3(0.18f, 2.6f, 0.18f), MatWood, collider: true, rot: swingRot);
        Prim(PrimitiveType.Cube, "Post_R", swing, SR(new Vector3(1.4f, 1.3f, 0f)),
             new Vector3(0.18f, 2.6f, 0.18f), MatWood, collider: true, rot: swingRot);
        Prim(PrimitiveType.Cube, "TopBar", swing, SR(new Vector3(0f, 2.6f, 0f)),
             new Vector3(3.1f, 0.16f, 0.16f), MatWood, rot: swingRot);
        Prim(PrimitiveType.Cylinder, "Rope_L", swing, SR(new Vector3(-0.45f, 1.85f, 0f)),
             new Vector3(0.04f, 0.72f, 0.04f), MatFence);
        Prim(PrimitiveType.Cylinder, "Rope_R", swing, SR(new Vector3(0.45f, 1.85f, 0f)),
             new Vector3(0.04f, 0.72f, 0.04f), MatFence);
        Prim(PrimitiveType.Cube, "Seat", swing, SR(new Vector3(0f, 1.1f, 0f)),
             new Vector3(1.1f, 0.1f, 0.4f), MatPink, rot: swingRot);
        MakeMarker(markers, "Marker_Clue_01_Swing", At(swingS - 1.2f, 1.9f), MatMarkerClue, 0.34f);
        NoTreeZones.Add(new Vector4(swingBase.x, swingBase.y, swingBase.z, 2.6f));

        // 사진첩이 걸린 나무
        float photoS = InArea("ClueWalk", 0.72f);
        Vector3 photoTree = At(photoS, -2.9f);
        MakeTree(parent, "PhotoTree", photoTree, 5.0f, 1.7f, MatLeaf, rng);
        Prim(PrimitiveType.Cube, "PhotoFrame", parent, photoTree + Vector3.up * 1.7f + SpineRight(photoS) * 0.45f,
             new Vector3(0.7f, 0.9f, 0.08f), MatWhiteFlower, rot: Facing(photoS) * Quaternion.Euler(0f, 35f, 4f));
        MakeMarker(markers, "Marker_Clue_02_PhotoAlbum", At(photoS - 0.8f, -1.8f), MatMarkerClue, 0.34f);

        // 길가 벤치
        float benchS = InArea("ClueWalk", 0.5f);
        Prim(PrimitiveType.Cube, "Bench_Seat", parent, At(benchS, -2.1f) + Vector3.up * 0.45f,
             new Vector3(1.6f, 0.12f, 0.5f), MatWood, collider: true, rot: Facing(benchS));

        MakeFenceRun(parent, "Fence_Left", InArea("ClueWalk", 0.05f), InArea("ClueWalk", 0.5f), -1f, 1.0f);
        MakeFlowerBed(parent, "FlowerBed_A", At(InArea("ClueWalk", 0.15f), 2.4f), 1.4f, 6, rng);
        MakeFlowerBed(parent, "FlowerBed_B", At(InArea("ClueWalk", 0.9f), 2.6f), 1.5f, 7, rng);
        MakeTree(parent, "Tree_A", At(InArea("ClueWalk", 0.95f), -3.0f), 4.0f, 1.3f, MatLeafAlt, rng);
    }

    // ==========================================================
    // 3. 온실 / 연못 - 넓게 열린 정원
    // ==========================================================
    private static void BuildGreenhousePond(Transform parent, Transform markers)
    {
        var rng = new System.Random(303);

        float houseS = InArea("GreenhousePond", 0.28f);
        Vector3 houseOrigin = At(houseS, -6.0f);

        var placeholder = Child(parent, "Greenhouse");
        BuildGreenhouse(placeholder, houseOrigin, Facing(houseS));

        if (Rectangular)
        {
            // 새 공예 온실로 교체한다.
            // 예전 프리미티브 온실은 지우지 않고 꺼 두어서 언제든 다시 켜 비교할 수 있게 남긴다.
            placeholder.gameObject.SetActive(false);
            placeholder.name = "Greenhouse_Placeholder (비활성 - 비교용)";
            BuildCraftGreenhouse(parent, houseS);
        }
        MakeMarker(markers, "Marker_Clue_03_Greenhouse", At(houseS, -2.8f), MatMarkerClue, 0.34f);

        // 온실은 이 구간의 주인공이다 - 경계 조경이 나무로 덮지 못하게 막는다
        NoTreeZones.Add(new Vector4(houseOrigin.x, houseOrigin.y, houseOrigin.z, 4.6f));

        // 연못
        float pondS = InArea("GreenhousePond", 0.55f);
        var pond = Child(parent, "Pond");
        Vector3 pondCenter = At(pondS, 6.0f);
        NoTreeZones.Add(new Vector4(pondCenter.x, pondCenter.y, pondCenter.z, 4.2f));
        Prim(PrimitiveType.Cylinder, "Rim", pond, pondCenter + Vector3.up * 0.02f,
             new Vector3(6.0f, 0.02f, 6.0f), MatStone);
        Prim(PrimitiveType.Cylinder, "Water", pond, pondCenter + Vector3.up * 0.05f,
             new Vector3(5.1f, 0.02f, 5.1f), MatWater);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * (360f / 7f) * Mathf.Deg2Rad;
            Prim(PrimitiveType.Sphere, $"Stone_{i}", pond,
                 pondCenter + new Vector3(Mathf.Cos(angle) * 3.1f, 0.12f, Mathf.Sin(angle) * 3.1f),
                 new Vector3(0.6f, 0.35f, 0.6f), MatStone);
        }
        Prim(PrimitiveType.Cylinder, "LilyPad_A", pond, pondCenter + new Vector3(0.9f, 0.08f, 0.6f),
             new Vector3(0.8f, 0.02f, 0.8f), MatLeafAlt);
        Prim(PrimitiveType.Cylinder, "LilyPad_B", pond, pondCenter + new Vector3(-1.3f, 0.08f, -0.9f),
             new Vector3(0.6f, 0.02f, 0.6f), MatLeafAlt);

        // 길을 가로지르는 개울과 나무 다리
        float bridgeS = InArea("GreenhousePond", 0.86f);
        var bridge = Child(parent, "Bridge");
        Quaternion facing = Facing(bridgeS);
        float ribbon = RibbonHalfWidth(bridgeS);

        Prim(PrimitiveType.Cube, "Stream", bridge, SpinePoint(bridgeS) + Vector3.up * 0.03f,
             new Vector3(CorridorHalfWidth(bridgeS) * 2f - 1f, 0.02f, 1.9f), MatWater, rot: facing);
        Prim(PrimitiveType.Cube, "Deck", bridge, SpinePoint(bridgeS) + Vector3.up * 0.16f,
             new Vector3(ribbon * 2f + 0.4f, 0.16f, 3.0f), MatWood, collider: true, rot: facing);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 railBase = At(bridgeS, (ribbon + 0.15f) * side);
            Prim(PrimitiveType.Cube, $"Rail_{side}", bridge, railBase + Vector3.up * 0.8f,
                 new Vector3(0.1f, 0.1f, 3.0f), MatWood, rot: facing);
            Prim(PrimitiveType.Cube, $"RailPost_A{side}", bridge,
                 At(bridgeS + 1.3f, (ribbon + 0.15f) * side) + Vector3.up * 0.45f,
                 new Vector3(0.14f, 0.9f, 0.14f), MatWood, rot: facing);
            Prim(PrimitiveType.Cube, $"RailPost_B{side}", bridge,
                 At(bridgeS - 1.3f, (ribbon + 0.15f) * side) + Vector3.up * 0.45f,
                 new Vector3(0.14f, 0.9f, 0.14f), MatWood, rot: facing);
        }

        // 넓은 구역이 비어 보이지 않게 채운다
        MakeFlowerBed(parent, "FlowerBed_A", At(InArea("GreenhousePond", 0.12f), 4.2f), 2.2f, 10, rng);
        MakeFlowerBed(parent, "FlowerBed_B", At(InArea("GreenhousePond", 0.42f), -2.9f), 1.8f, 8, rng);
        MakeFlowerBed(parent, "FlowerBed_C", At(InArea("GreenhousePond", 0.7f), -4.4f), 2.0f, 9, rng);
        MakeTree(parent, "Tree_A", At(InArea("GreenhousePond", 0.2f), 7.0f), 4.4f, 1.5f, MatLeaf, rng);
        MakeTree(parent, "Tree_B", At(InArea("GreenhousePond", 0.72f), 7.4f), 4.2f, 1.45f, MatLeafAlt, rng);
        MakeTree(parent, "Tree_C", At(InArea("GreenhousePond", 0.95f), -6.6f), 4.6f, 1.55f, MatLeaf, rng);
        MakeLamp(parent, "Lamp_A", At(InArea("GreenhousePond", 0.45f), 2.8f));
        MakeLamp(parent, "Lamp_B", At(InArea("GreenhousePond", 0.95f), 3.0f));
    }

    private static void BuildGreenhouse(Transform parent, Vector3 origin, Quaternion rot)
    {
        const float width = 6f, depth = 5f, wallHeight = 2.7f;
        Vector3 O(Vector3 local) => origin + rot * local;

        Prim(PrimitiveType.Cube, "Base", parent, O(new Vector3(0f, 0.07f, 0f)),
             new Vector3(width + 0.3f, 0.14f, depth + 0.3f), MatStone, collider: true, rot: rot);

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Prim(PrimitiveType.Cube, $"Post_{sx}_{sz}", parent,
                     O(new Vector3(sx * width * 0.5f, wallHeight * 0.5f, sz * depth * 0.5f)),
                     new Vector3(0.16f, wallHeight, 0.16f), MatWhiteFlower, collider: true, rot: rot);
            }
        }

        // 길 쪽(+X) 은 드나들 수 있게 가운데를 비워 둔다
        Prim(PrimitiveType.Cube, "Glass_Back", parent, O(new Vector3(-width * 0.5f, wallHeight * 0.5f, 0f)),
             new Vector3(0.08f, wallHeight, depth), MatGlass, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Glass_Side_A", parent, O(new Vector3(0f, wallHeight * 0.5f, -depth * 0.5f)),
             new Vector3(width, wallHeight, 0.08f), MatGlass, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Glass_Side_B", parent, O(new Vector3(0f, wallHeight * 0.5f, depth * 0.5f)),
             new Vector3(width, wallHeight, 0.08f), MatGlass, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Glass_Front_A", parent, O(new Vector3(width * 0.5f, wallHeight * 0.5f, -1.75f)),
             new Vector3(0.08f, wallHeight, 1.5f), MatGlass, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Glass_Front_B", parent, O(new Vector3(width * 0.5f, wallHeight * 0.5f, 1.75f)),
             new Vector3(0.08f, wallHeight, 1.5f), MatGlass, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Glass_Front_Top", parent, O(new Vector3(width * 0.5f, wallHeight - 0.35f, 0f)),
             new Vector3(0.08f, 0.7f, 2f), MatGlass, rot: rot);

        Prim(PrimitiveType.Cube, "Roof_A", parent, O(new Vector3(-1.5f, wallHeight + 0.45f, 0f)),
             new Vector3(3.4f, 0.07f, depth + 0.2f), MatGlass, rot: rot * Quaternion.Euler(0f, 0f, -28f));
        Prim(PrimitiveType.Cube, "Roof_B", parent, O(new Vector3(1.5f, wallHeight + 0.45f, 0f)),
             new Vector3(3.4f, 0.07f, depth + 0.2f), MatGlass, rot: rot * Quaternion.Euler(0f, 0f, 28f));
        Prim(PrimitiveType.Cube, "Ridge", parent, O(new Vector3(0f, wallHeight + 1.2f, 0f)),
             new Vector3(0.14f, 0.14f, depth + 0.3f), MatWhiteFlower, rot: rot);

        var rng = new System.Random(3031);
        for (int i = 0; i < 4; i++)
        {
            float z = Lerp(-1.6f, 1.6f, i / 3f);
            Prim(PrimitiveType.Cube, $"Planter_{i}", parent, O(new Vector3(-1.9f, 0.4f, z)),
                 new Vector3(1.4f, 0.55f, 0.8f), MatWood, collider: true, rot: rot);
            MakeFlower(parent, $"PottedFlower_{i}", O(new Vector3(-1.9f, 0.7f, z)), 0.5f,
                       i % 2 == 0 ? MatPink : MatPurple, rng);
        }
    }

    // ==========================================================
    // 4. 정화총 연결길 - 다시 좁게. 경로 약 41m 지점.
    //    "필수 단서 조사 -> 장난감 총이 정화총으로 바뀌는" 이벤트 자리.
    // ==========================================================
    private static void BuildWeaponLink(Transform parent, Transform markers)
    {
        var rng = new System.Random(404);
        float treeS = InArea("WeaponLink", 0.52f);
        Vector3 treeBase = At(treeS, -3.3f);
        Quaternion rot = Facing(treeS);
        Vector3 T(Vector3 local) => treeBase + rot * local;

        // 트리하우스 자체가 큰 나무다 - 주변에 경계 나무를 더 세우면 형태가 묻힌다
        NoTreeZones.Add(new Vector4(treeBase.x, treeBase.y, treeBase.z, 4.2f));

        var treehouse = Child(parent, "Treehouse");
        Prim(PrimitiveType.Cylinder, "Trunk", treehouse, T(new Vector3(0f, 2.1f, 0f)),
             new Vector3(0.9f, 2.1f, 0.9f), MatTrunk, collider: true);
        Prim(PrimitiveType.Cube, "Platform", treehouse, T(new Vector3(0f, 3.05f, 0f)),
             new Vector3(3.6f, 0.2f, 3.6f), MatWood, collider: true, rot: rot);

        for (int side = -1; side <= 1; side += 2)
        {
            Prim(PrimitiveType.Cube, $"Rail_X{side}", treehouse, T(new Vector3(side * 1.75f, 3.55f, 0f)),
                 new Vector3(0.1f, 0.8f, 3.6f), MatWood, rot: rot);
            Prim(PrimitiveType.Cube, $"Rail_Z{side}", treehouse, T(new Vector3(0f, 3.55f, side * 1.75f)),
                 new Vector3(3.6f, 0.8f, 0.1f), MatWood, rot: rot);
        }

        Prim(PrimitiveType.Cube, "Hut", treehouse, T(new Vector3(-0.5f, 3.95f, -0.4f)),
             new Vector3(2.0f, 1.6f, 2.0f), MatWood, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "HutRoof_A", treehouse, T(new Vector3(-1.0f, 4.95f, -0.4f)),
             new Vector3(1.5f, 0.1f, 2.3f), MatPink, rot: rot * Quaternion.Euler(0f, 0f, -30f));
        Prim(PrimitiveType.Cube, "HutRoof_B", treehouse, T(new Vector3(0f, 4.95f, -0.4f)),
             new Vector3(1.5f, 0.1f, 2.3f), MatPink, rot: rot * Quaternion.Euler(0f, 0f, 30f));

        var ladder = Child(treehouse, "Ladder");
        Vector3 ladderTop = T(new Vector3(1.5f, 3.0f, 1.6f));
        Vector3 ladderBottom = T(new Vector3(2.4f, 0f, 2.6f));
        Vector3 ladderMid = (ladderTop + ladderBottom) * 0.5f;
        float ladderLength = Vector3.Distance(ladderTop, ladderBottom);
        Quaternion ladderRot = Quaternion.LookRotation(ladderTop - ladderBottom) * Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Rail_A", ladder, ladderMid + rot * new Vector3(0.28f, 0f, -0.2f),
             new Vector3(0.09f, ladderLength, 0.09f), MatWood, rot: ladderRot);
        Prim(PrimitiveType.Cube, "Rail_B", ladder, ladderMid + rot * new Vector3(-0.28f, 0f, 0.2f),
             new Vector3(0.09f, ladderLength, 0.09f), MatWood, rot: ladderRot);
        for (int i = 0; i < 6; i++)
        {
            Vector3 rung = Vector3.Lerp(ladderBottom, ladderTop, (i + 0.5f) / 6f);
            Prim(PrimitiveType.Cube, $"Rung_{i}", ladder, rung, new Vector3(0.75f, 0.07f, 0.1f), MatWood,
                 rot: rot * Quaternion.Euler(0f, 45f, 0f));
        }

        Prim(PrimitiveType.Sphere, "Canopy_A", treehouse, T(new Vector3(0f, 5.6f, 0f)), Vector3.one * 3.4f, MatLeaf);
        Prim(PrimitiveType.Sphere, "Canopy_B", treehouse, T(new Vector3(-1.6f, 5.1f, 0.9f)), Vector3.one * 2.4f, MatLeafAlt);
        Prim(PrimitiveType.Sphere, "Canopy_C", treehouse, T(new Vector3(1.5f, 5.0f, -1.1f)), Vector3.one * 2.2f, MatLeafDeep);

        if (Rectangular)
        {
            // V1(계단+데크형 큰 집, CraftTreehouse_Playable.prefab)은 최종 디자인으로
            // 채택하지 않는다 - 삭제하지 않고 비활성 상태로만 남긴다.
            treehouse.gameObject.SetActive(false);
            treehouse.name = "Treehouse_Placeholder (비활성 - 비교용)";
            var v1 = BuildCraftTreehousePlayable(parent, InArea("WeaponLink", 0.58f));
            if (v1 != null)
            {
                v1.SetActive(false);
                v1.name = "CraftTreehouse_Playable_V1 (비활성 - 채택 안 함)";
            }

            // V2 - "큰 나무 + 나무에 붙은 작은 비밀 아지트" 블록아웃(프리미티브 전용).
            BuildTreehouseV2Blockout(parent, InArea("WeaponLink", 0.30f));
        }

        // 삼각 깃발 - 기둥은 반드시 길 바깥에
        // Rect 버전은 트리하우스 계단이 fraction 0.88 근처(구간 뒤쪽)까지 차지하므로,
        // 깃대는 트리하우스보다 앞쪽(구간 시작부) 빈 자리로 옮긴다.
        var bunting = Child(parent, "Bunting");
        float postS = InArea("WeaponLink", Rectangular ? 0.08f : 0.88f);
        Vector3 buntingFrom = T(new Vector3(1.8f, 3.5f, 1.8f));
        Vector3 buntingTo = At(postS, 2.9f) + Vector3.up * 2.6f;
        Prim(PrimitiveType.Cube, "Post", bunting, buntingTo + Vector3.down * 1.3f,
             new Vector3(0.12f, 2.6f, 0.12f), MatWood, collider: true);
        for (int i = 1; i < 8; i++)
        {
            float t = i / 8f;
            Vector3 point = Vector3.Lerp(buntingFrom, buntingTo, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.4f;
            Material flagMat = i % 3 == 0 ? MatPink : i % 3 == 1 ? MatYellow : MatPurple;
            Prim(PrimitiveType.Cube, $"Flag_{i}", bunting, point, new Vector3(0.28f, 0.36f, 0.03f), flagMat,
                 rot: Quaternion.Euler(0f, 0f, 45f));
        }

        // Clue + Weapon 이벤트 마커 - 길 바로 옆
        float eventS = InArea("WeaponLink", 0.5f);
        Vector3 pickup = At(eventS, -(RibbonHalfWidth(eventS) + 0.8f));
        var eventMarker = Child(markers, "Marker_ClueWeaponEvent_ToyGunToPurifier");
        eventMarker.position = pickup;
        Prim(PrimitiveType.Cube, "Pedestal", eventMarker, pickup + Vector3.up * 0.25f,
             new Vector3(0.9f, 0.5f, 0.9f), MatStone, collider: true, rot: Facing(eventS));
        Prim(PrimitiveType.Sphere, "ToyGun_ClueGem", eventMarker, pickup + Vector3.up * 0.78f,
             Vector3.one * 0.34f, MatMarkerClue);
        Prim(PrimitiveType.Sphere, "PurifierGem", eventMarker, pickup + Vector3.up * 1.32f,
             Vector3.one * 0.42f, MatMarkerWeapon);
        Prim(PrimitiveType.Cylinder, "Ring", eventMarker, pickup + Vector3.up * 0.03f,
             new Vector3(1.5f, 0.015f, 1.5f), MatMarkerWeapon);
        KeepClear.Add(new Vector4(pickup.x, pickup.y, pickup.z, 2.4f));

        // Rect 버전은 실제 트리하우스 프리팹(계단 포함, 약 7 x 10.6m)이 이 구간 s 범위를
        // 거의 다(약 fraction 0.2 ~ 1.1) 차지해서, 예전 자리(fraction 0.2 / 0.12)에 그대로
        // 두면 계단과 겹친다. 트리하우스가 시작되기 전(구간 맨 앞) 빈 자리로 옮긴다.
        float flowerLamp_t = Rectangular ? 0.02f : 0.2f;
        float lamp_t = Rectangular ? 0.05f : 0.12f;
        MakeFlowerBed(parent, "FlowerBed_A", At(InArea("WeaponLink", flowerLamp_t), 2.6f), 1.5f, 6, rng);
        MakeLamp(parent, "Lamp_A", At(InArea("WeaponLink", lamp_t), 2.4f));
    }

    // ==========================================================
    // 실제 놀이 가능한 트리하우스 (CraftTreehouse_Playable.prefab)
    //
    // 실측 기준
    //   - 콜라이더 기준 바깥 폭 6.99(X) x 6.30(Y) x 10.61(Z), 렌더러 기준(캐노피 포함) 8.45 x 7.25 x 12.35
    //   - 피벗은 트렁크 밑동 지면, 바닥 y = 0
    //   - 계단 시작점 로컬 (-2.9, 0, -5.28), 문 폭 1.6m x 높이 2.35m, 로컬 -Z 쪽
    //
    // WeaponLink 리본 폭은 3m(±1.5), 코리도 폭은 9m(±4.5)뿐이라 이 프리팹의 폭(7m)이 리본보다
    // 훨씬 넓다. 코리도 안에 완전히 들어가는 좌우 위치는 lateral +0.55 하나뿐이고(±3.5m, 양쪽에
    // 여유 1m), 그 자리에서는 트렁크 하나가 리본 한가운데에 걸린다 - 실제 통행로는 리본이 아니라
    // 코리도 전체이므로 걸어서 돌아갈 수 있다. Spine / RibbonHalfWidth / CorridorHalfWidth 는
    // 그대로 두고 이 배치 값만 조정했다.
    // ==========================================================
    private const string TreehousePlayableFbxPath =
        "Assets/BrightDream/Art/CraftTreehouse/CraftTreehouse_Playable.prefab";

    private const float TreehousePlayableLateral = 0.55f;

    private static GameObject BuildCraftTreehousePlayable(Transform parent, float s)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(TreehousePlayableFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 놀이 가능 트리하우스 프리팹을 찾지 못했다 - " + TreehousePlayableFbxPath);
            return null;
        }

        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = "CraftTreehouse_Playable";
        Vector3 pos = At(s, TreehousePlayableLateral);
        go.transform.position = pos;
        // 180도 돌려서 계단 쪽이 구간 뒤쪽(TutorialNook 방향)을 향하게 한다 - 그래야
        // 계단 시작부가 GreenhousePond 다리 반대쪽으로 가서 겹치지 않는다.
        go.transform.rotation = Facing(s) * Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = Vector3.one;

        // 프리팹 몸통이 커서(약 7 x 10.6m), 이후 배경 나무가 겹쳐 심기지 않도록 넉넉히 비워 둔다.
        NoTreeZones.Add(new Vector4(pos.x, pos.y, pos.z, 8.5f));

        return go;
    }

    // ==========================================================
    // 트리하우스 V2 - "큰 나무 + 나무에 얹힌 작은 비밀 아지트" 플레이 블록아웃.
    //
    // V1(계단+데크형 큰 집)과 컨셉이 다르다: 집보다 나무가 커야 하고, 계단이 아니라
    // 몸통을 살짝 감싸며 오르는 짧은 사다리(경사면 + 발판 장식)로 오른다.
    // 아트 제작 없이 Cube/Cylinder/Sphere 프리미티브만 쓴다 - 크기/위치/동선/내부
    // 공간을 먼저 확정하는 단계.
    //
    // 실측: 전체 footprint 약 6.7m, 집 3.6(W) x 3.2(D), 플랫폼 높이 3.0m,
    //       입구 1.5m x 2.3m, 내부 높이 2.5m, 사다리 경사 약 44°(제한 50°보다 안전).
    //
    // 자리 잡기: 트렁크(반지름 0.85) + 사다리가 자유롭게 놓일 자리가 필요해서 여러 후보를
    // 실측 검증했다. WeaponLink 이벤트 마커(fraction 0.5, 건드리면 안 됨)와 왼쪽 낮은
    // 울타리(코리도 반폭 4.5) 사이에 낄 수 있는 좁은 틈이 있어, 그 틈에 맞춰 s=0.30,
    // lateral=-2.8 로 잡고 사다리는 마커도 울타리도 향하지 않는 방향으로만 뻗게 했다.
    // ==========================================================
    private static void BuildTreehouseV2Blockout(Transform parent, float s)
    {
        const float lateral = -2.8f;
        Vector3 origin = At(s, lateral);
        Quaternion rot = Facing(s);
        Vector3 T(Vector3 local) => origin + rot * local;
        Quaternion R(Quaternion local) => rot * local;

        var root = Child(parent, "TreehouseV2_Blockout");
        NoTreeZones.Add(new Vector4(origin.x, origin.y, origin.z, 5.5f));
        KeepClear.Add(new Vector4(origin.x, origin.y, origin.z, 4.5f));

        // ---- 나무 - 집보다 훨씬 커야 한다 ----
        Vector3 trunkXZ = new Vector3(0.3f, 0f, 0f);
        const float trunkRadius = 0.85f, trunkHeight = 5.8f;
        Prim(PrimitiveType.Cylinder, "Trunk", root, T(trunkXZ + Vector3.up * trunkHeight * 0.5f),
             new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f), MatTrunk, collider: true, rot: rot);
        Prim(PrimitiveType.Sphere, "Canopy_A", root, T(trunkXZ + Vector3.up * (trunkHeight - 0.5f)),
             Vector3.one * 3.6f, MatLeaf);
        Prim(PrimitiveType.Sphere, "Canopy_B", root, T(trunkXZ + new Vector3(-1.2f, trunkHeight - 1.0f, 0.9f)),
             Vector3.one * 2.5f, MatLeafAlt);
        Prim(PrimitiveType.Sphere, "Canopy_C", root, T(trunkXZ + new Vector3(1.1f, trunkHeight - 1.3f, -0.8f)),
             Vector3.one * 2.2f, MatLeafDeep);

        // ---- 나무 밑동 작은 접근 공간(시각 전용) ----
        Vector3 rampBottom = new Vector3(-0.3f, 0f, 3.4f);
        Vector3 rampMid = new Vector3(0.1f, 1.5f, 4.9f);
        Vector3 rampTop = new Vector3(-0.2f, 3.0f, 6.4f);
        Prim(PrimitiveType.Cylinder, "ApproachPad", root, T(new Vector3(rampBottom.x, 0.015f, rampBottom.z - 0.9f)),
             new Vector3(2.2f, 0.015f, 2.2f), MatPathStepIvory);

        // ---- 몸통을 살짝 감싸며 오르는 짧은 사다리(경사로 콜라이더 + 장식 발판) ----
        // slopeLimit(50도)보다 낮은 약 44도라 CharacterController가 그대로 걸어 올라간다.
        var ladder = Child(root, "WrapLadder");
        System.Action<Vector3, Vector3, string, int> BuildRampSegment = (from, to, name, rungCount) =>
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            Vector3 mid = (from + to) * 0.5f;
            Quaternion segRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Prim(PrimitiveType.Cube, name, ladder, T(mid), new Vector3(1.2f, 0.12f, length), MatWood,
                 collider: true, rot: R(segRot));
            for (int i = 1; i <= rungCount; i++)
            {
                float t = i / (float)(rungCount + 1);
                Vector3 p = Vector3.Lerp(from, to, t) + Vector3.up * 0.08f;
                Prim(PrimitiveType.Cube, $"{name}_Rung_{i}", ladder, T(p), new Vector3(1.2f, 0.05f, 0.09f),
                     MatTrunk, rot: R(segRot));
            }
        };
        BuildRampSegment(rampBottom, rampMid, "Ramp_1", 5);
        BuildRampSegment(rampMid, rampTop, "Ramp_2", 5);
        Prim(PrimitiveType.Cube, "MidLanding", ladder, T(rampMid), new Vector3(1.3f, 0.12f, 1.3f), MatWood, collider: true);
        // 난간은 최소한으로 - 계단참과 도착 지점 바깥쪽에 기둥 하나씩만
        Prim(PrimitiveType.Cube, "RailPost_Mid", ladder, T(rampMid + new Vector3(0.6f, 0.5f, 0f)),
             new Vector3(0.08f, 1.0f, 0.08f), MatWood);
        Prim(PrimitiveType.Cube, "RailPost_Top", ladder, T(rampTop + new Vector3(0.6f, 0.5f, 0f)),
             new Vector3(0.08f, 1.0f, 0.08f), MatWood);

        // ---- 작은 플랫폼 ----
        const float platformTopY = 3.0f;
        Prim(PrimitiveType.Cube, "Platform", root, T(new Vector3(0.3f, platformTopY - 0.1f, 0.5f)),
             new Vector3(3.7f, 0.2f, 3.6f), MatWood, collider: true, rot: rot);

        // ---- 작은 비밀 아지트(집) ----
        const float houseWidth = 3.6f, houseDepth = 3.2f, interiorHeight = 2.5f;
        Vector3 houseCenterXZ = new Vector3(0.3f, 0f, 0.2f);
        float wallCenterY = platformTopY + interiorHeight * 0.5f;
        float halfW = houseWidth * 0.5f, halfD = houseDepth * 0.5f;
        const float doorWidth = 1.5f, doorHeight = 2.3f, wallThick = 0.14f;
        float doorHalf = doorWidth * 0.5f;
        float frontZ = houseCenterXZ.z + halfD; // 사다리가 도착하는 +Z 쪽에 입구
        float sideW = halfW - doorHalf, sideC = doorHalf + sideW * 0.5f;

        Prim(PrimitiveType.Cube, "Wall_Front_L", root, T(new Vector3(houseCenterXZ.x - sideC, wallCenterY, frontZ)),
             new Vector3(sideW, interiorHeight, wallThick), MatPathButter, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Front_R", root, T(new Vector3(houseCenterXZ.x + sideC, wallCenterY, frontZ)),
             new Vector3(sideW, interiorHeight, wallThick), MatPathButter, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Door_Lintel", root,
             T(new Vector3(houseCenterXZ.x, platformTopY + doorHeight + (interiorHeight - doorHeight) * 0.5f, frontZ)),
             new Vector3(doorWidth, interiorHeight - doorHeight, wallThick), MatPathButter, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Back", root, T(new Vector3(houseCenterXZ.x, wallCenterY, houseCenterXZ.z - halfD)),
             new Vector3(houseWidth, interiorHeight, wallThick), MatPathButter, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Left", root, T(new Vector3(houseCenterXZ.x - halfW, wallCenterY, houseCenterXZ.z)),
             new Vector3(wallThick, interiorHeight, houseDepth), MatPathButter, collider: true, rot: rot);

        // 오른쪽(+X) 벽 - 정원이 보이는 작은 창문
        const float winSize = 0.85f;
        float winCenterZ = houseCenterXZ.z + halfD * 0.4f;
        float winHalf = winSize * 0.5f;
        float frontSegD = (houseCenterXZ.z + halfD) - (winCenterZ + winHalf);
        float backSegD = (winCenterZ - winHalf) - (houseCenterXZ.z - halfD);
        Prim(PrimitiveType.Cube, "Wall_Right_Front", root,
             T(new Vector3(houseCenterXZ.x + halfW, wallCenterY, houseCenterXZ.z + halfD - frontSegD * 0.5f)),
             new Vector3(wallThick, interiorHeight, frontSegD), MatPathButter, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Right_Back", root,
             T(new Vector3(houseCenterXZ.x + halfW, wallCenterY, houseCenterXZ.z - halfD + backSegD * 0.5f)),
             new Vector3(wallThick, interiorHeight, backSegD), MatPathButter, collider: true, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Right_Sill", root,
             T(new Vector3(houseCenterXZ.x + halfW, platformTopY + (interiorHeight - winSize) * 0.3f, winCenterZ)),
             new Vector3(wallThick, (interiorHeight - winSize) * 0.6f, winSize), MatPathButter, rot: rot);
        Prim(PrimitiveType.Cube, "Wall_Right_Head", root,
             T(new Vector3(houseCenterXZ.x + halfW, wallCenterY + interiorHeight * 0.5f - (interiorHeight - winSize) * 0.15f, winCenterZ)),
             new Vector3(wallThick, (interiorHeight - winSize) * 0.4f, winSize), MatPathButter, rot: rot);
        Prim(PrimitiveType.Cube, "Window_Glass", root,
             T(new Vector3(houseCenterXZ.x + halfW, platformTopY + interiorHeight * 0.5f, winCenterZ)),
             new Vector3(0.05f, winSize, winSize), MatGlass, rot: rot);

        // ---- 지붕 ----
        float wallTopY = platformTopY + interiorHeight;
        Prim(PrimitiveType.Cube, "Roof_A", root, T(new Vector3(houseCenterXZ.x - halfW * 0.5f, wallTopY + 0.45f, houseCenterXZ.z)),
             new Vector3(halfW + 0.3f, 0.08f, houseDepth + 0.4f), MatWood, rot: rot * Quaternion.Euler(0f, 0f, -24f));
        Prim(PrimitiveType.Cube, "Roof_B", root, T(new Vector3(houseCenterXZ.x + halfW * 0.5f, wallTopY + 0.45f, houseCenterXZ.z)),
             new Vector3(halfW + 0.3f, 0.08f, houseDepth + 0.4f), MatWood, rot: rot * Quaternion.Euler(0f, 0f, 24f));
    }

    // ==========================================================
    // 5. 튜토리얼 공터 - 정화총 획득 직후
    // ==========================================================
    private static void BuildTutorialNook(Transform parent, Transform markers)
    {
        var rng = new System.Random(450);
        float nookS = InArea("TutorialNook", 0.5f);

        Prim(PrimitiveType.Cylinder, "TutorialGround", parent, SpinePoint(nookS) + Vector3.up * 0.012f,
             new Vector3(9f, 0.02f, 9f), MatSand);

        Vector3 spawn = At(nookS, 2.6f);
        var marker = Child(markers, "Marker_EnemyTutorial_01");
        marker.position = spawn;
        Prim(PrimitiveType.Capsule, "EnemyPlaceholder", marker, spawn + Vector3.up * 0.9f,
             new Vector3(0.8f, 0.85f, 0.8f), MatEnemy);
        Prim(PrimitiveType.Cylinder, "SpawnRing", marker, spawn + Vector3.up * 0.04f,
             new Vector3(1.6f, 0.02f, 1.6f), MatMarkerEnemy);
        KeepClear.Add(new Vector4(spawn.x, spawn.y, spawn.z, 2.0f));

        MakeCrate(parent, "Crate_A", At(InArea("TutorialNook", 0.75f), -3.4f), 0.9f, 22f);
        MakeFlowerBed(parent, "FlowerBed_A", At(InArea("TutorialNook", 0.2f), -3.6f), 1.5f, 6, rng);
        MakeTree(parent, "Tree_A", At(InArea("TutorialNook", 0.9f), 4.6f), 4.0f, 1.4f, MatLeafAlt, rng);
    }

    // ==========================================================
    // 6. 전투장 - 가장 크게 열리는 구간
    // ==========================================================
    private static void BuildCombatArena(Transform parent, Transform markers)
    {
        var rng = new System.Random(505);

        // 예전에는 반지름 18m 짜리 모래 바닥을 구간 안에 6장 깔았는데, 반지름이 전투장
        // 길이보다 훨씬 커서 6장이 사실상 같은 자리에 완전히 겹쳐 있었다(같은 높이 코인시던스로
        // Z-fighting 발생). 어차피 한 장만으로도 전투장 전체를 덮으므로 중앙 한 장만 남긴다.
        float arenaGroundS = InArea("CombatArena", 0.5f);
        Prim(PrimitiveType.Cylinder, "ArenaGround", parent, SpinePoint(arenaGroundS) + Vector3.up * 0.012f,
             new Vector3(18f, 0.02f, 18f), MatSand);

        (float t, float lateral)[] spawns =
        {
            (0.22f, -4.6f),
            (0.5f, 5.0f),
            (0.78f, -4.0f),
        };
        for (int i = 0; i < spawns.Length; i++)
        {
            Vector3 spot = At(InArea("CombatArena", spawns[i].t), spawns[i].lateral);
            var spawn = Child(markers, $"Marker_EnemySpawn_{i + 1:D2}");
            spawn.position = spot;
            Prim(PrimitiveType.Capsule, "EnemyPlaceholder", spawn, spot + Vector3.up * 0.9f,
                 new Vector3(0.8f, 0.85f, 0.8f), MatEnemy);
            Prim(PrimitiveType.Cylinder, "SpawnRing", spawn, spot + Vector3.up * 0.04f,
                 new Vector3(1.6f, 0.02f, 1.6f), MatMarkerEnemy);
        }

        // 눈높이보다 낮은 엄폐물
        MakeCrate(parent, "Crate_A", At(InArea("CombatArena", 0.3f), -7.4f), 1.0f, 18f);
        MakeCrate(parent, "Crate_B", At(InArea("CombatArena", 0.34f), -8.0f), 0.8f, -25f);
        MakeCrate(parent, "Crate_C", At(InArea("CombatArena", 0.62f), 7.6f), 1.0f, -12f);
        MakeCrate(parent, "Crate_D", At(InArea("CombatArena", 0.18f), 7.0f), 0.9f, 32f);
        MakeCrate(parent, "Crate_Mid_A", At(InArea("CombatArena", 0.4f), -2.0f), 0.9f, -20f);
        MakeCrate(parent, "Crate_Mid_B", At(InArea("CombatArena", 0.68f), 2.4f), 0.8f, 15f);

        for (int i = 0; i < 3; i++)
        {
            float t = Lerp(0.2f, 0.8f, (float)rng.NextDouble());
            float lateral = Lerp(-6f, 6f, (float)rng.NextDouble());
            Prim(PrimitiveType.Cylinder, $"Stump_{i}", parent, At(InArea("CombatArena", t), lateral) + Vector3.up * 0.22f,
                 new Vector3(0.7f, 0.22f, 0.7f), MatTrunk, collider: true);
        }

        // 넓은 마당 가장자리 조경.
        // 예전에는 양옆으로 5 그루씩 늘어세워 전투장이 숲속 공터처럼 보였다.
        // 지금은 네 귀퉁이 포인트로만 나무를 두고, 나머지는 낮은 수풀 / 꽃밭이 채운다.
        MakeTree(parent, "Tree_L0", At(InArea("CombatArena", 0.12f), -10.6f), 4.6f, 1.5f, MatLeaf, rng);
        MakeTree(parent, "Tree_L1", At(InArea("CombatArena", 0.86f), -10.2f), 4.3f, 1.4f, MatLeafAlt, rng);
        MakeTree(parent, "Tree_R0", At(InArea("CombatArena", 0.30f), 10.8f), 4.8f, 1.55f, MatLeafDeep, rng);
        MakeTree(parent, "Tree_R1", At(InArea("CombatArena", 0.72f), 10.4f), 4.4f, 1.45f, MatLeaf, rng);

        for (int i = 0; i < 6; i++)
        {
            float t = Lerp(0.10f, 0.90f, i / 5f);
            MakeBush(parent, $"EdgeBush_L{i}", At(InArea("CombatArena", t), -9.4f),
                     Lerp(0.9f, 1.5f, (float)rng.NextDouble()), i % 2 == 0 ? MatLeafSoft : MatLeafAlt);
            MakeBush(parent, $"EdgeBush_R{i}", At(InArea("CombatArena", t + 0.05f), 9.6f),
                     Lerp(0.9f, 1.5f, (float)rng.NextDouble()), i % 2 == 0 ? MatLeafAlt : MatLeafSoft);
        }

        for (int i = 0; i < 3; i++)
        {
            float t = Lerp(0.2f, 0.8f, i / 2f);
            MakeFlowerBed(parent, $"EdgeBed_L{i}", At(InArea("CombatArena", t), -8.0f), 1.7f, 9, rng);
            MakeFlowerBed(parent, $"EdgeBed_R{i}", At(InArea("CombatArena", t + 0.08f), 8.2f), 1.5f, 8, rng);
        }
    }

    // ==========================================================
    // 7. 유니콘 접근로 - 가장 좁고, 장식이 점점 빽빽해진다
    // ==========================================================
    private static void BuildUnicornApproach(Transform parent, Transform markers)
    {
        var rng = new System.Random(606);
        float a = AreaStartS("UnicornApproach"), b = AreaEndS("UnicornApproach");

        for (float s = a + 0.8f; s < b; s += 1.1f)
        {
            float density = Mathf.InverseLerp(a, b, s);
            float ribbon = RibbonHalfWidth(s);

            for (int side = -1; side <= 1; side += 2)
            {
                int count = Mathf.RoundToInt(Lerp(2f, 7f, density));
                MakeFlowerBed(parent, $"Bed_{s:F0}_{side}", At(s, (ribbon + 0.75f) * side),
                              Lerp(0.6f, 1.0f, density), count, rng);

                if ((float)rng.NextDouble() < density * 0.75f)
                {
                    MakeStakeStar(parent, $"Star_{s:F0}_{side}", At(s, (ribbon + 0.45f) * side),
                                  Lerp(1.3f, 2.1f, density));
                }
            }

            if ((float)rng.NextDouble() < density * 0.5f)
            {
                MakeLamp(parent, $"Lamp_{s:F0}", At(s, ribbon + 1.9f));
            }
        }

        // 꽃 아치 - 마지막 구역으로 들어가는 문
        float archS = InArea("UnicornApproach", 0.45f);
        var arch = Child(parent, "FlowerArch");
        Quaternion facing = Facing(archS);
        float archHalf = RibbonHalfWidth(archS) + 0.6f;

        Prim(PrimitiveType.Cube, "Post_L", arch, At(archS, -archHalf) + Vector3.up * 1.4f,
             new Vector3(0.2f, 2.8f, 0.2f), MatFence, collider: true, rot: facing);
        Prim(PrimitiveType.Cube, "Post_R", arch, At(archS, archHalf) + Vector3.up * 1.4f,
             new Vector3(0.2f, 2.8f, 0.2f), MatFence, collider: true, rot: facing);
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            Vector3 point = At(archS, Mathf.Lerp(-archHalf, archHalf, t))
                            + Vector3.up * (2.8f + Mathf.Sin(t * Mathf.PI) * 0.55f);
            Prim(PrimitiveType.Sphere, $"ArchFlower_{i}", arch, point, Vector3.one * 0.42f,
                 i % 3 == 0 ? MatPink : i % 3 == 1 ? MatWhiteFlower : MatPurple);
        }

        BuildPlazaGateMobile(Child(parent, "PlazaGateMobile"), rng);
    }

    /// <summary>
    /// 유니콘 광장 입구에 매달린 공예 모빌.
    ///
    /// 털실 뭉치와 솜뭉치, 종이별을 천장에서 늘어뜨린 커튼이다.
    /// 아이가 방문에 걸어 둔 모빌을 헤치고 들어가는 느낌을 내면서,
    /// 마지막 굴곡을 돌자마자 유니콘이 통째로 보여 버리는 것을 자연스럽게 늦춘다.
    ///
    /// 가닥은 중심선 바로 위를 비우고 좌우로만 내려온다.
    /// 길 한가운데 0.45m 반경은 그대로 열려 있어야 통행 검사를 통과한다.
    /// </summary>
    private static void BuildPlazaGateMobile(Transform parent, System.Random rng)
    {
        float b = AreaEndS("UnicornApproach");

        // s = 광장 직전, lateral = 길 좌우.
        // 길 한가운데(|lateral| < 1.3m)는 비워 둔다 - 걸어 들어가는 통로이자 통행 검사 구간이다.
        //
        // 예전에는 바깥쪽(|lateral| 2.0~2.8m)까지 가닥을 늘어뜨려 놀이공원 입구 아치처럼 보였다.
        // 지금은 길 양옆 한 줄씩만 남긴다. 유니콘을 가려 주는 것도 이 안쪽 가닥들이다.
        (float S, float Lateral, float Low)[] strands =
        {
            (b - 5.6f, -1.45f, 1.80f),
            (b - 5.6f,  1.45f, 1.80f),
            (b - 4.8f, -1.35f, 1.75f),
            (b - 4.8f,  1.35f, 1.75f),
            (b - 4.0f, -1.55f, 1.75f),
            (b - 4.0f,  1.55f, 1.75f),
            (b - 3.2f, -1.70f, 1.90f),
            (b - 3.2f,  1.40f, 1.80f),
            (b - 2.4f, -1.50f, 1.85f),
            (b - 2.4f,  1.50f, 1.85f),
        };

        int index = 0;
        foreach (var strand in strands)
        {
            Vector3 anchor = At(strand.S, strand.Lateral);
            var mobile = Child(parent, $"Mobile_{index:D2}");

            // 솜뭉치와 종이 구슬을 아래에서 위로 두세 개 꿴다.
            // 알록달록한 털실 대신 솜 / 종이 위주로 두어야 과하지 않다.
            float y = strand.Low;
            int beads = 2 + rng.Next(2);
            for (int i = 0; i < beads; i++)
            {
                float radius = Lerp(0.34f, 0.22f, beads == 1 ? 0f : i / (float)(beads - 1));
                Material mat = ((index + i) % 5) switch
                {
                    0 => MatPink,          // 분홍은 가끔 한 알만 - 포인트
                    1 => MatCloud,
                    2 => MatWhiteFlower,
                    3 => MatCloud,
                    _ => MatWhiteFlower,
                };
                // 길 가장자리에 걸린 가닥만 실제 부피를 갖는다.
                // 덕분에 마지막 굴곡을 돌아도 모빌 너머 유니콘이 아직 통째로 보이지는 않는다.
                Prim(PrimitiveType.Sphere, $"Bead_{i}", mobile, anchor + Vector3.up * y,
                     Vector3.one * radius * 2f, mat, collider: i < 2);
                y += radius + 0.42f;
            }

            // 맨 위 종이별 + 천장까지 이어지는 실
            Prim(PrimitiveType.Cube, "StarBlade_A", mobile, anchor + Vector3.up * (y + 0.1f),
                 new Vector3(0.52f, 0.14f, 0.04f), MatStar);
            Prim(PrimitiveType.Cube, "StarBlade_B", mobile, anchor + Vector3.up * (y + 0.1f),
                 new Vector3(0.14f, 0.52f, 0.04f), MatStar);
            MakeString(mobile, "String", anchor + Vector3.up * (y + 0.1f));

            index++;
        }
    }

    // ==========================================================
    // 8. 유니콘 광장 - 크게 열리며 끝난다
    // ==========================================================
    private static void BuildUnicornPlaza(Transform parent, Transform markers)
    {
        var rng = new System.Random(707);
        Vector3 center = SpinePoint(UnicornS);
        // Rect 버전은 광장이 좁고 답답하다는 피드백으로 바닥/링 장식 반지름을 키운다(자유형은 그대로).
        float radius = Rectangular ? 11.5f : 9.5f;

        // 가장자리만 톤을 살짝 바꾸고, 중앙(Inner + Core)은 하나의 크림색으로 통일해 "최대한 깨끗하게" 읽히게 한다
        Prim(PrimitiveType.Cylinder, "Plaza_Outer", parent, center + Vector3.up * 0.015f,
             new Vector3(radius * 2f, 0.015f, radius * 2f), MatPathIvory);
        Prim(PrimitiveType.Cylinder, "Plaza_Mid", parent, center + Vector3.up * 0.03f,
             new Vector3(radius * 1.5f, 0.015f, radius * 1.5f), MatPathBeige);
        Prim(PrimitiveType.Cylinder, "Plaza_Inner", parent, center + Vector3.up * 0.045f,
             new Vector3(radius * 1.0f, 0.015f, radius * 1.0f), MatPathCream);
        Prim(PrimitiveType.Cylinder, "Plaza_Core", parent, center + Vector3.up * 0.06f,
             new Vector3(radius * 0.5f, 0.015f, radius * 0.5f), MatPathCream);

        BuildUnicorn(Child(parent, "Unicorn_Placeholder"), center, Facing(UnicornS));
        MakeMarker(markers, "Marker_Unicorn_Boss", center, MatMarkerDream, 0.5f);
        MakeMarker(markers, "Marker_DreamTransition_To2D", At(UnicornS - 2.6f, 0f), MatMarkerDream, 0.35f);

        // 뒤쪽의 특별한 큰 나무 (최종 아트 Hero Tree 자리)
        // Rect 버전은 광장을 넓히면서 이 나무가 Cap_End(마개)와 붙어 광장 바닥 위로
        // 초록 판이 걸쳐 보이는 문제가 있었다. Cap_End는 아예 없앴고, 이 나무도 사용자
        // 요청대로 Rect 에서는 꺼 둔다(지우지 않고 비활성화 - 자유형은 그대로 유지).
        var bigTree = Child(parent, "BigTree_Placeholder");
        Vector3 treeBase = SpinePoint(UnicornS + 4.6f);
        Prim(PrimitiveType.Cylinder, "Trunk", bigTree, treeBase + Vector3.up * 2.2f,
             new Vector3(1.5f, 2.2f, 1.5f), MatTrunk, collider: true);
        Prim(PrimitiveType.Sphere, "Canopy_A", bigTree, treeBase + new Vector3(0f, 5.5f, 0f), Vector3.one * 3.8f, MatLeaf);
        Prim(PrimitiveType.Sphere, "Canopy_B", bigTree, treeBase + new Vector3(-2.3f, 4.8f, 0.4f), Vector3.one * 2.8f, MatLeafAlt);
        Prim(PrimitiveType.Sphere, "Canopy_C", bigTree, treeBase + new Vector3(2.4f, 4.9f, -0.3f), Vector3.one * 2.9f, MatLeafDeep);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            MakeStakeStar(bigTree, $"HangingStar_{i}",
                          treeBase + new Vector3(Mathf.Cos(angle) * 2.8f, 0f, Mathf.Sin(angle) * 1.6f), 2.4f);
        }
        if (Rectangular)
        {
            bigTree.gameObject.SetActive(false);
            bigTree.name = "BigTree_Placeholder (비활성 - 광장 확장으로 제외)";
        }

        // 광장을 넓히면서(Rect 버전) UnicornApproach 의 마지막 굴곡이 광장 뒤쪽과 가까이
        // 지나가는 자리와 테두리 장식이 실제로 겹치는 경우가 생겼다(자유형은 광장이 작아서
        // 문제 없음). Spine 이나 굴곡 자체는 손대지 않고, 장식 쪽에서 그 근처 스파인까지
        // 실제 거리를 재서 너무 가까우면 그 장식 하나만 건너뛴다.
        var spineCheckSamples = new List<Vector3>();
        if (Rectangular)
        {
            for (float cs = AreaStartS("CombatArena"); cs <= SpineTotalLength; cs += 0.5f)
                spineCheckSamples.Add(SpinePoint(cs));
        }
        const float spineSafeMargin = 2.6f;
        System.Func<Vector3, bool> tooCloseToSpine = (p) =>
        {
            foreach (var sp in spineCheckSamples)
                if ((sp - p).sqrMagnitude < spineSafeMargin * spineSafeMargin) return true;
            return false;
        };

        // 광장 테두리 - 경계 나무(벽 근처)에 묻히지 않게 광장 안쪽 가장자리에 둔다
        float ringRadius = radius - 2.4f;
        for (int i = 0; i < 20; i++)
        {
            float angle = i * (360f / 20f) * Mathf.Deg2Rad;
            Vector3 ring = center + new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
            if (IsKeptClear(ring)) continue;
            if (Vector3.Distance(ring, SpinePoint(UnicornS - 6f)) < 4.0f) continue;   // 입구는 비워 둔다
            if (tooCloseToSpine(ring)) continue;

            MakeFlowerBed(parent, $"RingBed_{i:D2}", ring, 0.85f, 6, rng);
            Vector3 lampSpot = ring + (ring - center).normalized * 1.4f;
            Vector3 starSpot = ring + (ring - center).normalized * 1.2f;
            if (i % 4 == 0 && !tooCloseToSpine(lampSpot)) MakeLamp(parent, $"RingLamp_{i:D2}", lampSpot);
            if (i % 4 == 2 && !tooCloseToSpine(starSpot)) MakeStakeStar(parent, $"RingStar_{i:D2}", starSpot, 2.0f);
        }

        // 바닥 단이 밋밋해 보이지 않게 낮은 디딤돌 테두리를 둘러 준다.
        // 예전에는 크기와 회전이 전부 똑같아 돌담처럼 보였다 - 손으로 자른 종이판처럼 조금씩 다르게 흔든다.
        for (int i = 0; i < 26; i++)
        {
            float angle = i * (360f / 26f) * Mathf.Deg2Rad;
            Vector3 spot = center + new Vector3(Mathf.Cos(angle) * (radius * 0.52f), 0.09f, Mathf.Sin(angle) * (radius * 0.52f));
            if (IsKeptClear(spot)) continue;
            if (tooCloseToSpine(spot)) continue;

            float sizeJitter = Lerp(0.85f, 1.15f, (float)rng.NextDouble());
            float rotJitter = Lerp(-9f, 9f, (float)rng.NextDouble());
            Prim(PrimitiveType.Cube, $"PlazaKerb_{i:D2}", parent, spot,
                 new Vector3(0.7f * sizeJitter, 0.16f, 0.5f * sizeJitter),
                 MatPathStepIvory, rot: Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + rotJitter, 0f));
        }
    }

    private static void BuildUnicorn(Transform parent, Vector3 center, Quaternion pathRot)
    {
        // 플레이어가 걸어오는 쪽을 바라보게 180도 돌린다
        Quaternion rot = pathRot * Quaternion.Euler(0f, 180f, 0f);
        const float scale = 1.25f;
        Vector3 U(Vector3 local) => center + rot * local;

        Prim(PrimitiveType.Capsule, "Body", parent, U(new Vector3(0f, 1.6f, 0f)),
             new Vector3(1.35f, 1.25f, 1.35f), MatUnicorn, rot: rot * Quaternion.Euler(90f, 0f, 0f));
        Prim(PrimitiveType.Capsule, "Neck", parent, U(new Vector3(0f, 2.3f, 0.8f)),
             new Vector3(0.62f, 0.6f, 0.62f), MatUnicorn, rot: rot * Quaternion.Euler(-38f, 0f, 0f));
        Prim(PrimitiveType.Sphere, "Head", parent, U(new Vector3(0f, 2.94f, 1.42f)),
             new Vector3(0.7f, 0.68f, 1.05f), MatUnicorn, rot: rot);
        Prim(PrimitiveType.Sphere, "Muzzle", parent, U(new Vector3(0f, 2.8f, 1.9f)),
             new Vector3(0.47f, 0.43f, 0.47f), MatUnicorn, rot: rot);
        Prim(PrimitiveType.Sphere, "Eye_L", parent, U(new Vector3(-0.28f, 3.06f, 1.72f)), Vector3.one * 0.15f, MatEnemy);
        Prim(PrimitiveType.Sphere, "Eye_R", parent, U(new Vector3(0.28f, 3.06f, 1.72f)), Vector3.one * 0.15f, MatEnemy);

        for (int i = 0; i < 4; i++)
        {
            float t = i / 4f;
            Prim(PrimitiveType.Cylinder, $"Horn_{i}", parent,
                 U(new Vector3(0f, 3.38f + i * 0.26f, 1.58f - i * 0.05f)),
                 new Vector3(0.19f - t * 0.05f, 0.14f, 0.19f - t * 0.05f), MatGold,
                 rot: rot * Quaternion.Euler(-12f, 0f, 0f));
        }

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Prim(PrimitiveType.Cylinder, $"Leg_{sx}_{sz}", parent,
                     U(new Vector3(sx * 0.48f, 0.5f, sz * 0.7f)),
                     new Vector3(0.25f, 0.5f, 0.25f), MatUnicorn, collider: true);
            }
        }

        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            Prim(PrimitiveType.Sphere, $"Mane_{i}", parent,
                 U(new Vector3(0f, 2.22f + t * 1.05f, 0.5f + t * 0.8f)),
                 Vector3.one * Lerp(0.58f, 0.38f, t) * scale, MatMane);
        }
        for (int i = 0; i < 4; i++)
        {
            float t = i / 3f;
            Prim(PrimitiveType.Sphere, $"Tail_{i}", parent,
                 U(new Vector3(0f, 1.9f - t * 0.75f, -1.2f - t * 0.25f)),
                 Vector3.one * Lerp(0.55f, 0.34f, t) * scale, MatMane);
        }
    }

    // ==========================================================
    // 경계 조경
    //
    // 예전에는 2.6m 마다 양옆에 한 그루씩 세웠다.
    // 그러면 나무가 벽처럼 이어져서 "어린 여자아이의 꿈속 정원" 이 아니라 "빽빽한 숲길" 로 보인다.
    //
    // 지금은 나무를 군락으로만 세운다.
    //   - 한 번에 한쪽 옆에만 (반대쪽은 항상 열려 있다)
    //   - 2~3 그루씩 묶어서
    //   - 군락 사이는 낮은 조경으로 비운다
    //   - 유니콘 접근로부터는 경계 나무를 아예 놓지 않는다
    //
    // 시야는 복도 벽과 맵 굴곡이 이미 막아 주기 때문에 나무로 가릴 필요가 없다.
    // 비워진 자리는 종이 언덕 / 낮은 수풀 / 꽃밭 / 울타리 / 열린 잔디밭이 대신 채운다.
    // ==========================================================

    /// <summary>나무 군락. F = 전체 이동거리에 대한 비율, Side = -1 왼쪽 / +1 오른쪽, Count = 그루 수.</summary>
    private static readonly (float F, float Side, int Count)[] TreeClusters =
    {
        // 시작 정원 (폭 11m)
        (0.040f, -1f, 4),
        (0.080f,  1f, 4),
        (0.120f, -1f, 3),

        // 온실 / 연못 (폭 20m) - 가장 넓어서 나무를 길에서 멀찍이 물릴 수 있다
        (0.298f, -1f, 5),
        (0.340f,  1f, 5),
        (0.385f, -1f, 5),
        (0.420f,  1f, 4),

        // 튜토리얼 공터 (폭 13m)
        (0.572f,  1f, 4),
        (0.612f, -1f, 4),

        // 전투장 (폭 26m)
        (0.660f,  1f, 5),
        (0.705f, -1f, 5),
        (0.750f,  1f, 4),
        (0.790f, -1f, 3),

        // 단서 산책길 / 정화총 연결길 / 유니콘 접근로는 폭이 좁다.
        // 좁은 구간에 나무를 세우면 길 양옆에 딱 붙어 다시 가로수길이 되므로
        // 그 구간은 낮은 수풀 / 꽃 / 울타리만으로 꾸민다.
    };

    /// <summary>열린 잔디밭 가장자리를 정원답게 정리해 주는 낮은 울타리.</summary>
    private static readonly (float From, float To, float Side, float Offset)[] MeadowFences =
    {
        (0.078f, 0.112f, -1f, 2.7f),
        (0.160f, 0.192f,  1f, 2.5f),
        (0.300f, 0.338f, -1f, 3.4f),
        (0.372f, 0.404f,  1f, 2.8f),
        (0.448f, 0.478f, -1f, 2.6f),
        (0.516f, 0.548f,  1f, 3.0f),
        (0.590f, 0.622f, -1f, 3.6f),
        (0.730f, 0.762f,  1f, 2.4f),
    };

    private static void BuildBoundaryLandscaping(Transform parent)
    {
        var rng = new System.Random(808);
        var left = Child(parent, "Boundary_Left");
        var right = Child(parent, "Boundary_Right");
        var meadow = Child(parent, "Meadow");
        var fences = Child(parent, "MeadowFences");

        float w = WalkLength;
        var treeSpots = new List<Vector2>();       // x = s, y = side
        var treePositions = new List<Vector3>();   // 겹침 검사용 월드 좌표

        // ---------- 1. 나무 군락 ----------
        int treeIndex = 0;
        foreach (var cluster in TreeClusters)
        {
            float baseS = cluster.F * w;
            Transform target = cluster.Side < 0f ? left : right;

            for (int i = 0; i < cluster.Count; i++)
            {
                // 한 줄로 세우면 다시 울타리처럼 보인다. 앞뒤 + 안팎으로 흩어 작은 숲무리를 만든다.
                float spread = cluster.Count == 1 ? 0.5f : i / (float)(cluster.Count - 1);
                float s = baseS + Lerp(-2.8f, 2.8f, spread) + Lerp(-0.5f, 0.5f, (float)rng.NextDouble());
                if (s < 1.5f || s > SpineTotalLength - 2f) continue;

                // 벽에 바짝 붙은 그루와 한 발 앞으로 나온 그루를 섞는다
                float depth = i % 2 == 0 ? Lerp(1.1f, 1.9f, (float)rng.NextDouble())
                                         : Lerp(2.4f, 3.4f, (float)rng.NextDouble());

                float half = CorridorHalfWidth(s);
                float ribbon = RibbonHalfWidth(s);

                // 길가에서 최소 2.6m 는 물러나 있어야 "가로수" 가 아니라 "저 멀리 나무 무리" 로 읽힌다.
                // 그만한 여유가 없는 좁은 구간이면 아예 세우지 않는다.
                if (half - ribbon < 3.6f) continue;

                float lateral = Mathf.Max(half - depth, ribbon + 2.6f);
                if (lateral > half - 0.4f) continue;

                float side = cluster.Side;
                Vector3 spot = At(s, lateral * side);

                // 온실 / 연못 / 트리하우스 위라면 반대편에 세운다 - 군락 수는 지키고 주인공은 안 가린다
                if (IsNoTreeZone(spot) || IsKeptClear(spot))
                {
                    side = -side;
                    spot = At(s, lateral * side);
                    if (IsNoTreeZone(spot) || IsKeptClear(spot)) continue;
                    target = side < 0f ? left : right;
                }
                else
                {
                    target = cluster.Side < 0f ? left : right;
                }

                // 두 그루가 겹쳐 한 덩어리로 뭉치면 군락이 아니라 큰 수풀로 보인다
                bool tooClose = false;
                foreach (var placed in treePositions)
                {
                    if ((placed - spot).sqrMagnitude < 1.5f * 1.5f) { tooClose = true; break; }
                }
                if (tooClose) continue;
                treePositions.Add(spot);

                MakeTree(target, $"Tree_{treeIndex:D2}", spot,
                         Lerp(3.6f, 5.0f, (float)rng.NextDouble()),
                         Lerp(1.2f, 1.55f, (float)rng.NextDouble()),
                         treeIndex % 3 == 0 ? MatLeaf : treeIndex % 3 == 1 ? MatLeafAlt : MatLeafDeep, rng);

                treeSpots.Add(new Vector2(s, side));
                treeIndex++;
            }
        }

        // ---------- 2. 종이 언덕 + 낮은 조경 ----------
        int index = 0;
        for (float s = 1.5f; s < SpineTotalLength - 1f; s += 2.6f, index++)
        {
            float progress = Mathf.Clamp01(s / w);
            float half = CorridorHalfWidth(s);
            float ribbon = RibbonHalfWidth(s);

            for (int side = -1; side <= 1; side += 2)
            {
                Transform target = side < 0 ? left : right;

                bool nearTree = false;
                foreach (var spot in treeSpots)
                {
                    if (spot.y * side > 0f && Mathf.Abs(spot.x - s) < 2.6f) { nearTree = true; break; }
                }

                // 종이 언덕 - 4칸 놓고 3칸 비우기를 반복해 군락으로 끊는다.
                // 좌우 위상을 어긋나게 해서 양쪽이 동시에 막히는 구간이 생기지 않게 한다.
                // 이렇게 해야 Top View 에서 외곽이 하나로 이어진 초록 띠로 보이지 않는다.
                if (((index + (side > 0 ? 3 : 0)) % 7) < 4)
                {
                    // 언덕은 벽에 걸쳐 놓기 때문에 절반이 복도 안으로 들어온다.
                    // 좁은 구간에서 크게 만들면 길 위를 덮어 버리므로 남는 폭에 맞춰 줄인다.
                    float hillRoom = (half + 0.25f) - (ribbon + 1.2f);
                    float hillHalf = Mathf.Clamp(Lerp(1.3f, 3.4f, (float)rng.NextDouble()), 0.8f, hillRoom);
                    // 자유형은 언덕이 벽에 반쯤 걸쳐 벽 아랫단을 가린다.
                    // 직사각형 버전은 그 자리에 울타리 수풀이 서 있으므로 한 발 안쪽에 심는다.
                    float hillLateral = Rectangular ? (half - 0.45f) : (half + 0.25f);
                    Prim(PrimitiveType.Sphere, $"Hill_{index:D2}_{side}", target,
                         At(s, hillLateral * side) + Vector3.down * 0.75f,
                         new Vector3(hillHalf * 2f,
                                     Lerp(3.0f, 4.4f, (float)rng.NextDouble()),
                                     Lerp(4.4f, 6.2f, (float)rng.NextDouble())),
                         PastelHill(index * 2 + (side > 0 ? 1 : 0)), rot: Facing(s));
                }

                // 네 칸에 한 칸은 낮은 조경도 놓지 않는다.
                // 모든 칸을 채우면 다시 "장식이 빽빽한 길" 이 된다.
                if (((index + (side > 0 ? 2 : 0)) % 4) == 3) continue;

                float lowLateral = Mathf.Max(half - Lerp(1.7f, 3.2f, (float)rng.NextDouble()), ribbon + 1.1f);
                if (lowLateral > half - 0.4f) continue;

                Vector3 low = At(s + Lerp(-0.9f, 0.9f, (float)rng.NextDouble()), lowLateral * side);
                if (IsKeptClear(low)) continue;

                // 온실 / 연못 / 트리하우스 둘레는 조경을 비워 그 오브젝트가 주인공이 되게 한다
                if (IsNoTreeZone(low)) continue;

                if (nearTree)
                {
                    // 나무 밑동 수풀 - 군락이 땅에서 솟아난 것처럼 묶어 준다
                    MakeBush(target, $"Bush_{index:D2}_{side}", low,
                             Lerp(0.8f, 1.4f, (float)rng.NextDouble()),
                             rng.Next(2) == 0 ? MatLeafSoft : MatLeafAlt);
                }
                else if (progress > 0.60f &&
                         rng.Next(100) < Mathf.RoundToInt(Lerp(30f, 90f, Mathf.InverseLerp(0.60f, 1f, progress))))
                {
                    // 유니콘으로 갈수록 나무 대신 꽃밭과 별 장식이 늘어난다
                    MakeFlowerBed(target, $"Bed_{index:D2}_{side}", low,
                                  Lerp(0.9f, 1.6f, (float)rng.NextDouble()),
                                  Mathf.RoundToInt(Lerp(5f, 11f, progress)), rng);

                    if (rng.Next(100) < Mathf.RoundToInt(Lerp(20f, 80f, Mathf.InverseLerp(0.60f, 1f, progress))))
                    {
                        MakeStakeStar(target, $"Star_{index:D2}_{side}",
                                      At(s + 1.1f, (lowLateral + 0.9f) * side),
                                      Lerp(1.2f, 2.0f, (float)rng.NextDouble()));
                    }
                }
                else if (index % 3 == 0)
                {
                    MakeFlowerBed(target, $"Bed_{index:D2}_{side}", low,
                                  Lerp(1.0f, 1.7f, (float)rng.NextDouble()), 6 + rng.Next(4), rng);
                }
                else if (index % 3 == 1)
                {
                    MakeBush(target, $"Bush_{index:D2}_{side}", low,
                             Lerp(0.7f, 1.2f, (float)rng.NextDouble()),
                             rng.Next(2) == 0 ? MatLeafSoft : MatLeafAlt);
                }
                else
                {
                    MakeBush(target, $"Bush_{index:D2}_{side}", low,
                             Lerp(0.6f, 1.0f, (float)rng.NextDouble()), MatLeafSoft);
                }
            }
        }

        // ---------- 3. 열린 잔디밭 ----------
        // 나무 군락이 없는 쪽에 아무것도 세우지 않은 잔디 공터를 깐다.
        // "무엇이 있느냐" 보다 "무엇이 비어 있느냐" 가 열린 정원 느낌을 만든다.
        int meadowIndex = 0;
        for (float s = 4f; s < SpineTotalLength - 4f; s += 2.8f, meadowIndex++)
        {
            float half = CorridorHalfWidth(s);
            float ribbon = RibbonHalfWidth(s);
            if (half - ribbon < 2.6f) continue;

            for (int side = -1; side <= 1; side += 2)
            {
                bool nearTree = false;
                foreach (var spot in treeSpots)
                {
                    if (spot.y * side > 0f && Mathf.Abs(spot.x - s) < 2.4f) { nearTree = true; break; }
                }
                if (nearTree) continue;

                float lateral = (ribbon + half) * 0.5f;
                Prim(PrimitiveType.Cylinder, $"Meadow_{meadowIndex:D2}_{side}", meadow,
                     At(s, lateral * side) + Vector3.up * 0.014f,
                     new Vector3(Lerp(3.4f, 5.6f, (float)rng.NextDouble()), 0.014f,
                                 Lerp(3.4f, 5.6f, (float)rng.NextDouble())),
                     rng.Next(2) == 0 ? MatMeadow : MatGrassAlt);
            }
        }

        // ---------- 4. 잔디밭 울타리 ----------
        foreach (var f in MeadowFences)
        {
            MakeFenceRun(fences, $"Fence_{Mathf.RoundToInt(f.From * 100f):D2}",
                         f.From * w, f.To * w, f.Side, f.Offset);
        }
    }

    /// <summary>종이 언덕 색. 공예 색지를 번갈아 쓴 것처럼 톤을 흔든다.</summary>
    private static Material PastelHill(int seed)
    {
        switch (((seed % 6) + 6) % 6)
        {
            case 0: return MatHillMint;
            case 1: return MatGrassAlt;
            case 2: return MatMeadow;
            case 3: return MatHillMint;
            case 4: return MatHillPeach;
            default: return MatHillLilac;
        }
    }

    // ==========================================================
    // 길가 조경 - 걷고 싶게 만드는 안내선
    // ==========================================================
    private static void BuildPathsideLandscaping(Transform parent)
    {
        var rng = new System.Random(909);
        var container = Child(parent, "Pathside");
        int index = 0;

        float approachStart = AreaStartS("UnicornApproach");
        float combatStart = AreaStartS("CombatArena");
        float combatEnd = AreaEndS("CombatArena");

        // 예전에는 1.7m 마다 좌우 양쪽에 꽃을 심어서, 길 양옆이 화단 테두리처럼 일정하게 늘어섰다.
        // 지금은 한 번에 한쪽에만, 몇 걸음 건너 한 무리씩 뭉쳐 심는다.
        // 꽃이 없는 구간이 있어야 있는 구간이 살아난다.
        for (float s = 2f; s < approachStart; s += 1.7f, index++)
        {
            bool combat = s > combatStart + 1f && s < combatEnd - 1f;
            if (combat) continue;                 // 전투장 가장자리는 EdgeBush / EdgeBed 가 이미 맡는다
            if (index % 2 == 1) continue;         // 한 칸 건너 한 칸만

            float ribbon = RibbonHalfWidth(s);
            int side = (index / 2) % 2 == 0 ? -1 : 1;   // 좌우 번갈아 - 반대쪽은 늘 비어 있다

            Vector3 spot = At(s, (ribbon + Lerp(0.35f, 0.85f, (float)rng.NextDouble())) * side);
            if (IsKeptClear(spot) || IsNoTreeZone(spot)) continue;

            if (index % 6 == 0)
            {
                MakeBush(container, $"Bush_{index:D3}_{side}", At(s, (ribbon + 1.25f) * side),
                         Lerp(0.6f, 1.0f, (float)rng.NextDouble()), rng.Next(2) == 0 ? MatLeafSoft : MatLeaf);
            }

            // 한 무리 = 4~6 송이. 반경 0.7m 안에 모아 심어 "군락" 으로 읽히게 한다.
            // 수를 줄이는 대신 한 무리를 두툼하게 해야 듬성듬성해 보이지 않는다.
            int petals = 4 + rng.Next(3);
            for (int k = 0; k < petals; k++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = (float)rng.NextDouble() * 0.7f;
                Vector3 petalSpot = spot + SpineRight(s) * side * Mathf.Cos(angle) * radius
                                         + SpineForward(s) * Mathf.Sin(angle) * radius;
                if (IsKeptClear(petalSpot)) continue;

                Material mat = rng.Next(4) switch
                {
                    0 => MatPink,
                    1 => MatYellow,
                    2 => MatPurple,
                    _ => MatWhiteFlower,
                };
                MakeFlower(container, $"Flower_{index:D3}_{side}_{k}", petalSpot,
                           Lerp(0.3f, 0.6f, (float)rng.NextDouble()), mat, rng);
            }
        }
    }

    // ==========================================================
    // 천장 장식 - 꺾인 천장을 따라 매달린다
    // ==========================================================
    private static void BuildCeilingDecorations(Transform parent)
    {
        var decorations = Child(parent, "CeilingDecorations");
        var rng = new System.Random(7788);

        // Rect 버전은 천장이 훨씬 높아졌으니(RectCeilingY) 구름도 그만큼 위로 올리고,
        // 다 같은 높이가 아니라 낮음/중간/높음 3단으로 층을 줘서 "높은 가짜 하늘에 매달린
        // 공예 솜구름" 느낌을 만든다. 자유형 버전은 기존 높이 그대로 둔다.
        float lowMin, lowMax, midMin, midMax, highMin, highMax;
        if (Rectangular)
        {
            // MakeHangingCloud 의 퍼프(구 모양 뭉치)는 크기를 줄여도(위에서 최대 2.0)
            // 중심 기준 아래로 최대 약 0.62m, 위로 최대 약 0.77m(반지름+흔들림)까지 튀어
            // 나온다. lowMin 은 Treehouse 최고점(7.1)보다 최소 0.3m 위에서 시작하고,
            // highMax 는 천장(10.5)보다 최소 0.3m 아래에서 끝나도록 역산했다.
            // (천장까지 닿는 것은 매다는 줄 하나뿐이어야 한다 - 구름 몸통은 안 닿는다.)
            lowMin = 8.05f; lowMax = 8.35f;
            midMin = 8.55f; midMax = 8.85f;
            highMin = 9.05f; highMax = 9.4f;
        }
        else
        {
            lowMin = 5.4f; lowMax = 5.9f;
            midMin = 6.0f; midMax = 6.4f;
            highMin = 6.5f; highMax = 6.8f;
        }

        // 주요 랜드마크 바로 위에는 구름이 몰리지 않도록, 그 근처 s 에서는 배치 확률을
        // 크게 낮춘다(완전히 금지하지는 않는다 - 아예 없으면 오히려 부자연스럽다).
        var landmarkS = new List<float>
        {
            InArea("StartGarden", 0.5f),
            GreenhouseS,
            InArea("WeaponLink", 0.30f),      // Treehouse V2
            InArea("CombatArena", 0.5f),
            InArea("UnicornApproach", 0.5f),
            InArea("UnicornPlaza", 0.35f),    // 유니콘이 서는 자리 근처
        };
        const float landmarkGap = 3.0f;

        // 공중 장식은 구간마다 밀도를 다르게 둔다.
        // 시작 정원에서는 하늘을 거의 비워 두고, 유니콘에 가까워질수록 촘촘해진다.
        // 처음부터 하늘이 꽉 차 있으면 마지막 구간이 특별해 보이지 않는다.
        // 간격도 3m 고정이 아니라 3~6m 사이로 흔들어서 격자처럼 규칙적으로 안 보이게 한다.
        int cloudIndex = 0;
        for (float s = 3.5f; s < SpineTotalLength - 3f; s += Lerp(3f, 6f, (float)rng.NextDouble()))
        {
            float progress = Mathf.Clamp01(s / WalkLength);
            float chance = progress < 0.32f ? 0.62f
                         : progress < 0.78f ? 0.80f
                         : 0.95f;

            float nearestLandmarkDist = float.MaxValue;
            foreach (var ls in landmarkS) nearestLandmarkDist = Mathf.Min(nearestLandmarkDist, Mathf.Abs(s - ls));
            if (nearestLandmarkDist < landmarkGap) chance *= 0.5f;

            if (rng.NextDouble() > chance) continue;

            float half = CorridorHalfWidth(s);
            // 가운데로 쏠리지 않게 코리도 폭의 85%까지 넓게 흩뿌린다
            float lateral = Lerp(-half * 0.85f, half * 0.85f, (float)rng.NextDouble());

            int roll = rng.Next(100);
            float y = roll < 40 ? Lerp(lowMin, lowMax, (float)rng.NextDouble())
                    : roll < 70 ? Lerp(midMin, midMax, (float)rng.NextDouble())
                    : Lerp(highMin, highMax, (float)rng.NextDouble());

            // 뭉치(퍼프)는 중심에서 반지름만큼 아래로도 튀어나온다 - Rect 버전은 층 사이
            // 여유가 빠듯하니 구름을 자유형보다 살짝 작게 잡아 트리하우스/천장에 안 닿게 한다.
            float cloudSize = Rectangular ? Lerp(1.4f, 2.0f, (float)rng.NextDouble())
                                           : Lerp(1.6f, 2.8f, (float)rng.NextDouble());

            MakeHangingCloud(decorations, $"Cloud_{cloudIndex:D2}", At(s, lateral) + Vector3.up * y, cloudSize, rng);
            cloudIndex++;
        }

        // 종이별은 유니콘 접근로에 와서야 눈에 띄게 늘어난다
        for (float s = 7f; s < SpineTotalLength - 3f; s += 4.0f)
        {
            float progress = Mathf.Clamp01(s / WalkLength);
            int count = progress < 0.55f ? (rng.Next(100) < 28 ? 1 : 0)
                      : progress < 0.80f ? 1
                      : 2 + rng.Next(2);
            if (count == 0) continue;

            float half = CorridorHalfWidth(s);
            for (int i = 0; i < count; i++)
            {
                float lateral = Lerp(-half * 0.75f, half * 0.75f, (float)rng.NextDouble());
                MakePaperStar(decorations, $"Star_{s:F0}_{i}",
                              At(s + Lerp(-1.5f, 1.5f, (float)rng.NextDouble()), lateral)
                              + Vector3.up * Lerp(5.6f, 7.0f, (float)rng.NextDouble()),
                              Lerp(0.35f, 0.65f, (float)rng.NextDouble()));
            }
        }

        // 종이 태양 - 시작 정원 쪽
        var sun = Child(decorations, "Sun");
        Vector3 sunPos = At(10f, -3.5f) + Vector3.up * 6.4f;
        Prim(PrimitiveType.Cylinder, "SunDisc", sun, sunPos, new Vector3(2.2f, 0.08f, 2.2f), MatSun,
             rot: Quaternion.Euler(90f, 0f, 0f));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, $"SunRay_{i}", sun,
                 sunPos + new Vector3(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle) * 1.5f, 0f),
                 new Vector3(0.9f, 0.28f, 0.06f), MatSun, rot: Quaternion.Euler(0f, 0f, i * 45f));
        }
        MakeString(sun, "SunString", sunPos);
    }

    private static void MakeHangingCloud(Transform parent, string name, Vector3 center, float size, System.Random rng)
    {
        var cloud = Child(parent, name);
        int puffs = 4 + rng.Next(2);
        for (int i = 0; i < puffs; i++)
        {
            float t = puffs == 1 ? 0.5f : i / (float)(puffs - 1);
            float bulge = Mathf.Sin(t * Mathf.PI) * 0.45f + 0.55f;
            Vector3 offset = new Vector3(Lerp(-size * 0.5f, size * 0.5f, t),
                                         (float)rng.NextDouble() * 0.15f,
                                         (float)rng.NextDouble() * 0.4f - 0.2f);
            Prim(PrimitiveType.Sphere, $"Puff_{i}", cloud, center + offset,
                 Vector3.one * size * bulge * 0.62f, MatCloud);
        }
        MakeString(cloud, "String", center + Vector3.up * size * 0.2f);
    }

    private static void MakePaperStar(Transform parent, string name, Vector3 center, float size)
    {
        var star = Child(parent, name);
        Prim(PrimitiveType.Cube, "BladeA", star, center, new Vector3(size * 2f, size * 0.5f, 0.05f), MatStar);
        Prim(PrimitiveType.Cube, "BladeB", star, center, new Vector3(size * 0.5f, size * 2f, 0.05f), MatStar);
        Prim(PrimitiveType.Cube, "BladeC", star, center, new Vector3(size * 1.5f, size * 0.4f, 0.05f), MatStar,
             rot: Quaternion.Euler(0f, 0f, 45f));
        Prim(PrimitiveType.Cube, "BladeD", star, center, new Vector3(size * 0.4f, size * 1.5f, 0.05f), MatStar,
             rot: Quaternion.Euler(0f, 0f, 45f));
        MakeString(star, "String", center);
    }

    private static void MakeString(Transform parent, string name, Vector3 from)
    {
        float height = CeilingY - from.y;
        if (height <= 0.05f) return;
        Prim(PrimitiveType.Cylinder, name, parent,
             new Vector3(from.x, from.y + height * 0.5f, from.z),
             new Vector3(0.02f, height * 0.5f, 0.02f), MatCloud);
    }

    // ==========================================================
    // 조각 만들기 도우미
    // ==========================================================
    private static void MakeTree(Transform parent, string name, Vector3 basePos, float height,
                                 float canopyRadius, Material leafMat, System.Random rng)
    {
        var tree = Child(parent, name);

        float maxTop = CeilingY - 0.4f;
        if (height * 0.92f + canopyRadius > maxTop) canopyRadius = Mathf.Max(0.6f, maxTop - height * 0.92f);

        Prim(PrimitiveType.Cylinder, "Trunk", tree, basePos + Vector3.up * height * 0.5f,
             new Vector3(0.28f, height * 0.5f, 0.28f), MatTrunk, collider: true);
        Prim(PrimitiveType.Sphere, "Canopy_A", tree, basePos + Vector3.up * height * 0.92f,
             Vector3.one * canopyRadius * 2f, leafMat);
        Prim(PrimitiveType.Sphere, "Canopy_B", tree,
             basePos + new Vector3((float)rng.NextDouble() * 0.9f - 0.45f, height * 0.74f, (float)rng.NextDouble() * 0.9f - 0.45f),
             Vector3.one * canopyRadius * 1.5f, leafMat);
        Prim(PrimitiveType.Sphere, "Canopy_C", tree,
             basePos + new Vector3((float)rng.NextDouble() * 0.9f - 0.45f, height * 1.02f, (float)rng.NextDouble() * 0.7f - 0.35f),
             Vector3.one * canopyRadius * 1.15f, leafMat);
    }

    private static void MakeBush(Transform parent, string name, Vector3 pos, float radius, Material mat)
    {
        var bush = Child(parent, name);
        Prim(PrimitiveType.Sphere, "Main", bush, pos + Vector3.up * radius * 0.45f,
             new Vector3(radius * 2f, radius * 1.3f, radius * 2f), mat, collider: true);
        Prim(PrimitiveType.Sphere, "Side", bush, pos + new Vector3(radius * 0.6f, radius * 0.3f, radius * 0.3f),
             new Vector3(radius * 1.3f, radius * 0.9f, radius * 1.3f), mat);
    }

    private static void MakeFlower(Transform parent, string name, Vector3 pos, float height, Material mat, System.Random rng)
    {
        var flower = Child(parent, name);
        Prim(PrimitiveType.Cylinder, "Stem", flower, pos + Vector3.up * height * 0.5f,
             new Vector3(0.035f, height * 0.5f, 0.035f), MatLeafDeep);
        Prim(PrimitiveType.Sphere, "Head", flower, pos + Vector3.up * height,
             Vector3.one * Lerp(0.18f, 0.28f, (float)rng.NextDouble()), mat);
    }

    private static void MakeFlowerBed(Transform parent, string name, Vector3 center, float radius, int count, System.Random rng)
    {
        var bed = Child(parent, name);
        Prim(PrimitiveType.Cylinder, "Soil", bed, center + Vector3.up * 0.015f,
             new Vector3(radius * 2f, 0.015f, radius * 2f), MatLeafDeep);

        for (int i = 0; i < count; i++)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float distance = (float)rng.NextDouble() * radius * 0.85f;
            Vector3 spot = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            Material mat = rng.Next(4) switch
            {
                0 => MatPink,
                1 => MatYellow,
                2 => MatPurple,
                _ => MatWhiteFlower,
            };
            MakeFlower(bed, $"Flower_{i:D2}", spot, Lerp(0.3f, 0.55f, (float)rng.NextDouble()), mat, rng);
        }
    }

    private static void MakeFenceRun(Transform parent, string name, float fromS, float toS, float side, float offset)
    {
        var fence = Child(parent, name);
        int index = 0;

        for (float s = fromS; s < toS; s += 0.55f, index++)
        {
            Vector3 spot = At(s, (RibbonHalfWidth(s) + offset) * side);
            Quaternion facing = Facing(s);
            Prim(PrimitiveType.Cube, $"Picket_{index:D2}", fence, spot + Vector3.up * 0.5f,
                 new Vector3(0.1f, 1.0f, 0.06f), MatFence, rot: facing);

            if (index % 4 == 0)
            {
                Prim(PrimitiveType.Cube, $"Rail_{index:D2}", fence, spot + Vector3.up * 0.72f,
                     new Vector3(0.05f, 0.09f, 2.3f), MatFence, rot: facing);
                Prim(PrimitiveType.Cube, $"RailLow_{index:D2}", fence, spot + Vector3.up * 0.34f,
                     new Vector3(0.05f, 0.09f, 2.3f), MatFence, rot: facing);
            }
        }
    }

    private static void MakeLamp(Transform parent, string name, Vector3 pos)
    {
        var lamp = Child(parent, name);
        Prim(PrimitiveType.Cylinder, "Base", lamp, pos + Vector3.up * 0.1f,
             new Vector3(0.34f, 0.1f, 0.34f), MatStone, collider: true);
        Prim(PrimitiveType.Cylinder, "Pole", lamp, pos + Vector3.up * 1.35f,
             new Vector3(0.1f, 1.35f, 0.1f), MatWood, collider: true);
        Prim(PrimitiveType.Sphere, "Glass", lamp, pos + Vector3.up * 2.85f, Vector3.one * 0.46f, MatSun);
        Prim(PrimitiveType.Cube, "Cap", lamp, pos + Vector3.up * 3.14f, new Vector3(0.5f, 0.1f, 0.5f), MatWood);
    }

    private static void MakeCrate(Transform parent, string name, Vector3 pos, float size, float rotY)
    {
        Prim(PrimitiveType.Cube, name, parent, pos + Vector3.up * size * 0.5f,
             Vector3.one * size, MatWood, collider: true, rot: Quaternion.Euler(0f, rotY, 0f));
    }

    private static void MakeStakeStar(Transform parent, string name, Vector3 pos, float height)
    {
        var stake = Child(parent, name);
        Prim(PrimitiveType.Cylinder, "Stick", stake, pos + Vector3.up * height * 0.5f,
             new Vector3(0.04f, height * 0.5f, 0.04f), MatWood);
        Prim(PrimitiveType.Cube, "BladeA", stake, pos + Vector3.up * (height + 0.2f),
             new Vector3(0.62f, 0.16f, 0.04f), MatStar);
        Prim(PrimitiveType.Cube, "BladeB", stake, pos + Vector3.up * (height + 0.2f),
             new Vector3(0.16f, 0.62f, 0.04f), MatStar);
        Prim(PrimitiveType.Cube, "BladeC", stake, pos + Vector3.up * (height + 0.2f),
             new Vector3(0.48f, 0.13f, 0.04f), MatStar, rot: Quaternion.Euler(0f, 0f, 45f));
        Prim(PrimitiveType.Cube, "BladeD", stake, pos + Vector3.up * (height + 0.2f),
             new Vector3(0.13f, 0.48f, 0.04f), MatStar, rot: Quaternion.Euler(0f, 0f, 45f));
    }

    private static void MakeMarker(Transform parent, string name, Vector3 pos, Material mat, float size)
    {
        var marker = Child(parent, name);
        marker.position = pos;
        Prim(PrimitiveType.Sphere, "Gem", marker, pos + Vector3.up * 1.1f, Vector3.one * size, mat);
        Prim(PrimitiveType.Cylinder, "Ring", marker, pos + Vector3.up * 0.03f,
             new Vector3(size * 3f, 0.015f, size * 3f), mat);

        KeepClear.Add(new Vector4(pos.x, pos.y, pos.z, 2.2f));
    }
}
