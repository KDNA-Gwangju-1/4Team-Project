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
    public const string ScenePath = "Assets/Scenes/SD_BrightDream_Blockout.unity";
    private const string RootFolder = "Assets/BrightDream";
    private const string MaterialFolder = RootFolder + "/Materials";

    public const float CeilingY = 7.5f;
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
    private static Material MatGrass, MatGrassAlt, MatPath, MatPathAlt, MatSand, MatStone;
    private static Material MatSky, MatCloud, MatSun, MatStar;
    private static Material MatTrunk, MatLeaf, MatLeafAlt, MatLeafDeep;
    private static Material MatFence, MatWood, MatGlass, MatWater;
    private static Material MatPink, MatYellow, MatPurple, MatWhiteFlower;
    private static Material MatEnemy, MatUnicorn, MatMane, MatGold;
    private static Material MatMarkerClue, MatMarkerWeapon, MatMarkerEnemy, MatMarkerDream;

    // ==========================================================
    // 진입점
    // ==========================================================
    [MenuItem("Tools/Bright Dream/Build Blockout Scene")]
    public static void BuildBlockout()
    {
        EnsureFolders();
        CreateMaterials();
        KeepClear.Clear();
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

        BuildBoundaryLandscaping(landscaping);
        BuildPathsideLandscaping(landscaping);
        BuildCeilingDecorations(ceiling);

        var path = BuildPathData(mainPath);
        BuildPlayer(root, path);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LogMetrics(root);
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
        int n = SpineControls.Length;
        var kx = new Keyframe[n];
        var kz = new Keyframe[n];
        for (int i = 0; i < n; i++)
        {
            kx[i] = new Keyframe(i, SpineControls[i].x);
            kz[i] = new Keyframe(i, SpineControls[i].y);
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
    public static float CorridorHalfWidth(float s)
    {
        float w = WalkLength;
        var keys = new List<Vector2> { new Vector2(0f, Areas[0].HalfWidth) };

        foreach (var area in Areas)
        {
            float a = area.StartF * w, b = area.EndF * w;
            float hold = (b - a) * 0.28f;
            keys.Add(new Vector2(a + hold, area.HalfWidth));
            keys.Add(new Vector2(b - hold, area.HalfWidth));
        }

        keys.Add(new Vector2(w, Areas[Areas.Length - 1].HalfWidth));
        keys.Add(new Vector2(spineLength, 8f));   // 광장 뒤쪽은 살짝 좁혀 닫는다
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
    // ==========================================================
    private static void BuildMainPath(Transform parent)
    {
        const float step = 0.7f;
        int index = 0;

        for (float s = 0.6f; s <= spineLength - 1f; s += step, index++)
        {
            float half = RibbonHalfWidth(s);
            Prim(PrimitiveType.Cube, $"PathTile_{index:D3}", parent,
                 SpinePoint(s) + Vector3.up * 0.025f,
                 new Vector3(half * 2f, 0.05f, step * 1.35f),
                 index % 2 == 0 ? MatPath : MatPathAlt,
                 rot: Facing(s));
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
        MatPath = Mat("Path", new Color(0.80f, 0.73f, 0.58f));
        MatPathAlt = Mat("PathAlt", new Color(0.73f, 0.66f, 0.52f));
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
        RenderSettings.ambientGroundColor = new Color(0.40f, 0.46f, 0.38f);
        RenderSettings.ambientIntensity = 0.55f;
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
        report.AppendLine("--- 구간별 ---");
        foreach (var area in Areas)
        {
            float a = area.StartF * WalkLength, b = area.EndF * WalkLength;
            report.AppendLine($"{area.Name,-16} s {a,5:F1} ~ {b,5:F1}  길이 {b - a,5:F1}m  폭 {area.HalfWidth * 2f,4:F0}m");
        }
        report.AppendLine($"총 GameObject 수 : {root.GetComponentsInChildren<Transform>(true).Length}");
        Debug.Log(report.ToString());
    }
}
