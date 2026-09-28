using UnityEngine;

/// <summary>
/// 대사 · 단서 조사처럼 게임을 멈추는 곳에서 쓸 timeScale.
/// Unity Recorder 는 게임 시간(Time.time)으로 영상 프레임을 찍어서, 녹화 중에 timeScale 을 0 으로 멈추면
/// 그 구간(대사창 · 나레이션)이 영상에서 통째로 빠진다. 그래서 자동 녹화 중에는 멈추지 않는다
/// (플레이어 조작은 각 스크립트가 따로 꺼 두므로 화면은 그대로 서 있다).
/// </summary>
public static class RecordingClock
{
    /// <summary>AutoPlayRecorder 가 녹화하는 동안 켜 두는 SessionState 키.</summary>
    public const string RecordingKey = "ReDream.AutoPlayRecorder.Recording";

    public static float PausedTimeScale
    {
        get
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(RecordingKey, false)) return 1f;
#endif
            return 0f;
        }
    }
}
