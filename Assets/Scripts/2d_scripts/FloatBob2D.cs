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
    private float applied;
    private bool captured;

    void OnEnable()
    {
        Recapture();
    }

    void OnDisable()
    {
        // hand the transform back the way we found it
        if (!captured || applied == 0f) return;
        Vector3 p = transform.position;
        p.y -= applied;
        transform.position = p;
        applied = 0f;
    }

    // The ledge is repositioned by the cutscene, so the rest pose has to be
    // re-read once it has actually arrived.
    public void Recapture()
    {
        // whatever the last frame added is not part of the rest pose. Folding it
        // back in is what walked the ledge a few centimetres further up on every
        // recompile, until it was seven units above the boss standing on it.
        baseY = transform.position.y - applied;
        applied = 0f;
        captured = true;
    }

    void LateUpdate()
    {
        if (!captured || period <= 0.001f) return;

        // Outside play mode it sits at its rest pose. A bob that is live while
        // nothing is running ends up saved into the scene.
        float offset = Application.isPlaying
            ? Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) * amplitude
            : 0f;

        Vector3 p = transform.position;
        p.y = baseY + offset;
        transform.position = p;
        applied = offset;
    }
}
