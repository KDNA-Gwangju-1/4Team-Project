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
    [Tooltip("Draw order while he stands. Behind the body, so it reads as his far hand rather than his chest.")]
    public int idleSortingOrder = -1;
    [Tooltip("Where along its length the hand grips it. 0.5 puts the butt of the handle on the hand.")]
    [Range(0f, 1f)] public float gripToCenter01 = 0.5f;

    private SpriteRenderer sr;
    private PlayerMovement2D player;
    private Rigidbody2D body;
    private int airborneSortingOrder;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<PlayerMovement2D>();
        if (player != null) body = player.GetComponent<Rigidbody2D>();
        if (sr != null)
        {
            airborneSortingOrder = sr.sortingOrder;
            sr.enabled = false;
        }
    }

    private void LateUpdate()
    {
        // Before pickup PoseFlashlight is not called at all. Keep visibility in
        // sync independently, but let cutscenes own the pose while input is disabled.
        if (player != null && player.enabled) RefreshPose();
    }

    // Refresh immediately for the beam; LateUpdate also covers no-input/no-lantern frames.
    public bool TryGetEmitter(out Vector3 position)
    {
        RefreshPose();
        position = transform.position;
        if (sr == null || !sr.enabled || sr.sprite == null) return false;
        // The supplied art includes decorative rays beyond the actual lens (77% of width).
        Bounds bounds = sr.sprite.bounds;
        position = transform.TransformPoint(new Vector3(bounds.min.x + bounds.size.x * 0.77f, bounds.center.y, 0f));
        return true;
    }

    private void RefreshPose()
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

        // standing, it belongs behind him; in the air the old order still applies
        sr.sortingOrder = player.IsGrounded ? idleSortingOrder : airborneSortingOrder;

        float facing = player.LightDirection.x >= 0f ? 1f : -1f;
        Vector2 offset = player.IsGrounded ? idleHandOffset : player.jumpFlashlightHandOffset;
        Vector2 hand = (Vector2)player.transform.position + new Vector2(offset.x * facing, offset.y);

        Vector2 dir = player.LightDirection.normalized;
        // Standing, the sprite pivots at its middle so it gets pushed forward to
        // leave the handle on the hand. Airborne it must NOT be pushed: the jump
        // offset is already tuned for the raised arm, and adding the push sends it
        // flying off along the beam whenever he looks up.
        transform.position = player.IsGrounded
            ? hand + dir * (targetLength * gripToCenter01)
            : hand;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
