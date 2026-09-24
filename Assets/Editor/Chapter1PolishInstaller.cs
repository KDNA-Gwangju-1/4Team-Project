using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit authoring command; never runs during gameplay or an automatic import.
public static class Chapter1PolishInstaller
{
    const string Root = "Assets/BrightDream/Art/Chapter1Polish/";
    static Material[] Materials(Renderer source)
    {
        return source.sharedMaterials.Select(original =>
        {
            string path = Root + original.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("BrightDream/MetreFelt"));
                material.color = original.color;
                material.SetFloat("_Glossiness", original.name.Contains("Claw") ? .32f : .12f);
                material.SetFloat("_Grain", .16f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }).ToArray();
    }

    static MeshFilter Model(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Root + name + ".fbx").GetComponentInChildren<MeshFilter>();
    }

    [MenuItem("Tools/Bright Dream/Apply Blender Chapter 1 Polish")]
    public static void ApplyMenu() { Apply(); }

    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "SD_BrightDream_Blockout_Rect") throw new System.InvalidOperationException("Open the Chapter 1 scene first.");
        int count = 0;
        var replacements = new Dictionary<string, string> {
            {"10_backdrop_treeline", "Treeline"}, {"09_backdrop_hills", "Hills"},
            {"14_tree_half", "HalfTree"}, {"12_bush_row", "BushRow"}
        };
        foreach (var entry in replacements)
        {
            var source = Model(entry.Value);
            var targets = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.sharedMesh != null && (AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/" + entry.Key + "/")
                    || AssetDatabase.GetAssetPath(f.sharedMesh) == Root + entry.Value + "_Fitted.asset")).ToArray();
            if (targets.Length == 0) continue;
            var oldBounds = targets[0].sharedMesh.bounds;
            var bounds = source.sharedMesh.bounds;
            var scale = new Vector3(oldBounds.size.x / bounds.size.x, oldBounds.size.y / bounds.size.y, oldBounds.size.z / bounds.size.z);
            string path = Root + entry.Value + "_Fitted.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = Object.Instantiate(source.sharedMesh); AssetDatabase.CreateAsset(mesh, path); }
            else EditorUtility.CopySerialized(source.sharedMesh, mesh);
            mesh.vertices = source.sharedMesh.vertices.Select(v => Vector3.Scale(v - bounds.center, scale) + oldBounds.center).ToArray();
            mesh.normals = source.sharedMesh.normals.Select(n => new Vector3(n.x / scale.x, n.y / scale.y, n.z / scale.z).normalized).ToArray();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var materials = Materials(source.GetComponent<Renderer>());
            foreach (var target in targets)
            {
                Undo.RecordObject(target, "Replace blurry wall mesh");
                Undo.RecordObject(target.GetComponent<Renderer>(), "Replace wall materials");
                // Preserve every Transform, collider, placement and navigation obstacle.
                target.sharedMesh = mesh;
                target.GetComponent<Renderer>().sharedMaterials = materials;
                count++;
            }
        }
        var platform = GameObject.Find("05_boss_platform");
        if (platform != null && GameObject.Find("BossArena_BlenderPaving") == null)
        {
            var old = platform.GetComponent<Renderer>();
            var bounds = old.bounds;
            var source = Model("Arena");
            var obj = new GameObject("BossArena_BlenderPaving");
            Undo.RegisterCreatedObjectUndo(obj, "Add sculpted paving");
            obj.transform.SetParent(platform.transform.parent, true);
            obj.transform.position = new Vector3(bounds.center.x, bounds.max.y - source.sharedMesh.bounds.max.y, bounds.center.z);
            var mesh = Object.Instantiate(source.sharedMesh);
            mesh.vertices = mesh.vertices.Select(v =>
            {
                float radius = new Vector2(v.x, v.z).magnitude;
                v.y += -.53f + Mathf.SmoothStep(0, .48f, Mathf.InverseLerp(12.5f, 16.2f, radius));
                return v;
            }).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Root + "Arena_Conformed.asset");
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials = Materials(source.GetComponent<Renderer>());
            obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            Undo.RecordObject(platform.GetComponent<MeshCollider>(), "Match collision to new paving");
            platform.GetComponent<MeshCollider>().enabled = false;
            Undo.RecordObject(old, "Replace blurry arena visual");
            old.enabled = false;
        }

        NightmareGripInstaller.Install();
        // Every combat consumer must migrate with the visual/collider replacement.
        // Disabled colliders report zero-sized bounds and collapse monster containment.
        var newFloor = GameObject.Find("BossArena_BlenderPaving").GetComponent<Collider>();
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(behaviour);
            var property = serialized.GetIterator();
            bool changed = false;
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var collider = property.objectReferenceValue as Collider;
                if (collider == null || collider.name != "05_boss_platform") continue;
                property.objectReferenceValue = newFloor;
                changed = true;
            }
            if (changed) serialized.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Replaced scenery: " + count + "; arena and authored grip prefab saved.";
    }
}
