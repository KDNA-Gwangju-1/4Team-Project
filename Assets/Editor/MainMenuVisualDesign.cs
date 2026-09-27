using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuVisualDesign
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    private static readonly Color Brass = new Color(.73f, .56f, .36f, 1f);
    private static readonly Color Paper = new Color(.96f, .90f, .78f, 1f);
    private static readonly Color Ink = new Color(.07f, .075f, .12f, .96f);
    private static readonly Color InkSoft = new Color(.11f, .11f, .17f, .94f);

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }

        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Stretch(Transform parent, string name)
    {
        var rect = Rect(parent, name, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Fill(RectTransform rect, Color color)
    {
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void Border(RectTransform parent, Vector2 size, float inset)
    {
        Fill(Rect(parent, "TopRule", new Vector2(0, size.y / 2 - inset), new Vector2(size.x - inset * 2, 2)), Brass);
        Fill(Rect(parent, "BottomRule", new Vector2(0, -size.y / 2 + inset), new Vector2(size.x - inset * 2, 2)), Brass);
        Fill(Rect(parent, "LeftRule", new Vector2(-size.x / 2 + inset, 0), new Vector2(2, size.y - inset * 2)), Brass);
        Fill(Rect(parent, "RightRule", new Vector2(size.x / 2 - inset, 0), new Vector2(2, size.y - inset * 2)), Brass);
    }

    private static Text Label(Transform parent, string name, string value, Vector2 position,
                              Vector2 size, int fontSize, Font font, Color color,
                              TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var rect = Rect(parent, name, position, size);
        var text = rect.GetComponent<Text>();
        if (text == null) text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void AddTextOutline(Text text, Color color)
    {
        var outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    public static void Apply(RectTransform panel)
    {
        var names = new[] { "StartButton", "OptionButton", "ExitButton" };
        var labels = new[] { "START GAME", "SETTINGS", "EXIT" };
        Font font = null;

        for (int i = 0; i < names.Length; i++)
        {
            var rect = Rect(panel, names[i], new Vector2(0, -155 - i * 96), new Vector2(400, 76));
            var button = rect.GetComponent<Button>();
            if (button == null) continue;

            var image = rect.GetComponent<Image>();
            image.sprite = null;
            image.color = Color.white;
            image.raycastTarget = true;

            var colors = button.colors;
            colors.normalColor = new Color(.12f, .10f, .08f, .96f);
            colors.highlightedColor = new Color(.28f, .21f, .13f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.39f, .29f, .16f, 1f);
            colors.disabledColor = new Color(.13f, .13f, .13f, .6f);
            colors.fadeDuration = .12f;
            button.colors = colors;

            var text = button.GetComponentInChildren<Text>();
            font = text.font;
            text.text = labels[i];
            text.color = Paper;
            text.fontSize = 26;
            text.fontStyle = FontStyle.Normal;
            Border(rect, new Vector2(400, 76), 5);
            Label(rect, "Index", "0" + (i + 1), new Vector2(-164, 0), new Vector2(42, 42), 16, font, Brass);
            Label(rect, "Arrow", ">", new Vector2(164, 0), new Vector2(32, 42), 22, font, Brass);
        }

        var box = Rect(panel, "GameTitleBox", new Vector2(0, 260), new Vector2(650, 218));
        var background = panel.parent.Find("Background")?.GetComponent<Image>();
        box.gameObject.SetActive(!MainMenuLobbyArt.HasReDreamArtwork(background));
        Fill(box, new Color(.10f, .085f, .065f, .88f));
        Border(box, new Vector2(650, 218), 10);
        Label(box, "Eyebrow", "D E T E C T I V E   A R C H I V E", new Vector2(0, 64), new Vector2(580, 35), 17, font, Brass);
        Label(box, "GameTitle", "가제", new Vector2(0, 0), new Vector2(580, 95), 60, font, Paper);
        Label(box, "PlaceholderHint", "W O R K I N G   T I T L E", new Vector2(0, -68), new Vector2(580, 30), 14, font, Brass);
    }

    public static void ApplyOption(RectTransform panel)
    {
        var title = panel.Find("OptionTitle")?.GetComponent<Text>();
        var font = title != null ? title.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var backdrop = Stretch(panel, "OptionBackdrop");
        Fill(backdrop, new Color(.015f, .02f, .05f, .42f));
        backdrop.SetAsFirstSibling();

        var card = Rect(panel, "OptionCard", Vector2.zero, new Vector2(780, 590));
        Fill(card, Ink);
        Border(card, new Vector2(780, 590), 10);
        card.SetSiblingIndex(1);

        if (title != null)
        {
            Rect(title.transform.parent, title.name, new Vector2(0, 205), new Vector2(620, 58));
            title.text = "SETTINGS";
            title.fontSize = 40;
            title.fontStyle = FontStyle.Bold;
            title.color = Paper;
            title.alignment = TextAnchor.MiddleCenter;
            AddTextOutline(title, new Color(0, 0, 0, .8f));
        }

        Label(panel, "OptionEyebrow", "D R E A M   C A L I B R A T I O N",
              new Vector2(0, 252), new Vector2(650, 26), 14, font, Brass);
        Fill(Rect(panel, "OptionDivider", new Vector2(0, 162), new Vector2(620, 2)), Brass);

        StyleLabel(panel, "MouseSensitivityLabel", "MOUSE SENSITIVITY", new Vector2(-40, 112), font);
        StyleSlider(panel, "MouseSensitivitySlider", new Vector2(-45, 58));
        StyleValue(panel, "MouseSensitivityValue", new Vector2(276, 58), font);

        StyleLabel(panel, "MasterVolumeLabel", "MASTER VOLUME", new Vector2(-40, -22), font);
        StyleSlider(panel, "MasterVolumeSlider", new Vector2(-45, -76));
        StyleValue(panel, "MasterVolumeValue", new Vector2(276, -76), font);

        StyleBackButton(panel, font);
    }

    private static void StyleLabel(RectTransform panel, string name, string value, Vector2 position, Font font)
    {
        var text = panel.Find(name)?.GetComponent<Text>();
        if (text == null) return;
        Rect(panel, name, position, new Vector2(600, 38));
        text.text = value;
        text.font = font;
        text.fontSize = 23;
        text.fontStyle = FontStyle.Bold;
        text.color = Paper;
        text.alignment = TextAnchor.MiddleLeft;
        AddTextOutline(text, new Color(0, 0, 0, .75f));
    }

    private static void StyleValue(RectTransform panel, string name, Vector2 position, Font font)
    {
        var text = panel.Find(name)?.GetComponent<Text>();
        if (text == null) return;
        Rect(panel, name, position, new Vector2(100, 42));
        text.font = font;
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.color = Brass;
        text.alignment = TextAnchor.MiddleCenter;
        AddTextOutline(text, new Color(0, 0, 0, .85f));
    }

    private static void StyleSlider(RectTransform panel, string name, Vector2 position)
    {
        var slider = panel.Find(name)?.GetComponent<Slider>();
        if (slider == null) return;

        Rect(panel, name, position, new Vector2(520, 32));
        slider.direction = Slider.Direction.LeftToRight;

        var background = slider.transform.Find("Background")?.GetComponent<Image>();
        if (background != null) background.color = new Color(.025f, .03f, .055f, 1f);

        var fill = slider.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = Brass;

        var handleRect = slider.transform.Find("Handle Slide Area/Handle") as RectTransform;
        if (handleRect != null)
        {
            handleRect.sizeDelta = new Vector2(28, 28);
            var handle = handleRect.GetComponent<Image>();
            if (handle != null) handle.color = Paper;
        }

        var colors = slider.colors;
        colors.normalColor = Paper;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = Brass;
        colors.fadeDuration = .08f;
        slider.colors = colors;
    }

    private static void StyleBackButton(RectTransform panel, Font font)
    {
        var rect = Rect(panel, "BackButton", new Vector2(0, -215), new Vector2(360, 68));
        var button = rect.GetComponent<Button>();
        var image = rect.GetComponent<Image>();
        if (button == null || image == null) return;

        image.sprite = null;
        image.color = Color.white;
        image.raycastTarget = true;

        var colors = button.colors;
        colors.normalColor = InkSoft;
        colors.highlightedColor = new Color(.27f, .20f, .13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.39f, .29f, .16f, 1f);
        colors.disabledColor = new Color(.10f, .10f, .13f, .55f);
        colors.fadeDuration = .12f;
        button.colors = colors;

        var text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = "<   BACK";
            text.font = font;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.color = Paper;
            AddTextOutline(text, new Color(0, 0, 0, .8f));
        }

        Border(rect, new Vector2(360, 68), 5);
        Label(rect, "BackHint", "ESC", new Vector2(145, 0), new Vector2(42, 32), 13, font, Brass);
    }

    [MenuItem("Tools/Main Menu/Apply Visual Design")]
    public static void ApplyToCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MainMenuScenePath)
        {
            Debug.LogError("[MainMenuVisualDesign] MainMenu 씬을 연 뒤 다시 실행해 주세요.");
            return;
        }

        var canvas = GameObject.Find("Canvas")?.transform;
        var mainPanel = canvas?.Find("MainMenuPanel") as RectTransform;
        var optionPanel = canvas?.Find("OptionPanel") as RectTransform;
        if (canvas == null || mainPanel == null || optionPanel == null)
        {
            Debug.LogError("[MainMenuVisualDesign] MainMenu UI 계층을 찾지 못했습니다.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply Main Menu Visual Design");
        Apply(mainPanel);
        ApplyOption(optionPanel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainMenuVisualDesign] 메인 메뉴와 옵션 화면 디자인을 적용했습니다.");
    }
}
