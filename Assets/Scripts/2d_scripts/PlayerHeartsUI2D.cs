using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Minecraft-style row of hearts, one per point of player health.
public class PlayerHeartsUI2D : MonoBehaviour
{
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public float heartSize = 44f;
    public float spacing = 4f;
    public int heartsPerRow = 10;

    public float shakeOnDamage = 6f;
    public float shakeDuration = 0.35f;

    private readonly List<Image> hearts = new List<Image>();
    private RectTransform rect;
    private int lastHealth = -1;
    private float shakeTimer;
    private Vector2 basePosition;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        basePosition = rect.anchoredPosition;
    }

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null) return;

        if (hearts.Count != player.maxHealth) Rebuild(player.maxHealth);

        int health = player.CurrentHealth;
        if (health != lastHealth)
        {
            if (lastHealth >= 0 && health < lastHealth) shakeTimer = shakeDuration;
            lastHealth = health;
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].sprite = (i < health) ? fullHeart : emptyHeart;
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

    private void Rebuild(int count)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] != null) Destroy(hearts[i].gameObject);
        }
        hearts.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Heart_" + (i + 1));
            go.transform.SetParent(transform, false);

            RectTransform r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(heartSize, heartSize);

            int column = i % heartsPerRow;
            int row = i / heartsPerRow;
            r.anchoredPosition = new Vector2(
                column * (heartSize + spacing),
                -row * (heartSize + spacing));

            Image img = go.AddComponent<Image>();
            img.sprite = fullHeart;
            img.preserveAspect = true;
            img.raycastTarget = false;
            hearts.Add(img);
        }
        lastHealth = -1;
    }
}
