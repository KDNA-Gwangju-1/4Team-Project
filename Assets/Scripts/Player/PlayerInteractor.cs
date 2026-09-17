using UnityEngine;

/// <summary>
/// 플레이어가 앞에 있는 Interactable 을 찾아서 E 로 상호작용하게 해 주는 스크립트.
///
/// 찾는 방법은 두 가지를 같이 쓴다.
///  1) 화면 한가운데에서 앞으로 굵은 광선을 쏴서 맞는 것 (정확히 쳐다볼 때)
///  2) 그래도 못 찾으면 주변을 둘러보고 "가깝고 + 시야 안에 있는" 것 (대충 볼 때)
///
/// 둘 다 쓰는 이유는, 침대에 누운 사람처럼 낮고 넓은 대상은
/// 광선만으로는 조준이 까다롭기 때문이다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워 두면 자식에서 카메라를 찾는다.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("E 안내문을 띄울 UI. 비워 두면 씬에서 찾는다.")]
    [SerializeField] private InteractionPromptUI promptUI;

    [Header("조작")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("찾는 범위")]
    [Tooltip("광선을 쏘는 최대 거리 (m)")]
    [SerializeField] private float maxDistance = 2.4f;

    [Tooltip("광선의 굵기. 굵을수록 조준이 널널해진다.")]
    [SerializeField] private float castRadius = 0.28f;

    [Tooltip("주변 탐색을 할 반경 (m)")]
    [SerializeField] private float nearbyRadius = 2.4f;

    [Tooltip("이 각도 안에 들어와 있어야 '쳐다보고 있다'고 친다 (도)")]
    [SerializeField] private float viewAngle = 70f;
    // ------------------------------------------------------------
    private Interactable _current;
    private readonly Collider[] _overlapBuffer = new Collider[32];

    /// <summary>지금 조준되어 있는 대상. 없으면 null.</summary>
    public Interactable Current => _current;

    private void Awake()
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        if (promptUI == null) promptUI = FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        // 대사가 나오는 동안에는 쉰다.
        // 대사를 넘기려고 누른 E 가 상호작용까지 같이 눌러 버리면 안 되기 때문이다.
        if (SubtitleUI.Blocking)
        {
            _current = null;
            if (promptUI != null) promptUI.Hide();
            return;
        }

        _current = FindTarget();

        // ---------- 안내문 ----------
        if (promptUI != null)
        {
            if (_current != null) promptUI.Show(_current.DisplayName, _current.ActionLabel);
            else                  promptUI.Hide();
        }

        // ---------- 실행 ----------
        if (_current != null && Input.GetKeyDown(interactKey))
        {
            _current.Interact(this);
        }
    }

    // ============================================================
    // 대상 찾기
    // ============================================================

    private Interactable FindTarget()
    {
        if (viewCamera == null) return null;

        Interactable found = CastForward();
        if (found != null) return found;

        return SearchNearby();
    }

    /// <summary>화면 한가운데에서 앞으로 굵은 광선을 쏜다.</summary>
    private Interactable CastForward()
    {
        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);

        var hits = Physics.SphereCastAll(ray, castRadius, maxDistance, ~0, QueryTriggerInteraction.Collide);
        if (hits.Length == 0) return null;

        // 가까운 것부터 살펴본다.
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var interactable = hits[i].collider.GetComponentInParent<Interactable>();
            if (IsUsable(interactable)) return interactable;
        }

        return null;
    }

    /// <summary>주변에서 가깝고 시야 안에 있는 것을 고른다.</summary>
    private Interactable SearchNearby()
    {
        Vector3 eye = viewCamera.transform.position;

        int count = Physics.OverlapSphereNonAlloc(eye, nearbyRadius, _overlapBuffer, ~0, QueryTriggerInteraction.Collide);

        Interactable best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var interactable = _overlapBuffer[i].GetComponentInParent<Interactable>();
            if (!IsUsable(interactable)) continue;

            Vector3 toTarget = _overlapBuffer[i].bounds.ClosestPoint(eye) - eye;
            float distance = toTarget.magnitude;
            if (distance > interactable.InteractRange) continue;

            // 등지고 있으면 제외한다.
            float angle = Vector3.Angle(viewCamera.transform.forward, toTarget);
            if (angle > viewAngle) continue;

            // 가까울수록 + 정면일수록 좋은 점수
            float score = distance + angle * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                best = interactable;
            }
        }

        return best;
    }

    private bool IsUsable(Interactable interactable)
    {
        if (interactable == null || !interactable.CanInteract) return false;

        // 각 대상이 정해 둔 Interact Range 를 넘어가면 잡지 않는다.
        float distance = Vector3.Distance(viewCamera.transform.position, interactable.transform.position);

        return distance <= interactable.InteractRange;
    }
}
