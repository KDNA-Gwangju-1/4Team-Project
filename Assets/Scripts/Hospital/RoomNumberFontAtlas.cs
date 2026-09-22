using UnityEngine;

/// <summary>Keeps the depth-tested room-number material in sync with the dynamic font atlas.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
public sealed class RoomNumberFontAtlas : MonoBehaviour
{
    private TextMesh textMesh;
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material runtimeMaterial;

    private void OnEnable()
    {
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();
        originalMaterial = meshRenderer.sharedMaterial;
        runtimeMaterial = new Material(originalMaterial);
        meshRenderer.sharedMaterial = runtimeMaterial;
        Font.textureRebuilt += RefreshAtlas;
        RefreshAtlas(textMesh.font);
    }

    private void RefreshAtlas(Font font)
    {
        if (font != null && font == textMesh.font && runtimeMaterial != null)
            runtimeMaterial.mainTexture = font.material.mainTexture;
    }

    private void OnDisable()
    {
        Font.textureRebuilt -= RefreshAtlas;
        if (meshRenderer != null) meshRenderer.sharedMaterial = originalMaterial;
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
