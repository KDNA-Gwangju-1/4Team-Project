using UnityEngine;
using UnityEngine.UI;

// The one line that tells the player a key will do something here. Built at
// runtime so nothing has to be wired per scene, and owned by whatever raised it
// so it cannot outlive the thing it is talking about.
public class InteractPrompt2D : MonoBehaviour
{
    public Font font;
    public int fontSize = 34;
    [Tooltip("Read against a dark purple arena, so it is a strong gold rather than a pale cream.")]
    public Color textColor = new Color(1f, 0.84f, 0.24f);
    public bool useChapterSkin = true;
    private Image skinBacking;
    [Tooltip("Hard outline. Without it the text disappears wherever the background happens to be light.")]
    public Color outlineColor = new Color(0.04f, 0.02f, 0.06f, 1f);
    public float outlineThickness = 2.2f;
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
        if (skinBacking != null)
        {
            Color backingColor = skinBacking.color;
            backingColor.a = shown * 0.92f;
            skinBacking.color = backingColor;
        }
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
        label.font = font != null ? font : HangulFont.Get();
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = message;

        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineThickness, -outlineThickness);
        outline.useGraphicAlpha = true;

        Shadow shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = true;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, screenHeight01);
        rt.anchorMax = new Vector2(1f, screenHeight01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = Vector2.zero;

        Color c = textColor;
        c.a = 0f;
        label.color = c;
        if (useChapterSkin)
        {
            HangulFont.Apply(label);
            label.lineSpacing = 1.15f;
            textColor = new Color(0.91f, 0.87f, 1f);
            var backingGO = new GameObject("ChapterPromptBacking", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backingGO.transform.SetParent(canvasGO.transform, false);
            backingGO.transform.SetAsFirstSibling();
            skinBacking = backingGO.GetComponent<Image>();
            skinBacking.raycastTarget = false;
            // same rounded card as the HUD; the fade drives its alpha
            ChapterHudStyle.SkinCard(skinBacking, false);
            skinBacking.color = new Color(1f, 1f, 1f, 0f);
            RectTransform backingRect = skinBacking.rectTransform;
            backingRect.anchorMin = backingRect.anchorMax = new Vector2(0.5f, screenHeight01);
            backingRect.sizeDelta = new Vector2(680f, 64f);
            backingRect.anchoredPosition = Vector2.zero;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, screenHeight01);
            rt.sizeDelta = new Vector2(632f, 56f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.resizeTextMaxSize = Mathf.Clamp(fontSize, 24, 28);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }
}
