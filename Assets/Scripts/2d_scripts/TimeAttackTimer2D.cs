using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BadDream_Stage1~3 세 씬을 관통하는 타임어택 타이머. 화면 중앙 상단에 표시된다.
///
/// 남은 시간은 static으로 들고 있어 씬이 바뀌어도(Stage1→2→3) 리셋되지 않고 이어진다 -
/// PlayerMovement2D.CarriedHealth 등 이 프로젝트의 기존 씬 전환 상태 유지 패턴과 동일하다.
///
/// 시간이 멈추는 구간은 SD_BrightDream_Blockout_Rect의 TimeAttackTimer를 그대로 따른다:
///   1) Time.timeScale == 0 인 동안 (PauseMenu 일시정지, DeathRetryUI2D 사망/시간초과 연출) -
///      Time.deltaTime 자체가 0이 되어 저절로 멈춘다.
///   2) 2D 파트는 대사/컷신 중 timeScale을 건드리지 않고 대신 PlayerMovement2D 컴포넌트를
///      꺼서(enabled = false) 조작을 막는다 (Stage1IntroCutscene, Portal2D 등) - 그래서
///      DialogueUI.IsShowing 대신 PlayerMovement2D.Instance.enabled를 직접 확인한다.
/// </summary>
public class TimeAttackTimer2D : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [Tooltip("제한 시간(초). 기본 5분.")]
    [SerializeField] private float timeLimit = 300f;

    [Header("남은 시간이 얼마 없을 때")]
    [SerializeField] private float warningTime = 30f;
    [SerializeField] private Color warningColor = new Color(0.75f, 0.16f, 0.22f);
    [SerializeField] private float warningBlinksPerSecond = 2f;

    private static bool started;
    private static bool expired;
    private static float remaining;

    private Color normalColor;

    /// <summary>시간이 다 되어 게임 오버로 넘어갔으면 true.</summary>
    public static bool Expired => expired;

    private void Awake()
    {
        // 처음 진입(!started)이거나 죽어서 재시도하는 경우(DeathRetryUI2D.Retry가 그 전에
        // ResetTimer를 호출해 started를 꺼 둔다) 시간을 새로 채운다. 그 외(Stage1→2→3처럼
        // 죽지 않고 다음 스테이지로 넘어가는 경우)에는 남은 시간을 그대로 이어받는다.
        if (!started || expired)
        {
            started = true;
            expired = false;
            remaining = timeLimit;
        }
    }

    private void Start()
    {
        if (timerText != null)
        {
            var rect = timerText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -54);
            ChapterNoticeStyle.Apply(timerText, ChapterDialogueSkin.Theme.BadDream, 160, 60, 30);
            normalColor = ChapterDialogueSkin.Ink(ChapterDialogueSkin.Theme.BadDream);
        }
        UpdateText();
    }

    private void Update()
    {
        if (expired)
        {
            UpdateText();
            return;
        }

        // 대사/컷신 중(플레이어 조작이 꺼진 구간)에는 시간을 멈춘다.
        if (PlayerMovement2D.Instance != null && !PlayerMovement2D.Instance.enabled) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        UpdateText();

        if (remaining > 0f) return;

        expired = true;
        var retry = FindFirstObjectByType<DeathRetryUI2D>();
        if (retry != null)
        {
            retry.deathMessage = "시간 초과";
            retry.Show();
        }
    }

    private void UpdateText()
    {
        if (timerText == null) return;

        // 0.4초가 남았을 때 00:00으로 보이면 안 되므로 올림 처리한다.
        int totalSeconds = Mathf.CeilToInt(remaining);
        timerText.text = $"{totalSeconds / 60:0}:{totalSeconds % 60:00}";

        bool warning = !expired && remaining <= warningTime;
        if (!warning)
        {
            timerText.color = normalColor;
            return;
        }
        float blink = Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * warningBlinksPerSecond));
        timerText.color = Color.Lerp(warningColor * 0.6f, warningColor, blink);
    }

    /// <summary>메인 메뉴로 나가는 등 다음 판을 처음부터 시작할 때 호출해 타이머를 비운다.</summary>
    public static void ResetTimer()
    {
        started = false;
        expired = false;
        remaining = 0f;
    }
}
