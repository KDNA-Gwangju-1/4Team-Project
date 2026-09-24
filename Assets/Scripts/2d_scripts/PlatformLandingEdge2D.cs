using UnityEngine;

// A thin landing seam follows the collider and the platform's reveal/fade state.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class PlatformLandingEdge2D : MonoBehaviour
{
    private SpriteRenderer surface;
    private BoxCollider2D shape;
    private LineRenderer seam;
    private static Material seamMaterial;

    private void Awake()
    {
        surface = GetComponent<SpriteRenderer>();
        shape = GetComponent<BoxCollider2D>();
        if (seamMaterial == null) seamMaterial = new Material(Shader.Find("Sprites/Default"));
        seam = new GameObject("LandingSeam").AddComponent<LineRenderer>();
        seam.transform.SetParent(transform, false);
        seam.sharedMaterial = seamMaterial;
        seam.useWorldSpace = true;
        seam.positionCount = 2;
        seam.startWidth = seam.endWidth = .035f;
        seam.sortingLayerID = surface.sortingLayerID;
        seam.sortingOrder = surface.sortingOrder + 1;
    }

    private void LateUpdate()
    {
        seam.enabled = surface.enabled && shape.enabled && surface.color.a > .01f;
        if (!seam.enabled) return;
        var bounds = shape.bounds;
        float inset = Mathf.Min(.1f, bounds.extents.x * .1f);
        seam.SetPosition(0, new Vector3(bounds.min.x + inset, bounds.max.y, transform.position.z));
        seam.SetPosition(1, new Vector3(bounds.max.x - inset, bounds.max.y, transform.position.z));
        seam.startColor = seam.endColor = new Color(.84f, .88f, 1f, surface.color.a * .65f);
    }

    private void OnDisable() { if (seam != null) seam.enabled = false; }
}
