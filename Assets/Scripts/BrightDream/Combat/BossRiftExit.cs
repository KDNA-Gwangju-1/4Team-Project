using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스를 정화하면 균열에서 어두운 꿈 보스의 손이 뻗어 나와 유니콘을 붙잡고
    /// 균열 안으로 끌고 들어간다.
    ///
    /// 진행 순서
    ///   1. 균열 확대   이미 열려 있는 균열이 조금 더 벌어지고 밝아진다
    ///   2. 손 등장     균열에서 팔이 뻗어 나온다. 손아귀가 유니콘을 향한다
    ///   3. 붙잡기      손이 유니콘에 닿는 순간 카메라 흔들림
    ///   4. 끌고 감     손과 유니콘이 함께 균열로 빨려 들어가며 작아진다
    ///   5. 잦아듦     손이 사라지고 균열이 잦아든다. 완전히 닫지 않고 찢어진 흔적을
    ///                 남겨 둔다 - 여기가 다음 챕터로 넘어가는 포탈이 된다
    ///
    /// BossWeakpointController.OnBossDefeated 를 구독해 시작한다. 같은 이벤트를
    /// BossArenaLockdown 도 듣고 있어서 벽은 그쪽이 알아서 연다.
    ///
    /// 시간은 전부 unscaled 로 돈다. 대사나 단서 조사로 Time.timeScale 이 0 이 되면
    /// WaitForSeconds 는 그 자리에서 멈춰 연출이 끝나지 않는다. BossRiftEntrance 와 같은 규칙.
    /// </summary>
    public class BossRiftExit : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("BossClear_Rift3D 루트.")]
        [SerializeField] private Transform rift;
        [Tooltip("끌려 들어갈 유니콘 보스.")]
        [SerializeField] private Transform boss;
        [Tooltip("BossHand 프리팹. 비우면 손 없이 보스만 빨려 들어간다.")]
        [SerializeField] private GameObject handPrefab;
        [Tooltip("유니콘 몸통 가운데 뼈(Spine). 손바닥의 GripAnchor가 이 지점을 붙잡고, 애니메이션 후에도 접촉을 유지한다.")]
        [SerializeField] private Transform grabPoint;

        [Header("타이밍")]
        [Tooltip("정화 완료 후 연출이 시작되기까지의 뜸.")]
        [SerializeField] private float preDelay = 1.0f;
        [Tooltip("손이 균열에서 유니콘까지 뻗는 시간.")]
        [SerializeField] private float reachDuration = 1.1f;
        [Tooltip("붙잡은 뒤 끌기 시작까지의 뜸.")]
        [SerializeField] private float grabHold = 0.45f;
        [Tooltip("손가락 관절이 몸통을 감싸 쥐는 시간.")]
        [SerializeField] private float fingerCloseDuration = 0.85f;
        [Tooltip("유니콘이 균열로 끌려 들어가는 시간.")]
        [SerializeField] private float dragDuration = 1.3f;
        [Tooltip("균열이 잦아드는 시간.")]
        [SerializeField] private float closeDuration = 1.2f;

        [Header("연출")]
        [Tooltip("GripAnchor가 없는 구형 프리팹에서만 사용하는 손목 앞 접촉 거리.")]
        [SerializeField] private float grabOffset = 1.2f;
        [Tooltip("끌려갈 때 흔들리는 폭.")]
        [SerializeField] private float struggleAmount = 0.35f;
        [Tooltip("끌려 들어가며 줄어드는 최종 크기 배율.")]
        [SerializeField] private float shrinkTo = 0.12f;
        [SerializeField] private float grabShakeDuration = 0.5f;
        [SerializeField] private float grabShakeMagnitude = 0.4f;

        [Header("남는 흔적")]
        [Tooltip("연출이 끝난 뒤 남길 균열의 양. 0 이면 완전히 닫히고, 1 이면 그대로 남는다.")]
        [Range(0f, 1f)]
        [SerializeField] private float residualProgress = 0.55f;
        [Tooltip("남은 균열의 밝기 배율. 원래 밝기를 1 로 본 값이다.")]
        [Range(0f, 2f)]
        [SerializeField] private float residualIntensity = 0.45f;

        [Header("메시지")]
        [SerializeField] private string message = "";
        [SerializeField] private float messageDuration = 3f;

        /// <summary>유니콘이 완전히 끌려 들어가고 균열이 닫힌 순간 발생.</summary>
        public static event Action OnRiftExitFinished;

        public bool IsPlaying { get; private set; }
        public bool HasGrabbedBoss { get; private set; }

        private Renderer[] riftRenderers;
        private float[] riftBaseIntensity;
        private MaterialPropertyBlock mpb;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private GameObject hand;
        private bool done;
        private Transform contactAnchor;
        private Transform activeArm, activeGrip;
        private Vector3 activeArmBase, activeGripBase;
        private float reachBlend, trackedStretch;
        private bool trackingReach;
        private BossHandGrip fingerRig;

        // Animator evaluates the Spine after the coroutine. Correct contact afterwards,
        // so defeat/struggle animation cannot slide the body out of the palm.
        private void LateUpdate()
        {
            if (trackingReach) TrackReach();
            if (HasGrabbedBoss && boss != null && grabPoint != null && contactAnchor != null)
                boss.position += contactAnchor.position - grabPoint.position;
        }

        private void TrackReach()
        {
            if (hand == null || activeGrip == null || contactAnchor == null) return;
            Vector3 target = BossAimPoint();
            Vector3 direction = target - hand.transform.position;
            if (direction.sqrMagnitude < 0.0001f) return;
            // The anchor sits below the palm: aim the wrist above the torso, keeping
            // the dorsum upward. Compensate that offset instead of snapping the boss.
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = hand.transform.InverseTransformPoint(contactAnchor.position);
                offset.z = 0f;
                Vector3 aim = direction - hand.transform.TransformVector(offset);
                hand.transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
            }
            SetHandExtension(activeArm, activeArmBase, activeGrip, activeGripBase, 0f);
            float start = Vector3.Dot(contactAnchor.position - hand.transform.position, hand.transform.forward);
            SetHandExtension(activeArm, activeArmBase, activeGrip, activeGripBase, 1f);
            float end = Vector3.Dot(contactAnchor.position - hand.transform.position, hand.transform.forward);
            trackedStretch = Mathf.Abs(end - start) > 0.0001f
                ? Mathf.Max(0f, (Vector3.Dot(direction, hand.transform.forward) - start) / (end - start)) : 1f;
            SetHandExtension(activeArm, activeArmBase, activeGrip, activeGripBase, trackedStretch * reachBlend);
        }

        private void Awake()
        {
            if (rift != null)
            {
                riftRenderers = rift.GetComponentsInChildren<Renderer>(true);
                mpb = new MaterialPropertyBlock();

                // 균열은 파트마다 머티리얼이 달라 _Intensity 기본값도 다르다(1.9 / 2.0 …).
                // 절대값을 넣으면 연출이 시작되는 순간 밝기가 튄다. 원래 값을 재 두고 배율로 쓴다.
                riftBaseIntensity = new float[riftRenderers.Length];
                for (int i = 0; i < riftRenderers.Length; i++)
                {
                    var r = riftRenderers[i];
                    var m = r != null ? r.sharedMaterial : null;
                    riftBaseIntensity[i] = (m != null && m.HasProperty(IntensityId)) ? m.GetFloat(IntensityId) : 0f;
                }
            }
        }

        private void OnEnable()
        {
            BossWeakpointController.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
            trackingReach = false;
            HasGrabbedBoss = false;
            if (hand != null) Destroy(hand);
        }

        private void HandleBossDefeated()
        {
            if (done || IsPlaying) return;
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            IsPlaying = true;

            if (!string.IsNullOrEmpty(message))
            {
                var ui = StageMessageUI.Instance;
                if (ui != null) ui.ShowMessage(message, messageDuration);
            }

            if (preDelay > 0f) yield return new WaitForSecondsRealtime(preDelay);

            // 보스를 물리/네비게이션에서 떼어낸다. NavMeshAgent 가 살아 있으면 매 프레임
            // 위치를 NavMesh 위로 되돌려 버려서 공중의 균열로 끌려가지 않는다.
            // BossAI.HandleBossDefeated 는 agent.isStopped 만 세우고 컴포넌트는 켜 둔다.
            ReleaseBossFromWorld();

            Vector3 riftPos = rift != null ? rift.position : transform.position;
            Vector3 bossPos = boss != null ? boss.position : riftPos;
            Vector3 bossScale = boss != null ? boss.localScale : Vector3.one;

            // 조준은 트랜스폼 원점이 아니라 렌더러 중심으로 한다. 유니콘의 원점은 발밑이라
            // 그대로 겨누면 손이 바닥을 긁으며 다리만 감싼다.
            Vector3 bossAim = BossAimPoint();
            Vector3 toBoss = (bossAim - riftPos);
            float dist = toBoss.magnitude;
            Vector3 dir = dist > 0.001f ? toBoss / dist : Vector3.forward;

            // ── 1) 균열이 조금 더 벌어진다 ──
            // 먼저 균열을 열린 기본 상태로 맞춘다. 등장 연출이 MaterialPropertyBlock 으로
            // 건드려 둔 값이 있어도 여기서부터는 이 연출이 기준이 된다.
            SetRiftFloat(ProgressId, 1f);
            SetRiftIntensityScale(1f);

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / 0.5f);
                SetRiftIntensityScale(Mathf.Lerp(1f, 1.7f, p));
                yield return null;
            }

            // ── 2) 손이 뻗어 나온다 ──
            // 팔 뿌리는 균열에 붙박아 두고 축(+Z) 방향으로만 늘린다. 위치를 옮기면
            // 팔이 균열에서 떨어져 허공을 날아가는 것처럼 보인다.
            float reachDist = Mathf.Max(dist - grabOffset, 0.1f);
            float stretch = 1f;        // 팔뚝을 몇 배로 늘려야 유니콘에 닿는지
            Transform arm = null;      // 늘어나는 팔뚝
            Transform grip = null;     // 손. 늘리지 않고 팔 끝을 따라간다
            Vector3 gripBase = Vector3.zero;
            Vector3 armBase = Vector3.one;

            if (handPrefab != null)
            {
                hand = Instantiate(handPrefab, riftPos, Quaternion.LookRotation(dir, Vector3.up));
                hand.name = "BossHand_Runtime";
                fingerRig = hand.GetComponent<BossHandGrip>();
                if (fingerRig != null) fingerRig.SetGrip(0f);

                foreach (var tr in hand.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == "Arm") arm = tr;
                    else if (tr.name == "Hand") grip = tr;
                    else if (tr.name == "GripAnchor") contactAnchor = tr;
                }
                if (grip != null) gripBase = grip.localPosition;

                if (arm != null)
                {
                    // 임포터가 넣어 준 1/100 보정 스케일(100)을 기준값으로 보존한다.
                    // Vector3.one 으로 덮어쓰면 팔이 1/100 크기로 쪼그라든다.
                    armBase = arm.localScale;
                    float armLen = MeasureArmLength(hand.transform, arm);
                    if (armLen > 0.001f) stretch = reachDist / armLen;
                }

                // 붙잡을 지점이 있으면 손목이 정확히 그 앞 grabOffset 에 멈추도록 늘림 배수를 맞춘다.
                // 손목 위치는 늘림 배수에 정비례하므로 0배/1배 두 번만 재면 필요한 배수가 나온다.
                if (grabPoint != null && grip != null)
                {
                    SetHandExtension(arm, armBase, grip, gripBase, 0f);
                    float d0 = Vector3.Dot(grip.position - riftPos, dir);
                    SetHandExtension(arm, armBase, grip, gripBase, 1f);
                    float d1 = Vector3.Dot(grip.position - riftPos, dir);
                    if (Mathf.Abs(d1 - d0) > 0.0001f) stretch = (reachDist - d0) / (d1 - d0);
                }
                SetHandExtension(arm, armBase, grip, gripBase, 0f);
                if (grip != null)
                {
                    // Old prefabs still receive an explicit forward contact point.
                    if (contactAnchor == null)
                    {
                        contactAnchor = new GameObject("GripAnchor").transform;
                        contactAnchor.position = grip.position + hand.transform.forward * grabOffset;
                        contactAnchor.SetParent(grip, true);
                    }
                    activeArm = arm;
                    activeGrip = grip;
                    activeArmBase = armBase;
                    activeGripBase = gripBase;
                    reachBlend = 0f;
                    trackingReach = true;
                }
            }

            t = 0f;
            while (t < reachDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(reachDuration, 0.0001f));
                float e = 1f - (1f - p) * (1f - p) * (1f - p);      // ease out cubic
                SetHandExtension(arm, armBase, grip, gripBase, stretch * e);
                reachBlend = e;
                if (fingerRig != null) fingerRig.SetGrip(e * .12f);
                if (trackingReach) TrackReach();
                yield return null;
            }

            if (trackingReach)
            {
                reachBlend = 1f;
                TrackReach();
                stretch = trackedStretch;
            }
            // Close MCP, PIP and DIP joints around the torso before applying attachment.
            t = 0f;
            while (t < fingerCloseDuration)
            {
                t += Time.unscaledDeltaTime;
                float close = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(.001f, fingerCloseDuration));
                if (fingerRig != null) fingerRig.SetGrip(Mathf.Lerp(.12f, 1f, close));
                if (trackingReach) TrackReach();
                yield return null;
            }
            if (fingerRig != null) fingerRig.SetGrip(1f);
            stretch = trackedStretch;
            trackingReach = false;
            HasGrabbedBoss = grabPoint != null && contactAnchor != null && boss != null;

            // ── 3) 붙잡는 순간 ──
            var shake = CameraShake.Instance;
            if (shake != null) shake.Shake(grabShakeDuration, grabShakeMagnitude);
            if (grabHold > 0f) yield return new WaitForSecondsRealtime(grabHold);

            // ── 4) 끌고 들어간다 ──
            // 팔은 다시 줄어들고 유니콘은 그 끝에 매달려 균열로 딸려 온다.
            // 손바닥 GripAnchor에 몸통을 고정하고,
            // 흔들림도 유니콘만이 아니라 팔 전체를 좌우로 휘둘러 준다 - 따로 움직이면 손이 놓친 것처럼 보인다.
            bool holdInHand = grabPoint != null && grip != null && boss != null && hand != null;
            Vector3 gripScale = grip != null ? grip.localScale : Vector3.one;
            Quaternion handRotation = hand != null ? hand.transform.rotation : Quaternion.identity;

            t = 0f;
            while (t < dragDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(dragDuration, 0.0001f));
                float e = p * p;                                     // ease in - 점점 빨라진다

                // 저항하며 흔들린다
                float wob = Mathf.Sin(p * Mathf.PI * 7f) * struggleAmount * (1f - p);
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                if (holdInHand)
                {
                    float swayDeg = Mathf.Rad2Deg * wob / Mathf.Max(reachDist, 0.5f);
                    hand.transform.rotation = Quaternion.AngleAxis(swayDeg, Vector3.up) * handRotation;
                    SetHandExtension(arm, armBase, grip, gripBase, stretch * (1f - e));

                    boss.localScale = Vector3.Lerp(bossScale, bossScale * shrinkTo, e);
                    float shrink = bossScale.x > 0.0001f ? boss.localScale.x / bossScale.x : 1f;
                    grip.localScale = gripScale * shrink;
                    if (arm != null)
                        arm.localScale = new Vector3(armBase.x * shrink, armBase.y * shrink, arm.localScale.z);
                    if (contactAnchor != null) boss.position += contactAnchor.position - grabPoint.position;
                }
                else
                {
                    if (boss != null)
                    {
                        boss.position = Vector3.Lerp(bossPos, riftPos, e) + side * wob;
                        boss.localScale = Vector3.Lerp(bossScale, bossScale * shrinkTo, e);
                    }
                    SetHandExtension(arm, armBase, grip, gripBase, stretch * (1f - e));
                }
                yield return null;
            }

            if (boss != null) boss.gameObject.SetActive(false);
            HasGrabbedBoss = false;
            if (hand != null) Destroy(hand);

            // ── 5) 균열이 잦아든다 ──
            // 완전히 닫지 않는다. 찢어진 자국이 남아 다음 챕터로 넘어가는 포탈이 된다.
            t = 0f;
            while (t < closeDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(closeDuration, 0.0001f));
                float e = p * p * (3f - 2f * p);
                SetRiftFloat(ProgressId, Mathf.Lerp(1f, residualProgress, e));
                SetRiftIntensityScale(Mathf.Lerp(1.7f, residualIntensity, e));
                yield return null;
            }
            SetRiftFloat(ProgressId, residualProgress);
            SetRiftIntensityScale(residualIntensity);

            IsPlaying = false;
            done = true;
            if (OnRiftExitFinished != null) OnRiftExitFinished();
        }

        /// <summary>
        /// 팔뚝을 k 배로 늘리고, 손을 그 끝에 옮겨 붙인다.
        /// 손은 스케일을 건드리지 않아 아무리 늘려도 비율이 유지된다.
        /// </summary>
        private static void SetHandExtension(Transform arm, Vector3 armBase, Transform grip, Vector3 gripBase, float k)
        {
            if (arm != null) arm.localScale = new Vector3(armBase.x, armBase.y, armBase.z * k);
            if (grip != null) grip.localPosition = gripBase * k;
        }

        /// <summary>
        /// 팔뚝이 루트의 정면(+Z)으로 몇 미터 뻗어 있는지 월드 기준으로 잰다.
        /// FBX 가 1/100 로 들어오고 임포터가 축 보정 회전까지 넣어 주므로, 프리팹 구조가
        /// 바뀌어도 값을 하드코딩하지 않도록 실제 메시에서 측정한다.
        /// arm.localScale 이 기준값(늘리기 전)인 상태에서 호출할 것.
        /// </summary>
        private static float MeasureArmLength(Transform root, Transform arm)
        {
            float far = 0f;
            foreach (var mf in arm.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                // 메시 로컬 AABB 의 8 꼭짓점을 루트 기준으로 옮겨 +Z 최댓값을 찾는다.
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    var world = mf.transform.TransformPoint(corner);
                    float z = Vector3.Dot(world - root.position, root.forward);
                    if (z > far) far = z;
                }
            }
            return far;
        }

        /// <summary>보스의 몸통 한가운데. 렌더러가 없으면 트랜스폼 위치로 물러선다.</summary>
        private Vector3 BossAimPoint()
        {
            if (boss == null) return transform.position;
            if (grabPoint != null) return grabPoint.position;
            var rs = boss.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Bounds b = new Bounds();
            foreach (var r in rs)
            {
                if (r == null) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any ? b.center : boss.position;
        }

        /// <summary>끌려가는 동안 보스가 지형·물리에 붙들리지 않게 떼어낸다.</summary>
        private void ReleaseBossFromWorld()
        {
            if (boss == null) return;

            var agent = boss.GetComponentInChildren<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            var rb = boss.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            foreach (var c in boss.GetComponentsInChildren<Collider>(true))
                if (c != null) c.enabled = false;
        }

        /// <summary>균열 각 파트의 원래 밝기에 배율을 곱한다.</summary>
        private void SetRiftIntensityScale(float k)
        {
            if (riftRenderers == null || riftBaseIntensity == null || mpb == null) return;
            for (int i = 0; i < riftRenderers.Length; i++)
            {
                var r = riftRenderers[i];
                if (r == null || riftBaseIntensity[i] <= 0f) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(IntensityId, riftBaseIntensity[i] * k);
                r.SetPropertyBlock(mpb);
            }
        }

        private void SetRiftFloat(int id, float value)
        {
            if (riftRenderers == null || mpb == null) return;
            foreach (var r in riftRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(id, value);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
