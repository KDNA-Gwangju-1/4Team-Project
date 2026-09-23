using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스가 돌진하기 전에 바닥에 까는 피격 경로 표시. BossSlamIndicator 의 돌진 버전.
    ///
    /// 돌진 판정은 "보스 몸 반경 원이 출발점에서 도착점까지 쓸고 가는 영역"이라, 같은 모양
    /// (양끝이 둥근 막대)을 그린다. 05_boss_platform 은 돌판이 솟고 사이가 파인 굴곡진 바닥이라
    /// 격자 점마다 아래로 레이캐스트해 실제 바닥 높이에 붙인다.
    /// </summary>
    public class BossChargeIndicator : MonoBehaviour
    {
        [Tooltip("BrightDream/ChargeIndicator. 비우면 이름으로 찾아본다.")]
        [SerializeField] private Shader indicatorShader;
        [Tooltip("바닥 높이를 찾을 때 겨냥할 콜라이더 - 05_boss_platform. 비우면 보스 발밑 높이를 쓴다.")]
        [SerializeField] private Collider groundCollider;
        [SerializeField] private Color fillColor = new Color(1f, 0.25f, 0.18f, 1f);
        [SerializeField] private Color edgeColor = new Color(1f, 0.75f, 0.45f, 1f);
        [Tooltip("바닥 위로 띄우는 높이. 바닥 굴곡을 따라가므로 조금만 띄우면 된다.")]
        [SerializeField] private float surfaceOffset = 0.06f;
        [Tooltip("격자 한 칸 크기(m). 작을수록 돌판 굴곡을 촘촘히 따라간다.")]
        [SerializeField] private float cellSize = 0.4f;

        private GameObject surface;
        private Renderer surfaceRenderer;
        private MaterialPropertyBlock mpb;
        private Mesh mesh;
        private int cols, rows;
        private Vector3[] vertices;
        private float[] groundHeights;

        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int LengthId = Shader.PropertyToID("_Length");
        private static readonly int WidthId = Shader.PropertyToID("_Width");

        private bool EnsureSurface()
        {
            if (surface != null) return true;

            var shader = indicatorShader != null ? indicatorShader : Shader.Find("BrightDream/ChargeIndicator");
            if (shader == null)
            {
                Debug.LogWarning("[BossChargeIndicator] 표시 셰이더를 못 찾아 경로 표시 없이 진행합니다.", this);
                return false;
            }

            // 콜라이더 없는 오브젝트로 직접 만든다 - 표시용이라 물리에 끼면 플레이어가 위에 걸려 넘어진다.
            surface = new GameObject("BossChargeIndicator");
            mesh = new Mesh { name = "BossChargeIndicatorGrid" };
            mesh.MarkDynamic();
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surfaceRenderer = surface.AddComponent<MeshRenderer>();
            surfaceRenderer.sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surfaceRenderer.receiveShadows = false;

            mpb = new MaterialPropertyBlock();
            surface.SetActive(false);
            return true;
        }

        private void OnDestroy()
        {
            if (surfaceRenderer != null && surfaceRenderer.sharedMaterial != null)
                DestroyImmediate(surfaceRenderer.sharedMaterial);
            if (mesh != null) Destroy(mesh);
            if (surface != null) Destroy(surface);
        }

        /// <summary>start 에서 dir 방향으로 distance 만큼 돌진하는 경로를, 판정 반경 radius 로 띄운다.</summary>
        public void Show(Vector3 start, Vector3 dir, float distance, float radius)
        {
            if (!EnsureSurface()) return;

            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float length = distance + radius * 2f;
            float width = radius * 2f;

            BuildGrid(Mathf.Max(2, Mathf.CeilToInt(width / cellSize)), Mathf.Max(2, Mathf.CeilToInt(length / cellSize)));

            float fallbackY = SampleGroundHeight(start);
            if (float.IsNaN(fallbackY)) fallbackY = start.y;

            // 표시 오브젝트는 원점에 두고 정점을 월드 좌표로 바로 쓴다.
            surface.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            surface.transform.localScale = Vector3.one;

            int n = cols + 1;
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= cols; x++)
            {
                float along = z / (float)rows * length - radius;   // 보스 뒤쪽 끝(-radius)부터 도착점 앞(distance + radius)까지
                float across = (x / (float)cols - 0.5f) * width;
                Vector3 p = start + dir * along + side * across;
                float h = SampleGroundHeight(p);
                groundHeights[z * n + x] = float.IsNaN(h) ? fallbackY : h;
                vertices[z * n + x] = p;
            }

            // 자기와 주변 8칸 중 가장 높은 바닥에 붙인다 - 점 사이 삼각형이 솟은 돌판 가장자리를 파고들지 않게.
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= cols; x++)
            {
                float top = float.MinValue;
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx, sz = z + dz;
                    if (sx < 0 || sz < 0 || sx > cols || sz > rows) continue;
                    top = Mathf.Max(top, groundHeights[sz * n + sx]);
                }
                vertices[z * n + x].y = top + surfaceOffset;
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            surface.SetActive(true);
            surfaceRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(LengthId, length);
            mpb.SetFloat(WidthId, width);
            surfaceRenderer.SetPropertyBlock(mpb);
            SetFill(0f);
        }

        public void SetFill(float fill)
        {
            if (surfaceRenderer == null || mpb == null) return;
            surfaceRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(FillId, Mathf.Clamp01(fill));
            mpb.SetFloat(AlphaId, 1f);
            mpb.SetColor(ColorId, fillColor);
            mpb.SetColor(EdgeColorId, edgeColor);
            surfaceRenderer.SetPropertyBlock(mpb);
        }

        public void Hide()
        {
            if (surface != null) surface.SetActive(false);
        }

        /// <summary>cols x rows 칸 격자의 삼각형/uv 를 만든다. 칸 수가 그대로면 다시 만들지 않는다.</summary>
        private void BuildGrid(int newCols, int newRows)
        {
            if (newCols == cols && newRows == rows && vertices != null) return;
            cols = newCols; rows = newRows;
            int n = cols + 1;
            vertices = new Vector3[n * (rows + 1)];
            groundHeights = new float[vertices.Length];

            var uvs = new Vector2[vertices.Length];
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= cols; x++)
                uvs[z * n + x] = new Vector2(x / (float)cols, z / (float)rows);

            var triangles = new int[cols * rows * 6];
            int t = 0;
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < cols; x++)
            {
                int i = z * n + x;
                triangles[t++] = i; triangles[t++] = i + n; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + n; triangles[t++] = i + n + 1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
        }

        private float SampleGroundHeight(Vector3 point)
        {
            if (groundCollider != null)
            {
                Bounds b = groundCollider.bounds;
                var ray = new Ray(new Vector3(point.x, b.max.y + 5f, point.z), Vector3.down);
                if (groundCollider.Raycast(ray, out RaycastHit hit, b.size.y + 10f)) return hit.point.y;
                return float.NaN; // 경로 끝이 돌판 밖으로 나간 경우 - 호출한 쪽이 출발점 높이로 채운다.
            }
            return point.y;
        }
    }
}
