using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 아래에 대사를 한 줄씩 띄워 주는 자막.
///
/// 한 줄이 다 찍히면 ▼ 가 깜빡이면서 기다리고,
/// 스페이스나 E 를 눌러야 다음 줄로 넘어간다. (혼자 넘어가지 않는다)
/// 재생 중에 새 대사가 들어오면 지금 것을 끊고 새 것으로 넘어간다.
///
/// 붙이는 곳 : UI_Canvas 밑의 Subtitle 오브젝트 (CanvasGroup + Text 가 있어야 함)
/// 부르는 쪽 : DialogueTrigger 가 알아서 찾아서 불러 준다.
/// </summary>
[DisallowMultipleComponent]
public class SubtitleUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Text lineText;

    // ============================================================
    // 넘기기
    // ============================================================
    [Header("넘기기")]
    [Tooltip("켜면 아래 키를 누를 때까지 다음 줄로 안 넘어간다. 끄면 Hold Time 만큼 있다가 혼자 넘어간다.")]
    [SerializeField] private bool waitForInput = true;

    [Tooltip("대사를 넘기는 키. 여러 개 넣어도 된다.")]
    [SerializeField] private KeyCode[] advanceKeys = { KeyCode.Space, KeyCode.E };

    [Tooltip("글자가 찍히는 도중에 누르면 남은 글자를 한 번에 보여 준다.")]
    [SerializeField] private bool skipTypingOnInput = true;

    // ============================================================
    // 타이밍
    // ============================================================
    [Header("타이밍 (초)")]
    [Tooltip("나타나는 시간")]
    [SerializeField] private float fadeInTime = 0.35f;

    [Tooltip("사라지는 시간")]
    [SerializeField] private float fadeOutTime = 0.5f;

    [Tooltip("한 줄이 화면에 머무는 시간. '눌러서 넘기기' 를 껐을 때만 쓴다.")]
    [SerializeField] private float holdTime = 2.4f;

    [Tooltip("줄과 줄 사이 쉬는 시간. '눌러서 넘기기' 를 껐을 때만 쓴다.")]
    [SerializeField] private float gapTime = 0.3f;

    // ============================================================
    // 타자기 효과
    // ============================================================
    [Header("타자기 효과")]
    [Tooltip("끄면 한 번에 다 나온다.")]
    [SerializeField] private bool typewriter = true;

    [Tooltip("초당 몇 글자씩 찍힐지")]
    [SerializeField] private float charsPerSecond = 26f;

    [Tooltip("쉼표·마침표에서 잠깐 쉬는 시간 (초)")]
    [SerializeField] private float punctuationPause = 0.14f;

    // ============================================================
    // 선택 요소 (비워 두어도 동작한다)
    // ============================================================
    [Header("선택 요소")]
    [Tooltip("대사창 배경. 같은 CanvasGroup 안에 있으면 같이 페이드된다.")]
    [SerializeField] private Graphic panel;

    [Tooltip("줄이 다 찍힌 뒤 깜빡이는 표시 (▼ 같은 것)")]
    [SerializeField] private Graphic continueIndicator;
    [SerializeField] private float indicatorBlinkSpeed = 3.2f;

    [Tooltip("화자 이름표 상자. 이름이 비어 있으면 자동으로 숨는다.")]
    [SerializeField] private GameObject speakerTag;

    [SerializeField] private Text speakerText;
    // ------------------------------------------------------------
    private Coroutine _routine;

    /// <summary>지금 대사가 나오는 중인지</summary>
    public bool IsPlaying => _routine != null;

    /// <summary>
    /// 대사가 나오는 동안 true.
    ///
    /// 대사를 넘기려고 누른 E 가 상호작용까지 같이 눌러 버리면 곤란해서,
    /// PlayerInteractor 가 이 값을 보고 그동안 쉰다.
    /// </summary>
    public static bool Blocking { get; private set; }

    private void Awake()
    {
        // 플레이 모드를 껐다 켜도 남아 있지 않게 시작할 때 꺼 둔다.
        Blocking = false;

        // 한글이 깨지지 않도록 폰트를 갈아 끼운다.
        HangulFont.ApplyAll(gameObject);

        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (lineText != null) lineText.text = "";
    }

    private void OnDisable()
    {
        // 대사 도중에 꺼졌으면 플레이어를 묶어 둔 채로 두면 안 된다.
        Blocking = false;
    }

    // ============================================================
    // 바깥에서 부르는 것들
    // ============================================================

    /// <summary>대사 여러 줄을 순서대로 띄운다.</summary>
    public void Play(IList<string> lines)
    {
        Play(lines, null);
    }

    /// <summary>화자 이름과 함께 띄운다. 이름을 비우면 이름표가 안 나온다.</summary>
    public void Play(IList<string> lines, string speaker)
    {
        if (lines == null || lines.Count == 0) return;

        SetSpeaker(speaker);

        // 재생 중이던 게 있으면 끊고 새로 시작한다.
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(lines));
    }

    private void SetSpeaker(string speaker)
    {
        bool has = !string.IsNullOrWhiteSpace(speaker);

        if (has && speakerText != null) speakerText.text = speaker;
        if (speakerTag != null) speakerTag.SetActive(has);
    }

    /// <summary>대사 한 줄만 띄운다.</summary>
    public void PlayOne(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        Play(new[] { line });
    }

    /// <summary>지금 나오는 대사를 즉시 지운다.</summary>
    public void Clear()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        Blocking = false;

        if (group != null) group.alpha = 0f;
        if (lineText != null) lineText.text = "";
        if (speakerTag != null) speakerTag.SetActive(false);
    }

    // ============================================================
    // 내부
    // ============================================================

    private IEnumerator PlayRoutine(IList<string> lines)
    {
        Blocking = true;

        // 창은 처음에 한 번만 띄우고 끝까지 켜 둔다.
        // 줄마다 껐다 켜면 누를 때마다 깜빡여서 눈이 피곤하다.
        if (lineText != null) lineText.text = "";
        SetIndicatorAlpha(0f);

        yield return Fade(group != null ? group.alpha : 0f, 1f, fadeInTime);

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (lineText != null) lineText.text = "";
            SetIndicatorAlpha(0f);

            yield return TypeLine(line);

            if (waitForInput)
            {
                yield return WaitForAdvance();
            }
            else
            {
                yield return HoldWithIndicator(holdTime);
                SetIndicatorAlpha(0f);

                if (i < lines.Count - 1 && gapTime > 0f)
                    yield return new WaitForSeconds(gapTime);
            }
        }

        SetIndicatorAlpha(0f);
        yield return Fade(1f, 0f, fadeOutTime);

        if (lineText != null) lineText.text = "";
        _routine = null;
        Blocking = false;
    }

    /// <summary>글자를 하나씩 찍는다. 도중에 키를 누르면 나머지를 한 번에 보여 준다.</summary>
    private IEnumerator TypeLine(string line)
    {
        if (lineText == null) yield break;

        if (!typewriter || charsPerSecond <= 0f)
        {
            lineText.text = line;
            yield break;
        }

        float perChar = 1f / charsPerSecond;

        for (int c = 1; c <= line.Length; c++)
        {
            lineText.text = line.Substring(0, c);

            char just = line[c - 1];
            float wait = perChar;

            // 쉼표·마침표에서 살짝 쉬면 말하는 맛이 산다.
            if (just == ',' || just == '.' || just == '?' || just == '!' || just == '…')
                wait += punctuationPause;

            // WaitForSeconds 로 묶어 버리면 그 사이 키 입력을 놓친다. 그래서 직접 센다.
            float elapsed = 0f;
            while (elapsed < wait)
            {
                if (skipTypingOnInput && AdvancePressed())
                {
                    // 이 입력은 '건너뛰기' 로 다 쓴다.
                    // 한 프레임 넘겨야 다음 줄까지 같이 넘어가지 않는다.
                    lineText.text = line;
                    yield return null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        lineText.text = line;
    }

    /// <summary>▼ 를 깜빡이면서, 넘기기 키를 누를 때까지 기다린다.</summary>
    private IEnumerator WaitForAdvance()
    {
        // 직전 줄을 건너뛴 입력이 그대로 이어져 두 줄이 한 번에 넘어가지 않도록 한 프레임 비운다.
        yield return null;

        float elapsed = 0f;
        while (!AdvancePressed())
        {
            elapsed += Time.deltaTime;

            float a = (Mathf.Sin(elapsed * indicatorBlinkSpeed * Mathf.PI) + 1f) * 0.5f;
            SetIndicatorAlpha(Mathf.Lerp(0.15f, 1f, a));

            yield return null;
        }

        SetIndicatorAlpha(0f);
    }

    /// <summary>넘기기 키 중 하나라도 이번 프레임에 눌렸는지</summary>
    private bool AdvancePressed()
    {
        if (advanceKeys == null) return false;

        for (int i = 0; i < advanceKeys.Length; i++)
        {
            if (advanceKeys[i] == KeyCode.None) continue;
            if (Input.GetKeyDown(advanceKeys[i])) return true;
        }
        return false;
    }

    /// <summary>▼ 를 깜빡이며 정해진 시간만큼 기다린다. (혼자 넘어가는 모드용)</summary>
    private IEnumerator HoldWithIndicator(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (continueIndicator != null)
            {
                float a = (Mathf.Sin(elapsed * indicatorBlinkSpeed * Mathf.PI) + 1f) * 0.5f;
                SetIndicatorAlpha(Mathf.Lerp(0.15f, 1f, a));
            }
            yield return null;
        }
    }

    private void SetIndicatorAlpha(float a)
    {
        if (continueIndicator == null) return;

        var c = continueIndicator.color;
        c.a = a;
        continueIndicator.color = c;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f) { group.alpha = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
