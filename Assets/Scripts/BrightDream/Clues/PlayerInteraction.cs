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
        [SerializeField] private CrosshairController crosshair;

        private ClueInteractable currentTarget;
        private WeaponPickup currentWeapon;
        private string cluePrompt;

        private void Awake()
        {
            ChapterNoticeStyle.Apply(promptText, ChapterDialogueSkin.Theme.BrightDream);
            if (playerCamera == null) playerCamera = Camera.main;
            if (promptText != null) promptText.gameObject.SetActive(false);
            cluePrompt = promptText != null ? promptText.text : "E 조사하기";
        }

        private void Update()
        {
            // ESC 일시정지 중에는 입력을 받지 않는다.
            if (PauseMenu.IsPaused) return;
            DetectTarget();

            if (currentWeapon != null && Input.GetKeyDown(KeyCode.E))
            {
                currentWeapon.TryInteract();
                currentWeapon = null;
                SetPrompt(false);
                if (crosshair != null) crosshair.SetHovering(false);
                return;
            }

            if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
            {
                currentTarget.Investigate();
                SetPrompt(false);
                if (crosshair != null) crosshair.SetHovering(false);
                currentTarget = null;
            }
        }

        private void DetectTarget()
        {
            ClueInteractable hitClue = null;
            WeaponPickup hitWeapon = null;
            if (playerCamera != null &&
                Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                hitClue = hit.collider.GetComponentInParent<ClueInteractable>();
                if (hitClue != null && hitClue.IsCollected) hitClue = null;
                hitWeapon = hit.collider.GetComponentInParent<WeaponPickup>();
                if (hitWeapon != null && !hitWeapon.CanInteract) hitWeapon = null;
            }

            if (hitClue != currentTarget || hitWeapon != currentWeapon)
            {
                if (currentTarget != null) currentTarget.SetHighlighted(false);
                currentTarget = hitClue;
                currentWeapon = hitWeapon;
                if (currentTarget != null) currentTarget.SetHighlighted(true);
                if (promptText != null) promptText.text = currentWeapon != null ? "E  정화총 획득하기" : cluePrompt;
                SetPrompt(currentTarget != null || currentWeapon != null);
                if (crosshair != null) crosshair.SetHovering(currentTarget != null || currentWeapon != null);
            }
        }

        private void SetPrompt(bool show)
        {
            if (promptText != null) promptText.gameObject.SetActive(show);
        }
    }
}
