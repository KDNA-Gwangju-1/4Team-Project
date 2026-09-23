using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 전체를 담당하는 하나의 관리 스크립트.
///
/// - START  : gameSceneName 에 적힌 Scene 으로 이동
/// - OPTION : 메인 메뉴를 끄고 옵션 패널을 켬
/// - EXIT   : 게임 종료 (에디터에서는 Debug.Log 출력)
///
/// 버튼의 OnClick 과 슬라이더의 OnValueChanged 는
/// 아래 public 메서드들에 이미 연결되어 있다.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private const string DefaultMenuMusicResourcePath = "Audio/MainMenu/WhispersOfTheNight";

    // ============================================================
    // 이동할 게임 Scene 이름
    // 여기 기본값을 바꾸거나, Inspector 에서 직접 바꿔도 된다.
    // (Scene 은 File > Build Profiles 의 Scene List 에 들어 있어야 한다)
    // ============================================================
    [Header("Scene 설정")]
    [Tooltip("START 를 눌렀을 때 이동할 Scene 이름")]
    [SerializeField] private string gameSceneName = "HospitalRoom";
    private bool startingGame;

    [Header("패널 (둘 중 하나만 켜진다)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionPanel;

    [Header("옵션 - Mouse Sensitivity")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Text   mouseSensitivityValueText;

    [Header("옵션 - Master Volume")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text   masterVolumeValueText;

    [Header("AudioMixer (선택 사항)")]
    [Tooltip("AudioMixer 를 넣으면 Mixer 로 볼륨을 조절하고, 비워두면 AudioListener.volume 으로 조절한다.")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("AudioMixer 에서 Expose 한 볼륨 파라미터 이름")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("메인 메뉴 BGM")]
    [Tooltip("비워 두면 Resources/Audio/MainMenu/WhispersOfTheNight 를 자동으로 불러온다.")]
    [SerializeField] private AudioClip menuMusicClip;
    [Tooltip("직접 지정하지 않으면 이 오브젝트에 AudioSource 를 자동으로 추가한다.")]
    [SerializeField] private AudioSource menuMusicSource;
    [Range(0f, 1f)]
    [SerializeField] private float menuMusicVolume = 0.55f;

    // ============================================================
    // 시작할 때: 저장된 설정을 불러와 슬라이더와 실제 값에 반영한다.
    // ============================================================
    private void Start()
    {
        HangulFont.ApplyAll(mainMenuPanel);
        HangulFont.ApplyAll(optionPanel);
        // 슬라이더의 최소/최대값을 GameSettings 의 범위로 맞춘다.
        mouseSensitivitySlider.minValue = GameSettings.MouseSensitivityMin;
        mouseSensitivitySlider.maxValue = GameSettings.MouseSensitivityMax;

        masterVolumeSlider.minValue = GameSettings.MasterVolumeMin;
        masterVolumeSlider.maxValue = GameSettings.MasterVolumeMax;

        // 저장돼 있던 값을 슬라이더에 넣는다.
        // (값이 바뀌면 OnValueChanged 가 불려서 아래 메서드들이 자동 실행된다)
        mouseSensitivitySlider.value = GameSettings.MouseSensitivity;
        masterVolumeSlider.value     = GameSettings.MasterVolume;

        // 슬라이더 값이 우연히 그대로여서 OnValueChanged 가 안 불릴 수도 있으므로
        // 한 번 직접 호출해 글자와 실제 볼륨을 확실히 맞춰 준다.
        OnMouseSensitivityChanged(mouseSensitivitySlider.value);
        OnMasterVolumeChanged(masterVolumeSlider.value);

        InitializeMenuMusic();

        // 처음에는 메인 메뉴만 보이게 한다.
        ShowMainMenu();
    }

    // ============================================================
    // 버튼
    // ============================================================

    /// <summary>START 버튼</summary>
    public void OnStartButton()
    {
        if (startingGame) return;
        // 저장 안 된 설정이 남아 있을 수 있으니 여기서 확정 저장한다.
        GameSettings.Save();

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenuManager] gameSceneName 이 비어 있습니다. Inspector 에서 Scene 이름을 적어 주세요.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"[MainMenuManager] '{gameSceneName}' Scene 을 찾을 수 없습니다. " +
                           "File > Build Profiles 의 Scene List 에 추가했는지 확인해 주세요.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(LoadingScreen.LoadingSceneName))
        {
            Debug.LogError("[MainMenuManager] Loading scene is missing from Build Settings.");
            return;
        }

        StopMenuMusic();
        startingGame = true;
        mainMenuPanel.SetActive(false);
        optionPanel.SetActive(false);
        var cinematic = gameObject.AddComponent<OpeningCinematicPlayer>();
        cinematic.Play(
            () => LoadingScreen.Go(gameSceneName),
            () => { startingGame = false; PlayMenuMusic(); ShowMainMenu(); },
            audioMixer != null ? GameSettings.MasterVolume : 1f);
    }

    /// <summary>OPTION 버튼 : 메인 메뉴를 숨기고 옵션 패널을 연다.</summary>
    public void OnOptionButton()
    {
        mainMenuPanel.SetActive(false);
        optionPanel.SetActive(true);
    }

    /// <summary>BACK 버튼 : 옵션을 닫고 메인 메뉴로 돌아온다.</summary>
    public void OnBackButton()
    {
        GameSettings.Save();   // 옵션에서 바꾼 값을 확정 저장
        ShowMainMenu();
    }

    /// <summary>EXIT 버튼 : 게임 종료</summary>
    public void OnExitButton()
    {
        GameSettings.Save();
        Debug.Log("Game Exit");

#if UNITY_EDITOR
        // 에디터에서는 Application.Quit() 이 동작하지 않으므로 플레이 모드를 멈춘다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    // 슬라이더
    // ============================================================

    /// <summary>Mouse Sensitivity 슬라이더가 움직일 때마다 호출된다.</summary>
    public void OnMouseSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;

        // 1.0 처럼 소수점 한 자리로 표시
        mouseSensitivityValueText.text = value.ToString("0.0");
    }

    /// <summary>Master Volume 슬라이더가 움직일 때마다 호출된다.</summary>
    public void OnMasterVolumeChanged(float value)
    {
        GameSettings.MasterVolume = value;

        // 0~1 값을 0~100 으로 바꿔서 표시
        masterVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString();

        ApplyVolume(value);
    }

    // ============================================================
    // 내부 도우미
    // ============================================================

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionPanel.SetActive(false);
    }

    private void InitializeMenuMusic()
    {
        if (menuMusicClip == null)
            menuMusicClip = Resources.Load<AudioClip>(DefaultMenuMusicResourcePath);

        if (menuMusicClip == null)
        {
            Debug.LogWarning("[MainMenuManager] 메인 메뉴 BGM을 찾지 못했습니다: Resources/" +
                             DefaultMenuMusicResourcePath);
            return;
        }

        if (menuMusicSource == null)
            menuMusicSource = gameObject.AddComponent<AudioSource>();

        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.spatialBlend = 0f;
        menuMusicSource.clip = menuMusicClip;
        UpdateMenuMusicVolume(GameSettings.MasterVolume);
        PlayMenuMusic();
    }

    private void PlayMenuMusic()
    {
        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
            menuMusicSource.Play();
    }

    private void StopMenuMusic()
    {
        if (menuMusicSource != null && menuMusicSource.isPlaying)
            menuMusicSource.Stop();
    }

    private void UpdateMenuMusicVolume(float masterVolume)
    {
        if (menuMusicSource == null) return;

        // Mixer가 없으면 AudioListener가 마스터 볼륨을 담당한다.
        // Mixer가 있으면 메뉴 BGM이 중복 감쇠되지 않도록 소스에서만 보정한다.
        menuMusicSource.volume = menuMusicVolume * (audioMixer != null ? masterVolume : 1f);
    }

    /// <summary>0~1 볼륨 값을 실제 소리에 적용한다.</summary>
    private void ApplyVolume(float volume01)
    {
        if (audioMixer != null && !string.IsNullOrEmpty(masterVolumeParameter))
        {
            // AudioMixer 의 볼륨 단위는 dB 라서 0~1 값을 데시벨로 바꿔 줘야 한다.
            // 0 일 때 로그 계산이 불가능하므로 아주 작은 값(0.0001)으로 막아 준다.
            float decibel = Mathf.Log10(Mathf.Max(volume01, 0.0001f)) * 20f;
            audioMixer.SetFloat(masterVolumeParameter, decibel);
        }
        else
        {
            // AudioMixer 가 없을 때는 전체 볼륨을 직접 조절한다.
            AudioListener.volume = volume01;
        }

        UpdateMenuMusicVolume(volume01);
    }

    /// <summary>게임이 꺼질 때도 한 번 저장해 둔다.</summary>
    private void OnApplicationQuit()
    {
        GameSettings.Save();
    }
}
