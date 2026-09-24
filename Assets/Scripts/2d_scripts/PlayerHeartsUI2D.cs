using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Row of hearts showing the player's health as a fraction of max health.
// Every chapter shows the same five hearts at the same size (see BrightDream's HeartHealthUI),
// so a heart is not one hit point: with maxHealth 8 each heart holds 1.6 and fills partly.
public class PlayerHeartsUI2D : MonoBehaviour
{
    public Sprite fullHeart;
    public Sprite emptyHeart;
    [Tooltip("Hearts always shown, whatever maxHealth is. Kept equal to HeartHealthUI.heartsShown.")]
    public int heartsShown = ChapterHudStyle.HeartCount;

    public float shakeOnDamage = 6f;
    public float shakeDuration = 0.35f;

    // shared layout numbers (ChapterHudStyle) so the widget is identical in 3D and 2D
    private const float HeartSize = ChapterHudStyle.HeartSize;
    private const float HeartStep = ChapterHudStyle.HeartStep;

    private readonly List<Image> fills = new List<Image>();
    private RectTransform rect;
    private int lastHealth = -1;
    private int lastMax = -1;
    private float shakeTimer;
    private Vector2 basePosition;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        ChapterHudStyle.TopLeft(rect, ChapterHudStyle.LeftColumnY(0));
        basePosition = rect.anchoredPosition;
    }

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null) return;

        if (fills.Count != heartsShown) Rebuild();

        int health = player.CurrentHealth;
        int max = Mathf.Max(1, player.maxHealth);
        if (health != lastHealth || max != lastMax)
        {
            if (lastHealth >= 0 && health < lastHealth) shakeTimer = shakeDuration;
            lastHealth = health;
            lastMax = max;
            float filled = Mathf.Clamp01(health / (float)max) * fills.Count;
            for (int i = 0; i < fills.Count; i++)
            {
                fills[i].fillAmount = Mathf.Clamp01(filled - i);
            }
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float falloff = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
            rect.anchoredPosition = basePosition + new Vector2(
                Random.Range(-1f, 1f) * shakeOnDamage * falloff,
                Random.Range(-1f, 1f) * shakeOnDamage * falloff);
        }
        else
        {
            rect.anchoredPosition = basePosition;
        }
    }

    private void Rebuild()
    {
        ChapterHudStyle.Frame(rect, false, ChapterHudStyle.CardWidth, ChapterHudStyle.HeartCardHeight, "체력");
        foreach (Image fill in fills)
        {
            if (fill != null) Destroy(fill.transform.parent.gameObject);
        }
        fills.Clear();

        for (int i = 0; i < heartsShown; i++)
        {
            GameObject heart = new GameObject("Heart_" + (i + 1));
            heart.transform.SetParent(transform, false);

            RectTransform r = heart.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(HeartSize, HeartSize);
            r.anchoredPosition = new Vector2(ChapterHudStyle.HeartLeft + i * HeartStep, -ChapterHudStyle.HeartTop);

            // empty heart underneath, full heart on top revealed left to right
            Image empty = heart.AddComponent<Image>();
            empty.sprite = emptyHeart;
            empty.preserveAspect = true;
            empty.raycastTarget = false;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(heart.transform, false);
            RectTransform fr = fillGO.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;

            Image fill = fillGO.AddComponent<Image>();
            fill.sprite = fullHeart;
            fill.preserveAspect = true;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fills.Add(fill);
        }
        lastHealth = -1;
    }
}
