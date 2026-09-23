using System;
using System.Collections;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스 아레나에 진입하면 균열이 열리고 그 안에서 유니콘 보스가 튀어나온 뒤
    /// 전투가 시작되게 한다.
    ///
    /// 진행 순서
    ///   1. 대기       보스와 균열을 숨기고 BossAI 를 꺼 둔다
    ///   2. 금이 감     균열은 처음부터 제 크기 자리에 있고, _Progress 가 0 에서 1 로 오르며
    ///                 금이 중심에서 바깥으로 갈라져 나간다. 유리가 깨지듯 퍼진다
    ///   3. 구멍 뚫림   금이 다 간 뒤에 안쪽이 터져 구멍(터널)이 열린다
    ///   3. 등장       보스가 균열 안에서 작게 나타나 아레나 착지 지점까지 날아온다
    ///   4. 착지       카메라 흔들림. 그 뒤 BossAI 를 켜서 전투 시작
    ///
    /// BossAI 를 컴포넌트째 꺼 두면 Unity 가 Start() 를 켜질 때까지 미룬다. 켜는 순간
    /// BossAI 가 스스로 StageProgressManager.CurrentStage 를 보고 활성화하므로
    /// BossAI 쪽 코드는 손대지 않아도 된다.
    ///
    /// 시간은 전부 unscaled 로 돈다. 단서 조사나 인트로 대사가 뜨면 Time.timeScale 이 0 이
    /// 되는데, WaitForSeconds 를 쓰면 그 사이 연출이 멈춰 버리고 최악의 경우 보스가
    /// 영영 활성화되지 않는다. HeartHealthUI 등 기존 연출과 같은 규칙이다.
    ///
    /// 균열 _Progress 는 MaterialPropertyBlock 으로 넣는다. sharedMaterial 을 직접 쓰면
    /// 에디터에서 플레이할 때 머티리얼 에셋 파일이 실제로 변경되어 버린다.
    ///
    /// 숨길 때는 _Progress 0 만으로는 부족해서 렌더러를 통째로 끈다. _Progress 는 균열 무늬
    /// (CrackReveal) 에만 있는 값이고, 터널 안쪽(RiftVoidStencil) 과 홀마스크에는 그런 값이
    /// 없어서 0 으로 둬도 보라색 구멍이 그대로 보인다. 예전에는 그래서 게임 시작부터
    /// 아레나에 구멍이 떠 있었다.
    ///
    /// 그래서 렌더러를 두 갈래로 나눠 다룬다. 금(CrackReveal) 쪽은 _Progress 로 갈라지는
    /// 연출이 이미 되어 있으니 제 크기 그대로 두고 값만 올리고, 구멍(홀마스크·터널) 쪽은
    /// 그런 값이 없으니 금이 다 간 뒤에 크기를 키워 뚫는다. 균열 전체를 점에서 키우면
    /// 금이 갈라지는 게 아니라 그림이 확대되는 것처럼 보인다.
    /// </summary>
    public class BossRiftEntrance : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("BossClear_Rift3D 루트. 자식 렌더러 전체의 _Progress 를 제어한다.")]
        [SerializeField] private Transform rift;
        [SerializeField] private BossAI boss;
        [Tooltip("보스가 착지할 지점. 비우면 보스의 시작 위치를 그대로 쓴다.")]
        [SerializeField] private Transform landingPoint;

        [Header("진행 조건")]
        [Tooltip("이 스테이지에 도달하면 연출이 시작된다. BossAI 의 Activate At Stage 와 같은 값.")]
        [SerializeField] private int triggerAtStage = 3;

        [Header("타이밍")]
        [Tooltip("진입 후 균열이 열리기까지의 뜸.")]
        [SerializeField] private float preDelay = 0.6f;
        [Tooltip("금이 갈라져 퍼지는 시간.")]
        [SerializeField] private float riftGrowDuration = 1.6f;
        [Tooltip("금이 다 간 뒤 안쪽 구멍이 뚫리는 시간.")]
        [SerializeField] private float holeOpenDuration = 0.5f;
        [Tooltip("균열이 다 열린 뒤 보스가 나오기까지의 뜸.")]
        [SerializeField] private float holdAfterRift = 0.35f;
        [Tooltip("보스가 균열에서 착지 지점까지 날아오는 시간.")]
        [SerializeField] private float emergeDuration = 0.9f;
        [Tooltip("착지 후 전투가 시작되기까지의 뜸.")]
        [SerializeField] private float postDelay = 0.5f;

        [Header("등장 연출")]
        [Tooltip("균열에서 나올 때의 시작 크기 배율.")]
        [SerializeField] private float emergeStartScale = 0.25f;
        [Tooltip("날아오는 궤적을 위로 띄우는 높이.")]
        [SerializeField] private float emergeArcHeight = 2.5f;
        [SerializeField] private float landingShakeDuration = 0.45f;
        [SerializeField] private float landingShakeMagnitude = 0.35f;

        [Header("메시지")]
        [SerializeField] private string message = "";
        [SerializeField] private float messageDuration = 2.5f;

        /// <summary>연출이 끝나 전투가 시작되는 순간 발생.</summary>
        public static event Action OnBossEntranceFinished;

        public bool IsPlaying { get; private set; }

        private Renderer[] riftRenderers;
        private Renderer[] holeRenderers;        // 홀마스크 + 터널. _Progress 가 없어 따로 다룬다
        private Vector3[] holeBaseScales;
        private MaterialPropertyBlock mpb;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private Renderer[] bossRenderers;
        private Collider[] bossColliders;
        private Vector3 landPos;
        private Vector3 baseScale;
        private bool done;

        private void Awake()
        {
            if (rift != null)
            {
                riftRenderers = rift.GetComponentsInChildren<Renderer>(true);
                mpb = new MaterialPropertyBlock();
                CacheHoleRenderers();
            }

            if (boss != null)
            {
                bossRenderers = boss.GetComponentsInChildren<Renderer>(true);
                bossColliders = boss.GetComponentsInChildren<Collider>(true);
                baseScale = boss.transform.localScale;
                landPos = landingPoint != null ? landingPoint.position : boss.transform.position;

                // 연출이 끝날 때까지 전투 로직을 재운다.
                boss.enabled = false;
            }

            SetRiftProgress(0f);
            SetRiftVisible(false);
            SetBossVisible(false);
        }

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
        }

        private void Start()
        {
            // 이미 해당 스테이지를 지난 상태로 시작했다면 연출 없이 바로 전투로.
            if (StageProgressManager.Instance != null &&
                StageProgressManager.Instance.CurrentStage >= triggerAtStage)
            {
                SkipToCombat();
            }
        }

        private void HandleStageChanged(int currentStage)
        {
            if (done || IsPlaying) return;
            if (currentStage < triggerAtStage) return;
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

            // 1) 금이 중심에서 바깥으로 갈라져 나간다. 크기는 처음부터 제 크기다.
            SetRiftProgress(0f);
            SetHoleScale(0f);
            SetRiftVisible(true);

            float t = 0f;
            while (t < riftGrowDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(riftGrowDuration, 0.0001f));
                SetRiftProgress(p * p * (3f - 2f * p));   // smoothstep
                yield return null;
            }
            SetRiftProgress(1f);

            // 2) 금이 다 간 자리가 터져 구멍이 열린다.
            t = 0f;
            while (t < holeOpenDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(holeOpenDuration, 0.0001f));
                SetHoleScale(p * p * (3f - 2f * p));
                yield return null;
            }
            SetHoleScale(1f);

            if (holdAfterRift > 0f) yield return new WaitForSecondsRealtime(holdAfterRift);

            // 2) 보스가 균열에서 나와 착지 지점으로
            Vector3 from = rift != null ? rift.position : landPos + Vector3.up * 5f;
            if (boss != null)
            {
                boss.transform.position = from;
                boss.transform.localScale = baseScale * emergeStartScale;
                SetBossVisible(true);

                t = 0f;
                while (t < emergeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / Mathf.Max(emergeDuration, 0.0001f));
                    float e = 1f - (1f - p) * (1f - p);                 // ease out
                    Vector3 pos = Vector3.Lerp(from, landPos, e);
                    pos.y += Mathf.Sin(p * Mathf.PI) * emergeArcHeight; // 포물선
                    boss.transform.position = pos;
                    boss.transform.localScale = Vector3.Lerp(baseScale * emergeStartScale, baseScale, e);
                    yield return null;
                }
                boss.transform.position = landPos;
                boss.transform.localScale = baseScale;
            }

            // 3) 착지 충격
            var shake = CameraShake.Instance;
            if (shake != null) shake.Shake(landingShakeDuration, landingShakeMagnitude);

            if (postDelay > 0f) yield return new WaitForSecondsRealtime(postDelay);

            // 4) 전투 시작
            EnableCombat();
            IsPlaying = false;
            done = true;
            if (OnBossEntranceFinished != null) OnBossEntranceFinished();
        }

        /// <summary>연출을 건너뛰고 즉시 전투 상태로 만든다 (세이브 로드나 디버그 진입용).</summary>
        public void SkipToCombat()
        {
            if (done) return;
            StopAllCoroutines();
            SetRiftProgress(1f);
            SetHoleScale(1f);
            SetRiftVisible(true);
            if (boss != null)
            {
                boss.transform.position = landPos;
                boss.transform.localScale = baseScale;
            }
            SetBossVisible(true);
            EnableCombat();
            IsPlaying = false;
            done = true;
            if (OnBossEntranceFinished != null) OnBossEntranceFinished();
        }

        private void EnableCombat()
        {
            // 컴포넌트를 켜는 순간 BossAI.Start() 가 돌면서 스스로 활성화한다.
            if (boss != null) boss.enabled = true;
        }

        private void SetRiftProgress(float value)
        {
            if (riftRenderers == null || mpb == null) return;
            foreach (var r in riftRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(ProgressId, value);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>홀마스크와 터널을 따로 모아 둔다. 이쪽은 _Progress 를 읽지 않는다.</summary>
        private void CacheHoleRenderers()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in riftRenderers)
            {
                if (r == null || r.sharedMaterial == null || r.sharedMaterial.shader == null) continue;
                string sh = r.sharedMaterial.shader.name;
                if (sh.Contains("RiftVoidStencil") || sh.Contains("RiftHoleMask")) list.Add(r);
            }
            holeRenderers = list.ToArray();
            holeBaseScales = new Vector3[holeRenderers.Length];
            for (int i = 0; i < holeRenderers.Length; i++)
                holeBaseScales[i] = holeRenderers[i].transform.localScale;
        }

        /// <summary>구멍이 뚫린 정도. 0 이면 닫혀 있고 1 이면 제 크기로 열린다.</summary>
        private void SetHoleScale(float k)
        {
            if (holeRenderers == null || holeBaseScales == null) return;
            for (int i = 0; i < holeRenderers.Length; i++)
                if (holeRenderers[i] != null)
                    holeRenderers[i].transform.localScale = holeBaseScales[i] * k;
        }

        /// <summary>
        /// 균열을 통째로 보이거나 감춘다. _Progress 만으로는 터널 안쪽이 남아서 안 된다.
        /// </summary>
        private void SetRiftVisible(bool visible)
        {
            if (riftRenderers == null) return;
            foreach (var r in riftRenderers) if (r != null) r.enabled = visible;
        }

        private void SetBossVisible(bool visible)
        {
            if (bossRenderers != null)
                foreach (var r in bossRenderers) if (r != null) r.enabled = visible;
            if (bossColliders != null)
                foreach (var c in bossColliders) if (c != null) c.enabled = visible;
        }
    }
}
