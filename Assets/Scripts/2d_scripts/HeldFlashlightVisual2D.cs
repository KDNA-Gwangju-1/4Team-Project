using UnityEngine;

public class HeldFlashlightVisual2D : MonoBehaviour
{
    public Sprite offSprite;
    public Sprite onSprite;
    public Vector2 handOffset = new Vector2(0.4f, 0.1f);
    [Tooltip("Where it sits while he stands still - back by his trailing hand, so the idle pose can keep its stance.")]
    public Vector2 idleHandOffset = new Vector2(0.12f, -0.05f);
    public float targetLength = 0.9f;
    [Tooltip("Horizontal speed under this counts as standing still.")]
    public float idleSpeedThreshold = 0.1f;

    private SpriteRenderer sr;
    private PlayerMovement2D player;
    private Rigidbody2D body;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<PlayerMovement2D>();
        if (player != null) body = player.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (sr == null || player == null) return;

        // Only the WALK art has a flashlight drawn into it. Jumping never did, and
        // standing still used to fake it by holding a walk frame - which splayed his
        // legs out the moment the light came on. Both of those poses get the overlay
        // instead, so the body can keep its own stance.
        float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        bool standingStill = player.IsGrounded && speed <= idleSpeedThreshold;
        sr.enabled = player.HasLantern && player.IsLightOn && (!player.IsGrounded || standingStill);
        if (!sr.enabled) return;

        sr.sprite = onSprite;
        if (sr.sprite != null && sr.sprite.bounds.size.x > 0f)
        {
            float scale = targetLength / sr.sprite.bounds.size.x;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        float facing = player.LightDirection.x >= 0f ? 1f : -1f;
        Vector2 offset = player.IsGrounded ? idleHandOffset : player.jumpFlashlightHandOffset;
        transform.position = (Vector2)player.transform.position + new Vector2(offset.x * facing, offset.y);

        Vector2 dir = player.LightDirection;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
