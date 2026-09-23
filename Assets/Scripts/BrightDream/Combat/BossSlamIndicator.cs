using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스가 내려찍기 전에 바닥에 까는 피격 반경 표시.
    ///
    /// 바닥 굴곡을 따라가는 격자 메시 한 장에 셰이더의 _Fill 만 움직인다. 매번 만들고 부수지 않고
    /// 한 번 만들어 껐다 켠다.
    ///
    /// 05_boss_platform 은 돌판이 솟고 사이가 파인 굴곡진 모델이라, 평평한 판 한 장은 반경 9m 안에서
    /// 면적의 약 40% 가 튀어나온 돌에 파묻힌다. 그래서 격자 점마다 아래로 레이캐스트해 실제 바닥 높이에 붙인다.
    /// </summary>
    public class BossSlamIndicator : MonoBehaviour
    {
        [Tooltip("BrightDream/SlamIndicator. 비우면 이름으로 찾아본다.")]
        [SerializeField] private Shader indicatorShader;
        [Tooltip("바닥 높이를 찾을 때 겨냥할 콜라이더 - 05_boss_platform. 비우면 보스 발밑 높이를 쓴다.")]
        [SerializeField] private Collider groundCollider;
        [SerializeField] private Color fillColor = new Color(1f, 0.25f, 0.18f, 1f);
        [SerializeField] private Color edgeColor = new Color(1f, 0.75f, 0.45f, 1f);
        [Tooltip("바닥 위로 띄우는 높이. 표시가 바닥 굴곡을 따라가므로 조금만 띄우면 된다.")]
        [SerializeField] private float surfaceOffset = 0.06f;
        [Tooltip("표시 격자의 한 변 칸 수. 클수록 돌판 굴곡을 촘촘히 따라간다 (반경 9m 기준 48칸 = 0.375m 간격).")]
        [SerializeField, Range(8, 96)] private int gridResolution = 48;

        private GameObject quad;
        private Renderer quadRenderer;
        private MaterialPropertyBlock mpb;
        private Mesh gridMesh;
        private Vector3[] gridVertices;
        private float[] groundHeights;

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

            // 콜라이더 없는 오브젝트로 직접 만든다 - 표시용이라 물리에 끼면 플레이어가 위에 걸려 넘어진다.
            quad = new GameObject("BossSlamIndicator");
            gridMesh = BuildGridMesh(gridResolution);
            quad.AddComponent<MeshFilter>().sharedMesh = gridMesh;
            gridVertices = gridMesh.vertices;
            groundHeights = new float[gridVertices.Length];

            quadRenderer = quad.AddComponent<MeshRenderer>();
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
            if (gridMesh != null) Destroy(gridMesh);
            if (quad != null) Destroy(quad);
        }

        /// <summary>center 자리에 반경 radius 로 표시를 띄운다.</summary>
        public void Show(Vector3 center, float radius)
        {
            if (!EnsureQuad()) return;

            float centerY = SampleGroundHeight(center);
            if (float.IsNaN(centerY)) centerY = center.y;
            quad.transform.SetPositionAndRotation(new Vector3(center.x, 0f, center.z), Quaternion.identity);
            quad.transform.localScale = Vector3.one;
            ConformToGround(center, radius, centerY);

            quad.SetActive(true);
            SetFill(0f);
        }

        /// <summary>
        /// 격자 점마다 바닥 높이를 재고, 자기와 주변 8칸 중 가장 높은 값에 붙인다. 점 사이의 삼각형은
        /// 직선으로 이어지므로, 이렇게 한 칸씩 부풀려야 솟은 돌판 가장자리를 가로지르는 면이 돌 속으로 파고들지 않는다.
        /// </summary>
        private void ConformToGround(Vector3 center, float radius, float fallbackY)
        {
            int n = gridResolution + 1;
            float size = radius * 2f;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                float lx = (x / (float)gridResolution - 0.5f) * size;
                float lz = (z / (float)gridResolution - 0.5f) * size;
                float h = SampleGroundHeight(new Vector3(center.x + lx, fallbackY, center.z + lz));
                groundHeights[z * n + x] = float.IsNaN(h) ? fallbackY : h;
                gridVertices[z * n + x] = new Vector3(lx, 0f, lz);
            }

            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                float top = float.MinValue;
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx, sz = z + dz;
                    if (sx < 0 || sz < 0 || sx >= n || sz >= n) continue;
                    top = Mathf.Max(top, groundHeights[sz * n + sx]);
                }
                gridVertices[z * n + x].y = top + surfaceOffset;
            }

            gridMesh.vertices = gridVertices;
            gridMesh.RecalculateBounds();
        }

        /// <summary>XZ 평면에 눕힌 (resolution x resolution) 칸 격자. uv 는 Quad 와 같이 0~1 이라 셰이더를 그대로 쓴다.</summary>
        private static Mesh BuildGridMesh(int resolution)
        {
            int n = resolution + 1;
            var vertices = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                vertices[z * n + x] = new Vector3(x / (float)resolution - 0.5f, 0f, z / (float)resolution - 0.5f);
                uvs[z * n + x] = new Vector2(x / (float)resolution, z / (float)resolution);
            }

            var triangles = new int[resolution * resolution * 6];
            int t = 0;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int i = z * n + x;
                triangles[t++] = i; triangles[t++] = i + n; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + n; triangles[t++] = i + n + 1;
            }

            var mesh = new Mesh { name = "BossSlamIndicatorGrid" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
                // 격자 끝이 돌판 밖으로 나간 경우 - 호출한 쪽이 가운데 높이로 대신 채운다.
                return float.NaN;
            }
            return point.y;
        }
    }
}
