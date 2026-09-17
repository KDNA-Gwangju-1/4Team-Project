using UnityEngine;

public class FloatingMotion2D : MonoBehaviour
{
    public float amplitude = 0.3f;
    public float speed = 1f;

    private Vector3 basePosition;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        float y = basePosition.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = new Vector3(basePosition.x, y, basePosition.z);
    }
}
