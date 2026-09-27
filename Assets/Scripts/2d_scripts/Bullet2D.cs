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

        BossTentacle2D tentacle = other.GetComponent<BossTentacle2D>();
        if (tentacle != null && tentacle.IsRevealed)
        {
            tentacle.Kill();
            Destroy(gameObject);
            return;
        }

        TentacleStrike2D strike = other.GetComponent<TentacleStrike2D>();
        if (strike != null)
        {
            if (strike.CanBeKilled) strike.Kill();
            Destroy(gameObject);
            return;
        }

        Boss2D boss = other.GetComponent<Boss2D>();
        if (boss != null)
        {
            if (boss.CanBeShot)
            {
                boss.TakeDamage(1);
            }
            else
            {
                // she is out of reach: show the shot being swallowed instead of
                // letting it disappear with no feedback at all
                SpriteRenderer mine = GetComponent<SpriteRenderer>();
                FadeAwayPuff2D.Spawn(
                    transform.position,
                    mine != null ? mine.sprite : null,
                    new Color(0.15f, 0.12f, 0.18f, 0.85f),
                    1.1f,
                    mine != null ? mine.sortingOrder : 0,
                    0.3f,
                    2.6f);
            }
            Destroy(gameObject);
            return;
        }

        RangedMonster2D rangedMonster = other.GetComponent<RangedMonster2D>();
        if (rangedMonster != null && rangedMonster.IsRevealed)
        {
            rangedMonster.Kill();
            Destroy(gameObject);
        }
    }
}
