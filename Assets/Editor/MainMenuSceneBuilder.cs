using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 Scene 과 임시 게임 Scene 을 자동으로 만들어 주는 에디터 전용 도구.
///
/// 이미 완성된 Scene 이 프로젝트에 들어 있으므로 평소에는 쓸 일이 없지만,
/// Scene 을 망가뜨렸을 때 Tools > Main Menu > Rebuild Scenes 로 다시 만들 수 있다.
/// (Editor 폴더에 있으므로 빌드된 게임에는 포함되지 않는다)
/// </summary>
public static class MainMenuSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath     = "Assets/Scenes/Game.unity";
    private const string GameSceneName     = "HospitalRoom";

    // 오프닝 배경 프레임이 들어 있는 폴더 (opening_000.png ~ )
    private const string OpeningFramesFolder = "Assets/Art/Opening";

    // 원본 GIF 가 프레임당 120ms 였으므로 초당 약 8.33장
    private const float OpeningFramesPerSecond = 8.3333f;

    // ---- 색상 ----
    private static readonly Color BackgroundGray  = new Color(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Color ButtonNormal    = new Color(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color ButtonHighlight = new Color(0.95f, 0.95f, 0.95f, 1f);
    private static readonly Color ButtonPressed   = new Color(0.65f, 0.65f, 0.65f, 1f);
    private static readonly Color ButtonTextColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color PanelTextColor  = new Color(0.93f, 0.93f, 0.93f, 1f);

    [MenuItem("Tools/Main Menu/Rebuild Scenes")]
    public static void BuildAll()
    {
        BuildGameScene();
        BuildMainMenuScene();

        // Build 목록에 MainMenu(0번), Game(1번) 순서로 등록한다.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Loading.unity", true),
            new EditorBuildSettingsScene(GameScenePath,     true),
            new EditorBuildSettingsScene("Assets/Scenes/HospitalRoom.unity", true),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MainMenuSceneBuilder] MainMenu.unity / Game.unity 생성 완료.");
    }

    // ==========================================================
    // 메인 메뉴 Scene
    // ==========================================================
    private static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- Main Camera ----------
        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BackgroundGray;
        cameraGO.transform.position = new Vector3(0f, 1f, -10f);

        // ---------- EventSystem (버튼 클릭을 받으려면 반드시 필요) ----------
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ---------- Canvas ----------
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;   // 가로/세로 변화에 골고루 대응

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ---------- Background (오프닝 애니메이션) ----------
        var background = CreateStretchedObject("Background", canvasRT);
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.raycastTarget = false;

        var openingFrames = LoadOpeningFrames();
        if (MainMenuLobbyArt.Apply(bgImage))
        {
            // ReDream animation (or the fallback lobby artwork) is configured here.
        }
        else if (openingFrames.Length > 0)
        {
            bgImage.sprite = openingFrames[0];
            bgImage.color   = Color.white;
            bgImage.type    = Image.Type.Simple;

            // 프레임을 순서대로 갈아 끼우는 스크립트를 붙이고 값을 채워 준다.
            var player = background.gameObject.AddComponent<OpeningBackgroundPlayer>();
            var pso = new SerializedObject(player);
            pso.FindProperty("targetImage").objectReferenceValue = bgImage;

            var frameArray = pso.FindProperty("frames");
            frameArray.arraySize = openingFrames.Length;
            for (int i = 0; i < openingFrames.Length; i++)
                frameArray.GetArrayElementAtIndex(i).objectReferenceValue = openingFrames[i];

            pso.FindProperty("framesPerSecond").floatValue = OpeningFramesPerSecond;
            pso.FindProperty("loop").boolValue = true;
            pso.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[MainMenuSceneBuilder] 오프닝 프레임 " + openingFrames.Length + "장을 배경에 연결했습니다.");
        }
        else
        {
            // 프레임을 못 찾으면 원래대로 단색 회색 배경을 쓴다.
            bgImage.color = BackgroundGray;
            Debug.LogWarning("[MainMenuSceneBuilder] " + OpeningFramesFolder + " 에서 오프닝 프레임을 찾지 못해 회색 배경을 사용합니다.");
        }

        // ---------- Scrim (배경 위를 살짝 덮어 버튼 글씨를 읽기 쉽게) ----------
        var scrim = CreateStretchedObject("Scrim", canvasRT);
        var scrimImage = scrim.gameObject.AddComponent<Image>();
        scrimImage.color = new Color(0f, 0f, 0f, MainMenuLobbyArt.HasReDreamArtwork(bgImage) ? .06f : .18f);
        scrimImage.raycastTarget = false;

        // ---------- 관리 스크립트를 붙일 오브젝트 ----------
        var managerGO = new GameObject("MainMenuManager");
        var manager = managerGO.AddComponent<MainMenuManager>();

        var res = GetDefaultUIResources();

        // ---------- MainMenuPanel ----------
        var mainPanel = CreateStretchedObject("MainMenuPanel", canvasRT);
        var startButton  = CreateButton(mainPanel, "StartButton",  "START",  new Vector2(0f,  110f), res);
        var optionButton = CreateButton(mainPanel, "OptionButton", "OPTION", new Vector2(0f,    0f), res);
        var exitButton   = CreateButton(mainPanel, "ExitButton",   "EXIT",   new Vector2(0f, -110f), res);
        MainMenuVisualDesign.Apply(mainPanel);

        // ---------- OptionPanel ----------
        var optionPanel = CreateStretchedObject("OptionPanel", canvasRT);

        CreateLabel(optionPanel, "OptionTitle", "OPTION",
                    new Vector2(0f, 230f), new Vector2(600f, 60f), 44, TextAnchor.MiddleCenter);

        CreateLabel(optionPanel, "MouseSensitivityLabel", "Mouse Sensitivity",
                    new Vector2(-60f, 130f), new Vector2(600f, 40f), 28, TextAnchor.MiddleLeft);
        var sensitivitySlider = CreateSlider(optionPanel, "MouseSensitivitySlider",
                    new Vector2(-50f, 80f), new Vector2(500f, 24f), res);
        var sensitivityValue = CreateLabel(optionPanel, "MouseSensitivityValue", "1.0",
                    new Vector2(290f, 80f), new Vector2(140f, 40f), 28, TextAnchor.MiddleCenter);

        CreateLabel(optionPanel, "MasterVolumeLabel", "Master Volume",
                    new Vector2(-60f, 0f), new Vector2(600f, 40f), 28, TextAnchor.MiddleLeft);
        var volumeSlider = CreateSlider(optionPanel, "MasterVolumeSlider",
                    new Vector2(-50f, -50f), new Vector2(500f, 24f), res);
        var volumeValue = CreateLabel(optionPanel, "MasterVolumeValue", "80",
                    new Vector2(290f, -50f), new Vector2(140f, 40f), 28, TextAnchor.MiddleCenter);

        var backButton = CreateButton(optionPanel, "BackButton", "BACK", new Vector2(0f, -190f), res);
        MainMenuVisualDesign.ApplyOption(optionPanel);

        // 슬라이더 기본 범위 (실행 시 GameSettings 의 저장값으로 다시 맞춰진다)
        sensitivitySlider.minValue = GameSettings.MouseSensitivityMin;
        sensitivitySlider.maxValue = GameSettings.MouseSensitivityMax;
        sensitivitySlider.value    = GameSettings.MouseSensitivityDefault;

        volumeSlider.minValue = GameSettings.MasterVolumeMin;
        volumeSlider.maxValue = GameSettings.MasterVolumeMax;
        volumeSlider.value    = GameSettings.MasterVolumeDefault;

        // ---------- Inspector 필드 연결 ----------
        var so = new SerializedObject(manager);
        so.FindProperty("gameSceneName").stringValue = GameSceneName;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel.gameObject;
        so.FindProperty("optionPanel").objectReferenceValue   = optionPanel.gameObject;
        so.FindProperty("mouseSensitivitySlider").objectReferenceValue    = sensitivitySlider;
        so.FindProperty("mouseSensitivityValueText").objectReferenceValue = sensitivityValue;
        so.FindProperty("masterVolumeSlider").objectReferenceValue        = volumeSlider;
        so.FindProperty("masterVolumeValueText").objectReferenceValue     = volumeValue;
        so.FindProperty("masterVolumeParameter").stringValue = "MasterVolume";
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- 버튼 OnClick / 슬라이더 OnValueChanged 연결 ----------
        UnityEventTools.AddPersistentListener(startButton.onClick,  new UnityAction(manager.OnStartButton));
        UnityEventTools.AddPersistentListener(optionButton.onClick, new UnityAction(manager.OnOptionButton));
        UnityEventTools.AddPersistentListener(exitButton.onClick,   new UnityAction(manager.OnExitButton));
        UnityEventTools.AddPersistentListener(backButton.onClick,   new UnityAction(manager.OnBackButton));

        UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged,
            new UnityAction<float>(manager.OnMouseSensitivityChanged));
        UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged,
            new UnityAction<float>(manager.OnMasterVolumeChanged));

        // 옵션 패널은 처음에 꺼 둔다.
        optionPanel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    }

    // ==========================================================
    // START 로 이동할 임시 게임 Scene
    // ==========================================================
    private static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.position = new Vector3(0f, 0.5f, 0f);

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 2f, -6f);
            mainCam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    // ==========================================================
    // UI 만들기 도우미
    // ==========================================================

    /// <summary>
    /// 오프닝 프레임 PNG 들을 Sprite 로 읽어 온다.
    /// 3D 프로젝트에서는 PNG 가 기본적으로 Sprite 가 아니라서, 먼저 임포트 설정을 맞춰 준다.
    /// </summary>
    private static string OpeningFramePath(int index)
    {
        return string.Format("{0}/opening_{1:D3}.png", OpeningFramesFolder, index);
    }

    private static Sprite[] LoadOpeningFrames()
    {
        // ── 1단계: 프레임 PNG 들의 임포트 설정을 먼저 전부 맞춘다 ──
        // (설정을 바꾸자마자 같은 자리에서 Sprite 로 읽으면 재임포트가 끝나기 전이라 null 이 나온다)
        int frameCount = 0;

        for (int i = 0; ; i++)
        {
            string path = OpeningFramePath(i);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                break;   // 더 이상 프레임이 없다

            frameCount++;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                // 화면을 꽉 채우는 배경이라 밉맵이 필요 없다 (메모리 절약)
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.alphaSource != TextureImporterAlphaSource.None)
            {
                // 불투명한 배경이라 알파 채널이 필요 없다
                importer.alphaSource = TextureImporterAlphaSource.None;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }

        // 재임포트가 모두 반영되도록 한 번 정리한다.
        AssetDatabase.Refresh();

        // ── 2단계: 설정이 끝난 뒤에 Sprite 로 읽어 온다 ──
        var sprites = new System.Collections.Generic.List<Sprite>();

        for (int i = 0; i < frameCount; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpeningFramePath(i));
            if (sprite != null)
                sprites.Add(sprite);
        }

        if (sprites.Count != frameCount)
        {
            Debug.LogWarning("[MainMenuSceneBuilder] 프레임 " + frameCount + "개 중 " + sprites.Count +
                             "개만 Sprite 로 읽혔습니다. 메뉴를 한 번 더 실행하면 나머지가 붙습니다.");
        }

        return sprites.ToArray();
    }

    private static DefaultControls.Resources GetDefaultUIResources()
    {
        return new DefaultControls.Resources
        {
            standard   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };
    }

    /// <summary>부모 전체를 꽉 채우는 빈 RectTransform 오브젝트를 만든다.</summary>
    private static RectTransform CreateStretchedObject(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>화면 한가운데를 기준으로 위치를 잡아 준다.</summary>
    private static void PlaceAtCenter(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    private static Button CreateButton(RectTransform parent, string name, string label,
                                       Vector2 anchoredPosition, DefaultControls.Resources res)
    {
        var go = DefaultControls.CreateButton(res);
        go.name = name;
        go.transform.SetParent(parent, false);

        PlaceAtCenter(go.GetComponent<RectTransform>(), anchoredPosition, new Vector2(320f, 80f));

        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        // 마우스를 올렸을 때 살짝 밝아지는 정도의 Hover 효과
        var colors = button.colors;
        colors.normalColor      = ButtonNormal;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor     = ButtonPressed;
        colors.selectedColor    = ButtonNormal;   // 클릭 후에도 평소 색으로 보이게
        colors.disabledColor    = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        colors.fadeDuration     = 0.1f;
        button.colors = colors;

        var text = go.GetComponentInChildren<Text>();
        text.text      = label;
        text.fontSize  = 32;
        text.fontStyle = FontStyle.Bold;
        text.color     = ButtonTextColor;
        text.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    private static Text CreateLabel(RectTransform parent, string name, string content,
                                    Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor)
    {
        var go = DefaultControls.CreateText(GetDefaultUIResources());
        go.name = name;
        go.transform.SetParent(parent, false);

        PlaceAtCenter(go.GetComponent<RectTransform>(), anchoredPosition, size);

        var text = go.GetComponent<Text>();
        text.text          = content;
        text.fontSize      = fontSize;
        text.color         = PanelTextColor;
        text.alignment     = anchor;
        text.raycastTarget = false;
        return text;
    }

    private static Slider CreateSlider(RectTransform parent, string name,
                                       Vector2 anchoredPosition, Vector2 size, DefaultControls.Resources res)
    {
        var go = DefaultControls.CreateSlider(res);
        go.name = name;
        go.transform.SetParent(parent, false);

        PlaceAtCenter(go.GetComponent<RectTransform>(), anchoredPosition, size);
        return go.GetComponent<Slider>();
    }
}
