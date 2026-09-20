using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A line of text across the middle of the screen, for the moments the fight needs
// to teach something: the boss is open, point the light at her.
// Built at runtime so nothing has to be wired per scene.
public class ScreenHint2D : MonoBehaviour
{
    public Font font;
    public int fontSize = 40;
    public Color textColor = new Color(1f, 0.92f, 0.65f);
    [Tooltip("Height on screen, 0 = bottom, 1 = top.")]
    [Range(0f, 1f)] public float screenHeight01 = 0.68f;
    public float fadeDuration = 0.25f;
    [Tooltip("Slow pulse so it reads as urgent without flashing.")]
    public float pulseSpeed = 2.2f;
    public float pulseDepth = 0.25f;

    private GameObject canvasGO;
    private Text label;
    private Coroutine running;

    private void Build()
    {
        if (canvasGO != null) return;

        canvasGO = new GameObject("ScreenHintCanvas");
        // parented to the owner so it dies with it. Left at the root, the boss's
        // "she is tired" line outlived the boss and sat over the ending cutscene.
        // A screen space overlay canvas ignores its transform, so this is free.
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textGO = new GameObject("HintText");
        textGO.transform.SetParent(canvasGO.transform, false);

        label = textGO.AddComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = textColor;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, screenHeight01);
        rt.anchorMax = new Vector2(1f, screenHeight01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 80f);
        rt.anchoredPosition = Vector2.zero;

        label.text = "";
    }

    public void Show(string message, float duration)
    {
        Build();
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(message, duration));
    }

    public void Hide()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        if (label != null) SetAlpha(0f);
    }

    // the owner can be torn down mid-message; nothing should be left on screen
    void OnDisable()
    {
        if (canvasGO != null) canvasGO.SetActive(false);
    }

    void OnEnable()
    {
        if (canvasGO != null) canvasGO.SetActive(true);
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        label.text = message;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        float held = 0f;
        float visible = Mathf.Max(0f, duration - fadeDuration * 2f);
        while (held < visible)
        {
            held += Time.deltaTime;
            SetAlpha(1f - Mathf.Abs(Mathf.Sin(held * pulseSpeed)) * pulseDepth);
            yield return null;
        }

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        running = null;
    }

    private void SetAlpha(float a)
    {
        if (label == null) return;
        Color c = textColor;
        c.a = a;
        label.color = c;
    }
}
