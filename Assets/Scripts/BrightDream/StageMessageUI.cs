using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 정중앙에 스테이지 안내 문구를 잠깐 띄우는 싱글턴 UI 컨트롤러.
/// </summary>
public class StageMessageUI : MonoBehaviour
{
    public static StageMessageUI Instance { get; private set; }

    [SerializeField] private Text messageText;
    [SerializeField] private float defaultDuration = 4f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    public void ShowMessage(string text) => ShowMessage(text, defaultDuration);

    public void ShowMessage(string text, float duration)
    {
        if (messageText == null) return;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        hideRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (messageText != null) messageText.gameObject.SetActive(false);
        hideRoutine = null;
    }
}
