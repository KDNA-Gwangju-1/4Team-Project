using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation for transient notices; the existing owner controls text and visibility.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Text))]
public sealed class ChapterNoticeStyle : MonoBehaviour
{
    private Text label;
    private Image backing;
    private Image accent;
    private Color panelColor;
    private Color accentColor;

    public static void Apply(Text text, ChapterDialogueSkin.Theme theme, float width = 440f,
        float height = 64f, int fontSize = 26)
    {
        if (text == null) return;
        var style = text.GetComponent<ChapterNoticeStyle>();
        if (style == null) style = text.gameObject.AddComponent<ChapterNoticeStyle>();
        style.Configure(text, theme, width, height, fontSize);
    }

    private void Configure(Text text, ChapterDialogueSkin.Theme theme, float width, float height, int size)
    {
        label = text;
        float alpha = label.color.a;
        label.font = HangulFont.GetEmphasis();
        label.fontStyle = FontStyle.Normal;
        label.fontSize = size;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(20, size - 4);
        label.resizeTextMaxSize = size;
        label.lineSpacing = 1.25f;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        Color ink = ChapterDialogueSkin.Ink(theme); ink.a = alpha; label.color = ink;
        foreach (var effect in label.GetComponents<Shadow>()) effect.enabled = false;

        bool bright = theme == ChapterDialogueSkin.Theme.BrightDream;
        panelColor = bright ? new Color(1f, .96f, .86f, .96f) : new Color(.055f, .035f, .11f, .94f);
        accentColor = bright ? new Color(.34f, .72f, .84f) : new Color(.55f, .42f, .88f);
        var rect = label.rectTransform;
        // Preserve the owner's screen position, but give letters symmetric safe padding.
        Vector2 center = (rect.anchorMin + rect.anchorMax) * .5f;
        rect.anchorMin = rect.anchorMax = center;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width - 48f, height - 16f);
        if (backing == null)
        {
            backing = new GameObject("NoticePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            backing.raycastTarget = false;
            var border = backing.gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1.5f, -1.5f);
            border.effectColor = accentColor;
            accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            accent.transform.SetParent(backing.transform, false);
            accent.raycastTarget = false;
            var ar = accent.rectTransform;
            ar.anchorMin = new Vector2(0f, .22f); ar.anchorMax = new Vector2(0f, .78f);
            ar.pivot = new Vector2(0f, .5f); ar.sizeDelta = new Vector2(3f, 0f);
            ar.anchoredPosition = new Vector2(10f, 0f);
        }
        var br = backing.rectTransform;
        br.SetParent(rect.parent, false);
        int textIndex = rect.GetSiblingIndex();
        br.SetSiblingIndex(textIndex - (br.GetSiblingIndex() < textIndex ? 1 : 0));
        br.anchorMin = br.anchorMax = center; br.pivot = rect.pivot;
        br.anchoredPosition = rect.anchoredPosition;
        br.localRotation = rect.localRotation; br.localScale = rect.localScale;
        br.sizeDelta = new Vector2(width, height);
        SyncVisibility();
    }

    private void LateUpdate() { SyncVisibility(); }
    private void OnEnable() { SyncVisibility(); }
    private void OnDisable() { if (backing != null) backing.gameObject.SetActive(false); }
    private void OnDestroy() { if (backing != null) Destroy(backing.gameObject); }

    private void SyncVisibility()
    {
        if (backing == null || label == null) return;
        bool visible = isActiveAndEnabled && label.enabled && !string.IsNullOrWhiteSpace(label.text) && label.color.a > .001f;
        if (backing.gameObject.activeSelf != visible) backing.gameObject.SetActive(visible);
        Color panel = panelColor; panel.a *= label.color.a; backing.color = panel;
        Color edge = accentColor; edge.a *= label.color.a; accent.color = edge;
    }
}
