using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로딩 화면.
///
/// 이 씬이 뜨면 다음 씬을 뒤에서 미리 읽어들이고, 다 되면 알아서 넘어간다.
///
/// 쓰는 법 — 어디서든 이렇게 부르면 된다.
/// <code>
///     LoadingScreen.Go("HospitalRoom");                  // 기본 배경으로
///     LoadingScreen.Go("Dream", dreamBackgroundSprite);  // 배경을 바꿔서
///     LoadingScreen.Go("", dreamBackgroundSprite);       // 갈 곳 없이 로딩 화면만
/// </code>
///
/// 배경 그림만 갈아 끼우는 방식이라, 전환 종류가 늘어나도 로딩 씬은 하나면 된다.
/// 달 아이콘과 표시 시간은 로딩 씬에 있는 값을 그대로 쓰므로 어디서 불러도 똑같이 나온다.
///
/// 주의 : 로딩 씬과 목적지 씬이 둘 다 Build Settings 에 들어가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingScreen : MonoBehaviour
{
    /// <summary>로딩 씬 이름. 씬 파일 이름을 바꾸면 여기도 같이 바꿔야 한다.</summary>
    public const string LoadingSceneName = "Loading";

    [Header("참조")]
    [Tooltip("배경 그림. Go() 에 스프라이트를 같이 넘기면 이 자리에 갈아 끼워진다.\n" +
             "비워 두면 씬에서 'Background' 라는 이름으로 찾아본다.")]
    [SerializeField] private Image background;

    [Header("설정")]
    [Tooltip("Go() 를 거치지 않고 이 씬을 직접 실행했을 때 갈 곳. 테스트용이다.")]
    [SerializeField] private string fallbackScene = "HospitalRoom";

    [Tooltip("다 읽었더라도 최소 이만큼은 보여 준다.\n" +
             "안 그러면 빠른 컴퓨터에서 로딩 화면이 번쩍 지나가 버린다.")]
    [SerializeField] private float minimumDisplayTime = 2.5f;

    /// <summary>로딩이 끝나면 갈 씬. 비어 있으면 로딩 화면에 머문다.</summary>
    public static string NextScene { get; private set; }

    /// <summary>이번 전환에 쓸 배경 그림. 비어 있으면 로딩 씬에 원래 깔린 그림을 쓴다.</summary>
    public static Sprite NextBackground { get; private set; }

    // Go() 를 거쳐 들어왔는지. 로딩 씬을 그냥 직접 실행한 경우와 구분하려고 둔다.
    private static bool requested;

    /// <summary>로딩 화면을 거쳐서 씬을 바꾼다.</summary>
    public static void Go(string sceneName)
    {
        Go(sceneName, null);
    }

    /// <summary>
    /// 배경 그림을 바꿔 가며 로딩 화면을 띄운다.
    /// sceneName 을 비워 두면 갈 곳 없이 로딩 화면만 띄우고 머문다.
    /// (목적지 씬을 아직 안 만들었을 때 쓴다)
    /// </summary>
    public static void Go(string sceneName, Sprite background)
    {
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogError("[LoadingScreen] Loading scene is missing from Build Settings.");
            return;
        }

        bool hasTarget = !string.IsNullOrWhiteSpace(sceneName);
        if (hasTarget && (sceneName == LoadingSceneName ||
                          !Application.CanStreamedLevelBeLoaded(sceneName)))
        {
            Debug.LogError("[LoadingScreen] Invalid loading destination: " + sceneName);
            return;
        }

        NextScene = hasTarget ? sceneName : null;
        NextBackground = background;
        requested = true;

        SceneManager.LoadScene(LoadingSceneName);
    }

    private void Start()
    {
        // 앞 씬의 상태가 넘어오지 않게 되돌린다. 1인칭 씬은 커서를 잠근 채로 떠날 수 있고
        // (BrightDream 포탈은 컨트롤러만 끈다) 2D 스테이지는 커서를 따로 풀지 않는다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        bool viaGo = requested;
        requested = false;

        string target = viaGo ? NextScene : fallbackScene;
        NextScene = null;
        ApplyBackground(target);

        if (string.IsNullOrWhiteSpace(target))
        {
            // 갈 곳 없이 불린 경우. 로딩 화면만 띄운 채로 둔다.
            Debug.LogWarning("[LoadingScreen] 갈 씬이 비어 있어 로딩 화면에 머뭅니다. " +
                             "부른 쪽에 씬 이름을 채우면 넘어갑니다.", this);
            return;
        }

        if (target == LoadingSceneName || !Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError("[LoadingScreen] '" + target + "' 씬을 못 찾았습니다. " +
                           "Build Settings 에 들어가 있는지 확인해 주세요.", this);
            return;
        }

        StartCoroutine(LoadRoutine(target));
    }

    /// <summary>이번 전환에 넘어온 배경 그림이 있으면 갈아 끼운다.</summary>
    private void ApplyBackground(string target)
    {
        var art = NextBackground;
        NextBackground = null;          // 다음 전환에 흘러가지 않도록 항상 비운다.
        if (art == null)
        {
            string resource = target == "HospitalRoom" || target == "MainMenu" ? "HospitalDayLoadingBackground"
                : !string.IsNullOrEmpty(target) && target.Contains("BrightDream") ? "DreamLoadingBackground"
                : !string.IsNullOrEmpty(target) && target.Contains("BadDream") ? "BadDreamLoadingBackground" : null;
            if (resource != null) art = Resources.Load<Sprite>(resource);
        }

        var image = background;
        if (image == null)
        {
            // Inspector 연결이 비어 있으면 이름으로 찾아본다.
            var found = GameObject.Find("Background");
            if (found != null) image = found.GetComponent<Image>();
        }

        if (image != null)
        {
            if (art != null) image.sprite = art;
            FitBackground(image);
            ShowTip(image, target);
        }
        else Debug.LogWarning("[LoadingScreen] 배경 Image 를 못 찾아서 그림을 못 바꿨습니다. " +
                              "Background 칸에 연결해 주세요.", this);
    }

    /// <summary>Keep the complete artwork and its baked-in text visible without stretching.</summary>
    internal static void FitBackground(Image image)
    {
        if (image == null || image.sprite == null) return;
        // An opaque sibling covers the unused area, including during hospital overlay fades.
        // Keep it outside the fitted rect so it always covers the full viewport.
        var parent = image.transform.parent;
        if (parent == null) return;
        string backdropName = image.name + "Backdrop";
        var backdrop = parent.Find(backdropName);
        if (backdrop == null)
        {
            var fill = new GameObject(backdropName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(parent, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.color = new Color(0.02f, 0.02f, 0.05f, 1f);
            fill.raycastTarget = false;
            fill.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
        }
        image.preserveAspect = true;
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
        // Keep the animated moon beside the baked-in loading label when letterboxing.
        var icon = parent.Find("LoadingIcon") as RectTransform;
        if (icon != null)
        {
            Vector2 position = icon.anchoredPosition;
            icon.SetParent(image.transform, false);
            icon.anchoredPosition = position;
        }
        ShowTip(image);
    }

    // ============================================================
    // 로딩 팁 - 배경 그림의 두 가로줄 사이 빈칸에 매번 하나를 골라 띄운다.
    // (그림에 박혀 있던 문구는 지웠다. 문구를 더 넣으려면 이 배열에 추가하면 된다.)
    // ============================================================

    private static readonly string[] Tips =
    {
        "꿈병 환자는 깊은 잠에 빠진 채 깨어나지 않는다.\n뇌파는 정상이지만, 마음은 꿈속 어딘가에 머물러 있다.",
        "꿈탐정은 잠든 사람의 손을 잡아 그 꿈으로 들어간다.\n꿈속에서 찾은 기억의 조각이 깨어날 길을 알려 준다.",
        "쌍둥이는 같은 날 같은 꿈을 꾸기도 한답니다.\n마음이 닿아 있으면, 꿈도 이어질 수 있어요.",
        "밝은 꿈은 행복했던 기억으로 지어지고,\n악몽은 꺼내지 못한 마음으로 지어집니다.",
        "꿈속 인형들은 꿈꾸는 사람의 마음을 닮아요.\n검보라색 먹물은 그 마음에 스며든 상처랍니다.",
        "악몽 속 그림자는 빛을 비춰야 모습을 드러냅니다.\n보이지 않는 것과는 싸울 수도, 화해할 수도 없으니까요.",
        "누군가를 부러워하는 마음은 부끄러운 게 아니에요.\n다만 오래 숨겨 두면, 꿈속에서 모양을 갖게 된답니다.",
        "꿈병 환자의 심박은 매일 같은 시각에 치솟는다.\n그 시각은 환자가 가장 무서워했던 순간과 겹친다고 한다.",
        "꿈에서 찾은 물건은 대개 현실의 기억과 이어져 있어요.\n리본, 이름, 사진, 편지. 작은 것일수록 소중하답니다.",
        "전하지 못한 사과는 마음속에 오래 남아요.\n꿈은 가끔, 그 말을 건넬 두 번째 기회를 줍니다.",
    };

    private static int lastTip = -1;

    /// <summary>배경 그림 위 빈칸에 팁 하나를 무작위로 띄운다. 바로 앞에 나온 팁은 피한다.</summary>
    internal static void ShowTip(Image image, string destination = null)
    {
        if (image == null || image.sprite == null) return;

        const string tipName = "LoadingTip";
        var found = image.transform.Find(tipName);
        Text tip = found != null ? found.GetComponent<Text>() : null;
        if (tip == null)
        {
            tip = new GameObject(tipName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            tip.transform.SetParent(image.transform, false);
            tip.raycastTarget = false;
        }

        // 두 가로줄 사이 칸 (그림 기준 비율). 병원 그림과 꿈 그림은 칸 높이가 다르다.
        bool hospital = image.sprite.name == "LoadingBackground" || image.sprite.name == "HospitalDayLoadingBackground";
        float bottom = hospital ? 0.240f : 0.158f;
        float top = hospital ? 0.386f : 0.368f;
        var rt = tip.rectTransform;
        rt.anchorMin = new Vector2(0.207f, bottom);
        rt.anchorMax = new Vector2(0.793f, top);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        tip.font = HangulFont.Get();
        tip.fontStyle = FontStyle.Normal;
        tip.alignment = TextAnchor.MiddleLeft;
        tip.color = new Color(0.86f, 0.89f, 1f, 0.95f);
        tip.lineSpacing = 1.45f;
        tip.horizontalOverflow = HorizontalWrapMode.Wrap;
        tip.verticalOverflow = VerticalWrapMode.Truncate;
        tip.resizeTextForBestFit = true;
        tip.resizeTextMinSize = 20;
        tip.resizeTextMaxSize = 30;
        tip.fontSize = 30;

        int pick = Random.Range(0, Tips.Length);
        if (Tips.Length > 1 && pick == lastTip) pick = (pick + 1 + Random.Range(0, Tips.Length - 1)) % Tips.Length;
        lastTip = pick;
        if (destination == "MainMenu")
            tip.text = "메인 메뉴로 돌아갑니다.\n설정한 음량과 마우스 감도는 다음 플레이에도 유지됩니다.";
        else if (destination == "HospitalRoom")
            tip.text = "병원으로 들어갑니다.\n주변을 살펴보고, 안내가 나타나면 E 키로 상호작용하세요.";
        else if (!string.IsNullOrEmpty(destination) && destination.Contains("BrightDream"))
            tip.text = "밝은 꿈으로 들어갑니다.\n주변의 단서를 살펴보며 잠든 아이의 기억을 찾아보세요.";
        else if (!string.IsNullOrEmpty(destination) && destination.Contains("BadDream"))
            tip.text = "악몽으로 들어갑니다.\n손전등으로 그림자를 드러내고, 착지할 발판을 확인하세요.";
        else tip.text = Tips[pick];
    }

    private IEnumerator LoadRoutine(string target)
    {
        float started = Time.unscaledTime;
        // Let the loading artwork render before starting expensive scene deserialization.
        yield return null;

        var op = SceneManager.LoadSceneAsync(target);
        if (op == null)
        {
            Debug.LogError("[LoadingScreen] '" + target + "' 씬을 못 찾았습니다. " +
                           "Build Settings 에 들어가 있는지 확인해 주세요.");
            yield break;
        }

        // 다 읽어도 우리가 허락할 때까지 넘어가지 않게 잡아 둔다.
        op.allowSceneActivation = false;

        // allowSceneActivation 이 false 면 progress 는 0.9 에서 멈춘다. 그게 '다 읽었다' 는 뜻이다.
        while (op.progress < 0.9f) yield return null;

        float remain = minimumDisplayTime - (Time.unscaledTime - started);
        if (remain > 0f) yield return new WaitForSecondsRealtime(remain);

        op.allowSceneActivation = true;
    }
}
