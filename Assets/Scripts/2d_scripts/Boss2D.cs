using UnityEngine;

public class Boss2D : MonoBehaviour
{
    public int maxHealth = 6;

    private SpriteRenderer sr;
    private int currentHealth;

    public bool IsRevealed => sr != null && sr.enabled;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
