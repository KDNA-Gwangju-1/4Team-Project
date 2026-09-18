using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Clues
{
    /// <summary>
    /// 카메라 기준 짧은 Raycast 로 플레이어가 바라보고 있는 단서를 감지해 E 상호작용을 처리한다.
    /// 기존 플레이어 이동/시점 스크립트(SimpleFirstPersonController)는 건드리지 않고 독립적으로 동작하며,
    /// 같은 Main Camera 를 참조만 한다.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 3.5f;
        [SerializeField] private Text promptText;

        private ClueInteractable currentTarget;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        private void Update()
        {
            DetectTarget();

            if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
            {
                currentTarget.Investigate();
                SetPrompt(false);
                currentTarget = null;
            }
        }

        private void DetectTarget()
        {
            ClueInteractable hitClue = null;
            if (playerCamera != null &&
                Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                hitClue = hit.collider.GetComponentInParent<ClueInteractable>();
                if (hitClue != null && hitClue.IsCollected) hitClue = null;
            }

            if (hitClue != currentTarget)
            {
                if (currentTarget != null) currentTarget.SetHighlighted(false);
                currentTarget = hitClue;
                if (currentTarget != null) currentTarget.SetHighlighted(true);
                SetPrompt(currentTarget != null);
            }
        }

        private void SetPrompt(bool show)
        {
            if (promptText != null) promptText.gameObject.SetActive(show);
        }
    }
}
