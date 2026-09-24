using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement2D : MonoBehaviour
{
    public static PlayerMovement2D Instance { get; private set; }

    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.1f;
    [Tooltip("Downward kick when dropping through a platform, so he clears it even from a standstill.")]
    public float dropThroughNudge = 2f;
    public LayerMask groundLayer;

    public float wallCheckDistance = 0.6f;
    public float wallJumpUpForce = 7f;
    public float wallJumpPushForce = 6f;
    public float wallJumpLockDuration = 0.2f;
    public float wallJumpBoostSpeed = 8f;
    public float wallJumpBoostDuration = 0.3f;
    public LayerMask wallLayer;

    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.55f;
    [Tooltip("Invulnerable for this long after the dash ends, so finishing next to something is not a free hit.")]
    public float postDashInvincibility = 0.25f;
    [Tooltip("Air dashes keep a little gravity so they do not float.")]
    public float dashGravityScale = 0.15f;

    [Tooltip("How many dashes can be banked at once.")]
    public int dashStaminaMax = 3;
    [Tooltip("Seconds to earn one dash back.")]
    public float dashStaminaRegenTime = 2.8f;
    public bool allowAirDash = false;

    public Transform flashlight;
    public float lightRange = 3f;
    public float lightMaxSeconds = 10f;
    public float lightLockoutSeconds = 7f;
    public float lightHalfAngle = 15f;
    [Tooltip("Left on, the beam starts from wherever HeldFlashlightVisual2D puts the flashlight, so the cone and the prop can never drift apart.")]
    public bool beamFollowsHeldFlashlight = true;
    public Vector2 flashlightHandOffset = new Vector2(0.4f, 0.1f);
    public Vector2 jumpFlashlightHandOffset = new Vector2(0.75f, 0.15f);
    [Tooltip("How far past the lamp head the shot appears, so it reads as coming out of the flashlight.")]
    public float muzzleForward = 0.45f;

    public static bool LanternObtained = false;
    public static float? PendingSpawnX = null;
    public static int? CarriedHealth = null;

    /// <summary>
    /// Call when the 2D chapter starts over (new game, or entering it from the bright dream).
    /// Statics survive scene loads, so without this a second run starts with the lantern
    /// already picked up and the tutorial skipped.
    /// </summary>
    public static void ResetChapterState()
    {
        LanternObtained = false;
        PendingSpawnX = null;
        CarriedHealth = null;
        BossPhaseController2D.ResumeAtPhase2 = false;
        Stage2IntroCutscene.SkipIntroOnce = false;
        Stage3BossIntroCutscene.SkipIntroOnce = false;
    }

    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletMaxDistance = 8f;
    public float fireCooldown = 1f;

    public int maxHealth = 10;
    public float invincibilityDuration = 2f;
    public float invincibilityBlinkInterval = 0.1f;
    public string sceneOnDeath = "";

    public float fallRespawnY = -6f;
    public float checkpointEdgeMargin = 1f;

    public bool clampToBounds = false;
    public float minX;
    public float maxX;

    private Rigidbody2D rb;
    private Vector3 lastGroundedPosition;
    private bool jumpRequested;
    private bool wallJumpRequested;
    private float wallJumpDirection;
    private bool touchingWallLeft;
    private bool touchingWallRight;
    private float wallJumpLockTimer;
    private float wallJumpBoostTimer;
    private float wallJumpBoostDirection;
    private Collider2D pendingWallJumpCollider;
    private Collider2D usedWallCollider;
    private bool dashRequested;
    private bool isDashing;
    private float dashGraceTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashDirection;
    private float dashStamina;
    private int facingDirection = 1;
    private bool grounded;
    private SpriteRenderer flashlightRenderer;
    private HeldFlashlightVisual2D heldVisual;
    private SpriteMask flashlightMask;
    private Vector2 lightDirection = Vector2.right;
    private float lastFireTime = -999f;
    private SpriteRenderer sr;
    private int currentHealth;
    private bool isInvincible;
    private bool hasLantern;
    private bool respawnAnchorLocked;
    private float lightCharge;
    private bool lightLocked;

    public bool IsLightOn => flashlightRenderer != null && flashlightRenderer.enabled;
    public float LightCharge01 => lightMaxSeconds > 0f ? Mathf.Clamp01(lightCharge / lightMaxSeconds) : 0f;
    public bool LightLocked => lightLocked;
    public Vector2 LightOrigin => transform.position;
    public Vector2 LightDirection => lightDirection;
    public int CurrentHealth => currentHealth;
    public bool IsGrounded => grounded;
    public bool IsDashing => isDashing;
    public bool DashGraceActive => dashGraceTimer > 0f;
    // Cutscenes take control away, so anything still in flight would land as a
    // free hit the player had no way to avoid.
    public bool CutsceneInvulnerable { get; set; }
    public float DashStamina => dashStamina;
    public int DashChargesReady => Mathf.FloorToInt(dashStamina);
    public bool CanDash => dashStamina >= 1f && dashCooldownTimer <= 0f && !isDashing;
    public float DashDirection => dashDirection;
    public event System.Action OnDashStarted;
    public event System.Action OnBulletFired;
    public event System.Action OnHurt;
    public int FacingDirection => facingDirection;

    // 입력은 Update에서 예약하고 FixedUpdate에서 실행한다. 컷신이 이 컴포넌트를 끄면 예약만 남아
    // 있다가 다시 켜질 때 터진다 - 카메라가 훑는 동안 누른 점프가 컷신이 끝나자마자 실행됐다.
    private int enabledFrame = -1;

    void OnEnable()
    {
        enabledFrame = Time.frameCount;
    }

    void OnDisable()
    {
        jumpRequested = false;
        wallJumpRequested = false;
        dashRequested = false;
        // a cutscene that disables us mid-aim used to leave the beam on (and still revealing
        // monsters) for the whole scene; cutscenes that want it call CutsceneSetLight afterwards
        if (flashlightRenderer != null) flashlightRenderer.enabled = false;
        if (flashlightMask != null) flashlightMask.enabled = false;
    }

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        // 앞 스테이지 최대 체력이 더 크면 하트 개수보다 많은 체력을 들고 와서, 몇 대는 맞아도 하트가 안 줄어든다
        currentHealth = Mathf.Min(CarriedHealth ?? maxHealth, maxHealth);
        CarriedHealth = null;
        hasLantern = LanternObtained;

        if (PendingSpawnX.HasValue)
        {
            Vector3 pos = transform.position;
            pos.x = PendingSpawnX.Value;
            transform.position = pos;
            PendingSpawnX = null;
        }

        lastGroundedPosition = transform.position;
        lightCharge = lightMaxSeconds;
        dashStamina = dashStaminaMax;
        if (flashlight != null)
        {
            heldVisual = GetComponentInChildren<HeldFlashlightVisual2D>(true);
            flashlightRenderer = flashlight.GetComponent<SpriteRenderer>();
            if (flashlightRenderer != null) flashlightRenderer.enabled = false;
            flashlightMask = flashlight.GetComponent<SpriteMask>();
            if (flashlightMask != null) flashlightMask.enabled = false;
        }
    }

    void Update()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (dashGraceTimer > 0f) dashGraceTimer -= Time.deltaTime;

        if (!isDashing && dashStamina < dashStaminaMax)
        {
            dashStamina = Mathf.Min(dashStaminaMax, dashStamina + Time.deltaTime / Mathf.Max(0.01f, dashStaminaRegenTime));
        }

        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        grounded = groundHit.collider != null;

        RaycastHit2D wallHitRight = CastForWall(Vector2.right);
        RaycastHit2D wallHitLeft = CastForWall(Vector2.left);
        touchingWallRight = wallHitRight.collider != null;
        touchingWallLeft = wallHitLeft.collider != null;

        var keyboard = Keyboard.current;
        // Space also advances cutscene dialogue; the press that closes the last line must not
        // also jump on the frame the cutscene hands control back
        if (keyboard != null && Time.frameCount != enabledFrame)
        {
            if (keyboard.dKey.wasPressedThisFrame) facingDirection = 1;
            if (keyboard.aKey.wasPressedThisFrame) facingDirection = -1;

            if ((keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                && (grounded || allowAirDash) && !isDashing && dashCooldownTimer <= 0f
                && dashStamina >= 1f)
            {
                dashRequested = true;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                bool holdingDown = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
                DropThroughPlatform2D dropper = (grounded && holdingDown && groundHit.collider != null)
                    ? groundHit.collider.GetComponent<DropThroughPlatform2D>() : null;

                if (dropper != null)
                {
                    // down + jump falls to the platform below instead of jumping
                    StartCoroutine(DropThroughRoutine(groundHit.collider, dropper.passThroughTime));
                }
                else if (grounded)
                {
                    jumpRequested = true;
                }
                else if (touchingWallRight && wallHitRight.collider != usedWallCollider)
                {
                    wallJumpRequested = true;
                    wallJumpDirection = -1f;
                    pendingWallJumpCollider = wallHitRight.collider;
                }
                else if (touchingWallLeft && wallHitLeft.collider != usedWallCollider)
                {
                    wallJumpRequested = true;
                    wallJumpDirection = 1f;
                    pendingWallJumpCollider = wallHitLeft.collider;
                }
            }
        }

        if (grounded)
        {
            usedWallCollider = null;
            // 안 보이는 발판 위에서 되살아나면 어디 서 있는지 모른다 - 직전의 보이는 발판으로 보낸다
            if (!respawnAnchorLocked && groundHit.collider.GetComponent<LightRevealPlatform2D>() == null)
            {
                lastGroundedPosition = ComputeCheckpoint(groundHit.collider);
            }
        }
        else if (transform.position.y < fallRespawnY)
        {
            Respawn();
        }

        if (clampToBounds)
        {
            ClampToBounds();
        }

        UpdateFlashlight();
        UpdateShooting();
    }

    private void ClampToBounds()
    {
        Vector3 pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        if (clampedX == pos.x) return;

        pos.x = clampedX;
        transform.position = pos;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private static readonly float[] WallProbeHeights = { 0.8f, 0.4f, 0f, -0.4f, -0.8f };

    // Turns off collision with just that one platform for a moment. Disabling the
    // platform's collider outright would drop anything else standing on it too.
    private System.Collections.IEnumerator DropThroughRoutine(Collider2D platform, float duration)
    {
        Collider2D self = GetComponent<Collider2D>();
        if (self == null || platform == null) yield break;

        Physics2D.IgnoreCollision(self, platform, true);
        grounded = false;

        // a nudge down so he leaves the surface even when standing perfectly still
        if (rb != null && rb.linearVelocity.y > -0.1f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropThroughNudge);
        }

        yield return new WaitForSeconds(duration);

        if (self != null && platform != null) Physics2D.IgnoreCollision(self, platform, false);
    }

    private RaycastHit2D CastForWall(Vector2 direction)
    {
        foreach (float heightOffset in WallProbeHeights)
        {
            Vector2 origin = (Vector2)transform.position + Vector2.up * heightOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, wallLayer);
            if (hit.collider != null) return hit;
        }
        return default;
    }

    private Vector3 ComputeCheckpoint(Collider2D floorCollider)
    {
        Bounds bounds = floorCollider.bounds;
        float minX = bounds.min.x + checkpointEdgeMargin;
        float maxX = bounds.max.x - checkpointEdgeMargin;
        float x = transform.position.x;

        if (minX <= maxX)
        {
            x = Mathf.Clamp(x, minX, maxX);
        }

        return new Vector3(x, transform.position.y, transform.position.z);
    }

    public void SetGravityScale(float scale)
    {
        if (rb != null) rb.gravityScale = scale;
    }

    public void SetRespawnAnchor(Vector3 position)
    {
        lastGroundedPosition = position;
        respawnAnchorLocked = true;
    }

    public void ClearRespawnAnchor()
    {
        respawnAnchorLocked = false;
    }

    public void AddImpulse(Vector2 impulse)
    {
        if (rb == null) return;
        Vector2 velocity = rb.linearVelocity;
        velocity += impulse;
        rb.linearVelocity = velocity;
    }

    public void Respawn()
    {
        transform.position = lastGroundedPosition;
        rb.linearVelocity = Vector2.zero;
    }

    private Vector2 GetMouseDirection(Mouse mouse)
    {
        if (Camera.main == null) return Vector2.right;

        Vector2 mouseScreenPos = mouse.position.ReadValue();
        float distance = transform.position.z - Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, distance));

        Vector2 direction = (Vector2)mouseWorldPos - (Vector2)transform.position;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
    }

    public bool HasLantern => hasLantern;

    public void PickUpLantern()
    {
        hasLantern = true;
        LanternObtained = true;
    }

    void UpdateFlashlight()
    {
        if (flashlight == null || flashlightRenderer == null) return;

        var mouse = Mouse.current;
        bool wantsLight = hasLantern && mouse != null && mouse.rightButton.isPressed;

        // the lamp runs dry after lightMaxSeconds and stays dead until it has
        // fully recharged, which takes lightLockoutSeconds
        bool aiming = wantsLight && !lightLocked && lightCharge > 0f;
        if (aiming)
        {
            lightCharge -= Time.deltaTime;
            if (lightCharge <= 0f)
            {
                lightCharge = 0f;
                lightLocked = true;
                aiming = false;
            }
        }
        else if (lightCharge < lightMaxSeconds)
        {
            float rechargeRate = lightMaxSeconds / Mathf.Max(0.01f, lightLockoutSeconds);
            lightCharge = Mathf.Min(lightMaxSeconds, lightCharge + rechargeRate * Time.deltaTime);
            if (lightLocked && lightCharge >= lightMaxSeconds) lightLocked = false;
        }

        flashlightRenderer.enabled = aiming;
        if (flashlightMask != null) flashlightMask.enabled = aiming;

        if (hasLantern && mouse != null)
        {
            PoseFlashlight(GetMouseDirection(mouse));
        }
    }

    // 빛줄기의 위치와 회전. 입력에서 오든 컷신에서 오든 같은 자리에서 같은 각도로 나가야 한다.
    private void PoseFlashlight(Vector2 direction)
    {
        lightDirection = direction;
        float angle = Mathf.Atan2(lightDirection.y, lightDirection.x) * Mathf.Rad2Deg;
        float facing = lightDirection.x >= 0f ? 1f : -1f;
        // The cone and the drawn flashlight used to carry separate offsets, so
        // the beam left from a point that was not the lamp. One number now.
        Vector2 offset = grounded ? flashlightHandOffset : jumpFlashlightHandOffset;
        flashlight.position = transform.position + new Vector3(offset.x * facing, offset.y, 0f);
        if (beamFollowsHeldFlashlight && heldVisual != null && heldVisual.TryGetEmitter(out Vector3 emitter))
            flashlight.position = emitter;
        flashlight.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void UpdateShooting()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && Time.time - lastFireTime >= fireCooldown
            && Time.frameCount != enabledFrame)
        {
            Vector2 direction = GetMouseDirection(mouse);
            FireBullet(direction);
            lastFireTime = Time.time;
        }
    }

    // 컷신은 이 컴포넌트를 꺼두고 진행하므로 입력 경로를 거치지 않고 손전등과 발사를 직접 시킨다.
    public void CutsceneSetLight(bool on, Vector2 direction)
    {
        if (flashlightRenderer != null) flashlightRenderer.enabled = on && hasLantern;
        if (flashlightMask != null) flashlightMask.enabled = on && hasLantern;
        if (flashlight != null && direction.sqrMagnitude > 0.0001f) PoseFlashlight(direction.normalized);
    }

    public Vector3 FlashlightOrigin => flashlight != null ? flashlight.position : transform.position;

    public void CutsceneFire(Vector2 direction)
    {
        FireBullet(direction);
    }

    void FireBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        // fire from the lamp head rather than the player's chest
        Vector3 origin = (flashlight != null) ? flashlight.position : transform.position;
        origin += (Vector3)(direction.normalized * muzzleForward);

        GameObject bulletObj = Instantiate(bulletPrefab, origin, Quaternion.identity);
        Bullet2D bullet = bulletObj.GetComponent<Bullet2D>();
        if (bullet != null)
        {
            bullet.Init(direction, bulletSpeed, bulletMaxDistance);
        }
        if (OnBulletFired != null) OnBulletFired();
    }

    void FixedUpdate()
    {
        float moveInput = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) moveInput -= 1f;
            if (keyboard.dKey.isPressed) moveInput += 1f;
        }

        Vector2 velocity = rb.linearVelocity;

        if (dashRequested)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = facingDirection;
            dashRequested = false;
            dashStamina = Mathf.Max(0f, dashStamina - 1f);
            if (OnDashStarted != null) OnDashStarted();
        }

        if (isDashing)
        {
            velocity.x = dashDirection * dashSpeed;
            // grounded dashes stay flat; air dashes keep a little fall so the
            // player does not hang in the air mid-dash
            velocity.y = grounded ? 0f : velocity.y * dashGravityScale;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                // Contact damage is re-checked by OnTriggerStay the instant the dash
                // ends, so stopping inside something you just dashed through counted
                // as a hit. A short grace makes the dash actually get you out.
                dashGraceTimer = postDashInvincibility;
            }
        }
        else if (wallJumpLockTimer > 0f)
        {
            wallJumpLockTimer -= Time.fixedDeltaTime;
        }
        else if (wallJumpBoostTimer > 0f && moveInput == wallJumpBoostDirection)
        {
            wallJumpBoostTimer -= Time.fixedDeltaTime;
            velocity.x = wallJumpBoostDirection * wallJumpBoostSpeed;
        }
        else
        {
            wallJumpBoostTimer = 0f;
            velocity.x = moveInput * moveSpeed;
        }

        if (jumpRequested)
        {
            velocity.y = jumpForce;
            jumpRequested = false;
        }

        if (wallJumpRequested)
        {
            velocity.y = wallJumpUpForce;
            velocity.x = wallJumpDirection * wallJumpPushForce;
            wallJumpRequested = false;
            wallJumpLockTimer = wallJumpLockDuration;
            wallJumpBoostTimer = wallJumpBoostDuration;
            wallJumpBoostDirection = wallJumpDirection;
            usedWallCollider = pendingWallJumpCollider;
        }

        rb.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeDamageFrom(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryTakeDamageFrom(other);
    }

    private void TryTakeDamageFrom(Collider2D other)
    {
        if (other.GetComponent<Monster2D>() == null) return;
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || isDashing || dashGraceTimer > 0f || CutsceneInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (OnHurt != null) OnHurt();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibilityRoutine());
    }

    private void Die()
    {
        // 죽음 화면이 있는 씬은 그쪽이 리트라이까지 책임진다. 없는 씬은 종전대로 sceneOnDeath로 넘어간다.
        DeathRetryUI2D retry = FindFirstObjectByType<DeathRetryUI2D>();
        if (retry != null)
        {
            retry.Show();
            return;
        }

        if (!string.IsNullOrEmpty(sceneOnDeath))
        {
            SceneFader.LoadScene(sceneOnDeath);
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(invincibilityBlinkInterval);
            elapsed += invincibilityBlinkInterval;
        }

        if (sr != null) sr.enabled = true;
        isInvincible = false;
    }
}
