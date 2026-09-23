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
        line.fontSize = 30;
        line.resizeTextMinSize = 26;
        line.resizeTextMaxSize = 30;
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
        Place(text.rectTransform, art, 0.105f, 0.306f, 0.25f, 0.363f);
        text.text = value;
        StyleName(text, theme);
        return text;
    }

    public static void StyleName(Text text, Theme theme)
    {
        text.font = HangulFont.GetEmphasis();
        text.fontStyle = FontStyle.Normal;
        text.fontSize = 26;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 22;
        text.resizeTextMaxSize = 26;
        text.lineSpacing = 1f;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Ink(theme);
        text.raycastTarget = false;
    }

    public static Image PreservePortrait(RectTransform art, Sprite original)
    {
        var portraitRoot = new GameObject("PortraitLayer", typeof(RectTransform)).GetComponent<RectTransform>();
        Place(portraitRoot, art.parent, 0f, 0f, 1f, 1f);
        portraitRoot.SetAsFirstSibling();
        portraitRoot.pivot = art.pivot;
        var fit = portraitRoot.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = art.GetComponent<AspectRatioFitter>().aspectRatio;
        // Clip away only the old bottom UI, keeping the original per-speaker illustration.
        var mask = new GameObject("OriginalPortrait", typeof(RectTransform), typeof(RectMask2D))
            .GetComponent<RectTransform>();
        // Seat the cropped bust on the new panel's top edge, avoiding a floating cut edge.
        Place(mask, portraitRoot, 0f, 0.32f, 1f, 0.915f);
        mask.SetAsFirstSibling();
        Image portrait = ImageChild("Portrait", mask);
        Place(portrait.rectTransform, mask, 0f, -0.405f / 0.595f, 1f, 1f);
        portrait.sprite = original;
        portrait.preserveAspect = true;
        return portrait;
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
        protagonist = ChapterDialogueSkin.Load(ChapterDialogueSkin.Theme.Daily);
        name = nameText != null ? nameText : ChapterDialogueSkin.AddName(artRect, null, theme, "");
        ChapterDialogueSkin.StyleName(name, theme);
        portrait = ChapterDialogueSkin.PreservePortrait(artRect, protagonist);
        if (daily)
        {
            // Existing clean blue lower panel, clipped in UI; no portrait pixels or baked name remain.
            Sprite cleanBlue = Resources.Load<Sprite>("UI/ChapterSkins/DailyPanel");
            if (cleanBlue != null)
            {
                art.enabled = false;
                var clip = new GameObject("DailyPanelClip", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                ChapterDialogueSkin.Place(clip, artRect, 0f, 0f, 1f, 0.31f);
                clip.SetAsFirstSibling();
                var panel = new GameObject("BlueFrame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ChapterDialogueSkin.Place(panel.rectTransform, clip, 0f, 0f, 1f, 1f / 0.31f);
                panel.sprite = cleanBlue;
                panel.raycastTarget = false;
                var badge = new GameObject("DailyNamePlate", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<Image>();
                ChapterDialogueSkin.Place(badge.rectTransform, artRect, 0.085f, 0.302f, 0.265f, 0.371f);
                badge.color = new Color(0.025f, 0.085f, 0.18f);
                badge.raycastTarget = false;
                badge.GetComponent<Outline>().effectColor = new Color(0.3f, 0.75f, 1f);
                badge.GetComponent<Outline>().effectDistance = new Vector2(1f, -1f);
                name.transform.SetAsLastSibling();
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
        Sprite portraitSource = originalPortrait != null ? originalPortrait : (isPlayer ? protagonist : null);
        portrait.sprite = portraitSource;
        portrait.gameObject.SetActive(firstLine && portraitSource != null);
        firstLine = false;
    }
}
