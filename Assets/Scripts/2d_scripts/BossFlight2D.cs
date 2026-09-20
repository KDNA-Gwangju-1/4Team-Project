using UnityEngine;

// Phase 2 movement: she leaves the ledge and circles the arena overhead.
// Drifts between hover points instead of chasing, so the player can read where
// she is going and still has to reposition rather than camp one spot.
public class BossFlight2D : MonoBehaviour
{
    [Tooltip("Horizontal band she patrols, in world x.")]
    public float minX = 21f;
    public float maxX = 45f;
    [Tooltip("Height band above the arena floor.")]
    public float minY = 2.5f;
    public float maxY = 8f;

    [Tooltip("How long one drift between points takes.")]
    public float moveDuration = 2.2f;
    [Tooltip("Pause at each point before choosing the next.")]
    public float hoverTime = 0.6f;
    [Tooltip("Never pick a next point closer than this, or she barely moves.")]
    public float minTravel = 7f;

    [Tooltip("Keep her within reach of the player. The camera follows him, so a boss roaming the whole arena is off screen most of the time.")]
    public bool stayNearPlayer = true;
    [Tooltip("Furthest she drifts from the player horizontally.")]
    public float maxOffsetFromPlayer = 9f;
    [Tooltip("Closest she comes horizontally - she should not sit on top of him.")]
    public float minOffsetFromPlayer = 3f;

    [Tooltip("Gentle bob layered on top of the drift.")]
    public float bobAmplitude = 0.35f;
    public float bobPeriod = 1.8f;

    [Tooltip("Faces the player. Turn off if the art only looks right one way.")]
    public bool faceTravelDirection = true;

    private SpriteRenderer sr;
    private Vector2 from;
    private Vector2 to;
    private float travelTimer;
    private float waitTimer;
    private bool flying;
    private float seed;

    public bool Flying { get { return flying; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
    }

    public void Begin()
    {
        from = transform.position;
        to = PickPoint(from);
        travelTimer = 0f;
        waitTimer = 0f;
        flying = true;
    }

    public void Stop()
    {
        flying = false;
    }

    private Vector2 PickPoint(Vector2 current)
    {
        float lo = minX, hi = maxX;
        if (stayNearPlayer)
        {
            var player = PlayerMovement2D.Instance;
            if (player != null)
            {
                float px = player.transform.position.x;
                lo = Mathf.Max(minX, px - maxOffsetFromPlayer);
                hi = Mathf.Min(maxX, px + maxOffsetFromPlayer);
                if (hi - lo < 1f) { lo = Mathf.Max(minX, px - 2f); hi = Mathf.Min(maxX, px + 2f); }
            }
        }

        // a handful of tries is enough to land a point that is actually a trip
        for (int i = 0; i < 12; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(lo, hi), Random.Range(minY, maxY));
            if (Vector2.Distance(candidate, current) < minTravel) continue;
            if (stayNearPlayer && !FarEnoughFromPlayer(candidate)) continue;
            return candidate;
        }

        // fall back to the far side of whatever band is allowed
        float farX = (current.x - lo < hi - current.x) ? hi : lo;
        return new Vector2(farX, Random.Range(minY, maxY));
    }

    private bool FarEnoughFromPlayer(Vector2 candidate)
    {
        var player = PlayerMovement2D.Instance;
        if (player == null) return true;
        return Mathf.Abs(candidate.x - player.transform.position.x) >= minOffsetFromPlayer;
    }

    void Update()
    {
        if (!flying) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            ApplyBob(to);
            return;
        }

        travelTimer += Time.deltaTime;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(travelTimer / Mathf.Max(0.05f, moveDuration)));
        ApplyBob(Vector2.Lerp(from, to, k));

        if (faceTravelDirection && sr != null && Mathf.Abs(to.x - from.x) > 0.1f)
        {
            sr.flipX = to.x > from.x;
        }

        if (k >= 1f)
        {
            from = to;
            to = PickPoint(from);
            travelTimer = 0f;
            waitTimer = hoverTime;
        }
    }

    private void ApplyBob(Vector2 basePosition)
    {
        float bob = Mathf.Sin((Time.time / Mathf.Max(0.05f, bobPeriod) + seed) * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(basePosition.x, basePosition.y + bob, transform.position.z);
    }
}
