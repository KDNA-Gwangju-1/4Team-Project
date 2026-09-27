using UnityEngine;
using UnityEngine.UI;

/// <summary>Refit only when layout or speaker changes, including delayed aspect-ratio layout.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Text))]
public sealed class ChapterSpeakerNameFit : MonoBehaviour
{
    private Text label;
    private string previousText;
    private float previousWidth = -1f;
    private void Awake() { label = GetComponent<Text>(); }
    private void OnEnable() { previousWidth = -1f; }
    private void LateUpdate()
    {
        if (label == null) label = GetComponent<Text>();
        float width = label.rectTransform.rect.width;
        if (width <= 1f || (Mathf.Approximately(width,previousWidth) && label.text == previousText)) return;
        ChapterDialogueSkin.FitSpeakerName(label);
        previousText = label.text; previousWidth = width;
    }
}
