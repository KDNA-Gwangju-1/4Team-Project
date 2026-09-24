using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

public static class GardenPavingInstaller
{
    const string Root = "Assets/BrightDream/Art/GardenPaving/";
    [MenuItem("Tools/Bright Dream/Apply Grounded Garden Paving")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.name != "SD_BrightDream_Blockout_Rect")
            throw new System.InvalidOperationException("Open the Chapter 1 scene in edit mode.");
        var oldPath = GameObject.Find("MainPathStones_Final_V41");
        if (oldPath != null) { Undo.RecordObject(oldPath, "Replace scattered path"); oldPath.SetActive(false); }
        var path = GameObject.Find("GardenPath_Continuous");
        if (path == null) { path = new GameObject("GardenPath_Continuous"); Undo.RegisterCreatedObjectUndo(path, "Create continuous garden path"); }
        SetMesh(path, "GardenPath");
        path.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        path.transform.localScale = Vector3.one;

        // Keep the exact collider component referenced by AI, spawn and containment.
        var arena = GameObject.Find("BossArena_BlenderPaving");
        if (arena == null) throw new System.InvalidOperationException("Existing arena collider is missing.");
        Undo.RecordObject(arena.transform, "Ground arena pivot");
        arena.transform.SetPositionAndRotation(new Vector3(8.07f, 0f, 67.61f), Quaternion.identity);
        arena.transform.localScale = Vector3.one;
        SetMesh(arena, "ArenaTerrace");
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
        {
            // Retain the NavMeshData GUID so other serialized consumers stay valid.
            var existing = surface.navMeshData;
            surface.BuildNavMesh();
            var generated = surface.navMeshData;
            if (existing != null && generated != existing)
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(generated, existing);
                surface.navMeshData = existing;
                surface.AddData();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(generated);
            }
            EditorUtility.SetDirty(surface);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static void SetMesh(GameObject target, string sourceName)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + sourceName + ".fbx").GetComponentInChildren<MeshFilter>();
        var filter = target.GetComponent<MeshFilter>();
        if (filter == null) filter = target.AddComponent<MeshFilter>();
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = target.AddComponent<MeshRenderer>();
        var collider = target.GetComponent<MeshCollider>();
        if (collider == null) collider = target.AddComponent<MeshCollider>();
        Undo.RecordObjects(new Object[] { filter, renderer, collider }, "Apply garden paving");
        filter.sharedMesh = source.sharedMesh;
        collider.sharedMesh = source.sharedMesh;
        collider.enabled = true;
        renderer.sharedMaterials = source.GetComponent<Renderer>().sharedMaterials.Select(m =>
        {
            string materialPath = Root + m.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                material.color = m.color;
                material.SetFloat("_Glossiness", .12f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            return material;
        }).ToArray();
    }
}
