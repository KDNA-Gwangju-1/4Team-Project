using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC 일시정지 메뉴. 게임 플레이 씬 어디서든 ESC 로 연다 (메인 메뉴·로딩 씬 제외).
///
///   계속하기 / 설정(마우스 감도·전체 음량) / 메인 메뉴 / 게임 종료
///
/// - 열려 있는 동안 Time.timeScale = 0, AudioListener.pause = true, 커서를 풀어 보여 준다.
///   닫으면 열기 전 값으로 되돌린다 (단서 조사처럼 원래 0 이던 시간도 그대로 돌아간다).
/// - 다른 스크립트는 입력을 읽기 전에 PauseMenu.IsPaused 를 확인해 멈춘다.
/// - 쪽지·조작 안내창이 열려 있으면 ESC 는 그 창을 닫는 데 쓰이고 메뉴는 안 열린다.
///   게임 오버·사망 화면, 화면 전환(페이드) 중에도 안 열린다.
///
/// 씬에 따로 배치할 필요 없이, 게임이 시작될 때 스스로 하나 만들어 씬을 넘나들며 산다.
/// 화면은 코드로 짓는다 (디자인 단계에서 이 파일의 Build* 만 손보면 된다).
/// </summary>
public sealed class PauseMenu : MonoBehaviour
{
    private enum Page { Closed, Main, Settings, ConfirmMainMenu, ConfirmQuit }

    private static PauseMenu instance;

    /// <summary>일시정지 메뉴가 열려 있으면 true. 입력을 읽는 스크립트는 이 값을 보고 쉰다.</summary>
    public static bool IsPaused => instance != null && instance.page != Page.Closed;

    /// <summary>ESC 를 이번 프레임에 다른 창(쪽지 등)이 먼저 썼다고 알린다. 메뉴가 같이 열리지 않게 한다.</summary>
    public static void ConsumeEscapeThisFrame() { escapeConsumedFrame = Time.frameCount; }
    private static int escapeConsumedFrame = -1;

    private Page page = Page.Closed;
    private float savedTimeScale = 1f;
    private CursorLockMode savedLockState;
    private bool savedCursorVisible;

    private GameObject root;
    private GameObject mainPanel, settingsPanel, confirmPanel;
    private Text confirmText;
    private Button firstMainButton, firstSettingsButton, confirmYesButton;
    private Slider sensitivitySlider, volumeSlider;
    private Text sensitivityValue, volumeValue;
    private bool bright;

    // ============================================================
    // 생성
    // ============================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 메뉴는 항상 닫힌 상태로 시작한다 (메인 메뉴로 나간 경우 포함).
        if (page != Page.Closed) CloseImmediate(restoreState: false);
        // 챕터마다 색이 다르다 - 밝은 꿈은 크림색, 나머지는 어두운 카드.
        bool wantBright = scene.name.Contains("BrightDream");
        if (root != null && wantBright != bright) { Destroy(root); root = null; }
    }

    private static bool IsMenuScene()
    {
        string name = SceneManager.GetActiveScene().name;
        return name == "MainMenu" || name == LoadingScreen.LoadingSceneName;
    }

    // ============================================================
    // 입력
    // ============================================================

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        switch (page)
        {
            case Page.Closed:
                if (CanOpen()) Open();
                break;
            case Page.Main:
                Resume();
                break;
            default:
                ShowPage(Page.Main);   // 설정·확인 창에서는 한 단계 뒤로
                break;
        }
    }

    private bool CanOpen()
    {
        if (IsMenuScene()) return false;
        if (escapeConsumedFrame == Time.frameCount) return false;
        if (SceneFader.IsFading) return false;
        if (ControlGuideUI.Blocking) return false;                       // 조작 안내창은 ESC 로 닫힌다
        if (NoteReader2D.AnyOpen) return false;                          // 쪽지는 ESC 로 닫힌다
        if (BrightDream.Combat.GameOverController.IsGameOver) return false;
        if (DeathRetryUI2D.AnyShown) return false;
        var overlay = FindFirstObjectByType<LoadingOverlay>();
        if (overlay != null && overlay.IsShowing) return false;          // 병원 문 이동 중
        return true;
    }

    // ============================================================
    // 열기 / 닫기
    // ============================================================

    private void Open()
    {
        if (root == null) Build();

        savedTimeScale = Time.timeScale;
        savedLockState = Cursor.lockState;
        savedCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        root.SetActive(true);
        ShowPage(Page.Main);
    }

    /// <summary>계속하기.</summary>
    public void Resume()
    {
        if (page == Page.Closed) return;
        GameSettings.Save();
        CloseImmediate(restoreState: true);
    }

    private void CloseImmediate(bool restoreState)
    {
        page = Page.Closed;
        if (root != null) root.SetActive(false);
        AudioListener.pause = false;
        if (restoreState)
        {
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedLockState;
            Cursor.visible = savedCursorVisible;
        }
        else
        {
            Time.timeScale = 1f;
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void ShowPage(Page next)
    {
        page = next;
        mainPanel.SetActive(next == Page.Main);
        settingsPanel.SetActive(next == Page.Settings);
        confirmPanel.SetActive(next == Page.ConfirmMainMenu || next == Page.ConfirmQuit);

        GameObject select = null;
        if (next == Page.Main) select = firstMainButton.gameObject;
        else if (next == Page.Settings) { RefreshSettings(); select = firstSettingsButton.gameObject; }
        else
        {
            confirmText.text = next == Page.ConfirmMainMenu
                ? "메인 메뉴로 돌아갈까요?\n지금까지의 진행 상황은 사라져요."
                : "게임을 종료할까요?";
            select = confirmYesButton.gameObject;
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(select);
    }

    // ============================================================
    // 버튼 동작
    // ============================================================

    private void OnMainMenuConfirmed()
    {
        GameSettings.Save();
        CloseImmediate(restoreState: false);
        // 다음 판을 처음부터 시작하도록 챕터 간 static 상태를 비운다.
        PlayerMovement2D.ResetChapterState();
        TimeAttackTimer2D.ResetTimer();
        BrightDream.Combat.CheckpointRespawn.ResetCheckpoint();
        LoadingScreen.Go("MainMenu");
    }

    private void OnQuitConfirmed()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshSettings()
    {
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.0");
        volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f).ToString();
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;
        sensitivityValue.text = value.ToString("0.0");
    }

    private void OnVolumeChanged(float value)
    {
        GameSettings.MasterVolume = value;
        volumeValue.text = Mathf.RoundToInt(value * 100f).ToString();
        // 오디오 믹서가 없어서 마스터 볼륨은 AudioListener 가 맡는다 (메인 메뉴와 같은 방식).
        AudioListener.volume = value;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        // 2D 스테이지처럼 EventSystem 이 없는 씬에서는 버튼이 안 눌리므로 그 씬에 하나 만든다.
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // ============================================================
    // 화면 (시스템 단계의 기본 모양 - 디자인은 여기만 손본다)
    // ============================================================

    private void Build()
    {
        bright = SceneManager.GetActiveScene().name.Contains("BrightDream");

        root = new GameObject("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;   // HUD·대사창 위, SceneFader(32000) 아래
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = NewImage("Dim", root.transform);
        Stretch(dim.rectTransform);
        dim.color = new Color(0.03f, 0.02f, 0.06f, 0.62f);
        dim.raycastTarget = true;   // 뒤의 게임 UI 가 눌리지 않게 막는다

        mainPanel = BuildPanel("MainPanel", "일시정지", 440f);
        firstMainButton = AddButton(mainPanel.transform, "계속하기", 0, Resume);
        AddButton(mainPanel.transform, "설정", 1, () => ShowPage(Page.Settings));
        AddButton(mainPanel.transform, "메인 메뉴", 2, () => ShowPage(Page.ConfirmMainMenu));
        AddButton(mainPanel.transform, "게임 종료", 3, () => ShowPage(Page.ConfirmQuit));

        settingsPanel = BuildPanel("SettingsPanel", "설정", 440f);
        sensitivitySlider = AddSlider(settingsPanel.transform, "마우스 감도", 0,
            GameSettings.MouseSensitivityMin, GameSettings.MouseSensitivityMax, OnSensitivityChanged, out sensitivityValue);
        volumeSlider = AddSlider(settingsPanel.transform, "전체 음량", 1,
            GameSettings.MasterVolumeMin, GameSettings.MasterVolumeMax, OnVolumeChanged, out volumeValue);
        firstSettingsButton = AddButton(settingsPanel.transform, "뒤로", 3, () => { GameSettings.Save(); ShowPage(Page.Main); });

        confirmPanel = BuildPanel("ConfirmPanel", "", 360f);
        confirmText = NewText("Message", confirmPanel.transform, 28, TextAnchor.MiddleCenter);
        var mr = confirmText.rectTransform;
        mr.anchorMin = new Vector2(0f, 1f); mr.anchorMax = new Vector2(1f, 1f); mr.pivot = new Vector2(.5f, 1f);
        mr.sizeDelta = new Vector2(-60f, 120f); mr.anchoredPosition = new Vector2(0f, -50f);
        confirmYesButton = AddButton(confirmPanel.transform, "예", 2, () =>
        {
            if (page == Page.ConfirmMainMenu) OnMainMenuConfirmed(); else OnQuitConfirmed();
        });
        var noButton = AddButton(confirmPanel.transform, "아니요", 3, () => ShowPage(Page.Main));
        PlaceConfirmationButton(confirmYesButton, -110f);
        PlaceConfirmationButton(noButton, 110f);
        var yesNavigation = confirmYesButton.navigation;
        yesNavigation.mode = Navigation.Mode.Explicit;
        yesNavigation.selectOnRight = noButton;
        yesNavigation.selectOnLeft = noButton;
        confirmYesButton.navigation = yesNavigation;
        var noNavigation = noButton.navigation;
        noNavigation.mode = Navigation.Mode.Explicit;
        noNavigation.selectOnLeft = confirmYesButton;
        noNavigation.selectOnRight = confirmYesButton;
        noButton.navigation = noNavigation;

        root.SetActive(false);
    }

    private GameObject BuildPanel(string name, string title, float height)
    {
        var card = NewImage(name, root.transform);
        ChapterHudStyle.SkinCard(card, bright);
        card.raycastTarget = true;
        var rt = card.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(520f, height);
        rt.anchoredPosition = Vector2.zero;

        if (!string.IsNullOrEmpty(title))
        {
            var t = NewText("Title", card.transform, 40, TextAnchor.MiddleCenter);
            t.text = title;
            t.font = HangulFont.GetEmphasis();
            var tr = t.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(.5f, 1f);
            tr.sizeDelta = new Vector2(0f, 70f); tr.anchoredPosition = new Vector2(0f, -22f);
        }
        return card.gameObject;
    }

    private static void PlaceConfirmationButton(Button button, float x)
    {
        var rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(200f, 62f);
        rect.anchoredPosition = new Vector2(x, -236f);
    }

    // 패널 아래쪽부터 index 0,1,2,3 순서로 위에서 아래로 쌓는다 (제목 아래 첫 줄이 0).
    private static float RowY(int index) => -110f - index * 76f;

    private Button AddButton(Transform panel, string label, int row, UnityEngine.Events.UnityAction onClick)
    {
        var image = NewImage(label + "Button", panel);
        ChapterHudStyle.SkinCard(image, bright);
        image.raycastTarget = true;
        var rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f);
        rt.pivot = new Vector2(.5f, 1f);
        rt.sizeDelta = new Vector2(380f, 62f);
        rt.anchoredPosition = new Vector2(0f, RowY(row));

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, .92f);
        colors.highlightedColor = bright ? new Color(.86f, .95f, 1f) : new Color(.78f, .72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = bright ? new Color(.72f, .86f, .95f) : new Color(.62f, .55f, .9f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var text = NewText("Label", image.transform, 28, TextAnchor.MiddleCenter);
        text.text = label;
        text.font = HangulFont.GetEmphasis();
        Stretch(text.rectTransform);
        return button;
    }

    private Slider AddSlider(Transform panel, string label, int row, float min, float max,
        UnityEngine.Events.UnityAction<float> onChanged, out Text valueText)
    {
        var line = new GameObject(label + "Row", typeof(RectTransform)).GetComponent<RectTransform>();
        line.SetParent(panel, false);
        line.anchorMin = line.anchorMax = new Vector2(.5f, 1f); line.pivot = new Vector2(.5f, 1f);
        line.sizeDelta = new Vector2(420f, 62f); line.anchoredPosition = new Vector2(0f, RowY(row));

        var name = NewText("Label", line, 24, TextAnchor.UpperLeft);
        name.text = label;
        var nr = name.rectTransform;
        nr.anchorMin = new Vector2(0f, 1f); nr.anchorMax = new Vector2(1f, 1f); nr.pivot = new Vector2(0f, 1f);
        nr.sizeDelta = new Vector2(0f, 28f); nr.anchoredPosition = Vector2.zero;

        valueText = NewText("Value", line, 24, TextAnchor.UpperRight);
        var vr = valueText.rectTransform;
        vr.anchorMin = new Vector2(0f, 1f); vr.anchorMax = new Vector2(1f, 1f); vr.pivot = new Vector2(1f, 1f);
        vr.sizeDelta = new Vector2(0f, 28f); vr.anchoredPosition = Vector2.zero;

        // 막대 + 채움 + 손잡이
        var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        var sr = sliderGO.GetComponent<RectTransform>();
        sr.SetParent(line, false);
        sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(1f, 0f); sr.pivot = new Vector2(.5f, 0f);
        sr.sizeDelta = new Vector2(0f, 22f); sr.anchoredPosition = new Vector2(0f, 2f);

        var track = NewImage("Track", sr);
        Stretch(track.rectTransform);
        track.rectTransform.offsetMin = new Vector2(0f, 7f); track.rectTransform.offsetMax = new Vector2(0f, -7f);
        track.color = bright ? new Color(.80f, .86f, .90f) : new Color(.20f, .16f, .30f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sr, false); Stretch(fillArea);
        fillArea.offsetMin = new Vector2(0f, 7f); fillArea.offsetMax = new Vector2(0f, -7f);
        var fill = NewImage("Fill", fillArea);
        Stretch(fill.rectTransform);
        fill.color = ChapterHudStyle.Accent(bright);

        var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(sr, false); Stretch(handleArea);
        var handle = NewImage("Handle", handleArea);
        handle.rectTransform.sizeDelta = new Vector2(22f, 22f);
        handle.color = bright ? new Color(1f, .98f, .93f) : new Color(.93f, .90f, 1f);
        handle.raycastTarget = true;

        var slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private Image NewImage(string name, Transform parent)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.raycastTarget = false;
        return image;
    }

    private Text NewText(string name, Transform parent, int size, TextAnchor align)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = HangulFont.Get();
        text.fontSize = size;
        text.alignment = align;
        text.color = ChapterHudStyle.Ink(bright);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.3f;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
