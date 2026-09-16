using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    public static PlayerMovement2D Instance { get; private set; }

    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    public Transform flashlight;
    public float lightRange = 3f;
    public float lightHalfAngle = 15f;

    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletMaxDistance = 8f;
    public float fireCooldown = 1f;

    public int maxHealth = 10;
    public float invincibilityDuration = 2f;
    public float invincibilityBlinkInterval = 0.1f;

    public float fallRespawnY = -6f;
    public float checkpointEdgeMargin = 1f;

    private Rigidbody2D rb;
    private Vector3 lastGroundedPosition;
    private bool jumpRequested;
    private int facingDirection = 1;
    private SpriteRenderer flashlightRenderer;
    private Vector2 lightDirection = Vector2.right;
    private float lastFireTime = -999f;
    private SpriteRenderer sr;
    private int currentHealth;
    private bool isInvincible;

    public bool IsLightOn => flashlightRenderer != null && flashlightRenderer.enabled;
    public Vector2 LightOrigin => transform.position;
    public Vector2 LightDirection => lightDirection;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        lastGroundedPosition = transform.position;
        if (flashlight != null)
        {
            flashlightRenderer = flashlight.GetComponent<SpriteRenderer>();
            if (flashlightRenderer != null) flashlightRenderer.enabled = false;
        }
    }

    void Update()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        bool grounded = groundHit.collider != null;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.wasPressedThisFrame) facingDirection = 1;
            if (keyboard.aKey.wasPressedThisFrame) facingDirection = -1;

            if (keyboard.spaceKey.wasPressedThisFrame && grounded)
            {
                jumpRequested = true;
            }
        }

        if (grounded)
        {
            lastGroundedPosition = ComputeCheckpoint(groundHit.collider);
        }
        else if (transform.position.y < fallRespawnY)
        {
            Respawn();
        }

        UpdateFlashlight();
        UpdateShooting();
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

    void UpdateFlashlight()
    {
        if (flashlight == null || flashlightRenderer == null) return;

        var mouse = Mouse.current;
        bool aiming = mouse != null && mouse.leftButton.isPressed;
        flashlightRenderer.enabled = aiming;

        if (aiming)
        {
            lightDirection = GetMouseDirection(mouse);
            float angle = Mathf.Atan2(lightDirection.y, lightDirection.x) * Mathf.Rad2Deg;
            flashlight.position = transform.position;
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
        velocity.x = moveInput * moveSpeed;

        if (jumpRequested)
        {
            velocity.y = jumpForce;
            jumpRequested = false;
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
        if (isInvincible) return;
        if (other.GetComponent<Monster2D>() == null) return;

        currentHealth = Mathf.Max(0, currentHealth - 1);
        StartCoroutine(InvincibilityRoutine());
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
