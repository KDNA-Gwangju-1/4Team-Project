using System.Collections.Generic;
using UnityEngine;

// Turns the flat pellet into a bolt of light: it points where it is going,
// stretches into a streak, breathes, and drags a short fading tail behind it.
[RequireComponent(typeof(SpriteRenderer))]
public class LightBulletVisual2D : MonoBehaviour
{
    [Header("Shape")]
    public float stretch = 1.9f;
    public float thickness = 0.85f;

    [Header("Flicker")]
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 22f;

    [Header("Tail")]
    public int tailCount = 6;
    public float tailSpacing = 0.035f;
    public float tailLife = 0.16f;
    public Color tailStart = new Color(1f, 0.82f, 0.35f, 0.55f);
    public Color tailEnd = new Color(1f, 0.55f, 0.1f, 0f);
    public float tailShrink = 0.55f;

    private SpriteRenderer sr;
    private Vector3 lastPosition;
    private Vector3 baseScale;
    private readonly List<SpriteRenderer> tail = new List<SpriteRenderer>();
    private readonly List<float> life = new List<float>();
    private float spawnTimer;
    private bool oriented;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        baseScale = transform.localScale;

        for (int i = 0; i < tailCount; i++)
        {
            GameObject go = new GameObject("BulletTail_" + i);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sr.sprite;
            r.sortingLayerID = sr.sortingLayerID;
            r.sortingOrder = sr.sortingOrder - 1;
            r.enabled = false;
            tail.Add(r);
            life.Add(0f);
        }
    }

    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPosition;

        if (delta.sqrMagnitude > 0.000001f)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            oriented = true;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = tailSpacing;
                SpawnTail();
            }
        }

        if (oriented)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(
                baseScale.x * stretch * pulse,
                baseScale.y * thickness * pulse,
                baseScale.z);
        }

        lastPosition = transform.position;
        FadeTail();
    }

    private void SpawnTail()
    {
        int slot = -1;
        for (int i = 0; i < tail.Count; i++)
        {
            if (life[i] <= 0f) { slot = i; break; }
        }
        if (slot < 0) return;

        SpriteRenderer r = tail[slot];
        r.transform.position = transform.position;
        r.transform.rotation = transform.rotation;
        r.transform.localScale = transform.localScale * tailShrink;
        r.color = tailStart;
        r.enabled = true;
        life[slot] = tailLife;
    }

    private void FadeTail()
    {
        for (int i = 0; i < tail.Count; i++)
        {
            if (life[i] <= 0f) continue;

            life[i] -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(life[i] / tailLife);
            tail[i].color = Color.Lerp(tailStart, tailEnd, t);
            tail[i].transform.localScale = Vector3.Lerp(
                transform.localScale * tailShrink, Vector3.zero, t);

            if (life[i] <= 0f) tail[i].enabled = false;
        }
    }

    void OnDestroy()
    {
        // the bullet dies on impact; let its tail finish on its own
        for (int i = 0; i < tail.Count; i++)
        {
            if (tail[i] == null) continue;
            if (life[i] > 0f)
            {
                BulletTailFade fade = tail[i].gameObject.AddComponent<BulletTailFade>();
                fade.life = life[i];
                fade.from = tail[i].color;
                fade.to = tailEnd;
                tail[i].transform.SetParent(null, true);
            }
            else
            {
                Destroy(tail[i].gameObject);
            }
        }
    }
}

public class BulletTailFade : MonoBehaviour
{
    public float life = 0.16f;
    public Color from;
    public Color to;

    private SpriteRenderer sr;
    private float total;
    private float elapsed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        total = Mathf.Max(0.01f, life);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / total);
        if (sr != null) sr.color = Color.Lerp(from, to, t);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, t * 0.5f);
        if (t >= 1f) Destroy(gameObject);
    }
}
