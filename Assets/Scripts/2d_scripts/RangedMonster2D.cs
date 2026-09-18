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

    public Transform shimmer;
    public float shimmerScalePulse = 0.15f;
    public float shimmerAlphaBase = 0.25f;
    public float shimmerAlphaPulse = 0.1f;
    public float shimmerJitter = 0.06f;
    public float shimmerSpeed = 2.5f;

    private SpriteRenderer sr;
    private Collider2D col;
    private MonsterSpriteAnimator2D animator;
    private SpriteRenderer shimmerRenderer;
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

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            if (sr != null) sr.enabled = true;
            yield return animator.PlayDissolveRoutine();
        }
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPosition;
        if (col != null) col.enabled = true;
        if (animator != null) animator.ResetAnimation();
        isDead = false;
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

        int bulletLayer = LayerMask.NameToLayer("Bullet");
        raycastMask = ~(1 << bulletLayer);
    }

    void Update()
    {
        if (isDead || sr == null || col == null) return;

        var player = PlayerMovement2D.Instance;
        bool revealed = player != null && player.IsLightOn && IsLit(player);
        sr.enabled = revealed;

        UpdateShimmer(!revealed);
        TryFireAt(player);
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
