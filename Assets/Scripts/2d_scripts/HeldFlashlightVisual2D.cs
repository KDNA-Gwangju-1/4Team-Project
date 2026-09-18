using UnityEngine;

public class HeldFlashlightVisual2D : MonoBehaviour
{
    public Sprite offSprite;
    public Sprite onSprite;
    public Vector2 handOffset = new Vector2(0.4f, 0.1f);
    public float targetLength = 0.9f;

    private SpriteRenderer sr;
    private PlayerMovement2D player;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<PlayerMovement2D>();
    }

    void Update()
    {
        if (sr == null || player == null) return;

        // Idle/walk already show the flashlight built into the character art.
        // This overlay only fills the gap for the jump pose, which has no held-flashlight art.
        sr.enabled = player.HasLantern && !player.IsGrounded && player.IsLightOn;
        if (!sr.enabled) return;

        sr.sprite = onSprite;
        if (sr.sprite != null && sr.sprite.bounds.size.x > 0f)
        {
            float scale = targetLength / sr.sprite.bounds.size.x;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        float facing = player.LightDirection.x >= 0f ? 1f : -1f;
        Vector2 offset = player.jumpFlashlightHandOffset;
        transform.position = (Vector2)player.transform.position + new Vector2(offset.x * facing, offset.y);

        Vector2 dir = player.LightDirection;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
