using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 블록아웃을 눈으로 확인하기 위한 스크린샷 도구.
///
/// Play Mode 로 들어가지 않고 임시 카메라를 만들어 정해진 지점에서 PNG 로 찍는다.
/// 1인칭 컷은 실제 플레이어와 같은 눈높이(1.6m) / 화각(70) 을 쓰므로
/// "직접 걸었을 때 이렇게 보인다" 를 그대로 보여 준다.
///
/// 촬영 지점은 Z 좌표가 아니라 "START 에서부터의 거리(s)" 로 잡기 때문에
/// 중심선 모양을 바꿔도 같은 구간이 찍힌다.
///
/// 결과물은 Temp/ 아래에 떨어지므로 (.gitignore 대상) 저장소를 더럽히지 않는다.
/// </summary>
public static class BrightDreamBlockoutCapture
{
    private struct Shot
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Orthographic;
        public float OrthoSize;
        public int Width;
        public int Height;
        public bool HideCeiling;
    }

    public static string DefaultOutputDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "BrightDreamCaptures"));

    [MenuItem("Tools/Bright Dream/Capture Blockout Shots")]
    public static void CaptureDefault() => Capture(DefaultOutputDir);

    public static void Capture(string outputDir)
    {
        if (SceneManager.GetActiveScene().path != BrightDreamBlockoutBuilder.ScenePath)
        {
            EditorSceneManager.OpenScene(BrightDreamBlockoutBuilder.ScenePath, OpenSceneMode.Single);
        }

        BrightDreamBlockoutBuilder.BuildSpineTable();
        Directory.CreateDirectory(outputDir);

        var ceiling = GameObject.Find("BrightDream_Blockout/Ceiling");
        foreach (var shot in BuildShotList())
        {
            if (ceiling != null) ceiling.SetActive(!shot.HideCeiling);
            RenderShot(shot, Path.Combine(outputDir, shot.Name + ".png"));
        }
        if (ceiling != null) ceiling.SetActive(true);

        Debug.Log($"[BrightDream] 스크린샷 저장 완료 -> {outputDir}");
    }

    private static Shot[] BuildShotList()
    {
        var b = typeof(BrightDreamBlockoutBuilder);
        float walk = BrightDreamBlockoutBuilder.WalkLength;

        // 꺾인 맵 전체를 위에서 담기 위해 바운딩을 재서 카메라를 맞춘다
        var bounds = new Bounds(BrightDreamBlockoutBuilder.SpinePoint(0f), Vector3.zero);
        for (float s = 0f; s <= BrightDreamBlockoutBuilder.SpineTotalLength; s += 1f)
        {
            float half = BrightDreamBlockoutBuilder.CorridorHalfWidth(s) + 2f;
            bounds.Encapsulate(BrightDreamBlockoutBuilder.At(s, half));
            bounds.Encapsulate(BrightDreamBlockoutBuilder.At(s, -half));
        }
        float orthoSize = Mathf.Max(bounds.size.z, bounds.size.x * 0.62f) * 0.5f + 2f;

        return new[]
        {
            new Shot
            {
                Name = "01_TopView",
                Position = new Vector3(bounds.center.x, 80f, bounds.center.z),
                Rotation = Quaternion.Euler(90f, 0f, 0f),
                Orthographic = true,
                OrthoSize = orthoSize,
                Width = 1100,
                Height = 900,
                HideCeiling = true,
            },
            FirstPerson("02_Start_FPV", 1.8f),
            FirstPerson("03_ClueWalk_FPV", Area("ClueWalk", 0.35f)),
            FirstPerson("04_GreenhouseEntry_FPV", Area("GreenhousePond", 0.12f)),
            FirstPerson("05_GreenhousePond_FPV", Area("GreenhousePond", 0.33f), yaw: 34f),
            FirstPerson("06_WeaponEvent_FPV", Area("WeaponLink", 0.28f), yaw: -12f),
            FirstPerson("07_EnemyTutorial_FPV", Area("TutorialNook", 0.18f)),
            FirstPerson("08_CombatArena_FPV", Area("CombatArena", 0.18f)),
            FirstPerson("09_UnicornApproach_FPV", Area("UnicornApproach", 0.3f)),
            FirstPerson("10_UnicornFirstSight_FPV", Area("UnicornApproach", 0.92f)),
            FirstPerson("11_UnicornPlaza_FPV", walk - 7f),
            FirstPerson("12_CeilingLookUp_FPV", Area("GreenhousePond", 0.5f), pitch: -52f),
        };
    }

    private static float Area(string name, float t) => BrightDreamBlockoutBuilder.InArea(name, t);

    private static Shot FirstPerson(string name, float s, float yaw = 0f, float pitch = 0f)
    {
        return new Shot
        {
            Name = name,
            Position = BrightDreamBlockoutBuilder.SpinePoint(s) + Vector3.up * BrightDreamBlockoutBuilder.EyeHeight,
            Rotation = BrightDreamBlockoutBuilder.Facing(s) * Quaternion.Euler(pitch, yaw, 0f),
            Orthographic = false,
            Width = 1440,
            Height = 810,
        };
    }

    private static void RenderShot(Shot shot, string filePath)
    {
        var cameraGO = new GameObject("~BrightDreamCaptureCamera");
        cameraGO.hideFlags = HideFlags.HideAndDontSave;
        var camera = cameraGO.AddComponent<Camera>();

        camera.transform.SetPositionAndRotation(shot.Position, shot.Rotation);
        camera.orthographic = shot.Orthographic;
        camera.orthographicSize = shot.OrthoSize;
        camera.fieldOfView = BrightDreamBlockoutBuilder.FieldOfView;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 300f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.64f, 0.84f, 0.96f);

        var renderTexture = new RenderTexture(shot.Width, shot.Height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 8,
        };
        var texture = new Texture2D(shot.Width, shot.Height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, shot.Width, shot.Height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(filePath, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(cameraGO);
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }
}
