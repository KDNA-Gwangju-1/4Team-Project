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
        BuildGreenhouse(Child(parent, "Greenhouse"), At(houseS, -6.0f), Facing(houseS));
        MakeMarker(markers, "Marker_Clue_03_Greenhouse", At(houseS, -2.8f), MatMarkerClue, 0.34f);

        // 연못
        float pondS = InArea("GreenhousePond", 0.55f);
        var pond = Child(parent, "Pond");
        Vector3 pondCenter = At(pondS, 6.0f);
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

        // 삼각 깃발 - 기둥은 반드시 길 바깥에
        var bunting = Child(parent, "Bunting");
        float postS = InArea("WeaponLink", 0.88f);
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

        MakeFlowerBed(parent, "FlowerBed_A", At(InArea("WeaponLink", 0.2f), 2.8f), 1.5f, 6, rng);
        MakeLamp(parent, "Lamp_A", At(InArea("WeaponLink", 0.12f), 2.6f));
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

        for (float t = 0.1f; t <= 0.9f; t += 0.16f)
        {
            float s = InArea("CombatArena", t);
            Prim(PrimitiveType.Cylinder, $"ArenaGround_{t:F2}", parent, SpinePoint(s) + Vector3.up * 0.012f,
                 new Vector3(18f, 0.02f, 18f), MatSand);
        }

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

        // 넓은 마당 가장자리 조경 - 시야/이동은 막지 않는다
        for (int i = 0; i < 5; i++)
        {
            float t = Lerp(0.1f, 0.9f, i / 4f);
            MakeTree(parent, $"Tree_L{i}", At(InArea("CombatArena", t), -10.6f),
                     Lerp(4.2f, 5.0f, (float)rng.NextDouble()), 1.5f, i % 2 == 0 ? MatLeaf : MatLeafAlt, rng);
            MakeTree(parent, $"Tree_R{i}", At(InArea("CombatArena", t + 0.06f), 10.8f),
                     Lerp(4.2f, 5.0f, (float)rng.NextDouble()), 1.5f, i % 2 == 0 ? MatLeafDeep : MatLeaf, rng);
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
    }

    // ==========================================================
    // 8. 유니콘 광장 - 크게 열리며 끝난다
    // ==========================================================
    private static void BuildUnicornPlaza(Transform parent, Transform markers)
    {
        var rng = new System.Random(707);
        Vector3 center = SpinePoint(UnicornS);
        float radius = 9.5f;

        Prim(PrimitiveType.Cylinder, "Plaza_Outer", parent, center + Vector3.up * 0.015f,
             new Vector3(radius * 2f, 0.015f, radius * 2f), MatStone);
        Prim(PrimitiveType.Cylinder, "Plaza_Mid", parent, center + Vector3.up * 0.03f,
             new Vector3(radius * 1.5f, 0.015f, radius * 1.5f), MatPathAlt);
        Prim(PrimitiveType.Cylinder, "Plaza_Inner", parent, center + Vector3.up * 0.045f,
             new Vector3(radius * 1.0f, 0.015f, radius * 1.0f), MatPath);
        Prim(PrimitiveType.Cylinder, "Plaza_Core", parent, center + Vector3.up * 0.06f,
             new Vector3(radius * 0.5f, 0.015f, radius * 0.5f), MatStone);

        BuildUnicorn(Child(parent, "Unicorn_Placeholder"), center, Facing(UnicornS));
        MakeMarker(markers, "Marker_Unicorn_Boss", center, MatMarkerDream, 0.5f);
        MakeMarker(markers, "Marker_DreamTransition_To2D", At(UnicornS - 2.6f, 0f), MatMarkerDream, 0.35f);

        // 뒤쪽의 특별한 큰 나무 (최종 아트 Hero Tree 자리)
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

        // 광장 테두리 - 경계 나무(벽 근처)에 묻히지 않게 광장 안쪽 가장자리에 둔다
        float ringRadius = radius - 2.4f;
        for (int i = 0; i < 20; i++)
        {
            float angle = i * (360f / 20f) * Mathf.Deg2Rad;
            Vector3 ring = center + new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
            if (IsKeptClear(ring)) continue;
            if (Vector3.Distance(ring, SpinePoint(UnicornS - 6f)) < 4.0f) continue;   // 입구는 비워 둔다

            MakeFlowerBed(parent, $"RingBed_{i:D2}", ring, 0.85f, 6, rng);
            if (i % 4 == 0) MakeLamp(parent, $"RingLamp_{i:D2}", ring + (ring - center).normalized * 1.4f);
            if (i % 4 == 2) MakeStakeStar(parent, $"RingStar_{i:D2}", ring + (ring - center).normalized * 1.2f, 2.0f);
        }

        // 바닥 단이 밋밋해 보이지 않게 낮은 돌 테두리를 둘러 준다
        for (int i = 0; i < 26; i++)
        {
            float angle = i * (360f / 26f) * Mathf.Deg2Rad;
            Vector3 spot = center + new Vector3(Mathf.Cos(angle) * (radius * 0.52f), 0.09f, Mathf.Sin(angle) * (radius * 0.52f));
            if (IsKeptClear(spot)) continue;
            Prim(PrimitiveType.Cube, $"PlazaKerb_{i:D2}", parent, spot, new Vector3(0.7f, 0.18f, 0.5f),
                 MatStone, rot: Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
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
    // 경계 조경 - 꺾인 복도 벽을 따라가며 가린다
    // ==========================================================
    private static void BuildBoundaryLandscaping(Transform parent)
    {
        var rng = new System.Random(808);
        var left = Child(parent, "Boundary_Left");
        var right = Child(parent, "Boundary_Right");
        int index = 0;

        for (float s = 1.5f; s < SpineTotalLength - 1f; s += 2.6f, index++)
        {
            float half = CorridorHalfWidth(s);
            float ribbon = RibbonHalfWidth(s);

            for (int side = -1; side <= 1; side += 2)
            {
                Transform target = side < 0 ? left : right;

                // 길을 침범하지 않는 선에서 최대한 벽에 붙인다
                float treeLateral = Mathf.Max(half - 1.4f, ribbon + 1.2f);
                if (treeLateral > half - 0.3f) continue;

                MakeTree(target, $"Tree_{index:D2}_{side}", At(s, treeLateral * side),
                         Lerp(4.0f, 5.4f, (float)rng.NextDouble()), Lerp(1.3f, 1.6f, (float)rng.NextDouble()),
                         index % 3 == 0 ? MatLeafDeep : index % 3 == 1 ? MatLeaf : MatLeafAlt, rng);

                // 벽 아랫부분을 덮는 종이 언덕
                float hillHalf = Mathf.Clamp((half - treeLateral) + 1.6f, 1.0f, 3.4f);
                Prim(PrimitiveType.Sphere, $"Hill_{index:D2}_{side}", target,
                     At(s, (half + 0.2f) * side) + Vector3.down * 0.6f,
                     new Vector3(hillHalf * 2f, Lerp(3.2f, 4.6f, (float)rng.NextDouble()), 5.2f),
                     rng.Next(2) == 0 ? MatLeafDeep : MatLeafAlt, rot: Facing(s));

                if (index % 2 == 0)
                {
                    float bushLateral = Mathf.Max(half - 2.6f, ribbon + 1.0f);
                    if (bushLateral < half - 0.5f)
                    {
                        MakeBush(target, $"Bush_{index:D2}_{side}", At(s + 1.2f, bushLateral * side),
                                 Lerp(1.2f, 1.9f, (float)rng.NextDouble()),
                                 rng.Next(2) == 0 ? MatLeafAlt : MatLeafDeep);
                    }
                }
            }
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

        for (float s = 2f; s < approachStart; s += 1.7f, index++)
        {
            bool combat = s > combatStart + 1f && s < combatEnd - 1f;
            float ribbon = RibbonHalfWidth(s);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 spot = At(s, (ribbon + Lerp(0.35f, 0.9f, (float)rng.NextDouble())) * side);
                if (IsKeptClear(spot)) continue;

                if (!combat && index % 3 == 0)
                {
                    MakeBush(container, $"Bush_{index:D3}_{side}", At(s, (ribbon + 1.3f) * side),
                             Lerp(0.7f, 1.2f, (float)rng.NextDouble()), rng.Next(2) == 0 ? MatLeafAlt : MatLeaf);
                }
                else
                {
                    Material mat = rng.Next(4) switch
                    {
                        0 => MatPink,
                        1 => MatYellow,
                        2 => MatPurple,
                        _ => MatWhiteFlower,
                    };
                    MakeFlower(container, $"Flower_{index:D3}_{side}", spot, Lerp(0.35f, 0.6f, (float)rng.NextDouble()), mat, rng);
                }
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

        for (float s = 4f; s < SpineTotalLength - 3f; s += 5.2f)
        {
            float half = CorridorHalfWidth(s);
            float lateral = Lerp(-half * 0.65f, half * 0.65f, (float)rng.NextDouble());
            float y = Lerp(5.4f, 6.8f, (float)rng.NextDouble());
            MakeHangingCloud(decorations, $"Cloud_{s:F0}", At(s, lateral) + Vector3.up * y,
                             Lerp(1.6f, 3.0f, (float)rng.NextDouble()), rng);
        }

        for (float s = 7f; s < SpineTotalLength - 3f; s += 6.4f)
        {
            float half = CorridorHalfWidth(s);
            float lateral = Lerp(-half * 0.7f, half * 0.7f, (float)rng.NextDouble());
            MakePaperStar(decorations, $"Star_{s:F0}", At(s, lateral) + Vector3.up * Lerp(5.8f, 7.0f, (float)rng.NextDouble()),
                          Lerp(0.35f, 0.6f, (float)rng.NextDouble()));
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
