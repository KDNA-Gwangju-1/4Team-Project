using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// "밝은 꿈(Bright Dream)" 맵의 레벨 블록아웃 Scene 을 통째로 만들어 주는 에디터 전용 도구.
///
/// 2차 구조 변경: 맵이 더 이상 직사각형이 아니다.
/// 하나의 꺾인 중심선(Spine)을 따라 바닥 / 양옆 벽 / 천장이 통째로 휘어 나가고,
/// 구간마다 복도 폭이 부드럽게 변해서 "좁은 길 -> 넓은 정원 -> 좁은 길 -> 전투장" 리듬을 만든다.
/// 그래서 다음 구역은 인위적인 가림막이 아니라 맵 자체의 꺾임으로 가려진다.
///
/// 배치는 전부 (s, lateral) 좌표로 한다.
///   s       = START 에서부터의 실제 걸어간 거리(m)
///   lateral = 그 지점에서 길 중심선 기준 좌우 오프셋(m, +가 오른쪽)
/// 덕분에 중심선 모양을 바꿔도 오브젝트가 알아서 따라간다.
///
/// 메뉴: Tools > Bright Dream > Build Blockout Scene
/// </summary>
public static partial class BrightDreamBlockoutBuilder
{
    /// <summary>자유형(구불구불한 외곽) 버전 - 기본.</summary>
    public const string ScenePath = "Assets/Scenes/SD_BrightDream_Blockout.unity";

    /// <summary>비교 테스트용 직사각형 외곽 버전.</summary>
    public const string RectScenePath = "Assets/Scenes/SD_BrightDream_Blockout_Rect.unity";

    /// <summary>빌드 중에만 켜지는 플래그. 새 씬은 아직 경로가 없어서 씬 이름으로는 판단할 수 없다.</summary>
    private static bool buildingRectangular;

    /// <summary>
    /// 직사각형 버전인가.
    ///
    /// 빌드 중에는 플래그로, 빌드가 끝난 뒤에는 "지금 열려 있는 씬" 으로 판단한다.
    /// 그래야 Rect 씬을 열어 놓고 BuildSpineTable() 을 다시 불러도 같은 중심선이 나온다.
    /// </summary>
    public static bool Rectangular =>
        buildingRectangular || SceneManager.GetActiveScene().path == RectScenePath;

    public static string CurrentScenePath => Rectangular ? RectScenePath : ScenePath;
    private const string RootFolder = "Assets/BrightDream";
    private const string MaterialFolder = RootFolder + "/Materials";

    /// <summary>자유형 버전 천장 높이 - 기존 그대로 유지.</summary>
    private const float FreeformCeilingY = 7.5f;

    /// <summary>
    /// Rect 버전 전용 천장 높이. Treehouse V2(최고점 약 7.1m) 위로 시각적 여유를
    /// 2.5~3m 확보하기 위해 자유형보다 훨씬 높였다. 자유형 씬은 이 값의 영향을 받지 않는다.
    /// </summary>
    private const float RectCeilingY = 10.5f;

    public static float CeilingY => Rectangular ? RectCeilingY : FreeformCeilingY;
    public const float EyeHeight = 1.6f;
    public const float FieldOfView = 70f;
    public const float PlayerStartS = 1.5f;

    /// <summary>유니콘 뒤쪽 여유 공간. 광장이 막다른 벽에 딱 붙지 않게 한다.</summary>
    public const float TailLength = 6f;

    // ==========================================================
    // 중심선 제어점 - Top View 제안의 구간 꼭짓점을 그대로 옮긴 것.
    // 이 점들을 부드럽게 이어서 각진 코너가 아닌 완만한 곡선으로 만든다.
    // ==========================================================
    private static readonly Vector2[] SpineControls =
    {
        new Vector2(0.00f,  0.00f),   // START
        new Vector2(0.00f, 12.00f),   // 시작 정원 끝
        new Vector2(6.31f, 21.01f),   // 단서 산책길 끝
        new Vector2(18.87f, 24.38f),  // 온실 / 연못 끝
        new Vector2(22.29f, 33.78f),  // 정화총 연결길 끝
        new Vector2(20.48f, 40.54f),  // 튜토리얼 끝
        new Vector2(21.79f, 53.50f),  // 전투장 끝
        new Vector2(11.00f, 60.50f),  // 접근로 - 크게 왼쪽으로
        new Vector2(13.60f, 68.00f),  // 유니콘 광장 중심 - 다시 오른쪽으로 되꺾임
        new Vector2(15.50f, 73.50f),  // 광장 뒤쪽 여유
    };

    /// <summary>
    /// 직사각형 버전 전용 중심선.
    ///
    /// 자유형 버전은 복도 벽이 시야를 끊어 주지만, 직사각형 버전은 상자 안이 통째로 트여 있어서
    /// 전투장에서 유니콘이 40m 밖까지 그대로 보였다.
    ///
    /// 그래서 마지막 구간만 한 번 더 꺾는다.
    ///   전투장 끝에서 왼쪽으로 크게 빠졌다가, 광장으로 들어가며 오른쪽으로 되꺾인다.
    /// 전투장에서 유니콘으로 향하는 시선이 접근로 바깥(동쪽) 빈 땅을 지나가게 되고,
    /// 그 자리에 배경 숲무리를 놓아 자연스럽게 가린다.
    ///
    /// 앞부분(START ~ 전투장)은 자유형과 완전히 같다. 이동거리도 80~90m 안에 유지한다.
    /// </summary>
    private static readonly Vector2[] RectSpineControls =
    {
        new Vector2(0.00f,  0.00f),
        new Vector2(0.00f, 12.00f),
        new Vector2(6.31f, 21.01f),
        new Vector2(18.87f, 24.38f),
        new Vector2(22.29f, 33.78f),
        new Vector2(20.48f, 40.54f),
        new Vector2(21.79f, 53.50f),   // 전투장 끝 - 여기까지는 자유형과 동일
        new Vector2(12.80f, 58.20f),   // 접근로 - 왼쪽으로 크게
        new Vector2(9.50f, 64.20f),    // 접근로 - 더 왼쪽 위로
        new Vector2(15.30f, 67.60f),   // 마지막 굴곡 - 오른쪽으로 되꺾임
        new Vector2(18.80f, 69.80f),   // 광장 뒤 여유
    };

    private static Vector2[] ActiveSpineControls => Rectangular ? RectSpineControls : SpineControls;

    /// <summary>구간 정의. 경계는 전체 이동거리에 대한 비율이라 중심선을 손봐도 비율이 유지된다.</summary>
    public struct AreaDef
    {
        public string Name;
        public float StartF;
        public float EndF;
        public float HalfWidth;
    }

    public static readonly AreaDef[] Areas =
    {
        new AreaDef { Name = "StartGarden",     StartF = 0.000f, EndF = 0.144f, HalfWidth = 5.5f },
        new AreaDef { Name = "ClueWalk",        StartF = 0.144f, EndF = 0.275f, HalfWidth = 4.0f },
        new AreaDef { Name = "GreenhousePond",  StartF = 0.275f, EndF = 0.431f, HalfWidth = 10.0f },
        new AreaDef { Name = "WeaponLink",      StartF = 0.431f, EndF = 0.551f, HalfWidth = 4.5f },
        new AreaDef { Name = "TutorialNook",    StartF = 0.551f, EndF = 0.635f, HalfWidth = 6.5f },
        new AreaDef { Name = "CombatArena",     StartF = 0.635f, EndF = 0.814f, HalfWidth = 13.0f },
        new AreaDef { Name = "UnicornApproach", StartF = 0.814f, EndF = 0.934f, HalfWidth = 3.5f },
        new AreaDef { Name = "UnicornPlaza",    StartF = 0.934f, EndF = 1.000f, HalfWidth = 11.0f },
    };

    // ==========================================================
    // 파스텔 공예 팔레트
    // ==========================================================
    private static Material MatGrass, MatGrassAlt, MatSand, MatStone;
    private static Material MatPathCream, MatPathBeige, MatPathButter, MatPathIvory, MatPathPaperLight, MatPathStepIvory;
    private static Material MatSky, MatCloud, MatSun, MatStar;
    private static Material MatTrunk, MatLeaf, MatLeafAlt, MatLeafDeep, MatLeafSoft;
    private static Material MatMeadow, MatHillMint, MatHillPeach, MatHillLilac;
    private static Material MatFence, MatWood, MatGlass, MatWater;
    private static Material MatPink, MatYellow, MatPurple, MatWhiteFlower;
    private static Material MatEnemy, MatUnicorn, MatMane, MatGold;
    private static Material MatMarkerClue, MatMarkerWeapon, MatMarkerEnemy, MatMarkerDream;

    // ==========================================================
    // 진입점
    // ==========================================================
    [MenuItem("Tools/Bright Dream/Build Blockout Scene")]
    public static void BuildBlockout() => BuildBlockout(false);

    /// <summary>외곽만 직사각형인 비교 버전. 중심선 / 구간 / Marker / 조경은 자유형과 동일하다.</summary>
    [MenuItem("Tools/Bright Dream/Build Rect Blockout Scene (비교용)")]
    public static void BuildRectBlockout() => BuildBlockout(true);

    private static void BuildBlockout(bool rectangular)
    {
        buildingRectangular = rectangular;
        EnsureFolders();
        CreateMaterials();
        KeepClear.Clear();
        NoTreeZones.Clear();
        BuildSpineTable();

        var scene = CreateWorkingScene();
        ApplyLighting();

        var root = new GameObject("BrightDream_Blockout").transform;

        var boundaries = Child(root, "Boundaries");
        var ceiling = Child(root, "Ceiling");
        var ground = Child(root, "Ground");
        var mainPath = Child(root, "MainPath");
        var startGarden = Child(root, "StartGarden");
        var clueWalk = Child(root, "ClueWalk");
        var greenhousePond = Child(root, "GreenhousePond");
        var weaponLink = Child(root, "WeaponLink");
        var tutorialNook = Child(root, "TutorialNook");
        var combatArena = Child(root, "CombatArena");
        var unicornApproach = Child(root, "UnicornApproach");
        var unicornPlaza = Child(root, "UnicornPlaza");
        var landscaping = Child(root, "Landscaping");
        var markers = Child(root, "Markers");

        BuildCorridor(ground, boundaries, ceiling);
        BuildMainPath(mainPath);

        BuildStartGarden(startGarden, markers);
        BuildClueWalk(clueWalk, markers);
        BuildGreenhousePond(greenhousePond, markers);
        BuildWeaponLink(weaponLink, markers);
        BuildTutorialNook(tutorialNook, markers);
        BuildCombatArena(combatArena, markers);
        BuildUnicornApproach(unicornApproach, markers);
        BuildUnicornPlaza(unicornPlaza, markers);

        if (Rectangular) BuildRectBackdrop(landscaping);
        BuildBoundaryLandscaping(landscaping);
        BuildPathsideLandscaping(landscaping);
        BuildCeilingDecorations(ceiling);

        if (Rectangular)
        {
            // 조경은 온실보다 먼저 깔리므로, 다 깐 뒤에 겹치는 것만 걷어낸다
            ClearGreenhouseOverlap(root, GameObject.Find("BrightDream_Blockout/GreenhousePond/CraftGardenGreenhouse"));
        }

        var path = BuildPathData(mainPath);
        BuildPlayer(root, path);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, CurrentScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LogMetrics(root);
        buildingRectangular = false;
    }

    // ==========================================================
    // 중심선 - 제어점을 부드럽게 이어 거리(s) 로 조회할 수 있게 만든다
    // ==========================================================
    private static List<Vector3> spineSamples;
    private static List<float> spineArc;
    private static float spineLength;

    /// <summary>중심선 전체 길이(유니콘 뒤 여유 포함).</summary>
    public static float SpineTotalLength => spineLength;

    /// <summary>START 에서 유니콘까지의 실제 이동 거리(m).</summary>
    public static float WalkLength => spineLength - TailLength;

    /// <summary>유니콘이 서 있는 지점의 s.</summary>
    public static float UnicornS => WalkLength;

    public static void BuildSpineTable()
    {
        var controls = ActiveSpineControls;
        int n = controls.Length;
        var kx = new Keyframe[n];
        var kz = new Keyframe[n];
        for (int i = 0; i < n; i++)
        {
            kx[i] = new Keyframe(i, controls[i].x);
            kz[i] = new Keyframe(i, controls[i].y);
        }

        var curveX = new AnimationCurve(kx);
        var curveZ = new AnimationCurve(kz);
        for (int i = 0; i < n; i++)
        {
            curveX.SmoothTangents(i, 0f);
            curveZ.SmoothTangents(i, 0f);
        }

        spineSamples = new List<Vector3>();
        spineArc = new List<float>();

        Vector3 previous = new Vector3(curveX.Evaluate(0f), 0f, curveZ.Evaluate(0f));
        spineSamples.Add(previous);
        spineArc.Add(0f);

        float total = 0f;
        for (float t = 0.01f; t <= n - 1f; t += 0.01f)
        {
            Vector3 point = new Vector3(curveX.Evaluate(t), 0f, curveZ.Evaluate(t));
            total += Vector3.Distance(previous, point);
            spineSamples.Add(point);
            spineArc.Add(total);
            previous = point;
        }
        spineLength = total;
    }

    /// <summary>START 에서 s 미터 걸어간 지점의 월드 좌표.</summary>
    public static Vector3 SpinePoint(float s)
    {
        if (spineSamples == null) BuildSpineTable();
        s = Mathf.Clamp(s, 0f, spineLength);

        int low = 0, high = spineArc.Count - 1;
        while (low < high - 1)
        {
            int mid = (low + high) / 2;
            if (spineArc[mid] <= s) low = mid; else high = mid;
        }

        float span = spineArc[high] - spineArc[low];
        float t = span <= 0.0001f ? 0f : (s - spineArc[low]) / span;
        return Vector3.Lerp(spineSamples[low], spineSamples[high], t);
    }

    public static Vector3 SpineForward(float s)
    {
        Vector3 delta = SpinePoint(Mathf.Min(s + 0.3f, spineLength)) - SpinePoint(Mathf.Max(s - 0.3f, 0f));
        delta.y = 0f;
        return delta.sqrMagnitude < 0.0001f ? Vector3.forward : delta.normalized;
    }

    public static Vector3 SpineRight(float s)
    {
        Vector3 forward = SpineForward(s);
        return new Vector3(forward.z, 0f, -forward.x);
    }

    /// <summary>중심선 기준 좌우 오프셋을 준 월드 좌표. 배치는 거의 전부 이걸로 한다.</summary>
    public static Vector3 At(float s, float lateral) => SpinePoint(s) + SpineRight(s) * lateral;

    public static Quaternion Facing(float s) => Quaternion.LookRotation(SpineForward(s));

    // ==========================================================
    // 폭 - 구간 사이가 방처럼 끊기지 않게 부드럽게 이어 붙인다
    // ==========================================================

    /// <summary>
    /// 복도(맵 외곽) 반폭.
    ///
    /// 각 구간은 제 폭을 가운데 절반 동안 유지하고, 경계 부근에서만 다음 폭으로 넘어간다.
    /// 구간 중앙값만 이어 버리면 넓은 전투장에서 좁은 접근로까지 폭이 계속 완만하게 줄어들어
    /// 큰 깔때기가 생기고, 그 틈으로 유니콘 광장이 미리 보여 버린다.
    /// </summary>
    /// <summary>
    /// UnicornPlaza 전용 폭 배율(Rect 버전에서만). Areas[] 는 자유형과 공유하는 배열이라
    /// 거기서 직접 숫자를 바꾸면 자유형에도 영향을 주므로, 여기서만 조건부로 넓힌다.
    /// </summary>
    // 처음엔 27%(14.0m)로 넓혔는데, 배경 조경(Bush_32_1)이 다른 지점에서 스파인과
    // 너무 가까워지는 부작용이 실측으로 확인됐다(조경 코드는 이번 작업 범위 밖이라
    // 손대지 않음). 13.0~13.5m 사이에서 재실측해 안전한 상한을 찾았고, 정확히
    // 20% 확장인 13.2m 로 고정한다.
    private const float RectUnicornPlazaHalfWidth = 13.2f; // 기존 11.0m 대비 20% 확장

    // 주의: 이 값은 EndCap(마개) 크기(half*2+1.6)에도 그대로 쓰인다. 한 번 12로 올려봤더니
    // 스파인 맨 끝의 마개가 25.6m 폭으로 커지면서, 꼬리 쪽이 굽어 있는 탓에 마개가 광장
    // 한복판을 가로지르는 초록 대각선 판으로 보이는 버그가 생겼다(스크린샷으로 확인).
    // 그래서 이 값은 기존(8f) 그대로 두고, "뒤쪽 여유"는 광장 자체 폭(위 14.0)을 넓혀서
    // 유니콘 근처부터 이미 더 넓은 채로 시작하는 방식으로만 확보한다.
    private const float RectUnicornPlazaTailHalfWidth = 8.0f;

    public static float CorridorHalfWidth(float s)
    {
        float w = WalkLength;
        var keys = new List<Vector2> { new Vector2(0f, Areas[0].HalfWidth) };

        foreach (var area in Areas)
        {
            float a = area.StartF * w, b = area.EndF * w;
            float hold = (b - a) * 0.28f;
            float halfWidth = (Rectangular && area.Name == "UnicornPlaza") ? RectUnicornPlazaHalfWidth : area.HalfWidth;
            keys.Add(new Vector2(a + hold, halfWidth));
            keys.Add(new Vector2(b - hold, halfWidth));
        }

        float lastHalfWidth = Rectangular ? RectUnicornPlazaHalfWidth : Areas[Areas.Length - 1].HalfWidth;
        keys.Add(new Vector2(w, lastHalfWidth));
        // 광장 뒤쪽은 살짝 좁혀 닫는다 - Rect 버전은 유니콘 뒤쪽 여유를 위해 덜 좁힌다.
        keys.Add(new Vector2(spineLength, Rectangular ? RectUnicornPlazaTailHalfWidth : 8f));
        return SmoothSample(keys, s);
    }

    /// <summary>실제로 밟고 걷는 자갈길 리본의 반폭. 전투장과 광장에서만 크게 넓어진다.</summary>
    public static float RibbonHalfWidth(float s)
    {
        float w = WalkLength;
        var keys = new List<Vector2>
        {
            new Vector2(0f,          1.7f),
            new Vector2(0.21f * w,   1.5f),
            new Vector2(0.35f * w,   1.7f),
            new Vector2(0.49f * w,   1.5f),
            new Vector2(0.59f * w,   2.2f),
            new Vector2(0.70f * w,   3.6f),
            new Vector2(0.78f * w,   3.6f),
            new Vector2(0.87f * w,   1.4f),
            new Vector2(0.94f * w,   1.5f),
            new Vector2(w,           4.5f),
            new Vector2(spineLength, 5.0f),
        };
        // 길이 복도 벽을 뚫고 나가지 않도록 항상 여유를 남긴다
        return Mathf.Min(SmoothSample(keys, s), CorridorHalfWidth(s) - 0.8f);
    }

    /// <summary>키 사이를 smoothstep 으로 보간. AnimationCurve 와 달리 값이 튀지 않는다.</summary>
    private static float SmoothSample(List<Vector2> keys, float s)
    {
        if (s <= keys[0].x) return keys[0].y;
        for (int i = 1; i < keys.Count; i++)
        {
            if (s > keys[i].x) continue;
            float t = Mathf.InverseLerp(keys[i - 1].x, keys[i].x, s);
            return Mathf.Lerp(keys[i - 1].y, keys[i].y, t * t * (3f - 2f * t));
        }
        return keys[keys.Count - 1].y;
    }

    public static string AreaNameAt(float s)
    {
        float f = s / WalkLength;
        foreach (var area in Areas)
        {
            if (f >= area.StartF && f < area.EndF) return area.Name;
        }
        return f >= 1f ? Areas[Areas.Length - 1].Name : "-";
    }

    public static float AreaStartS(string name)
    {
        foreach (var a in Areas) if (a.Name == name) return a.StartF * WalkLength;
        return 0f;
    }

    public static float AreaEndS(string name)
    {
        foreach (var a in Areas) if (a.Name == name) return a.EndF * WalkLength;
        return WalkLength;
    }

    /// <summary>구간 안에서의 상대 위치(0~1)를 실제 s 로 바꾼다.</summary>
    public static float InArea(string name, float t)
    {
        float a = AreaStartS(name), b = AreaEndS(name);
        return Mathf.Lerp(a, b, t);
    }

    // ==========================================================
    // 복도 생성 - 바닥 / 양옆 벽 / 천장이 전부 중심선을 따라 휘어 나간다
    // ==========================================================
    private static void BuildCorridor(Transform ground, Transform boundaries, Transform ceiling)
    {
        if (Rectangular)
        {
            // 바깥은 직사각형 상자, 길 좌우는 벽 대신 울타리 수풀
            BuildRectShell(ground, boundaries, ceiling);
            BuildSoftBoundary(boundaries);
            return;
        }

        const float step = 0.8f;
        const float overlap = 1.18f;   // 곡선 바깥쪽에 틈이 생기지 않게 살짝 겹친다

        var leftWall = Child(boundaries, "Wall_Left");
        var rightWall = Child(boundaries, "Wall_Right");
        int index = 0;

        for (float s = 0f; s <= spineLength; s += step, index++)
        {
            float half = CorridorHalfWidth(s);
            Vector3 center = SpinePoint(s);
            Vector3 right = SpineRight(s);
            Quaternion rot = Facing(s);
            float length = step * overlap;

            Prim(PrimitiveType.Cube, $"Floor_{index:D3}", ground,
                 center + Vector3.down * 0.25f,
                 new Vector3(half * 2f, 0.5f, length), MatGrass, collider: true, rot: rot);

            MakeWallSegment(leftWall, $"WallL_{index:D3}", center - right * (half + 0.25f), length, rot);
            MakeWallSegment(rightWall, $"WallR_{index:D3}", center + right * (half + 0.25f), length, rot);

            var slab = Prim(PrimitiveType.Cube, $"Ceiling_{index:D3}", ceiling,
                            center + Vector3.up * (CeilingY + 0.25f),
                            new Vector3(half * 2f + 1f, 0.5f, length), MatSky, collider: true, rot: rot);
            slab.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // 양 끝 마개 - 플레이어가 복도 밖으로 나가지 못하게 한다
        MakeEndCap(boundaries, "Wall_Start", 0f, -1f);
        MakeEndCap(boundaries, "Wall_End", spineLength, 1f);
    }

    private static void MakeWallSegment(Transform parent, string name, Vector3 position, float length, Quaternion rot)
    {
        var wall = Prim(PrimitiveType.Cube, name, parent,
                        position + Vector3.up * (CeilingY * 0.5f),
                        new Vector3(0.5f, CeilingY + 1f, length), MatSky, collider: true, rot: rot);
        // 벽이 햇빛을 막아 맵이 어두워지지 않도록 그림자를 끈다
        wall.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void MakeEndCap(Transform parent, string name, float s, float direction)
    {
        float half = CorridorHalfWidth(s);
        var cap = Prim(PrimitiveType.Cube, name, parent,
                       SpinePoint(s) + SpineForward(s) * direction * 0.3f + Vector3.up * (CeilingY * 0.5f),
                       new Vector3(half * 2f + 1.5f, CeilingY + 1f, 0.5f), MatSky, collider: true, rot: Facing(s));
        cap.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // ==========================================================
    // 메인 길 - START 부터 광장까지 끊기지 않는 하나의 리본
    //
    // "손으로 자른 색지를 이어 붙인 공예 산책로" 처럼 보이도록
    //   - 크림 / 베이지 / 버터 / 아이보리 네 톤을 섞어서 한 장의 평평한 색으로 보이지 않게 한다
    //   - 경계마다 좌우 위치 / 폭을 아주 살짝만 흔들어 손으로 자른 가장자리 느낌을 준다
    // 시각 전용 리본이라 콜라이더가 없다 - 실제로 밟는 바닥은 BuildCorridor 의 Floor_* 가 그대로 맡는다.
    //
    // 예전에는 구간마다 회전된 독립적인 사각형(Cube)을 곡선 틈이 안 보이도록 35% 겹치게
    // 늘어놓았는데, 겹친 부분이 서로 정확히 같은 높이라 카메라가 움직이면 Z-fighting이
    // 났다. 지금은 각 조각을 "독립된 회전 사각형"이 아니라 이웃과 경계(왼쪽/오른쪽 끝점)를
    // 정확히 공유하는 사다리꼴(trapezoid) 리본 조각으로 만든다. 이웃 조각끼리 같은 점을
    // 공유하므로 틈도 겹침도 생기지 않고, 따라서 높이를 흔드는 보정도 필요 없다 -
    // 리본 전체가 처음부터 끝까지 같은 높이(0.025)를 유지한다.
    // ==========================================================
    private static void BuildMainPath(Transform parent)
    {
        const float step = 0.7f;
        var tones = new[] { MatPathCream, MatPathBeige, MatPathButter, MatPathIvory };
        var rng = new System.Random(5150);

        // 리본을 나눌 경계(스파인 위의 점)들을 먼저 전부 뽑는다. 지터도 "조각"이 아니라
        // "경계"마다 하나씩만 만들어서, 이웃한 두 조각이 서로 같은 경계점을 공유하게 한다.
        var boundS = new List<float>();
        for (float s = 0.6f; s <= spineLength - 1f; s += step) boundS.Add(s);
        if (boundS.Count < 2) { BuildPathDecor(parent); return; }

        int n = boundS.Count;
        var centers = new Vector3[n];
        var rightDir = new Vector3[n];
        var halfArr = new float[n];
        var leftPts = new Vector3[n];
        var rightPts = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float s = boundS[i];
            // 가장자리가 완벽한 직선이 아니라 살짝 손으로 자른 것처럼 - 단, 과하게 흔들면 울퉁불퉁해 보인다
            float edgeJitter = Lerp(-0.12f, 0.12f, (float)rng.NextDouble());
            float widthJitter = Lerp(0.95f, 1.05f, (float)rng.NextDouble());

            float half = RibbonHalfWidth(s) * widthJitter;

            // 접근로처럼 스파인이 급하게 꺾이는 자리에서는 리본 폭(half)이 그 지점의 곡률
            // 반지름보다 넓으면 리본의 안쪽 가장자리가 스스로를 가로질러 겹친다(오프셋
            // 곡선의 자기교차). 우선 그 지점만의 순간 곡률로 1차로 눌러 둔다 - 그래도
            // 급커브가 여러 경계에 걸쳐 이어지면 바로 옆이 아닌 몇 칸 떨어진 조각끼리
            // 겹칠 수 있어서, 아래에서 실제 겹침을 다시 검사해 완전히 없앤다.
            Vector3 fwdPrev = SpineForward(Mathf.Max(0f, s - step * 0.5f));
            Vector3 fwdNext = SpineForward(Mathf.Min(spineLength, s + step * 0.5f));
            float turnAngle = Vector3.Angle(fwdPrev, fwdNext) * Mathf.Deg2Rad;
            if (turnAngle > 0.0005f)
            {
                float curveRadius = step / turnAngle;
                half = Mathf.Min(half, curveRadius * 0.9f);
            }

            centers[i] = SpinePoint(s) + SpineRight(s) * edgeJitter + Vector3.up * 0.025f;
            rightDir[i] = SpineRight(s);
            halfArr[i] = half;
        }

        System.Action<int> rebuildPoint = (i) =>
        {
            leftPts[i] = centers[i] - rightDir[i] * halfArr[i];
            rightPts[i] = centers[i] + rightDir[i] * halfArr[i];
        };
        for (int i = 0; i < n; i++) rebuildPoint(i);

        // Spine/구조/콜라이더는 그대로 두고, 리본 메시의 폭(halfArr)만 실제로 겹치는 게
        // 없어질 때까지 문제가 되는 경계에서 반복적으로 조금씩 줄인다. 순간 곡률만으로는
        // 못 잡는, 급커브가 여러 경계에 걸쳐 누적되는 경우까지 확실히 잡아낸다.
        System.Func<int, int, bool> QuadsOverlap = (a, b) =>
        {
            Vector2[] pa = { new Vector2(leftPts[a].x, leftPts[a].z), new Vector2(rightPts[a].x, rightPts[a].z),
                             new Vector2(rightPts[a + 1].x, rightPts[a + 1].z), new Vector2(leftPts[a + 1].x, leftPts[a + 1].z) };
            Vector2[] pb = { new Vector2(leftPts[b].x, leftPts[b].z), new Vector2(rightPts[b].x, rightPts[b].z),
                             new Vector2(rightPts[b + 1].x, rightPts[b + 1].z), new Vector2(leftPts[b + 1].x, leftPts[b + 1].z) };
            var polys = new[] { pa, pb };
            foreach (var poly in polys)
            {
                for (int k = 0; k < poly.Length; k++)
                {
                    Vector2 p1 = poly[k], p2 = poly[(k + 1) % poly.Length];
                    Vector2 edge = p2 - p1;
                    Vector2 axis = new Vector2(-edge.y, edge.x);
                    if (axis.sqrMagnitude < 1e-8f) continue;
                    axis.Normalize();
                    float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue;
                    foreach (var pt in pa) { float d = Vector2.Dot(pt, axis); minA = Mathf.Min(minA, d); maxA = Mathf.Max(maxA, d); }
                    foreach (var pt in pb) { float d = Vector2.Dot(pt, axis); minB = Mathf.Min(minB, d); maxB = Mathf.Max(maxB, d); }
                    const float eps = 1e-4f;
                    if (maxA <= minB + eps || maxB <= minA + eps) return false;
                }
            }
            return true;
        };

        const float minHalf = 0.5f;
        for (int pass = 0; pass < 40; pass++)
        {
            bool anyOverlap = false;
            for (int a = 0; a < n - 1; a++)
            {
                Vector3 midA = (centers[a] + centers[a + 1]) * 0.5f;
                for (int b = a + 1; b < n - 1; b++)
                {
                    Vector3 midB = (centers[b] + centers[b + 1]) * 0.5f;
                    if ((midA - midB).sqrMagnitude > 15f * 15f) continue;
                    if (!QuadsOverlap(a, b)) continue;

                    anyOverlap = true;
                    foreach (int idx in new[] { a, a + 1, b, b + 1 })
                    {
                        halfArr[idx] = Mathf.Max(minHalf, halfArr[idx] * 0.92f);
                        rebuildPoint(idx);
                    }
                }
            }
            if (!anyOverlap) break;
        }

        int lastTone = -1;
        for (int i = 0; i < n - 1; i++)
        {
            int tone = rng.Next(tones.Length);
            if (tone == lastTone) tone = (tone + 1 + rng.Next(tones.Length - 1)) % tones.Length;
            lastTone = tone;

            // 이웃 조각과 leftPts[i+1] / rightPts[i+1] 을 그대로 공유하므로 여기서 끝나는
            // 자리가 곧 다음 조각이 시작하는 자리다 - 틈도, 겹침도 생길 수 없다.
            MakeRibbonQuad(parent, $"PathTile_{i:D3}",
                           leftPts[i], rightPts[i], rightPts[i + 1], leftPts[i + 1], tones[tone]);
        }

        BuildPathDecor(parent);
    }

    /// <summary>
    /// 네 점(근접-왼쪽, 근접-오른쪽, 원거리-오른쪽, 원거리-왼쪽)으로 이루어진 평평한 사다리꼴
    /// 메시 한 장을 만든다. Prim() 과 달리 회전된 상자가 아니라 정확히 그 네 점을 잇는 면이라,
    /// 이웃 조각과 점을 공유하는 이상 겹치거나 틈이 생기지 않는다.
    /// </summary>
    private static GameObject MakeRibbonQuad(Transform parent, string name,
                                             Vector3 nearLeft, Vector3 nearRight,
                                             Vector3 farRight, Vector3 farLeft, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var mesh = new Mesh { name = name };
        mesh.vertices = new[] { nearLeft, farLeft, farRight, nearRight };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;

        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    /// <summary>구간별 길 장식 밀도 - 전투 가독성이 필요한 곳은 0으로 비운다.</summary>
    private struct PathDecorProfile
    {
        public float PatchChance;
        public float StoneChance;
    }

    private static PathDecorProfile PathDecorProfileFor(string area)
    {
        switch (area)
        {
            case "StartGarden":      return new PathDecorProfile { PatchChance = 0.10f, StoneChance = 0.05f };
            case "ClueWalk":         return new PathDecorProfile { PatchChance = 0.22f, StoneChance = 0.18f };
            case "GreenhousePond":   return new PathDecorProfile { PatchChance = 0.16f, StoneChance = 0.10f };
            case "WeaponLink":       return new PathDecorProfile { PatchChance = 0.08f, StoneChance = 0.04f };
            case "TutorialNook":     return new PathDecorProfile { PatchChance = 0.08f, StoneChance = 0.04f };
            case "CombatArena":      return new PathDecorProfile { PatchChance = 0f, StoneChance = 0f };
            case "UnicornApproach":  return new PathDecorProfile { PatchChance = 0.20f, StoneChance = 0.26f };
            case "UnicornPlaza":     return new PathDecorProfile { PatchChance = 0.08f, StoneChance = 0f };
            default:                 return new PathDecorProfile { PatchChance = 0.1f, StoneChance = 0.05f };
        }
    }

    /// <summary>
    /// 길 위 자투리 색지 패치 + 디딤돌.
    ///
    /// 전투장은 가독성이 가장 중요해 완전히 비운다. 나머지 구간은 구간 성격에 맞춰 밀도만 다르게 둔다.
    /// 전부 콜라이더 없는 장식이라 실제 걷는 바닥(Floor_*)에는 아무 영향이 없다.
    /// </summary>
    private static void BuildPathDecor(Transform parent)
    {
        var patches = Child(parent, "PathPatches");
        var stones = Child(parent, "PathStepStones");
        var rng = new System.Random(6161);
        int patchIndex = 0, stoneIndex = 0;

        for (float s = 1.2f; s <= spineLength - 2f; s += 0.9f)
        {
            var profile = PathDecorProfileFor(AreaNameAt(s));
            if (profile.PatchChance <= 0f && profile.StoneChance <= 0f) continue;

            float half = RibbonHalfWidth(s);

            if (rng.NextDouble() < profile.PatchChance)
            {
                MakePathPatch(patches, $"Patch_{patchIndex:D3}", s, half, rng);
                patchIndex++;
            }

            if (rng.NextDouble() < profile.StoneChance)
            {
                MakeStepStone(stones, $"StepStone_{stoneIndex:D3}", s, half, rng);
                stoneIndex++;
            }
        }

        BuildGreenhouseApproachDeck(parent);
    }

    /// <summary>얇은 색지 조각 한 장 - 가끔 좁고 긴 tape 형태로도 나온다.</summary>
    private static void MakePathPatch(Transform parent, string name, float s, float half, System.Random rng)
    {
        Vector3 basePos = SpinePoint(s) + SpineRight(s) * Lerp(-half * 0.7f, half * 0.7f, (float)rng.NextDouble());
        bool tape = rng.NextDouble() < 0.25;
        Material mat = rng.Next(3) switch
        {
            0 => MatPathPaperLight,
            1 => MatPathIvory,
            _ => MatPathButter,
        };

        float rotY = Lerp(0f, 360f, (float)rng.NextDouble());
        Vector3 size = tape
            ? new Vector3(Lerp(0.16f, 0.24f, (float)rng.NextDouble()), 0.01f, Lerp(0.5f, 0.85f, (float)rng.NextDouble()))
            : new Vector3(Lerp(0.4f, 0.7f, (float)rng.NextDouble()), 0.01f, Lerp(0.3f, 0.55f, (float)rng.NextDouble()));

        Prim(PrimitiveType.Cube, name, parent, basePos + Vector3.up * 0.052f, size, mat,
             rot: Quaternion.Euler(0f, rotY, 0f));
    }

    /// <summary>디딤돌 한 장 - 손으로 자른 종이판 느낌으로 크기와 회전을 조금씩 흔든다.</summary>
    private static void MakeStepStone(Transform parent, string name, float s, float half, System.Random rng)
    {
        float side = rng.Next(2) == 0 ? -1f : 1f;
        float lateral = side * Lerp(half * 0.25f, half * 0.65f, (float)rng.NextDouble());
        Vector3 pos = SpinePoint(s) + SpineRight(s) * lateral;

        float radius = Lerp(0.40f, 0.58f, (float)rng.NextDouble());
        float squash = Lerp(0.82f, 1.0f, (float)rng.NextDouble());
        float rotY = Lerp(0f, 360f, (float)rng.NextDouble());

        Prim(PrimitiveType.Cylinder, name, parent, pos + Vector3.up * 0.033f,
             new Vector3(radius, 0.033f, radius * squash),
             MatPathStepIvory, rot: Quaternion.Euler(0f, rotY, 0f));
    }

    /// <summary>
    /// 온실 입구로 이어지는 짧은 가지길.
    ///
    /// Rect 버전 공예 온실에서만 자리가 확실하다(자유형은 예전 프리미티브 온실이라 위치가 다르다).
    /// 그래서 이 가지길은 Rect 버전에서만 놓는다. 출입구 앞 KeepClear 반경 안쪽은 건드리지 않는다.
    /// </summary>
    private static void BuildGreenhouseApproachDeck(Transform parent)
    {
        if (!Rectangular) return;

        float houseS = GreenhouseS;
        Vector3 pathStart = At(houseS, RibbonHalfWidth(houseS) * -0.6f);
        Vector3 doorApproach = GreenhouseCenter + SpineRight(houseS) * 5.6f;

        var deck = Child(parent, "GreenhouseApproachDeck");
        const int steps = 4;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 p = Vector3.Lerp(pathStart, doorApproach, t);
            Prim(PrimitiveType.Cylinder, $"Deck_{i}", deck, p + Vector3.up * 0.03f,
                 new Vector3(0.6f, 0.03f, 0.6f), MatPathStepIvory);
        }
    }

    private static BrightDreamPath BuildPathData(Transform mainPath)
    {
        var path = mainPath.gameObject.AddComponent<BrightDreamPath>();

        var waypoints = new List<Vector3>();
        for (float s = PlayerStartS; s < UnicornS - 2.5f; s += 1.5f) waypoints.Add(SpinePoint(s));
        waypoints.Add(SpinePoint(UnicornS - 2.5f));
        path.waypoints = waypoints.ToArray();

        var areas = new BrightDreamPath.Area[Areas.Length];
        for (int i = 0; i < Areas.Length; i++)
        {
            areas[i] = new BrightDreamPath.Area
            {
                name = Areas[i].Name,
                startZ = Areas[i].StartF * WalkLength,
                endZ = Areas[i].EndF * WalkLength,
            };
        }
        path.areas = areas;
        path.useArcLength = true;
        return path;
    }

    // ==========================================================
    // Scene / 폴더 준비
    // ==========================================================
    private static Scene CreateWorkingScene()
    {
        bool anyDirty = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty) anyDirty = true;
        }

        if (anyDirty)
        {
            Debug.LogWarning("[BrightDream] 열려 있는 Scene 에 저장하지 않은 변경이 있어 " +
                             "기존 Scene 을 닫지 않고 Additive 로 만든다.");
            var additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(additive);
            return additive;
        }
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.CreateFolder("Assets", "BrightDream");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(RootFolder, "Materials");
    }

    // ==========================================================
    // Material
    // ==========================================================
    private static void CreateMaterials()
    {
        MatGrass = Mat("Grass", new Color(0.52f, 0.72f, 0.46f));
        MatGrassAlt = Mat("GrassAlt", new Color(0.45f, 0.65f, 0.41f));

        // 공예 색지 산책로 - 한 톤만 쓰면 평평한 카펫처럼 보여서 아주 가까운 파스텔 네 가지를 섞는다
        MatPathCream = Mat("Path_Cream", new Color(0.95f, 0.90f, 0.78f));
        MatPathBeige = Mat("Path_Beige", new Color(0.88f, 0.79f, 0.64f));
        MatPathButter = Mat("Path_Butter", new Color(0.96f, 0.89f, 0.68f));
        MatPathIvory = Mat("Path_Ivory", new Color(0.95f, 0.93f, 0.86f));
        // 길 위에 덧대는 자투리 색지 - 바닥 톤보다 살짝 더 하얗게 두어 "덧댄 조각" 임을 읽히게 한다
        MatPathPaperLight = Mat("Path_PaperLight", new Color(0.97f, 0.95f, 0.89f));
        // 디딤돌 - 종이/폼보드 느낌의 아이보리, 바닥보다 살짝 차분하게
        MatPathStepIvory = Mat("Path_StepIvory", new Color(0.92f, 0.91f, 0.85f));

        MatSand = Mat("Sand", new Color(0.76f, 0.66f, 0.50f));
        MatStone = Mat("Stone", new Color(0.70f, 0.70f, 0.75f));

        MatSky = Mat("Sky", new Color(0.64f, 0.84f, 0.96f), unlit: true);
        MatCloud = Mat("Cloud", new Color(0.93f, 0.94f, 0.97f));
        MatSun = Mat("Sun", new Color(0.97f, 0.82f, 0.35f));
        MatStar = Mat("Star", new Color(0.96f, 0.79f, 0.33f));

        MatTrunk = Mat("Trunk", new Color(0.60f, 0.44f, 0.33f));
        MatLeaf = Mat("Leaf", new Color(0.47f, 0.70f, 0.42f));
        MatLeafAlt = Mat("LeafAlt", new Color(0.39f, 0.62f, 0.38f));
        MatLeafDeep = Mat("LeafDeep", new Color(0.31f, 0.52f, 0.33f));
        // 나무를 덜어 낸 자리를 채우는 낮은 조경용 - 잎보다 한 톤 밝은 파스텔
        MatLeafSoft = Mat("LeafSoft", new Color(0.66f, 0.83f, 0.60f));

        // 열린 잔디밭과 종이 언덕 - 공예 색지처럼 톤을 흔든다
        MatMeadow = Mat("Meadow", new Color(0.63f, 0.80f, 0.53f));
        MatHillMint = Mat("HillMint", new Color(0.66f, 0.84f, 0.71f));
        MatHillPeach = Mat("HillPeach", new Color(0.94f, 0.80f, 0.66f));
        MatHillLilac = Mat("HillLilac", new Color(0.80f, 0.77f, 0.91f));

        MatFence = Mat("Fence", new Color(0.89f, 0.88f, 0.84f));
        MatWood = Mat("Wood", new Color(0.70f, 0.54f, 0.38f));
        MatGlass = Mat("Glass", new Color(0.72f, 0.88f, 0.93f, 0.25f), transparent: true);
        MatWater = Mat("Water", new Color(0.50f, 0.76f, 0.87f, 0.80f), transparent: true);

        MatPink = Mat("FlowerPink", new Color(0.94f, 0.58f, 0.72f));
        MatYellow = Mat("FlowerYellow", new Color(0.97f, 0.83f, 0.42f));
        MatPurple = Mat("FlowerPurple", new Color(0.70f, 0.60f, 0.88f));
        MatWhiteFlower = Mat("FlowerWhite", new Color(0.94f, 0.93f, 0.89f));

        MatEnemy = Mat("Enemy", new Color(0.20f, 0.18f, 0.26f));
        MatUnicorn = Mat("Unicorn", new Color(0.93f, 0.91f, 0.94f));
        MatMane = Mat("UnicornMane", new Color(0.93f, 0.58f, 0.78f));
        MatGold = Mat("Gold", new Color(0.94f, 0.75f, 0.30f));

        MatMarkerClue = Mat("MarkerClue", new Color(1.00f, 0.83f, 0.25f), emission: new Color(0.55f, 0.42f, 0.08f));
        MatMarkerWeapon = Mat("MarkerWeapon", new Color(0.40f, 0.90f, 0.95f), emission: new Color(0.10f, 0.42f, 0.48f));
        MatMarkerEnemy = Mat("MarkerEnemy", new Color(0.92f, 0.32f, 0.36f), emission: new Color(0.42f, 0.06f, 0.08f));
        MatMarkerDream = Mat("MarkerDream", new Color(0.78f, 0.55f, 0.95f), emission: new Color(0.34f, 0.16f, 0.48f));
    }

    private static Material Mat(string name, Color color, bool unlit = false, bool transparent = false, Color? emission = null)
    {
        string assetPath = $"{MaterialFolder}/BD_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        var shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        material.shader = shader;
        material.color = color;

        if (!unlit)
        {
            material.SetFloat("_Glossiness", 0.05f);
            material.SetFloat("_Metallic", 0f);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.SetColor("_EmissionColor", emission.Value);
            }
            if (transparent) MakeTransparent(material);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void MakeTransparent(Material material)
    {
        material.SetFloat("_Mode", 3f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    // ==========================================================
    // 조명
    // ==========================================================
    private static void ApplyLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.80f, 0.78f, 0.72f);
        // 아래를 향한 면(솜구름 밑면, 수관 아래, 아치 안쪽)이 받는 색.
        // 어두운 초록으로 두면 올려다본 구름이 잿빛으로 가라앉아
        // "하늘색 천장 / 구름 / 별이 잘 보인다" 는 목표가 깨진다. 색지처럼 밝게 잡는다.
        RenderSettings.ambientGroundColor = new Color(0.68f, 0.70f, 0.64f);
        RenderSettings.ambientIntensity = 0.62f;
        RenderSettings.fog = false;
        RenderSettings.skybox = null;

        var lightGO = new GameObject("Directional Light", typeof(Light));
        var light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.00f, 0.96f, 0.88f);
        light.intensity = 0.78f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.5f;
        lightGO.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
    }

    // ==========================================================
    // 플레이어
    // ==========================================================
    private static void BuildPlayer(Transform root, BrightDreamPath path)
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.SetParent(root, false);
        playerGO.transform.position = SpinePoint(PlayerStartS) + Vector3.up * 0.2f;
        playerGO.transform.rotation = Facing(PlayerStartS);

        var controller = playerGO.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 50f;

        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.SetParent(playerGO.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

        var camera = cameraGO.GetComponent<Camera>();
        camera.fieldOfView = FieldOfView;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 220f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.64f, 0.84f, 0.96f);

        var fps = playerGO.AddComponent<SimpleFirstPersonController>();
        var serialized = new SerializedObject(fps);
        serialized.FindProperty("cameraTransform").objectReferenceValue = cameraGO.transform;
        serialized.FindProperty("path").objectReferenceValue = path;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ==========================================================
    // 검증 도구
    // ==========================================================

    [MenuItem("Tools/Bright Dream/Validate Path Clearance")]
    public static void ValidatePathClearance()
    {
        BuildSpineTable();
        Physics.SyncTransforms();

        const float playerRadius = 0.45f;
        var report = new System.Text.StringBuilder();
        int blocked = 0;

        for (float s = PlayerStartS; s <= UnicornS - 2f; s += 0.5f)
        {
            Vector3 point = SpinePoint(s);
            foreach (var hit in Physics.OverlapCapsule(point + Vector3.up * 0.5f, point + Vector3.up * 1.4f, playerRadius))
            {
                string name = hit.transform.name;
                string parent = hit.transform.parent == null ? "-" : hit.transform.parent.name;
                if (name == "Player" || name.StartsWith("Floor") || name.StartsWith("PathTile")) continue;
                if (parent == "Bridge" || parent == "Unicorn_Placeholder") continue;
                report.AppendLine($"  s={s,5:F1}  {parent}/{name}");
                blocked++;
            }
        }

        if (blocked == 0) Debug.Log($"[BrightDream] 길 통행 검사 통과 - 0 ~ {UnicornS:F1}m 사이에 막힌 곳 없음.");
        else Debug.LogWarning($"[BrightDream] 길을 막는 오브젝트 {blocked}건:\n{report}");
    }

    [MenuItem("Tools/Bright Dream/Validate Sightlines")]
    public static void ValidateSightlines()
    {
        BuildSpineTable();
        Physics.SyncTransforms();

        var report = new System.Text.StringBuilder();
        report.AppendLine("===== 시야 거리 (눈높이 1.6m) =====");
        for (float s = PlayerStartS; s <= UnicornS - 2f; s += 4f)
        {
            report.AppendLine($"  s={s,5:F1}  앞으로 {VisiblePathDistance(s),5:F1}m   [{AreaNameAt(s)}]");
        }
        Debug.Log(report.ToString());
    }

    public static float VisiblePathDistance(float fromS)
    {
        Vector3 eye = SpinePoint(fromS) + Vector3.up * EyeHeight;
        for (float ahead = 2f; ahead <= 60f; ahead += 1f)
        {
            float target = fromS + ahead;
            if (target > spineLength - 1f) return ahead;
            if (Physics.Linecast(eye, SpinePoint(target) + Vector3.up * EyeHeight)) return ahead;
        }
        return 60f;
    }

    // ==========================================================
    // 공통 도우미
    // ==========================================================
    internal static readonly List<Vector4> KeepClear = new List<Vector4>();

    /// <summary>
    /// 나무를 세우면 안 되는 자리 (x, y, z, 반지름).
    ///
    /// 온실 / 연못 / 트리하우스 / 그네처럼 "이게 뭔지 한눈에 읽혀야 하는" 정원 요소들이다.
    /// 경계 조경이 이 위에 나무를 덮어 버리면 어린아이 꿈속 정원의 주인공이 가려진다.
    /// </summary>
    internal static readonly List<Vector4> NoTreeZones = new List<Vector4>();

    internal static bool IsNoTreeZone(Vector3 spot)
    {
        foreach (var zone in NoTreeZones)
        {
            Vector3 center = new Vector3(zone.x, zone.y, zone.z);
            if ((new Vector3(spot.x, center.y, spot.z) - center).sqrMagnitude < zone.w * zone.w) return true;
        }
        return false;
    }

    internal static bool IsKeptClear(Vector3 spot)
    {
        foreach (var zone in KeepClear)
        {
            Vector3 center = new Vector3(zone.x, zone.y, zone.z);
            if ((new Vector3(spot.x, center.y, spot.z) - center).sqrMagnitude < zone.w * zone.w) return true;
        }
        return false;
    }

    internal static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    internal static GameObject Prim(PrimitiveType type, string name, Transform parent,
                                    Vector3 position, Vector3 scale, Material material,
                                    bool collider = false, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.rotation = rot.Value;

        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());

        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    internal static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // ==========================================================
    // 완성 후 수치 리포트
    // ==========================================================
    private static void LogMetrics(Transform root)
    {
        var bounds = new Bounds(SpinePoint(0f), Vector3.zero);
        for (float s = 0f; s <= spineLength; s += 1f)
        {
            float half = CorridorHalfWidth(s);
            bounds.Encapsulate(At(s, half));
            bounds.Encapsulate(At(s, -half));
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("===== Bright Dream 블록아웃 (꺾인 외곽) =====");
        report.AppendLine($"중심선 전체 길이 : {spineLength:F1}m (유니콘 뒤 여유 {TailLength}m 포함)");
        report.AppendLine($"START -> 유니콘  : {WalkLength:F1}m / 걷기 3.0m/s 기준 {WalkLength / 3f:F1}초");
        report.AppendLine($"외곽 바운딩      : {bounds.size.x:F1}m x {bounds.size.z:F1}m x {CeilingY}m");
        if (Rectangular)
        {
            var rect = RectFootprint();
            report.AppendLine($"직사각형 상자    : {rect.size.x:F1}m x {rect.size.z:F1}m x {CeilingY}m");
        }
        report.AppendLine("--- 구간별 ---");
        foreach (var area in Areas)
        {
            float a = area.StartF * WalkLength, b = area.EndF * WalkLength;
            report.AppendLine($"{area.Name,-16} s {a,5:F1} ~ {b,5:F1}  길이 {b - a,5:F1}m  폭 {area.HalfWidth * 2f,4:F0}m");
        }
        int trees = 0, bushes = 0, beds = 0, meadows = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.childCount == 0) continue;
            if (t.name.StartsWith("Tree_") || t.name == "PhotoTree") trees++;
            else if (t.name.StartsWith("Bush_") || t.name.StartsWith("EdgeBush_") || t.name.StartsWith("BackBush_")) bushes++;
            else if (t.name.StartsWith("FlowerBed") || t.name.StartsWith("Bed_") || t.name.StartsWith("EdgeBed_")) beds++;
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Meadow_")) meadows++;

        report.AppendLine("--- 조경 밀도 ---");
        report.AppendLine($"나무 {trees}그루 / 수풀 {bushes} / 꽃밭 {beds} / 열린 잔디밭 {meadows}");
        report.AppendLine($"총 GameObject 수 : {root.GetComponentsInChildren<Transform>(true).Length}");
        Debug.Log(report.ToString());
    }
}
