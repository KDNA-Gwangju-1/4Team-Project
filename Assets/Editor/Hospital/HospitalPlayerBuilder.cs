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

    // ============================================================
    // 플레이어
    // ============================================================

    /// <summary>1인칭 플레이어를 만들고 루트 오브젝트를 돌려준다.</summary>
    public static GameObject BuildPlayer(Vector3 spawnPosition, float yaw, string materialFolder,
                                         InteractionPromptUI promptUI)
    {
        int viewModelLayer = BuildUtil.EnsureLayer(ViewModelLayerName);

        var skinMat   = BuildUtil.Mat(materialFolder, "M_HandSkin", new Color(0.90f, 0.76f, 0.68f), 0.10f);
        var sleeveMat = BuildUtil.Mat(materialFolder, "M_Sleeve",   new Color(0.31f, 0.36f, 0.43f), 0.06f);

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

        // ---------- 손 ----------
        var hands = BuildUtil.Empty("Hands", viewModelCameraGO.transform, new Vector3(0f, 0f, 0f));
        var handsComponent = hands.AddComponent<ViewModelHands>();

        MakeHand(hands.transform, -1f, skinMat, sleeveMat);   // 왼손
        MakeHand(hands.transform, +1f, skinMat, sleeveMat);   // 오른손

        BuildUtil.SetLayerRecursive(hands, viewModelLayer);

        // ---------- 스크립트 ----------
        var firstPerson = player.AddComponent<FirstPersonController>();
        var interactor  = player.AddComponent<PlayerInteractor>();

        var fpsSerialized = new SerializedObject(firstPerson);
        fpsSerialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
        fpsSerialized.ApplyModifiedPropertiesWithoutUndo();

        var handsSerialized = new SerializedObject(handsComponent);
        handsSerialized.FindProperty("player").objectReferenceValue = firstPerson;
        handsSerialized.ApplyModifiedPropertiesWithoutUndo();

        var interactorSerialized = new SerializedObject(interactor);
        interactorSerialized.FindProperty("viewCamera").objectReferenceValue = camera;
        interactorSerialized.FindProperty("promptUI").objectReferenceValue   = promptUI;
        interactorSerialized.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    /// <summary>손 한 짝을 만든다. side 가 -1 이면 왼손, +1 이면 오른손.</summary>
    private static void MakeHand(Transform parent, float side, Material skin, Material sleeve)
    {
        string name = side < 0f ? "Hand_L" : "Hand_R";

        // 화면 아래쪽 좌우 구석에 손이 걸치게 놓는다.
        var root = BuildUtil.Empty(name, parent, new Vector3(side * 0.235f, -0.170f, 0.42f));
        root.transform.localRotation = Quaternion.Euler(-16f, side * -13f, side * 7f);

        // 소매(팔뚝) — 화면 아래로 빠져나가도록 뒤쪽으로 길게 뻗는다.
        var forearm = BuildUtil.Capsule("Forearm", root.transform, new Vector3(0f, 0f, -0.22f), 0.090f, 0.34f, sleeve);
        forearm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 손목
        var wrist = BuildUtil.Capsule("Wrist", root.transform, new Vector3(0f, 0f, -0.048f), 0.072f, 0.09f, skin);
        wrist.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 손바닥
        BuildUtil.Box("Palm", root.transform, new Vector3(0f, 0f, 0.025f),
                      new Vector3(0.082f, 0.030f, 0.095f), skin);

        // 손가락 4개 (가운데가 조금 더 길다)
        for (int i = 0; i < 4; i++)
        {
            float x = -0.0285f + i * 0.019f;
            float length = 0.068f - Mathf.Abs(i - 1) * 0.007f;

            var finger = BuildUtil.Box($"Finger_{i + 1}", root.transform,
                                       new Vector3(x, -0.004f, 0.072f + length * 0.5f),
                                       new Vector3(0.0165f, 0.020f, length), skin);
            finger.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
        }

        // 엄지 (안쪽으로 붙는다)
        var thumb = BuildUtil.Box("Thumb", root.transform,
                                  new Vector3(side * -0.048f, -0.004f, 0.030f),
                                  new Vector3(0.022f, 0.024f, 0.056f), skin);
        thumb.transform.localRotation = Quaternion.Euler(6f, side * -34f, 0f);
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
