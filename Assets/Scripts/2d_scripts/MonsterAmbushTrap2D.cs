using UnityEngine;

public class MonsterAmbushTrap2D : MonoBehaviour
{
    public GameObject monster;
    public int spawnCount = 10;
    public float spawnRadius = 8f;

    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        triggered = true;
        if (monster == null) return;

        Vector2 center = player.transform.position;
        for (int i = 0; i < spawnCount; i++)
        {
            float angle = (Mathf.PI * 2f / spawnCount) * i;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            GameObject clone = Instantiate(monster, center + offset, Quaternion.identity);
            clone.SetActive(true);
        }
    }
}
