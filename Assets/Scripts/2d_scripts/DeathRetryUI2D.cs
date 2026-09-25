using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 죽었을 때 화면을 덮고 리트라이를 받는다. ScreenHint2D처럼 런타임에 캔버스를 만들어서
// 씬마다 배선할 것이 없다. 이 컴포넌트가 씬에 있으면 PlayerMovement2D.Die()가
// sceneOnDeath로 씬을 넘기는 대신 이쪽을 부른다.
public class DeathRetryUI2D : MonoBehaviour
{
    public Font font;
    public string deathMessage = "악몽에 삼켜졌다";
    public string retryPrompt = "R키를 눌러 다시 도전";
    public Color messageColor = new Color(0.86f, 0.24f, 0.28f);
    public Color promptColor = new Color(1f, 0.92f, 0.65f);
    public float fadeDuration = 0.8f;
    [Tooltip("덮개가 가장 진해졌을 때의 불투명도. 1이면 뒤가 완전히 안 보인다.")]
    [Range(0f, 1f)] public float veilAlpha = 0.88f;
    [Tooltip("프롬프트가 뜨기까지 기다리는 시간. 죽자마자 키가 먹으면 오입력으로 넘어간다.")]
    public float promptDelay = 0.7f;

    private GameObject canvasGO;
    private Image veil;
    private Text messageText;
    private Text promptText;
    private bool shown;
    private bool acceptingInput;
    private bool diedInPhase2;

    public bool IsShown => shown;

    /// <summary>A death screen is up in this scene (the pause menu stays shut while it is).</summary>
    public static bool AnyShown { get; private set; }

    void OnDestroy() { AnyShown = false; }

    public void Show()
    {
        if (shown) return;
        shown = true;
        AnyShown = true;

        // 씬을 다시 올리면 페이즈가 날아가므로 지금 읽어둔다
        BossPhaseController2D phases = FindFirstObjectByType<BossPhaseController2D>();
        diedInPhase2 = phases != null && phases.Phase == 2;

        Build();
        // 보스 코루틴과 플레이어를 한 번에 세운다. 아래 연출은 전부 unscaled로 돈다.
        Time.timeScale = 0f;
        StartCoroutine(ShowRoutine());
    }

    private void Build()
    {
        if (canvasGO != null) return;

        canvasGO = new GameObject("DeathRetryCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // ScreenHint2D가 15라 그 위를 덮어야 한다
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        veil = NewChild("Veil").AddComponent<Image>();
        veil.color = new Color(0.03f, 0.01f, 0.05f, 0f);
        veil.raycastTarget = false;
        Stretch(veil.rectTransform);

        messageText = NewText("DeathMessage", deathMessage, 72, messageColor, 0.56f);
        promptText = NewText("RetryPrompt", retryPrompt, 34, promptColor, 0.40f);
        ChapterNoticeStyle.Apply(promptText, ChapterDialogueSkin.Theme.BadDream, 580f, 72f, 28);
    }

    private GameObject NewChild(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvasGO.transform, false);
        return go;
    }

    private Text NewText(string name, string content, int size, Color color, float height01)
    {
        Text t = NewChild(name).AddComponent<Text>();
        t.font = font != null ? font : HangulFont.Get();
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        HangulFont.Apply(t);
        t.lineSpacing = 1.25f;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(color.r, color.g, color.b, 0f);
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.text = content;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, height01);
        rt.anchorMax = new Vector2(1f, height01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, size + 40f);
        rt.anchoredPosition = Vector2.zero;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator ShowRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            SetAlpha(veil, k * veilAlpha);
            SetAlpha(messageText, k);
            yield return null;
        }
        SetAlpha(veil, veilAlpha);
        SetAlpha(messageText, 1f);

        float held = 0f;
        while (held < promptDelay)
        {
            held += Time.unscaledDeltaTime;
            yield return null;
        }

        SetAlpha(promptText, 1f);
        acceptingInput = true;
    }

    void Update()
    {
        if (!acceptingInput) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            acceptingInput = false;
            Retry();
        }
    }

    private void Retry()
    {
        // timeScale 은 SceneFader 가 화면을 다 덮은 뒤 1 로 되돌린다 - 페이드 동안에는 멈춘 채로 둔다.
        // 이 씬의 인트로만 건너뛴다. 인트로 컷신은 재생이 끝나면 스스로 사라지므로 컴포넌트 존재로는
        // 판정할 수 없고, 정적 플래그가 다른 스테이지로 새면 처음 입장에서도 컷신이 빠진다.
        var scene = SceneManager.GetActiveScene();
        if (scene.name == "BadDream_Stage3") Stage3BossIntroCutscene.SkipIntroOnce = true;
        if (scene.name == "BadDream_stage2") Stage2IntroCutscene.SkipIntroOnce = true;
        BossPhaseController2D.ResumeAtPhase2 = diedInPhase2;
        // 죽고 다시 도전하는 거니 타임어택도 5분 그대로 다시 채운다.
        TimeAttackTimer2D.ResetTimer();
        SceneFader.LoadScene(scene.buildIndex);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = a;
        g.color = c;
    }
}
