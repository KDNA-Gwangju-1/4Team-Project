using UnityEngine;
using BrightDream.Clues;

/// <summary>
/// 이전 스테이지를 완료하기 전까지 통로를 물리적으로 막는 게이트.
/// StageProgressManager.CurrentStage가 requiredStage 이상이 되면 콜라이더를 비활성화해 통과를 허용한다.
/// requireAllCluesCollected가 켜져 있으면 단서 4개를 모두 모으기 전까지는 스테이지 조건을 만족해도 열리지 않는다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StageGate : MonoBehaviour
{
    [Tooltip("이 값 이상으로 CurrentStage가 오르기 전까지 게이트가 막혀 있다.")]
    [SerializeField] private int requiredStage = 1;

    [Tooltip("체크하면 ClueManager 기준 단서 4개를 모두 모으기 전까지 게이트가 막힌다.")]
    [SerializeField] private bool requireAllCluesCollected = false;

    private Collider blockingCollider;

    private void Awake()
    {
        blockingCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        StageProgressManager.OnStageChanged += HandleStageChanged;
        RefreshState();
    }

    private void OnDisable()
    {
        StageProgressManager.OnStageChanged -= HandleStageChanged;
    }

    private void Update()
    {
        // 단서 수집은 별도 이벤트 구독 없이 매 프레임 재확인한다 (초기화 순서 이슈 회피, 게이트 개수가 적어 비용 무시 가능).
        if (requireAllCluesCollected) RefreshState();
    }

    private void HandleStageChanged(int currentStage)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        int currentStage = StageProgressManager.Instance != null ? StageProgressManager.Instance.CurrentStage : 0;
        bool stageLocked = currentStage < requiredStage;
        bool cluesLocked = requireAllCluesCollected
            && (ClueManager.Instance == null || ClueManager.Instance.CollectedCount < ClueManager.TotalClueCount);
        blockingCollider.enabled = stageLocked || cluesLocked;
    }
}
