using System;
using System.Collections;
using UnityEngine;

public class Boss2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public int maxHealth = 6;
    public Color hitFlashColor = new Color(1f, 0.3f, 0.3f);
    public float hitFlashDuration = 0.15f;

    [Tooltip("Resting colour. Used to push her into the background while she is out of reach; the attack and hit flashes return to this instead of pure white.")]
    public Color baseTint = Color.white;
    public bool requireLightToDamage = false;
    public Color weakPointColor = new Color(1f, 0.92f, 0.65f);

    public event Action<int, int> OnDamaged;
    public event Action OnDied;

    private SpriteRenderer sr;
    private MonsterSpriteAnimator2D animator;
    private Collider2D col;
    private int currentHealth;
    private bool isDying;
    private bool weakPointExposed;
    private Coroutine flashRoutine;
    private int raycastMask;

    public bool Invulnerable { get; set; }
    public bool IsRevealed => sr != null && sr.enabled;
    public bool IsWeakPointExposed => weakPointExposed;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public Color BaseColor => weakPointExposed ? weakPointColor : baseTint;
    public bool CanBeShot => !isDying && !Invulnerable && (!requireLightToDamage || weakPointExposed);

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<MonsterSpriteAnimator2D>();
        col = GetComponent<Collider2D>();
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = baseTint;
        }

        currentHealth = maxHealth;

        int bulletLayer = LayerMask.NameToLayer("Bullet");
        raycastMask = ~(1 << bulletLayer);
    }

    void Update()
    {
        if (isDying || !requireLightToDamage || col == null) return;

        var player = PlayerMovement2D.Instance;
        bool exposed = player != null && player.IsLightOn && IsLit(player);
        if (exposed == weakPointExposed) return;

        weakPointExposed = exposed;
        if (flashRoutine == null && sr != null)
        {
            sr.color = exposed ? weakPointColor : baseTint;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDying || Invulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (OnDamaged != null) OnDamaged(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isDying = true;
            if (OnDied != null) OnDied();
            StartCoroutine(DieSequence());
        }
        else if (sr != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlashRoutine());
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        sr.color = BaseColor;
        flashRoutine = null;
    }

    private IEnumerator DieSequence()
    {
        if (col != null) col.enabled = false;
        if (sr != null) sr.color = baseTint;

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            yield return animator.PlayDissolveRoutine();
        }

        Destroy(gameObject);
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
