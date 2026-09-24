using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public Transform shimmer;
    public float shimmerScalePulse = 0.15f;
    public float shimmerAlphaBase = 0.25f;
    public float shimmerAlphaPulse = 0.1f;
    public float shimmerJitter = 0.06f;
    public float shimmerSpeed = 2.5f;

    public float detectionRange = 5f;
    public float chaseSpeed = 2f;
    public float respawnDelay = 20f;
    [Tooltip("Will not respawn while the player is this close to its spawn point.")]
    public float respawnClearRadius = 7f;
    [Tooltip("Seconds it is visible but harmless after respawning.")]
    public float respawnGrace = 0.9f;

    [Header("Ground hopping")]
    [Tooltip("Sticks to the floor and closes in with hops instead of flying straight at the player.")]
    public bool groundHopper = false;
    public LayerMask groundLayer;
    public float hopInterval = 0.85f;
    public float hopTime = 0.42f;
    public float hopHeight = 1.5f;
    public float hopDistance = 2.3f;
    public float groundProbeUp = 3f;
    [Tooltip("Empty margin under the artwork, as a fraction of sprite height - keeps the drawn feet on the floor.")]
    public float spriteBottomPad = 0.0254f;
    public float groundProbeDistance = 40f;

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
    private bool lit;
    private float hopCooldown;
    private float hopElapsed;
    private bool hopping;
    private float hopStartX;
    private float hopTargetX;
    private float hopGroundY;

    // with the flashlight mask the body renderer is always on, so what can be
    // shot is tracked separately from what is drawn
    public bool IsRevealed => useSilhouetteVisual ? lit : (sr != null && sr.enabled);
    public bool IsDead => isDead;

    public void Kill()
    {
        // no permanent kill list: monsters come back after respawnDelay anyway, and a
        // static list outlived retries, so shared ids wiped whole groups on the next try
        StartCoroutine(KillSequence());
    }

    private IEnumerator KillSequence()
    {
        isDead = true;
        if (col != null) col.enabled = false;
        if (shimmerRenderer != null) shimmerRenderer.enabled = false;
        // the silhouette layers were being left on, so a black shape stayed
        // behind after the monster died
        if (silhouetteRenderer != null) silhouetteRenderer.enabled = false;
        if (outlineRenderer != null) outlineRenderer.enabled = false;

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            if (sr != null)
            {
                sr.enabled = true;
                sr.maskInteraction = SpriteMaskInteraction.None;   // death plays in full, beam or not
            }
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
        if (sr != null && useSilhouetteVisual) sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
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
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            outlineRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            silhouetteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
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
        lit = revealed;

        if (useSilhouetteVisual)
        {
            // both layers stay on; the flashlight's sprite mask splits them
            sr.enabled = true;
            UpdateSilhouette(true);
        }
        else
        {
            sr.enabled = revealed;
            UpdateShimmer(!revealed);
        }

        if (groundHopper) HopTowardPlayer(player);
        else ChasePlayer(player);
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

    // Hops along the floor: a beat on the ground, then a short arc toward the
    // player. Height comes from the arc, never from leaving the floor behind.
    private void HopTowardPlayer(PlayerMovement2D player)
    {
        if (hopping)
        {
            hopElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(hopElapsed / Mathf.Max(0.05f, hopTime));
            float x = Mathf.Lerp(hopStartX, hopTargetX, t);
            float y = hopGroundY + Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = new Vector3(x, y, transform.position.z);

            if (t >= 1f)
            {
                hopping = false;
                hopCooldown = hopInterval;
                transform.position = new Vector3(hopTargetX, GroundYAt(hopTargetX), transform.position.z);
            }
            return;
        }

        transform.position = new Vector3(transform.position.x, GroundYAt(transform.position.x), transform.position.z);

        hopCooldown -= Time.deltaTime;
        if (player == null || hopCooldown > 0f) return;

        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > detectionRange) return;

        float dir = Mathf.Sign(dx);
        float step = Mathf.Min(hopDistance, Mathf.Abs(dx));
        float targetX = transform.position.x + dir * step;
        float groundY;

        // no floor under the next step - turn back onto solid ground instead of
        // walking off the edge, then it'll try for the player again next hop
        if (!TryGetGroundY(targetX, out groundY))
        {
            dir = -dir;
            targetX = transform.position.x + dir * step;
            if (!TryGetGroundY(targetX, out groundY)) return;
        }

        hopStartX = transform.position.x;
        hopTargetX = targetX;
        hopGroundY = groundY;
        hopElapsed = 0f;
        hopping = true;

        if (sr != null) sr.flipX = dir < 0f;
    }

    private float GroundYAt(float x)
    {
        TryGetGroundY(x, out float groundY);
        return groundY;
    }

    private bool TryGetGroundY(float x, out float groundY)
    {
        groundY = transform.position.y;
        if (groundLayer.value == 0) return true;

        Vector2 origin = new Vector2(x, transform.position.y + groundProbeUp);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundProbeDistance, groundLayer);
        if (hit.collider == null) return false;

        // line up the DRAWN feet with the floor, not the collider box: the art
        // is taller than the collider, which is what sank the monster into the sand
        float feetOffset = 1f;
        if (sr != null && sr.sprite != null)
        {
            feetOffset = (transform.position.y - sr.bounds.min.y) - sr.bounds.size.y * spriteBottomPad;
        }
        else if (col != null)
        {
            feetOffset = col.bounds.extents.y;
        }
        groundY = hit.point.y + feetOffset;
        return true;
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
