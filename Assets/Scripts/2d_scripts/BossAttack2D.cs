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
    [Tooltip("The claw swing IS the firing motion in phase 2 - bullets leave on the impact frame.")]
    public bool phase2FiresOnClaw = true;
    public int phase2ClawImpactFrame = 5;
    public float phase2ClawFrameStep = 0.07f;
    public float phase2Telegraph = 0.6f;
    public int aimedSpreadCount = 5;
    public float aimedSpreadAngle = 40f;
    public float aimedBulletSpeed = 5.5f;
    public int ringCount = 8;
    public float ringBulletSpeed = 4f;
    // Phase 2 cycles these in order, one per claw swing. Few, well-spaced streams
    // beat a dense wall: at the range she rests from, bullets under ~1.6 apart
    // leave no gap the player can physically fit through.
    public BulletFan2D[] phase2Fans;

    [Tooltip("The wide pattern is a fan too, not a ring - she throws them, so nothing should fly out behind her.")]
    public float ringSpreadAngle = 120f;
    [Header("Phase 2 hit box")]
    // Her collider was authored for the small phase 1 sprite. Blowing her up to
    // fill the screen drags that collider far below the arena, so the beam can
    // never touch it. Refit it to the drawn body when phase 2 starts.
    public bool fitColliderToPhase2Art = true;
    [Tooltip("Centre of the drawn body inside the frame. Measured: 50% across, 43% up.")]
    public Vector2 phase2HitCentre01 = new Vector2(0.5f, 0.43f);
    [Tooltip("Hit radius as a fraction of the frame width.")]
    public float phase2HitRadius01 = 0.28f;

    [Header("Phase 2 tired window")]
    // She is untouchable while throwing. The opening is the breather afterwards:
    // she drops to a shadow he has to light up before he can hit anything.
    // Without this the phase is over in twenty seconds.
    [Tooltip("How many fans she throws before she needs a rest.")]
    public int attacksBeforeTired = 2;
    public float tiredDuration = 4f;
    [Tooltip("Carves the real picture out of the shadow wherever the beam lands, instead of flipping the whole sprite at once.")]
    public bool tiredUsesSilhouette = true;
    [Tooltip("What she looks like while resting and unlit.")]
    public Color tiredSilhouette = Color.black;
    [Tooltip("What the beam reveals - this is the window where shots land.")]
    public Color tiredRevealed = new Color(1f, 0.92f, 0.65f);
    [Tooltip("Her normal phase 2 colour while attacking.")]
    public Color phase2ActiveTint = Color.white;
    [TextArea] public string tiredHint = "보스가 지쳤다!  비추고 공격하자";
    [Tooltip("How far she leans toward the player while resting. 0 = stay put.")]
    public float tiredApproach = 0f;
    public float tiredApproachTime = 0.6f;

    [Tooltip("Fan centre when she is not aiming: straight out from her, toward the platforms.")]
    public float fanBaseAngle = 180f;
    [Tooltip("How much the fan leans toward the player. 0 = always straight out, 1 = fully aimed.")]
    [Range(0f, 1f)] public float fanAimBlend = 0.6f;

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

        if (phase == 2 && bossRef != null)
        {
            FitHitBoxToArt();
            bossRef.Invulnerable = true;
            bossRef.requireLightToDamage = false;
            bossRef.baseTint = phase2ActiveTint;
            if (sr != null) sr.color = phase2ActiveTint;
        }

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
                yield return ClawWindUp();

                if (phase2Fans != null && phase2Fans.Length > 0)
                {
                    yield return FanRoutine(phase2Fans[(phase2PatternIndex - 1) % phase2Fans.Length]);
                }
                else if (phase2PatternIndex % 2 == 1) yield return AimedSpreadRoutine(player);
                else yield return RingBurstRoutine();

                yield return ClawFollowThrough();

                if (attacksBeforeTired > 0 && phase2PatternIndex % attacksBeforeTired == 0)
                {
                    yield return TiredRoutine();
                }
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

    // Rears back, and the shot leaves on the frame the claw lands. Splitting it
    // this way keeps the swing and the bullets reading as one action.
    private IEnumerator ClawWindUp()
    {
        if (!phase2FiresOnClaw || animator == null || clawFrames == null || clawFrames.Length == 0) yield break;

        int impact = Mathf.Clamp(phase2ClawImpactFrame, 0, clawFrames.Length - 1);
        for (int i = 0; i <= impact; i++)
        {
            animator.ShowFrame(clawFrames[i]);
            yield return new WaitForSeconds(phase2ClawFrameStep);
        }
    }

    private IEnumerator ClawFollowThrough()
    {
        if (!phase2FiresOnClaw || animator == null || clawFrames == null || clawFrames.Length == 0) yield break;

        int impact = Mathf.Clamp(phase2ClawImpactFrame, 0, clawFrames.Length - 1);
        for (int i = impact + 1; i < clawFrames.Length; i++)
        {
            animator.ShowFrame(clawFrames[i]);
            yield return new WaitForSeconds(phase2ClawFrameStep);
        }
        animator.ReleaseFrame();
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

        float baseAngle = FanCentreAngle();

        for (int i = 0; i < aimedSpreadCount; i++)
        {
            float t = (aimedSpreadCount == 1) ? 0f : (i / (float)(aimedSpreadCount - 1)) * 2f - 1f;
            SpawnBullet(baseAngle + t * aimedSpreadAngle * 0.5f, aimedBulletSpeed);
        }
        yield return null;
    }

    // The wide one. Still a fan, just a broader sweep - a full ring would send
    // half the bullets out of the back of a boss that never turns around.
    private IEnumerator RingBurstRoutine()
    {
        yield return TelegraphRoutine(phase2Telegraph);

        float baseAngle = FanCentreAngle();
        for (int i = 0; i < ringCount; i++)
        {
            float t = (ringCount == 1) ? 0f : (i / (float)(ringCount - 1)) * 2f - 1f;
            SpawnBullet(baseAngle + t * ringSpreadAngle * 0.5f, ringBulletSpeed);
        }
        yield return null;
    }

    private void FitHitBoxToArt()
    {
        if (!fitColliderToPhase2Art || sr == null || sr.sprite == null) return;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null) return;

        // local frame, pivot at bottom centre
        Vector2 frame = sr.sprite.bounds.size;
        circle.offset = new Vector2((phase2HitCentre01.x - 0.5f) * frame.x, phase2HitCentre01.y * frame.y);
        circle.radius = phase2HitRadius01 * frame.x;
    }

    // The breather. She can only be hurt here, and only where the beam falls.
    private IEnumerator TiredRoutine()
    {
        if (bossRef == null) yield break;

        Vector3 home = transform.position;
        if (tiredApproach != 0f) yield return SlideBoss(home, home + Vector3.left * tiredApproach, tiredApproachTime);

        LightSilhouette2D shadow = GetComponent<LightSilhouette2D>();
        if (tiredUsesSilhouette && shadow != null)
        {
            // the mask does the reveal, so the body keeps its own colours
            shadow.silhouetteColor = tiredSilhouette;
            shadow.Active = true;
            bossRef.baseTint = phase2ActiveTint;
            bossRef.weakPointColor = phase2ActiveTint;
            if (sr != null) sr.color = phase2ActiveTint;
        }
        else
        {
            bossRef.baseTint = tiredSilhouette;
            bossRef.weakPointColor = tiredRevealed;
            if (sr != null) sr.color = tiredSilhouette;
        }

        bossRef.requireLightToDamage = true;
        bossRef.Invulnerable = false;

        if (!string.IsNullOrEmpty(tiredHint))
        {
            ScreenHint2D hint = GetComponent<ScreenHint2D>();
            if (hint == null) hint = gameObject.AddComponent<ScreenHint2D>();
            hint.Show(tiredHint, tiredDuration);
        }

        yield return new WaitForSeconds(tiredDuration);

        if (shadow != null) shadow.Active = false;
        bossRef.requireLightToDamage = false;
        bossRef.Invulnerable = true;
        bossRef.baseTint = phase2ActiveTint;
        if (sr != null) sr.color = phase2ActiveTint;

        if (tiredApproach != 0f) yield return SlideBoss(transform.position, home, tiredApproachTime);
    }

    private IEnumerator SlideBoss(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        transform.position = to;
    }

    private IEnumerator FanRoutine(BulletFan2D fan)
    {
        if (fan == null) yield break;
        yield return TelegraphRoutine(phase2Telegraph);

        float baseAngle = FanCentreAngle(fan.aimBlend);
        int n = Mathf.Max(1, fan.count);
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (i / (float)(n - 1)) * 2f - 1f;
            SpawnBullet(baseAngle + t * fan.spreadAngle * 0.5f, fan.speed);
        }
        yield return null;
    }

    // Leans toward the player without ever swinging behind her.
    private float FanCentreAngle()
    {
        return FanCentreAngle(fanAimBlend);
    }

    private float FanCentreAngle(float blend)
    {
        var player = PlayerMovement2D.Instance;
        if (player == null) return fanBaseAngle;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)firePoint.position;
        float aimed = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        return Mathf.LerpAngle(fanBaseAngle, aimed, blend);
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
