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
    private bool isDead;

    public bool IsRevealed => sr != null && sr.enabled;

    public void Kill()
    {
        if (!string.IsNullOrEmpty(monsterId))
        {
            DefeatedMonsterIds.Add(monsterId);
        }
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
        if (!string.IsNullOrEmpty(monsterId) && DefeatedMonsterIds.Contains(monsterId))
        {
            Destroy(gameObject);
            return;
        }

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

        ChasePlayer(player);
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
