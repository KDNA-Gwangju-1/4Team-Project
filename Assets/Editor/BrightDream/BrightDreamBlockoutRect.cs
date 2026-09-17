using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 비교 테스트용 "직사각형 외곽" 변형.
///
/// 자유형 버전은 바닥 / 양옆 벽 / 천장이 전부 중심선을 따라 휘어 나가서
/// 맵 바깥 모양 자체가 구불구불하다.
///
/// 이 변형은 같은 중심선을 그대로 쓰되,
///   - 바깥 껍데기(바닥 / 사방 벽 / 천장)는 하나의 긴 직사각형 상자
///   - 걷는 길의 좌우 경계는 벽 대신 "공예 울타리 수풀(Hedge)"
/// 로 만든다.
///
/// 즉 "긴 직사각형 공예 세트 안에 구불구불한 정원 산책로가 들어 있는" 구조다.
/// 길과 구역 배치는 (s, lateral) 계산을 그대로 쓰기 때문에 두 버전이 완전히 같다.
///
/// 상자 안에서 길이 쓰지 않고 남는 공간은 배경 정원(낮은 언덕 / 잔디밭 / 꽃무리)이 채운다.
/// </summary>
public static partial class BrightDreamBlockoutBuilder
{
    /// <summary>상자 안쪽 여백 - 길 바깥 경계에서 벽까지.</summary>
    private const float RectMargin = 3.5f;

    /// <summary>바닥 / 천장 타일 한 변.</summary>
    private const float RectTile = 6.5f;

    /// <summary>길이 차지하는 전체 범위 + 여백 = 상자 바닥 크기.</summary>
    public static Bounds RectFootprint()
    {
        var b = new Bounds(SpinePoint(0f), Vector3.zero);
        for (float s = 0f; s <= spineLength; s += 0.5f)
        {
            float half = CorridorHalfWidth(s);
            b.Encapsulate(At(s, half));
            b.Encapsulate(At(s, -half));
        }
        b.Expand(new Vector3(RectMargin * 2f, 0f, RectMargin * 2f));
        return b;
    }

    // ==========================================================
    // 직사각형 껍데기 - 바닥 / 사방 벽 / 천장
    // ==========================================================
    private static void BuildRectShell(Transform ground, Transform boundaries, Transform ceiling)
    {
        var rect = RectFootprint();
        float minX = rect.min.x, maxX = rect.max.x;
        float minZ = rect.min.z, maxZ = rect.max.z;
        float width = maxX - minX, depth = maxZ - minZ;

        // --- 바닥 : 색지를 이어 붙인 것처럼 타일로 깐다 ---
        int cols = Mathf.CeilToInt(width / RectTile);
        int rows = Mathf.CeilToInt(depth / RectTile);
        float tileW = width / cols, tileD = depth / rows;

        for (int ix = 0; ix < cols; ix++)
        {
            for (int iz = 0; iz < rows; iz++)
            {
                Vector3 center = new Vector3(minX + (ix + 0.5f) * tileW, -0.25f, minZ + (iz + 0.5f) * tileD);
                Prim(PrimitiveType.Cube, $"Floor_{ix:D2}_{iz:D2}", ground, center,
                     new Vector3(tileW, 0.5f, tileD),
                     (ix + iz) % 2 == 0 ? MatGrass : MatGrassAlt, collider: true);
            }
        }

        // --- 사방 벽 : 하늘색 배경판 ---
        RectWall(boundaries, "Wall_West", new Vector3(minX - 0.25f, CeilingY * 0.5f, rect.center.z), new Vector3(0.5f, CeilingY + 1f, depth + 1f));
        RectWall(boundaries, "Wall_East", new Vector3(maxX + 0.25f, CeilingY * 0.5f, rect.center.z), new Vector3(0.5f, CeilingY + 1f, depth + 1f));
        RectWall(boundaries, "Wall_South", new Vector3(rect.center.x, CeilingY * 0.5f, minZ - 0.25f), new Vector3(width + 1f, CeilingY + 1f, 0.5f));
        RectWall(boundaries, "Wall_North", new Vector3(rect.center.x, CeilingY * 0.5f, maxZ + 0.25f), new Vector3(width + 1f, CeilingY + 1f, 0.5f));

        // --- 천장 : 하늘색 판 ---
        for (int ix = 0; ix < cols; ix++)
        {
            for (int iz = 0; iz < rows; iz++)
            {
                Vector3 center = new Vector3(minX + (ix + 0.5f) * tileW, CeilingY + 0.25f, minZ + (iz + 0.5f) * tileD);
                var slab = Prim(PrimitiveType.Cube, $"Ceiling_{ix:D2}_{iz:D2}", ceiling, center,
                                new Vector3(tileW + 0.05f, 0.5f, tileD + 0.05f), MatSky, collider: true);
                slab.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }

    private static void RectWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var wall = Prim(PrimitiveType.Cube, name, parent, position, scale, MatSky, collider: true);
        // 배경판이 햇빛을 막아 상자 안이 어두워지지 않게 한다
        wall.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>
    /// 경계를 어떤 식으로 보여 줄지.
    ///
    /// 예전에는 전 구간을 2.5~5m 초록 벽이 둘러쌌다.
    /// 그러면 "상자 안의 정원" 이 아니라 "초록 벽으로 만든 미로" 로 읽힌다.
    /// 지금은 구간 성격에 따라 셋으로 나눈다.
    /// </summary>
    private enum EdgeStyle
    {
        /// <summary>보여 주는 경계 없음. 조경과 배경 정원이 알아서 영역을 읽히게 한다.</summary>
        Open,

        /// <summary>눈높이보다 낮은 수풀 띠. 시야는 안 막고 "여기까지" 만 알려 준다.</summary>
        Low,

        /// <summary>유니콘을 가려야 하는 자리에만 세우는 높은 가림막.</summary>
        Tall,
    }

    private static EdgeStyle EdgeStyleAt(float s)
    {
        switch (AreaNameAt(s))
        {
            // 좁게 이어지는 산책 구간 - 낮은 수풀로 길만 알려 준다
            case "ClueWalk":
            case "WeaponLink":
            case "TutorialNook":
                return EdgeStyle.Low;

            // 유니콘을 미리 보여 주면 안 되는 구간
            case "UnicornApproach":
                return EdgeStyle.Tall;

            // 시작 정원 / 온실·연못 / 전투장 / 광장 - 활짝 열어 둔다
            default:
                return EdgeStyle.Open;
        }
    }

    /// <summary>낮은 수풀 띠 높이 - 눈높이(1.6m)보다 낮게.</summary>
    private const float LowHedgeHeight = 1.05f;

    /// <summary>높은 가림막 높이.</summary>
    private const float TallScreenHeight = 4.2f;

    /// <summary>이탈 방지용 투명 벽 높이.</summary>
    private const float InvisibleWallHeight = 2.4f;

    /// <summary>
    /// 접근로에 세우는 가림막 위치 - 구간 안에서의 비율.
    ///
    /// 연속으로 두르면 다시 초록 복도가 되므로, 유니콘 시선을 끊는 데 필요한 자리에만
    /// 판을 몇 장 세우고 사이는 비운다. 비운 자리는 꽃밭과 별 장식이 채운다.
    /// </summary>
    private static readonly (float From, float To)[] ApproachScreens =
    {
        (0.02f, 0.30f),
        (0.40f, 0.66f),
        (0.74f, 0.96f),
    };

    private static bool InApproachScreen(float s)
    {
        float a = AreaStartS("UnicornApproach"), b = AreaEndS("UnicornApproach");
        float t = Mathf.InverseLerp(a, b, s);
        foreach (var w in ApproachScreens)
        {
            if (t >= w.From && t <= w.To) return true;
        }
        return false;
    }

    // ==========================================================
    // 길 좌우 경계
    //
    // 보이는 경계(수풀 / 가림막)와 실제로 막는 경계(투명 콜라이더)를 분리한다.
    //   - 막는 일은 어디서나 투명 콜라이더가 한다.
    //   - 보여 주는 일은 구간 성격에 따라 다르게 한다.
    // 덕분에 전투장이나 광장처럼 열려야 하는 곳은 초록 벽 없이도 밖으로 못 나간다.
    // ==========================================================
    private static void BuildSoftBoundary(Transform parent)
    {
        const float step = 0.8f;
        const float overlap = 1.18f;

        var left = Child(parent, "Edge_Left");
        var right = Child(parent, "Edge_Right");

        // 보이는 수풀과 막는 상자를 따로 둔다.
        // 나중에 VARCO3D 수풀 프리팹으로 갈아끼울 때 Edge_Left / Edge_Right 만 바꾸면 되고,
        // 충돌 모양은 경계 곡선에서 계산되므로 그대로 유지된다.
        var solids = Child(parent, "Edge_Colliders");
        int index = 0;

        Vector3 EdgeAt(float at, float side) => At(at, (CorridorHalfWidth(at) + 0.25f) * side);

        // 광장을 넓히면서(20%) UnicornApproach 의 마지막 굴곡이 접히는 자리 일부가 광장
        // 바닥 범위 안쪽으로 들어왔다. 시야차단(Tall) 가림막은 접근로를 "걷는 동안" 필요한
        // 것이지 광장 바닥 위에 서 있을 필요는 없으므로, 광장 중심에서 너무 가까운 자리는
        // 콜라이더(실제로 막는 역할)는 그대로 두고 눈에 보이는 판만 뺀다 - 사용자가 스크린샷으로
        // 확인한 문제. Unicorn 최초 노출 거리(10.62m)는 이 변경 전후로 재실측해 동일함을 확인했다.
        Vector3 plazaCenter = SpinePoint(UnicornS);
        const float plazaVisualClearRadius = 12.5f;

        for (float s = 0f; s <= spineLength; s += step, index++)
        {
            Quaternion rot = Facing(s);
            Vector3 curL = EdgeAt(s, -1f), nextL = EdgeAt(s + step, -1f);
            Vector3 curR = EdgeAt(s, 1f), nextR = EdgeAt(s + step, 1f);

            // --- 보여 주는 경계 ---
            var style = EdgeStyleAt(s);
            if (style == EdgeStyle.Open) continue;

            bool tall = style == EdgeStyle.Tall && InApproachScreen(s);
            if (style == EdgeStyle.Tall && !tall) continue;   // 가림막 사이는 비워 둔다

            float height = tall ? TallScreenHeight : LowHedgeHeight;
            float waveL = 1f + Mathf.Sin(s * 0.11f) * 0.05f;
            float waveR = 1f + Mathf.Sin(s * 0.09f + 1.9f) * 0.05f;

            bool visible = !(tall && Vector3.Distance(SpinePoint(s), plazaCenter) < plazaVisualClearRadius);

            HedgeRun(left, solids, $"HedgeL_{index:D3}", curL, nextL, height * waveL, overlap, rot, index, visible);
            HedgeRun(right, solids, $"HedgeR_{index:D3}", curR, nextR, height * waveR, overlap, rot, index + 40, visible);
        }

        // 길 양 끝 마개 - 시작 뒤쪽과 광장 뒤쪽.
        // Cap_End(광장 뒤쪽)는 스파인이 막 꺾인 채로 끝나는 지점이라 Facing(s) 방향의 판이
        // 대각선으로 BigTree_Placeholder/광장 바닥 위에 길게 걸쳐 보이는 문제가 스크린샷으로
        // 확인됐다. 사용자 요청대로 Cap_End 자체를 만들지 않는다(이 파일은 Rect 전용이라
        // 자유형에는 영향 없음). 코리도 바깥은 어차피 BuildRectShell 의 사각 상자 벽이
        // 감싸고 있어 이 마개가 없어도 맵을 벗어날 수는 없다.
        EndCap(parent, solids, "Cap_Start", 0f, -1f);
    }

    private static void EndCap(Transform parent, Transform solids, string name, float s, float direction)
    {
        float half = CorridorHalfWidth(s);
        Vector3 spot = SpinePoint(s) + SpineForward(s) * direction * 0.35f;
        Quaternion rot = Facing(s);

        Prim(PrimitiveType.Cube, name, parent, spot + Vector3.up * (LowHedgeHeight * 0.5f),
             new Vector3(half * 2f + 1.6f, LowHedgeHeight, 0.8f), MatLeafAlt, rot: rot);

        HedgeSolid(solids, name + "_Solid", spot, LowHedgeHeight, 0.8f, rot, half * 2f + 1.6f);
    }

    /// <summary>경계선 위의 두 점을 잇는 수풀 한 토막.</summary>
    private static void HedgeRun(Transform parent, Transform solids, string name, Vector3 from, Vector3 to,
                                 float height, float overlap, Quaternion fallbackRot, int index, bool visible = true)
    {
        Vector3 delta = to - from;
        delta.y = 0f;
        float length = delta.magnitude;
        Quaternion rot = length > 0.01f ? Quaternion.LookRotation(delta) : fallbackRot;
        Vector3 mid = (from + to) * 0.5f;
        float run = Mathf.Max(0.8f, length) * overlap;

        // 시야를 막는 실제 역할(콜라이더)은 그대로 두고, 눈에 보이는 수풀 판만 뺄 수 있게
        // 한다 - UnicornApproach 의 마지막 굴곡이 넓어진 광장 쪽으로 접히면서, 접근로용
        // 가림막(Tall) 몇 토막이 광장 바닥 위에 초록 벽처럼 보이는 문제가 있었다(사용자 확인).
        if (visible) HedgeSegment(parent, name, mid, height, run, rot, index);
        HedgeSolid(solids, name + "_Solid", mid, height, run, rot, HedgeThickness);
    }

    /// <summary>수풀 몸통 두께 - 보이는 것과 막는 것이 같은 값을 쓴다.</summary>
    private const float HedgeThickness = 0.78f;

    /// <summary>
    /// 수풀을 막아 주는 단순 상자.
    ///
    /// 보이는 수풀 메시에 콜라이더를 켜지 않고 따로 세운다.
    /// 그래야 나중에 VARCO3D 수풀 프리팹으로 비주얼만 갈아끼워도
    /// 충돌 모양과 통행 검증 결과가 그대로 유지된다.
    /// (수풀 FBX 에 MeshCollider 를 거는 방식은 비용이 크고, 잎 사이 틈에 플레이어가 낀다)
    /// </summary>
    private static void HedgeSolid(Transform parent, string name, Vector3 basePos,
                                   float height, float length, Quaternion rot, float thickness)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = basePos + Vector3.up * (height * 0.5f);
        go.transform.rotation = rot;
        go.transform.localScale = new Vector3(thickness, height, length);
        go.AddComponent<BoxCollider>();
    }

    private static void HedgeSegment(Transform parent, string name, Vector3 basePos, float height,
                                     float length, Quaternion rot, int index)
    {
        // 색은 네댓 칸(약 4m)마다 한 번만 바꾼다.
        // 한 칸마다 바꾸면 줄무늬가 생겨서 수풀이 아니라 초록 슬래브를 세워 둔 것처럼 보인다.
        Material mat = ((index / 5) % 3) switch
        {
            0 => MatLeafDeep,
            1 => MatLeafAlt,
            _ => MatLeaf,
        };

        Prim(PrimitiveType.Cube, name, parent, basePos + Vector3.up * (height * 0.5f),
             new Vector3(0.78f, height, length), mat, collider: false, rot: rot);

        // 다듬은 수풀의 윗동 - 끊기지 않게 모든 칸에 얹어 하나의 밝은 능선으로 보이게 한다
        Prim(PrimitiveType.Cube, name + "_Top", parent, basePos + Vector3.up * (height + 0.07f),
             new Vector3(0.96f, 0.22f, length), MatLeafSoft, rot: rot);
    }

    // ==========================================================
    // 배경 숲무리
    //
    // 초록 벽을 걷어내면 상자 안이 통째로 트여서, START 에서 유니콘까지
    // 대각선으로 시야가 뚫린다. 벽을 다시 세우는 대신
    // "정원 저쪽에 있는 나무 무리" 로 그 대각선을 끊는다.
    //
    // 자리는 길이 지나가지 않는 상자 왼쪽 빈 땅이다.
    // 벽이 아니라 정원의 일부라서, 플레이어에게는 그냥 배경으로 읽힌다.
    // ==========================================================

    // ==========================================================
    // 공예 온실 (CraftGardenGreenhouse.fbx)
    //
    // 모델 기준
    //   - 바깥 크기 7.03(X) x 5.21(Y) x 9.09(Z)
    //   - 피벗은 바닥 한가운데, 바닥 윗면이 y = 0
    //   - 출입구는 로컬 -Z 면 (2.0 x 2.45m)
    //
    // 길 왼쪽에 놓되, 출입구가 "걸어오는 쪽 + 길 쪽" 을 함께 보도록 비스듬히 돌린다.
    // 길과 나란히 놓으면 폭 7.03m 만 길 쪽으로 쓰기 때문에
    // 리본 가장자리와 이탈 방지 벽 사이(약 8.5m)에 들어간다.
    // ==========================================================
    private const string GreenhouseFbxPath =
        "Assets/BrightDream/Art/CraftGreenhouse/CraftGardenGreenhouse.fbx";

    /// <summary>온실 중심이 놓이는 길 기준 좌우 위치(음수 = 왼쪽).</summary>
    private const float GreenhouseLateral = -9.5f;

    /// <summary>온실을 감싸는 "정원 만" 반지름 - 이탈 방지 벽이 여기를 피해 돌아간다.</summary>
    private const float GreenhouseBayRadius = 7.2f;

    /// <summary>온실이 놓이는 지점의 s.</summary>
    private static float GreenhouseS => InArea("GreenhousePond", 0.28f);

    /// <summary>온실 중심 - 배치 전에도 계산할 수 있어야 벽을 미리 우회시킬 수 있다.</summary>
    private static Vector3 GreenhouseCenter => At(GreenhouseS, GreenhouseLateral);

    // 예전 프리미티브 온실은 출입구가 로컬 +X, 즉 SpineRight(길 쪽) 정면을 보고 있었다.
    // 새 온실도 같은 방향으로 맞춘다. 산책로를 걷다가 옆을 보면 입구가 정면으로 보인다.

    private static GameObject BuildCraftGreenhouse(Transform parent, float s)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GreenhouseFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 온실 FBX 를 찾지 못했다 - " + GreenhouseFbxPath);
            return null;
        }

        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = "CraftGardenGreenhouse";
        go.transform.position = GreenhouseCenter;
        go.transform.localScale = Vector3.one;

        // 이 모델의 출입구는 로컬 -Z 면에 있다.
        // 예전 온실과 같이 길 쪽(SpineRight)을 정면으로 보게 하려면 로컬 -Z 가 그쪽을 향해야 한다.
        Vector3 entranceDir = SpineRight(s);
        go.transform.rotation = Quaternion.LookRotation(-entranceDir);

        BuildGreenhouseColliders(go.transform);

        // 온실이 주인공이 되도록 둘레 조경을 비운다
        // 온실 위에만 안 심으면 된다. 너무 크게 비우면 온실이 허허벌판에 놓인 것처럼 보인다.
        Vector3 c = go.transform.position;
        NoTreeZones.Add(new Vector4(c.x, c.y, c.z, 5.6f));
        KeepClear.Add(new Vector4(c.x, c.y, c.z, 4.6f));

        // 출입구 앞 접근 공간도 비워 둔다
        Vector3 porch = c + entranceDir * 5.6f;
        KeepClear.Add(new Vector4(porch.x, porch.y, porch.z, 2.2f));

        return go;
    }

    /// <summary>
    /// 온실 콜라이더.
    ///
    /// MeshCollider 는 쓰지 않는다. 유리 패널까지 전부 메시로 잡으면
    /// 안에서 걸리는 자리가 생기고 비용도 크다.
    /// 바닥 / 사방 벽 / 문틀만 단순 BoxCollider 로 세우고, 문 폭은 그대로 비워 둔다.
    /// </summary>
    private static void BuildGreenhouseColliders(Transform house)
    {
        // 모델 로컬 기준 치수
        const float halfX = 3.47f;      // 벽 안쪽까지
        const float frontZ = -4.45f;    // 출입구가 있는 면
        const float backZ = 4.45f;
        const float wallH = 3.2f;       // 사람이 넘지 못할 높이면 충분하다
        const float thick = 0.22f;
        const float doorHalf = 1.0f;    // 출입구 2.0m
        const float doorH = 2.45f;

        var box = Child(house, "Colliders");

        // 바닥
        AddBox(box, "Floor", new Vector3(0f, -0.09f, 0f), new Vector3(7.0f, 0.18f, 9.0f));

        // 옆벽 두 장
        AddBox(box, "Wall_Left", new Vector3(-halfX, wallH * 0.5f, 0f), new Vector3(thick, wallH, 8.9f));
        AddBox(box, "Wall_Right", new Vector3(halfX, wallH * 0.5f, 0f), new Vector3(thick, wallH, 8.9f));

        // 뒷벽
        AddBox(box, "Wall_Back", new Vector3(0f, wallH * 0.5f, backZ), new Vector3(7.0f, wallH, thick));

        // 앞벽 - 가운데 출입구만 비운다
        float sideW = halfX - doorHalf;
        float sideC = doorHalf + sideW * 0.5f;
        AddBox(box, "Wall_Front_L", new Vector3(-sideC, wallH * 0.5f, frontZ), new Vector3(sideW, wallH, thick));
        AddBox(box, "Wall_Front_R", new Vector3(sideC, wallH * 0.5f, frontZ), new Vector3(sideW, wallH, thick));

        // 문 위 상인방 - 문틀 위로는 막는다
        AddBox(box, "Door_Lintel", new Vector3(0f, doorH + (wallH - doorH) * 0.5f, frontZ),
               new Vector3(doorHalf * 2f, wallH - doorH, thick));
    }

    // ==========================================================
    // 공예 울타리 (07_fence_picket.fbx)
    //
    // identity 회전으로 인스턴스를 하나 띄워서 실측한 결과 (Import 축 보정이 꺼져 있어
    // Blender Z-up 원본 축이 그대로 남아 있다 - .fbx.meta 의 bakeAxisConversion: 0):
    //   로컬 +X = 길이 1.63m (판자를 이어붙이는 방향)
    //   로컬 +Y = 두께 0.18m
    //   로컬 +Z = 높이 1.10m
    // 즉 모델이 "누워' 있는 상태 - 로컬 Z(높이)가 Unity 의 Up(Y)이 아니다.
    // Facing(s) 는 로컬 +Z 를 진행 방향(접선)에, 로컬 +Y 를 월드 Up 에 맞추는 LookRotation 이므로,
    // Facing(s) 만으로는 로컬 Z 축(실제 높이)이 절대 세워지지 않는다 - Y 축 회전만 추가해서는
    // (예전 Euler(0,90,0) 시도) 눕는 방향만 바뀔 뿐 세워지지 않았던 이유가 이것이다.
    //
    // 그래서 Facing(s) 앞에 축 자체를 재배치하는 보정 회전을 곱한다.
    //   로컬 X(길이)  -> 중간 Z  (Facing 이 중간 Z 를 진행 방향에 맞춘다)
    //   로컬 Y(두께)  -> 중간 X  (Facing 이 중간 X 를 옆(경계 안/밖) 방향에 맞춘다)
    //   로컬 Z(높이)  -> 중간 Y  (Facing 이 중간 Y 를 월드 Up 에 맞춘다)
    // 이 재배치가 정확히 Quaternion.LookRotation(Vector3.up, Vector3.right) 이다
    // (로컬 Z -> up, 로컬 Y -> right 로 보내는 회전을 요청하면 나머지 로컬 X 는 자동으로 forward 로 간다).
    //
    // 주의: 이 FBX 는 루트 노드 자체에 scale=100 이 구워져 있다(원본 좌표가 워낙 작아서
    // Blender 쪽에서 100배로 내보낸 것으로 보인다). InstantiatePrefab 이 만든 인스턴스의
    // localScale 을 여기서 (1,1,1) 로 다시 덮어쓰면 실제로는 100배 축소돼 안 보이게 된다.
    // "원본 스케일 유지" 는 이 100배 자체를 그대로 둔다는 뜻이라 localScale 은 건드리지 않는다.
    //
    // 예전 프리미티브 Picket/Rail 은 지우지 않고 꺼 두어서 언제든 비교할 수 있게 남긴다.
    // ==========================================================

    /// <summary>
    /// 모델 로컬 축 -> 진행방향/Up/옆 재배치 보정 (identity 인스턴스 실측으로 확인, 위 주석 참고).
    /// Facing(s) 앞에 곱해서 쓴다: Facing(s) * FencePicketAxisFix.
    /// </summary>
    private static readonly Quaternion FencePicketAxisFix = Quaternion.LookRotation(Vector3.up, Vector3.right);
    private const string FencePicketFbxPath = "Assets/BrightDream/Models/07_fence_picket.fbx";
    private const float FencePicketLength = 1.63f;

    private static void BuildCraftFenceRun(Transform parent, string name, float fromS, float toS, float side, float offset)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FencePicketFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 울타리 FBX 를 찾지 못했다 - " + FencePicketFbxPath);
            return;
        }

        // s 간격이 아니라 실제 월드 호 길이 기준으로 배치한다.
        // RibbonHalfWidth(s) 가 구간마다 달라서, 같은 s 간격도 곡선/폭 변화 지점에서는
        // 실제 거리가 크게 벌어지거나 좁아진다 - 판자를 이어붙이려면 s 가 아니라
        // 실측 거리로 맞춰야 틈/겹침이 고르게 나온다.
        const float sampleStep = 0.05f;
        var sSamples = new List<float>();
        var cumLen = new List<float>();
        float acc = 0f;
        Vector3 prevPos = At(fromS, (RibbonHalfWidth(fromS) + offset) * side);
        sSamples.Add(fromS); cumLen.Add(0f);
        for (float s = fromS + sampleStep; s < toS; s += sampleStep)
        {
            Vector3 pos = At(s, (RibbonHalfWidth(s) + offset) * side);
            acc += Vector3.Distance(prevPos, pos);
            sSamples.Add(s); cumLen.Add(acc);
            prevPos = pos;
        }
        Vector3 endPos = At(toS, (RibbonHalfWidth(toS) + offset) * side);
        acc += Vector3.Distance(prevPos, endPos);
        sSamples.Add(toS); cumLen.Add(acc);

        float totalLength = acc;
        if (totalLength < 0.05f) return;
        int count = Mathf.Max(1, Mathf.RoundToInt(totalLength / FencePicketLength));
        float step = totalLength / count;

        System.Func<float, float> sAtLength = (targetLen) =>
        {
            for (int i = 1; i < cumLen.Count; i++)
            {
                if (cumLen[i] >= targetLen)
                {
                    float segLen = cumLen[i] - cumLen[i - 1];
                    float t = segLen > 0.0001f ? (targetLen - cumLen[i - 1]) / segLen : 0f;
                    return Mathf.Lerp(sSamples[i - 1], sSamples[i], t);
                }
            }
            return sSamples[sSamples.Count - 1];
        };

        var run = Child(parent, name + "_Craft");
        for (int i = 0; i < count; i++)
        {
            float s = sAtLength(step * (i + 0.5f));
            Vector3 spot = At(s, (RibbonHalfWidth(s) + offset) * side);
            Quaternion rot = Facing(s) * FencePicketAxisFix;

            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, run);
            go.name = $"FencePanel_{i:D2}";
            go.transform.position = spot;
            go.transform.rotation = rot;
        }
    }

    // ==========================================================
    // 공예 수풀 (19_bush.fbx)
    //
    // identity 인스턴스로 실측한 결과:
    //   - 이 FBX 는 Fence 와 달리 import 시 이미 "서 있는" 회전이 구워져 있다
    //     (로컬 eulerAngles = (270.02, 0, 0)). 축을 재배치할 필요가 없어 이 회전을 그대로 곱해서 쓴다.
    //   - scale 100 원본 그대로일 때 실측 월드 Bounds: 지름 1.37m(반지름 0.685m) x 높이 0.80m, 바닥에 정확히 붙는다.
    //   - 콜라이더는 0개. Hedge(HedgeSolid) 와 같은 이유로 잎사귀 메시에 MeshCollider 를 걸지 않고
    //     별도의 단순 SphereCollider 를 세운다 (비용 + 잎 사이 끼임 방지).
    // ==========================================================
    private const string BushFbxPath = "Assets/BrightDream/Models/19_bush.fbx";

    /// <summary>identity 인스턴스 실측값 - scale 100 기준 반지름 / 높이(m).</summary>
    private const float BushNativeRadius = 0.685f;
    private const float BushNativeHeight = 0.80f;

    /// <summary>
    /// 기존 Primitive 수풀 자리에 공예 수풀을 세운다.
    ///
    /// pos / radius 는 호출부(MakeBush)가 이미 계산해 둔 값을 그대로 받는다 - 위치/분포 의도는 손대지 않는다.
    /// 반지름은 19_bush 실측 크기를 기준으로 스케일해서 맞추고, 아주 약한 변주만 더한다.
    /// </summary>
    private static void BuildCraftBush(Transform parent, string name, Vector3 pos, float radius)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BushFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 수풀 FBX 를 찾지 못했다 - " + BushFbxPath);
            return;
        }

        var rng = new System.Random(name.GetHashCode());
        float sizeMul = (radius / BushNativeRadius) * Lerp(0.94f, 1.06f, (float)rng.NextDouble());
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

        var root = Child(parent, name + "_Craft");
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        // 걷는 자리를 막던 원래 Primitive 콜라이더를 대신한다 - 시각 메시와는 분리된 단순 구.
        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = Vector3.up * (BushNativeHeight * sizeMul * 0.5f);
        var sc = col.AddComponent<SphereCollider>();
        sc.radius = BushNativeRadius * sizeMul;
    }

    /// <summary>
    /// 온실과 겹치는 조경을 걷어낸다.
    ///
    /// 언덕 / 수풀 / 꽃밭은 온실이 놓이기 전에 정해진 규칙대로 깔리기 때문에
    /// 온실 자리와 겹치는 것들이 생긴다. 온실이 우선이므로 겹치는 쪽을 지운다.
    /// 조각 하나씩이 아니라 무리 단위로 지워야 반쪽만 남는 일이 없다.
    /// </summary>
    private static void ClearGreenhouseOverlap(Transform root, GameObject greenhouse)
    {
        if (greenhouse == null) return;

        var rends = greenhouse.GetComponentsInChildren<MeshRenderer>(true);
        if (rends.Length == 0) return;

        // 월드 AABB 로 자르면 온실 둘레가 네모나게 휑해진다.
        // 온실이 실제로 서 있는 자리(로컬 박스)만 기준으로 삼고 여유도 조금만 준다.
        var houseT = greenhouse.transform;
        const float halfX = 3.51f + 0.45f;
        const float halfZ = 4.55f + 0.45f;

        string[] prefixes =
        {
            "Hill_", "BackHill_", "RidgeHill_", "GroveHill_",
            "Bush_", "BackBush_", "EdgeBush_",
            "Tree_", "BackTree_", "PhotoTree",
            "Bed_", "FlowerBed", "EdgeBed_", "BackBed_",
            "Meadow_", "BackMeadow_", "Flower_", "Star_",
            "Picket_", "Rail_", "RailLow_", "Fence_",
            "HedgeL_", "HedgeR_", "Lamp_", "Crate_",
        };

        var doomed = new List<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t.IsChildOf(greenhouse.transform)) continue;

            bool match = false;
            foreach (var p in prefixes)
            {
                if (t.name.StartsWith(p)) { match = true; break; }
            }
            if (!match) continue;

            var rr = t.GetComponentsInChildren<Renderer>(true);
            if (rr.Length == 0) continue;
            var b = rr[0].bounds;
            for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);

            // 조경 덩어리가 온실 자리 안으로 실제로 파고드는지만 본다
            Vector3 lp = houseT.InverseTransformPoint(b.center);
            Vector3 le = b.extents;
            float reach = Mathf.Max(le.x, le.z) * 0.6f;      // 덩어리 반경 근사
            if (Mathf.Abs(lp.x) - reach < halfX && Mathf.Abs(lp.z) - reach < halfZ)
            {
                doomed.Add(t.gameObject);
            }
        }

        int removed = 0;
        foreach (var go in doomed)
        {
            if (go == null) continue;
            Object.DestroyImmediate(go);
            removed++;
        }
        if (removed > 0) Debug.Log($"[BrightDream] 온실과 겹쳐 걷어낸 조경 : {removed}개");
    }

    private static void AddBox(Transform parent, string name, Vector3 localPos, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        var bc = go.AddComponent<BoxCollider>();
        bc.size = size;
    }

    /// <summary>
    /// 배경 언덕 능선이 지나는 길 (월드 x, z).
    ///
    /// 나무 무리만으로는 그루 사이로 시선이 새어 나간다.
    /// 그래서 상자 왼쪽 빈 땅에 종이 언덕을 겹쳐 깔아 하나의 능선을 만든다.
    /// 초록 벽과 달리 둥글고 낮게 깔리기 때문에, 산책로에서는
    /// "정원 저쪽이 언덕으로 솟아 있다" 로 읽힌다.
    /// </summary>
    private static readonly Vector2[] BackdropRidge =
    {
        new Vector2( 1.0f, 27.0f),
        new Vector2( 3.5f, 31.0f),
        new Vector2( 5.5f, 35.0f),
        new Vector2( 6.8f, 39.0f),
        new Vector2( 7.5f, 43.0f),
        new Vector2( 8.0f, 47.0f),
        new Vector2( 7.5f, 51.0f),
    };

    private static void BuildBackdropRidge(Transform parent, System.Func<Vector3, float> outsideCorridor,
                                           System.Random rng)
    {
        var ridge = Child(parent, "BackdropRidge");
        int index = 0;

        for (int i = 1; i < BackdropRidge.Length; i++)
        {
            Vector2 a = BackdropRidge[i - 1], b = BackdropRidge[i];
            float span = Vector2.Distance(a, b);
            int steps = Mathf.Max(2, Mathf.RoundToInt(span / 1.8f));

            for (int k = 0; k < steps; k++)
            {
                Vector2 p = Vector2.Lerp(a, b, k / (float)steps);
                Vector3 spot = new Vector3(p.x + Lerp(-1.0f, 1.0f, (float)rng.NextDouble()), 0f,
                                           p.y + Lerp(-1.0f, 1.0f, (float)rng.NextDouble()));
                if (outsideCorridor(spot) < 1.2f) continue;

                // 구는 가장자리로 갈수록 낮아진다. 능선이 시선을 확실히 끊으려면
                // 봉우리만 높은 게 아니라 옆구리도 눈높이보다 한참 높아야 한다.
                float r = Lerp(5.0f, 7.0f, (float)rng.NextDouble());
                Prim(PrimitiveType.Sphere, $"RidgeHill_{index:D2}", ridge,
                     spot + Vector3.down * 2.0f,
                     new Vector3(r * 2f, Lerp(12.0f, 16.0f, (float)rng.NextDouble()), r * 2f),
                     PastelHill(index), collider: true);
                index++;
            }
        }
    }

    /// <summary>배경 숲무리 (중심 x, 중심 z, 나무 수, 퍼지는 반경).</summary>
    private static readonly (float X, float Z, int Trees, float Radius)[] BackdropGroves =
    {
        // START 에서 유니콘으로 향하는 대각선이 z = 30~55 사이 왼쪽 빈 땅을 지난다.
        // 그 위에 무리를 얹어 시선을 끊는다. 산책로에서는 "저 너머 숲" 으로만 보인다.
        (4.5f, 31.0f, 7, 5.0f),
        (6.5f, 35.0f, 6, 4.2f),
        (7.5f, 39.5f, 8, 4.6f),
        (8.5f, 43.5f, 7, 4.2f),
        (8.0f, 47.5f, 7, 4.2f),
        (4.0f, 56.0f, 5, 4.0f),
        (-1.0f, 44.0f, 4, 4.5f),
        (0.0f, 66.0f, 4, 4.5f),
    };

    private static void BuildBackdropGroves(Transform parent, System.Func<Vector3, float> outsideCorridor,
                                            System.Random rng)
    {
        var grove = Child(parent, "BackdropGroves");
        int index = 0;

        foreach (var g in BackdropGroves)
        {
            for (int i = 0; i < g.Trees; i++)
            {
                float angle = (i / (float)g.Trees) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.9f;
                float radius = Lerp(g.Radius * 0.25f, g.Radius, (float)rng.NextDouble());
                Vector3 spot = new Vector3(g.X + Mathf.Cos(angle) * radius, 0f, g.Z + Mathf.Sin(angle) * radius);

                if (outsideCorridor(spot) < 1.5f) continue;   // 길 위에는 두지 않는다

                float treeHeight = Lerp(5.4f, 6.8f, (float)rng.NextDouble());
                float canopy = Lerp(2.2f, 2.9f, (float)rng.NextDouble());
                MakeTree(grove, $"BackTree_{index:D2}", spot, treeHeight, canopy,
                         index % 3 == 0 ? MatLeaf : index % 3 == 1 ? MatLeafAlt : MatLeafDeep, rng);

                // 수관은 원래 콜라이더가 없어서 시야 검증에 잡히지 않는다.
                // 배경 숲무리는 시선을 끊는 것이 일인 만큼 부피를 갖게 해 둔다.
                // (높이 4m 이상이라 길 통행 검사에는 영향이 없다)
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blocker.name = $"BackTree_{index:D2}_Volume";
                blocker.transform.SetParent(grove, false);
                blocker.transform.position = spot + Vector3.up * (treeHeight * 0.92f);
                blocker.transform.localScale = Vector3.one * canopy * 2f;
                Object.DestroyImmediate(blocker.GetComponent<MeshRenderer>());
                index++;
            }

        }
    }

    // ==========================================================
    // 배경 정원 - 상자 안에서 길이 쓰지 않는 공간
    // ==========================================================
    /// <summary>
    /// 상자 배경에 깔리는 장식용 둥근 언덕(BackHill_)을 켤지.
    ///
    /// 팀원 페이퍼크래프트 에셋을 순차적으로 배치하는 동안, 이 언덕들이 나무/수풀/꽃/온실/연못과
    /// 자주 겹쳐서 실제 배치 판단을 방해한다는 요청으로 일단 꺼 둔다.
    /// 콜라이더가 없는 순수 시각 장식이라 꺼도 Path Clearance / 시야 차단 검증에는 영향이 없다.
    /// 다시 켜려면 이 값만 true 로 되돌리면 된다 - 나머지 로직/확률 분포는 그대로 남아 있다.
    /// </summary>
    private const bool RectBackdropHillsEnabled = false;

    private static void BuildRectBackdrop(Transform parent)
    {
        var rng = new System.Random(2024);
        var backdrop = Child(parent, "RectBackdrop");
        var rect = RectFootprint();

        // 중심선을 미리 샘플링해 두고 "길에서 얼마나 떨어졌는지" 를 빠르게 잰다
        var samples = new List<Vector3>();
        var halves = new List<float>();
        for (float s = 0f; s <= spineLength; s += 1.0f)
        {
            samples.Add(SpinePoint(s));
            halves.Add(CorridorHalfWidth(s));
        }

        float DistanceOutsideCorridor(Vector3 p)
        {
            float worst = float.MaxValue;
            for (int i = 0; i < samples.Count; i++)
            {
                Vector2 d = new Vector2(p.x - samples[i].x, p.z - samples[i].z);
                worst = Mathf.Min(worst, d.magnitude - halves[i]);
            }
            return worst;   // 양수면 길 바깥
        }

        BuildBackdropGroves(backdrop, DistanceOutsideCorridor, rng);

        int index = 0;
        for (float x = rect.min.x + 2f; x < rect.max.x - 2f; x += 3.2f)
        {
            for (float z = rect.min.z + 2f; z < rect.max.z - 2f; z += 3.2f, index++)
            {
                Vector3 spot = new Vector3(x + Lerp(-1.1f, 1.1f, (float)rng.NextDouble()), 0f,
                                           z + Lerp(-1.1f, 1.1f, (float)rng.NextDouble()));

                float outside = DistanceOutsideCorridor(spot);
                if (outside < 1.6f) continue;   // 길이거나 울타리에 너무 가깝다

                // 벽에 가까울수록 낮은 언덕, 가운데 남는 땅은 잔디밭 위주
                int roll = rng.Next(100);
                if (roll < 34)
                {
                    if (RectBackdropHillsEnabled)
                    {
                        // 반원 돔처럼 솟지 않게 넓고 납작하게 깐다.
                        // 멀리서 "정원 바닥이 살짝 기복이 있다" 정도로만 읽히면 된다.
                        float r = Lerp(2.2f, 4.0f, (float)rng.NextDouble());
                        Prim(PrimitiveType.Sphere, $"BackHill_{index:D3}", backdrop,
                             spot + Vector3.down * Lerp(1.5f, 2.2f, (float)rng.NextDouble()),
                             new Vector3(r * 2f, Lerp(2.6f, 3.6f, (float)rng.NextDouble()), r * 2.2f),
                             PastelHill(index));
                    }
                    // 꺼져 있으면 이 칸은 다른 장식으로 넘기지 않고 그대로 빈 잔디로 남긴다.
                }
                else if (roll < 56)
                {
                    Prim(PrimitiveType.Cylinder, $"BackMeadow_{index:D3}", backdrop,
                         spot + Vector3.up * 0.014f,
                         new Vector3(Lerp(3.4f, 5.8f, (float)rng.NextDouble()), 0.014f,
                                     Lerp(3.4f, 5.8f, (float)rng.NextDouble())),
                         rng.Next(2) == 0 ? MatMeadow : MatGrassAlt);
                }
                else if (roll < 70)
                {
                    MakeBush(backdrop, $"BackBush_{index:D3}", spot,
                             Lerp(0.8f, 1.5f, (float)rng.NextDouble()),
                             rng.Next(2) == 0 ? MatLeafSoft : MatLeafAlt);
                }
                else if (roll < 80)
                {
                    MakeFlowerBed(backdrop, $"BackBed_{index:D3}", spot,
                                  Lerp(1.0f, 1.8f, (float)rng.NextDouble()), 4 + rng.Next(4), rng);
                }
                // 나머지 20% 는 아무것도 두지 않는다 - 빈 잔디가 있어야 상자가 답답하지 않다
            }
        }
    }
}
