using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 1인칭 플레이어 한 벌(몸통 + 카메라 + 보이는 손)과
/// 화면에 뜨는 E 안내문 UI 를 만들어 주는 에디터 전용 도구.
///
/// 손이 벽을 뚫고 잘리지 않도록, 손만 따로 ViewModel 레이어 + 전용 카메라로 그린다.
///   - Main Camera      : ViewModel 레이어를 빼고 방을 그린다
///   - ViewModelCamera  : 그 위에 ViewModel 레이어(손)만 덧그린다
/// </summary>
public static class HospitalPlayerBuilder
{
    public const string ViewModelLayerName = "ViewModel";

    // 화면 아래에 보이는 팔 모델
    private const string LeftArmModelPath  = "Assets/Art/Models/LeftArm.fbx";
    private const string RightArmModelPath = "Assets/Art/Models/RightArm.fbx";

    // 씨에서 눈으로 맞춘 값.
    // 더 낮추고 싶으면 y 를 내리고, 더 보이게 하려면 y 를 올린다.
    // 멈춰 있을 때의 자리. 화면 아래로 완전히 숨는 위치다.
    // 걸으면 FirstPersonHandsController 가 WalkOffset 만큼 올려 준다.
    private static readonly Vector3 HandsRootLocalPosition = new Vector3(0f, -0.434f, 0.470f);
    private static readonly Vector3 HandsWalkOffset        = new Vector3(0f, 0.150f, 0.030f);
    private const float HandsRootScale = 0.88f;
    // 팔을 바깥쪽으로 벌리는 각도와, 손끓이 살짝 들리게 하는 각도
    private const float ArmYawOutward     = 12f;    // 바깥으로 벌리는 각도
    private const float ArmOutwardOffset  = 0.06f;  // 바깥으로 더 밀어내는 거리 (m)
    private const float ArmPitch          = 14f;    // 클수록 팔을 아래로 내린다
    // ============================================================
    // 플레이어
    // ============================================================

    /// <summary>1인칭 플레이어를 만들고 루트 오브젝트를 돌려준다.</summary>
    public static GameObject BuildPlayer(Vector3 spawnPosition, float yaw, string materialFolder,
                                         InteractionPromptUI promptUI)
    {
        int viewModelLayer = BuildUtil.EnsureLayer(ViewModelLayerName);



        // ---------- 몸통 ----------
        var player = new GameObject("Player");
        player.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, yaw, 0f));

        var controller = player.AddComponent<CharacterController>();
        controller.height    = 1.70f;
        controller.radius    = 0.28f;
        controller.center    = new Vector3(0f, 0.86f, 0f);
        controller.skinWidth = 0.02f;
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.30f;

        // ---------- 머리(카메라가 달릴 자리) ----------
        var pivot = BuildUtil.Empty("CameraPivot", player.transform, new Vector3(0f, 1.62f, 0f));

        // ---------- 메인 카메라 ----------
        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.SetParent(pivot.transform, false);

        var camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags     = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
        camera.fieldOfView    = 62f;
        camera.nearClipPlane  = 0.05f;
        camera.farClipPlane   = 300f;
        camera.cullingMask    = ~(1 << viewModelLayer);   // 손은 이 카메라가 그리지 않는다

        // ---------- 손 전용 카메라 ----------
        var viewModelCameraGO = new GameObject("ViewModelCamera", typeof(Camera));
        viewModelCameraGO.transform.SetParent(cameraGO.transform, false);

        var viewModelCamera = viewModelCameraGO.GetComponent<Camera>();
        viewModelCamera.clearFlags    = CameraClearFlags.Depth;   // 방 위에 덧그린다
        viewModelCamera.cullingMask   = 1 << viewModelLayer;      // 손만 그린다
        viewModelCamera.fieldOfView   = 55f;
        viewModelCamera.nearClipPlane = 0.01f;
        viewModelCamera.farClipPlane  = 6f;
        viewModelCamera.depth         = camera.depth + 1;
        viewModelCamera.useOcclusionCulling = false;
        // ---------- 손 (모델) ----------
        var handsRoot = BuildUtil.Empty("HandsRoot", viewModelCameraGO.transform, HandsRootLocalPosition);
        handsRoot.transform.localScale = Vector3.one * HandsRootScale;
        var handsComponent = handsRoot.AddComponent<FirstPersonHandsController>();        PlaceArm(handsRoot.transform, "LeftArm",  LeftArmModelPath,  -ArmYawOutward);
        PlaceArm(handsRoot.transform, "RightArm", RightArmModelPath,  ArmYawOutward);
        // 손은 벽에 붙어도 잘리면 안 되므로 ViewModel 레이어로 둔다.
        BuildUtil.SetLayerRecursive(handsRoot, viewModelLayer);
        // ---------- 스크립트 ----------
        var firstPerson = player.AddComponent<FirstPersonController>();
        var interactor  = player.AddComponent<PlayerInteractor>();

        var fpsSerialized = new SerializedObject(firstPerson);
        fpsSerialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
        fpsSerialized.ApplyModifiedPropertiesWithoutUndo();

        var handsSerialized = new SerializedObject(handsComponent);
        handsSerialized.FindProperty("playerController").objectReferenceValue = firstPerson;
        handsSerialized.FindProperty("playerBody").objectReferenceValue = player.transform;
        handsSerialized.FindProperty("walkOffset").vector3Value = HandsWalkOffset;
        handsSerialized.FindProperty("leftArm").objectReferenceValue  = handsRoot.transform.Find("LeftArm");
        handsSerialized.FindProperty("rightArm").objectReferenceValue = handsRoot.transform.Find("RightArm");
        handsSerialized.ApplyModifiedPropertiesWithoutUndo();

        var interactorSerialized = new SerializedObject(interactor);
        interactorSerialized.FindProperty("viewCamera").objectReferenceValue = camera;
        interactorSerialized.FindProperty("promptUI").objectReferenceValue   = promptUI;
        interactorSerialized.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    /// <summary>손 한 짝을 만든다. side 가 -1 이면 왼손, +1 이면 오른손.</summary>
    /// <summary>
    /// 팔 모델 한 쪽을 HandsRoot 밑에 놓는다.
    /// 좌우 위치는 모델 자체에 들어 있으므로 위치는 0 으로 두고 각도만 준다.
    /// </summary>
    /// <summary>
    /// 팔 모델 한 쪽을 HandsRoot 밑에 놓는다.
    /// 좌우 간격은 모델 자체에 들어 있고, 여기서는 바깥으로 조금 더 밀고 각도만 준다.
    /// </summary>
    private static void PlaceArm(Transform parent, string name, string assetPath, float yaw)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (model == null)
        {
            Debug.LogWarning($"[HospitalPlayerBuilder] 팔 모델을 찾지 못했습니다: {assetPath}");
            return;
        }

        var arm = (GameObject)PrefabUtility.InstantiatePrefab(model);
        arm.name = name;
        arm.transform.SetParent(parent, false);

        float side = yaw < 0f ? -1f : 1f;
        arm.transform.localPosition = new Vector3(side * ArmOutwardOffset, 0f, 0f);

        // FBX 임포터가 넣어 둔 축 보정은 그대로 두고 그 위에 각도만 준다.
        arm.transform.localRotation = Quaternion.Euler(ArmPitch, yaw, 0f) * model.transform.localRotation;

        // 크기도 모델이 가지고 있는 값을 그대로 쓴다. (1 로 덮어쓰면 100배 작아진다)
        arm.transform.localScale = model.transform.localScale;
    }

    // ============================================================
    // 상호작용 안내문 UI
    // ============================================================

    /// <summary>화면 가운데 조준점 + 아래쪽 E 안내문을 만든다.</summary>
    public static InteractionPromptUI BuildPromptUI()
    {
        // 버튼을 쓸 일이 생길 때를 대비해 EventSystem 도 같이 둔다.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var canvasGO = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ---------- 조준점 ----------
        var crosshair = NewImage("Crosshair", canvasRT, Vector2.zero, new Vector2(7f, 7f),
                                 new Color(1f, 1f, 1f, 0.5f));
        crosshair.raycastTarget = false;

        // ---------- 안내문 뭉치 ----------
        var promptGO = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(CanvasGroup));
        var promptRT = promptGO.GetComponent<RectTransform>();
        promptRT.SetParent(canvasRT, false);
        Place(promptRT, new Vector2(0f, -170f), new Vector2(760f, 130f));

        var group = promptGO.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // 윗줄 : 대상 이름
        var nameText = NewText("NameText", promptRT, "", 34, TextAnchor.LowerCenter,
                               new Vector2(0f, 44f), new Vector2(760f, 48f),
                               new Color(0.98f, 0.96f, 0.92f));

        // 아랫줄 : [E] + 행동
        var badge = NewImage("KeyBadge", promptRT, new Vector2(-67f, 0f), new Vector2(46f, 46f),
                             new Color(0.10f, 0.11f, 0.13f, 0.78f));
        badge.raycastTarget = false;

        var keyText = NewText("KeyText", badge.rectTransform, "E", 26, TextAnchor.MiddleCenter,
                              Vector2.zero, new Vector2(46f, 46f), Color.white);
        keyText.fontStyle = FontStyle.Bold;

        var actionText = NewText("ActionText", promptRT, "손대기", 30, TextAnchor.MiddleLeft,
                                 new Vector2(100f, 0f), new Vector2(260f, 46f),
                                 new Color(0.95f, 0.95f, 0.95f));

        // ---------- 스크립트 연결 ----------
        var promptUI = promptGO.AddComponent<InteractionPromptUI>();

        var serialized = new SerializedObject(promptUI);
        serialized.FindProperty("group").objectReferenceValue      = group;
        serialized.FindProperty("nameText").objectReferenceValue   = nameText;
        serialized.FindProperty("actionText").objectReferenceValue = actionText;
        serialized.FindProperty("keyText").objectReferenceValue    = keyText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return promptUI;
    }

    // ============================================================
    // UI 도우미
    // ============================================================

    private static void Place(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    private static Image NewImage(string name, RectTransform parent, Vector2 anchoredPosition,
                                  Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Place(rt, anchoredPosition, size);

        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text NewText(string name, RectTransform parent, string content, int fontSize,
                                TextAnchor anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Place(rt, anchoredPosition, size);

        var text = go.GetComponent<Text>();

        // 저장이 되는 기본 폰트를 넣어 둔다.
        // 한글 폰트는 실행할 때 HangulFont 가 갈아 끼운다. (OS 폰트는 에셋으로 저장되지 않기 때문)
        text.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text          = content;
        text.fontSize      = fontSize;
        text.alignment     = anchor;
        text.color         = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow   = VerticalWrapMode.Overflow;

        // 어두운 배경에서도 읽히도록 그림자를 준다.
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}
