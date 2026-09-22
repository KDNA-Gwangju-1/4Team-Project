using UnityEngine;
using BrightDream.Clues;

namespace BrightDream.Combat
{
    /// <summary>
    /// 테스트용 스테이지 스킵 치트키.
    ///
    /// F9  정화총을 즉시 지급하고 Stage1·Stage2를 강제로 완료시켜 "Stage2 클리어" 상태로 만든다
    ///     - 보스 스테이지 입구까지 걸어가는 것만 남기고 나머지 진행(단서 수집,
    ///     Stage2 몬스터 10마리 정화)을 전부 건너뛴다.
    /// F10 보스 스테이지를 클리어한다. 어디서 눌러도 되도록 보스 스테이지까지 진행시킨 뒤
    ///     등장 연출을 건너뛰고 보스를 처치한다.
    /// </summary>
    public class DebugStageSkip : MonoBehaviour
    {
        [SerializeField] private KeyCode skipKey = KeyCode.F9;
        [Tooltip("보스 스테이지를 즉시 클리어한다.")]
        [SerializeField] private KeyCode bossClearKey = KeyCode.F10;
        [Tooltip("보스 스테이지 번호. BossAI 의 Activate At Stage 와 같은 값.")]
        [SerializeField] private int bossStageIndex = 3;
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
            if (Input.GetKeyDown(bossClearKey)) ClearBossStage();
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

        /// <summary>
        /// 보스 스테이지를 클리어한다. 보스 처치 경로(BossWeakpointController.Defeat)를 그대로
        /// 타기 때문에 아레나 개방, "Chapter1 Clear", 균열 연출이 실제 플레이와 똑같이 이어진다.
        /// </summary>
        private void ClearBossStage()
        {
            StageProgressManager progress = StageProgressManager.Instance;
            if (progress == null) return;

            // 아직 보스 스테이지 전이라면 거기까지 밀어 올린다. TryCompleteStage 는 바로 다음
            // 순서만 인정하므로 한 칸씩 올려야 한다.
            while (progress.CurrentStage < bossStageIndex)
            {
                if (!progress.TryCompleteStage(progress.CurrentStage + 1)) break;
            }

            // 보스가 균열에서 나오는 연출 도중이면 건너뛰어 전투 상태로 만든다.
            // 이걸 거치지 않으면 보스가 숨겨진 채로 클리어 연출이 돌아 아무것도 안 보인다.
            BossRiftEntrance entrance = FindObjectOfType<BossRiftEntrance>();
            if (entrance != null) entrance.SkipToCombat();

            if (BossWeakpointController.Instance != null)
                BossWeakpointController.Instance.DebugDefeat();
        }
    }
}
