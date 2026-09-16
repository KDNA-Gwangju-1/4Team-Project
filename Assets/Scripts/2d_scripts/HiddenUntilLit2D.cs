using UnityEngine;

public class HiddenUntilLit2D : MonoBehaviour
{
    private const int RayCount = 15;

    private SpriteRenderer sr;
    private Collider2D col;
    private bool discovered;
    private int hiddenLayerMask;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null) sr.enabled = false;

        int hiddenLayer = LayerMask.NameToLayer("Hidden");
        int defaultLayer = LayerMask.NameToLayer("Default");
        gameObject.layer = hiddenLayer;
        Physics2D.IgnoreLayerCollision(hiddenLayer, defaultLayer, true);
        hiddenLayerMask = 1 << hiddenLayer;
    }

    void Update()
    {
        if (discovered || sr == null || col == null) return;

        var player = PlayerMovement2D.Instance;
        if (player == null || !player.IsLightOn) return;

        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, player.lightRange, hiddenLayerMask);
            if (hit.collider == col)
            {
                Discover();
                return;
            }
        }
    }

    private void Discover()
    {
        discovered = true;
        sr.enabled = true;
        gameObject.layer = LayerMask.NameToLayer("Ground");
    }
}
