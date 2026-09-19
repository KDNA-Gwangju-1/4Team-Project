using System.Collections;
using UnityEngine;

public class RangedMonster2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public float detectionRange = 8f;
    public float fireCooldown = 2f;
    public GameObject bulletPrefab;
    public float bulletSpeed = 3f;
    public float respawnDelay = 20f;
    [Tooltip("Will not respawn while the player is this close to its spawn point.")]
    public float respawnClearRadius = 7f;
    [Tooltip("Seconds it is visible but harmless after respawning.")]
    public float respawnGrace = 0.9f;

    public float approachSpeed = 1.5f;
    public float preferredDistance = 4f;

    public Transform shimmer;
    public float shimmerScalePulse = 0.15f;
    public float shimmerAlphaBase = 0.25f;
    public float shimmerAlphaPulse = 0.1f;
    public float shimmerJitter = 0.06f;
    public float shimmerSpeed = 2.5f;

    public bool useSilhouetteVisual = false;
    public Color silhouetteColor = Color.black;
    public Color silhouetteOutlineColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public float silhouetteOutlineScale = 1.12f;

    private SpriteRenderer sr;
    private Collider2D col;
    private MonsterSpriteAnimator2D animator;
    private SpriteRenderer shimmerRenderer;
    private SpriteRenderer silhouetteRenderer;
    private SpriteRenderer outlineRenderer;
    private Vector3 shimmerBasePosition;
    private Vector3 spawnPosition;
    private int raycastMask;
    private float lastFireTime = -999f;
    private bool isDead;

    public bool IsRevealed => sr != null && sr.enabled;

    public void Kill()
    {
        StartCoroutine(KillSequence());
    }

    private IEnumerator KillSequence()
    {
        isDead = true;
        if (col != null) col.enabled = false;
        if (shimmerRenderer != null) shimmerRenderer.enabled = false;
        // same leak as Monster2D: the silhouette stayed behind after death
        if (silhouetteRenderer != null) silhouetteRenderer.enabled = false;
        if (outlineRenderer != null) outlineRenderer.enabled = false;

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            if (sr != null) sr.enabled = true;
            yield return animator.PlayDissolveRoutine();
        }
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // never pop back in on top of the player - that is a free hit they
        // had no way to avoid
        while (true)
        {
            var waiting = PlayerMovement2D.Instance;
            if (waiting == null) break;
            if (Vector2.Distance(waiting.transform.position, spawnPosition) > respawnClearRadius) break;
            yield return null;
        }

        transform.position = spawnPosition;
        if (animator != null) animator.ResetAnimation();

        // fade back in harmless for a moment so the player sees it coming
        isDead = false;
        yield return new WaitForSeconds(respawnGrace);
        if (col != null) col.enabled = true;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<MonsterSpriteAnimator2D>();
        if (sr != null) sr.enabled = false;
        spawnPosition = transform.position;

        if (shimmer != null)
        {
            shimmerRenderer = shimmer.GetComponent<SpriteRenderer>();
            shimmerBasePosition = shimmer.localPosition;
        }

        if (useSilhouetteVisual)
        {
            outlineRenderer = CreateSilhouetteLayer("Outline", silhouetteOutlineColor, silhouetteOutlineScale, sr.sortingOrder - 1);
            silhouetteRenderer = CreateSilhouetteLayer("Silhouette", silhouetteColor, 1f, sr.sortingOrder);
        }

        int bulletLayer = LayerMask.NameToLayer("Bullet");
        raycastMask = ~(1 << bulletLayer);
    }

    private SpriteRenderer CreateSilhouetteLayer(string name, Color color, float scale, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.color = color;
        renderer.sortingLayerName = sr.sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        return renderer;
    }

    void Update()
    {
        if (isDead || sr == null || col == null) return;

        var player = PlayerMovement2D.Instance;
        bool revealed = player != null && player.IsLightOn && IsLit(player);
        sr.enabled = revealed;

        if (useSilhouetteVisual)
        {
            UpdateSilhouette(!revealed);
        }
        else
        {
            UpdateShimmer(!revealed);
        }

        ApproachPlayer(player);
        TryFireAt(player);
    }

    private void ApproachPlayer(PlayerMovement2D player)
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        if (distance <= preferredDistance || distance > detectionRange) return;

        Vector2 direction = toPlayer.normalized;
        transform.position += (Vector3)(direction * approachSpeed * Time.deltaTime);
    }

    private void UpdateSilhouette(bool active)
    {
        if (silhouetteRenderer == null || outlineRenderer == null) return;

        silhouetteRenderer.enabled = active;
        outlineRenderer.enabled = active;
        if (!active) return;

        silhouetteRenderer.sprite = sr.sprite;
        outlineRenderer.sprite = sr.sprite;
        silhouetteRenderer.flipX = sr.flipX;
        outlineRenderer.flipX = sr.flipX;
    }

    private void UpdateShimmer(bool active)
    {
        if (shimmerRenderer == null) return;

        shimmerRenderer.enabled = active;
        if (!active) return;

        float t = Time.time * shimmerSpeed;

        float scale = 1f + Mathf.Sin(t) * shimmerScalePulse;
        shimmer.localScale = new Vector3(scale, scale, 1f);

        Color c = shimmerRenderer.color;
        c.a = shimmerAlphaBase + Mathf.Sin(t * 1.3f) * shimmerAlphaPulse;
        shimmerRenderer.color = c;

        Vector3 jitter = new Vector3(Mathf.Sin(t * 2.3f), Mathf.Cos(t * 1.7f), 0f) * shimmerJitter;
        shimmer.localPosition = shimmerBasePosition + jitter;
    }

    private void TryFireAt(PlayerMovement2D player)
    {
        if (player == null || bulletPrefab == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > detectionRange * detectionRange) return;
        if (Time.time - lastFireTime < fireCooldown) return;

        Vector2 direction = toPlayer.normalized;
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        BossBullet2D bullet = bulletObj.GetComponent<BossBullet2D>();
        if (bullet != null)
        {
            bullet.Init(direction, bulletSpeed);
        }

        lastFireTime = Time.time;
    }

    private bool IsLit(PlayerMovement2D player)
    {
        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float rayDistance = Mathf.Max(0f, player.lightRange - SkinMargin);

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 rayOrigin = origin + dir * SkinMargin;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, rayDistance, raycastMask);
            if (hit.collider == col) return true;
        }
        return false;
    }
}
