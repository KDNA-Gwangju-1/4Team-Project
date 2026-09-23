using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared spacing and chapter palette; resource values remain owned by gameplay.</summary>
public static class ChapterHudStyle
{
    public static void Frame(RectTransform root, bool bright, float width, float height, string caption)
    {
        if (root == null) return;
        root.sizeDelta = new Vector2(width, height);
        Transform found = root.Find("HudCard");
        Image panel = found != null ? found.GetComponent<Image>() :
            new GameObject("HudCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        if (found == null)
        {
            ChapterDialogueSkin.Place(panel.rectTransform, root, 0, 0, 1, 1);
            panel.transform.SetAsFirstSibling(); panel.raycastTarget = false;
            var edge = panel.gameObject.AddComponent<Outline>();
            edge.effectDistance = new Vector2(1f,-1f);
            edge.effectColor = bright ? new Color(.36f,.69f,.78f,.85f) : new Color(.43f,.32f,.65f,.9f);
        }
        panel.color = bright ? new Color(1f,.97f,.88f,.92f) : new Color(.035f,.025f,.075f,.9f);
        Text title;
        var existing = root.Find("HudCaption");
        if (existing == null)
        {
            title = new GameObject("HudCaption",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(root,false);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0,1);
            title.rectTransform.pivot = new Vector2(0,1);
            title.rectTransform.anchoredPosition = new Vector2(12,-7);
            title.rectTransform.sizeDelta = new Vector2(width-24,22);
            title.font = HangulFont.GetEmphasis(); title.fontSize = 17;
            title.alignment = TextAnchor.MiddleLeft; title.raycastTarget = false;
        }
        else title = existing.GetComponent<Text>();
        title.text = caption;
        title.color = bright ? new Color(.22f,.34f,.40f) : new Color(.77f,.74f,.9f);
    }

    public static void TopLeft(RectTransform root, float y)
    {
        root.anchorMin = root.anchorMax = new Vector2(0,1);
        root.pivot = new Vector2(0,1);
        root.anchoredPosition = new Vector2(24,-y);
    }
}
