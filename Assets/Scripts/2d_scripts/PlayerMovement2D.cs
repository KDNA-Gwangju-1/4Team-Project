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
    public LayerMask groundLayer;

    public float wallCheckDistance = 0.6f;
    public float wallJumpUpForce = 7f;
    public float wallJumpPushForce = 6f;
    public float wallJumpLockDuration = 0.2f;
    public float wallJumpBoostSpeed = 8f;
    public float wallJumpBoostDuration = 0.3f;
    public LayerMask wallLayer;

    public float dashSpeed = 14f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.6f;
    public bool allowAirDash = false;

    public Transform flashlight;
    public float lightRange = 3f;
    public float lightMaxSeconds = 10f;
    public float lightLockoutSeconds = 7f;
    public float lightHalfAngle = 15f;
    public Vector2 flashlightHandOffset = new Vector2(0.4f, 0.1f);
    public Vector2 jumpFlashlightHandOffset = new Vector2(0.75f, 0.15f);

    public static bool LanternObtained = false;
    public static float? PendingSpawnX = null;
    public static int? CarriedHealth = null;

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
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashDirection;
    private int facingDirection = 1;
    private bool grounded;
    private SpriteRenderer flashlightRenderer;
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
    public int FacingDirection => facingDirection;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = CarriedHealth ?? maxHealth;
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
        if (flashlight != null)
        {
            flashlightRenderer = flashlight.GetComponent<SpriteRenderer>();
            if (flashlightRenderer != null) flashlightRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        grounded = groundHit.collider != null;

        RaycastHit2D wallHitRight = CastForWall(Vector2.right);
        RaycastHit2D wallHitLeft = CastForWall(Vector2.left);
        touchingWallRight = wallHitRight.collider != null;
        touchingWallLeft = wallHitLeft.collider != null;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.wasPressedThisFrame) facingDirection = 1;
            if (keyboard.aKey.wasPressedThisFrame) facingDirection = -1;

            if ((keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                && (grounded || allowAirDash) && !isDashing && dashCooldownTimer <= 0f)
            {
                dashRequested = true;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (grounded)
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
            if (!respawnAnchorLocked && groundHit.collider.GetComponent<TimedRevealPlatform2D>() == null)
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

    private void Respawn()
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
        bool wantsLight = hasLantern && mouse != null && mouse.leftButton.isPressed;

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

        if (aiming)
        {
            lightDirection = GetMouseDirection(mouse);
            float angle = Mathf.Atan2(lightDirection.y, lightDirection.x) * Mathf.Rad2Deg;
            float facing = lightDirection.x >= 0f ? 1f : -1f;
            Vector2 offset = grounded ? flashlightHandOffset : jumpFlashlightHandOffset;
            flashlight.position = transform.position + new Vector3(offset.x * facing, offset.y, 0f);
            flashlight.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void UpdateShooting()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame && Time.time - lastFireTime >= fireCooldown)
        {
            Vector2 direction = GetMouseDirection(mouse);
            FireBullet(direction);
            lastFireTime = Time.time;
        }
    }

    void FireBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet2D bullet = bulletObj.GetComponent<Bullet2D>();
        if (bullet != null)
        {
            bullet.Init(direction, bulletSpeed, bulletMaxDistance);
        }
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
        }

        if (isDashing)
        {
            velocity.x = dashDirection * dashSpeed;
            velocity.y = 0f;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) isDashing = false;
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
        if (isInvincible || isDashing) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibilityRoutine());
    }

    private void Die()
    {
        if (!string.IsNullOrEmpty(sceneOnDeath))
        {
            SceneManager.LoadScene(sceneOnDeath);
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
