using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    public bool clampToBounds = false;
    public float minX;
    public float maxX;

    private Vector3 offset;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (clampToBounds && cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            float lo = minX + halfWidth;
            float hi = maxX - halfWidth;
            desired.x = lo <= hi ? Mathf.Clamp(desired.x, lo, hi) : (minX + maxX) * 0.5f;
        }

        transform.position = desired;
    }
}
