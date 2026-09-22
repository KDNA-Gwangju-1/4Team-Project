using UnityEngine;
using BrightDream.Clues;

namespace BrightDream.Combat
{
    /// <summary>
    /// 테스트용 스테이지 스킵 치트키. skipKey(기본 F9)를 누르면 정화총을 즉시 지급하고
    /// Stage1·Stage2를 강제로 완료시켜 "Stage2 클리어" 상태로 만든다 - 보스 스테이지 입구까지
    /// 걸어가는 것만 남기고 나머지 진행(단서 수집, Stage2 몬스터 10마리 정화)을 전부 건너뛴다.
    /// </summary>
    public class DebugStageSkip : MonoBehaviour
    {
        [SerializeField] private KeyCode skipKey = KeyCode.F9;
        [SerializeField] private WeaponPickup weaponPickup;
        [Tooltip("F9를 누르면 이 위치·각도로 플레이어를 순간이동시킨다 (매번 걸어가지 않아도 되게).")]
        [SerializeField] private Transform teleportTarget;

        private SimpleFirstPersonController playerController;

        private void Awake()
        {
            playerController = GetComponent<SimpleFirstPersonController>();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(skipKey)) SkipToStage2Cleared();
#endif
        }

        private void SkipToStage2Cleared()
        {
            if (StageProgressManager.Instance == null) return;

            if (weaponPickup != null) weaponPickup.DebugGrant();

            // TryCompleteStage(1)이 ClueManager의 단서 체크리스트 UI를 다시 켜므로,
            // 그 이후에 꺼야 계속 꺼진 채로 유지된다.
            StageProgressManager.Instance.TryCompleteStage(1);
            ClueManager.Instance?.DebugHideProgressUI();
            StageProgressManager.Instance.TryCompleteStage(2);

            // Stage2 정화 목표를 강제로 채운다 - MonsterPurifyManager가 원래 로직 그대로
            // "Stage2 Clear" 메시지 표시, 아레나 봉쇄 해제, 체력 회복까지 처리해 준다.
            for (int i = 0; i < MonsterPurifyManager.TargetCount; i++)
                MonsterPurifyManager.Instance?.RegisterPurify();

            if (playerController != null && teleportTarget != null)
                playerController.Teleport(teleportTarget.position, teleportTarget.eulerAngles.y);
        }
    }
}
