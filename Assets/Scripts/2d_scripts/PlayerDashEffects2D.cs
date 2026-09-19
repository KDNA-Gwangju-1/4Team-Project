using System.Collections.Generic;
using UnityEngine;

// Everything that makes the dash read as a dash: a trail of fading copies, a
// tint that announces the invincibility window, a puff of dust at the start,
// and a small camera kick.
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerDashEffects2D : MonoBehaviour
{
    [Header("Afterimages")]
    public int maxAfterimages = 10;
    public float spawnInterval = 0.025f;
    public float afterimageLife = 0.28f;
    public Color afterimageStart = new Color(0.65f, 0.9f, 1f, 0.75f);
    public Color afterimageEnd = new Color(0.4f, 0.7f, 1f, 0f);
    public float afterimageStretch = 1.2f;
    public int sortingOffset = -1;

    [Header("Invincibility tint")]
    public Color dashTint = new Color(0.72f, 0.92f, 1f);

    [Header("Dust")]
    public Sprite dustSprite;
    public float dustLife = 0.22f;
    public Vector2 dustSize = new Vector2(1.1f, 0.5f);
    public Color dustColor = new Color(0.85f, 0.8f, 0.7f, 0.7f);

    [Header("Camera")]
    public float cameraKick = 0.14f;
    public float cameraKickTime = 0.16f;

    private PlayerMovement2D player;
    private SpriteRenderer sr;
    private CameraFollow2D cameraFollow;

    private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    private readonly List<float> life = new List<float>();
    private float spawnTimer;
    private bool wasDashing;
    private Color baseColor = Color.white;

    void Awake()
    {
        player = GetComponent<PlayerMovement2D>();
        sr = GetComponent<SpriteRenderer>();
        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();

        for (int i = 0; i < maxAfterimages; i++)
        {
            GameObject go = new GameObject("DashAfterimage_" + i);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.enabled = false;
            if (sr != null)
            {
                r.sortingLayerID = sr.sortingLayerID;
                r.sortingOrder = sr.sortingOrder + sortingOffset;
            }
            pool.Add(r);
            life.Add(0f);
        }
    }

    void OnEnable()
    {
        if (player != null) player.OnDashStarted += HandleDashStarted;
    }

    void OnDisable()
    {
        if (player != null) player.OnDashStarted -= HandleDashStarted;
    }

    private void HandleDashStarted()
    {
        SpawnDust();
        if (cameraFollow != null) cameraFollow.Shake(cameraKickTime, cameraKick);
    }

    void LateUpdate()
    {
        bool dashing = player != null && player.IsDashing;

        if (dashing)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                SpawnAfterimage();
            }
            if (sr != null) sr.color = dashTint;
        }
        else if (wasDashing)
        {
            if (sr != null) sr.color = baseColor;
            spawnTimer = 0f;
        }
        wasDashing = dashing;

        FadeAfterimages();
    }

    private void SpawnAfterimage()
    {
        if (sr == null || sr.sprite == null) return;

        int slot = -1;
        for (int i = 0; i < pool.Count; i++)
        {
            if (life[i] <= 0f) { slot = i; break; }
        }
        if (slot < 0) return;

        SpriteRenderer r = pool[slot];
        r.sprite = sr.sprite;
        r.flipX = sr.flipX;
        r.enabled = true;
        r.color = afterimageStart;
        r.transform.position = transform.position;
        r.transform.rotation = transform.rotation;

        Vector3 scale = transform.localScale;
        scale.x *= afterimageStretch;   // a touch of horizontal smear sells the speed
        r.transform.localScale = scale;

        life[slot] = afterimageLife;
    }

    private void FadeAfterimages()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (life[i] <= 0f) continue;

            life[i] -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(life[i] / afterimageLife);
            pool[i].color = Color.Lerp(afterimageStart, afterimageEnd, t);

            if (life[i] <= 0f) pool[i].enabled = false;
        }
    }

    private void SpawnDust()
    {
        if (dustSprite == null) return;

        GameObject go = new GameObject("DashDust");
        go.transform.position = transform.position + new Vector3(-player.DashDirection * 0.35f, -0.85f, 0f);

        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = dustSprite;
        r.color = dustColor;
        if (sr != null)
        {
            r.sortingLayerID = sr.sortingLayerID;
            r.sortingOrder = sr.sortingOrder - 1;
        }

        float nw = r.sprite.bounds.size.x;
        float nh = r.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(dustSize.x / Mathf.Max(0.001f, nw), dustSize.y / Mathf.Max(0.001f, nh), 1f);

        DashDustFade fade = go.AddComponent<DashDustFade>();
        fade.life = dustLife;
        fade.drift = -player.DashDirection * 1.6f;
    }
}

public class DashDustFade : MonoBehaviour
{
    public float life = 0.22f;
    public float drift;

    private SpriteRenderer sr;
    private float elapsed;
    private Color start;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        start = sr != null ? sr.color : Color.white;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, life));

        transform.position += new Vector3(drift * Time.deltaTime, 0f, 0f);
        transform.localScale = new Vector3(
            transform.localScale.x * (1f + 1.6f * Time.deltaTime),
            transform.localScale.y,
            1f);

        if (sr != null)
        {
            Color c = start;
            c.a = start.a * (1f - t);
            sr.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
