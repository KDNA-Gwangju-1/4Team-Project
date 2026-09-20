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
    [Tooltip("Her default in phase 2 is to stalk the floor and swipe, Bloody Queen style. Every Nth attack she takes off for the barrage instead.")]
    public int airBarrageEvery = 4;
    [Tooltip("How long she hangs in the air before the barrage lands.")]
    public float airRiseTime = 0.8f;
    public float airBarrageHeight = 6f;
    public float airReturnTime = 0.7f;
    [Tooltip("She has to be this close to swipe. Further away she closes the gap first.")]
    public float phase2ClawRange = 4.5f;

    [Header("Phase 2 dash swipe")]
    // Her walk is slow on purpose, so the one time she moves fast has to be
    // announced. The blink is the tell; miss it and the dash connects.
    [Tooltip("Every Nth ground attack becomes the dash instead of a standing swipe.")]
    public int dashSwipeEvery = 3;
    public int dashBlinkCount = 3;
    public float dashBlinkTime = 0.12f;
    public Color dashBlinkColor = new Color(1f, 0.35f, 0.35f);
    public float dashSpeed = 26f;
    [Tooltip("How far past the player she carries through.")]
    public float dashOvershoot = 3f;
    public float dashRecover = 0.4f;
    [Tooltip("She keeps her tentacles in phase 2 - the arena is the same ground, so the floor threat still works.")]
    public bool phase2UsesTentacles = true;
    [Tooltip("A tentacle pattern every N fans. 1 = after every fan.")]
    public int phase2TentacleEvery = 2;
    public TentaclePattern[] phase2TentaclePatterns = new TentaclePattern[] { TentaclePattern.Domino };
    public int phase2TentacleCount = 2;
    // Phase 1's wave has to stay slower than a running player (5 u/s) or it cannot
    // be outrun at all. Phase 2 is deliberately faster than a run: the answer becomes
    // jumping it rather than racing it, and the air dash is there for that.
    [Tooltip("Override the wave speed in phase 2. Spacing / delay is its travel speed.")]
    public bool phase2OverridesWave = true;
    public float phase2DominoSpacing = 3f;
    public float phase2DominoDelay = 0.5f;
    [Tooltip("Shorter per-tentacle warning to match the quicker wave.")]
    public float phase2DominoWarn = 0.4f;
    [Tooltip("Phase 2 only: light the whole wave path first. Phase 1 keeps its per-tentacle markers.")]
    public bool phase2WarnsWholePath = true;
    public float phase2PathWarnDuration = 1.6f;

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
    [Tooltip("Rest on a clock, not a count. She stays airborne throwing patterns and comes down on this interval.")]
    public float tiredIntervalSeconds = 15f;
    [Tooltip("Legacy count-based trigger. 0 = off, use the interval instead.")]
    public int attacksBeforeTired = 0;
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
    [Tooltip("She drops out of the sky to rest on the floor - a boss you have to light up should not be hovering out of reach.")]
    public bool tiredLandsOnFloor = true;
    public float tiredLandY = -2.63f;
    public float tiredLandTime = 0.8f;
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
    private float lastTiredTime;
    private bool isTired;
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

            lastTiredTime = Time.time;
            BossFlight2D flight = GetComponent<BossFlight2D>();
            if (flight != null) flight.EnterAir();
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

                // she stays up there; the only ground time is the rest
                if (phase2Fans != null && phase2Fans.Length > 0)
                {
                    yield return FanRoutine(phase2Fans[(phase2PatternIndex - 1) % phase2Fans.Length]);
                }
                else
                {
                    yield return AimedSpreadRoutine(player);
                }

                if (!isTired && phase2UsesTentacles && strikeField != null && strikeField.HasPoints
                    && phase2TentacleEvery > 0 && phase2PatternIndex % phase2TentacleEvery == 0
                    && phase2TentaclePatterns != null && phase2TentaclePatterns.Length > 0)
                {
                    TentaclePattern shape = phase2TentaclePatterns[waveCounter % phase2TentaclePatterns.Length];
                    waveCounter++;

                    float keepSpacing = strikeField.dominoSpacing;
                    float keepDelay = strikeField.dominoDelay;
                    float keepWarn = strikeField.dominoWarnDuration;
                    bool keepPathWarn = strikeField.dominoWarnsWholePath;
                    float keepPathTime = strikeField.dominoPathWarnDuration;
                    if (phase2OverridesWave)
                    {
                        strikeField.dominoSpacing = phase2DominoSpacing;
                        strikeField.dominoDelay = phase2DominoDelay;
                        strikeField.dominoWarnDuration = phase2DominoWarn;
                        strikeField.dominoWarnsWholePath = phase2WarnsWholePath;
                        strikeField.dominoPathWarnDuration = phase2PathWarnDuration;
                    }

                    yield return strikeField.RunPattern(shape, phase2TentacleCount);

                    strikeField.dominoSpacing = keepSpacing;
                    strikeField.dominoDelay = keepDelay;
                    strikeField.dominoWarnDuration = keepWarn;
                    strikeField.dominoWarnsWholePath = keepPathWarn;
                    strikeField.dominoPathWarnDuration = keepPathTime;
                }

                yield return ClawFollowThrough();

                bool byCount = attacksBeforeTired > 0 && phase2PatternIndex % attacksBeforeTired == 0;
                bool byClock = tiredIntervalSeconds > 0f && Time.time - lastTiredTime >= tiredIntervalSeconds;
                if (byCount || byClock)
                {
                    yield return TiredRoutine();
                    lastTiredTime = Time.time;
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

        if (tiredLandsOnFloor)
        {
            var target = PlayerMovement2D.Instance;
            float landX = transform.position.x;
            if (target != null && tiredApproach != 0f)
            {
                landX = Mathf.MoveTowards(landX, target.transform.position.x, tiredApproach);
            }
            yield return SlideBoss(transform.position, new Vector3(landX, tiredLandY, home.z), tiredLandTime);
        }
        else if (tiredApproach != 0f)
        {
            yield return SlideBoss(home, home + Vector3.left * tiredApproach, tiredApproachTime);
        }

        // no floor waves while she is down here - the player needs the ground free
        // to stand on and aim, and she is not the one throwing them right now
        isTired = true;
        ClearLiveTentacles();

        // she has to hold still to be shot at
        BossFlight2D flight = GetComponent<BossFlight2D>();
        if (flight != null) flight.Stop();

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

        // back into the air before she starts throwing again
        if (tiredLandsOnFloor || tiredApproach != 0f)
        {
            yield return SlideBoss(transform.position, home, tiredLandTime);
        }
        isTired = false;
        if (flight != null) flight.EnterAir();
    }

    private static void ClearLiveTentacles()
    {
        TentacleStrike2D[] live = UnityEngine.Object.FindObjectsOfType<TentacleStrike2D>();
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] != null) Destroy(live[i].gameObject);
        }
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

    // Her bread and butter in phase 2: close the gap on the floor and swipe.
    private IEnumerator GroundSwipeRoutine(PlayerMovement2D player)
    {
        BossFlight2D flight = GetComponent<BossFlight2D>();
        if (flight != null && flight.mode != BossFlight2D.Mode.Ground) flight.EnterGround();

        if (player != null)
        {
            // give her a moment to get within reach rather than swiping at air
            float waited = 0f;
            while (waited < 1.5f && Mathf.Abs(player.transform.position.x - transform.position.x) > phase2ClawRange)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        if (flight != null) flight.Stop();
        yield return ClawRoutine();
        if (flight != null) flight.EnterGround();
    }

    // Blink on the spot, then cross the arena with the claw out.
    private IEnumerator DashSwipeRoutine(PlayerMovement2D player)
    {
        BossFlight2D flight = GetComponent<BossFlight2D>();
        if (flight != null) flight.Stop();

        // the tell
        Color normal = sr != null ? sr.color : Color.white;
        for (int i = 0; i < dashBlinkCount; i++)
        {
            if (sr != null) sr.color = dashBlinkColor;
            yield return new WaitForSeconds(dashBlinkTime);
            if (sr != null) sr.color = normal;
            yield return new WaitForSeconds(dashBlinkTime);
        }

        if (player == null)
        {
            if (flight != null) flight.EnterGround();
            yield break;
        }

        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        if (Mathf.Approximately(dir, 0f)) dir = -1f;
        float targetX = player.transform.position.x + dir * dashOvershoot;

        if (sr != null) sr.flipX = dir > 0f;
        if (animator != null && clawFrames != null && clawFrames.Length > 0)
        {
            animator.ShowFrame(clawFrames[Mathf.Clamp(phase2ClawImpactFrame, 0, clawFrames.Length - 1)]);
        }
        if (clawHitbox != null) clawHitbox.SetHitboxActive(true);

        while (Mathf.Abs(targetX - transform.position.x) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                new Vector3(targetX, transform.position.y, transform.position.z), dashSpeed * Time.deltaTime);
            yield return null;
        }

        if (clawHitbox != null) clawHitbox.SetHitboxActive(false);
        if (animator != null) animator.ReleaseFrame();
        if (sr != null) sr.color = normal;

        yield return new WaitForSeconds(dashRecover);
        if (flight != null) flight.EnterGround();
    }

    // The one big pattern: she lifts off and empties everything at once.
    private IEnumerator AirBarrageRoutine()
    {
        BossFlight2D flight = GetComponent<BossFlight2D>();
        if (flight != null) flight.Stop();

        Vector3 ground = transform.position;
        Vector3 apex = new Vector3(ground.x, ground.y + airBarrageHeight, ground.z);
        yield return SlideBoss(ground, apex, airRiseTime);

        if (phase2Fans != null)
        {
            for (int i = 0; i < phase2Fans.Length; i++)
            {
                yield return FanRoutine(phase2Fans[i]);
            }
        }

        yield return SlideBoss(transform.position, ground, airReturnTime);
        if (flight != null) flight.EnterGround();
    }

    private IEnumerator FanRoutine(BulletFan2D fan)
    {
        if (fan == null) yield break;
        yield return TelegraphRoutine(phase2Telegraph);

        int n = Mathf.Max(1, fan.count);
        int waves = Mathf.Max(1, fan.waves);

        for (int w = 0; w < waves; w++)
        {
            // re-aim on every wave, so standing still is punished
            float baseAngle = FanCentreAngle(fan.aimBlend) + w * fan.waveAngleStep;
            float speed = Mathf.Max(0.5f, fan.speed + w * fan.waveSpeedStep);

            for (int i = 0; i < n; i++)
            {
                // sweeping runs edge to edge; otherwise it opens from the middle out
                int index = fan.sweep ? i : OutwardIndex(i, n);
                float t = (n == 1) ? 0f : (index / (float)(n - 1)) * 2f - 1f;
                SpawnBullet(baseAngle + t * fan.spreadAngle * 0.5f, speed);

                if (fan.stagger > 0f && i < n - 1) yield return new WaitForSeconds(fan.stagger);
            }

            if (w < waves - 1) yield return new WaitForSeconds(fan.waveInterval);
        }
    }

    // 0, n-1, 1, n-2, ... so the fan blooms outward from its centre line
    private static int OutwardIndex(int i, int n)
    {
        int mid = n / 2;
        int step = (i + 1) / 2;
        return (i % 2 == 0) ? Mathf.Clamp(mid - step, 0, n - 1) : Mathf.Clamp(mid + step, 0, n - 1);
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
