using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    private static readonly HashSet<string> DefeatedMonsterIds = new HashSet<string>();

    public string monsterId = "";

    public Transform shimmer;
    public float shimmerScalePulse = 0.15f;
    public float shimmerAlphaBase = 0.25f;
    public float shimmerAlphaPulse = 0.1f;
    public float shimmerJitter = 0.06f;
    public float shimmerSpeed = 2.5f;

    public float detectionRange = 5f;
    public float chaseSpeed = 2f;
    public float respawnDelay = 20f;

    private SpriteRenderer sr;
    private Collider2D col;
    private SpriteRenderer shimmerRenderer;
    private Vector3 shimmerBasePosition;
    private Vector3 spawnPosition;
    private int raycastMask;
    private bool isDead;

    public bool IsRevealed => sr != null && sr.enabled;

    public void Kill()
    {
        if (!string.IsNullOrEmpty(monsterId))
        {
            DefeatedMonsterIds.Add(monsterId);
        }
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        isDead = true;
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
        if (shimmerRenderer != null) shimmerRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPosition;
        if (col != null) col.enabled = true;
        isDead = false;
    }

    void Awake()
    {
        if (!string.IsNullOrEmpty(monsterId) && DefeatedMonsterIds.Contains(monsterId))
        {
            Destroy(gameObject);
            return;
        }

        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
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
        ChasePlayer(player);
    }

    private void ChasePlayer(PlayerMovement2D player)
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > detectionRange * detectionRange) return;

        Vector2 direction = toPlayer.normalized;
        transform.position += (Vector3)(direction * chaseSpeed * Time.deltaTime);
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
            if (hit.collider == col)
            {
                return true;
            }
        }
        return false;
    }
}
