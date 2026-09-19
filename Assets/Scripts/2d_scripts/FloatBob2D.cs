using UnityEngine;

// Slow vertical drift, so the boss's ledge reads as hanging in the dream rather
// than bolted to anything. Only touches Y - Parallax2D owns X and the two can
// share a transform without fighting.
[ExecuteAlways]
public class FloatBob2D : MonoBehaviour
{
    public float amplitude = 0.22f;
    public float period = 3.6f;
    [Tooltip("Fraction of a cycle to start at. Keep matched objects on the same value so they drift together.")]
    public float phase = 0f;

    private float baseY;
    private bool captured;

    void OnEnable()
    {
        Recapture();
    }

    // The ledge is repositioned by the cutscene, so the rest pose has to be
    // re-read once it has actually arrived.
    public void Recapture()
    {
        baseY = transform.position.y;
        captured = true;
    }

    void LateUpdate()
    {
        if (!captured || period <= 0.001f) return;

        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) * amplitude;
        transform.position = p;
    }
}
