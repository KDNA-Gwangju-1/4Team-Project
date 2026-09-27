using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 게임을 시작할 때 조작법을 먼저 보여 주는 안내창.
///
/// 아무 키나 누르면 닫히고, 닫히면서 On Closed 에 연결해 둔 것을 실행한다.
/// 보통 첫 대사(DialogueTrigger.Play)를 걸어 둔다.
///
/// 붙이는 곳 : 안내창 UI 오브젝트 (CanvasGroup 필요)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class ControlGuideUI : MonoBehaviour
{
    [Header("타이밍 (초)")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.25f;

    [Tooltip("이 시간 안에는 키를 눌러도 안 닫힌다. 시작하자마자 실수로 넘기는 걸 막는다.")]
    [SerializeField] private float minimumShowTime = 0.6f;

    [Header("닫기")]
    [Tooltip("켜면 아무 키나 눌러도 닫힌다. 끄면 아래 키로만 닫힌다.")]
    [SerializeField] private bool anyKeyCloses = true;

    [SerializeField] private KeyCode[] closeKeys = { KeyCode.Space, KeyCode.Return, KeyCode.E };

    [Header("닫힌 뒤")]
    [Tooltip("안내창이 사라지고 나서 실행할 것. 보통 첫 대사를 건다.")]
    [SerializeField] private UnityEvent onClosed;

    /// <summary>
    /// 안내창이 떠 있는 동안 true.
    /// PlayerInteractor 가 이 값을 보고 그동안 상호작용을 쉰다.
    /// </summary>
    public static bool Blocking { get; private set; }

    private CanvasGroup _group;
    private Sprite _guideFrame;

    private void Awake()
    {
        _guideFrame = ControlGuideSkin.Apply(transform as RectTransform);
        HangulFont.ApplyAll(gameObject);
        Blocking = false;   // 플레이 모드를 껐다 켜도 남지 않게

        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        Blocking = false;
    }

    private void OnDestroy()
    {
        if (_guideFrame != null) Destroy(_guideFrame);
    }

    private void Start()
    {
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        Blocking = true;

        yield return Fade(0f, 1f, fadeInTime);

        float shown = 0f;
        while (shown < minimumShowTime || !ClosePressed())
        {
            shown += Time.unscaledDeltaTime;
            yield return null;
        }

        // 닫은 키가 다음 대사까지 넘겨 버리지 않도록 한 프레임 비운다.
        yield return null;

        yield return Fade(1f, 0f, fadeOutTime);

        Blocking = false;
        gameObject.SetActive(false);

        if (onClosed != null) onClosed.Invoke();
    }

    private bool ClosePressed()
    {
        if (anyKeyCloses) return Input.anyKeyDown;

        if (closeKeys == null) return false;
        for (int i = 0; i < closeKeys.Length; i++)
        {
            if (closeKeys[i] == KeyCode.None) continue;
            if (Input.GetKeyDown(closeKeys[i])) return true;
        }
        return false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { _group.alpha = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _group.alpha = to;
    }
}
