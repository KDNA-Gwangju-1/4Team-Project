using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Segmented dash stamina: one pip per banked dash, the next one filling up.
// Pips read better than a smooth bar here because the resource is spent in
// whole units - you can see at a glance how many dashes you actually have.
public class DashStaminaUI2D : MonoBehaviour
{
    public Sprite pipSprite;
    public float pipWidth = 58f;
    public float pipHeight = 14f;
    public float spacing = 5f;

    public Color readyColor = new Color(0.55f, 0.95f, 1f);
    public Color chargingColor = new Color(0.25f, 0.45f, 0.6f);
    public Color emptyColor = new Color(0.16f, 0.17f, 0.22f);

    private readonly List<Image> pips = new List<Image>();
    private readonly List<Image> fills = new List<Image>();

    void Awake()
    {
        var root = GetComponent<RectTransform>();
        ChapterHudStyle.TopLeft(root, ChapterHudStyle.LeftColumnY(2));
        ChapterHudStyle.Frame(root, false, ChapterHudStyle.CardWidth, ChapterHudStyle.GaugeCardHeight, "대시  ·  SHIFT");
        pipWidth = 84f; pipHeight = 12f; spacing = 8f;
    }

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null) return;

        if (pips.Count != player.dashStaminaMax) Rebuild(player.dashStaminaMax);

        // the lantern gauge is hidden until the lantern is picked up; close the gap under 체력
        ChapterHudStyle.TopLeft((RectTransform)transform, ChapterHudStyle.LeftColumnY(player.HasLantern ? 2 : 1));

        float stamina = player.DashStamina;
        for (int i = 0; i < pips.Count; i++)
        {
            float amount = Mathf.Clamp01(stamina - i);
            fills[i].fillAmount = amount;
            fills[i].color = amount >= 1f ? readyColor : chargingColor;
            pips[i].color = emptyColor;
        }
    }

    private void Rebuild(int count)
    {
        for (int i = 0; i < pips.Count; i++)
        {
            if (pips[i] != null) Destroy(pips[i].gameObject);
        }
        pips.Clear();
        fills.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject slot = new GameObject("DashPip_" + (i + 1));
            slot.transform.SetParent(transform, false);
            RectTransform r = slot.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(pipWidth, pipHeight);
            r.anchoredPosition = new Vector2(16f + i * (pipWidth + spacing), -32f);

            Image bg = slot.AddComponent<Image>();
            bg.sprite = pipSprite;
            bg.type = Image.Type.Sliced;
            bg.color = emptyColor;
            bg.raycastTarget = false;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(slot.transform, false);
            RectTransform fr = fillGO.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(2f, 2f);
            fr.offsetMax = new Vector2(-2f, -2f);

            Image fill = fillGO.AddComponent<Image>();
            fill.sprite = pipSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.color = readyColor;
            fill.raycastTarget = false;

            pips.Add(bg);
            fills.Add(fill);
        }
    }
}
