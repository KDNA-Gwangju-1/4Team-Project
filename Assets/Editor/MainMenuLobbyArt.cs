using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuLobbyArt
{
    public const string ArtPath = "Assets/Art/MainMenu/DetectiveOffice.png";

    public static bool Apply(Image image)
    {
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
}
