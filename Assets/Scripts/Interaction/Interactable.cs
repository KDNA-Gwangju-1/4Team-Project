using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// "가까이 가서 E 를 누르면 뭔가 일어나는 물건" 의 공통 부품.
///
/// 이 컴포넌트가 붙은 오브젝트에는 Collider 가 하나 있어야 한다.
/// (플레이어가 화면 가운데로 쳐다볼 때 그 Collider 를 맞춰서 찾는다)
///
/// 그냥 갖다 붙이고 On Interact 이벤트만 연결해도 되고,
/// 특별한 동작이 필요하면 이 클래스를 상속해서 Interact() 를 덮어쓰면 된다.
/// (예: PatientTouchInteractable)
/// </summary>
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour
{
    [Header("화면에 뜨는 말")]
    [Tooltip("윗줄에 작게 뜨는 대상 이름. 비워 두면 안 뜬다. 예) 서하린")]
    [SerializeField] protected string displayName = "";

    [Tooltip("아랫줄에 뜨는 행동. 예) 손대기, 살펴보기, 열기")]
    [SerializeField] protected string actionLabel = "상호작용";

    [Header("범위")]
    [Tooltip("이 거리 안까지 다가와야 E 가 뜬다 (m)")]
    [SerializeField] protected float interactRange = 1.6f;

    [Header("이벤트")]
    [Tooltip("E 를 눌렀을 때 실행할 것들. Inspector 에서 자유롭게 연결한다.")]
    [SerializeField] private UnityEvent onInteract;

    /// <summary>프롬프트 윗줄 (대상 이름)</summary>
    public virtual string DisplayName => displayName;

    /// <summary>프롬프트 아랫줄 (행동)</summary>
    public virtual string ActionLabel => actionLabel;

    /// <summary>상호작용이 가능한 거리</summary>
    public float InteractRange => interactRange;

    /// <summary>지금 상호작용할 수 있는 상태인지</summary>
    public virtual bool CanInteract => isActiveAndEnabled;

    /// <summary>E 를 눌렀을 때 호출된다.</summary>
    public virtual void Interact(PlayerInteractor interactor)
    {
        onInteract?.Invoke();
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
