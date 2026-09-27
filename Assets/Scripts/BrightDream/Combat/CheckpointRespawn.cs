using UnityEngine;
using BrightDream.Clues;

namespace BrightDream.Combat
{
    /// <summary>
    /// 죽었다가 리트라이(씬 리로드)했을 때 어디서부터 다시 시작할지 정한다.
    ///
    /// GameOverController.TriggerGameOver()가 죽는 순간의 StageProgressManager.CurrentStage를
    /// RecordDeath()로 기록해 둔다 - static이라 GameOverController.RestartScene()의 씬 리로드에도
    /// 남는다. 새로 로드된 씬의 Start()에서 그 값을 보고 DebugStageSkip과 같은 방식으로 진행
    /// 상태를 재생시킨 뒤 텔레포트한다.
    ///
    ///   CurrentStage 0~1(Stage1 중 사망)         → 아무것도 하지 않는다 (씬 맨 처음부터).
    ///   CurrentStage 2 (Stage2 중 사망)           → "Stage1 완료, 정화총 줍기 전"으로:
    ///                                              TryCompleteStage(1)만 밟고 픽업만 드러내며
    ///                                              무기는 지급하지 않는다.
    ///   CurrentStage 3 이상(보스 스테이지 중 사망) → "Stage2 완료" 상태로: 무기 지급 +
    ///                                              몬스터 정화 10회 + TryCompleteStage(1)→(2).
    ///                                              Stage3 진입은 플레이어가 BossStageTrigger를
    ///                                              직접 다시 밟게 두어(등장 연출 재생) "보스
    ///                                              스테이지 초입부터"를 그대로 만족시킨다.
    ///
    /// TryCompleteStage(1)은 TimeAttackTimer.HandleStageChanged를 통해 타이머도 함께 새로 시작시킨다.
    /// </summary>
    [RequireComponent(typeof(SimpleFirstPersonController))]
    public class CheckpointRespawn : MonoBehaviour
    {
        [SerializeField] private WeaponPickup weaponPickup;
        [Tooltip("Stage2 중 사망(체크포인트) 시 텔레포트할 위치 - 정화총 픽업 근처.")]
        [SerializeField] private Transform stage1ClearedRespawnPoint;
        [Tooltip("보스 스테이지 중 사망(체크포인트) 시 텔레포트할 위치 - 보스 스테이지 트리거 바로 앞.")]
        [SerializeField] private Transform stage2ClearedRespawnPoint;
        [Tooltip("체크포인트로 재시작할 때 같이 꺼야 하는 씬 시작 연출 - 조작 안내창(ClueCanvas).")]
        [SerializeField] private BrightDreamControlGuide controlGuide;

        private static int checkpointStage;

        private SimpleFirstPersonController playerController;
        private IntroDialogueTrigger introDialogueTrigger;

        private void Awake()
        {
            playerController = GetComponent<SimpleFirstPersonController>();
            introDialogueTrigger = GetComponent<IntroDialogueTrigger>();
        }

        /// <summary>죽는 순간 호출해 다음 리트라이가 시작할 지점을 기록한다.</summary>
        public static void RecordDeath()
        {
            checkpointStage = StageProgressManager.Instance != null ? StageProgressManager.Instance.CurrentStage : 0;
        }

        /// <summary>메인 메뉴로 나가는 등 새 판을 시작할 때 기록을 지운다.</summary>
        public static void ResetCheckpoint()
        {
            checkpointStage = 0;
        }

        private void Start()
        {
            if (checkpointStage < 2) return;

            // 체크포인트로 재시작하는 거라 씬 맨 처음에만 나와야 하는 조작 안내창/도입부
            // 대사(첫 이동 감지)는 건너뛴다.
            if (introDialogueTrigger != null) introDialogueTrigger.enabled = false;
            if (controlGuide != null) controlGuide.ForceClose();

            StageProgressManager progress = StageProgressManager.Instance;
            if (progress == null) return;

            // WeaponPickup의 자동 노출, StageGate의 requireAllCluesCollected 등 실제 단서
            // 개수를 보는 모든 곳이 정상 통과하도록 단서 4개를 진짜로 모은 것으로 처리한다.
            ClueManager.Instance?.DebugCollectAll();

            if (checkpointStage == 2)
            {
                progress.TryCompleteStage(1);
                ClueManager.Instance?.DebugHideProgressUI();
                if (weaponPickup != null) weaponPickup.DebugReveal();
                Teleport(stage1ClearedRespawnPoint);
                return;
            }

            // 3 이상 - 보스 스테이지 중 사망: Stage2 완료 상태로.
            if (weaponPickup != null) weaponPickup.DebugGrant();
            progress.TryCompleteStage(1);
            ClueManager.Instance?.DebugHideProgressUI();
            progress.TryCompleteStage(2);
            for (int i = 0; i < MonsterPurifyManager.TargetCount; i++)
                MonsterPurifyManager.Instance?.RegisterPurify();
            Teleport(stage2ClearedRespawnPoint);
        }

        private void Teleport(Transform target)
        {
            if (target != null && playerController != null)
                playerController.Teleport(target.position, target.eulerAngles.y);
        }
    }
}
