using UnityEngine;

public class Boss2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public int maxHealth = 6;

    public float moveRangeX = 4f;
    public float baseMoveSpeed = 1.5f;

    public GameObject bulletPrefab;
    public float baseBulletSpeed = 6f;
    public float baseFireCooldown = 2f;

    private SpriteRenderer sr;
    private Collider2D col;
    private int currentHealth;
    private int raycastMask;
    private Vector3 startPosition;
    private float lastFireTime = -999f;

    public bool IsRevealed => sr != null && sr.enabled;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) sr.enabled = false;

        currentHealth = maxHealth;
        startPosition = transform.position;

        int bulletLayer = LayerMask.NameToLayer("Bullet");
        raycastMask = ~(1 << bulletLayer);
    }

    void Update()
    {
        if (sr == null || col == null) return;

        var player = PlayerMovement2D.Instance;
        sr.enabled = player != null && player.IsLightOn && IsLit(player);

        float phase = GetPhase();
        Patrol(phase);
        TryFire(player, phase);
    }

    private float GetPhase()
    {
        float healthFraction = (float)currentHealth / maxHealth;
        if (healthFraction > 0.66f) return 1f;
        if (healthFraction > 0.33f) return 2f;
        return 3f;
    }

    private void Patrol(float phase)
    {
        float t = Time.time * baseMoveSpeed * phase;
        Vector3 pos = startPosition;
        pos.x += Mathf.Sin(t) * moveRangeX;
        transform.position = pos;
    }

    private void TryFire(PlayerMovement2D player, float phase)
    {
        if (player == null || bulletPrefab == null) return;

        float cooldown = baseFireCooldown / phase;
        if (Time.time - lastFireTime < cooldown) return;

        Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        BossBullet2D bullet = bulletObj.GetComponent<BossBullet2D>();
        if (bullet != null)
        {
            bullet.Init(direction, baseBulletSpeed * (1f + 0.2f * (phase - 1f)));
        }

        lastFireTime = Time.time;
    }

    public void TakeDamage(int amount)
    {
        if (!IsRevealed) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
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
