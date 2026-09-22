using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 지정한 스테이지가 시작되면 아레나 사방 벽 콜라이더를 활성화해 플레이어가 못 나가게 막는다.
    /// 몬스터는 kinematic Rigidbody + trigger 콜라이더라 벽으로는 못 막으므로,
    /// 같은 시점에 MonsterCombat.SetArenaBounds로 바닥 콜라이더 범위를 넘겨 위치를 직접 고정시킨다.
    /// StageGate와 반대 방향(도달하면 잠긴 채로 계속 유지, 다시 안 열림).
    /// </summary>
    public class ArenaLockdown : MonoBehaviour
    {
        [SerializeField] private int sealAtStage = 2;
        [SerializeField] private Collider[] wallColliders;
        [Tooltip("몬스터 위치를 고정할 기준 범위 - 아레나 바닥(ArenaGround) 콜라이더.")]
        [SerializeField] private Collider arenaFloorCollider;

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            MonsterPurifyManager.OnStageCleared += HandleStageCleared;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            MonsterPurifyManager.OnStageCleared -= HandleStageCleared;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != sealAtStage) return;

            foreach (Collider c in wallColliders) if (c != null) c.enabled = true;
            if (arenaFloorCollider != null) MonsterCombat.SetArenaBounds(arenaFloorCollider.bounds);
        }

        /// <summary>정화 목표를 다 채우면(Stage2 Clear) 벽을 다시 열어 아레나 밖으로 나갈 수 있게 한다.</summary>
        private void HandleStageCleared()
        {
            foreach (Collider c in wallColliders) if (c != null) c.enabled = false;
            MonsterCombat.ClearArenaBounds();
        }
    }
}
