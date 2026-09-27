using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 06_unicorn_boss 전투 AI. NavMeshAgent로 플레이어를 추적하다가 페이즈별 쿨타임마다 공격 패턴을 실행한다.
    /// 공격 패턴과 별개로, 보스 몸에 그냥 부딪히기만 해도(OnTriggerEnter) 하트 반개의 접촉 피해를 준다.
    ///
    /// 페이즈 - 약점 명중 진행도(BossWeakpointController.HitProgress)로 나뉜다.
    ///   1페이즈 (시작)     : 몸통 박치기 / 점프 착지 번갈아
    ///   2페이즈 (명중 2회) : + 별똥별 낙하
    ///   3페이즈 (명중 4회) : 박치기가 3연속 돌진으로 강화, + 회전 뿔 레이저, 별똥별 개수 증가
    /// 페이즈가 바뀌면 포효(Roar)하며 충격파로 플레이어를 밀어낸다.
    ///
    /// 패턴마다 회피 방법이 겹치지 않게 했다.
    ///   박치기/연속 돌진 : 바닥에 그린 경로 밖으로
    ///   점프 착지        : 반경 밖으로 나가거나, 착지 순간 점프
    ///   별똥별           : 원 밖으로 걸어 나가기만 (점프로는 못 피한다)
    ///   회전 뿔 레이저   : 빔이 도는 방향과 같은 쪽으로 보스 주위를 달리기 (점프로는 못 넘는다)
    ///
    /// 모션은 BossAnimationDriver에 직접 지시한다. 판정 타이밍은 전부 이 코드가 쥐고 있어서,
    /// 나중에 모션을 교체해도 클립 길이만 대략 맞추면 판정은 그대로다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BossAI : MonoBehaviour
    {
        private enum Pattern { Headbutt, ChainCharge, GroundSlam, Starfall, HornLaser }

        [Tooltip("점프 착지 피해 판정 범위 - 05_boss_platform의 MeshCollider를 그대로 물린다.")]
        [SerializeField] private Collider arenaFloorCollider;
        [Tooltip("StageProgressManager가 이 스테이지에 도달하기 전까지(= Boss Stage 문구가 뜨기 전까지) 가만히 서서 기다린다.")]
        [SerializeField] private int activateAtStage = 3;
        [SerializeField] private float contactDamage = 20f; // 하트 1개 (박치기 / 점프 착지)
        [Tooltip("공격 패턴과 무관하게, 그냥 몸에 부딪히기만 해도 주는 피해 - 하트 반개.")]
        [SerializeField] private float bodyBumpDamage = 10f;
        [Tooltip("몸에 부딪혔을 때 보스 반대 방향으로 밀어내는 힘(수평).")]
        [SerializeField] private float bodyBumpKnockback = 6f;
        [Tooltip("밀려날 때 살짝 떠오르는 정도. 0이면 수평으로만 밀린다.")]
        [SerializeField] private float bodyBumpKnockbackUpward = 1.5f;

        [Header("페이즈")]
        [Tooltip("이 약점 명중 횟수에 도달하면 다음 페이즈로 넘어간다 (2페이즈, 3페이즈 순).")]
        [SerializeField] private int[] phaseHitThresholds = { 2, 4 };
        [Tooltip("페이즈별 공격 쿨타임(초) - 1, 2, 3페이즈 순.")]
        [SerializeField] private float[] phaseCooldowns = { 5f, 4f, 3.5f };
        [SerializeField] private float transitionShockwaveRadius = 10f;
        [SerializeField] private float transitionKnockback = 9f;
        [SerializeField] private float transitionKnockbackUpward = 2.5f;
        [Header("패턴 순서 (2페이즈부터)")]
        [Tooltip("큰 패턴(별똥별·회전 레이저) 사이에 끼우는 작은 패턴(박치기·찍기·연속 돌진) 개수. 작은 패턴끼리는 무작위.")]
        [SerializeField] private int smallPatternsBetweenBig = 2;
        [Tooltip("별똥별이 끝난 뒤 평소 쿨타임에 더해 쉬는 시간(초).")]
        [SerializeField] private float restAfterStarfall = 1.5f;
        [Tooltip("회전 레이저가 끝난 뒤 평소 쿨타임에 더해 쉬는 시간(초).")]
        [SerializeField] private float restAfterLaser = 2.5f;

        [Tooltip("Roar 클립에서 앞발이 땅을 찍는 시점(초) - 충격파가 여기에 맞춰 터진다.")]
        [SerializeField] private float roarImpactTime = 1.45f;
        [SerializeField] private float roarDuration = 1.8f;

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

        [Header("3연속 돌진 (3페이즈)")]
        [SerializeField] private int chainChargeCount = 3;
        [SerializeField] private float chainFirstChargeDuration = 0.75f;
        [Tooltip("두 번째 돌진부터는 조준 시간이 짧아진다.")]
        [SerializeField] private float chainNextChargeDuration = 0.45f;
        [SerializeField] private float chainLungeDistance = 5f;
        [SerializeField] private float chainPause = 0.15f;

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

        [Header("별똥별 (2페이즈부터)")]
        [SerializeField] private BossStarfall starfall;
        [Tooltip("소환 시간 동안 떨어지는 별 개수 - 2페이즈.")]
        [SerializeField] private int starCountPhase2 = 7;
        [Tooltip("소환 시간 동안 떨어지는 별 개수 - 3페이즈.")]
        [SerializeField] private int starCountPhase3 = 11;
        [Tooltip("Roar 시작 후 첫 별을 부르는 시점(초) - 머리를 가장 높이 치켜든 순간.")]
        [SerializeField] private float starfallCastTime = 0.9f;
        [Tooltip("첫 별부터 마지막 별을 부를 때까지의 시간(초). 이 동안 보스는 제자리에서 소환 모션을 유지한다.")]
        [SerializeField] private float starSummonDuration = 4f;

        [Header("회전 뿔 레이저 (3페이즈)")]
        [SerializeField] private BossHornLaser hornLaser;
        [Tooltip("쏘기 전에 아레나 가운데로 뛰어가는 시간.")]
        [SerializeField] private float laserHopDuration = 0.9f;
        [SerializeField] private float laserHopHeight = 2.5f;
        [Tooltip("가운데에 착지할 때 이 반경 안의 플레이어를 피해 없이 밀어낸다 - 보스가 플레이어 위로 떨어져 피할 수 없는 몸통 피해를 주지 않게.")]
        [SerializeField] private float landingPushRadius = 3.5f;
        [Tooltip("밀어내는 힘. 플레이어 넉백이 초당 25씩 줄어들어서, 14면 약 4m 밀려나 보스 몸 밖으로 나간다.")]
        [SerializeField] private float landingPushForce = 14f;
        [SerializeField] private float landingPushUpward = 2f;
        [Tooltip("뿔 끝 발광 - 별똥별과 레이저에서 서로 다른 빛을 낸다.")]
        [SerializeField] private BossHornGlow hornGlow;

        /// <summary>일반 몬스터(MonsterCombat)가 보스와 겹치지 않게 피해 다닐 때 참조하는 보스 위치.</summary>
        public static BossAI Instance { get; private set; }

        /// <summary>현재 페이즈 (1부터).</summary>
        public int CurrentPhase => currentPhase;

        private NavMeshAgent agent;
        private BossAnimationDriver animDriver;
        private Transform player;
        private bool isAttacking;
        private bool isDefeated;
        private bool isActive;
        private int currentPhase = 1;
        private int targetPhase = 1;
        private Pattern? lastPattern;
        private int scheduleStep;   // 0 = 다음은 큰 패턴, 1.. = 작은 패턴 몇 번째
        private int bigIndex;       // 3페이즈에서 레이저/별똥별을 번갈아 고르는 순번
        private float extraRest;    // 큰 패턴 뒤 추가 휴식
        private bool suppressBodyBump; // 중앙 점프 중에는 몸에 닿아도 피해를 주지 않는다

        private void Awake()
        {
            Instance = this;
            agent = GetComponent<NavMeshAgent>();
            agent.isStopped = true; // Boss Stage가 뜨기 전까지는 가만히 있는다.
            if (chargeIndicator == null) chargeIndicator = GetComponent<BossChargeIndicator>();
            if (slamIndicator == null) slamIndicator = GetComponent<BossSlamIndicator>();
            if (starfall == null) starfall = GetComponent<BossStarfall>();
            if (hornLaser == null) hornLaser = GetComponent<BossHornLaser>();
            if (hornGlow == null) hornGlow = GetComponentInChildren<BossHornGlow>(true);
            animDriver = GetComponentInChildren<BossAnimationDriver>(true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            BossWeakpointController.OnBossDefeated += HandleBossDefeated;
            BossWeakpointController.OnHitProgressChanged += HandleHitProgress;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
            BossWeakpointController.OnHitProgressChanged -= HandleHitProgress;
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

        private void HandleHitProgress(int progress)
        {
            int phase = 1;
            foreach (int threshold in phaseHitThresholds) if (progress >= threshold) phase++;
            if (phase > targetPhase) targetPhase = phase;
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

        private float CurrentCooldown => phaseCooldowns != null && phaseCooldowns.Length > 0
            ? phaseCooldowns[Mathf.Clamp(currentPhase - 1, 0, phaseCooldowns.Length - 1)]
            : 5f;

        private IEnumerator AttackLoop()
        {
            while (!isDefeated)
            {
                // 쿨타임 중이라도 페이즈가 바뀌면 바로 전환 연출로 넘어간다.
                float waited = 0f;
                float wait = CurrentCooldown + extraRest;
                while (waited < wait && targetPhase <= currentPhase && !isDefeated)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                extraRest = 0f;
                if (isDefeated) yield break;

                // 포효가 끝나면 쿨타임 없이 바로 새 페이즈의 대표 기믹(별똥별/레이저)으로 이어간다.
                while (targetPhase > currentPhase && !isDefeated)
                    yield return DoPhaseTransition(targetPhase);
                if (isDefeated) yield break;

                // 약점 노출과 공격 패턴은 서로 독립적으로 돌아간다 - 노출 중에 공격이 발동돼도
                // 노출을 닫지 않고 그대로 둔 채(=노출된 채로) 패턴을 실행한다.
                Pattern pattern = PickPattern();
                lastPattern = pattern;
                switch (pattern)
                {
                    case Pattern.Headbutt: yield return DoHeadbutt(); break;
                    case Pattern.ChainCharge: yield return DoChainCharge(); break;
                    case Pattern.GroundSlam: yield return DoGroundSlam(); break;
                    case Pattern.Starfall: yield return DoStarfall(); extraRest = restAfterStarfall; break;
                    case Pattern.HornLaser: yield return DoHornLaser(); extraRest = restAfterLaser; break;
                }
            }
        }

        /// <summary>
        /// 다음 패턴을 고른다.
        ///   1페이즈 : 박치기와 점프 착지를 번갈아.
        ///   2페이즈~: 큰 패턴 하나 → 작은 패턴 smallPatternsBetweenBig개(무작위) → 큰 패턴 … 을 반복한다.
        ///             큰 패턴은 2페이즈는 별똥별, 3페이즈는 회전 레이저와 별똥별을 번갈아.
        /// 큰 패턴의 자리를 고정해서 위험한 패턴이 연달아 나오지 않게 하고, 작은 패턴만 섞어 매번 조금씩 다르게 한다.
        /// 페이즈가 바뀌면 순서를 처음부터 시작해서, 새 페이즈의 대표 기믹(별똥별/레이저)이 제일 먼저 나온다.
        /// </summary>
        private Pattern PickPattern()
        {
            if (currentPhase == 1)
                return lastPattern == Pattern.Headbutt ? Pattern.GroundSlam : Pattern.Headbutt;

            if (scheduleStep == 0)
            {
                scheduleStep = 1;
                Pattern? big = NextBigPattern();
                if (big.HasValue) return big.Value;
            }

            Pattern first = currentPhase >= 3 ? Pattern.ChainCharge : Pattern.Headbutt;
            Pattern small = Random.value < 0.5f ? first : Pattern.GroundSlam;
            // 작은 패턴도 같은 게 두 번 연속 나오지는 않게 한다.
            if (lastPattern == small) small = small == first ? Pattern.GroundSlam : first;

            scheduleStep++;
            if (scheduleStep > smallPatternsBetweenBig) scheduleStep = 0;
            return small;
        }

        private Pattern? NextBigPattern()
        {
            bool canStarfall = starfall != null;
            bool canLaser = hornLaser != null && currentPhase >= 3;
            if (currentPhase >= 3)
            {
                bool laserTurn = bigIndex % 2 == 0;
                bigIndex++;
                if (laserTurn && canLaser) return Pattern.HornLaser;
                if (canStarfall) return Pattern.Starfall;
                if (canLaser) return Pattern.HornLaser;
                return null;
            }
            return canStarfall ? Pattern.Starfall : (Pattern?)null;
        }

        // ==========================================================
        // 페이즈 전환
        // ==========================================================
        private IEnumerator DoPhaseTransition(int newPhase)
        {
            isAttacking = true;
            BeginManualMove();
            FacePlayer();
            currentPhase = newPhase;
            // 새 페이즈는 순서를 처음부터 - 대표 기믹부터 나온다.
            scheduleStep = 0;
            bigIndex = 0;

            animDriver?.PlayRoar();
            GameSfx.Play("PhaseShift", .6f);

            yield return new WaitForSeconds(roarImpactTime);
            // 앞발이 땅을 찍는 순간 - 충격파로 밀어낸다.
            CameraShake.Instance?.Shake();
            PushPlayerAway(transitionShockwaveRadius, transitionKnockback, transitionKnockbackUpward);
            yield return new WaitForSeconds(Mathf.Max(0f, roarDuration - roarImpactTime));

            EndManualMove();
            isAttacking = false;
        }

        // ==========================================================
        // 몸통 박치기 / 3연속 돌진
        // ==========================================================
        private IEnumerator DoHeadbutt()
        {
            GameSfx.At("Warning", transform.position, .55f);
            isAttacking = true;
            BeginManualMove();

            Vector3 start = transform.position;
            Vector3 dir = AimAtPlayer();
            Vector3 target = start + dir * headbuttLungeDistance;

            // 차지 - 방향을 이 시점에 고정하고, 실제 판정 영역(몸 반경 원이 돌진 경로를 쓸고 가는 모양)을
            // 바닥에 그린다. 이 동안은 피해가 없어서 플레이어가 경로 밖으로 비켜설 수 있다.
            yield return ChargeTelegraph(start, dir, headbuttLungeDistance, headbuttChargeDuration);

            GameSfx.At("Charge", transform.position, .6f);
            animDriver?.PlayHeadbutt();
            yield return Lunge(start, target, headbuttLungeDuration, true);
            if (chargeIndicator != null) chargeIndicator.Hide();

            // 제자리로 물러난다.
            yield return Lunge(target, start, headbuttLungeDuration, false);
            transform.position = start;

            EndManualMove();
            isAttacking = false;
        }

        private IEnumerator DoChainCharge()
        {
            GameSfx.At("Warning", transform.position, .6f);
            isAttacking = true;
            BeginManualMove();

            for (int i = 0; i < chainChargeCount && !isDefeated; i++)
            {
                Vector3 start = transform.position;
                Vector3 dir = AimAtPlayer();
                Vector3 target = ClampToNavMesh(start, start + dir * chainLungeDistance);
                float distance = Vector3.Distance(start, target);

                yield return ChargeTelegraph(start, dir, distance, i == 0 ? chainFirstChargeDuration : chainNextChargeDuration);
                GameSfx.At("Charge", transform.position, .6f);
                animDriver?.PlayHeadbutt();
                yield return Lunge(start, target, headbuttLungeDuration, true);
                if (chargeIndicator != null) chargeIndicator.Hide();
                transform.position = target;
                if (chainPause > 0f) yield return new WaitForSeconds(chainPause);
            }

            EndManualMove();
            isAttacking = false;
        }

        private IEnumerator ChargeTelegraph(Vector3 start, Vector3 dir, float distance, float duration)
        {
            if (chargeIndicator != null) chargeIndicator.Show(start, dir, distance, bodyContactRadius);
            float charge = 0f;
            while (charge < duration)
            {
                charge += Time.deltaTime;
                if (chargeIndicator != null) chargeIndicator.SetFill(charge / Mathf.Max(duration, 0.0001f));
                yield return null;
            }
        }

        private IEnumerator Lunge(Vector3 from, Vector3 to, float duration, bool dealsDamage)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, elapsed / duration);
                // 돌진 중 매 프레임 범위를 다시 확인한다 - 범위 안에 있을 때만 맞고, 벗어나면 회피된다.
                if (dealsDamage) TryDamagePlayerInBodyRange();
                yield return null;
            }
        }

        private Vector3 AimAtPlayer()
        {
            Vector3 dir = player != null ? (player.position - transform.position) : transform.forward;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            transform.forward = dir;
            return dir;
        }

        /// <summary>연속 돌진은 제자리로 돌아오지 않아서, 아레나 밖으로 튀어 나가지 않게 NavMesh 안으로 자른다.</summary>
        private static Vector3 ClampToNavMesh(Vector3 from, Vector3 to)
        {
            if (NavMesh.Raycast(from, to, out NavMeshHit hit, NavMesh.AllAreas))
                return hit.position - (to - from).normalized * 0.3f;
            return to;
        }

        // ==========================================================
        // 점프 착지
        // ==========================================================
        private IEnumerator DoGroundSlam()
        {
            GameSfx.Play("Warning", .55f);
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
            animDriver?.PlayGroundSlam();
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
            GameSfx.At("Slam", groundPos, .65f);

            if (slamIndicator != null) slamIndicator.Hide();
            CameraShake.Instance?.Shake();

            EndManualMove();
            isAttacking = false;
        }

        // ==========================================================
        // 별똥별 낙하 (2페이즈부터)
        // ==========================================================
        private IEnumerator DoStarfall()
        {
            isAttacking = true;
            BeginManualMove();
            FacePlayer();

            // 포효하며 뿔이 금빛으로 반짝이고, 머리를 가장 높이 든 순간부터 별을 부르기 시작한다.
            animDriver?.PlayRoar();
            if (hornGlow != null) hornGlow.SetMode(BossHornGlow.Mode.Starfall);
            yield return new WaitForSeconds(starfallCastTime);

            // 포효가 끝나 갈 즈음 소환 반복 모션으로 넘어가, 마지막 별을 부를 때까지 제자리에서 버틴다.
            // 보스가 멈춰 서 있는 몇 초는 약점이 열려 있다면 노려 쏘기 좋은 틈이기도 하다.
            float roarLeft = Mathf.Max(0f, roarDuration - starfallCastTime - 0.2f);
            int count = currentPhase >= 3 ? starCountPhase3 : starCountPhase2;
            Coroutine summon = starfall != null ? StartCoroutine(starfall.Channel(starSummonDuration, count, player)) : null;
            yield return new WaitForSeconds(roarLeft);
            animDriver?.SetStarSummoning(true);
            if (summon != null) yield return summon;
            else yield return new WaitForSeconds(Mathf.Max(0f, starSummonDuration - roarLeft));

            animDriver?.SetStarSummoning(false);
            if (hornGlow != null) hornGlow.SetMode(BossHornGlow.Mode.Off);

            // 마지막 별들이 떨어지는 동안에는 다시 쫓아온다.
            EndManualMove();
            isAttacking = false;
        }

        // ==========================================================
        // 회전 뿔 레이저 (3페이즈)
        // ==========================================================
        private IEnumerator DoHornLaser()
        {
            isAttacking = true;
            BeginManualMove();

            // 아레나 가운데로 뛰어가 선다 - 빔이 사방으로 고르게 뻗고, 플레이어가 돌 공간이 어디서나 같게.
            if (TryGetArenaCenter(out Vector3 center) && Vector3.Distance(transform.position, center) > 1.5f)
                yield return HopTo(center, laserHopDuration, laserHopHeight);

            yield return hornLaser.Run(transform, player, animDriver);
            EndManualMove();
            isAttacking = false;
        }

        private IEnumerator HopTo(Vector3 target, float duration, float height)
        {
            Vector3 from = transform.position;
            Vector3 dir = target - from; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.forward = dir.normalized;
            GameSfx.At("Jump", from, .5f);
            animDriver?.PlayGroundSlam(); // 뛰어오르는 모션을 같이 쓴다
            // 이동용 점프라 공격이 아니다 - 플레이어 위로 떨어져도 몸통 피해를 주지 않는다.
            suppressBodyBump = true;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(duration, 0.0001f));
                Vector3 pos = Vector3.Lerp(from, target, p);
                pos.y += Mathf.Sin(p * Mathf.PI) * height;
                transform.position = pos;
                yield return null;
            }
            transform.position = target;
            GameSfx.At("Land", target, .6f);
            CameraShake.Instance?.Shake();
            // 착지 자리에 있던 플레이어는 피해 없이 바깥으로 밀어내고, 밀려나는 동안에는 부딪혀도 피해가 없게 잠깐 더 막아 둔다.
            PushPlayerAway(landingPushRadius, landingPushForce, landingPushUpward);
            yield return new WaitForSeconds(0.25f);
            suppressBodyBump = false;
        }

        /// <summary>아레나 바닥 가운데 - NavMesh 위이면서 바닥 표면 높이에 맞춘 점.</summary>
        private bool TryGetArenaCenter(out Vector3 center)
        {
            center = transform.position;
            if (arenaFloorCollider == null) return false;
            Vector3 c = arenaFloorCollider.bounds.center;
            if (NavMesh.SamplePosition(c, out NavMeshHit hit, 4f, NavMesh.AllAreas)) c = hit.position;
            Bounds b = arenaFloorCollider.bounds;
            if (arenaFloorCollider.Raycast(new Ray(new Vector3(c.x, b.max.y + 5f, c.z), Vector3.down), out RaycastHit ground, b.size.y + 10f))
                c.y = ground.point.y;
            center = c;
            return true;
        }

        // ==========================================================
        // 공통
        // ==========================================================
        private void FacePlayer()
        {
            if (player == null) return;
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.forward = dir.normalized;
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

        /// <summary>공격 패턴과 무관하게 보스 몸에 그냥 부딪히기만 해도 하트 반개를 주고 밀어낸다.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (isDefeated || suppressBodyBump) return;
            CharacterController playerController = other.GetComponentInParent<CharacterController>();
            if (playerController == null) return;

            ApplyDamage(bodyBumpDamage);
            ApplyKnockback(playerController);
        }

        private void ApplyDamage(float amount)
        {
            if (PlayerHealth.Instance == null || PlayerHealth.Instance.IsInvincible) return;
            PlayerHealth.Instance.TakeDamage(amount, grantInvincibility: true);
            CameraShake.Instance?.Shake();
        }

        /// <summary>보스 위치 기준 바깥쪽(수평)으로 플레이어를 밀어낸다.</summary>
        private void ApplyKnockback(CharacterController playerController)
        {
            SimpleFirstPersonController fpc = playerController.GetComponent<SimpleFirstPersonController>();
            if (fpc == null) return;

            Vector3 away = playerController.transform.position - transform.position;
            away.y = 0f;
            // 완전히 겹친 순간(방향을 못 구함)에는 보스가 보는 반대 방향으로 밀어낸다.
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;

            fpc.ApplyKnockback(away * bodyBumpKnockback + Vector3.up * bodyBumpKnockbackUpward);
        }

        /// <summary>페이즈 전환 충격파 - 반경 안의 플레이어를 피해 없이 바깥으로 밀어낸다.</summary>
        private void PushPlayerAway(float radius, float force, float upward)
        {
            if (player == null) return;
            Vector3 away = player.position - transform.position;
            away.y = 0f;
            if (away.magnitude > radius) return;
            var fpc = player.GetComponent<SimpleFirstPersonController>();
            if (fpc == null) return;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;
            fpc.ApplyKnockback(away * force + Vector3.up * upward);
        }

        private void HandleBossDefeated()
        {
            isDefeated = true;
            suppressBodyBump = false;
            StopAllCoroutines();
            agent.isStopped = true;
            // 공격 도중에 처치되면 코루틴이 끊겨 표시·빔이 남으므로 여기서 직접 정리한다.
            if (chargeIndicator != null) chargeIndicator.Hide();
            if (slamIndicator != null) slamIndicator.Hide();
            if (starfall != null) starfall.Cancel();
            if (hornLaser != null) hornLaser.Cancel();
            if (hornGlow != null) hornGlow.SetMode(BossHornGlow.Mode.Off);
            animDriver?.SetHornCharging(false);
            animDriver?.SetStarSummoning(false);
        }
    }
}
