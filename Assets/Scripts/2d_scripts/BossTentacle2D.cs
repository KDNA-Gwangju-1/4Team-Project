using System.Collections;
using UnityEngine;

public class BossTentacle2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public Boss2D boss;
    public int damageToBoss = 1;
    public float respawnDelay = 12f;

    public Color silhouetteColor = Color.black;
    public Color silhouetteOutlineColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public float silhouetteOutlineScale = 1.12f;

    private SpriteRenderer sr;
    private Collider2D col;
    private MonsterSpriteAnimator2D animator;
    private SpriteRenderer silhouetteRenderer;
    private SpriteRenderer outlineRenderer;
    private int raycastMask;
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
        if (silhouetteRenderer != null) silhouetteRenderer.enabled = false;
        if (outlineRenderer != null) outlineRenderer.enabled = false;

        if (boss != null)
        {
            boss.TakeDamage(damageToBoss);
        }

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            if (sr != null) sr.enabled = true;
            yield return animator.PlayDissolveRoutine();
        }
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

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

        outlineRenderer = CreateSilhouetteLayer("Outline", silhouetteOutlineColor, silhouetteOutlineScale, sr.sortingOrder - 1);
        silhouetteRenderer = CreateSilhouetteLayer("Silhouette", silhouetteColor, 1f, sr.sortingOrder);

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

        UpdateSilhouette(!revealed);
    }

    private void UpdateSilhouette(bool active)
    {
        if (silhouetteRenderer == null || outlineRenderer == null) return;

        silhouetteRenderer.enabled = active;
        outlineRenderer.enabled = active;
        if (!active) return;

        silhouetteRenderer.sprite = sr.sprite;
        outlineRenderer.sprite = sr.sprite;
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
