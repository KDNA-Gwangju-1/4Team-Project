using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Portal2D : MonoBehaviour
{
    public string targetSceneName;
    public bool overrideSpawnX = false;
    public float spawnX = 0f;

    public bool requireLantern = false;
    public Text lockedMessageText;
    public string lockedMessage = "안이 어둡다. 빛이 될만한 걸 찾아보자";
    public float lockedMessageDuration = 2.5f;

    private Coroutine hideMessageRoutine;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        if (requireLantern && !player.HasLantern)
        {
            ShowLockedMessage();
            return;
        }

        if (overrideSpawnX)
        {
            PlayerMovement2D.PendingSpawnX = spawnX;
        }

        PlayerMovement2D.CarriedHealth = player.CurrentHealth;
        SceneManager.LoadScene(targetSceneName);
    }

    private void ShowLockedMessage()
    {
        if (lockedMessageText == null) return;

        lockedMessageText.text = lockedMessage;
        lockedMessageText.enabled = true;

        if (hideMessageRoutine != null) StopCoroutine(hideMessageRoutine);
        hideMessageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(lockedMessageDuration);
        lockedMessageText.enabled = false;
    }
}
