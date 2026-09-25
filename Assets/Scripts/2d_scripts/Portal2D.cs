using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The door out of the stage. Walking into it is not enough - the player stops in
// front of it and opens it, so leaving is a decision rather than an accident.
public class Portal2D : MonoBehaviour
{
    public string targetSceneName;
    public bool overrideSpawnX = false;
    public float spawnX = 0f;
    [Tooltip("끄면 다음 씬에서 체력이 가득 찬 채로 시작한다. 보스전 입구처럼 앞 구간의 피해를 끌고 가지 않을 때.")]
    public bool carryHealth = true;

    public bool requireLantern = false;
    public Text lockedMessageText;
    public string lockedMessage = "안이 어둡다. 빛이 될만한 걸 찾아보자";
    public float lockedMessageDuration = 2.5f;

    [Header("Interaction")]
    [Tooltip("How close the player has to be before the prompt appears.")]
    public float useRange = 2.8f;
    public Key useKey = Key.E;
    [TextArea] public string promptMessage = "E키를 눌러 이동";

    private Coroutine hideMessageRoutine;
    private InteractPrompt2D prompt;
    private bool leaving;

    void Update()
    {
        // ESC 일시정지 중에는 입력을 받지 않는다.
        if (PauseMenu.IsPaused) return;
        if (leaving || string.IsNullOrEmpty(targetSceneName)) return;

        PlayerMovement2D player = PlayerMovement2D.Instance;
        bool inRange = player != null
            && Vector2.Distance(player.transform.position, transform.position) <= useRange;

        if (prompt == null && inRange) prompt = gameObject.AddComponent<InteractPrompt2D>();
        if (prompt != null) prompt.SetVisible(inRange, promptMessage);

        if (!inRange) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[useKey].wasPressedThisFrame) return;

        if (requireLantern && !player.HasLantern)
        {
            ShowLockedMessage();
            return;
        }

        leaving = true;
        GameSfx.Play("Door", .4f);
        if (prompt != null) prompt.SetVisible(false, promptMessage);

        if (overrideSpawnX) PlayerMovement2D.PendingSpawnX = spawnX;
        PlayerMovement2D.CarriedHealth = carryHealth ? player.CurrentHealth : (int?)null;
        // 스테이지 사이는 로딩 화면 없이 짧게 어두워졌다 밝아진다. 덮는 동안 조작은 멈춘다.
        player.enabled = false;
        SceneFader.LoadScene(targetSceneName);
    }

    private void ShowLockedMessage()
    {
        GameSfx.Play("UiBack", .3f, true);
        if (lockedMessageText == null) return;

        lockedMessageText.text = lockedMessage;
        lockedMessageText.enabled = true;

        if (hideMessageRoutine != null) StopCoroutine(hideMessageRoutine);
        hideMessageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(lockedMessageDuration);
        if (lockedMessageText != null) lockedMessageText.enabled = false;
    }
}
