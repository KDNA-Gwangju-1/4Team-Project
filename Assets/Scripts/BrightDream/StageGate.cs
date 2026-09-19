using UnityEngine;

/// <summary>
/// 이전 스테이지를 완료하기 전까지 통로를 물리적으로 막는 게이트.
/// StageProgressManager.CurrentStage가 requiredStage 이상이 되면 콜라이더를 비활성화해 통과를 허용한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StageGate : MonoBehaviour
{
    [Tooltip("이 값 이상으로 CurrentStage가 오르기 전까지 게이트가 막혀 있다.")]
    [SerializeField] private int requiredStage = 1;

    private Collider blockingCollider;

    private void Awake()
    {
        blockingCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        StageProgressManager.OnStageChanged += HandleStageChanged;
        int currentStage = StageProgressManager.Instance != null ? StageProgressManager.Instance.CurrentStage : 0;
        UpdateGateState(currentStage);
    }

    private void OnDisable()
    {
        StageProgressManager.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int currentStage)
    {
        UpdateGateState(currentStage);
    }

    private void UpdateGateState(int currentStage)
    {
        blockingCollider.enabled = currentStage < requiredStage;
    }
}
