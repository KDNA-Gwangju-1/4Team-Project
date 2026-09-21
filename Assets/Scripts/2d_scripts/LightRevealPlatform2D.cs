using UnityEngine;

public class TimedRevealPlatform2D : MonoBehaviour
{
    private const int RayCount = 15;

    [Range(0f, 1f)]
    public float silhouetteAlpha = 0.25f;
    [Range(0f, 1f)]
    public float revealedAlpha = 1f;
    public float timeToReveal = 2.5f;
    public float revealedDuration = 10f;
    public string revealedLayerName = "Ground";

    private SpriteRenderer sr;
    private Collider2D col;
    private float litTimer;
    private float revealedTimer;
    private bool isSolid;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) sr.enabled = false;

        int hiddenLayer = LayerMask.NameToLayer("Hidden");
        int defaultLayer = LayerMask.NameToLayer("Default");
        gameObject.layer = hiddenLayer;
        Physics2D.IgnoreLayerCollision(hiddenLayer, defaultLayer, true);
    }

    void Update()
    {
        if (sr == null || col == null) return;

        if (isSolid)
        {
            revealedTimer -= Time.deltaTime;
            if (revealedTimer <= 0f)
            {
                Hide();
            }
            return;
        }

        if (IsCurrentlyLit())
        {
            litTimer += Time.deltaTime;
            sr.enabled = true;
            SetAlpha(silhouetteAlpha);

            if (litTimer >= timeToReveal)
            {
                Reveal();
            }
        }
        else
        {
            litTimer = 0f;
            sr.enabled = false;
        }
    }

    private void Reveal()
    {
        isSolid = true;
        revealedTimer = revealedDuration;
        gameObject.layer = LayerMask.NameToLayer(revealedLayerName);
        SetAlpha(revealedAlpha);
    }

    private void Hide()
    {
        isSolid = false;
        litTimer = 0f;
        sr.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Hidden");
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private bool IsCurrentlyLit()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || !player.IsLightOn) return false;

        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        int layerMask = 1 << gameObject.layer;

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, player.lightRange, layerMask);
            if (hit.collider == col) return true;
        }

        return false;
    }
}
