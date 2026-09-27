using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// Records the Game view to an MP4 (with sound) every time Play is pressed, and
// saves it when Play stops. Output: <project>/Recordings/<date>_<scene>.mp4
//
//   Tools/녹화/플레이 시 자동 녹화   - on/off (per computer, teammates are unaffected)
//   Tools/녹화/녹화 폴더 열기
//
// Only compiles when the Unity Recorder package is installed (see the asmdef).
[InitializeOnLoad]
public static class AutoPlayRecorder
{
    private const string EnabledKey = "ReDream.AutoPlayRecorder.Enabled";
    private const string MenuToggle = "Tools/녹화/플레이 시 자동 녹화";
    private const int Width = 1920, Height = 1080;
    private const float FrameRate = 60f;

    private static RecorderController controller;
    private static string currentFile;

    static AutoPlayRecorder()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));

    [MenuItem(MenuToggle, false, 0)]
    private static void Toggle() => Enabled = !Enabled;

    [MenuItem(MenuToggle, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuToggle, Enabled);
        return true;
    }

    [MenuItem("Tools/녹화/녹화 폴더 열기", false, 1)]
    private static void OpenFolder()
    {
        Directory.CreateDirectory(Folder);
        EditorUtility.RevealInFinder(Folder + Path.DirectorySeparatorChar);
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && Enabled) StartRecording();
        else if (change == PlayModeStateChange.ExitingPlayMode) StopRecording();
    }

    private static void StartRecording()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            currentFile = Path.Combine(Folder, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + scene);

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "Auto Play Recording";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            movie.CaptureAudio = true;
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.OutputFile = currentFile;

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            // Variable: the game keeps running in real time while you play,
            // instead of slowing down to hand the encoder every frame.
            settings.FrameRatePlayback = FrameRatePlayback.Variable;
            settings.FrameRate = FrameRate;
            settings.CapFrameRate = false;

            controller = new RecorderController(settings);
            controller.PrepareRecording();
            if (controller.StartRecording())
                Debug.Log("[AutoPlayRecorder] 녹화 시작 → " + currentFile + ".mp4");
            else
            {
                Debug.LogWarning("[AutoPlayRecorder] 녹화를 시작하지 못했어요.");
                controller = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AutoPlayRecorder] 녹화 시작 실패: " + e.Message);
            controller = null;
        }
    }

    private static void StopRecording()
    {
        if (controller == null) return;
        try
        {
            if (controller.IsRecording()) controller.StopRecording();
            Debug.Log("[AutoPlayRecorder] 녹화 저장 → " + currentFile + ".mp4");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AutoPlayRecorder] 녹화 저장 실패: " + e.Message);
        }
        controller = null;
    }
}
