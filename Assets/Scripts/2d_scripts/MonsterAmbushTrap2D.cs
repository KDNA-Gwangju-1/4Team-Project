using UnityEngine;

public class MonsterAmbushTrap2D : MonoBehaviour
{
    public GameObject monster;
    public float spawnRadius = 10f;

    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerMovement2D>() == null) return;

        triggered = true;
        if (monster != null)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            monster.transform.position = (Vector2)transform.position + offset;
            monster.SetActive(true);
        }
    }
}
