using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 정중앙에 표시되는 심플한 점 크로스헤어.
/// 어떤 시스템이든 SetHovering(true/false) 하나만 호출하면 강조 상태로 바뀐다 -
/// 지금은 PlayerInteraction(단서 조준)이 쓰고, 나중에 정화총 조준 등 다른 시스템도 그대로 재사용할 수 있다.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [SerializeField] private Image dot;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.98f, 0.92f, 0.72f);
    [SerializeField] private float normalSize = 8f;
    [SerializeField] private float hoverSize = 11f;
    [SerializeField] private float transitionSpeed = 12f;

    private RectTransform rect;
    private bool hovering;

    private void Awake()
    {
        if (dot == null) dot = GetComponent<Image>();
        rect = dot != null ? dot.rectTransform : null;
    }

    public void SetHovering(bool isHovering)
    {
        hovering = isHovering;
    }

    private void Update()
    {
        if (dot == null || rect == null) return;

        Color targetColor = hovering ? hoverColor : normalColor;
        float targetSize = hovering ? hoverSize : normalSize;
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);

        dot.color = Color.Lerp(dot.color, targetColor, t);
        rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, new Vector2(targetSize, targetSize), t);
    }
}
