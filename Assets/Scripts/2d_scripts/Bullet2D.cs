using UnityEngine;

public class Bullet2D : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private Vector3 startPosition;

    public void Init(Vector2 dir, float bulletSpeed, float distance)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        maxDistance = distance;
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
        int otherLayer = other.gameObject.layer;
        if (otherLayer == LayerMask.NameToLayer("Ground") || otherLayer == LayerMask.NameToLayer("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        Monster2D monster = other.GetComponent<Monster2D>();
        if (monster != null && monster.IsRevealed)
        {
            monster.Kill();
            Destroy(gameObject);
            return;
        }

        Boss2D boss = other.GetComponent<Boss2D>();
        if (boss != null && boss.IsRevealed)
        {
            boss.TakeDamage(1);
            Destroy(gameObject);
            return;
        }

        RangedMonster2D rangedMonster = other.GetComponent<RangedMonster2D>();
        if (rangedMonster != null && rangedMonster.IsRevealed)
        {
            Destroy(rangedMonster.gameObject);
            Destroy(gameObject);
        }
    }
}
