using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Installs Blender architecture without rebuilding furniture, triggers, UI or patient data.</summary>
public static class HospitalArchitectureInstaller
{
    private const string ModelPath = "Assets/Art/Models/HospitalArchitecture/HospitalArchitecture.fbx";
    private const string MaterialDir = "Assets/Materials/Hospital/Architecture";
    private const string TextureDir = "Assets/Art/Textures/HospitalArchitecture";
    private const string RootName = "HospitalArchitecture";

    [MenuItem("Tools/Hospital/Apply Detailed Architecture")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/HospitalRoom.unity")
            throw new InvalidOperationException("Open HospitalRoom in Edit Mode before applying architecture.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("Export HospitalArchitecture.fbx from Blender first.");

        Directory.CreateDirectory(MaterialDir);
        Directory.CreateDirectory(TextureDir);
        var textures = CreateTextures();
        var materials = CreateMaterials(textures);
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply detailed hospital architecture");
        var previous = GameObject.Find(RootName);
        if (previous != null) Undo.DestroyObjectImmediate(previous);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = RootName;
        Undo.RegisterCreatedObjectUndo(instance, "Install hospital architecture");
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
        {
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && materials.TryGetValue(slots[i].name, out var material)) slots[i] = material;
            renderer.sharedMaterials = slots;
            renderer.gameObject.isStatic = true;
        }

        int replaced = 0;
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string path = HierarchyPath(renderer.transform);
            if (!Replaces(path)) continue;
            Undo.RecordObject(renderer, "Replace blockout surface");
            renderer.enabled = false;
            replaced++;
        }

        // Keep the night outside, but use neutral clinical light indoors.
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (HierarchyPath(light.transform).StartsWith("Corridor/Lighting/"))
            {
                Undo.RecordObject(light, "Balance corridor light");
                light.renderMode = LightRenderMode.ForcePixel;
                light.color = new Color(0.96f, 0.985f, 1f);
                light.intensity = 1.25f;
            }
            if (!HierarchyPath(light.transform).StartsWith("Lighting/")) continue;
            Undo.RecordObject(light, "Balance hospital light");
            if (light.type == LightType.Directional)
            {
                light.intensity = 0.32f;
                light.color = new Color(0.78f, 0.86f, 1f);
                light.shadows = LightShadows.Soft;
            }
            else if (light.name.StartsWith("CeilingLight"))
            {
                light.color = new Color(0.96f, 0.985f, 1f);
                light.intensity = 1.12f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f;
                light.shadowBias = 0.035f;
                light.renderMode = LightRenderMode.ForcePixel;
            }
            else if (light.name.StartsWith("BedLight"))
            {
                light.color = new Color(1f, 0.94f, 0.84f);
                light.intensity = 0.32f;
            }
        }
        var fill = new GameObject("WindowBounce");
        fill.transform.SetParent(instance.transform, false);
        fill.transform.position = new Vector3(-3.75f, 2.2f, -0.3f);
        var fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(0.83f, 0.92f, 1f);
        fillLight.intensity = 0.48f;
        fillLight.range = 7f;
        fillLight.shadows = LightShadows.None;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.43f, 0.45f, 0.46f);
        // Only the far sightline fades; the playable ward remains clear.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.48f, 0.56f, 0.56f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 36f;

        for (int side = -1; side <= 1; side += 2)
        for (int bay = 0; bay < 5; bay++)
        {
            var lamp = new GameObject("ExtensionFill_" + side + "_" + bay);
            lamp.transform.SetParent(instance.transform, false);
            lamp.transform.position = new Vector3(side * (11.5f + bay * 4.8f), 2.55f, -5.325f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.94f, 0.98f, 1f);
            light.intensity = 0.8f;
            light.range = 5.8f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        AddBedNumber(instance.transform, materials["HA_Blue"], "01", -2.26f);
        AddBedNumber(instance.transform, materials["HA_Blue"], "02", -0.776f);
        AddBedNumber(instance.transform, materials["HA_Blue"], "03", 2.2f);
        var numberTemplate = GameObject.Find("Corridor/RoomSigns/Sign_302/Number");
        if (numberTemplate != null)
        for (int direction = -1; direction <= 1; direction += 2)
        for (int bay = 0; bay < 8; bay++)
        for (int wall = 0; wall < 2; wall++)
        {
            var label = UnityEngine.Object.Instantiate(numberTemplate, instance.transform);
            label.name = "ExtensionRoomNumber_" + direction + "_" + bay + "_" + wall;
            label.transform.position = new Vector3(direction * (11.5f + bay * 4.8f), 2.42f,
                wall == 0 ? -6.745f : -3.785f);
            label.transform.rotation = Quaternion.Euler(0f, wall == 0 ? 180f : 0f, 0f);
            label.GetComponent<TextMesh>().text = (310 + (direction > 0 ? 16 : 0) + bay * 2 + wall).ToString();
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[HospitalArchitecture] Installed Blender architecture; replaced " + replaced + " blockout renderers. Furniture, colliders and callbacks retained.");
    }

    private static bool Replaces(string path)
    {
        // Retain collision/trigger objects but remove their opaque end surfaces.
        if (path.StartsWith("Corridor/DarkEnd/")) return true;
        if (path.StartsWith("Room/Window/")) return !path.EndsWith("/Glass");
        if (path.StartsWith("Room/"))
        {
            string name = path.Substring(5);
            if (!name.Contains("/") && (name == "Floor" || name == "Ceiling" || name.StartsWith("Wall_") ||
                name.StartsWith("Wainscot_") || name.StartsWith("Column_"))) return true;
        }
        if (path.StartsWith("Corridor/") && !path.StartsWith("Corridor/DarkEnd/"))
        {
            string name = path.Substring(9);
            if (!name.Contains("/") && (name == "Floor" || name == "Ceiling" || name.StartsWith("Wall_") || name.StartsWith("Wainscot_"))) return true;
        }
        if (path.StartsWith("Props/HeadUnit_") && (path.EndsWith("/Backboard") || path.EndsWith("/Shelf"))) return true;
        if (path.StartsWith("Props/OverbedTable_") && path.EndsWith("/Top")) return true;
        return path.StartsWith("Lighting/CeilingPanel_") || path.StartsWith("Corridor/Lighting/CeilingPanel_");
    }

    private static string HierarchyPath(Transform item)
    {
        string path = item.name;
        for (var parent = item.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }

    private static Dictionary<string, Material> CreateMaterials(Dictionary<string, Texture2D> textures)
    {
        var result = new Dictionary<string, Material>();
        AddMaterial(result, "HA_Porcelain", new Color(0.88f, 0.91f, 0.90f), 0.22f);
        AddMaterial(result, "HA_Sage", new Color(0.55f, 0.68f, 0.64f), 0.32f);
        AddMaterial(result, "HA_Vinyl", Color.white, 0.24f, textures["Vinyl"]);
        AddMaterial(result, "HA_Trim", new Color(0.78f, 0.84f, 0.82f), 0.4f);
        AddMaterial(result, "HA_Aluminium", new Color(0.55f, 0.64f, 0.66f), 0.58f, null, 0.65f);
        AddMaterial(result, "HA_Gasket", new Color(0.12f, 0.18f, 0.19f), 0.15f);
        AddMaterial(result, "HA_Oak", Color.white, 0.32f, textures["Oak"]);
        AddMaterial(result, "HA_Ceiling", Color.white, 0.12f, textures["Ceiling"]);
        AddMaterial(result, "HA_LED", new Color(0.94f, 0.98f, 1f), 0.45f);
        result["HA_LED"].SetColor("_EmissionColor", new Color(0.90f, 0.95f, 1f) * 1.2f);
        result["HA_LED"].globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        result["HA_LED"].EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(result["HA_LED"]);
        AddMaterial(result, "HA_Blue", new Color(0.12f, 0.36f, 0.44f), 0.3f);
        AddMaterial(result, "HA_Oxygen", new Color(0.24f, 0.56f, 0.42f), 0.4f);
        AddMaterial(result, "HA_Amber", new Color(0.83f, 0.61f, 0.20f), 0.4f);
        return result;
    }

    private static void AddMaterial(Dictionary<string, Material> result, string name, Color color,
        float smoothness, Texture2D texture = null, float metallic = 0f)
    {
        string path = MaterialDir + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.name = name;
        mat.color = color;
        mat.mainTexture = texture;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        result[name] = mat;
    }

    private static Dictionary<string, Texture2D> CreateTextures()
    {
        var result = new Dictionary<string, Texture2D>();
        foreach (string name in new[] { "Vinyl", "Oak", "Ceiling" })
        {
            string path = TextureDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                const int size = 512;
                var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
                var pixels = new Color[size * size];
                var random = new System.Random(302);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float noise = (float)random.NextDouble();
                    Color color;
                    if (name == "Oak")
                    {
                        float grain = Mathf.Sin(x * 0.40f + Mathf.Sin(y * Mathf.PI * 2f / size) * 1.8f) * 0.035f;
                        color = new Color(0.68f + grain, 0.57f + grain, 0.42f + grain) * (0.97f + noise * 0.06f);
                    }
                    else if (name == "Vinyl")
                    {
                        float speckle = noise > 0.97f ? 0.11f : (noise < 0.06f ? -0.065f : (noise - 0.5f) * 0.028f);
                        color = new Color(0.69f + speckle, 0.735f + speckle, 0.715f + speckle);
                    }
                    else
                    {
                        float n = noise < 0.025f ? -0.075f : (noise - 0.5f) * 0.015f;
                        color = new Color(0.85f + n, 0.885f + n, 0.875f + n);
                    }
                    pixels[y * size + x] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            result[name] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return result;
    }

    private static void AddBedNumber(Transform parent, Material backing, string value, float x)
    {
        var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        badge.name = "BedNumber_" + value;
        badge.transform.SetParent(parent, false);
        badge.transform.position = new Vector3(x, 2.12f, 3.47f);
        badge.transform.localScale = new Vector3(0.28f, 0.18f, 0.035f);
        badge.GetComponent<Renderer>().sharedMaterial = backing;
        UnityEngine.Object.DestroyImmediate(badge.GetComponent<Collider>());
        var label = new GameObject("Number", typeof(TextMesh));
        label.transform.SetParent(parent, false);
        label.transform.position = new Vector3(x, 2.12f, 3.44f);
        label.transform.rotation = Quaternion.identity;
        var text = label.GetComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.characterSize = 0.032f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.text = value;
        text.color = Color.white;
        label.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Hospital/RoomNumberText.mat");
        label.AddComponent<RoomNumberFontAtlas>();
    }
}
