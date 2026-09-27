using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 병원 · 챕터1 시작 조작 안내창을 그리는 공용 빌더.
///
///   [조작 안내]
///   301호 병실
///   병실을 둘러보고, 아이에게 다가가 보세요.
///   ─────────────────────────────
///   움직이기                 행동
///   [W][A][S][D]  이동        [E]  상호작용
///   [Shift]       달리기      ...
///   ─────────────────────────────
///            [Space] 키를 눌러 시작
///
/// 키캡·마우스 아이콘과 카드는 런타임에 그려서 임포트할 에셋이 없다.
/// 입력과 닫는 타이밍은 부르는 쪽(ControlGuideUI, BrightDreamControlGuide)이 가진다.
/// </summary>
public static class ControlGuideBuilder
{
    public enum Theme { Daily, BrightDream }

    /// <summary>안내 한 줄. keys 에 "WASD" 를 넣으면 방향키 묶음으로, mouse 는 "L" / "R" / "M"(움직이기).</summary>
    public struct Row
    {
        public string[] keys;
        public string mouse;
        public string action;
        public string detail;

        public Row(string action, string detail, params string[] keys)
        {
            this.keys = keys; mouse = null; this.action = action; this.detail = detail;
        }

        public static Row Mouse(string button, string action, string detail)
        {
            return new Row { keys = new string[0], mouse = button, action = action, detail = detail };
        }
    }

    public struct Content
    {
        public string title;
        public string subtitle;
        public string leftHeader;
        public Row[] left;
        public string rightHeader;
        public Row[] right;
        public string continueKey;
        public string continueText;
    }

    private struct Palette
    {
        public Color panel, edge, ink, accent, muted, section, capFace, capLip, capLine, capInk;
    }

    private static Palette Colors(Theme theme)
    {
        if (theme == Theme.Daily)
            return new Palette
            {
                panel = new Color(.035f, .075f, .15f, .96f),
                edge = new Color(.43f, .86f, 1f),
                ink = Color.white,
                accent = new Color(.43f, .86f, 1f),
                muted = new Color(.70f, .80f, .92f),
                section = new Color(1f, 1f, 1f, .045f),
                capFace = new Color(.94f, .97f, 1f),
                capLip = new Color(.50f, .68f, .84f),
                capLine = new Color(.02f, .07f, .15f),
                capInk = new Color(.05f, .13f, .25f),
            };
        return new Palette
        {
            panel = new Color(1f, .972f, .905f, .97f),
            edge = new Color(.44f, .72f, .88f),
            ink = new Color(.18f, .24f, .32f),
            accent = new Color(.24f, .53f, .69f),
            muted = new Color(.42f, .49f, .56f),
            section = new Color(.44f, .72f, .88f, .13f),
            capFace = Color.white,
            capLip = new Color(.60f, .77f, .88f),
            capLine = new Color(.22f, .40f, .54f),
            capInk = new Color(.16f, .26f, .36f),
        };
    }

    private const float PanelW = 1180f, ColW = 530f, RowH = 84f, CapH = 50f;
    private static float PanelH = 690f;

    /// <summary>안내창을 만든다. 돌려주는 CanvasGroup 은 하단 "시작" 줄 - Pulse() 로 깜빡이게 한다.</summary>
    public static CanvasGroup Build(RectTransform root, Theme theme, Content content)
    {
        Palette p = Colors(theme);
        // 줄 수에 맞춰 창 높이를 정한다: 머리글 222 + 칸 + 여백 30 + 하단 86
        int rows = Mathf.Max(content.left != null ? content.left.Length : 0, content.right != null ? content.right.Length : 0);
        PanelH = 222f + (44f + rows * RowH + 14f) + 30f + 86f;

        var dim = NewImage("GuideDim", root, new Color(.01f, .02f, .05f, theme == Theme.Daily ? .66f : .45f));
        Stretch(dim.rectTransform);

        var panel = NewImage("GuidePanel", root, Color.white);
        panel.sprite = RoundedCard(p.panel, p.edge, 20, 4f);
        panel.type = Image.Type.Sliced;
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(.5f, .5f);
        prt.sizeDelta = new Vector2(PanelW, PanelH);
        var shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, theme == Theme.Daily ? .5f : .25f);
        shadow.effectDistance = new Vector2(0f, -6f);
        Transform t = panel.transform;
        float top = PanelH * .5f;

        // 머리글: 작은 캡션 알약 + 제목 + 목표 한 줄
        var pill = NewImage("CaptionPill", t, Color.white);
        pill.sprite = RoundedCard(new Color(p.accent.r, p.accent.g, p.accent.b, theme == Theme.Daily ? .18f : .14f),
            new Color(p.accent.r, p.accent.g, p.accent.b, .55f), 14, 2f);
        pill.type = Image.Type.Sliced;
        Place(pill.rectTransform, 0f, top - 50f, 150f, 36f);
        Label("Caption", pill.transform, "조작 안내", 20, p.accent, true, Vector2.zero, new Vector2(150f, 36f), TextAnchor.MiddleCenter);

        Label("Title", t, content.title, 46, p.ink, true, new Vector2(0f, top - 106f), new Vector2(PanelW - 120f, 60f), TextAnchor.MiddleCenter);
        Label("Subtitle", t, content.subtitle, 24, p.muted, false, new Vector2(0f, top - 156f), new Vector2(PanelW - 120f, 36f), TextAnchor.MiddleCenter);
        Rule(t, p, top - 192f);

        // 두 칸: 움직이기 / 행동
        float colTop = top - 222f;
        Column(t, p, theme, -PanelW * .25f + 8f, colTop, content.leftHeader, content.left, rows);
        Column(t, p, theme, PanelW * .25f - 8f, colTop, content.rightHeader, content.right, rows);

        // 하단: [Space] 키를 눌러 시작
        Rule(t, p, -top + 86f);
        var footer = new GameObject("Continue", typeof(RectTransform), typeof(CanvasGroup));
        footer.transform.SetParent(t, false);
        var frt = (RectTransform)footer.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(.5f, .5f);
        frt.anchoredPosition = new Vector2(0f, -top + 46f);
        frt.sizeDelta = new Vector2(PanelW, 50f);
        float capW = CapWidth(content.continueKey, 44f);
        var textSize = 26;
        float textW = EstimateWidth(content.continueText, textSize);
        float total = capW + 14f + textW;
        Keycap(footer.transform, p, content.continueKey, new Vector2(-total * .5f + capW * .5f, 0f), capW, 44f, 22);
        Label("ContinueText", footer.transform, content.continueText, textSize, p.accent, true,
            new Vector2(-total * .5f + capW + 14f + textW * .5f, 0f), new Vector2(textW + 20f, 44f), TextAnchor.MiddleCenter);
        return footer.GetComponent<CanvasGroup>();
    }

    /// <summary>하단 "시작" 줄을 천천히 깜빡인다. 매 프레임 부른다 (timeScale 과 무관).</summary>
    public static void Pulse(CanvasGroup footer)
    {
        if (footer == null) return;
        footer.alpha = .55f + .45f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 3.2f));
    }

    // ------------------------------------------------------------
    // 칸과 줄
    // ------------------------------------------------------------
    private static void Column(Transform parent, Palette p, Theme theme, float centerX, float top, string header, Row[] rows, int slots)
    {
        if (rows == null) rows = new Row[0];
        // 두 칸은 줄 수가 달라도 같은 높이로 맞춘다
        float height = 44f + Mathf.Max(slots, rows.Length) * RowH + 14f;
        var box = NewImage("Section", parent, Color.white);
        box.sprite = RoundedCard(p.section, new Color(p.edge.r, p.edge.g, p.edge.b, theme == Theme.Daily ? .22f : .35f), 14, 2f);
        box.type = Image.Type.Sliced;
        Place(box.rectTransform, centerX, top - height * .5f, ColW, height);
        Transform t = box.transform;
        float localTop = height * .5f;

        var bar = NewImage("HeaderBar", t, p.accent);
        Place(bar.rectTransform, -ColW * .5f + 26f, localTop - 26f, 5f, 22f);
        Label("Header", t, header, 22, p.accent, true, new Vector2(-ColW * .5f + 38f + 150f, localTop - 26f), new Vector2(300f, 30f), TextAnchor.MiddleLeft);

        for (int i = 0; i < rows.Length; i++)
        {
            float y = localTop - 44f - RowH * (i + .5f);
            DrawRow(t, p, rows[i], y);
        }
    }

    private static void DrawRow(Transform parent, Palette p, Row row, float y)
    {
        const float iconArea = 196f;
        float left = -ColW * .5f + 22f;

        // 아이콘들을 왼쪽 칸 안에서 가운데 정렬
        var widths = new List<float>();
        if (row.keys != null)
            foreach (string key in row.keys) widths.Add(key == "WASD" ? 3 * 38f + 2 * 5f : CapWidth(key, CapH));
        if (!string.IsNullOrEmpty(row.mouse)) widths.Add(46f);
        float iconsW = 0f; foreach (float w in widths) iconsW += w;
        iconsW += Mathf.Max(0, widths.Count - 1) * 8f;
        float x = left + (iconArea - iconsW) * .5f;
        int wi = 0;
        if (row.keys != null)
            foreach (string key in row.keys)
            {
                float w = widths[wi++];
                if (key == "WASD") Wasd(parent, p, new Vector2(x + w * .5f, y));
                else Keycap(parent, p, key, new Vector2(x + w * .5f, y), w, CapH, key.Length > 1 ? 18 : 24);
                x += w + 8f;
            }
        if (!string.IsNullOrEmpty(row.mouse)) MouseIcon(parent, p, row.mouse, new Vector2(x + 23f, y));

        float textX = left + iconArea + 16f;
        float textW = ColW * .5f - 18f - textX;
        Label("Action", parent, row.action, 26, p.ink, true, new Vector2(textX + textW * .5f, y + 14f), new Vector2(textW, 34f), TextAnchor.MiddleLeft);
        Label("Detail", parent, row.detail, 19, p.muted, false, new Vector2(textX + textW * .5f, y - 16f), new Vector2(textW, 28f), TextAnchor.MiddleLeft);
    }

    // ------------------------------------------------------------
    // 키캡 · 마우스
    // ------------------------------------------------------------
    private static float CapWidth(string key, float h)
    {
        if (string.IsNullOrEmpty(key)) return h;
        if (key == "Space") return h * 2.8f;
        if (key.Length > 1) return h * .55f + key.Length * h * .26f;
        return h;
    }

    private static void Wasd(Transform parent, Palette p, Vector2 center)
    {
        const float s = 38f, gap = 5f;
        Keycap(parent, p, "W", center + new Vector2(0f, s * .5f + gap * .5f), s, s, 18);
        Keycap(parent, p, "A", center + new Vector2(-s - gap, -s * .5f - gap * .5f), s, s, 18);
        Keycap(parent, p, "S", center + new Vector2(0f, -s * .5f - gap * .5f), s, s, 18);
        Keycap(parent, p, "D", center + new Vector2(s + gap, -s * .5f - gap * .5f), s, s, 18);
    }

    private static void Keycap(Transform parent, Palette p, string key, Vector2 pos, float w, float h, int fontSize)
    {
        var cap = NewImage("Key_" + key, parent, Color.white);
        cap.sprite = KeycapSprite(p);
        cap.type = Image.Type.Sliced;
        Place(cap.rectTransform, pos.x, pos.y, w, h);
        // 아랫쪽 두께(입술)만큼 글자를 살짝 올린다
        Label("Glyph", cap.transform, key, fontSize, p.capInk, true, new Vector2(0f, h * .06f), new Vector2(w, h), TextAnchor.MiddleCenter);
    }

    private static void MouseIcon(Transform parent, Palette p, string button, Vector2 pos)
    {
        var icon = NewImage("Mouse_" + button, parent, Color.white);
        icon.sprite = MouseSprite(p, button);
        icon.preserveAspect = true;
        Place(icon.rectTransform, pos.x, pos.y, 46f, 66f);
    }

    // ------------------------------------------------------------
    // 런타임 스프라이트 (테마·종류별로 한 번씩만 만든다)
    // ------------------------------------------------------------
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    private static string Key(string kind, params Color[] colors)
    {
        string k = kind;
        foreach (var c in colors) k += "|" + ColorUtility.ToHtmlStringRGBA(c);
        return k;
    }

    /// <summary>둥근 카드: 채움 + 테두리. 9-slice.</summary>
    private static Sprite RoundedCard(Color fill, Color edge, int radius, float edgeWidth)
    {
        string k = Key("card" + radius + "_" + edgeWidth, fill, edge);
        if (cache.TryGetValue(k, out var cached) && cached != null) return cached;
        int size = radius * 2 + 8;
        var tex = NewTex(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundedDist(x, y, size, size, radius);
                Color c = d > -edgeWidth ? edge : fill;
                c.a *= Mathf.Clamp01(.5f - d);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return cache[k] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    /// <summary>키캡: 윗면 + 아래쪽 두께 + 외곽선. 9-slice.</summary>
    private static Sprite KeycapSprite(Palette p)
    {
        string k = Key("cap", p.capFace, p.capLip, p.capLine);
        if (cache.TryGetValue(k, out var cached) && cached != null) return cached;
        const int size = 48, radius = 10, lip = 6;
        var tex = NewTex(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float outer = RoundedDist(x, y, size, size, radius);
                // 윗면은 아래쪽 lip 만큼 위로 올라간 둥근 사각형
                float face = RoundedDist(x, y - lip, size, size - lip, radius - 1) + 2.5f;
                Color c = outer > -2f ? p.capLine : face < 0f ? p.capFace : p.capLip;
                // 윗면 위쪽에 얇은 하이라이트
                if (face < 0f && face > -2f && y > size * .6f) c = Color.Lerp(c, Color.white, .5f);
                c.a *= Mathf.Clamp01(.5f - outer);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return cache[k] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius + 2, radius + lip + 2, radius + 2, radius + 2));
    }

    /// <summary>마우스: 몸통, 좌우 버튼 경계, 휠. L/R 은 해당 버튼을 강조색으로, M 은 양옆에 움직임 화살표.</summary>
    private static Sprite MouseSprite(Palette p, string button)
    {
        string k = Key("mouse" + button, p.capFace, p.capLine, p.accent);
        if (cache.TryGetValue(k, out var cached) && cached != null) return cached;
        const int w = 80, h = 116, bodyL = 14, bodyR = 66, bodyB = 4, bodyT = 112;
        var tex = NewTex(w, h);
        float splitY = bodyT - 44;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = new Color(0, 0, 0, 0);
                float d = RoundedDist(x - bodyL, y - bodyB, bodyR - bodyL, bodyT - bodyB, 24);
                if (d < .5f)
                {
                    bool upper = y > splitY;
                    bool left = x < w / 2;
                    c = p.capFace;
                    if (upper && ((button == "L" && left) || (button == "R" && !left))) c = p.accent;
                    if (d > -3f) c = p.capLine;
                    else if (Mathf.Abs(y - splitY) < 1.5f) c = p.capLine;
                    else if (upper && Mathf.Abs(x - w / 2f + .5f) < 1.5f) c = p.capLine;
                    // 휠
                    if (Mathf.Abs(x - w / 2f + .5f) < 4.5f && y > splitY + 12 && y < splitY + 30)
                        c = Mathf.Abs(x - w / 2f + .5f) < 2.5f ? p.capLip : p.capLine;
                    c.a *= Mathf.Clamp01(.5f - d);
                }
                // 움직이기: 양옆 작은 삼각형 화살표
                if (button == "M")
                {
                    int cy = h / 2;
                    int dy = Mathf.Abs(y - cy);
                    if ((x < 11 && x >= 2 && dy <= (x - 2) * .8f) || (x > w - 12 && x <= w - 3 && dy <= (w - 3 - x) * .8f))
                        c = p.accent;
                }
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return cache[k] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
    }

    // signed distance to a rounded rectangle occupying [0,w) x [0,h); < 0 inside
    private static float RoundedDist(float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(x + .5f - w * .5f) - (w * .5f - r);
        float qy = Mathf.Abs(y + .5f - h * .5f) - (h * .5f - r);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outside - r;
    }

    private static Texture2D NewTex(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
    }

    // ------------------------------------------------------------
    // UI 헬퍼
    // ------------------------------------------------------------
    private static void Rule(Transform parent, Palette p, float y)
    {
        var rule = NewImage("Rule", parent, new Color(p.edge.r, p.edge.g, p.edge.b, .35f));
        Place(rule.rectTransform, 0f, y, PanelW - 100f, 2f);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static Text Label(string name, Transform parent, string value, int size, Color color, bool emphasis,
        Vector2 pos, Vector2 box, TextAnchor anchor)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        Place(text.rectTransform, pos.x, pos.y, box.x, box.y);
        text.text = value;
        text.font = emphasis ? HangulFont.GetEmphasis() : HangulFont.Get();
        text.fontStyle = FontStyle.Normal;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.RoundToInt(size * .7f);
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        return text;
    }

    // 한글 한 글자 ≈ 글자 크기, 영문·공백 ≈ 절반
    private static float EstimateWidth(string s, int size)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        float w = 0f;
        foreach (char ch in s) w += ch > 0x2000 ? size * .98f : size * .5f;
        return w;
    }
}
