using UnityEngine;

public class BossClawHitbox2D : MonoBehaviour
{
    public int damage = 1;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.enabled = false;
        }
    }

    public void SetHitboxActive(bool active)
    {
        if (col != null) col.enabled = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        if (player != null) player.TakeDamage(damage);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        if (player != null) player.TakeDamage(damage);
    }
}
