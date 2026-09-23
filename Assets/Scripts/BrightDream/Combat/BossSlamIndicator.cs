using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스가 내려찍기 전에 바닥에 까는 피격 반경 표시.
    ///
    /// Quad 한 장을 바닥에 눕혀 두고 셰이더의 _Fill 만 움직인다. 매번 만들고 부수지 않고
    /// 한 번 만들어 껐다 켠다.
    ///
    /// 바닥 높이는 값으로 두지 않고 아래로 레이캐스트해 찾는다. 05_boss_platform 은 굴곡진
    /// 모델이라 위치마다 실제 바닥 Y 가 달라서, 고정값을 쓰면 표시가 바닥에 파묻히거나 뜬다.
    /// </summary>
    public class BossSlamIndicator : MonoBehaviour
    {
        [Tooltip("BrightDream/SlamIndicator. 비우면 이름으로 찾아본다.")]
        [SerializeField] private Shader indicatorShader;
        [Tooltip("바닥 높이를 찾을 때 겨냥할 콜라이더 - 05_boss_platform. 비우면 보스 발밑 높이를 쓴다.")]
        [SerializeField] private Collider groundCollider;
        [SerializeField] private Color fillColor = new Color(1f, 0.25f, 0.18f, 1f);
        [SerializeField] private Color edgeColor = new Color(1f, 0.75f, 0.45f, 1f);
        [Tooltip("바닥 위로 띄우는 높이. 너무 낮으면 굴곡진 바닥에 파묻힌다.")]
        [SerializeField] private float surfaceOffset = 0.05f;

        private GameObject quad;
        private Renderer quadRenderer;
        private MaterialPropertyBlock mpb;

        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");

        public void SetGroundCollider(Collider value) => groundCollider = value;

        private bool EnsureQuad()
        {
            if (quad != null) return true;

            var shader = indicatorShader != null ? indicatorShader : Shader.Find("BrightDream/SlamIndicator");
            if (shader == null)
            {
                Debug.LogWarning("[BossSlamIndicator] 표시 셰이더를 못 찾아 반경 표시 없이 진행합니다.", this);
                return false;
            }

            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BossSlamIndicator";
            // 표시용이라 물리에 끼면 안 된다. 플레이어가 위에 걸려 넘어진다.
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            mpb = new MaterialPropertyBlock();
            quad.SetActive(false);
            return true;
        }

        private void OnDestroy()
        {
            if (quadRenderer != null && quadRenderer.sharedMaterial != null)
                DestroyImmediate(quadRenderer.sharedMaterial);
            if (quad != null) Destroy(quad);
        }

        /// <summary>center 자리에 반경 radius 로 표시를 띄운다.</summary>
        public void Show(Vector3 center, float radius)
        {
            if (!EnsureQuad()) return;

            float y = SampleGroundHeight(center);
            quad.transform.position = new Vector3(center.x, y + surfaceOffset, center.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // Quad 는 기본이 수직이라 눕힌다
            quad.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            quad.SetActive(true);
            SetFill(0f);
        }

        public void SetFill(float fill)
        {
            if (quadRenderer == null || mpb == null) return;
            quadRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(FillId, Mathf.Clamp01(fill));
            mpb.SetFloat(AlphaId, 1f);
            mpb.SetColor(ColorId, fillColor);
            mpb.SetColor(EdgeColorId, edgeColor);
            quadRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>내려찍은 뒤 남은 표시를 서서히 지울 때 쓴다.</summary>
        public void SetAlpha(float alpha)
        {
            if (quadRenderer == null || mpb == null) return;
            quadRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(AlphaId, Mathf.Clamp01(alpha));
            quadRenderer.SetPropertyBlock(mpb);
        }

        public void Hide()
        {
            if (quad != null) quad.SetActive(false);
        }

        private float SampleGroundHeight(Vector3 point)
        {
            if (groundCollider != null)
            {
                Bounds b = groundCollider.bounds;
                var ray = new Ray(new Vector3(point.x, b.max.y + 5f, point.z), Vector3.down);
                if (groundCollider.Raycast(ray, out RaycastHit hit, b.size.y + 10f)) return hit.point.y;
                return b.max.y;
            }
            return point.y;
        }
    }
}
