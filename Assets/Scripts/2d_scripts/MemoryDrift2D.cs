using UnityEngine;

/// <summary>Decor-only depth parallax and bounded, layered drifting. Never attach to platforms.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class MemoryDrift2D : MonoBehaviour
{
    [SerializeField] private Transform viewCamera;
    [SerializeField] private Vector2 cameraInfluence = new Vector2(.06f, .03f);
    [SerializeField] private Vector2 orbit = new Vector2(.22f, .18f);
    [SerializeField, Min(1f)] private float period = 17f;
    [SerializeField] private float phase;
    [SerializeField, Range(0f, 10f)] private float roll = 2f;
    private Vector3 restPosition, cameraOrigin;
    private Quaternion restRotation;
    private float elapsed;
    private bool captured;

    private void OnEnable()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
        cameraOrigin = viewCamera != null ? viewCamera.position : Vector3.zero;
        elapsed = 0f;
        captured = true;
    }

    private void LateUpdate()
    {
        if (!captured || Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        float t = elapsed * (Mathf.PI * 2f / Mathf.Max(1f, period));
        // Incommensurate harmonics form slow curved trajectories, not synchronized vertical bobbing.
        Vector2 drift = Trajectory(t) - Trajectory(0f);
        Vector3 travel = viewCamera != null ? viewCamera.position - cameraOrigin : Vector3.zero;
        Vector3 shift = new Vector3(travel.x * cameraInfluence.x, travel.y * cameraInfluence.y, 0f);
        if (transform.parent != null) shift = transform.parent.InverseTransformVector(shift);
        transform.localPosition = restPosition + shift + new Vector3(drift.x, drift.y, 0f);
        float angle = roll * (Mathf.Sin(t * .47f + phase) - Mathf.Sin(phase));
        transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 Trajectory(float t)
    {
        return new Vector2(
            orbit.x * (.72f * Mathf.Sin(t + phase) + .28f * Mathf.Sin(t * .37f + phase * 1.7f)),
            orbit.y * (.68f * Mathf.Cos(t * .73f + phase) + .32f * Mathf.Sin(t * 1.13f + phase * .6f)));
    }

    private void OnDisable()
    {
        if (!captured) return;
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
        captured = false;
    }
}
