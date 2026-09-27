#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Editor-only recording assistant. All actors still run their normal gameplay/physics.
// Attached only during Play Mode; never saved in a scene or included in a player build.
public sealed class CinematicLiveCapture : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrepareStagedBoss()
    {
        string staging = SessionState.GetString("Trailer.BossStaging", "");
        SessionState.EraseString("Trailer.BossStaging");
        if (staging == "") return;
        if (staging == "stage2")
        {
            Stage2IntroCutscene.SkipIntroOnce = true;
            PlayerMovement2D.LanternObtained = true;
            return;
        }
        Stage3BossIntroCutscene.SkipIntroOnce = true;
        BossPhaseController2D.ResumeAtPhase2 = staging == "phase2";
        PlayerMovement2D.LanternObtained = true;
    }
    public string shot;
    public int frames;
    public string mode;
    public int frame;
    private Keyboard keyboard;
    private Mouse mouse;
    private int previousRate;
    private bool previousBackground;

    public static void Begin(string name, float seconds, string action)
    {
        if (!Application.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
        var go = new GameObject("CinematicLiveCapture_TEMP");
        var r = go.AddComponent<CinematicLiveCapture>();
        r.shot = name;
        r.frames = Mathf.RoundToInt(seconds * 24);
        r.mode = action;
        r.previousRate = Time.captureFramerate;
        r.previousBackground = Application.runInBackground;
        Time.captureFramerate = 24;
        Application.runInBackground = true;
        if (action.StartsWith("2d"))
        {
            r.keyboard = InputSystem.AddDevice<Keyboard>();
            r.mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.onBeforeUpdate += r.DriveInput;
        }
        var view = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.Show(); view.Focus();
        r.StartCoroutine(r.Record());
    }

    private void DriveInput()
    {
        if (keyboard == null) return;
        int phase = frame % 96;
        var keys = new System.Collections.Generic.List<Key>();
        // Short advances, jumps, and recovery beats keep the player on the visible platforms.
        if (mode == "2d-run" && phase < 58) keys.Add(Key.D);
        if (mode == "2d-fight" && phase < 22) keys.Add(Key.D);
        if (mode == "2d-fight" && phase >= 48 && phase < 70) keys.Add(Key.A);
        if (phase == 16 || phase == 65) keys.Add(Key.Space);
        if (phase == 34) keys.Add(Key.LeftShift);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
        var state = new MouseState { position = new Vector2(Screen.width * .75f, Screen.height * .47f) };
        state = state.WithButton(MouseButton.Right, true).WithButton(MouseButton.Left, frame % 30 == 0);
        InputSystem.QueueStateEvent(mouse, state);
    }

    private IEnumerator Record()
    {
        string dir = "output/cinematic-v2-frames/" + shot;
        Directory.CreateDirectory(dir);
        var end = new WaitForEndOfFrame();
        for (frame = 0; frame < frames; frame++)
        {
            yield return end;
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, frame.ToString("D4") + ".jpg"), texture.EncodeToJPG(92));
            Destroy(texture);
        }
        File.WriteAllText(Path.Combine(dir, "capture.json"), JsonUtility.ToJson(new CaptureInfo { scene = gameObject.scene.path, frames = frames, fps = 24, mode = mode }, true));
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        InputSystem.onBeforeUpdate -= DriveInput;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        Time.captureFramerate = previousRate;
        Application.runInBackground = previousBackground;
    }

    [System.Serializable] private class CaptureInfo { public string scene; public int frames; public int fps; public string mode; }
}

#endif
