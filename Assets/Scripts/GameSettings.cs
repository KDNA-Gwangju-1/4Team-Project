using UnityEngine;

/// <summary>
/// 게임 설정(마우스 감도 / 마스터 볼륨)을 보관하고 PlayerPrefs에 저장하는 곳.
///
/// PlayerPrefs 키 이름과 값의 범위를 "이 파일 한 곳에서만" 관리한다.
/// 다른 스크립트에서는 GameSettings.MouseSensitivity 처럼 읽기만 하면 된다.
///
/// 예) 플레이어 카메라 회전에 감도를 적용할 때:
///     float mouseX = Input.GetAxis("Mouse X") * GameSettings.MouseSensitivity;
/// </summary>
public static class GameSettings
{
    // ------------------------------------------------------------
    // PlayerPrefs 키 이름 (문자열을 직접 쓰지 말고 항상 이 상수를 쓸 것)
    // ------------------------------------------------------------
    public const string MouseSensitivityKey = "Settings.MouseSensitivity";
    public const string MasterVolumeKey     = "Settings.MasterVolume";

    // ------------------------------------------------------------
    // 마우스 감도 : 0.1 ~ 10.0 (기본 1.0)
    // ------------------------------------------------------------
    public const float MouseSensitivityMin     = 0.1f;
    public const float MouseSensitivityMax     = 10.0f;
    public const float MouseSensitivityDefault = 1.0f;

    // ------------------------------------------------------------
    // 마스터 볼륨 : 0.0(무음) ~ 1.0(최대), UI에는 0~100으로 표시 (기본 0.8 = 80)
    // ------------------------------------------------------------
    public const float MasterVolumeMin     = 0.0f;
    public const float MasterVolumeMax     = 1.0f;
    public const float MasterVolumeDefault = 0.8f;

    /// <summary>마우스 감도. 값을 넣으면 곧바로 PlayerPrefs에 기록된다.</summary>
    public static float MouseSensitivity
    {
        get
        {
            float value = PlayerPrefs.GetFloat(MouseSensitivityKey, MouseSensitivityDefault);
            return Mathf.Clamp(value, MouseSensitivityMin, MouseSensitivityMax);
        }
        set
        {
            float clamped = Mathf.Clamp(value, MouseSensitivityMin, MouseSensitivityMax);
            PlayerPrefs.SetFloat(MouseSensitivityKey, clamped);
        }
    }

    /// <summary>마스터 볼륨(0~1). 값을 넣으면 곧바로 PlayerPrefs에 기록된다.</summary>
    public static float MasterVolume
    {
        get
        {
            float value = PlayerPrefs.GetFloat(MasterVolumeKey, MasterVolumeDefault);
            return Mathf.Clamp(value, MasterVolumeMin, MasterVolumeMax);
        }
        set
        {
            float clamped = Mathf.Clamp(value, MasterVolumeMin, MasterVolumeMax);
            PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
        }
    }

    /// <summary>
    /// 지금까지 바꾼 값을 디스크에 확정 저장한다.
    /// (슬라이더를 드래그하는 매 프레임마다 저장하면 느려지므로,
    ///  BACK 버튼을 누를 때나 게임을 끌 때처럼 "한 번만" 호출한다.)
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
