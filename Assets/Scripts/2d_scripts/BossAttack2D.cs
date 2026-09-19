using System.Collections;
using UnityEngine;

[System.Serializable]
public class TentacleWave2D
{
    public string label = "stage";
    public int fromBossHealth = 99;   // applies once boss health drops to or below this
    // cycled in order, so a stage can mix shapes instead of repeating one
    public TentaclePattern[] patterns = new TentaclePattern[] { TentaclePattern.Single };
    public int tentacleCount = 1;
    public float waveInterval = 1.5f;
    public float warnDuration = 1.2f;
}

public class BossAttack2D : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab;
    public MonsterSpriteAnimator2D animator;

    public Sprite[] clawFrames;
    public float clawFrameDuration = 0.08f;

    public float phase1Interval = 3.2f;
    public float clawRange = 6f;
    public float clawTelegraph = 0.8f;
    public float clawActiveTime = 0.25f;
    public BossClawHitbox2D clawHitbox;
    public float tendrilBulletSpeed = 6f;
    public int tendrilBulletCount = 2;
    public float tendrilSpreadAngle = 14f;

    public TentacleStrikeField2D strikeField;
    public int strikeCount = 2;
    public TentacleWave2D[] phase1Waves;

    public float phase2Interval = 2.4f;
    public float phase2Telegraph = 0.6f;
    public int aimedSpreadCount = 5;
    public float aimedSpreadAngle = 40f;
    public float aimedBulletSpeed = 5.5f;
    public int ringCount = 8;
    public float ringBulletSpeed = 4f;

    public Color telegraphColor = new Color(1f, 0.55f, 0.55f);

    private SpriteRenderer sr;
    private Boss2D bossRef;
    private Coroutine loopRoutine;
    private int phase;
    private int phase2PatternIndex;
    private int waveCounter;

    public int Phase => phase;
    private Color BaseColor => bossRef != null ? bossRef.BaseColor : Color.white;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        bossRef = GetComponent<Boss2D>();
        if (firePoint == null) firePoint = transform;
    }

    public void SetPhase(int newPhase)
    {
        phase = newPhase;
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
        if (clawHitbox != null) clawHitbox.SetHitboxActive(false);
        if (sr != null) sr.color = BaseColor;

        if (phase > 0) loopRoutine = StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            TentacleWave2D wave = (phase == 1) ? CurrentWave() : null;
            float interval = (phase != 1) ? phase2Interval
                : (wave != null ? wave.waveInterval : phase1Interval);
            yield return new WaitForSeconds(interval);

            var player = PlayerMovement2D.Instance;
            if (player == null) continue;

            if (phase == 1)
            {
                if (strikeField != null && strikeField.HasPoints)
                {
                    int count = wave != null ? wave.tentacleCount : strikeCount;
                    TentaclePattern shape = TentaclePattern.Single;
                    if (wave != null && wave.patterns != null && wave.patterns.Length > 0)
                    {
                        shape = wave.patterns[waveCounter % wave.patterns.Length];
                    }
                    waveCounter++;
                    if (wave != null) strikeField.warnDuration = wave.warnDuration;
                    yield return strikeField.RunPattern(shape, count);
                }
                else
                {
                    float distance = Vector2.Distance(transform.position, player.transform.position);
                    if (distance <= clawRange) yield return ClawRoutine();
                    else yield return TendrilRoutine(player);
                }
            }
            else
            {
                phase2PatternIndex++;
                if (phase2PatternIndex % 2 == 1) yield return AimedSpreadRoutine(player);
                else yield return RingBurstRoutine();
            }
        }
    }

    private TentacleWave2D CurrentWave()
    {
        if (phase1Waves == null || phase1Waves.Length == 0) return null;

        int health = bossRef != null ? bossRef.CurrentHealth : 0;
        TentacleWave2D chosen = phase1Waves[0];
        for (int i = 0; i < phase1Waves.Length; i++)
        {
            if (health <= phase1Waves[i].fromBossHealth) chosen = phase1Waves[i];
        }
        return chosen;
    }

    private IEnumerator TelegraphRoutine(float duration)
    {
        if (sr == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        float blink = 0.12f;
        while (elapsed < duration)
        {
            sr.color = telegraphColor;
            yield return new WaitForSeconds(blink);
            sr.color = BaseColor;
            yield return new WaitForSeconds(blink);
            elapsed += blink * 2f;
        }
        sr.color = BaseColor;
    }

    private IEnumerator ClawRoutine()
    {
        yield return TelegraphRoutine(clawTelegraph);

        if (animator != null && clawFrames != null && clawFrames.Length > 0)
        {
            StartCoroutine(animator.PlayOneShotRoutine(clawFrames, clawFrameDuration));
        }

        if (clawHitbox != null)
        {
            clawHitbox.SetHitboxActive(true);
            yield return new WaitForSeconds(clawActiveTime);
            clawHitbox.SetHitboxActive(false);
        }
    }

    private IEnumerator TendrilRoutine(PlayerMovement2D player)
    {
        yield return TelegraphRoutine(clawTelegraph * 0.6f);

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)firePoint.position;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        for (int i = 0; i < tendrilBulletCount; i++)
        {
            float t = (tendrilBulletCount == 1) ? 0f : (i / (float)(tendrilBulletCount - 1)) * 2f - 1f;
            SpawnBullet(baseAngle + t * tendrilSpreadAngle, tendrilBulletSpeed);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator AimedSpreadRoutine(PlayerMovement2D player)
    {
        yield return TelegraphRoutine(phase2Telegraph);

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)firePoint.position;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        for (int i = 0; i < aimedSpreadCount; i++)
        {
            float t = (aimedSpreadCount == 1) ? 0f : (i / (float)(aimedSpreadCount - 1)) * 2f - 1f;
            SpawnBullet(baseAngle + t * aimedSpreadAngle * 0.5f, aimedBulletSpeed);
        }
        yield return null;
    }

    private IEnumerator RingBurstRoutine()
    {
        yield return TelegraphRoutine(phase2Telegraph);

        float step = 360f / Mathf.Max(1, ringCount);
        float offset = Random.Range(0f, step);
        for (int i = 0; i < ringCount; i++)
        {
            SpawnBullet(offset + i * step, ringBulletSpeed);
        }
        yield return null;
    }

    private void SpawnBullet(float angleDegrees, float speed)
    {
        if (bulletPrefab == null) return;

        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        GameObject obj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        BossBullet2D bullet = obj.GetComponent<BossBullet2D>();
        if (bullet != null) bullet.Init(dir, speed);
    }
}
