using UnityEngine;

/// <summary>
/// 단서 실루엣 가장자리에 얇은 Outline을 그리는 컴포넌트 (마인크래프트 Glowing과 비슷한 은은한 느낌).
/// 원본 Renderer/Material은 전혀 건드리지 않고, 같은 메시를 쓰는 별도의 자식 Renderer를 만들어
/// 전용 Outline 셰이더로 그린다. 깊이 테스트를 그대로 쓰므로 벽/다른 오브젝트에 가리면 안 보인다.
/// idle/hover/collected 상태는 ClueInteractable이 호출해서 제어한다.
/// </summary>
public class ClueOutlineController : MonoBehaviour
{
    public enum OutlineMode
    {
        Hull, // 닫힌 3D 메시(리본/사진첩/편지) - 뒤집힌 껍질 방식
        Flat, // 얇은 평면형 decal(나무 새김 글자) - 살짝 키워서 뒤에 깔아두는 방식
    }

    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private OutlineMode mode = OutlineMode.Hull;

    [Header("기본 상태 (약한 Outline)")]
    [SerializeField] private Color idleColor = new Color(0.98f, 0.92f, 0.72f, 1f);
    [SerializeField] private float idleWidth = 0.006f;
    [Tooltip("Flat 모드 전용: 텍스처 알파 기반 가장자리 두께(텍셀 단위) - 나무 새김처럼 글자 모양이 텍스처에만 있는 decal에 사용")]
    [SerializeField] private float idleEdgeTexels = 2.5f;
    [SerializeField] private float idlePulseAmount = 0.10f;
    [SerializeField] private float idlePulseSpeed = 1.0f;

    [Header("조준 상태 (조금 더 밝게)")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.97f, 0.82f, 1f);
    [SerializeField] private float hoverWidth = 0.012f;
    [SerializeField] private float hoverEdgeTexels = 4.5f;
    [SerializeField] private float hoverPulseAmount = 0.15f;
    [SerializeField] private float hoverPulseSpeed = 1.8f;

    private GameObject[] outlineObjects;
    private Material[] outlineMaterials;
    private bool collected;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineWidthTexelsId = Shader.PropertyToID("_OutlineWidthTexels");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
    private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        string shaderName = mode == OutlineMode.Hull ? "BrightDream/ClueOutlineHull" : "BrightDream/ClueOutlineFlat";
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning("[ClueOutlineController] shader not found: " + shaderName);
            return;
        }

        outlineObjects = new GameObject[targetRenderers.Length];
        outlineMaterials = new Material[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer src = targetRenderers[i];
            if (src == null) continue;
            MeshFilter srcMf = src.GetComponent<MeshFilter>();
            if (srcMf == null || srcMf.sharedMesh == null) continue;

            var go = new GameObject(src.gameObject.name + "_Outline");
            go.transform.SetParent(src.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var outMf = go.AddComponent<MeshFilter>();
            // 원본 메시는 건드리지 않기 위해 복제본에만 노멀을 재계산한다.
            // (양면 렌더링용으로 각 삼각형이 반대 방향 winding으로 중복되어 있는 얇은 decal 메시는
            // RecalculateNormals가 정반대 방향 면끼리 상쇄되어 노멀이 0벡터가 되므로, 먼저 중복된
            // reverse-winding 삼각형을 제거한 단면 사본을 만든 뒤 노멀을 계산한다.)
            var meshCopy = BuildSingleSidedCopy(srcMf.sharedMesh);
            meshCopy.RecalculateNormals();
            outMf.sharedMesh = meshCopy;

            var outMr = go.AddComponent<MeshRenderer>();
            var mat = new Material(shader);
            if (mode == OutlineMode.Flat && mat.HasProperty(MainTexId) && src.sharedMaterial != null)
            {
                // Flat 모드는 글자 모양이 텍스처 알파에만 있으므로, 같은 텍스처/타일링을 그대로 참조한다.
                mat.SetTexture(MainTexId, src.sharedMaterial.mainTexture);
                mat.mainTextureScale = src.sharedMaterial.mainTextureScale;
                mat.mainTextureOffset = src.sharedMaterial.mainTextureOffset;
            }
            outMr.sharedMaterial = mat;
            outMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outMr.receiveShadows = false;

            outlineObjects[i] = go;
            outlineMaterials[i] = mat;
        }

        ApplyIdle();
    }

    /// <summary>
    /// 양면 렌더링을 위해 각 삼각형이 반대 winding으로 중복된 메시에서,
    /// 같은 3개 정점을 참조하는 reverse-winding 중복 삼각형을 제거한 단면 사본을 만든다.
    /// (정점/UV는 그대로 유지하고 triangles 목록만 절반으로 줄인다 - 이후 셰이더에서 Cull Off로
    /// 그리므로 시각적으로는 동일하게 양면 렌더링되지만, RecalculateNormals가 상쇄되지 않는다.)
    /// </summary>
    private static Mesh BuildSingleSidedCopy(Mesh source)
    {
        var copy = Object.Instantiate(source);
        var tris = source.triangles;
        var kept = new System.Collections.Generic.List<int>(tris.Length);
        var seen = new System.Collections.Generic.HashSet<long>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2];
            int lo = Mathf.Min(a, Mathf.Min(b, c));
            int hi = Mathf.Max(a, Mathf.Max(b, c));
            int mid = a + b + c - lo - hi;
            long key = ((long)lo * 1000000L) + ((long)mid * 1000L) + hi;

            if (seen.Contains(key)) continue;
            seen.Add(key);
            kept.Add(a); kept.Add(b); kept.Add(c);
        }

        copy.triangles = kept.ToArray();
        return copy;
    }

    public void SetHovering(bool hovering)
    {
        if (collected) return;
        if (hovering) ApplyHover();
        else ApplyIdle();
    }

    public void SetCollected()
    {
        collected = true;
        if (outlineObjects == null) return;
        foreach (var go in outlineObjects)
            if (go != null) go.SetActive(false);
    }

    private void ApplyIdle() => ApplyState(idleColor, idleWidth, idleEdgeTexels, idlePulseAmount, idlePulseSpeed);
    private void ApplyHover() => ApplyState(hoverColor, hoverWidth, hoverEdgeTexels, hoverPulseAmount, hoverPulseSpeed);

    private void ApplyState(Color color, float width, float edgeTexels, float pulseAmount, float pulseSpeed)
    {
        if (outlineMaterials == null) return;
        foreach (var mat in outlineMaterials)
        {
            if (mat == null) continue;
            mat.SetColor(OutlineColorId, color);
            if (mat.HasProperty(OutlineWidthId)) mat.SetFloat(OutlineWidthId, width);
            if (mat.HasProperty(OutlineWidthTexelsId)) mat.SetFloat(OutlineWidthTexelsId, edgeTexels);
            mat.SetFloat(PulseAmountId, pulseAmount);
            mat.SetFloat(PulseSpeedId, pulseSpeed);
        }
    }
}
