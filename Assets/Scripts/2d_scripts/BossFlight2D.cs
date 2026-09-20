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
        // a handful of tries is enough to land a point that is actually a trip
        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (Vector2.Distance(candidate, current) >= minTravel) return candidate;
        }
        // fall back to the far side of the band
        float farX = (current.x - minX < maxX - current.x) ? maxX : minX;
        return new Vector2(farX, Random.Range(minY, maxY));
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
