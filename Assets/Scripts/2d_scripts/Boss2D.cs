using System.Collections;
using UnityEngine;

public class Boss2D : MonoBehaviour
{
    public int maxHealth = 6;

    private SpriteRenderer sr;
    private MonsterSpriteAnimator2D animator;
    private Collider2D col;
    private int currentHealth;
    private bool isDying;

    public bool IsRevealed => sr != null && sr.enabled;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<MonsterSpriteAnimator2D>();
        col = GetComponent<Collider2D>();
        if (sr != null) sr.enabled = true;

        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDying) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
        {
            isDying = true;
            StartCoroutine(DieSequence());
        }
    }

    private IEnumerator DieSequence()
    {
        if (col != null) col.enabled = false;

        if (animator != null && animator.dissolveFrames != null && animator.dissolveFrames.Length > 0)
        {
            yield return animator.PlayDissolveRoutine();
        }

        Destroy(gameObject);
    }
}
