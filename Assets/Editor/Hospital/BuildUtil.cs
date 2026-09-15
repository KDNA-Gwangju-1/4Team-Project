using UnityEditor;
using UnityEngine;

/// <summary>
/// 씬 빌더들이 같이 쓰는 도우미 모음. (에디터 전용)
///
/// 기본 도형(큐브/원기둥/구/캡슐)을 "중심 위치 + 실제 크기(m)" 로 바로 놓을 수 있게 해 주고,
/// 머티리얼과 폴더를 만들어 준다.
///
/// 장식용 오브젝트는 기본적으로 Collider 를 떼어 낸다.
/// 벽·바닥·가구처럼 플레이어가 부딪혀야 하는 것만 keepCollider: true 로 남긴다.
/// </summary>
public static class BuildUtil
{
    // ============================================================
    // 도형 놓기
    // ============================================================

    /// <summary>정육면체. size 는 실제 가로·세로·높이(m).</summary>
    public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size,
                                 Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, name, parent, center, size, mat, keepCollider);
        return go;
    }

    /// <summary>원기둥. diameter 는 지름(m), height 는 전체 높이(m).</summary>
    public static GameObject Cylinder(string name, Transform parent, Vector3 center,
                                      float diameter, float height, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, name, parent, center, new Vector3(diameter, height * 0.5f, diameter), mat, keepCollider);
        return go;
    }

    /// <summary>구. size 는 실제 지름(m) 세 방향.</summary>
    public static GameObject Sphere(string name, Transform parent, Vector3 center, Vector3 size,
                                    Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Setup(go, name, parent, center, size, mat, keepCollider);
        return go;
    }

    /// <summary>캡슐. diameter 는 지름(m), height 는 전체 길이(m). 기본은 Y축 방향으로 선다.</summary>
    public static GameObject Capsule(string name, Transform parent, Vector3 center,
                                     float diameter, float height, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Setup(go, name, parent, center, new Vector3(diameter, height * 0.5f, diameter), mat, keepCollider);
        return go;
    }

    /// <summary>자식 없이 위치만 잡아 두는 빈 오브젝트.</summary>
    public static GameObject Empty(string name, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    private static void Setup(GameObject go, string name, Transform parent, Vector3 center,
                              Vector3 size, Material mat, bool keepCollider)
    {
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);

        go.transform.localPosition = center;
        go.transform.localScale = size;

        if (mat != null)
        {
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        var collider = go.GetComponent<Collider>();
        if (collider != null && !keepCollider) Object.DestroyImmediate(collider);
    }

    // ============================================================
    // 머티리얼
    // ============================================================

    /// <summary>불투명 머티리얼을 만들어 에셋으로 저장한다. 이미 있으면 값만 덮어쓴다.</summary>
    public static Material Mat(string folder, string name, Color color,
                               float smoothness = 0.2f, float metallic = 0f)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.SetColor("_Color", color);
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        return Save(mat, folder, name);
    }

    /// <summary>스스로 빛나는 머티리얼. (조명 패널, 창밖 건물 불빛 등)</summary>
    public static Material EmissiveMat(string folder, string name, Color color, Color emission,
                                       float smoothness = 0.2f)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.SetColor("_Color", color);
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        return Save(mat, folder, name);
    }

    /// <summary>반투명 머티리얼. (창유리 등)</summary>
    public static Material GlassMat(string folder, string name, Color color, float smoothness = 0.85f)
    {
        var mat = new Material(Shader.Find("Standard"));

        // Standard 셰이더를 Transparent 모드로 바꾸는 정해진 절차
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        mat.SetColor("_Color", color);
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);

        return Save(mat, folder, name);
    }

    private static Material Save(Material mat, string folder, string name)
    {
        EnsureFolder(folder);

        string path = $"{folder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            // 이미 쓰고 있는 곳들이 끊기지 않도록, 에셋은 그대로 두고 값만 갈아 끼운다.
            int queue = mat.renderQueue;
            existing.shader = mat.shader;
            existing.CopyPropertiesFromMaterial(mat);
            existing.renderQueue = queue;
            existing.globalIlluminationFlags = mat.globalIlluminationFlags;

            Object.DestroyImmediate(mat);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ============================================================
    // 폴더 / 레이어
    // ============================================================

    /// <summary>"Assets/A/B/C" 같은 경로의 폴더를 없으면 만들어 준다.</summary>
    public static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];   // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    /// <summary>레이어가 없으면 빈 칸에 만들어 주고 번호를 돌려준다.</summary>
    public static int EnsureLayer(string layerName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[BuildUtil] TagManager 를 찾지 못해 '{layerName}' 레이어를 만들지 못했습니다.");
            return 0;
        }

        var tagManager = new SerializedObject(assets[0]);
        var layers = tagManager.FindProperty("layers");

        // 이미 있으면 그 번호를 쓴다.
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;
        }

        // 0~7 번은 유니티가 쓰는 자리라 8번부터 찾는다.
        for (int i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        Debug.LogWarning($"[BuildUtil] 빈 레이어 칸이 없어 '{layerName}' 을 만들지 못했습니다. Default 로 둡니다.");
        return 0;
    }

    /// <summary>자식까지 전부 같은 레이어로 바꾼다.</summary>
    public static void SetLayerRecursive(GameObject root, int layer)
    {
        if (root == null) return;

        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
