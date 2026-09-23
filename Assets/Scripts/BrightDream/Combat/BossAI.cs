using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 06_unicorn_boss 전투 AI. NavMeshAgent로 플레이어를 추적하다가 일정 쿨타임마다
    /// 몸통 박치기 / 점프 착지 공격을 번갈아 실행한다. 공격 패턴과 별개로, 보스 몸에 그냥
    /// 부딪히기만 해도(OnTriggerEnter) 하트 반개의 접촉 피해를 준다.
    ///
    /// 몸통 박치기는 보스 자신의 몸 반경(bodyContactRadius) 안에 있을 때만 맞고 벗어나면 회피된다.
    /// 점프 착지는 뛰어오르기 전에 자세를 낮추고 바닥에 피격 반경을 그리는 차지 구간이 있다.
    /// 판정은 그 반경 안이면서 05_boss_platform 위일 때만 들어가고, 착지하는 그 순간 플레이어가
    /// 바닥에 붙어 있지 않으면(점프해서 공중에 있으면) 피해를 받지 않는다 - 유예 시간 없이 그 프레임만 본다.
    /// 즉 회피 수단이 점프와 반경 밖으로 빠지기 두 가지다.
    ///
    /// 06_unicorn_boss.glb에는 아직 애니메이션이 없어서, 이동/공격 모션은 전부 코드로 만든
    /// 임시 연출(Transform 직접 이동)이다. 나중에 실제 애니메이션이 들어오면 이 코루틴들의
    /// Transform 이동 부분만 애니메이션 재생으로 바꾸면 되고, 판정 타이밍/로직은 그대로 쓰면 된다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BossAI : MonoBehaviour
    {
        [Tooltip("점프 착지 피해 판정 범위 - 05_boss_platform의 MeshCollider를 그대로 물린다.")]
        [SerializeField] private Collider arenaFloorCollider;
        [Tooltip("StageProgressManager가 이 스테이지에 도달하기 전까지(= Boss Stage 문구가 뜨기 전까지) 가만히 서서 기다린다.")]
        [SerializeField] private int activateAtStage = 3;
        [SerializeField] private float attackCooldown = 5f;
        [SerializeField] private float contactDamage = 20f; // 하트 1개 (박치기 / 점프 착지)
        [Tooltip("공격 패턴과 무관하게, 그냥 몸에 부딪히기만 해도 주는 피해 - 하트 반개.")]
        [SerializeField] private float bodyBumpDamage = 10f;

        [Header("몸통 박치기")]
        [Tooltip("이 거리 안에 플레이어가 있으면 박치기에 맞는다 - 보스 몸 자체 크기에 맞춘 반경.")]
        [SerializeField] private float bodyContactRadius = 3f;
        [Tooltip("돌진 시 실제로 앞으로 이동하는 거리 - 클수록 더 멀리서부터 돌진해 온다.")]
        [SerializeField] private float headbuttLungeDistance = 4f;
        [SerializeField] private float headbuttLungeDuration = 0.35f;
        [Tooltip("돌진하기 전에 방향을 정하고 바닥에 경로를 그리는 시간. 이 동안 피해 판정은 없다.")]
        [SerializeField] private float headbuttChargeDuration = 0.8f;
        [Tooltip("바닥 돌진 경로 표시. 비우면 같은 오브젝트에서 찾고, 없으면 표시 없이 진행한다.")]
        [SerializeField] private BossChargeIndicator chargeIndicator;

        [Header("점프 착지")]
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float jumpDuration = 0.8f;
        [Tooltip("뛰어오르기 전에 자세를 낮추고 바닥에 반경을 그리는 시간. 이 동안 피해 판정은 없다.")]
        [SerializeField] private float slamChargeDuration = 1.1f;
        [Tooltip("내려찍기 피해 반경(m). 이 밖으로 걸어 나가면 맞지 않는다.")]
        [SerializeField] private float slamRadius = 9f;
        [Tooltip("차지 동안 몸을 눌러 주는 정도. 0 이면 자세 변화 없음.")]
        [SerializeField] private float slamCrouch = 0.25f;
        [Tooltip("바닥 반경 표시. 비우면 같은 오브젝트에서 찾고, 없으면 표시 없이 진행한다.")]
        [SerializeField] private BossSlamIndicator slamIndicator;

        /// <summary>일반 몬스터(MonsterCombat)가 보스와 겹치지 않게 피해 다닐 때 참조하는 보스 위치.</summary>
        public static BossAI Instance { get; private set; }

        private NavMeshAgent agent;
        private Transform player;
        private bool nextIsHeadbutt = true;
        private bool isAttacking;
        private bool isDefeated;
        private bool isActive;

        private void Awake()
        {
            Instance = this;
            agent = GetComponent<NavMeshAgent>();
            agent.isStopped = true; // Boss Stage가 뜨기 전까지는 가만히 있는다.
            if (chargeIndicator == null) chargeIndicator = GetComponent<BossChargeIndicator>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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

        private void Start()
        {
            if (PlayerHealth.Instance != null) player = PlayerHealth.Instance.transform;

            // 이미 Boss Stage를 지난 시점(예: 테스트 재시작)이면 바로 활성화하고,
            // 아니라면 StageProgressManager가 Boss Stage에 도달할 때까지 기다린다.
            if (StageProgressManager.Instance != null && StageProgressManager.Instance.CurrentStage >= activateAtStage)
                Activate();
        }

        private void HandleStageChanged(int currentStage)
        {
            if (!isActive && currentStage >= activateAtStage) Activate();
        }

        private void Activate()
        {
            if (isActive || isDefeated) return;
            isActive = true;
            agent.isStopped = false;
            StartCoroutine(AttackLoop());
        }

        private void Update()
        {
            if (!isActive || isDefeated || isAttacking || player == null) return;
            agent.SetDestination(player.position);
        }

        private IEnumerator AttackLoop()
        {
            while (!isDefeated)
            {
                yield return new WaitForSeconds(attackCooldown);
                if (isDefeated) yield break;

                // 약점 노출과 공격 패턴은 서로 독립적으로 돌아간다 - 노출 중에 공격이 발동돼도
                // 노출을 닫지 않고 그대로 둔 채(=노출된 채로) 패턴을 실행한다.
                if (nextIsHeadbutt) yield return DoHeadbutt();
                else yield return DoGroundSlam();
                nextIsHeadbutt = !nextIsHeadbutt;
            }
        }

        private IEnumerator DoHeadbutt()
        {
            isAttacking = true;
            BeginManualMove();

            Vector3 start = transform.position;
            Vector3 dir = player != null ? (player.position - start) : transform.forward;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            transform.forward = dir;
            Vector3 target = start + dir * headbuttLungeDistance;

            // 차지 - 방향을 이 시점에 고정하고, 실제 판정 영역(몸 반경 원이 돌진 경로를 쓸고 가는 모양)을
            // 바닥에 그린다. 이 동안은 피해가 없어서 플레이어가 경로 밖으로 비켜설 수 있다.
            if (chargeIndicator != null) chargeIndicator.Show(start, dir, headbuttLungeDistance, bodyContactRadius);
            float charge = 0f;
            while (charge < headbuttChargeDuration)
            {
                charge += Time.deltaTime;
                if (chargeIndicator != null) chargeIndicator.SetFill(charge / Mathf.Max(headbuttChargeDuration, 0.0001f));
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < headbuttLungeDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / headbuttLungeDuration);
                // 돌진 중 매 프레임 범위를 다시 확인한다 - 범위 안에 있을 때만 맞고, 벗어나면 회피된다.
                TryDamagePlayerInBodyRange();
                yield return null;
            }
            if (chargeIndicator != null) chargeIndicator.Hide();

            elapsed = 0f;
            while (elapsed < headbuttLungeDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(target, start, elapsed / headbuttLungeDuration);
                yield return null;
            }
            transform.position = start;

            EndManualMove();
            isAttacking = false;
        }

        private IEnumerator DoGroundSlam()
        {
            isAttacking = true;
            BeginManualMove();
            Vector3 groundPos = transform.position;

            // 차지 - 자세를 낮추고 바닥에 피격 반경을 그린다. 이 동안은 피해가 없어서
            // 플레이어가 반경 밖으로 걸어 나가거나 점프 타이밍을 잡을 수 있다.
            if (slamIndicator != null) slamIndicator.Show(groundPos, slamRadius);

            Vector3 baseScale = transform.localScale;
            float charge = 0f;
            while (charge < slamChargeDuration)
            {
                charge += Time.deltaTime;
                float p = Mathf.Clamp01(charge / Mathf.Max(slamChargeDuration, 0.0001f));
                if (slamIndicator != null) slamIndicator.SetFill(p);
                // 끝으로 갈수록 더 눌린다 - 튀어오르기 직전이 제일 낮다.
                transform.localScale = new Vector3(
                    baseScale.x * (1f + slamCrouch * 0.35f * p),
                    baseScale.y * (1f - slamCrouch * p),
                    baseScale.z * (1f + slamCrouch * 0.35f * p));
                yield return null;
            }
            transform.localScale = baseScale;

            // 위로 뛰어오른다.
            float upDuration = jumpDuration * 0.5f;
            float elapsed = 0f;
            while (elapsed < upDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / upDuration;
                transform.position = groundPos + Vector3.up * (jumpHeight * Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            // 착지한다.
            float downDuration = jumpDuration - upDuration;
            elapsed = 0f;
            while (elapsed < downDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / downDuration;
                transform.position = groundPos + Vector3.up * (jumpHeight * (1f - Mathf.Sin(t * Mathf.PI * 0.5f)));
                yield return null;
            }
            transform.position = groundPos;

            // 착지한 이 프레임이 유일한 판정 순간이다 - 유예 시간 없음.
            TryDamagePlayerOnLanding(groundPos);

            if (slamIndicator != null) slamIndicator.Hide();
            CameraShake.Instance?.Shake();

            EndManualMove();
            isAttacking = false;
        }

        /// <summary>공격 중 Transform을 직접 움직이는 동안 NavMeshAgent가 위치를 되돌리지 못하게 막는다.</summary>
        private void BeginManualMove()
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        /// <summary>수동 이동이 끝나면 에이전트의 내부 위치를 현재 Transform과 다시 맞춘 뒤 제어를 돌려준다.</summary>
        private void EndManualMove()
        {
            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        /// <summary>몸통 박치기 - 보스 자신의 몸 반경 안에 플레이어가 있을 때만 피해를 준다.</summary>
        private void TryDamagePlayerInBodyRange()
        {
            if (player == null) return;
            if (Vector3.Distance(transform.position, player.position) > bodyContactRadius) return; // 범위 밖 - 회피 성공

            ApplyDamage(contactDamage);
        }

        /// <summary>
        /// 점프 착지 판정. 세 가지를 모두 만족해야 맞는다.
        ///   1. 05_boss_platform 범위 안에 있을 것
        ///   2. 차지 때 그려 준 반경(slamRadius) 안에 있을 것
        ///   3. 그 순간 바닥에 붙어 있을 것 (점프해 있으면 회피)
        ///
        /// 예전에는 플랫폼 전체가 판정이라 피할 방법이 점프뿐이었다. 차지 중에 반경을
        /// 보여 주기로 한 이상 그 원 밖으로 걸어 나가는 것도 회피가 되어야 표시가 거짓말이
        /// 되지 않는다.
        /// </summary>
        private void TryDamagePlayerOnLanding(Vector3 slamCenter)
        {
            if (player == null) return;

            Vector3 flatCenter = new Vector3(slamCenter.x, player.position.y, slamCenter.z);
            if (Vector3.Distance(flatCenter, player.position) > slamRadius) return; // 반경 밖 - 회피 성공

            if (arenaFloorCollider != null)
            {
                Bounds b = arenaFloorCollider.bounds;
                Vector3 flatPlayer = new Vector3(player.position.x, b.center.y, player.position.z);
                if (!b.Contains(flatPlayer)) return; // 플랫폼 밖 - 판정 대상 아님
            }

            SimpleFirstPersonController controller = player.GetComponent<SimpleFirstPersonController>();
            if (controller != null && !controller.IsGrounded) return; // 점프해서 회피 성공

            ApplyDamage(contactDamage);
        }

        /// <summary>공격 패턴과 무관하게 보스 몸에 그냥 부딪히기만 해도 하트 반개를 준다.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (isDefeated) return;
            if (other.GetComponentInParent<CharacterController>() == null) return;

            ApplyDamage(bodyBumpDamage);
        }

        private void ApplyDamage(float amount)
        {
            if (PlayerHealth.Instance == null || PlayerHealth.Instance.IsInvincible) return;
            PlayerHealth.Instance.TakeDamage(amount, grantInvincibility: true);
            CameraShake.Instance?.Shake();
        }

        private void HandleBossDefeated()
        {
            isDefeated = true;
            StopAllCoroutines();
            agent.isStopped = true;
            // 공격 도중에 처치되면 코루틴이 끊겨 바닥 표시가 남으므로 여기서 직접 끈다.
            if (chargeIndicator != null) chargeIndicator.Hide();
            if (slamIndicator != null) slamIndicator.Hide();
        }
    }
}
