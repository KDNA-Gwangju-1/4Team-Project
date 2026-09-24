using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared HUD layout and look for every chapter; resource values remain owned by gameplay.
///
/// Layout (1920x1080 reference, all scenes):
///   top-left   player resources - 체력, then (2D) 손전등, 대시, stacked with the same gap
///   top-centre timer
///   top-right  objectives / quest cards (ChapterObjectiveStyle), stacked automatically
/// </summary>
public static class ChapterHudStyle
{
    public const float Margin = 24f;
    public const float Gap = 12f;
    public const float CardWidth = 300f;

    // Hearts: identical in 3D and 2D.
    public const int HeartCount = 5;
    public const float HeartSize = 46f;
    public const float HeartStep = 52f;
    public const float HeartTop = 30f;          // below the caption
    public const float HeartCardHeight = 90f;
    public const float GaugeCardHeight = 56f;
    public static float HeartLeft => (CardWidth - ((HeartCount - 1) * HeartStep + HeartSize)) * 0.5f;

    /// <summary>Top of the n-th card in the left column (0 = 체력, 1 = 손전등, 2 = 대시).</summary>
    public static float LeftColumnY(int index)
    {
        float y = Margin;
        if (index >= 1) y += HeartCardHeight + Gap;
        if (index >= 2) y += GaugeCardHeight + Gap;
        return y;
    }

    public static Color Accent(bool bright) => bright ? new Color(.36f, .68f, .86f) : new Color(.58f, .45f, .92f);
    public static Color Ink(bool bright) => bright ? new Color(.16f, .23f, .30f) : new Color(.95f, .94f, 1f);
    public static Color CaptionInk(bool bright) => bright ? new Color(.25f, .42f, .52f) : new Color(.78f, .74f, .95f);

    private static Sprite brightCard, darkCard;

    /// <summary>
    /// Rounded 9-sliced card: soft fill, a coloured felt/glow border and a thin inner line,
    /// generated once at runtime so it needs no imported asset.
    /// </summary>
    public static Sprite Card(bool bright)
    {
        Sprite cached = bright ? brightCard : darkCard;
        if (cached != null) return cached;

        const int size = 64, radius = 16, border = 20;
        Color fill = bright ? new Color(1f, .972f, .905f, .96f) : new Color(.06f, .045f, .12f, .93f);
        Color edge = bright ? new Color(.44f, .72f, .88f, 1f) : new Color(.50f, .38f, .84f, 1f);
        Color inner = bright ? new Color(1f, 1f, 1f, .9f) : new Color(.45f, .85f, .95f, .38f);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // signed distance to a rounded square filling the texture
            float qx = Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius);
            float qy = Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
            float d = outside - radius;                                  // <0 inside
            Color c = fill;
            if (d > -3.5f) c = edge;                                     // outer border
            float innerLine = Mathf.Abs(d + 6.5f);
            if (innerLine < 1f) c = Color.Lerp(c, inner, 1f - innerLine);
            c.a *= Mathf.Clamp01(.5f - d);                               // anti-aliased edge
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.name = bright ? "HudCardBright" : "HudCardDark";
        if (bright) brightCard = sprite; else darkCard = sprite;
        return sprite;
    }

    /// <summary>Turns an Image into a themed card with a soft drop shadow.</summary>
    public static void SkinCard(Image image, bool bright)
    {
        if (image == null) return;
        image.sprite = Card(bright);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = Color.white;
        foreach (var old in image.GetComponents<Outline>()) Object.Destroy(old);
        var shadow = image.GetComponent<Shadow>();
        if (shadow == null || shadow is Outline) shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = bright ? new Color(.20f, .30f, .40f, .28f) : new Color(0f, 0f, 0f, .55f);
        shadow.effectDistance = new Vector2(0f, -3f);
    }

    public static void Frame(RectTransform root, bool bright, float width, float height, string caption)
    {
        if (root == null) return;
        root.sizeDelta = new Vector2(width, height);
        Transform found = root.Find("HudCard");
        Image panel = found != null ? found.GetComponent<Image>() :
            new GameObject("HudCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        if (found == null)
        {
            ChapterDialogueSkin.Place(panel.rectTransform, root, 0, 0, 1, 1);
            panel.transform.SetAsFirstSibling(); panel.raycastTarget = false;
        }
        SkinCard(panel, bright);
        Text title;
        var existing = root.Find("HudCaption");
        if (existing == null)
        {
            title = new GameObject("HudCaption",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(root,false);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0,1);
            title.rectTransform.pivot = new Vector2(0,1);
            title.alignment = TextAnchor.MiddleLeft; title.raycastTarget = false;
        }
        else title = existing.GetComponent<Text>();
        title.rectTransform.anchoredPosition = new Vector2(16,-7);
        title.rectTransform.sizeDelta = new Vector2(width-32,22);
        title.font = HangulFont.GetEmphasis(); title.fontStyle = FontStyle.Normal; title.fontSize = 18;
        title.text = caption;
        title.color = CaptionInk(bright);
    }

    public static void TopLeft(RectTransform root, float y)
    {
        root.anchorMin = root.anchorMax = new Vector2(0,1);
        root.pivot = new Vector2(0,1);
        root.anchoredPosition = new Vector2(Margin,-y);
    }

    public static void TopRight(RectTransform root, float y)
    {
        root.anchorMin = root.anchorMax = new Vector2(1,1);
        root.pivot = new Vector2(1,1);
        root.anchoredPosition = new Vector2(-Margin,-y);
    }
}
