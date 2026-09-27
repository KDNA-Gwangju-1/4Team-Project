using UnityEngine;

/// <summary>Keeps a camera-child backdrop large enough to cover every rotation angle.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ViewportBackdrop2D : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(1f)] private float overscan = 1.06f;
    private SpriteRenderer backdrop;

    private void Awake() { backdrop = GetComponent<SpriteRenderer>(); }
    private void LateUpdate() { Fit(); }

    public void Fit()
    {
        if (backdrop == null) backdrop = GetComponent<SpriteRenderer>();
        if (viewCamera == null || !viewCamera.orthographic || backdrop.sprite == null) return;
        Vector2 size = backdrop.sprite.bounds.size;
        float shortest = Mathf.Min(size.x, size.y);
        if (shortest <= 0f) return;
        // A square enclosing the viewport diagonal covers corners at any Z rotation.
        float height = viewCamera.orthographicSize * 2f;
        float diagonal = height * Mathf.Sqrt(1f + viewCamera.aspect * viewCamera.aspect);
        float scale = diagonal * Mathf.Max(1f, overscan) / shortest;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
