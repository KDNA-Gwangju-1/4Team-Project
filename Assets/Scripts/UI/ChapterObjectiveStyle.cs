using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns a quest / objective Text into a card in the top-right column.
/// Cards stack by slot, and only visible ones take space, so whichever objectives are showing
/// (clues, then purify count, then boss + weak-point gauge) line up without overlapping.
/// The owner keeps writing the text and toggling the GameObject; this only styles and places it.
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Text))]
public sealed class ChapterObjectiveStyle : MonoBehaviour
{
    private const float Padding = 18f;

    private static readonly List<ChapterObjectiveStyle> all = new List<ChapterObjectiveStyle>();

    private Text label;
    private Image card;
    private Image accent;
    private bool bright;
    private int slot;
    private float width;

    public static void Apply(Text text, ChapterDialogueSkin.Theme theme, int slot, float width = ChapterHudStyle.CardWidth)
    {
        if (text == null) return;
        var style = text.GetComponent<ChapterObjectiveStyle>();
        if (style == null) style = text.gameObject.AddComponent<ChapterObjectiveStyle>();
        style.Configure(text, theme == ChapterDialogueSkin.Theme.BrightDream, slot, width);
    }

    private void Configure(Text text, bool brightTheme, int cardSlot, float cardWidth)
    {
        label = text;
        bright = brightTheme;
        slot = cardSlot;
        width = cardWidth;

        label.font = HangulFont.Get();
        label.fontStyle = FontStyle.Normal;
        label.fontSize = 24;
        label.supportRichText = true;
        label.resizeTextForBestFit = false;
        label.lineSpacing = 1.3f;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.color = ChapterHudStyle.Ink(bright);
        foreach (var effect in label.GetComponents<Shadow>()) effect.enabled = false;
        var notice = label.GetComponent<ChapterNoticeStyle>();
        if (notice != null) Destroy(notice);

        if (card == null)
        {
            card = new GameObject(name + "_Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            card.raycastTarget = false;
            accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            accent.transform.SetParent(card.transform, false);
            accent.raycastTarget = false;
            var ar = accent.rectTransform;
            ar.anchorMin = new Vector2(0f, 0f); ar.anchorMax = new Vector2(0f, 1f);
            ar.pivot = new Vector2(0f, .5f);
            ar.offsetMin = new Vector2(9f, 14f); ar.offsetMax = new Vector2(13f, -14f);
        }
        ChapterHudStyle.SkinCard(card, bright);
        accent.color = ChapterHudStyle.Accent(bright);

        var cr = card.rectTransform;
        cr.SetParent(label.rectTransform.parent, false);
        cr.SetSiblingIndex(label.rectTransform.GetSiblingIndex());
        ChapterHudStyle.TopRight(cr, ChapterHudStyle.Margin);

        var lr = label.rectTransform;
        lr.localRotation = Quaternion.identity; lr.localScale = Vector3.one;
        ChapterHudStyle.TopRight(lr, ChapterHudStyle.Margin);
        // preferredHeight wraps against the current width, so set the width first
        lr.sizeDelta = new Vector2(width - Padding * 2f - 10f, 100f);
        // a label that starts switched off never gets OnEnable/OnDisable, so hide its card now
        card.gameObject.SetActive(Visible);
        Layout();
    }

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
        Layout();
    }

    private void OnDisable()
    {
        all.Remove(this);
        if (card != null) card.gameObject.SetActive(false);
        RelayoutAll();
    }

    private void OnDestroy()
    {
        all.Remove(this);
        if (card != null) Destroy(card.gameObject);
    }

    private void LateUpdate()
    {
        // text can change every frame (counts), so keep the column in shape
        RelayoutAll();
    }

    private bool Visible => label != null && isActiveAndEnabled && label.enabled && !string.IsNullOrWhiteSpace(label.text);

    private float Height => label.preferredHeight + Padding * 2f;

    private static void RelayoutAll()
    {
        all.Sort((a, b) => a.slot.CompareTo(b.slot));
        float y = ChapterHudStyle.Margin;
        foreach (var s in all)
        {
            if (s.label == null || s.card == null) continue;
            bool visible = s.Visible;
            if (s.card.gameObject.activeSelf != visible) s.card.gameObject.SetActive(visible);
            if (!visible) continue;
            s.Place(y);
            y += s.Height + ChapterHudStyle.Gap;
        }
    }

    private void Layout()
    {
        if (label == null || card == null) return;
        RelayoutAll();
    }

    private void Place(float y)
    {
        float textWidth = width - Padding * 2f - 10f;
        var lr = label.rectTransform;
        lr.sizeDelta = new Vector2(textWidth, lr.sizeDelta.y);
        lr.sizeDelta = new Vector2(textWidth, label.preferredHeight);
        lr.anchoredPosition = new Vector2(-ChapterHudStyle.Margin - Padding, -y - Padding);
        var cr = card.rectTransform;
        cr.sizeDelta = new Vector2(width, Height);
        cr.anchoredPosition = new Vector2(-ChapterHudStyle.Margin, -y);
        Color c = Color.white; c.a = label.color.a; card.color = c;
    }
}
