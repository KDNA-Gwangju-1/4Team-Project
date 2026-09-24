using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only: the existing dialogue owners still control input, fades and callbacks.</summary>
public static class ChapterDialogueSkin
{
    public enum Theme { Daily, BrightDream, BadDream }

    public static Sprite Load(Theme theme)
    {
        return Resources.Load<Sprite>("UI/ChapterSkins/" + theme);
    }

    public static Color Ink(Theme theme)
    {
        return theme == Theme.BrightDream
            ? new Color(0.16f, 0.23f, 0.30f)
            : new Color(0.96f, 0.97f, 1f);
    }

    public static void Place(RectTransform rect, Transform parent, float x0, float y0, float x1, float y1)
    {
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image ImageChild(string name, Transform parent)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            .GetComponent<Image>();
        Place(image.rectTransform, parent, 0f, 0f, 1f, 1f);
        image.raycastTarget = false;
        return image;
    }

    public static RectTransform Apply(RectTransform root, Text line, Graphic oldBackground,
        Graphic indicator, Theme theme, bool expandToParent = true)
    {
        Sprite sprite = Load(theme);
        if (root == null || line == null || sprite == null) return null;
        // All mutations happen only after the fallback checks above.
        if (oldBackground != null) oldBackground.enabled = false;
        if (expandToParent) Place(root, root.parent, 0f, 0f, 1f, 1f);

        var viewport = new GameObject("ChapterSkinViewport", typeof(RectTransform)).GetComponent<RectTransform>();
        // Full-screen panels: 76% instead of 96%. The 2D owner is already 88% wide.
        float inset = expandToParent ? 0.12f : 0.07f;
        Place(viewport, root, inset, 0.015f, 1f - inset, 0.84f);
        viewport.SetAsFirstSibling();
        Image art = ImageChild("ChapterSkinArt", viewport);
        art.sprite = sprite;
        art.preserveAspect = true;
        art.rectTransform.pivot = new Vector2(0.5f, 0f);
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = sprite.rect.width / sprite.rect.height;

        // Coordinates are relative to the complete supplied PNG, not its visible bottom strip.
        // Visible body spans roughly y=0.06..0.31: center at 0.185 with equal vertical padding.
        Place(line.rectTransform, art.transform, 0.18f, 0.10f, 0.86f, 0.27f);
        line.color = Ink(theme);
        line.alignment = TextAnchor.MiddleLeft;
        line.horizontalOverflow = HorizontalWrapMode.Wrap;
        line.verticalOverflow = VerticalWrapMode.Truncate;
        line.resizeTextForBestFit = true;
        line.fontSize = 34;
        line.resizeTextMinSize = 28;
        line.resizeTextMaxSize = 34;
        line.lineSpacing = 1.35f;
        line.fontStyle = FontStyle.Normal;
        line.raycastTarget = false;
        // Cream paper needs clean dark ink rather than the old white-text shadow.
        foreach (var effect in line.GetComponents<Shadow>()) effect.enabled = false;
        HangulFont.Apply(line);
        if (indicator != null)
        {
            Place(indicator.rectTransform, art.transform, 0.88f, 0.09f, 0.93f, 0.135f);
            indicator.color = Ink(theme);
            indicator.raycastTarget = false;
            if (indicator is Text prompt) HangulFont.Apply(prompt);
        }
        return art.rectTransform;
    }

    public static Text AddName(RectTransform art, Font font, Theme theme, string value)
    {
        var text = new GameObject("ChapterSpeaker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
            .GetComponent<Text>();
        Place(text.rectTransform, art, 0.115f, 0.308f, 0.255f, 0.355f);
        text.text = value;
        StyleName(text, theme);
        return text;
    }

    public static void StyleName(Text text, Theme theme)
    {
        if (text.GetComponent<ChapterSpeakerNameFit>() == null) text.gameObject.AddComponent<ChapterSpeakerNameFit>();
        text.font = HangulFont.GetEmphasis();
        text.fontStyle = FontStyle.Normal;
        text.fontSize = 30;
        text.resizeTextForBestFit = false;
        text.lineSpacing = 1f;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Ink(theme);
        text.raycastTarget = false;
    }

    public static void FitSpeakerName(Text text)
    {
        if (text == null || text.font == null) return;
        float available = Mathf.Max(1f, text.rectTransform.rect.width - 12f);
        var settings = text.GetGenerationSettings(Vector2.zero);
        settings.resizeTextForBestFit = false;
        for (int size = 30; size >= 14; size--)
        {
            settings.fontSize = size;
            float width = text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text, settings) / text.pixelsPerUnit;
            text.fontSize = size;
            if (width <= available) break;
        }
    }

    /// <summary>The detective bust with no panel baked in. Its bottom edge already fades out.</summary>
    public static Sprite CleanProtagonist()
    {
        var clean = Resources.Load<Sprite>("UI/ChapterSkins/DetectivePortrait");
        return clean != null ? clean : Load(Theme.Daily);
    }

    // The clean bust is dropped by this much (fraction of the art height) so its faded bottom
    // sits behind the panel's top border instead of ending in mid-air beside the name plate.
    private const float CleanPortraitDrop = 0.058f;
    // Old cutscene frames have their own panel painted under the portrait: cut just below the
    // new panel's top border and fade the cut so no hard edge shows.
    private const float LegacyPortraitCut = 0.30f;

    public static Image PreservePortrait(RectTransform art, Sprite original)
    {
        var portraitRoot = new GameObject("PortraitLayer", typeof(RectTransform)).GetComponent<RectTransform>();
        Place(portraitRoot, art.parent, 0f, 0f, 1f, 1f);
        portraitRoot.SetAsFirstSibling();
        portraitRoot.pivot = art.pivot;
        var fit = portraitRoot.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = art.GetComponent<AspectRatioFitter>().aspectRatio;
        var mask = new GameObject("OriginalPortrait", typeof(RectTransform), typeof(RectMask2D))
            .GetComponent<RectTransform>();
        mask.SetParent(portraitRoot, false);
        mask.SetAsFirstSibling();
        Image portrait = ImageChild("Portrait", mask);
        portrait.preserveAspect = true;
        LayoutPortrait(portrait, original);
        return portrait;
    }

    /// <summary>Seats either the clean bust or a legacy cutscene frame so the cut never shows.</summary>
    public static void LayoutPortrait(Image portrait, Sprite source)
    {
        if (portrait == null) return;
        portrait.sprite = source;
        var mask = portrait.rectTransform.parent as RectTransform;
        var clip = mask.GetComponent<RectMask2D>();
        if (source != null && source.name == "DetectivePortrait")
        {
            Place(mask, mask.parent, 0f, 0f, 1f, 1f);
            Place(portrait.rectTransform, mask, 0f, -CleanPortraitDrop, 1f, 1f - CleanPortraitDrop);
            clip.softness = Vector2Int.zero;
        }
        else
        {
            Place(mask, mask.parent, 0f, LegacyPortraitCut, 1f, 1f);
            Place(portrait.rectTransform, mask, 0f, -LegacyPortraitCut / (1f - LegacyPortraitCut), 1f, 1f);
            // fade the bottom cut over ~5% of the art height
            float h = ((RectTransform)mask.parent).rect.height;
            clip.softness = new Vector2Int(0, Mathf.Max(8, Mathf.RoundToInt(h * 0.05f)));
        }
    }

    public static void StylePrompt(Text name, Text action, Text key)
    {
        if (name != null) { name.font = HangulFont.GetEmphasis(); name.fontStyle = FontStyle.Normal; name.fontSize = 26; }
        if (action != null) { HangulFont.Apply(action); action.fontSize = 26; action.lineSpacing = 1.15f; }
        if (key != null) { key.font = HangulFont.GetEmphasis(); key.fontStyle = FontStyle.Normal; key.fontSize = 24; }
        if (name != null) name.color = new Color(0.87f, 0.95f, 1f);
        if (action != null) action.color = Color.white;
        if (key == null) return;
        key.color = new Color(0.65f, 0.9f, 1f);
        var badge = key.transform.parent.GetComponent<Image>();
        if (badge != null) badge.color = new Color(0.025f, 0.09f, 0.18f, 0.96f);
    }
}

/// <summary>One view-local conversation; no global state and no resets between individual lines.</summary>
public sealed class DialogueSkinSession
{
    private readonly Image art;
    private readonly Text name;
    private readonly Image portrait;
    private readonly Sprite playerFrame;
    private readonly Sprite otherFrame;
    private readonly Sprite protagonist;
    private readonly bool daily;
    private bool firstLine = true;

    public bool PortraitVisible => portrait != null && portrait.gameObject.activeSelf;

    public DialogueSkinSession(RectTransform artRect, ChapterDialogueSkin.Theme theme, Text nameText = null)
    {
        art = artRect.GetComponent<Image>();
        daily = theme == ChapterDialogueSkin.Theme.Daily;
        playerFrame = Resources.Load<Sprite>("UI/ChapterSkins/" + theme + "Player");
        otherFrame = Resources.Load<Sprite>("UI/ChapterSkins/" + theme + "Other");
        protagonist = ChapterDialogueSkin.CleanProtagonist();
        name = nameText != null ? nameText : ChapterDialogueSkin.AddName(artRect, null, theme, "");
        ChapterDialogueSkin.StyleName(name, theme);
        portrait = ChapterDialogueSkin.PreservePortrait(artRect, protagonist);
        if (daily)
        {
            // One clean blue frame with an empty name plate (DailyFrame.png) for every speaker;
            // the name is drawn as text inside the painted plate. The old approach clipped a
            // frame that still had "쌍둥이 동생의 의식" and the sister painted in, and covered the
            // plate with a flat box.
            Sprite frame = Resources.Load<Sprite>("UI/ChapterSkins/DailyFrame");
            if (frame != null)
            {
                playerFrame = otherFrame = frame;
                art.sprite = frame;
                ChapterDialogueSkin.Place(name.rectTransform, artRect, 0.092f, 0.292f, 0.270f, 0.348f);
            }
        }
        Begin();
    }

    public void Begin()
    {
        firstLine = true;
        portrait.gameObject.SetActive(false);
    }

    public void ShowLine(bool isPlayer, string speaker, Sprite originalPortrait = null)
    {
        Sprite frame = isPlayer ? playerFrame : otherFrame;
        if (frame != null) art.sprite = frame;
        // Named player artwork already contains its label; other artwork gets a real speaker label.
        name.enabled = daily || !isPlayer || playerFrame == null;
        name.text = isPlayer ? "꿈탐정" : (speaker ?? "");
        ChapterDialogueSkin.FitSpeakerName(name);
        // The detective always uses the clean bust, even when a cutscene hands over its old
        // combined frame; other speakers keep their own illustration.
        Sprite portraitSource = isPlayer ? protagonist : originalPortrait;
        ChapterDialogueSkin.LayoutPortrait(portrait, portraitSource);
        portrait.gameObject.SetActive(firstLine && portraitSource != null);
        firstLine = false;
    }
}
