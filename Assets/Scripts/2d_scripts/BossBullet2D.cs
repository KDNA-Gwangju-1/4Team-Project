using UnityEngine;

public class BossBullet2D : MonoBehaviour
{
    public float maxDistance = 20f;

    private Vector2 direction;
    private float speed;
    private Vector3 startPosition;

    public void Init(Vector2 dir, float bulletSpeed)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        if (player != null)
        {
            player.TakeDamage(1);
            Destroy(gameObject);
        }
    }
}
