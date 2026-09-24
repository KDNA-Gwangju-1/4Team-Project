using UnityEngine;
using UnityEngine.UI;

// Long red boss bar across the top, MapleStory style: the red fill snaps to the
// real value while a paler ghost bar drains behind it so hits stay readable.
public class BossHealthBarUI2D : MonoBehaviour
{
    public Boss2D boss;
    public Image fill;
    public Image ghostFill;
    public Text nameLabel;
    public Text valueLabel;
    public CanvasGroup group;

    public string bossName = "???";
    public float ghostDrainSpeed = 0.35f;
    public float ghostDelay = 0.4f;
    public float fadeSpeed = 2.5f;
    public bool hideWhenBossGone = true;

    private float ghost = 1f;
    private float ghostHoldTimer;
    private BossPhaseController2D phaseController;
    private Text phaseLabel;
    private int shownPhase = -1;

    void Start()
    {
        if (boss == null) boss = Object.FindFirstObjectByType<Boss2D>();
        if (boss != null) phaseController = boss.GetComponent<BossPhaseController2D>();
        BuildFrame();
        if (nameLabel != null) nameLabel.text = bossName;
        ghost = 1f;
    }

    void Update()
    {
        if ((boss == null || boss.CurrentHealth <= 0) && hideWhenBossGone)
        {
            if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, 0f, fadeSpeed * Time.deltaTime);
            return;
        }
        if (boss == null) return;

        int phase = phaseController != null ? phaseController.Phase : 1;
        if (phase != shownPhase)
        {
            shownPhase = phase;
            if (phaseLabel != null) phaseLabel.text = phase == 0 ? "악몽이 변하고 있다" : phase == 2 ? "2단계 · 깨어난 악몽" : "1단계 · 얽힌 기억";
            if (fill != null) fill.color = phase == 2 ? new Color(1f, .29f, .42f) : new Color(.82f, .40f, .72f);
        }

        if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, 1f, fadeSpeed * Time.deltaTime);

        float ratio = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;

        if (fill != null) fill.fillAmount = ratio;

        if (ratio < ghost)
        {
            ghostHoldTimer += Time.deltaTime;
            if (ghostHoldTimer >= ghostDelay)
            {
                ghost = Mathf.MoveTowards(ghost, ratio, ghostDrainSpeed * Time.deltaTime);
            }
        }
        else
        {
            ghost = ratio;
            ghostHoldTimer = 0f;
        }

        if (ghostFill != null) ghostFill.fillAmount = ghost;
        if (valueLabel != null) valueLabel.text = boss.CurrentHealth + " / " + boss.MaxHealth;
    }

    private void BuildFrame()
    {
        var rect = transform as RectTransform;
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(820f, 92f);
        ChapterHudStyle.SkinCard(GetComponent<Image>(), false);
        PlaceGauge(ghostFill);
        PlaceGauge(fill);
        if (ghostFill != null) ghostFill.color = new Color(1f, .78f, .55f, .9f);
        PlaceLabel(nameLabel, new Vector2(20f, -12f), new Vector2(260f, 28f), TextAnchor.MiddleLeft);
        PlaceLabel(valueLabel, new Vector2(640f, -12f), new Vector2(160f, 28f), TextAnchor.MiddleRight);
        var old = transform.Find("PhaseLabel");
        phaseLabel = old != null ? old.GetComponent<Text>() : new GameObject("PhaseLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        phaseLabel.transform.SetParent(transform, false);
        PlaceLabel(phaseLabel, new Vector2(275f, -12f), new Vector2(360f, 28f), TextAnchor.MiddleCenter);
        phaseLabel.color = ChapterHudStyle.CaptionInk(false);
        phaseLabel.fontSize = 20;
    }

    private static void PlaceGauge(Image image)
    {
        if (image == null) return;
        var r = image.rectTransform;
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -54f);
        r.sizeDelta = new Vector2(-40f, 16f);
        image.raycastTarget = false;
    }

    private static void PlaceLabel(Text text, Vector2 position, Vector2 size, TextAnchor alignment)
    {
        if (text == null) return;
        var r = text.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = position; r.sizeDelta = size;
        text.font = HangulFont.GetEmphasis(); text.fontSize = 22;
        text.color = ChapterHudStyle.Ink(false); text.alignment = alignment;
        text.raycastTarget = false;
    }
}
