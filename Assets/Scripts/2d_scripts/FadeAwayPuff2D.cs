using UnityEngine;

// A one-shot sprite that swells and fades out, then deletes itself.
// Used to show a shot being swallowed instead of silently vanishing.
public class FadeAwayPuff2D : MonoBehaviour
{
    public float life = 0.28f;
    public float growth = 2.4f;

    private SpriteRenderer sr;
    private Color from;
    private Vector3 baseScale;
    private float elapsed;

    public static void Spawn(Vector3 position, Sprite sprite, Color color, float size, int sortingOrder, float life, float growth)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("Puff");
        go.transform.position = position;

        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = sortingOrder;

        float native = Mathf.Max(0.001f, sprite.bounds.size.x);
        go.transform.localScale = Vector3.one * (size / native);

        FadeAwayPuff2D puff = go.AddComponent<FadeAwayPuff2D>();
        puff.life = life;
        puff.growth = growth;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        from = sr != null ? sr.color : Color.white;
        baseScale = transform.localScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, life));

        transform.localScale = baseScale * Mathf.Lerp(1f, growth, t);
        if (sr != null)
        {
            Color c = from;
            c.a = from.a * (1f - t);
            sr.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
