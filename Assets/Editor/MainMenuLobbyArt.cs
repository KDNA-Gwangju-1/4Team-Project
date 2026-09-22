using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuLobbyArt
{
    public const string ArtPath = "Assets/Art/MainMenu/DetectiveOffice.png";
    public const string ReDreamFolder = "Assets/Art/MainMenu/ReDream";

    public static bool HasReDreamArtwork(Image image)
    {
        return image != null && image.sprite != null &&
               AssetDatabase.GetAssetPath(image.sprite).StartsWith(ReDreamFolder + "/");
    }

    public static bool Apply(Image image)
    {
        if (ApplyReDream(image)) return true;
        var importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
        if (importer == null) return false;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Point;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (sprite == null) return false;
        var oldLoop = image.GetComponent<OpeningBackgroundPlayer>();
        if (oldLoop != null) Object.DestroyImmediate(oldLoop);
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        var fit = image.GetComponent<AspectRatioFitter>();
        if (fit == null) fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = sprite.rect.width / sprite.rect.height;
        return true;
    }

    private static bool ApplyReDream(Image image)
    {
        var frames = new Sprite[5];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = ReDreamFolder + "/ReDream_" + (i + 1).ToString("00") + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null) return false;
        }

        image.sprite = frames[0];
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        var fit = image.GetComponent<AspectRatioFitter>();
        if (fit == null) fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        // Keep one fixed rectangle despite the two-pixel difference in frame 1.
        fit.aspectRatio = 1672f / 941f;

        var transitionTransform = image.transform.Find("ReDreamTransition");
        var transition = transitionTransform != null ? transitionTransform.GetComponent<Image>() : null;
        if (transition == null)
        {
            var go = new GameObject("ReDreamTransition", typeof(RectTransform), typeof(Image));
            go.layer = image.gameObject.layer;
            go.transform.SetParent(image.transform, false);
            transition = go.GetComponent<Image>();
        }
        var rect = transition.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        transition.sprite = frames[1];
        transition.type = Image.Type.Simple;
        transition.preserveAspect = false;
        transition.color = new Color(1f, 1f, 1f, 0f);
        transition.raycastTarget = false;

        var player = image.GetComponent<OpeningBackgroundPlayer>();
        if (player == null) player = image.gameObject.AddComponent<OpeningBackgroundPlayer>();
        var so = new SerializedObject(player);
        so.FindProperty("targetImage").objectReferenceValue = image;
        so.FindProperty("transitionImage").objectReferenceValue = transition;
        var array = so.FindProperty("frames");
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.FindProperty("framesPerSecond").floatValue = 1.6f;
        so.FindProperty("loop").boolValue = true;
        so.FindProperty("pingPong").boolValue = true;
        so.FindProperty("useUnscaledTime").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        player.enabled = true;
        return true;
    }

    [MenuItem("Tools/Main Menu/Apply ReDream Animation")]
    public static void ApplyToCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/MainMenu.unity")
            throw new System.InvalidOperationException("Open MainMenu in Edit Mode first.");
        var canvas = GameObject.Find("Canvas");
        var image = canvas != null ? canvas.transform.Find("Background")?.GetComponent<Image>() : null;
        if (image == null) throw new System.InvalidOperationException("MainMenu background was not found.");
        Undo.RegisterFullObjectHierarchyUndo(canvas, "Apply ReDream Animation");
        if (!ApplyReDream(image)) throw new System.InvalidOperationException("All five ReDream frames are required.");
        var title = canvas.transform.Find("MainMenuPanel/GameTitleBox");
        if (title != null) title.gameObject.SetActive(false);
        var scrim = canvas.transform.Find("Scrim")?.GetComponent<Image>();
        if (scrim != null) scrim.color = new Color(0f, 0f, 0f, .06f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ReDream] Applied 5 frames in numeric order, smooth ping-pong loop (5 seconds).");
    }
}
