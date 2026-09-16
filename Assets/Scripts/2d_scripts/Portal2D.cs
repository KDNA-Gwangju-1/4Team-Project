using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal2D : MonoBehaviour
{
    public string targetSceneName;
    public bool overrideSpawnX = false;
    public float spawnX = 0f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        if (overrideSpawnX)
        {
            PlayerMovement2D.PendingSpawnX = spawnX;
        }

        PlayerMovement2D.CarriedHealth = player.CurrentHealth;
        SceneManager.LoadScene(targetSceneName);
    }
}
