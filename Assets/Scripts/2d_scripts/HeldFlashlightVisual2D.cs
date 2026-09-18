using UnityEngine;

public class HeldFlashlightVisual2D : MonoBehaviour
{
    public Sprite offSprite;
    public Sprite onSprite;
    public Vector2 handOffset = new Vector2(0.4f, 0.1f);

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

        sr.enabled = player.HasLantern;
        if (!sr.enabled) return;

        sr.sprite = player.IsLightOn ? onSprite : offSprite;

        float facing = player.IsLightOn
            ? (player.LightDirection.x >= 0f ? 1f : -1f)
            : (player.FacingDirection >= 0 ? 1f : -1f);
        transform.position = (Vector2)player.transform.position + new Vector2(handOffset.x * facing, handOffset.y);

        Vector2 dir = player.LightDirection;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
