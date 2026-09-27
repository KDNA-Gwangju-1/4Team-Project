using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0.5f, -10f);

    public bool clampToBounds = false;
    public float minX;
    public float maxX;

    // A boss arena on a fixed island does not want the camera riding the
    // player's height - that is what lets the void under the island show.
    public bool lockY = false;
    public float lockedY;

    private Camera cam;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
        shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (lockY) desired.y = lockedY;

        if (clampToBounds && cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            float lo = minX + halfWidth;
            float hi = maxX - halfWidth;
            desired.x = lo <= hi ? Mathf.Clamp(desired.x, lo, hi) : (minX + maxX) * 0.5f;
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float falloff = Mathf.Clamp01(shakeTimer / shakeDuration);
            desired.x += Random.Range(-1f, 1f) * shakeMagnitude * falloff;
            desired.y += Random.Range(-1f, 1f) * shakeMagnitude * falloff;
        }

        transform.position = desired;
    }
}
