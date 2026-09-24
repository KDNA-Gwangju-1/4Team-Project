using UnityEngine;
using UnityEngine.AI;

namespace BrightDream.Combat
{
    /// <summary>
    /// BossAI/BossWeakpointController/BossRiftEntrance를 수정하지 않고 보스 상태를 읽어 Animator를 구동한다.
    /// - 추적 이동(NavMeshAgent 이동)일 때만 걷기.
    /// - BossAI는 공격 중 agent.updatePosition을 끄고 Transform을 직접 옮기므로, 그 첫 움직임이
    ///   수평이면 박치기, 수직이면 점프 찍기로 판단해 트리거를 1회 보낸다.
    /// - 약점 노출 중에는 머리/목/꼬리만 덮는 비틀거림 레이어를 켠다 (공격 중에는 공격 모션 우선).
    /// - 약점 명중(HitProgress 증가) 때 상체만 덮는 피격 레이어로 움찔한다.
    /// - 균열 등장 연출 중 보스가 보이기 시작하면 등장(날아와 착지 후 포효) 모션을 재생한다.
    /// - 보스 처치 즉시 쓰러짐 모션으로 눕고, BossRiftExit의 손이 실제로 붙잡는 순간부터 버둥거림 루프로 전환한다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BossAnimationDriver : MonoBehaviour
    {
        [SerializeField] private string walkingParameter = "IsWalking";
        [SerializeField] private string headbuttTrigger = "Headbutt";
        [SerializeField] private string groundSlamTrigger = "GroundSlam";
        [SerializeField] private string emergeTrigger = "Emerge";
        [SerializeField] private string defeatedParameter = "Defeated";
        [SerializeField] private string grabbedParameter = "Grabbed";
        [SerializeField] private string staggerLayerName = "Stagger";
        [SerializeField] private string hitLayerName = "Hit";
        [SerializeField] private string hitStateName = "Hit";
        [Tooltip("이 속도(m/s)를 넘으면 걷기 시작.")]
        [SerializeField] private float startWalkSpeed = 0.25f;
        [Tooltip("이 속도(m/s) 아래로 떨어지면 멈춤 - 시작값보다 낮게 둬서 경계에서 깜빡이지 않게 한다.")]
        [SerializeField] private float stopWalkSpeed = 0.1f;
        [Tooltip("비틀거림/피격 레이어가 켜지고 꺼지는 데 걸리는 시간(초).")]
        [SerializeField] private float layerFadeTime = 0.15f;
        [Tooltip("피격 움찔 클립 길이(초) - 이 시간 동안 피격 레이어를 켜 둔다.")]
        [SerializeField] private float hitDuration = 0.4f;
        [Tooltip("처치 후 손이 붙잡기까지의 시간(초). 씬에 BossRiftExit가 있으면 그 Pre Delay + 균열 벌어짐 + Reach Duration으로 덮어쓰므로, 이 값은 못 찾았을 때만 쓰인다.")]
        [SerializeField] private float grabDelayFallback = 4.1f;
        [Tooltip("BossRiftExit가 Pre Delay 뒤, 손을 뻗기 전에 균열을 벌리는 시간 - 그 스크립트 안에 0.5초로 고정되어 있다.")]
        [SerializeField] private float riftWidenDuration = 0.5f;

        private Animator animator;
        private NavMeshAgent agent;
        private BossWeakpointController weakpoint;
        private BossRiftEntrance entrance;
        private BossRiftExit exitSequence;
        private Renderer bodyRenderer;
        private int walkingHash, headbuttHash, slamHash, emergeHash, defeatedHash, grabbedHash, hitStateHash;
        private float grabDelay;
        private bool grabbed;
        private int staggerLayer, hitLayer;
        private bool isWalking;
        private bool attackTriggered;
        private bool wasVisible;
        private bool isDefeated;
        private float defeatedAtRealtime;
        private float staggerWeight, hitWeight, hitTimer;
        private int lastHitProgress;
        private Vector3 lastBossPosition;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponentInParent<NavMeshAgent>();
            weakpoint = GetComponentInParent<BossWeakpointController>();
            entrance = FindObjectOfType<BossRiftEntrance>();
            exitSequence = FindObjectOfType<BossRiftExit>();
            bodyRenderer = GetComponentInChildren<Renderer>(true);
            walkingHash = Animator.StringToHash(walkingParameter);
            headbuttHash = Animator.StringToHash(headbuttTrigger);
            slamHash = Animator.StringToHash(groundSlamTrigger);
            emergeHash = Animator.StringToHash(emergeTrigger);
            defeatedHash = Animator.StringToHash(defeatedParameter);
            grabbedHash = Animator.StringToHash(grabbedParameter);
            grabDelay = ReadGrabDelay();
            hitStateHash = Animator.StringToHash(hitStateName);
            staggerLayer = animator.GetLayerIndex(staggerLayerName);
            hitLayer = animator.GetLayerIndex(hitLayerName);
            if (agent != null) lastBossPosition = agent.transform.position;
            if (weakpoint != null) lastHitProgress = weakpoint.HitProgress;
            wasVisible = bodyRenderer != null && bodyRenderer.enabled;
        }

        private void OnEnable() => BossWeakpointController.OnBossDefeated += HandleBossDefeated;
        private void OnDisable() => BossWeakpointController.OnBossDefeated -= HandleBossDefeated;

        private void HandleBossDefeated()
        {
            isDefeated = true;
            defeatedAtRealtime = Time.realtimeSinceStartup;
            animator.SetBool(defeatedHash, true);
            // BossAI는 처치 시 isStopped만 세워서, 남은 관성으로 쓰러지는 동안 2m 넘게 미끄러진다. 제자리에 쓰러지게 한다.
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.velocity = Vector3.zero;
        }

        /// <summary>
        /// 손이 보스를 붙잡는 시점 = BossRiftExit의 preDelay + 균열 벌어짐 + reachDuration. BossRiftExit를 고치지 않고
        /// 그 직렬화 값을 읽어서, 나중에 연출 타이밍을 조정해도 버둥거림 시작이 어긋나지 않게 한다.
        /// </summary>
        private float ReadGrabDelay()
        {
            BossRiftExit exit = FindObjectOfType<BossRiftExit>();
            if (exit == null) return grabDelayFallback;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var pre = typeof(BossRiftExit).GetField("preDelay", flags);
            var reach = typeof(BossRiftExit).GetField("reachDuration", flags);
            if (pre == null || reach == null) return grabDelayFallback;
            return (float)pre.GetValue(exit) + riftWidenDuration + (float)reach.GetValue(exit);
        }

        private void Update()
        {
            UpdateEmerge();

            if (isDefeated)
            {
                SetWalking(false);
                // BossRiftExit는 실제 시간(WaitForSecondsRealtime)으로 진행하므로 같은 시간축으로 잰다.
                if (!grabbed && (exitSequence != null ? exitSequence.HasGrabbedBoss : Time.realtimeSinceStartup - defeatedAtRealtime >= grabDelay))
                {
                    grabbed = true;
                    animator.SetBool(grabbedHash, true);
                }
            }
            else
            {
                bool manualMove = agent != null && agent.enabled && !agent.updatePosition;
                UpdateAttack(manualMove);
                SetWalking(!manualMove && IsChasing());
                bool exposed = weakpoint != null && weakpoint.IsExposed && !manualMove && agent != null && agent.enabled;
                staggerWeight = FadeLayer(staggerLayer, staggerWeight, exposed);
            }
            if (isDefeated) staggerWeight = FadeLayer(staggerLayer, staggerWeight, false);

            UpdateHit();
        }

        private void UpdateEmerge()
        {
            bool visible = bodyRenderer != null && bodyRenderer.enabled;
            if (visible && !wasVisible && entrance != null && entrance.IsPlaying) animator.SetTrigger(emergeHash);
            wasVisible = visible;
        }

        private void UpdateAttack(bool manualMove)
        {
            if (agent == null) return;
            Vector3 position = agent.transform.position;
            Vector3 delta = position - lastBossPosition;
            lastBossPosition = position;

            if (!manualMove)
            {
                attackTriggered = false;
                return;
            }
            if (attackTriggered) return;

            float horizontal = new Vector2(delta.x, delta.z).magnitude;
            float vertical = Mathf.Abs(delta.y);
            if (horizontal < 0.001f && vertical < 0.001f) return; // 아직 이번 공격의 첫 이동이 안 나왔다.

            animator.SetTrigger(vertical > horizontal ? slamHash : headbuttHash);
            attackTriggered = true;
        }

        private void UpdateHit()
        {
            if (hitLayer < 0 || weakpoint == null) return;
            int progress = weakpoint.HitProgress;
            if (progress > lastHitProgress)
            {
                animator.Play(hitStateHash, hitLayer, 0f);
                hitTimer = hitDuration;
                hitWeight = 1f; // 움찔 정점이 0.07초라 페이드인 없이 바로 켠다.
            }
            lastHitProgress = progress;
            hitTimer -= Time.deltaTime;
            hitWeight = FadeLayer(hitLayer, hitWeight, hitTimer > 0f);
        }

        private bool IsChasing()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || agent.isStopped) return false;
            Vector3 v = agent.velocity;
            v.y = 0f;
            float speed = v.magnitude;
            return isWalking ? speed > stopWalkSpeed : speed > startWalkSpeed;
        }

        private void SetWalking(bool walking)
        {
            if (walking == isWalking) return;
            isWalking = walking;
            animator.SetBool(walkingHash, walking);
        }

        private float FadeLayer(int layer, float weight, bool on)
        {
            if (layer < 0) return weight;
            float step = layerFadeTime > 0f ? Time.deltaTime / layerFadeTime : 1f;
            weight = Mathf.MoveTowards(weight, on ? 1f : 0f, step);
            animator.SetLayerWeight(layer, weight);
            return weight;
        }
    }
}
