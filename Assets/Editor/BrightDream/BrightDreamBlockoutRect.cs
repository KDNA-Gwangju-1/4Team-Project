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
                     FeltTileMaterial(ix, iz), collider: true);
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

    /// <summary>
    /// 타일마다 톤 + 변주를 해시로 골라 체크무늬 대신 패치워크처럼 섞는다.
    /// 민트/세이지 비중을 높이고 흰색에 가까운 Cream 비중은 낮춘다. 톤과 변주를 서로 다른
    /// 해시로 뽑아서 같은 톤이 연달아 나와도 노이즈 위상/박음질 노출 변이 달라 보이게 한다.
    /// (ix, iz) 가 같으면 항상 같은 결과 - 재빌드해도 바닥이 흔들리지 않는다.
    /// </summary>
    private static Material FeltTileMaterial(int ix, int iz)
    {
        // 톤 인덱스(FeltToneColors 순서: Mint, Sage, Pale, Cream) 가중치 - Mint/Sage 3, Pale 2, Cream 1
        int[] toneWeights = { 0, 0, 0, 1, 1, 1, 2, 2, 3 };
        int hTone = (ix * 73856093) ^ (iz * 19349663);
        if (hTone < 0) hTone = -hTone;
        int tone = toneWeights[hTone % toneWeights.Length];

        int hVariant = (ix * 83492791) ^ (iz * 2038074743);
        if (hVariant < 0) hVariant = -hVariant;
        int variant = hVariant % FeltVariantCount;

        return MatFeltTiles[tone, variant];
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

    /// <summary>
    /// Scene 에서 담당자가 직접 손으로 미세 조정한 보정값(수동 배치 기준값).
    /// At(GreenhouseS, GreenhouseLateral) 로 계산한 기본 자리에서 이 만큼만 더 옮긴다.
    /// </summary>
    private static readonly Vector3 GreenhouseManualOffset = new Vector3(-0.637761f, 0.080000f, -0.097376f);

    /// <summary>온실 중심 - 배치 전에도 계산할 수 있어야 벽을 미리 우회시킬 수 있다.</summary>
    private static Vector3 GreenhouseCenter => At(GreenhouseS, GreenhouseLateral) + GreenhouseManualOffset;

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

    // ==========================================================
    // 공예 나무 (16_tree_large.fbx / 17_tree_medium.fbx / 18_tree_conifer.fbx)
    //
    // identity 인스턴스로 실측한 결과 - 셋 다 Bush 와 같은 패턴:
    //   - root scale 100 원본, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로 "서 있게" 구워져 있어
    //     축 재배치가 필요 없다.
    //   - 16_tree_large: 지름 5.69x3.65m, 높이 6.00m. 17_tree_medium: 지름 2.02x2.74m, 높이 4.00m.
    //     18_tree_conifer: 지름 2.05x2.06m, 높이 5.00m(뾰족한 침엽수 - 밀집 구역 변화감용).
    //   - 콜라이더는 0개. 원래 MakeTree 도 캐노피에는 콜라이더가 없고 몸통(Trunk)에만
    //     얇은 실린더 콜라이더를 뒀다 - MeshCollider 대신 그와 같은 단순 몸통 콜라이더만 세운다.
    // ==========================================================
    private const string TreeLargeFbxPath = "Assets/BrightDream/Models/16_tree_large.fbx";
    private const string TreeMediumFbxPath = "Assets/BrightDream/Models/17_tree_medium.fbx";
    private const string TreeConiferFbxPath = "Assets/BrightDream/Models/18_tree_conifer.fbx";
    private const float TreeLargeNativeHeight = 6.00f;
    private const float TreeMediumNativeHeight = 4.00f;
    private const float TreeConiferNativeHeight = 5.00f;

    /// <summary>원래 MakeTree 트렁크 반지름(0.28m 스케일의 실린더 반지름 0.14m)과 같은 근사치.</summary>
    private const float TreeNativeTrunkRadius = 0.14f;

    // 나무4.fbx - 4번째 나무 종. 팀 제공 3종(large/medium/conifer)과 export 관례가 다르다:
    // root scale=1(100 아님), centered pivot(min.y=-0.5) - 그네2/Garden Arch 와 같은 패턴.
    // 실측 바운드 0.89 x 1.00 x 0.56m. 텍스처는 embedded 추출로 이미 Tree4.mat 에 연결되어 있다.
    private const string TreeFourFbxPath = "Assets/BrightDream/Models/나무4.fbx";
    private const float TreeFourNativeHeight = 1.00f;
    private const float TreeFourNativeTrunkRadius = 0.10f;   // 폭(0.89m) 대비 어림값

    /// <summary>
    /// 기존 Primitive 나무 자리에 공예 나무를 세운다.
    /// pos / height 는 호출부(MakeTree)가 이미 계산해 둔 값을 그대로 받는다 - 위치/분포 의도는 손대지 않는다.
    /// </summary>
    private static void BuildCraftTree(Transform parent, string name, Vector3 pos, float height, string asset)
    {
        bool isTreeFour = asset == "tree4";
        string path = asset == "large" ? TreeLargeFbxPath
                     : asset == "conifer" ? TreeConiferFbxPath
                     : isTreeFour ? TreeFourFbxPath
                     : TreeMediumFbxPath;
        float nativeHeight = asset == "large" ? TreeLargeNativeHeight
                            : asset == "conifer" ? TreeConiferNativeHeight
                            : isTreeFour ? TreeFourNativeHeight
                            : TreeMediumNativeHeight;

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 나무 FBX 를 찾지 못했다 - " + path);
            return;
        }

        var rng = new System.Random(name.GetHashCode() ^ asset.GetHashCode());
        float sizeMul = (height / nativeHeight) * Lerp(0.95f, 1.05f, (float)rng.NextDouble());
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());
        float fullHeight = nativeHeight * sizeMul;

        var root = Child(parent, name + "_Craft");
        // tree4 는 centered pivot 이라 38_grass 테스트와 같은 이유로 높이 절반만큼 들어 올려야 한다.
        root.position = isTreeFour ? pos + Vector3.up * (fullHeight * 0.5f) : pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        // MeshCollider 금지 - 캐노피는 콜라이더 없이, 몸통 자리만 단순 캡슐로 막는다.
        // tree4 는 root 자체가 이미 중앙 높이에 있으므로 콜라이더는 root 기준 오프셋 0.
        var col = new GameObject("TrunkCollider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = isTreeFour ? Vector3.zero : Vector3.up * (fullHeight * 0.5f);
        var cc = col.AddComponent<CapsuleCollider>();
        cc.height = fullHeight;
        cc.radius = (isTreeFour ? TreeFourNativeTrunkRadius : TreeNativeTrunkRadius) * sizeMul;
    }

    // ==========================================================
    // 꽃 자산 테스트 (20_daisy_cluster.fbx / 21_flower_large.fbx)
    //
    // identity 인스턴스로 실측한 결과 - Bush/Tree 와 같은 패턴:
    //   - 둘 다 root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로
    //     "서 있게" 구워져 있어 축 재배치가 필요 없다.
    //   - 20_daisy_cluster: 낱개 꽃이 아니라 여러 송이가 뭉친 "군집" 메쉬 하나 -
    //     실측 지름 0.35m x 높이 0.35m. Base Fill(반복 채움)용.
    //   - 21_flower_large: 실측 폭 1.00m x 높이 1.60m x 깊이 0.50m - 원래 크기 그대로
    //     심으면 화단을 뒤덮으므로 항상 큰 폭으로 축소해서 단독 초점(Accent)으로만 쓴다.
    //   - 콜라이더는 기존 MakeFlower 와 동일하게 없음 - 걷는 데 걸리지 않는 장식.
    //   - 재질은 각각 이미 텍스처가 입혀진 Standard 재질 하나뿐이다. 이걸 별도 "3번째 종"으로
    //     세지 않고, 인스턴스별 MaterialPropertyBlock 색조 틴트로만 미세한 색 변주를 준다.
    //   - GreenhousePond 한정 교체다. AreaNameAt(s) == "GreenhousePond" 로 판정되는 자리만
    //     이 함수들로 만들고, 다른 구역은 그대로 프리미티브 MakeFlower/MakeFlowerBed 를 쓴다.
    //     (FlowerBed_A/B/C 뿐 아니라 BuildPathsideLandscaping 의 길가 꽃무리, BuildRectBackdrop
    //     의 BackBed_ 화단도 GreenhousePond 구간을 지날 때는 이 함수로 분기한다.)
    // ==========================================================
    private const string DaisyClusterFbxPath = "Assets/BrightDream/Models/20_daisy_cluster.fbx";
    private const string FlowerLargeFbxPath = "Assets/BrightDream/Models/21_flower_large.fbx";
    private const float DaisyClusterNativeDiameter = 0.35f;
    private const float FlowerLargeNativeHeight = 1.60f;

    private static void TintFlowerInstance(GameObject visual, System.Random rng)
    {
        float hue = (float)rng.NextDouble();
        float sat = Lerp(0.05f, 0.14f, (float)rng.NextDouble());
        float val = Lerp(0.92f, 1.0f, (float)rng.NextDouble());
        Color tint = Color.HSVToRGB(hue, sat, val);

        var mpb = new MaterialPropertyBlock();
        foreach (var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", tint);
            r.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Type A - Base Fill. 20_daisy_cluster 를 화단/길가에 반복 배치한다.</summary>
    private static void BuildCraftFlowerDaisy(Transform parent, string name, Vector3 pos, System.Random rng,
                                               float diameterMin = 0.30f, float diameterMax = 0.48f)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DaisyClusterFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 데이지 군집 FBX 를 찾지 못했다 - " + DaisyClusterFbxPath);
            return;
        }

        float targetDiameter = Lerp(diameterMin, diameterMax, (float)rng.NextDouble());
        float sizeMul = targetDiameter / DaisyClusterNativeDiameter;
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

        var root = Child(parent, name);
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        TintFlowerInstance(visual, rng);
    }

    /// <summary>Type B - Accent / Ground Cluster. 21_flower_large 를 초점 자리에 축소해 세운다.</summary>
    private static void BuildCraftFlowerLarge(Transform parent, string name, Vector3 pos, System.Random rng,
                                               float heightMin = 0.55f, float heightMax = 0.85f)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FlowerLargeFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 큰 꽃 FBX 를 찾지 못했다 - " + FlowerLargeFbxPath);
            return;
        }

        float targetHeight = Lerp(heightMin, heightMax, (float)rng.NextDouble());
        float sizeMul = targetHeight / FlowerLargeNativeHeight;
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

        var root = Child(parent, name);
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        TintFlowerInstance(visual, rng);
    }

    // 꽃3.fbx - GreenhousePond 이외 구역(StartGarden/ClueWalk/WeaponLink/TutorialNook) 전용 Base Fill.
    // 그네2/나무4/Garden Arch 와 같은 export 관례: root scale=1, centered pivot(min.y=-0.5).
    // 실측 바운드 0.54 x 1.00 x 0.29m - 가늘고 큰 daisy_cluster 보다 홑겹 꽃대에 가깝다.
    // GreenhousePond 는 이미 확정된 결과라 손대지 않고, 다른 구역에만 이 종을 새로 쓴다.
    private const string FlowerThreeFbxPath = "Assets/BrightDream/Models/꽃3.fbx";
    private const float FlowerThreeNativeHeight = 1.00f;

    /// <summary>Type A(다른 구역용) - Base Fill. 꽃3 를 화단에 반복 배치한다.</summary>
    private static void BuildCraftFlowerThree(Transform parent, string name, Vector3 pos, System.Random rng)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FlowerThreeFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 꽃3 FBX 를 찾지 못했다 - " + FlowerThreeFbxPath);
            return;
        }

        float targetHeight = Lerp(0.32f, 0.52f, (float)rng.NextDouble());
        float sizeMul = targetHeight / FlowerThreeNativeHeight;
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

        var root = Child(parent, name);
        // centered pivot 보정 - 38_grass 테스트와 같은 이유.
        root.position = pos + Vector3.up * (targetHeight * 0.5f);

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        TintFlowerInstance(visual, rng);
    }

    /// <summary>
    /// 화단 하나를 실제 꽃 에셋으로 채운다. count 의 약 20%(최소 1개)를 Type B(Accent,
    /// flower_large, 중심에 가깝게)로, 나머지를 Type A(Base Fill)로 채운다.
    /// fillUsesFlowerThree=false 면 GreenhousePond 와 같은 daisy_cluster, true 면 다른 구역용
    /// 꽃3 를 Base Fill 로 쓴다 - GreenhousePond 는 이미 확정된 결과라 이 스위치로 건드리지 않는다.
    /// Soil 디스크는 기존과 동일하게 유지한다.
    /// </summary>
    private static void BuildCraftFlowerBedTest(Transform parent, string name, Vector3 center, float radius, int count, System.Random rng, bool fillUsesFlowerThree = false)
    {
        var bed = Child(parent, name);
        Prim(PrimitiveType.Cylinder, "Soil", bed, center + Vector3.up * 0.015f,
             new Vector3(radius * 2f, 0.015f, radius * 2f), MatLeafDeep);

        int accentCount = Mathf.Max(1, Mathf.RoundToInt(count * 0.2f));
        int fillCount = count - accentCount;

        for (int i = 0; i < accentCount; i++)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float distance = Lerp(radius * 0.15f, radius * 0.55f, (float)rng.NextDouble());
            Vector3 spot = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            BuildCraftFlowerLarge(bed, $"Accent_{i:D2}", spot, rng);
        }

        for (int i = 0; i < fillCount; i++)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float distance = (float)rng.NextDouble() * radius * 0.85f;
            Vector3 spot = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            if (fillUsesFlowerThree)
                BuildCraftFlowerThree(bed, $"Fill_{i:D2}", spot, rng);
            else
                BuildCraftFlowerDaisy(bed, $"Fill_{i:D2}", spot, rng);
        }
    }

    // ==========================================================
    // 가로등 자산 (25_lamp_post.fbx) - GreenhousePond 한정 교체
    //
    // identity 인스턴스로 실측한 결과 - Bush/Tree 와 같은 패턴:
    //   - root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로 "서 있게" 구워져 있다.
    //   - 실측 폭 1.03 x 1.05m, 높이 3.00m - 원래 프리미티브 MakeLamp(총 높이 약 3.24m,
    //     Base/Pole 에만 콜라이더) 와 거의 같은 스케일이라 축소/확대가 거의 필요 없다.
    //   - 메쉬 하나(기둥+갓 전체 일체형)라 MeshCollider 대신 기존과 같은 취지로 기둥 자리에만
    //     단순 CapsuleCollider 를 세운다.
    // ==========================================================
    private const string LampPostFbxPath = "Assets/BrightDream/Models/25_lamp_post.fbx";
    private const float LampPostNativeHeight = 3.00f;

    /// <summary>
    /// 기존 Primitive 가로등(MakeLamp) 자리에 공예 가로등을 세운다.
    /// pos 는 호출부가 이미 계산해 둔 값을 그대로 받는다 - 위치 의도는 손대지 않는다.
    /// </summary>
    private static void BuildCraftLamp(Transform parent, string name, Vector3 pos)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(LampPostFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 가로등 FBX 를 찾지 못했다 - " + LampPostFbxPath);
            return;
        }

        var rng = new System.Random(name.GetHashCode());
        float sizeMul = Lerp(0.95f, 1.05f, (float)rng.NextDouble());
        float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

        var root = Child(parent, name);
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        float poleHeight = LampPostNativeHeight * sizeMul;
        var col = new GameObject("PostCollider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = Vector3.up * (poleHeight * 0.5f);
        var cc = col.AddComponent<CapsuleCollider>();
        cc.height = poleHeight;
        cc.radius = 0.18f * sizeMul;
    }

    // ==========================================================
    // 벤치 자산 (26_bench.fbx) - ClueWalk 의 유일한 벤치(Bench_Seat) 한정 교체
    //
    // identity 인스턴스로 실측한 결과 - Bush/Tree 와 같은 패턴:
    //   - root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로 "서 있게" 구워져 있다.
    //   - 실측 폭 1.28 x 높이 0.90 x 깊이 0.78m - 원래 프리미티브 Bench_Seat(1.6 x 0.12 x 0.5,
    //     y=0.45 에 좌판만) 보다 등받이까지 있는 온전한 벤치라 높이가 크다.
    //   - 메쉬 하나(전체 일체형)라 MeshCollider 대신 벤치 실측 크기의 단순 BoxCollider 를 세운다.
    // ==========================================================
    private const string BenchFbxPath = "Assets/BrightDream/Models/26_bench.fbx";
    private const float BenchNativeWidth = 1.28f;
    private const float BenchNativeHeight = 0.90f;
    private const float BenchNativeDepth = 0.78f;

    /// <summary>
    /// 기존 Primitive 벤치(Bench_Seat) 자리에 공예 벤치를 세운다.
    /// pos / rot 은 호출부가 이미 계산해 둔 값을 그대로 받는다 - 위치/방향 의도는 손대지 않는다.
    /// </summary>
    private static void BuildCraftBench(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BenchFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 벤치 FBX 를 찾지 못했다 - " + BenchFbxPath);
            return;
        }

        var rng = new System.Random(name.GetHashCode());
        float sizeMul = Lerp(0.95f, 1.05f, (float)rng.NextDouble());

        var root = Child(parent, name);
        root.position = pos;
        root.rotation = rot;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = Vector3.up * (BenchNativeHeight * sizeMul * 0.5f);
        var bc = col.AddComponent<BoxCollider>();
        bc.size = new Vector3(BenchNativeWidth * sizeMul, BenchNativeHeight * sizeMul, BenchNativeDepth * sizeMul);
    }

    // ==========================================================
    // 크레이트 자산 (27_crate.fbx) - MakeCrate 자리 7곳(TutorialNook 1 + CombatArena 6) 전부 교체
    //
    // identity 인스턴스로 실측한 결과 - Bush/Tree 와 같은 패턴:
    //   - root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로 "서 있게" 구워져 있다.
    //   - 실측 폭 0.98 x 높이 0.80 x 깊이 0.82m.
    //   - CombatArena 의 Crate 는 "눈높이보다 낮은 엄폐물"이라는 게임플레이 의도가 있는
    //     오브젝트다 - 원래 MakeCrate(size) 는 정육면체 한 변 길이를 그대로 최종 높이로 썼으므로,
    //     여기서도 size 를 "목표 높이"로 취급해 실측 높이(0.80m) 기준으로 스케일해서 그 의도를
    //     정확히 유지한다(폭/깊이는 원본 비율을 따라간다 - 완전한 정육면체가 아니어도 무방).
    //   - 메쉬 하나(전체 일체형)라 MeshCollider 대신 실측 크기의 단순 BoxCollider 를 세운다.
    // ==========================================================
    private const string CrateFbxPath = "Assets/BrightDream/Models/27_crate.fbx";
    private const float CrateNativeWidth = 0.98f;
    private const float CrateNativeHeight = 0.80f;
    private const float CrateNativeDepth = 0.82f;

    /// <summary>
    /// 기존 Primitive 크레이트(MakeCrate) 자리에 공예 크레이트를 세운다.
    /// pos / size(목표 높이) / rotY 는 호출부가 이미 계산해 둔 값을 그대로 받는다 -
    /// 위치/회전/엄폐 높이 의도는 손대지 않는다.
    /// </summary>
    private static void BuildCraftCrate(Transform parent, string name, Vector3 pos, float size, float rotY)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CrateFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 크레이트 FBX 를 찾지 못했다 - " + CrateFbxPath);
            return;
        }

        float sizeMul = size / CrateNativeHeight;

        var root = Child(parent, name);
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = Vector3.up * (CrateNativeHeight * sizeMul * 0.5f);
        col.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        var bc = col.AddComponent<BoxCollider>();
        bc.size = new Vector3(CrateNativeWidth * sizeMul, CrateNativeHeight * sizeMul, CrateNativeDepth * sizeMul);
    }

    // ==========================================================
    // 배럴 자산 (28_barrel.fbx) - 에셋 준비만 해 둔다. 아직 어디에도 배치하지 않는다
    // (배치 위치는 나중에 별도 지시로 결정).
    //
    // identity 인스턴스로 실측한 결과 - Crate 와 같은 패턴:
    //   - root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0).
    //   - 실측 지름 0.90m x 높이 0.90m(원통형).
    // ==========================================================
    private const string BarrelFbxPath = "Assets/BrightDream/Models/28_barrel.fbx";
    private const float BarrelNativeDiameter = 0.90f;
    private const float BarrelNativeHeight = 0.90f;

    /// <summary>아직 호출부가 없다 - 배치 위치가 정해지면 이 함수로 세운다.</summary>
    private static void BuildCraftBarrel(Transform parent, string name, Vector3 pos, float targetHeight, float yaw)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BarrelFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 배럴 FBX 를 찾지 못했다 - " + BarrelFbxPath);
            return;
        }

        float sizeMul = targetHeight / BarrelNativeHeight;

        var root = Child(parent, name);
        root.position = pos;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = Vector3.up * (BarrelNativeHeight * sizeMul * 0.5f);
        var cc = col.AddComponent<CapsuleCollider>();
        cc.height = BarrelNativeHeight * sizeMul;
        cc.radius = BarrelNativeDiameter * sizeMul * 0.5f;
    }

    // ==========================================================
    // 다리 자산 (13_bridge.fbx) - GreenhousePond 의 유일한 다리 교체
    //
    // identity 인스턴스로 실측한 결과 - Bush/Tree 와 같은 패턴:
    //   - root scale 100, 로컬 eulerAngles 이미 (270.02, 0, 0) 으로 "서 있게" 구워져 있다.
    //   - 실측 폭(난간 사이) 1.76m x 높이(난간 포함) 1.29m x 길이(건너는 방향) 2.60m.
    //
    // 중요한 크기 충돌 하나: 이 지점의 길 폭(리본 기준 실제 폭)은 3.65m 로, 폭에 맞춰
    // 균일 스케일하면 배율이 2.07배가 되어 난간이 2.7m, 길이가 5.4m 로 늘어나
    // 다른 오브젝트 대비 지나치게 커진다. 그래서:
    //   - 걷는 판정(콜라이더)은 기존 프리미티브 Deck 과 정확히 같은 폭/길이/높이로 그대로
    //     유지한다(투명 - 렌더러 없이 콜라이더만) - 걷는 폭 자체는 손대지 않는다.
    //   - 눈에 보이는 다리 모델은 원래 길이(3.0m, 개울을 건너는 방향)에 맞춰서만 균일
    //     스케일한다(배율 약 1.15배, 난간 높이 약 1.49m) - 비율이 자연스럽게 유지된다.
    //     그 결과 시각적 다리 폭(약 2.0m)이 걷는 길 전체 폭(3.65m)보다 좁아 보일 수 있다
    //     - 스크린샷으로 확인 후 필요하면 조정한다.
    //   - 기존 Stream(개울물)은 그대로 둔다 - 이 에셋에는 물이 포함되어 있지 않다.
    // ==========================================================
    private const string BridgeFbxPath = "Assets/BrightDream/Models/13_bridge.fbx";
    private const float BridgeNativeWidth = 1.76f;
    private const float BridgeNativeLength = 2.60f;

    private static void BuildCraftBridge(Transform parent, Vector3 centerPos, Quaternion facing,
                                          float deckWidth, float deckLength)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BridgeFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 다리 FBX 를 찾지 못했다 - " + BridgeFbxPath);
            return;
        }

        var root = Child(parent, "Bridge_Craft");
        root.position = centerPos;
        root.rotation = facing;

        // 걷는 판정 - 렌더러 없이 콜라이더만, 기존 Deck 과 동일한 폭/길이/높이.
        var deckCollider = new GameObject("DeckCollider");
        deckCollider.transform.SetParent(root, false);
        deckCollider.transform.localPosition = Vector3.up * 0.16f;
        var bc = deckCollider.AddComponent<BoxCollider>();
        bc.size = new Vector3(deckWidth, 0.16f, deckLength);

        // 시각적 다리 - 원래 길이(개울을 건너는 방향)에 맞춰서만 균일 스케일.
        float sizeMul = deckLength / BridgeNativeLength;
        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;
    }

    // ==========================================================
    // 그네 자산 (그네2.fbx) - ClueWalk 의 유일한 그네(Swing) 교체
    //
    // 이 에셋은 팀 제공 FBX들과 export 관례가 다르다 - root scale=1(100 아님), 로컬
    // eulerAngles은 동일하게 (270, 0, 0) 이 구워져 있다. 실측 바운드가 대략 0.87 x 1.00 x 0.98m
    // 로 원점을 중심(centered pivot, min.y=-0.5)에 두고 있어서, 바닥에 그대로 놓으면 절반이
    // 파묻힌다 - 38_grass 테스트와 같은 이유로 높이의 절반만큼 들어 올려야 한다.
    // "texture_mapping" 메쉬 + Default-Material(흰색, 텍스처 없음)이라 팀 텍스처가 없다 -
    // 기존 그네 프레임에 쓰던 MatWood 를 그대로 입혀서 색을 넣는다.
    //
    // 목표 높이는 기존 프리미티브 그네의 TopBar 높이(2.6m)를 그대로 따른다 - 위치/방향 의도는
    // 손대지 않는다.
    // ==========================================================
    private const string SwingFbxPath = "Assets/BrightDream/Models/그네2.fbx";
    private const float SwingNativeHeight = 1.00f;

    private static void BuildCraftSwing(Transform parent, Vector3 basePos, Quaternion rot, float targetHeight)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SwingFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 그네 FBX 를 찾지 못했다 - " + SwingFbxPath);
            return;
        }

        float sizeMul = targetHeight / SwingNativeHeight;

        var root = Child(parent, "Swing_Craft");
        root.position = basePos + Vector3.up * (targetHeight * 0.5f);   // centered pivot 보정
        root.rotation = rot;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;
        // Swing2.mat(원본 embedded texture 기반)이 FBX Importer external material remap 으로
        // 정상 연결되어 있어 프리팹 자체 머티리얼을 그대로 쓴다 - 더 이상 단색으로 덮지 않는다.

        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.87f, 1.00f, 0.98f) * sizeMul;
    }

    // ==========================================================
    // 정원 아치 자산 (Garden Arch.fbx) - UnicornApproach 의 유일한 아치(FlowerArch) 교체
    //
    // 그네2 와 같은 export 관례(root scale=1, rot=(270,0,0) 구워짐, centered pivot,
    // texture_mapping 메쉬, Default-Material). 실측 바운드는 0.35 x 1.00 x 0.99m 로
    // 폭(X)이 유난히 얇고 높이(Y)와 깊이(Z)가 거의 1:1 - 즉 이 모델은 "옆에서 봤을 때 두께
    // 0.35m 인 얇은 트렐리스 아치"이고, 사람이 지나가는 통로 폭은 X 가 아니라 Z 축이다.
    // 그대로 Facing(s) 만 곱하면 통로가 길 방향(진행 방향)으로 나 있게 되어 버려서,
    // 추가로 Y축 90도를 더 돌려 통로(원래 Z)가 길을 가로지르는 방향(좌우)을 보게 맞춘다.
    //
    // 목표 폭은 기존 FlowerArch 의 archHalf*2(이 지점 리본 폭 기준)를 그대로 따른다 - 위치/
    // 통과 폭 의도는 손대지 않는다. 통로 아래를 실제로 지나갈 수 있어야 하므로 전체 바운딩
    // 박스 콜라이더 대신, 아치 모양 그대로를 따르는 단순(비볼록) MeshCollider 를 쓴다.
    // ==========================================================
    private const string GardenArchFbxPath = "Assets/BrightDream/Models/Garden Arch.fbx";
    private const float GardenArchNativeSpan = 0.99f;   // 실제 통로 폭 축(원래 Z)
    private const float GardenArchExtraYaw = 90f;

    private static void BuildCraftGardenArch(Transform parent, Vector3 basePos, Quaternion facing, float targetSpan)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GardenArchFbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 정원 아치 FBX 를 찾지 못했다 - " + GardenArchFbxPath);
            return;
        }

        float sizeMul = targetSpan / GardenArchNativeSpan;
        float targetHeight = 1.00f * sizeMul;

        var root = Child(parent, "GardenArch_Craft");
        root.position = basePos + Vector3.up * (targetHeight * 0.5f);   // centered pivot 보정
        root.rotation = facing * Quaternion.Euler(0f, GardenArchExtraYaw, 0f);

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;
        // GardenArch.mat(원본 embedded texture 기반)이 FBX Importer external material remap 으로
        // 정상 연결되어 있어 프리팹 자체 머티리얼을 그대로 쓴다 - 더 이상 단색으로 덮지 않는다.

        var mf = visual.GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            var mc = visual.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    // ==========================================================
    // 38_grass 테스트 배치 (길가 장식용 작은 풀 - 모델 자체는 손대지 않는다)
    //
    // identity 인스턴스로 실측: 루트 scale=1, rotation=0 (Bush/Tree 와 달리 baked 축 보정이
    // 필요 없다). 메쉬 바운즈는 원점을 중심으로 대략 0.95 x 1.00 x 1.00m - pivot 이 중앙에
    // 있어서 그대로 놓으면 절반이 땅에 파묻히므로, 높이의 절반만큼 들어 올려 준다.
    //
    // 테스트 배치 3~5개 - Path/Fence/Tree 위치 계산은 기존 (s, lateral) 규칙을 그대로 쓰고,
    // 걷는 길(RibbonHalfWidth) 안쪽은 절대 침범하지 않는다. Collider 는 달지 않는다.
    // ==========================================================
    private const string Grass38FbxPath = "Assets/BrightDream/Models/38_grass.fbx";
    private const float Grass38NativeHeight = 0.9999f;

    private struct Grass38Spot
    {
        public string Name;
        public float S;
        public float Side;
        public float RibbonExtra;
        public float TargetHeight;
        public Grass38Spot(string name, float s, float side, float ribbonExtra, float targetHeight)
        { Name = name; S = s; Side = side; RibbonExtra = ribbonExtra; TargetHeight = targetHeight; }
    }

    private static void BuildTestGrass38Scatter(Transform parent)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Grass38FbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 테스트용 38_grass FBX 를 찾지 못했다 - " + Grass38FbxPath);
            return;
        }

        var group = Child(parent, "TestGrass38");

        // 좌표는 감으로 잡지 않고, 기존 조경(나무 캐노피/수풀/울타리) 렌더러 바운즈까지의
        // 실제 거리를 스캔해 겹치지 않는 자리만 골랐다 - StartGarden 은 나무/수풀이
        // 길가~상자 경계까지 빽빽하게 차 있어서, 얕게(Ribbon 바로 바깥) 잡아야 빈틈이 있었다.
        var spots = new List<Grass38Spot>
        {
            // StartGarden, Tree_B 캐노피 바로 옆 길가 (실측 여유 0.40m)
            new Grass38Spot("StartGarden_TreeSide", InArea("StartGarden", 0.56f), 1f, 0.26f, 0.55f),
            // StartGarden, Fence_Left 판넬 옆 (실측 여유 0.36m)
            new Grass38Spot("StartGarden_FenceSide", InArea("StartGarden", 0.94f), -1f, 0.50f, 0.45f),
            // ClueWalk("메인 산책로") 초입 길가 (실측 여유 0.38m)
            new Grass38Spot("MainPath_Edge_A", InArea("ClueWalk", 0.02f), -1f, 0.60f, 0.50f),
            // ClueWalk 중반 길가 (실측 여유 0.34m)
            new Grass38Spot("MainPath_Edge_B", InArea("ClueWalk", 0.50f), 1f, 0.50f, 0.65f),
        };

        foreach (var spot in spots)
        {
            float lateral = (RibbonHalfWidth(spot.S) + spot.RibbonExtra) * spot.Side;
            Vector3 pos = At(spot.S, lateral);

            var rng = new System.Random(spot.Name.GetHashCode());
            float sizeMul = spot.TargetHeight / Grass38NativeHeight;
            float yaw = Lerp(0f, 360f, (float)rng.NextDouble());

            var root = Child(group, spot.Name);
            root.position = pos + Vector3.up * (spot.TargetHeight * 0.5f);

            var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
            visual.transform.localScale = prefab.transform.localScale * sizeMul;
        }
    }

    // ==========================================================
    // MainPath 전체 - Blender 제작 저폴리 조약돌/자갈 배치
    // (StartGarden 구간 테스트에서 비주얼 채택 후 Rect MainPath 전체로 확장)
    //
    // 기존 Ribbon Path 의 지오메트리/폭/센터라인/Collider 는 전혀 건드리지 않는다.
    // 그 위에 아주 얇게(바닥 바로 위) 뜬 장식 메시만 얹는다 - Collider 는 달지 않는다
    // (걷는 판정은 기존 Ribbon Collider 그대로 쓴다).
    // ==========================================================
    private const string PathStonesFbxPath = "Assets/BrightDream/Models/PathStones.fbx";
    private static readonly string[] SteppingStoneNames =
        { "SteppingStone_A", "SteppingStone_B", "SteppingStone_C", "SteppingStone_D" };
    private static readonly string[] PebbleNames = { "Pebble_A", "Pebble_B" };

    private static Mesh FindStoneMesh(GameObject fbxRoot, string name)
    {
        var t = fbxRoot.transform.Find(name);
        if (t == null) return null;
        var mf = t.GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    /// <summary>
    /// 구간(Area)별 조약돌 배치 밀도 배율 - StartGarden 테스트에서 이미 채택된 밀도(활성 80%)를
    /// 기준으로, Greenhouse 쪽은 그와 비슷하게 높게, 일반 이동 구간은 중간, CombatArena 접근은
    /// 낮게, Unicorn 접근/광장은 거의 안 보이게 낮춘다.
    /// </summary>
    private static float PathStoneDensityFactor(string area)
    {
        switch (area)
        {
            case "StartGarden": return 0.80f;
            case "GreenhousePond": return 0.75f;
            case "ClueWalk": return 0.45f;
            case "WeaponLink": return 0.45f;
            case "TutorialNook": return 0.40f;
            case "CombatArena": return 0.12f;
            case "UnicornApproach": return 0.03f;
            case "UnicornPlaza": return 0.03f;
            default: return 0.4f;
        }
    }

    private static void BuildPathStones(Transform mainPath)
    {
        var fbxRoot = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PathStonesFbxPath);
        if (fbxRoot == null)
        {
            Debug.LogWarning("[BrightDream] PathStones FBX 를 찾지 못했다 - " + PathStonesFbxPath);
            return;
        }

        var steppingMeshes = new Mesh[SteppingStoneNames.Length];
        for (int i = 0; i < SteppingStoneNames.Length; i++) steppingMeshes[i] = FindStoneMesh(fbxRoot, SteppingStoneNames[i]);
        var pebbleMeshes = new Mesh[PebbleNames.Length];
        for (int i = 0; i < PebbleNames.Length; i++) pebbleMeshes[i] = FindStoneMesh(fbxRoot, PebbleNames[i]);

        var group = Child(mainPath, "PathStones");
        var rng = new System.Random(31337);

        // s=0.6 자리 BackBush_ 를 피해 2.5m 부터, 유니콘 뒤 꼬리 여유 구간 전에서 끝낸다.
        // (CombatArena/UnicornApproach/Plaza 는 제외하는 게 아니라 밀도 배율로 거의 안 보이게 한다.)
        float sFrom = 2.5f, sTo = SpineTotalLength - 3f;
        const float baseStep = 0.55f;

        // SteppingStone 은 "비누/쿠션" 처럼 두꺼워 보이지 않게 Y만 눌러서 바닥에 붙인다.
        // Pebble 은 이미 얇지만 통일감을 위해 약하게만 같이 눌러 준다.
        const float steppingHeightMul = 0.62f;
        const float pebbleHeightMul = 0.80f;

        int stoneIndex = 0;
        System.Action<Mesh, Vector3, float, float, float, Material> placeStone = (mesh, pos, yaw, scaleXZ, heightMul, mat) =>
        {
            if (mesh == null) return;
            var go = new GameObject($"Stone_{stoneIndex:D2}");
            stoneIndex++;
            go.transform.SetParent(group, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(scaleXZ, scaleXZ * heightMul, scaleXZ);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.BatchingStatic);
        };

        System.Func<Material> pickTone = () =>
        {
            // MatPathStoneTones = [Cream, Ivory, WarmBeige, PaleGray, Pink, Lavender, Butter]
            // 중립(Cream/Ivory/WarmBeige) 80%, PaleGray 소량 5%, 파스텔 포인트 15%
            int roll = rng.Next(100);
            if (roll < 80) return MatPathStoneTones[rng.Next(3)];         // 0,1,2
            if (roll < 85) return MatPathStoneTones[3];                  // PaleGray
            return MatPathStoneTones[4 + rng.Next(3)];                   // Pink/Lavender/Butter
        };

        // 큰/중간 돌 하나 주변에 자갈 2~3개를 느슨하게 무리 지어 놓는다 - 매번 붙이지 않고
        // 일부 지점에서만, 간격도 불규칙하게(자갈 포장처럼 촘촘해지지 않도록).
        System.Action<Vector3> scatterPebbleCluster = (center) =>
        {
            int count = 2 + rng.Next(3); // 2~4개 - Pebble cluster 비중 증가
            for (int i = 0; i < count; i++)
            {
                if (rng.NextDouble() < 0.15) continue; // 자리마다 건너뛰기도 해서 간격이 일정하지 않게
                float dist = Lerp(0.15f, 0.45f, (float)rng.NextDouble());
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                Vector3 pos = center + offset;
                int pv = rng.Next(pebbleMeshes.Length);
                placeStone(pebbleMeshes[pv], pos, Lerp(0f, 360f, (float)rng.NextDouble()),
                           Lerp(0.65f, 1.15f, (float)rng.NextDouble()), pebbleHeightMul, pickTone());
            }
        };

        // 몇 m 씩 묶어서 "이번 구간은 왼쪽만 / 오른쪽만 / 양쪽 / 아무것도 없음" 을 먼저 정한다 -
        // 매 스텝마다 새로 굴리면 지나치게 촘촘하고 규칙적으로 보인다. 구간이 걸치는 지역의
        // 밀도 배율만큼 "아무것도 없음" 쪽으로 더 기울인다(StartGarden 기준 활성 80%는 그대로).
        for (float s = sFrom; s < sTo; )
        {
            float runLength = Lerp(1.0f, 2.2f, (float)rng.NextDouble());
            float density = PathStoneDensityFactor(AreaNameAt(s + runLength * 0.5f));
            float activeChance = 0.80f * density; // StartGarden(density=0.80) 기준 원래 활성 80% 유지
            int roll = rng.Next(100);
            bool left = false, right = false;
            float activePct = activeChance * 100f;
            if (roll >= activePct) { /* 아무것도 없는 구간 */ }
            else
            {
                float sub = roll / Mathf.Max(activePct, 0.0001f); // 0..1 안에서 좌/우/양쪽 재분배
                if (sub < 0.375f) left = true;
                else if (sub < 0.75f) right = true;
                else { left = true; right = true; }
            }

            float runEnd = Mathf.Min(s + runLength, sTo);
            for (float ss = s; ss < runEnd; ss += Lerp(baseStep * 0.7f, baseStep * 1.4f, (float)rng.NextDouble()))
            {
                foreach (int side in new[] { -1, 1 })
                {
                    if (side < 0 && !left) continue;
                    if (side > 0 && !right) continue;
                    if (rng.NextDouble() > 0.7) continue; // 구간 안에서도 매 스텝을 다 채우지 않는다

                    float half = RibbonHalfWidth(ss);
                    // 대부분은 리본 경계에 바짝 붙되(안/밖 아주 약간), 아주 일부(약 15%)는
                    // 길 안쪽으로 0.10~0.20m 들어와 전부 바깥쪽에만 몰리지 않게 한다. 중앙
                    // 60~70%는 폭이 가장 좁은 구간에서도 여유가 남아 계속 비어 있다.
                    float edgeJitter = rng.NextDouble() < 0.85
                        ? Lerp(-0.06f, 0.14f, (float)rng.NextDouble())
                        : Lerp(-0.20f, -0.10f, (float)rng.NextDouble());
                    float lateral = (half + edgeJitter) * side;
                    Vector3 basePos = At(ss, lateral) + Vector3.up * 0.022f;

                    float yaw = Lerp(0f, 360f, (float)rng.NextDouble());
                    Material mat = pickTone();

                    // SteppingStone_A/B(큰 돌) 빈도를 크게 줄이고 C/D(작은/중간)를 메인으로
                    // 둔다 - 길 전체가 디딤돌 코스로 안 보이게: A 4%, B 11%, C 35%, D 50%.
                    int roll2 = rng.Next(100);
                    int variant = roll2 < 4 ? 0 : roll2 < 15 ? 1 : roll2 < 50 ? 2 : 3;
                    float scaleRange = variant == 0 ? Lerp(0.9f, 1.05f, (float)rng.NextDouble())
                                     : Lerp(0.85f, 1.1f, (float)rng.NextDouble());
                    placeStone(steppingMeshes[variant], basePos, yaw, scaleRange, steppingHeightMul, mat);

                    // Pebble cluster 비중을 늘린다 - 대부분의 돌 옆에 자갈이 곁들여지게.
                    if (rng.NextDouble() < 0.65) scatterPebbleCluster(basePos);
                }
            }
            s = runEnd + Lerp(0.3f, 1.0f, (float)rng.NextDouble()); // 다음 구간 사이에도 빈 여백을 둔다
        }
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

        // BackBed_ 가 GreenhousePond 구간 위에 놓이는지 판정 - samples 는 s=0,1,2... 순서라
        // 가장 가까운 샘플의 인덱스가 곧 그 지점의 s(정수 반올림) 다.
        string NearestAreaName(Vector3 p)
        {
            int bestI = 0; float bestD = float.MaxValue;
            for (int i = 0; i < samples.Count; i++)
            {
                float d = (p.x - samples[i].x) * (p.x - samples[i].x) + (p.z - samples[i].z) * (p.z - samples[i].z);
                if (d < bestD) { bestD = d; bestI = i; }
            }
            return AreaNameAt(bestI * 1.0f);
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
                    float bedRadius = Lerp(1.0f, 1.8f, (float)rng.NextDouble());
                    int bedCount = 4 + rng.Next(4);
                    if (Rectangular && NearestAreaName(spot) == "GreenhousePond")
                    {
                        // 중요: rng 는 상자 전체(모든 구역)가 공유하는 단일 순차 스트림이다.
                        // MakeFlowerBed 라면 꽃 한 송이당 5개 값(angle, distance, mat, height, headScale)
                        // 을 뽑았을 것이므로, 그 개수를 그대로 뽑아서 버려야 이후 다른 구역의 격자 칸
                        // 추첨(roll)이 밀리지 않는다. 실제 꽃 파라미터는 위치로 시드한 독립 rng 로 뽑는다.
                        for (int dummy = 0; dummy < bedCount; dummy++)
                        {
                            rng.NextDouble(); rng.NextDouble(); rng.Next(4); rng.NextDouble(); rng.NextDouble();
                        }
                        var craftRng = new System.Random(unchecked(index * 733 + 991));
                        BuildCraftFlowerBedTest(backdrop, $"BackBed_{index:D3}", spot, bedRadius, bedCount, craftRng);
                    }
                    else
                        MakeFlowerBed(backdrop, $"BackBed_{index:D3}", spot, bedRadius, bedCount, rng);
                }
                // 나머지 20% 는 아무것도 두지 않는다 - 빈 잔디가 있어야 상자가 답답하지 않다
            }
        }
    }

    // ==========================================================
    // 공예 구름 / 태양 / 별 (35_cloud / 36_sun / 37_star.fbx)
    //
    // 기존 절차적 배치(칸 나누기, 층, 지터, 랜드마크 회피, Ceiling→String 연결)는
    // MakeHangingCloud / BuildCeilingDecorations(Sun) / MakePaperStar 호출부에서
    // 전부 그대로 유지한다. 여기서는 "그 자리에 무엇을 그릴지"만 페이퍼 프리미티브에서
    // 팀 에셋으로 바꾼다. 셋 다 실측 결과 별도 텍스처 파일(embedded 아님)이 이미
    // BD_{name}.mat 파이프라인으로 정상 연결되어 있어 추가 Blender 언패킹은 불필요했다.
    // ==========================================================
    private const string Cloud35FbxPath = "Assets/BrightDream/Models/35_cloud.fbx";
    private const float Cloud35NativeWidth = 4.00f;   // identity 실측 bounds X
    private const float Cloud35NativeCenterY = 1.60f; // identity 실측 bounds center.y - 피벗이 바닥 쪽에 있어 보정 필요

    private static void BuildCraftCloud(Transform parent, string name, Vector3 center, float size, Quaternion rot)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Cloud35FbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 구름 FBX 를 찾지 못했다 - " + Cloud35FbxPath);
            return;
        }

        // size 는 기존 페이퍼 컷아웃 뭉치의 실질 폭(m)과 같은 단위 - 그대로 스케일 배수로 쓴다.
        float sizeMul = size / Cloud35NativeWidth;

        var root = Child(parent, name);
        root.position = center;
        root.rotation = rot;

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        // 이 모델은 피벗이 기하학적 중심이 아니라 바닥 쪽에 있다(identity 기준 center.y=1.60,
        // 거의 half-height 만큼 아래). center 인자가 "뭉치의 중심"을 뜻하는 기존 관례를 따르려면
        // 피벗을 그만큼 아래로 내려서 실제 메시 중심이 center 에 오도록 맞춰야 한다.
        visual.transform.localPosition = Vector3.down * (Cloud35NativeCenterY * sizeMul);
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        // 실은 구름 중심이 아니라 실제로 배치된 메시의 최상단에서 내려와야 자연스럽다 -
        // 기존 MakePaperCutoutCloud 와 같은 "실측 최상단" 원칙을 그대로 따른다.
        var mr = visual.GetComponentInChildren<MeshRenderer>();
        if (mr != null)
        {
            Vector3 topPoint = new Vector3(mr.bounds.center.x, mr.bounds.max.y, mr.bounds.center.z);
            MakeCloudThread(root, "Thread", topPoint + Vector3.down * 0.02f);
        }
    }

    private const string Sun36FbxPath = "Assets/BrightDream/Models/36_sun.fbx";
    private const float Sun36NativeWidth = 3.00f;    // identity 실측 bounds X
    private const float Sun36NativeCenterY = 1.50f;  // identity 실측 bounds center.y - 피벗 보정용
    private const float SunTargetWidth = 3.9f;       // 기존 원반(2.2m) + 광선 끝까지 폭과 맞춘 값

    // 얼굴(무늬) 정면 보정용 요(yaw). identity(0도) 기준 실측 결과 정면 법선이 월드 -Z
    // 방향을 향하고 있었다 - 4방향(0/90/180/270) 비교 스크린샷으로 직접 확인.
    // Sun 위치(-3.87,6.40,10.34)에서 그 근방 스파인 지점(s=10, (-0.39,0,9.98))으로 향하는
    // 방향은 거의 월드 -X(플레이어가 길을 걸으며 옆으로 올려다보는 방향)라서, 정면 법선을
    // (0,0,-1)에서 그 방향으로 돌리는 각도를 dot-product 스캔으로 실측(276도, dot=0.9999987).
    // Sun 위치/기존 원반이 바라보던 축(Z)과는 다른 값이지만, "플레이어가 실제로 보는 방향"
    // 기준으로 맞춘 것이 이번 요청의 목표에 더 맞는다고 판단했다.
    private const float SunFaceYawOffset = 276f;

    private static void BuildCraftSun(Transform parent, string name, Vector3 pos)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Sun36FbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 태양 FBX 를 찾지 못했다 - " + Sun36FbxPath);
            return;
        }

        float sizeMul = SunTargetWidth / Sun36NativeWidth;

        var root = Child(parent, name);
        root.position = pos;
        root.rotation = Quaternion.Euler(0f, SunFaceYawOffset, 0f);

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.down * (Sun36NativeCenterY * sizeMul);
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        // Collider 불필요 - 배경 천장 장식이라 상호작용하지 않는다.
        MakeString(root, "SunString", pos);
    }

    private const string Star37FbxPath = "Assets/BrightDream/Models/37_star.fbx";
    private const float Star37NativeSpan = 1.00f;     // identity 실측 bounds 중 가장 긴 축(Y)
    private const float Star37NativeCenterY = 0.50f;  // identity 실측 bounds center.y - 피벗 보정용

    private static void BuildCraftStar(Transform parent, string name, Vector3 pos, float size)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Star37FbxPath);
        if (prefab == null)
        {
            Debug.LogWarning("[BrightDream] 공예 별 FBX 를 찾지 못했다 - " + Star37FbxPath);
            return;
        }

        // 기존 BladeA(가장 긴 날, size*2 폭)와 같은 전체 폭이 되도록 맞춘다.
        float targetSpan = size * 2f;
        float sizeMul = targetSpan / Star37NativeSpan;
        // rng 를 새로 소비하면 이후 배치 시퀀스가 밀리므로, 위치 기반 결정론적 요(yaw)만 쓴다.
        float yaw = Mathf.Repeat(pos.x * 37f + pos.z * 13f, 360f);

        var root = Child(parent, name);
        root.position = pos;
        root.rotation = Quaternion.Euler(0f, yaw, 0f);

        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
        visual.transform.localPosition = Vector3.down * (Star37NativeCenterY * sizeMul);
        visual.transform.localRotation = prefab.transform.rotation;
        visual.transform.localScale = prefab.transform.localScale * sizeMul;

        MakeString(root, "String", pos);
    }
}
