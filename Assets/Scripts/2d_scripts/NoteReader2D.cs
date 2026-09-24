using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// A crumpled note on the floor next to the flashlight. Opening it is how the
// player is taught to use the thing they just picked up, so the tutorial is
// something they reach for rather than something thrown at them.
public class NoteReader2D : MonoBehaviour
{
    [Tooltip("How close the player has to be before the prompt appears.")]
    public float readRange = 2.6f;
    public Key readKey = Key.E;
    [TextArea] public string promptText = "E키를 눌러 펼치기";

    [Header("The opened note")]
    [Tooltip("The unfolded sheet the text is written on.")]
    public Sprite sheet;
    public Font font;
    public int fontSize = 27;
    [Tooltip("Ink, not UI. Dark, on paper.")]
    public Color inkColor = new Color(0.13f, 0.10f, 0.09f);
    [Tooltip("Height of the sheet as a fraction of the screen.")]
    [Range(0.3f, 1f)] public float sheetHeight01 = 0.92f;
    [Tooltip("Where the paper sits inside its image, 0..1 from the bottom left. The art has empty margin around it.")]
    public Rect paperRect01 = new Rect(0.26f, 0.12f, 0.48f, 0.75f);
    [Tooltip("Margin inside the paper before the writing starts, as a fraction of the paper.")]
    [Range(0f, 0.3f)] public float writingInset01 = 0.11f;
    public float openFadeDuration = 0.18f;

    [TextArea(12, 30)]
    [Tooltip("{lightSeconds} {lockout} {dashCount} {dashRegen} are filled in from the player so the numbers cannot go stale.")]
    public string noteBody =
        "도움말\n" +
        "\n" +
        "마우스 오른쪽 버튼  -  빛을 켭니다\n" +
        "마우스 왼쪽 버튼  -  광탄을 쏩니다\n" +
        "Shift  -  대시 회피\n" +
        "\n" +
        "몬스터를 빛으로 비춘 상태에서 광탄을 맞혀야\n" +
        "처치할 수 있습니다. 비추지 않고 쏘면 맞지 않습니다.\n" +
        "쏘는 동안에도 계속 비추고 있어야 합니다.\n" +
        "\n" +
        "빛과 대시는 게이지를 사용합니다.\n" +
        "빛은 {lightSeconds}초까지 켤 수 있고, 다 쓰면\n" +
        "{lockout}초 동안 다시 켜지지 않습니다.\n" +
        "대시는 {dashCount}번까지 연달아 쓸 수 있고,\n" +
        "한 칸이 {dashRegen}초마다 다시 찹니다.\n" +
        "\n" +
        "이 공간은 저중력이라 체감이 낯설 수 있습니다.";

    private InteractPrompt2D prompt;
    private GameObject canvasGO;
    private CanvasGroup group;
    private bool open;

    /// <summary>Any note open right now. ESC closes the note first, so the pause menu stays shut.</summary>
    public static bool AnyOpen { get; private set; }
    private float alpha;

    void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
    }

    void Update()
    {
        // ESC 일시정지 중에는 입력을 받지 않는다.
        if (PauseMenu.IsPaused) return;
        PlayerMovement2D player = PlayerMovement2D.Instance;
        Keyboard keyboard = Keyboard.current;

        if (open)
        {
            Fade(1f);
            bool close = keyboard != null
                && (keyboard[readKey].wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
            if (close)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) PauseMenu.ConsumeEscapeThisFrame();
                Close(player);
            }
            return;
        }

        Fade(0f);

        bool inRange = player != null
            && Vector2.Distance(player.transform.position, transform.position) <= readRange;

        if (prompt == null && inRange) prompt = gameObject.AddComponent<InteractPrompt2D>();
        if (prompt != null) prompt.SetVisible(inRange, promptText);

        if (!inRange || keyboard == null || !keyboard[readKey].wasPressedThisFrame) return;

        Open(player);
    }

    private void Open(PlayerMovement2D player)
    {
        Build(player);
        if (canvasGO == null) return;

        open = true;
        AnyOpen = true;
        if (prompt != null) prompt.SetVisible(false, promptText);
        // reading is a pause, not a moment to be walked into a monster during
        if (player != null) player.enabled = false;
    }

    private void Close(PlayerMovement2D player)
    {
        open = false;
        AnyOpen = false;
        if (player != null) player.enabled = true;
    }

    private void Fade(float target)
    {
        if (group == null) return;
        alpha = (openFadeDuration <= 0.01f)
            ? target
            : Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime / openFadeDuration);
        group.alpha = alpha;
        if (canvasGO != null) canvasGO.SetActive(alpha > 0.002f);
    }

    private void Build(PlayerMovement2D player)
    {
        if (canvasGO != null || sheet == null) return;

        canvasGO = new GameObject("NoteCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // knock the arena back so the paper is the only thing being looked at
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(canvasGO.transform, false);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.62f);
        Stretch(dimImage.rectTransform);

        float sheetH = 1080f * sheetHeight01;
        float sheetW = sheetH * (sheet.rect.width / sheet.rect.height);

        GameObject paper = new GameObject("Sheet");
        paper.transform.SetParent(canvasGO.transform, false);
        Image paperImage = paper.AddComponent<Image>();
        paperImage.sprite = sheet;
        paperImage.preserveAspect = true;
        RectTransform pr = paperImage.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(sheetW, sheetH);
        pr.anchoredPosition = Vector2.zero;

        // the writing goes on the paper, not on the empty margin around it
        GameObject textGO = new GameObject("Writing");
        textGO.transform.SetParent(paper.transform, false);
        Text body = textGO.AddComponent<Text>();
        body.font = font != null ? font : HangulFont.Get();
        body.fontSize = fontSize;
        HangulFont.Apply(body);
        body.color = inkColor;
        body.alignment = TextAnchor.MiddleCenter;
        body.lineSpacing = 1.25f;
        body.raycastTarget = false;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.text = Fill(noteBody, player);

        RectTransform tr = body.rectTransform;
        tr.anchorMin = new Vector2(paperRect01.xMin, paperRect01.yMin);
        tr.anchorMax = new Vector2(paperRect01.xMax, paperRect01.yMax);
        tr.offsetMin = new Vector2(sheetW * paperRect01.width * writingInset01,
                                   sheetH * paperRect01.height * writingInset01);
        tr.offsetMax = -tr.offsetMin;

        GameObject hintGO = new GameObject("CloseHint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        Text hint = hintGO.AddComponent<Text>();
        hint.font = body.font;
        hint.fontSize = 24;
        hint.color = new Color(1f, 0.92f, 0.70f, 0.85f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.raycastTarget = false;
        hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        hint.text = readKey + "키 또는 ESC로 닫기";
        RectTransform hr = hint.rectTransform;
        hr.anchorMin = new Vector2(0f, 0.035f);
        hr.anchorMax = new Vector2(1f, 0.035f);
        hr.pivot = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(0f, 40f);
        hr.anchoredPosition = Vector2.zero;
        ChapterNoticeStyle.Apply(hint, ChapterDialogueSkin.Theme.BadDream, 600f, 60f, 24);
    }

    // the numbers live on the player, so the note cannot drift out of date
    private string Fill(string source, PlayerMovement2D player)
    {
        if (player == null) return source;
        return source
            .Replace("{lightSeconds}", Mathf.RoundToInt(player.lightMaxSeconds).ToString())
            .Replace("{lockout}", Mathf.RoundToInt(player.lightLockoutSeconds).ToString())
            .Replace("{dashCount}", player.dashStaminaMax.ToString())
            .Replace("{dashRegen}", player.dashStaminaRegenTime.ToString("0.#"));
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
