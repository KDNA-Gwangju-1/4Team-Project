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
    [Tooltip("Off in phase 1: she sends her children out to play and the tentacles are what is actually connected to her, so shooting her is swallowed. TentacleStrike2D.Kill still reaches her.")]
    public bool acceptsDirectHits = true;
    public Color weakPointColor = new Color(1f, 0.92f, 0.65f);
    [Tooltip("Let the flashlight reach her through platforms - otherwise the nearest ledge eats the ray.")]
    public bool beamPassesThroughGround = true;

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

    // 리트라이로 2페이즈부터 재개할 때 쓴다. 체력바는 CurrentHealth를 매 프레임 읽으므로
    // OnDamaged를 쏘지 않는다 - 쐈다가는 페이즈 전환이 다시 걸린다.
    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }
    public Color BaseColor => weakPointExposed ? weakPointColor : baseTint;
    public bool CanBeShot => !isDying && !Invulnerable && acceptsDirectHits && (!requireLightToDamage || weakPointExposed);

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
        // Platforms must not block the beam. In phase 2 she sits behind a stack of
        // them, and a ray that stops on the nearest ledge means she can never be lit.
        raycastMask = ~(1 << bulletLayer);
        if (beamPassesThroughGround)
        {
            raycastMask &= ~(1 << LayerMask.NameToLayer("Ground"));
        }
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
        GameSfx.Play("Purify", .42f);
        if (OnDamaged != null) OnDamaged(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isDying = true;
            GameSfx.Play("Victory", .5f);
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
