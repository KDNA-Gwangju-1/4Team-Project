using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0.5f, -10f);

    public bool clampToBounds = false;
    public float minX;
    public float maxX;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
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
