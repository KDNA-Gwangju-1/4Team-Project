using UnityEngine;

public class SlowRotate2D : MonoBehaviour
{
    public float degreesPerSecond = 5f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
