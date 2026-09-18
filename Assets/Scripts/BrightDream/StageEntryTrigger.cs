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

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (other.GetComponentInParent<CharacterController>() == null) return;

        hasTriggered = true;
        StageMessageUI.Instance?.ShowMessage(message, displayDuration);
    }
}
