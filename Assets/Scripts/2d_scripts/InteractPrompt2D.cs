using UnityEngine;
using UnityEngine.UI;

// The one line that tells the player a key will do something here. Built at
// runtime so nothing has to be wired per scene, and owned by whatever raised it
// so it cannot outlive the thing it is talking about.
public class InteractPrompt2D : MonoBehaviour
{
    public Font font;
    public int fontSize = 30;
    public Color textColor = new Color(1f, 0.95f, 0.75f);
    [Tooltip("Height on screen, 0 = bottom, 1 = top.")]
    [Range(0f, 1f)] public float screenHeight01 = 0.32f;
    public float fadeDuration = 0.15f;
    [Tooltip("Slow pulse, so it reads as something to act on rather than a label.")]
    public float pulseSpeed = 3f;
    public float pulseDepth = 0.2f;

    private GameObject canvasGO;
    private Text label;
    private float alpha;

    void OnDisable()
    {
        if (canvasGO != null) canvasGO.SetActive(false);
    }

    void OnEnable()
    {
        if (canvasGO != null) canvasGO.SetActive(true);
    }

    void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
    }

    // Call every frame with whether the prompt should be up; it handles the fade.
    public void SetVisible(bool visible, string message)
    {
        if (visible) Build(message);
        if (label == null) return;

        if (visible && label.text != message) label.text = message;

        alpha = (fadeDuration <= 0.01f)
            ? (visible ? 1f : 0f)
            : Mathf.MoveTowards(alpha, visible ? 1f : 0f, Time.deltaTime / fadeDuration);

        float shown = visible
            ? alpha * (1f - Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseDepth)
            : alpha;

        Color c = textColor;
        c.a = shown;
        label.color = c;
        label.enabled = shown > 0.01f;
    }

    private void Build(string message)
    {
        if (canvasGO != null) return;

        canvasGO = new GameObject("InteractPromptCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(canvasGO.transform, false);

        label = textGO.AddComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = message;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, screenHeight01);
        rt.anchorMax = new Vector2(1f, screenHeight01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = Vector2.zero;

        Color c = textColor;
        c.a = 0f;
        label.color = c;
    }
}
