using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal2D : MonoBehaviour
{
    public string targetSceneName;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        PlayerMovement2D.CarriedHasLantern = player.HasLantern;
        SceneManager.LoadScene(targetSceneName);
    }
}
