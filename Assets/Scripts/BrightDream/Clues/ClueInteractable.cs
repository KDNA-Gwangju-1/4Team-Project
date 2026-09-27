using UnityEngine;

namespace BrightDream.Clues
{
    /// <summary>
    /// 개별 단서 오브젝트. Trigger Collider 로 상호작용 판정 범위를 잡고,
    /// 조사 완료 시 ClueManager 에 자신을 등록한다. 조사 후에도 Visual 은 그대로 남는다.
    /// 원본 Material 은 건드리지 않고, 강조(Emission) 효과는 런타임에 생성되는 복제본에만 적용한다.
    /// </summary>
    public class ClueInteractable : MonoBehaviour
    {
        [Header("단서 정보")]
        [SerializeField] private string clueId;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string investigateText;

        [Header("강조 효과 (은은한 Emission)")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color glowColor = new Color(1f, 0.93f, 0.7f);
        [SerializeField] private float idleIntensity = 0.15f;
        [SerializeField] private float highlightIntensity = 0.35f;
        [SerializeField] private float collectedIntensity = 0.03f;

        [Header("조사 후 처리")]
        [Tooltip("끄면 조사 후에도 visual(Renderer)이 그대로 남는다 - 나무 새김처럼 '집어가는' 게 아니라 자리에 계속 있어야 하는 단서용. Trigger Collider는 이 값과 무관하게 항상 꺼진다.")]
        [SerializeField] private bool hideVisualOnInvestigate = true;

        [Header("Outline (선택)")]
        [SerializeField] private ClueOutlineController outline;

        [Tooltip("자동 걷기 테스트 중 이 거리 안으로 플레이어가 지나가면 자동으로 조사 처리한다 - " +
                 "실제 상호작용(E키)은 카메라 레이캐스트라 경로에서 좀 떨어져 있어도 되는데, " +
                 "자동 걷기는 그 레이캐스트를 쏠 수 없어서 대신 거리로 판정한다.")]
        [SerializeField] private float autoWalkDetectRadius = 3f;

        public string ClueId => clueId;
        public string DisplayName => displayName;
        public string InvestigateText => investigateText;
        public bool IsCollected { get; private set; }

        private Material[] runtimeMaterials;
        private Collider[] targetColliders;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>();

            // Trigger Collider(상호작용 판정용)만 조사 후 끈다 - TrunkCollider 같은 solid Collider는
            // 나무 본체의 실제 충돌이므로 조사 여부와 무관하게 항상 그대로 둔다.
            var allColliders = GetComponentsInChildren<Collider>(true);
            var triggers = new System.Collections.Generic.List<Collider>();
            foreach (var c in allColliders) if (c.isTrigger) triggers.Add(c);
            targetColliders = triggers.ToArray();

            runtimeMaterials = new Material[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] == null) continue;
                // .material 접근 시 Unity 가 자동으로 복제본을 만들어 준다 (원본 에셋 Material 은 그대로 유지됨).
                Material mat = targetRenderers[i].material;
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                runtimeMaterials[i] = mat;
            }

            ApplyGlow(idleIntensity);
        }

        /// <summary>플레이어가 상호작용 거리 안에서 이 단서를 바라보고 있는지 여부.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (IsCollected) return;
            ApplyGlow(highlighted ? highlightIntensity : idleIntensity);
            if (outline != null) outline.SetHovering(highlighted);
        }

        private Transform autoWalkPlayer;

        /// <summary>
        /// 자동 걷기 테스트 중에는 E키를 누를 사람이 없으니 대신 거리로 자동 조사한다.
        /// 실제 상호작용은 카메라 레이캐스트(최대 3.5m)라 물리적으로 겹치지 않아도 되는데,
        /// OnTriggerEnter로 시도해보니 경로가 단서 콜라이더와 실제로 겹치지 않는 경우가 많아
        /// 매 프레임 거리 체크로 바꿨다 - 4개뿐이라 비용은 무시할 수 있다.
        /// </summary>
        private void Update()
        {
            if (IsCollected || !SimpleFirstPersonController.IsAutoWalking) return;

            if (autoWalkPlayer == null)
            {
                var player = BrightDream.Combat.PlayerHealth.Instance;
                if (player == null) return;
                autoWalkPlayer = player.transform;
            }

            if (Vector3.Distance(transform.position, autoWalkPlayer.position) <= autoWalkDetectRadius) Investigate();
        }

        public void Investigate()
        {
            if (IsCollected) return;
            IsCollected = true;
            ApplyGlow(collectedIntensity);
            ClueManager.Instance?.CollectClue(this);
            if (outline != null) outline.SetCollected();

            // 플레이어가 챙긴 것처럼 처리 - GameObject 자체는 유지한다.
            // Trigger(상호작용 판정)는 항상 끄고, visual은 hideVisualOnInvestigate가 켜져 있을 때만 끈다.
            if (hideVisualOnInvestigate)
                foreach (Renderer r in targetRenderers) if (r != null) r.enabled = false;
            foreach (Collider c in targetColliders) if (c != null) c.enabled = false;
        }

        private void ApplyGlow(float intensity)
        {
            if (runtimeMaterials == null) return;
            Color emission = glowColor * intensity;
            foreach (Material mat in runtimeMaterials)
            {
                if (mat != null) mat.SetColor(EmissionColorId, emission);
            }
        }
    }
}
