using UnityEngine;

/// <summary>
/// 정해진 순간에 자막 대사를 띄우는 장치.
///
/// 띄우는 때는 세 가지 중에 고른다.
///   OnStart  : 게임이 시작되자마자 (문 앞 스폰 대사용)
///   OnEnter  : 플레이어가 이 범위 안에 들어왔을 때 (방에 들어설 때, 빈 침대 앞 등)
///   Manual   : 다른 곳에서 Play() 를 불러 줄 때 (E 손대기 이벤트 등)
///
/// OnEnter 로 쓰려면 이 오브젝트에 Is Trigger 가 켜진 Collider 가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnStart,   // 시작하자마자
        OnEnter,   // 범위 안에 들어오면
        Manual,    // 다른 데서 불러 줄 때만
    }

    [Header("언제 띄울지")]
    [SerializeField] private TriggerMode mode = TriggerMode.OnEnter;

    [Tooltip("OnStart 일 때, 시작하고 몇 초 뒤에 띄울지")]
    [SerializeField] private float startDelay = 1.0f;

    [Tooltip("켜 두면 한 번만 나오고 다시는 안 나온다.")]
    [SerializeField] private bool playOnce = true;

    [Header("대사 (한 줄씩)")]
    [Tooltip("대사창 왼육 위에 뜨는 이름. 비우면 이름표가 안 나온다.")]
    [SerializeField] private string speaker = "꿈탐정";

    [TextArea(1, 3)]
    [SerializeField] private string[] lines;

    [Header("참조 (비워 두면 씬에서 찾는다)")]
    [SerializeField] private SubtitleUI subtitle;

    // ------------------------------------------------------------
    private bool _played;

    /// <summary>이미 나온 적 있는지</summary>
    public bool HasPlayed => _played;

    private void Awake()
    {
        if (subtitle == null)
            subtitle = FindFirstObjectByType<SubtitleUI>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (mode == TriggerMode.OnStart) Invoke(nameof(Play), Mathf.Max(startDelay, 0f));
    }

    /// <summary>대사를 띄운다. 유니티 이벤트(On Interact 등)에 그대로 연결할 수 있다.</summary>
    public void Play()
    {
        if (playOnce && _played) return;
        if (lines == null || lines.Length == 0) return;

        if (subtitle == null)
        {
            Debug.LogWarning($"[DialogueTrigger] 자막(SubtitleUI)을 찾지 못했습니다: {name}", this);
            return;
        }

        _played = true;

        // 다른 대사나 조작법 안내창이 떠 있으면 끊지 않고 끝날 때까지 기다렸다가 띄운다.
        // (SubtitleUI.Play 는 재생 중이던 대사를 끊고 새로 시작한다.)
        if (subtitle.IsPlaying || ControlGuideUI.Blocking)
        {
            if (gameObject.activeInHierarchy) StartCoroutine(PlayWhenFree());
            return;
        }

        subtitle.Play(lines, speaker);
    }

    private System.Collections.IEnumerator PlayWhenFree()
    {
        while (subtitle.IsPlaying || ControlGuideUI.Blocking) yield return null;
        subtitle.Play(lines, speaker);
    }

    /// <summary>다시 나올 수 있게 되돌린다.</summary>
    public void ResetTrigger()
    {
        _played = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mode != TriggerMode.OnEnter) return;

        // 플레이어인지 확인한다. (CharacterController 가 붙은 쪽)
        if (other.GetComponentInParent<FirstPersonController>() == null) return;

        Play();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (mode != TriggerMode.OnEnter) return;

        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
#endif
}
