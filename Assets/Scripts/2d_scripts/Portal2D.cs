using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal2D : MonoBehaviour
{
    public string targetSceneName;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;
        if (other.GetComponent<PlayerMovement2D>() == null) return;

        SceneManager.LoadScene(targetSceneName);
    }
}
