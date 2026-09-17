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

    // ==========================================================
    // Builder-생성 영역 / 수동 배치 영역 분리 (Rect 전용)
    //
    // 문제: 지금까지는 "Build Rect Blockout Scene" 이 매번 씬 전체를 통째로 새로
    // 만들어(CreateWorkingScene) 저장했다 - 사용자가 Hierarchy 에서 손으로 옮기거나
    // 삭제한 것까지 전부 날아갔다.
    //
    // 해결: Rect 씬을 재빌드할 때, 이미 저장된 Rect 씬이 있으면 그 씬을 그대로 열어
    // 재사용하고, __GENERATED 안의 내용만 지우고 다시 만든다. __MANUAL_LAYOUT 은
    // 이 빌드 패스 동안 단 한 번도 이동/삭제/재부모화하지 않는다 - 있으면 있는 그대로
    // 참조만 하고 끝낸다. 자유형 씬은 이 분리를 적용하지 않는다(기존 동작 그대로).
    // ==========================================================
    private const string GeneratedName = "__GENERATED";
    private const string ManualLayoutName = "__MANUAL_LAYOUT";

    /// <summary>이번 빌드 패스에서 찾은(또는 새로 만든) __MANUAL_LAYOUT - IsManuallyOwned 판정에 쓴다.</summary>
    private static Transform CurrentManualLayout;

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
    /// <summary>[톤(Mint/Sage/Pale/Cream), 변주(0~2)] - FeltTileMaterial 에서 해시로 골라 쓴다.</summary>
    private static Material[,] MatFeltTiles;

    private static readonly Color[] FeltToneColors =
    {
        new Color(0.50f, 0.72f, 0.60f), // Mint - 또렷한 민트
        new Color(0.30f, 0.44f, 0.32f), // Sage - 가장 어둡고 깊은 세이지 (톤 간 대비를 크게 벌리는 축)
        new Color(0.58f, 0.72f, 0.50f), // Pale - 넷 중 가장 밝지만, 노이즈가 얹혀도 Path 최저 밝기보단 확실히 낮게
        new Color(0.62f, 0.58f, 0.36f), // Cream - 흰색 대신 올리브빛 크림
    };
    private static readonly string[] FeltToneNames = { "Felt_Mint", "Felt_Sage", "Felt_Pale", "Felt_Cream" };
    private const int FeltVariantCount = 3;
    private static Material MatSky, MatCloud, MatSun, MatStar;
    private static Material[] MatCloudTones;
    private static Material MatCloudString;
    private static Material[] MatPathStoneTones;
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
        bushCraftKept = 0;
        BuildSpineTable();

        Scene scene;
        Transform root;
        bool reused = false;
        if (rectangular) reused = TryGetExistingRectScene(out scene, out root);
        else { scene = default; root = null; }
        if (!reused)
        {
            scene = CreateWorkingScene();
            root = new GameObject("BrightDream_Blockout").transform;
        }
        ApplyLighting();

        // __GENERATED / __MANUAL_LAYOUT 분리는 Rect 전용이다. 자유형은 예전처럼
        // 모든 procedural object 가 root 바로 아래에 생긴다(동작 변화 없음).
        Transform generated = root;
        Transform manualLayout = null;
        if (Rectangular)
        {
            if (reused)
            {
                // 재사용 빌드 - __GENERATED 만 지우고 다시 만든다. __MANUAL_LAYOUT 은
                // 존재하면 손끝 하나 대지 않고 그대로 참조만 한다(이동/재생성/삭제 없음).
                var oldGenerated = root.Find(GeneratedName);
                if (oldGenerated != null) Object.DestroyImmediate(oldGenerated.gameObject);
            }
            generated = Child(root, GeneratedName);
            manualLayout = EnsureManualLayout(root);
        }
        CurrentManualLayout = manualLayout;

        var boundaries = Child(generated, "Boundaries");
        var ceiling = Child(generated, "Ceiling");
        var ground = Child(generated, "Ground");
        var mainPath = Child(generated, "MainPath");
        var startGarden = Child(generated, "StartGarden");
        var clueWalk = Child(generated, "ClueWalk");
        var greenhousePond = Child(generated, "GreenhousePond");
        var weaponLink = Child(generated, "WeaponLink");
        var tutorialNook = Child(generated, "TutorialNook");
        var combatArena = Child(generated, "CombatArena");
        var unicornApproach = Child(generated, "UnicornApproach");
        var unicornPlaza = Child(generated, "UnicornPlaza");
        var landscaping = Child(generated, "Landscaping");
        var markers = Child(generated, "Markers");

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
        if (Rectangular && !IsManuallyOwned("TestGrass38")) BuildTestGrass38Scatter(landscaping);
        if (Rectangular && !IsManuallyOwned("PathStones")) BuildPathStones(mainPath);
        BuildCeilingDecorations(ceiling);

        if (Rectangular)
        {
            // 조경은 온실보다 먼저 깔리므로, 다 깐 뒤에 겹치는 것만 걷어낸다.
            // generated 안에서만 찾는다 - __MANUAL_LAYOUT 에 있는 동명 오브젝트까지
            // 쓸어버리면 안 된다.
            var craftGreenhouse = generated.Find("GreenhousePond/CraftGardenGreenhouse");
            ClearGreenhouseOverlap(generated, craftGreenhouse != null ? craftGreenhouse.gameObject : null);
        }

        var path = BuildPathData(mainPath);
        BuildPlayer(generated, path);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, CurrentScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LogMetrics(generated);
        CurrentManualLayout = null;
        buildingRectangular = false;
    }

    /// <summary>
    /// 이미 저장된 Rect 씬이 있으면 그 씬을 열어(또는 이미 열려 있으면 그대로) 재사용한다.
    /// __MANUAL_LAYOUT 을 보존하기 위한 전제 조건 - 이 함수가 true 를 반환해야만
    /// "기존 씬 재사용" 경로를 탄다. 현재 열린 씬(어느 씬이든)에 저장 안 한 변경이 있으면
    /// 안전하게 false 를 반환해 예전처럼 새 씬을 만들게 한다(저장 안 된 변경을 덮어쓰지 않는다).
    /// </summary>
    private static bool TryGetExistingRectScene(out Scene scene, out Transform root)
    {
        root = null;
        var active = SceneManager.GetActiveScene();
        if (active.path == RectScenePath && active.isLoaded)
        {
            scene = active;
        }
        else if (System.IO.File.Exists(RectScenePath))
        {
            if (AnySceneDirty())
            {
                Debug.LogWarning("[BrightDream] 열려 있는 Scene 에 저장하지 않은 변경이 있어 " +
                                 "저장된 Rect Scene 을 자동으로 열지 않는다 - 새 Scene 으로 빌드한다. " +
                                 "__MANUAL_LAYOUT 을 보존하려면 먼저 저장한 뒤 다시 실행할 것.");
                scene = default;
                return false;
            }
            scene = EditorSceneManager.OpenScene(RectScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = default;
            return false;
        }

        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "BrightDream_Blockout") { root = go.transform; return true; }
        }
        return false;
    }

    private static bool AnySceneDirty()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty) return true;
        }
        return false;
    }

    /// <summary>있으면 그대로 반환(절대 건드리지 않는다), 없으면 새로 만든다.</summary>
    private static Transform EnsureManualLayout(Transform root)
    {
        var existing = root.Find(ManualLayoutName);
        if (existing != null) return existing;
        return Child(root, ManualLayoutName);
    }

    /// <summary>
    /// __MANUAL_LAYOUT 안(재귀적으로) 어딘가에 이 이름의 오브젝트가 이미 있는지.
    /// 있으면 Builder 가 같은 이름으로 새로 만들지 않도록 호출부에서 건너뛴다.
    /// </summary>
    private static bool IsManuallyOwned(string name)
    {
        if (CurrentManualLayout == null) return false;
        foreach (var t in CurrentManualLayout.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return true;
        }
        return false;
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
        if (AnySceneDirty())
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

        // Rect 전용 펠트 바닥 - 자유형의 MatGrass/MatGrassAlt 는 그대로 두고 별도 재질로만 쓴다.
        // 톤 x 변주 조합을 해시로 섞어 체크무늬 대신 패치워크로 보이게 한다.
        MatFeltTiles = new Material[FeltToneColors.Length, FeltVariantCount];
        for (int tone = 0; tone < FeltToneColors.Length; tone++)
        {
            for (int variant = 0; variant < FeltVariantCount; variant++)
            {
                MatFeltTiles[tone, variant] = MatFelt(FeltToneNames[tone] + "_V" + variant, FeltToneColors[tone], variant);
            }
        }

        // 공예 색지 산책로 - 순백/차가운 톤 대신 Warm Cream/Beige/Ivory 로. 한 톤만 쓰면
        // 평평한 카펫처럼 보여서 아주 가까운 파스텔 네 가지를 섞는다. 각 재질에는 약한
        // felt/paper 표면 결(GeneratePathSurfaceTexture/Normal)을 얹어 밋밋한 단색을 깬다 -
        // Ground Felt 보다 약한 진폭이라(Bump 0.32 vs Ground 0.85) 사실적 흙/돌 텍스처처럼
        // 보이지 않는다. 1인칭에서 "흰 띠"로 날아 보이던 문제를 줄이려고 명도를 한 단계 더
        // 낮췄다 - 그래도 Ground(felt 최대 밝기 0.749)보다는 항상 밝게 여유를 남겼다.
        MatPathCream = MatPathSurface("Path_Cream", new Color(0.88f, 0.80f, 0.62f));
        MatPathBeige = MatPathSurface("Path_Beige", new Color(0.87f, 0.78f, 0.60f));
        MatPathButter = MatPathSurface("Path_Butter", new Color(0.88f, 0.78f, 0.54f));
        MatPathIvory = MatPathSurface("Path_Ivory", new Color(0.86f, 0.80f, 0.65f));
        // 길 위에 덧대는 자투리 색지 - 바닥 톤보다 살짝 더 밝게 두어 "덧댄 조각" 임을 읽히게 한다
        MatPathPaperLight = MatPathSurface("Path_PaperLight", new Color(0.90f, 0.83f, 0.67f));
        // 디딤돌 - 종이/폼보드 느낌의 아이보리, 바닥보다 살짝 차분하게
        MatPathStepIvory = MatPathSurface("Path_StepIvory", new Color(0.85f, 0.79f, 0.65f));

        MatSand = Mat("Sand", new Color(0.76f, 0.66f, 0.50f));
        MatStone = Mat("Stone", new Color(0.70f, 0.70f, 0.75f));

        MatSky = Mat("Sky", new Color(0.64f, 0.84f, 0.96f), unlit: true);
        MatCloud = Mat("Cloud", new Color(0.93f, 0.94f, 0.97f));
        MatSun = Mat("Sun", new Color(0.97f, 0.82f, 0.35f));
        MatStar = Mat("Star", new Color(0.96f, 0.79f, 0.33f));

        // 천장 종이 구름 cutout 전용 - 완전 흰색 대신 아이보리/크림/옅은 블루그레이를 섞어
        // 반복감을 줄인다. 매다는 실은 구름 몸판과 분리된 화이트/오프화이트로 둔다.
        //
        // 구름은 얇게 눌린 판이라 아랫면(플레이어가 보는 면)의 법선이 아래를 향해, 위에서
        // 내리쬐는 방향광을 거의 못 받는다 - 빛을 그대로 두면 아랫면이 원색과 다른 탁한
        // 회갈색으로 어둡게 보인다(측면 음영과는 다른, 조명 각도 때문에 생기는 결함).
        // 은은한 자체발광을 살짝 얹어 방향광이 안 닿아도 원래 톤이 유지되게 한다 - 새 셰이더
        // 없이 기존 Standard 셰이더의 Emission 채널만 쓴다.
        System.Func<Color, Color> softGlow = c => c * 0.3f;
        MatCloudTones = new[]
        {
            Mat("Cloud_Ivory",    new Color(0.96f, 0.94f, 0.87f), glossiness: 0.09f, emission: softGlow(new Color(0.96f, 0.94f, 0.87f))),
            Mat("Cloud_Cream",    new Color(0.97f, 0.93f, 0.81f), glossiness: 0.07f, emission: softGlow(new Color(0.97f, 0.93f, 0.81f))),
            Mat("Cloud_BlueGray", new Color(0.88f, 0.91f, 0.95f), glossiness: 0.13f, emission: softGlow(new Color(0.88f, 0.91f, 0.95f))),
        };
        MatCloudString = Mat("Cloud_String", new Color(0.95f, 0.95f, 0.93f), glossiness: 0.05f);

        // 길가 조약돌/자갈 전용 - Metallic 0, 낮은 Smoothness의 매트한 felt/foam 느낌.
        // 순백/차가운 톤을 피하고 Cream/Ivory/WarmBeige 를 주색으로 둔다. PaleGray 는 아주
        // 소량만, Pink/Lavender/Butter 포인트도 합쳐서 전체의 15~20% 이내로만 섞는다
        // (실제 배치 확률은 BuildPathStones 의 pickTone 가중치가 결정한다).
        // Path 재질 밝기와 겹치도록 톤을 한 단계 낮춰, 돌만 유독 하얗게 튀지 않게 했다.
        MatPathStoneTones = new[]
        {
            Mat("PathStone_Cream",     new Color(0.87f, 0.79f, 0.61f), glossiness: 0.09f),
            Mat("PathStone_Ivory",     new Color(0.85f, 0.80f, 0.66f), glossiness: 0.08f),
            Mat("PathStone_WarmBeige", new Color(0.80f, 0.68f, 0.50f), glossiness: 0.09f),
            Mat("PathStone_PaleGray",  new Color(0.74f, 0.70f, 0.66f), glossiness: 0.11f),
            Mat("PathStone_Pink",      new Color(0.85f, 0.68f, 0.70f), glossiness: 0.10f),
            Mat("PathStone_Lavender",  new Color(0.73f, 0.69f, 0.80f), glossiness: 0.10f),
            Mat("PathStone_Butter",    new Color(0.87f, 0.75f, 0.48f), glossiness: 0.10f),
        };

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

    private static Material Mat(string name, Color color, bool unlit = false, bool transparent = false, Color? emission = null, float glossiness = 0.05f)
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
            material.SetFloat("_Glossiness", glossiness);
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

    // ==========================================================
    // 펠트 바닥 재질 - Rect 전용
    //
    // 03_grass_tile.fbx 는 실측 결과(지름 2m, 높이 최대 0.94m 굴곡, 사실적인 잔디 텍스처)가
    // "펠트/퀼트" 방향과 맞지 않고, 6.5m 타일 하나를 덮으려면 다닥다닥 겹쳐 깔아야 해서
    // (RectTile 타일 하나에 2m 짜리를 여러 장 겹쳐야 함 - 삼각형 수가 감당 안 됨) 채택하지 않았다.
    // 대신 기존 Floor_* 타일(구조/Collider 그대로)의 재질만 절차적 텍스처로 바꾼다.
    // ==========================================================
    private static Material MatFelt(string name, Color baseColor, int variant)
    {
        var material = Mat(name, Color.white);

        string texPath = $"{MaterialFolder}/BD_{name}_Tex.asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            tex = GenerateFeltTexture(baseColor, variant);
            AssetDatabase.CreateAsset(tex, texPath);
        }

        string normalPath = $"{MaterialFolder}/BD_{name}_Normal.asset";
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal == null)
        {
            normal = GenerateFeltNormal(variant);
            AssetDatabase.CreateAsset(normal, normalPath);
        }

        material.mainTexture = tex;
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 0.85f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// 눈에 띄는 섬유 잡티 + 가장자리 점선 박음질을 가진 절차적 펠트 텍스처.
    /// variant(0~2)마다 노이즈 위상/축과 박음질이 노출되는 변만 달라져서, 같은 톤이라도
    /// 타일마다 조금씩 다르게 보이고 모든 타일 테두리가 똑같이 박음질되지 않는다.
    /// </summary>
    private static Texture2D GenerateFeltTexture(Color baseColor, int variant)
    {
        const int size = 160;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        float phaseX = variant * 37.1f;
        float phaseY = variant * 91.7f;
        bool swapAxes = variant == 1;

        // variant별로 두 변만 박음질 노출 - 네 변 다 두르면 모든 타일이 액자처럼 보인다.
        bool stitchTop = variant != 2;
        bool stitchBottom = variant == 0;
        bool stitchLeft = variant == 0;
        bool stitchRight = variant == 1;

        const float inset = 10f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size, ny = y / (float)size;
                if (swapAxes) { float t = nx; nx = ny; ny = t; }

                float fiber = Mathf.PerlinNoise(nx * 26f + phaseX, ny * 26f + phaseY) - 0.5f;
                float coarse = Mathf.PerlinNoise(nx * 5f + 50f + phaseX, ny * 5f + 50f + phaseY) - 0.5f;
                float weave = Mathf.Sin(nx * 90f) * Mathf.Sin(ny * 90f) * 0.02f;
                float shade = fiber * 0.11f + coarse * 0.08f + weave;
                Color c = baseColor + new Color(shade, shade, shade, 0f);

                // 가장자리 점선 박음질 - 타일 하나가 손으로 꿰맨 패치처럼 보이게 한다.
                float distTop = (size - 1) - y, distBottom = y, distLeft = x, distRight = (size - 1) - x;
                bool onStitchEdge =
                    (stitchTop && Mathf.Abs(distTop - inset) < 1.4f) ||
                    (stitchBottom && Mathf.Abs(distBottom - inset) < 1.4f) ||
                    (stitchLeft && Mathf.Abs(distLeft - inset) < 1.4f) ||
                    (stitchRight && Mathf.Abs(distRight - inset) < 1.4f);

                if (onStitchEdge && (((x + y) / 5) % 2 == 0))
                {
                    float stitchShade = baseColor.grayscale > 0.55f ? -0.14f : 0.16f;
                    c += new Color(stitchShade, stitchShade, stitchShade, 0f);
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>Perlin 높이장에서 뽑은 접선공간 노멀맵 - 가까이서 천 표면 굴곡이 읽히게 강도를 올렸다.</summary>
    private static Texture2D GenerateFeltNormal(int variant)
    {
        const int size = 160;
        const float noiseScale = 30f;
        float phaseX = variant * 37.1f;
        float phaseY = variant * 91.7f;
        bool swapAxes = variant == 1;

        var height = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size, ny = y / (float)size;
                if (swapAxes) { float t = nx; nx = ny; ny = t; }
                height[x, y] = Mathf.PerlinNoise(nx * noiseScale + phaseX, ny * noiseScale + phaseY);
            }
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        const float strength = 2.2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float hL = height[(x - 1 + size) % size, y];
                float hR = height[(x + 1) % size, y];
                float hD = height[x, (y - 1 + size) % size];
                float hU = height[x, (y + 1) % size];
                Vector3 n = new Vector3(-(hR - hL) * strength, -(hU - hD) * strength, 1f).normalized;
                tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Ribbon Path 전용 재질 - Ground Felt 와 같은 절차적 결 기법을 쓰되 진폭을 훨씬 약하게
    /// 낮춰서(Bump 0.22 vs Ground 0.85) "따뜻한 색지" 느낌만 살리고 사실적 흙/모래처럼
    /// 보이지 않게 한다. 텍스처/노멀은 파일이 있으면 재사용하고 없을 때만 새로 만든다.
    /// </summary>
    private static Material MatPathSurface(string name, Color baseColor)
    {
        var material = Mat(name, Color.white);

        string texPath = $"{MaterialFolder}/BD_{name}_Tex.asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            tex = GeneratePathSurfaceTexture(baseColor);
            AssetDatabase.CreateAsset(tex, texPath);
        }

        string normalPath = $"{MaterialFolder}/BD_{name}_Normal.asset";
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal == null)
        {
            normal = GeneratePathSurfaceNormal();
            AssetDatabase.CreateAsset(normal, normalPath);
        }

        material.mainTexture = tex;
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 0.32f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>아주 약한 밝기 얼룩만 얹는다 - baseColor 자체는 거의 그대로 유지된다.</summary>
    private static Texture2D GeneratePathSurfaceTexture(Color baseColor)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size, ny = y / (float)size;
                float fiber = Mathf.PerlinNoise(nx * 14f, ny * 14f) - 0.5f;
                float coarse = Mathf.PerlinNoise(nx * 4f + 50f, ny * 4f + 50f) - 0.5f;
                float shade = fiber * 0.055f + coarse * 0.035f; // 1인칭 거리에서도 옅게 보이는 정도
                tex.SetPixel(x, y, baseColor + new Color(shade, shade, shade, 0f));
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>Ground Felt 노멀보다 훨씬 얕은 높이장 - 종이 결 정도로만 표면을 흔든다.</summary>
    private static Texture2D GeneratePathSurfaceNormal()
    {
        const int size = 64;
        const float noiseScale = 14f;
        var height = new float[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size, ny = y / (float)size;
                height[x, y] = Mathf.PerlinNoise(nx * noiseScale, ny * noiseScale);
            }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        const float strength = 0.95f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float hL = height[(x - 1 + size) % size, y];
                float hR = height[(x + 1) % size, y];
                float hD = height[x, (y - 1 + size) % size];
                float hU = height[x, (y + 1) % size];
                Vector3 n = new Vector3(-(hR - hL) * strength, -(hU - hD) * strength, 1f).normalized;
                tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
            }
        }
        tex.Apply();
        return tex;
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
    // __MANUAL_LAYOUT 유틸리티
    // ==========================================================

    /// <summary>
    /// Hierarchy 에서 선택한 오브젝트를 Rect 씬의 __MANUAL_LAYOUT 아래로 옮긴다
    /// (월드 위치/회전/스케일은 그대로 유지 - Undo.SetTransformParent 사용).
    /// 이후 "Build Rect Blockout Scene" 을 다시 실행해도 이 오브젝트는 절대 지워지거나
    /// 옮겨지지 않는다. 이름이 PathStones / TestGrass38 / GardenArch_Craft 등
    /// Builder 가 아는 이름과 같으면, Builder 는 그 이름을 다시 만들지 않는다
    /// (IsManuallyOwned 판정).
    /// </summary>
    [MenuItem("Tools/Bright Dream/Move Selected To Manual Layout (Rect)")]
    public static void MoveSelectedToManualLayout()
    {
        if (SceneManager.GetActiveScene().path != RectScenePath)
        {
            Debug.LogWarning("[BrightDream] 이 명령은 Rect Scene 에서만 쓸 수 있다.");
            return;
        }

        var rootGo = GameObject.Find("BrightDream_Blockout");
        if (rootGo == null)
        {
            Debug.LogWarning("[BrightDream] BrightDream_Blockout 루트를 찾지 못했다.");
            return;
        }

        var selected = Selection.transforms;
        if (selected.Length == 0)
        {
            Debug.LogWarning("[BrightDream] Hierarchy 에서 옮길 오브젝트를 먼저 선택할 것.");
            return;
        }

        var manualLayout = EnsureManualLayout(rootGo.transform);
        int moved = 0;
        foreach (var t in selected)
        {
            if (t == manualLayout || t == rootGo.transform || t.IsChildOf(manualLayout)) continue;
            Undo.SetTransformParent(t, manualLayout, "Move to __MANUAL_LAYOUT");
            moved++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[BrightDream] {moved}개 오브젝트를 __MANUAL_LAYOUT 으로 옮겼다. " +
                  "Scene 을 저장해야 다음 실행/재빌드에서도 유지된다.");
    }

    // ==========================================================
    // 순수 참조(Reference) Scene 생성 - 진단 전용, 격리
    //
    // 목적: 지금 열려 있는(수동 편집이 섞인) Rect Scene 을 절대 건드리지 않고,
    // "코드만으로 처음부터 새로 만들면 어떤 결과가 나오는가" 를 별도 파일로 뽑아서
    // 나중에 비교(diff)할 수 있게 한다.
    //
    // 안전 설계 - BuildBlockout() 을 그대로 재사용하지 않고 별도 함수로 둔 이유:
    //   1. BuildBlockout 은 저장 경로가 RectScenePath 로 고정돼 있다(CurrentScenePath).
    //      실수로라도 보호 대상 파일에 저장되면 안 되므로, 이 함수는 저장 경로를
    //      ReferenceScenePath 로 별도 하드코딩한다.
    //   2. 지난 턴에 추가한 "기존 Rect Scene 재사용" 로직(TryGetExistingRectScene) 은
    //      절대 타지 않는다 - 여기서는 그 함수를 호출하지 않고, 매번
    //      NewSceneMode.Additive 로 완전히 새 빈 Scene 을 만든다(Single 모드는 현재
    //      열려 있는 모든 Scene 을 닫아버리므로 절대 쓰지 않는다).
    //   3. GameObject.Find 처럼 "이름으로 전체 로드된 Scene 을 뒤지는" API 는 이
    //      호출 체인 안에서 전혀 쓰지 않는다(사전 조사 완료 - BuildBlockout 계열
    //      함수 중 유일하게 위험했던 ClearGreenhouseOverlap 호출부는 이미
    //      generated.Find(상대경로) 로 바뀌어 있다). 참조 Scene 쪽 GreenhousePond 도
    //      root.Find(상대경로) 로만 찾는다.
    //   4. 끝나면 활성 Scene 을 원래대로 되돌리고, 참조 Scene 은 저장 후 즉시 닫아
    //      Hierarchy 에 남기지 않는다(파일은 디스크에 남는다).
    // ==========================================================
    private const string ReferenceSceneFolder = "Assets/__TEMP_REFERENCE";
    public const string ReferenceScenePath = ReferenceSceneFolder + "/SD_BrightDream_Blockout_Rect_PROCEDURAL_REFERENCE.unity";

    [MenuItem("Tools/Bright Dream/Build PURE Reference Scene (진단용, 격리)")]
    public static void BuildPureReferenceScene()
    {
        var originalActive = SceneManager.GetActiveScene();
        string originalActivePath = originalActive.path;
        string originalActiveName = originalActive.name;
        bool originalActiveWasDirty = originalActive.isDirty;
        int originalLoadedSceneCount = SceneManager.sceneCount;

        Debug.Log("[BrightDream] 참조 Scene 생성 시작 - 원래 활성 Scene='" + originalActivePath +
                  "' isDirty=" + originalActiveWasDirty + " loadedSceneCount=" + originalLoadedSceneCount);

        buildingRectangular = true;
        EnsureFolders();
        CreateMaterials();
        KeepClear.Clear();
        NoTreeZones.Clear();
        bushCraftKept = 0;
        BuildSpineTable();
        CurrentManualLayout = null; // 참조 빌드는 __MANUAL_LAYOUT 개념을 쓰지 않는다 - 순수 생성물만

        // Additive - 지금 열려 있는 Scene 은 전혀 닫거나 언로드하지 않는다.
        var refScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(refScene); // 새 GameObject 가 이 Scene 에 생성되도록 잠깐만 active 로

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

        BuildRectBackdrop(landscaping);
        BuildBoundaryLandscaping(landscaping);
        BuildPathsideLandscaping(landscaping);
        BuildTestGrass38Scatter(landscaping); // CurrentManualLayout=null 이라 IsManuallyOwned 는 항상 false
        BuildPathStones(mainPath);
        BuildCeilingDecorations(ceiling);

        var craftGreenhouse = root.Find("GreenhousePond/CraftGardenGreenhouse"); // 상대 검색만 - GameObject.Find 안 씀
        ClearGreenhouseOverlap(root, craftGreenhouse != null ? craftGreenhouse.gameObject : null);

        var path = BuildPathData(mainPath);
        BuildPlayer(root, path);

        if (!AssetDatabase.IsValidFolder(ReferenceSceneFolder))
            AssetDatabase.CreateFolder("Assets", "__TEMP_REFERENCE");

        EditorSceneManager.MarkSceneDirty(refScene);
        EditorSceneManager.SaveScene(refScene, ReferenceScenePath); // 보호 대상과 무관한 별도 경로
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LogMetrics(root);

        // 참조 Scene 은 파일로 저장 완료했으니 메모리에서 닫는다(Hierarchy 에 남기지 않는다).
        EditorSceneManager.CloseScene(refScene, true);

        // 활성 Scene 을 원래대로 복구.
        var restored = SceneManager.GetSceneByPath(originalActivePath);
        if (restored.IsValid()) SceneManager.SetActiveScene(restored);

        buildingRectangular = false;
        CurrentManualLayout = null;

        var afterActive = SceneManager.GetActiveScene();
        Debug.Log("[BrightDream] 참조 Scene 저장 완료 -> " + ReferenceScenePath +
                  " | 복구 후 활성 Scene='" + afterActive.path + "' isDirty=" + afterActive.isDirty +
                  " loadedSceneCount=" + SceneManager.sceneCount +
                  " (원래와 동일해야 정상: path 동일=" + (afterActive.path == originalActivePath) +
                  ", loadedCount 동일=" + (SceneManager.sceneCount == originalLoadedSceneCount) + ")");
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
