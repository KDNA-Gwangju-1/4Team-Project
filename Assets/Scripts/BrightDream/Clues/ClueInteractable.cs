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

        public string ClueId => clueId;
        public string DisplayName => displayName;
        public string InvestigateText => investigateText;
        public bool IsCollected { get; private set; }

        private Material[] runtimeMaterials;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>();

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
        }

        public void Investigate()
        {
            if (IsCollected) return;
            IsCollected = true;
            ApplyGlow(collectedIntensity);
            ClueManager.Instance?.CollectClue(this);
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
