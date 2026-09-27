using System.Linq;
using UnityEngine;
using UnityEditor;
using BrightDream.Combat;

public static class NightmareGripInstaller
{
    const string Root = "Assets/BrightDream/Art/NightmareGrip/";
    const string Prefab = "Assets/BrightDream/VFX/Crack/BossHand.prefab";

    [MenuItem("Tools/Bright Dream/Install Rigged Nightmare Hand")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before authoring.");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "NightmareHandRig.fbx");
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.SaveAndReimport();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "NightmareHandRig.fbx");
        var clip = AssetDatabase.LoadAllAssetsAtPath(Root + "NightmareHandRig.fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        var root = new GameObject("BossHand");
        root.transform.localScale = Vector3.one * 1.9f;
        var arm = new GameObject("Arm").transform;
        arm.SetParent(root.transform, false);
        var armSource = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Forearm.fbx").GetComponentInChildren<MeshFilter>();
        arm.gameObject.AddComponent<MeshFilter>().sharedMesh = armSource.sharedMesh;
        arm.gameObject.AddComponent<MeshRenderer>().sharedMaterials = Materials(armSource.GetComponent<Renderer>());
        var hand = new GameObject("Hand").transform;
        hand.SetParent(root.transform, false);
        hand.localPosition = Vector3.forward * 2.1f;
        var model = Object.Instantiate(source, hand);
        model.name = source.name;
        foreach (var animation in model.GetComponentsInChildren<Animation>()) Object.DestroyImmediate(animation);
        foreach (var animator in model.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = Materials(renderer);
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null) skinned.updateWhenOffscreen = true;
        }
        var anchor = new GameObject("GripAnchor").transform;
        anchor.SetParent(hand, false);
        anchor.localPosition = new Vector3(0, -.58f, 1.28f);
        var grip = root.AddComponent<BossHandGrip>();
        var data = new SerializedObject(grip);
        data.FindProperty("skeletonRoot").objectReferenceValue = model;
        data.FindProperty("closeClip").objectReferenceValue = clip;
        data.ApplyModifiedPropertiesWithoutUndo();
        grip.SetGrip(0f);
        PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
    }

    static Material[] Materials(Renderer renderer)
    {
        return renderer.sharedMaterials.Select(original =>
        {
            string path = Root + original.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("BrightDream/NightmareCloth"));
                material.color = original.color;
                material.SetFloat("_Glossiness", original.name.Contains("Claw") ? .48f : .22f);
                material.SetFloat("_Grain", original.name.Contains("Claw") ? .03f : .13f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }).ToArray();
    }
}
