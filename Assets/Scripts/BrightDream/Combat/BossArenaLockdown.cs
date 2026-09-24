using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스 스테이지에 진입하면 05_boss_platform 사방 벽을 활성화해 "Chapter1 Clear"가 뜨기 전까지
    /// 플레이어가 아레나 밖으로 못 나가게 막는다. 보스가 정화 완료(BossWeakpointController.OnBossDefeated)되면
    /// 벽을 다시 열고 클리어 메시지를 띄운 뒤 다음 스테이지로 진행시킨다.
    /// ArenaLockdown과 같은 벽 토글 패턴이지만, 해제 조건이 정화 목표(MonsterPurifyManager)가 아니라
    /// 보스 처치라 별도 클래스로 둔다.
    /// </summary>
    public class BossArenaLockdown : MonoBehaviour
    {
        [SerializeField] private int sealAtStage = 3;
        [SerializeField] private Collider[] wallColliders;
        [Tooltip("몬스터 위치를 고정할 기준 범위 - 05_boss_platform의 MeshCollider.")]
        [SerializeField] private Collider arenaFloorCollider;
        [Tooltip("보스 처치 후 이 값으로 스테이지를 진행시킨다 (StageProgressManager 순서 강제용).")]
        [SerializeField] private int clearStageIndex = 4;

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            BossWeakpointController.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != sealAtStage) return;

            foreach (Collider c in wallColliders) if (c != null) c.enabled = true;
            if (arenaFloorCollider != null) MonsterCombat.SetArenaBounds(arenaFloorCollider.bounds);
        }

        /// <summary>보스 처치 순간 벽을 열고, 남은 몬스터를 정리하고, "Chapter1 Clear"를 띄운다.</summary>
        private void HandleBossDefeated()
        {
            foreach (Collider c in wallColliders) if (c != null) c.enabled = false;
            MonsterCombat.ClearArenaBounds();
            MonsterCombat.DespawnAll();

            StageProgressManager.Instance?.TryCompleteStage(clearStageIndex);
            StageMessageUI.Instance?.ShowMessage("챕터 1 클리어");
        }
    }
}
