using UnityEngine;

// Turns a sprite into a shadow that the flashlight carves the real picture out of,
// the same trick the tentacles and monsters use: the body only draws inside the
// beam's mask, and a flat silhouette covers everything outside it.
//
// Unlike those, this one rides an animated renderer, so it re-copies the current
// frame every LateUpdate - otherwise the shadow would freeze on whichever frame
// was showing when it switched on.
[RequireComponent(typeof(SpriteRenderer))]
public class LightSilhouette2D : MonoBehaviour
{
    public Color silhouetteColor = Color.black;
    public Color outlineColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public float outlineScale = 1.04f;

    private SpriteRenderer body;
    private SpriteRenderer silhouette;
    private SpriteRenderer outline;
    private SpriteMaskInteraction bodyDefault;
    private bool built;
    private bool active;

    public bool Active
    {
        get { return active; }
        set
        {
            if (active == value) return;
            active = value;
            Apply();
        }
    }

    void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        bodyDefault = body.maskInteraction;
        Build();
        Apply();
    }

    private void Build()
    {
        if (built) return;
        built = true;

        outline = CreateLayer("LightOutline", outlineColor, outlineScale, body.sortingOrder - 1);
        silhouette = CreateLayer("LightSilhouette", silhouetteColor, 1f, body.sortingOrder);
    }

    private SpriteRenderer CreateLayer(string label, Color color, float scale, int order)
    {
        Transform existing = transform.Find(label);
        GameObject go = existing != null ? existing.gameObject : new GameObject(label);
        if (existing == null) go.transform.SetParent(transform, false);

        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        if (r == null) r = go.AddComponent<SpriteRenderer>();
        r.color = color;
        r.sortingLayerID = body.sortingLayerID;
        r.sortingOrder = order;
        r.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        r.enabled = false;
        return r;
    }

    private void Apply()
    {
        if (body == null) return;
        body.maskInteraction = active ? SpriteMaskInteraction.VisibleInsideMask : bodyDefault;
        if (silhouette != null) silhouette.enabled = active;
        if (outline != null) outline.enabled = active;
    }

    void LateUpdate()
    {
        if (!active || body == null) return;

        // the boss is animated, so the shadow has to track the current frame
        if (silhouette != null)
        {
            silhouette.sprite = body.sprite;
            silhouette.flipX = body.flipX;
            silhouette.color = silhouetteColor;
        }
        if (outline != null)
        {
            outline.sprite = body.sprite;
            outline.flipX = body.flipX;
            outline.color = outlineColor;
        }
    }
}
