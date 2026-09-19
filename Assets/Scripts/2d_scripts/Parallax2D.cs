using UnityEngine;

// Moves slower than the camera so the object reads as being further away.
// This is what tells the player the boss is not standing in their space.
[ExecuteAlways]
public class Parallax2D : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = pinned to the camera (infinitely far), 1 = moves with the world (same plane as the player).")]
    public float factor = 0.3f;
    public bool affectY = false;

    private Camera cam;
    private Vector3 anchor;
    private Vector3 cameraAnchor;
    private bool captured;

    void OnEnable()
    {
        Capture();
    }

    public void Capture()
    {
        cam = Camera.main;
        if (cam == null) return;
        anchor = transform.position;
        cameraAnchor = cam.transform.position;
        captured = true;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !captured) return;

        Vector3 travel = cam.transform.position - cameraAnchor;
        Vector3 pos = transform.position;
        pos.x = anchor.x + travel.x * (1f - factor);
        if (affectY) pos.y = anchor.y + travel.y * (1f - factor);
        transform.position = pos;
    }
}
