using UnityEngine;
using UnityEngine.UI;

public class LightGaugeUI2D : MonoBehaviour
{
    public Image fill;
    public Color readyColor = new Color(1f, 0.93f, 0.6f);
    public Color lowColor = new Color(1f, 0.6f, 0.2f);
    public Color lockedColor = new Color(0.45f, 0.45f, 0.5f);
    public float lowThreshold = 0.3f;
    public float lockedBlinkSpeed = 6f;

    private CanvasGroup group;

    void Awake()
    {
        var root = GetComponent<RectTransform>();
        ChapterHudStyle.TopLeft(root, ChapterHudStyle.LeftColumnY(1));
        ChapterHudStyle.Frame(root, false, ChapterHudStyle.CardWidth, ChapterHudStyle.GaugeCardHeight, "손전등");
        if (fill != null)
        {
            var rect = fill.rectTransform;
            rect.anchorMin = new Vector2(0,0); rect.anchorMax = new Vector2(1,0);
            rect.pivot = new Vector2(.5f,0);
            rect.sizeDelta = new Vector2(-32,10); rect.anchoredPosition = new Vector2(0,12);
        }
    }

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || fill == null) return;

        // 손전등을 아직 안 주운 스테이지(1스테이지 초반)에서는 게이지 자체를 숨긴다
        if (group == null) { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); }
        group.alpha = player.HasLantern ? 1f : 0f;
        if (!player.HasLantern) return;

        float charge = player.LightCharge01;
        fill.fillAmount = charge;

        if (player.LightLocked)
        {
            float blink = (Mathf.Sin(Time.time * lockedBlinkSpeed) + 1f) * 0.5f;
            fill.color = Color.Lerp(lockedColor, lowColor, blink * 0.35f);
        }
        else
        {
            fill.color = charge <= lowThreshold ? lowColor : readyColor;
        }
    }
}
