using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The shared talking-head panel. The frame art already carries the portrait and
// the name plate, so all this owns is the line, the pacing and the advance prompt.
// Built at runtime by whichever cutscene needs it - nothing to wire per scene.
public class DialogueWindow2D : MonoBehaviour
{
    public Font font;

    [Tooltip("Where the line sits inside the frame, in normalized frame coordinates.")]
    public Rect textArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int fontSize = 34;
    public Color textColor = Color.white;
    public float fadeDuration = 0.18f;

    [Tooltip("Speaker name spot, normalized frame coordinates. Matches where the player frame has its name plate drawn in.")]
    public Rect speakerArea = new Rect(0.11f, 0.30f, 0.18f, 0.06f);
    public int speakerFontSize = 32;

    [Tooltip("Panel width as a fraction of the screen.")]
    [Range(0.3f, 1f)] public float frameWidth01 = 0.88f;
    [Tooltip("Gap from the bottom of the screen, in 1080p reference pixels.")]
    public float frameBottomMargin = 24f;
    [Tooltip("Shift from screen centre, in 1080p reference pixels. Negative moves left.")]
    public float frameXOffset = -120f;

    [Tooltip("A line cannot be skipped before this, so a held key never eats one.")]
    public float lineMinDuration = 0.5f;
    [Tooltip("Times out on its own if nobody presses anything.")]
    public float lineAutoAdvance = 6f;
    public float lineGap = 0.3f;

    private GameObject canvasGO;
    private GameObject frameGO;
    private Image frameImage;
    private CanvasGroup group;
    private Text lineText;
    private Text speakerText;
    private Text advancePrompt;

    public bool Ready { get { return frameImage != null; } }

    public void Build(Sprite referenceFrame)
    {
        if (referenceFrame == null) return;

        canvasGO = new GameObject("DialogueWindowCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        frameGO = new GameObject("DialogueFrame");
        frameGO.transform.SetParent(canvasGO.transform, false);

        frameImage = frameGO.AddComponent<Image>();
        frameImage.raycastTarget = false;
        frameImage.preserveAspect = true;
        frameImage.sprite = referenceFrame;

        // the rect carries the art's own aspect, so preserveAspect never
        // letterboxes inside it and the line stays glued to the box
        float aspect = referenceFrame.rect.height > 0.001f
            ? referenceFrame.rect.width / referenceFrame.rect.height : 16f / 9f;
        float frameWidth = 1920f * frameWidth01;

        RectTransform frt = frameImage.rectTransform;
        frt.anchorMin = new Vector2(0.5f, 0f);
        frt.anchorMax = new Vector2(0.5f, 0f);
        frt.pivot = new Vector2(0.5f, 0f);
        frt.sizeDelta = new Vector2(frameWidth, frameWidth / aspect);
        frt.anchoredPosition = new Vector2(frameXOffset, frameBottomMargin);

        group = frameGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        GameObject lineGO = new GameObject("DialogueLine");
        lineGO.transform.SetParent(frameGO.transform, false);

        // 폰트가 비면 Text는 아무것도 안 그린다 - 창만 뜨고 글자가 없는 채로 조용히 실패한다
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        lineText = lineGO.AddComponent<Text>();
        lineText.font = font;
        lineText.text = "";
        lineText.fontSize = fontSize;
        lineText.color = textColor;
        lineText.alignment = TextAnchor.MiddleLeft;
        lineText.horizontalOverflow = HorizontalWrapMode.Wrap;
        lineText.verticalOverflow = VerticalWrapMode.Truncate;
        lineText.raycastTarget = false;
        // the panel scales with the screen, so let the line shrink with it
        // rather than spilling past the box on a long sentence
        lineText.resizeTextForBestFit = true;
        lineText.resizeTextMinSize = 10;
        lineText.resizeTextMaxSize = fontSize;
        StretchTo(lineText.rectTransform, textArea.xMin, textArea.yMin, textArea.xMax, textArea.yMax);

        // 화자 이름. 이름칸이 그림에 박힌 프레임(꿈탐정)은 빈 문자열을 넘겨 안 그린다.
        GameObject nameGO = new GameObject("SpeakerName");
        nameGO.transform.SetParent(frameGO.transform, false);
        speakerText = nameGO.AddComponent<Text>();
        speakerText.font = font;
        speakerText.text = "";
        speakerText.fontSize = speakerFontSize;
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.color = textColor;
        speakerText.alignment = TextAnchor.MiddleCenter;
        speakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        speakerText.raycastTarget = false;
        StretchTo(speakerText.rectTransform, speakerArea.xMin, speakerArea.yMin, speakerArea.xMax, speakerArea.yMax);

        GameObject promptGO = new GameObject("AdvancePrompt");
        promptGO.transform.SetParent(frameGO.transform, false);

        advancePrompt = promptGO.AddComponent<Text>();
        advancePrompt.font = font;
        advancePrompt.text = "▼";
        advancePrompt.fontSize = Mathf.Max(14, fontSize - 6);
        advancePrompt.alignment = TextAnchor.MiddleRight;
        advancePrompt.color = textColor;
        advancePrompt.raycastTarget = false;
        StretchTo(advancePrompt.rectTransform,
            textArea.xMax - 0.06f, textArea.yMin - 0.03f, textArea.xMax, textArea.yMin + 0.04f);
        advancePrompt.enabled = false;

        frameGO.SetActive(false);
    }

    private static void StretchTo(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public IEnumerator Show(Sprite frame, string line)
    {
        yield return Show(frame, line, "");
    }

    public IEnumerator Show(Sprite frame, string line, string speaker)
    {
        if (frameImage == null || frame == null) yield break;

        frameImage.sprite = frame;
        lineText.text = line;
        if (speakerText != null) speakerText.text = speaker ?? "";

        frameGO.SetActive(true);
        yield return Fade(0f, 1f);
        yield return WaitForAdvance();
        yield return Fade(1f, 0f);
        frameGO.SetActive(false);

        yield return new WaitForSeconds(lineGap);
    }

    // Reader-paced: Enter (or space / click) moves on, and the line times out on
    // its own if nobody touches anything.
    private IEnumerator WaitForAdvance()
    {
        float t = 0f;
        while (t < lineMinDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        advancePrompt.enabled = true;
        while (t < lineAutoAdvance)
        {
            if (AdvancePressed()) break;
            Color c = advancePrompt.color;
            c.a = Mathf.PingPong(Time.time * 1.6f, 1f) * 0.55f + 0.45f;
            advancePrompt.color = c;
            t += Time.deltaTime;
            yield return null;
        }
        advancePrompt.enabled = false;
    }

    public static bool AdvancePressed()
    {
        return Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (group == null) yield break;
        if (fadeDuration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        group.alpha = to;
    }

    public void Dispose()
    {
        if (canvasGO != null) Destroy(canvasGO);
        canvasGO = null;
        frameImage = null;
    }
}
