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

        [Tooltip("단서가 벤치처럼 뭉뚱그린 충돌 상자 안에 놓여 있어도, 먼저 맞은 상자 뒤로 이 거리 안이면 단서를 고른다.")]
        [SerializeField] private float clueSeeThroughDepth = 0.6f;

        private ClueInteractable currentTarget;
        private WeaponPickup currentWeapon;
        private string cluePrompt;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

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
            // 조작 안내 문구가 떠 있는 동안에도 입력을 받지 않는다.
            if (BrightDreamControlGuide.Blocking) return;
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
            if (playerCamera != null)
            {
                // 벤치 같은 소품은 좌석 위 빈 공간까지 덮는 상자 하나로 충돌을 잡아 둬서, 그 위에 놓인 단서의
                // 판정 상자가 통째로 파묻힌다. 첫 번째 단단한 충돌면 뒤로 조금(clueSeeThroughDepth)까지는
                // 단서를 찾아본다 - 벽 너머처럼 멀리 가려진 단서는 그대로 안 잡힌다.
                int count = Physics.RaycastNonAlloc(playerCamera.transform.position, playerCamera.transform.forward, hitBuffer, interactDistance, ~0, QueryTriggerInteraction.Collide);
                System.Array.Sort(hitBuffer, 0, count, HitDistanceComparer.Instance);
                float blockDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = hitBuffer[i];
                    if (hit.distance > blockDistance + clueSeeThroughDepth) break;

                    ClueInteractable clue = hit.collider.GetComponentInParent<ClueInteractable>();
                    if (clue != null && !clue.IsCollected) { hitClue = clue; break; }
                    // 정화총은 예전처럼 가려지지 않았을 때만 집는다.
                    WeaponPickup weapon = hit.distance <= blockDistance ? hit.collider.GetComponentInParent<WeaponPickup>() : null;
                    if (weapon != null && weapon.CanInteract) { hitWeapon = weapon; break; }

                    if (!hit.collider.isTrigger && float.IsPositiveInfinity(blockDistance)) blockDistance = hit.distance;
                }
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

        private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
