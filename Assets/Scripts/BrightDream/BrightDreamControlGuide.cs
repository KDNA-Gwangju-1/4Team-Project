using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 시작 시 조작 안내창을 띄우고, Space를 누르면 닫는다.
/// 떠 있는 동안은 Blocking이 true가 되어 이동/상호작용/발사 스크립트가 입력을 쉰다.
/// 생김새는 병원 안내와 같은 ControlGuideBuilder 로 그린다 (밝은 꿈 색).
/// </summary>
public class BrightDreamControlGuide : MonoBehaviour
{
    [Tooltip("예전 글자 안내. 새 안내창을 쓰므로 시작할 때 꺼 둔다.")]
    [SerializeField] private Text guideText;

    /// <summary>안내창이 떠 있는 동안 true. 플레이어 조작 스크립트가 이 값을 보고 입력을 쉰다.</summary>
    public static bool Blocking { get; private set; }

    private GameObject canvasGO;
    private CanvasGroup footer;

    private void Awake()
    {
        Blocking = true;
        if (guideText != null) guideText.gameObject.SetActive(false);
        Build();
    }

    private void Build()
    {
        // HUD 위에 확실히 올라오도록 자기 캔버스를 가진다
        // 씬 루트에 둔다 - 이 컴포넌트가 다른 캔버스 안에 있으면 중첩 캔버스가 되어 스케일러가 무시된다
        canvasGO = new GameObject("BrightDreamControlGuideCanvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        footer = ControlGuideBuilder.Build((RectTransform)canvasGO.transform, ControlGuideBuilder.Theme.BrightDream,
            new ControlGuideBuilder.Content
            {
                title = "밝은 꿈",
                subtitle = "동생의 꿈속을 돌아다니며 기억의 단서를 찾아보세요.",
                leftHeader = "움직이기",
                left = new[]
                {
                    new ControlGuideBuilder.Row("이동", "앞뒤 좌우로 걷기", "WASD"),
                    new ControlGuideBuilder.Row("달리기", "누르고 있는 동안 빨라져요", "Shift"),
                    new ControlGuideBuilder.Row("점프", "대사 중에는 대사 넘기기", "Space"),
                    ControlGuideBuilder.Row.Mouse("M", "둘러보기", "마우스를 움직여 시점 전환"),
                },
                rightHeader = "행동",
                right = new[]
                {
                    new ControlGuideBuilder.Row("조사하기", "단서 가까이에서 눌러 살펴보기", "E"),
                    ControlGuideBuilder.Row.Mouse("L", "정화총 발사", "정화총을 얻은 뒤 사용할 수 있어요"),
                    new ControlGuideBuilder.Row("일시정지", "설정 · 메인 메뉴", "ESC"),
                },
                continueKey = "Space",
                continueText = "키를 눌러 시작",
            });
    }

    private void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
    }

    private void OnDisable()
    {
        // 플레이 모드를 껐다 켜도 true 로 남지 않게.
        Blocking = false;
    }

    private void Update()
    {
        if (!Blocking) return;
        ControlGuideBuilder.Pulse(footer);
        if (Input.GetKeyDown(KeyCode.Space)) Close();
    }

    private void Close()
    {
        Blocking = false;
        if (guideText != null) guideText.gameObject.SetActive(false);
        if (canvasGO != null) canvasGO.SetActive(false);
    }

    /// <summary>체크포인트 리스폰 등, 씬 맨 처음이 아닌 지점에서 다시 시작할 때 안내창을 즉시 닫는다.</summary>
    public void ForceClose() => Close();
}
