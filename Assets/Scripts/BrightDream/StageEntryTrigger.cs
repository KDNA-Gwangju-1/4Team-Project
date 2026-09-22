using UnityEngine;

/// <summary>
/// 플레이어가 이 트리거 영역을 지나가면 화면 중앙에 안내 문구를 한 번 띄운다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StageEntryTrigger : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string message = "Stage 1 단서를 찾아라";
    [SerializeField] private float displayDuration = 4f;
    [SerializeField] private bool triggerOnce = true;
    [Tooltip("진행 순서. StageProgressManager 상 바로 다음 순서가 아니면 트리거가 무시된다.")]
    [SerializeField] private int stageIndex = 1;
    [Tooltip("켜두면 WeaponPickup.PlayerHasWeapon이 true일 때만(정화총을 먹은 뒤에만) 트리거가 동작한다.")]
    [SerializeField] private bool requireWeapon;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (other.GetComponentInParent<CharacterController>() == null) return;
        if (requireWeapon && !WeaponPickup.PlayerHasWeapon) return;
        if (StageProgressManager.Instance != null && !StageProgressManager.Instance.TryCompleteStage(stageIndex)) return;

        hasTriggered = true;
        if (!string.IsNullOrEmpty(message)) StageMessageUI.Instance?.ShowMessage(message, displayDuration);
    }
}
