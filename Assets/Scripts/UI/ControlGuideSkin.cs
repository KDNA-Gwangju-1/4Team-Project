using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses the daily chapter's frame and fonts; ControlGuideUI owns input and timing.</summary>
public static class ControlGuideSkin
{
    private static readonly Color Cyan = new Color(0.43f, 0.86f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.82f, 0.93f);

    public static Sprite Apply(RectTransform root)
    {
        var source = Resources.Load<Sprite>("UI/ChapterSkins/DailyFrame");
        if (root == null || source == null || root.Find("ChapterControlGuide") != null) return null;

        // Keep the serialized guide and its callback intact, with the original view as a fallback.
        for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);

        var dim = CreateImage("GuideDim", root, new Color(0.015f, 0.03f, 0.07f, 0.60f));
        ChapterDialogueSkin.Place(dim.rectTransform, root, 0f, 0f, 1f, 1f);

        var viewport = new GameObject("ChapterControlGuide", typeof(RectTransform)).GetComponent<RectTransform>();
        ChapterDialogueSkin.Place(viewport, root, 0.09f, 0.12f, 0.91f, 0.88f);
        var view = new GameObject("GuideFrameLayout", typeof(RectTransform)).GetComponent<RectTransform>();
        ChapterDialogueSkin.Place(view, viewport, 0f, 0f, 1f, 1f);
        var fit = view.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 2.65f;

        // DailyFrame has transparent portrait space above the panel. Slice only its frame strip,
        // keeping the painted corner details fixed while the centre expands for the guide.
        Rect rect = source.rect;
        var frame = Sprite.Create(source.texture,
            new Rect(rect.x, rect.y + rect.height * 0.045f, rect.width, rect.height * 0.325f),
            new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect,
            new Vector4(110f, 65f, 250f, 105f));
        frame.name = "ControlGuideDailyFrame";
        var background = CreateImage("ChapterFrame", view, Color.white);
        background.sprite = frame;
        background.type = Image.Type.Sliced;
        ChapterDialogueSkin.Place(background.rectTransform, view, 0f, 0f, 1f, 1f);

        Label("Title", view, "조작 안내", 30, Color.white, true, 0.075f, 0.90f, 0.255f, 0.99f);
        Label("Intro", view, "병실을 둘러보고, 아이에게 다가가 보세요.", 28, Color.white,
            false, 0.07f, 0.68f, 0.93f, 0.82f);

        Card(view, "Move", 0.07f, "W  A  S  D", "이동", "Shift를 누르면 빠르게 이동");
        Card(view, "Look", 0.365f, "마우스", "둘러보기", "마우스를 움직여 시점을 전환");
        Card(view, "Interact", 0.66f, "E", "상호작용", "안내가 나타나면 E 키 누르기");

        var rule = CreateImage("FooterRule", view, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
        ChapterDialogueSkin.Place(rule.rectTransform, view, 0.07f, 0.24f, 0.93f, 0.243f);
        Label("Continue", view, "아무 키나 눌러 시작", 26, Cyan, true,
            0.07f, 0.105f, 0.93f, 0.22f);
        return frame;
    }

    private static void Card(Transform parent, string name, float left, string key, string action, string detail)
    {
        var card = CreateImage(name, parent, new Color(0.015f, 0.055f, 0.14f, 0.72f));
        ChapterDialogueSkin.Place(card.rectTransform, parent, left, 0.30f, left + 0.27f, 0.66f);
        var accent = CreateImage("Accent", card.transform, Cyan);
        ChapterDialogueSkin.Place(accent.rectTransform, card.transform, 0.12f, 0.97f, 0.88f, 0.982f);
        Label("Key", card.transform, key, 32, Cyan, true, 0.05f, 0.59f, 0.95f, 0.90f);
        Label("Action", card.transform, action, 28, Color.white, true, 0.05f, 0.32f, 0.95f, 0.59f);
        Label("Detail", card.transform, detail, 20, Muted, false, 0.04f, 0.08f, 0.96f, 0.31f);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            .GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Label(string name, Transform parent, string value, int size, Color color,
        bool emphasis, float x0, float y0, float x1, float y1)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
            .GetComponent<Text>();
        ChapterDialogueSkin.Place(text.rectTransform, parent, x0, y0, x1, y1);
        text.text = value;
        text.font = emphasis ? HangulFont.GetEmphasis() : HangulFont.Get();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.RoundToInt(size * 0.75f);
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
    }
}
