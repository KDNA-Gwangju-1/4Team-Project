using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 가운데 아래쪽에 뜨는 상호작용 안내문.
///
///         서하린
///     [E]  손대기
///
/// PlayerInteractor 가 Show() / Hide() 를 불러 준다.
/// 직접 켜고 끌 일은 없다.
/// </summary>
[DisallowMultipleComponent]
public class InteractionPromptUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CanvasGroup group;

    [Tooltip("윗줄 - 대상 이름")]
    [SerializeField] private Text nameText;

    [Tooltip("아랫줄 - 행동 (손대기 등)")]
    [SerializeField] private Text actionText;

    [Tooltip("키 배지 - E")]
    [SerializeField] private Text keyText;

    [Header("연출")]
    [Tooltip("나타나고 사라지는 빠르기")]
    [SerializeField] private float fadeSpeed = 12f;

    private float _targetAlpha;

    private void Awake()
    {
        // 한글이 깨지지 않도록 폰트를 갈아 끼운다.
        HangulFont.ApplyAll(gameObject);
        ChapterDialogueSkin.StylePrompt(nameText, actionText, keyText);

        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0f;
        CreateCard();

        _targetAlpha = 0f;
    }

    private void Update()
    {
        if (group == null) return;

        group.alpha = Mathf.MoveTowards(group.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
    }

    /// <summary>안내문을 띄운다. displayName 이 비어 있으면 윗줄은 감춘다.</summary>
    public void Show(string displayName, string action)
    {
        if (nameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(displayName);
            nameText.gameObject.SetActive(hasName);
            if (hasName) nameText.text = displayName;
        }

        if (actionText != null) actionText.text = action;
        if (keyText != null && string.IsNullOrEmpty(keyText.text)) keyText.text = "E";

        FitCard();
        _targetAlpha = 1f;
    }

    // ------------------------------------------------------------
    // 배경 카드 - 글자만 떠 있으면 침대·벽 위에서 읽히지 않는다.
    // 다른 HUD 와 같은 카드 스킨을 글자 크기에 맞춰 깐다.
    // ------------------------------------------------------------
    private RectTransform _card;

    private void CreateCard()
    {
        var go = new GameObject("PromptCard", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        _card = (RectTransform)go.transform;
        _card.anchorMin = _card.anchorMax = new Vector2(.5f, .5f);
        var image = go.GetComponent<Image>();
        // 안내 글자는 밝은 색, [E] 배지는 짙은 남색이라 어두운 카드가 어울린다
        ChapterHudStyle.SkinCard(image, false);
        image.raycastTarget = false;
    }

    private void FitCard()
    {
        if (_card == null) return;
        const float padX = 24f, padY = 12f;

        // 아랫줄: [E] 배지 왼쪽 끝 ~ 행동 글자 오른쪽 끝
        float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
        var badge = keyText != null ? keyText.transform.parent as RectTransform : null;
        if (badge != null) Include(badge.anchoredPosition, badge.sizeDelta.x, badge.sizeDelta.y, ref left, ref right, ref bottom, ref top);
        if (actionText != null)
        {
            var rt = actionText.rectTransform;
            float w = Mathf.Min(actionText.preferredWidth, rt.sizeDelta.x);
            float x = rt.anchoredPosition.x;
            switch (actionText.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.MiddleLeft: case TextAnchor.LowerLeft:
                    x = x - rt.sizeDelta.x * .5f + w * .5f; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight:
                    x = x + rt.sizeDelta.x * .5f - w * .5f; break;
            }
            Include(new Vector2(x, rt.anchoredPosition.y), w, rt.sizeDelta.y, ref left, ref right, ref bottom, ref top);
        }
        if (left > right) return;

        // 윗줄 이름은 아랫줄 가운데에 맞춰 세운다
        float centerX = (left + right) * .5f;
        if (nameText != null && nameText.gameObject.activeSelf)
        {
            var rt = nameText.rectTransform;
            rt.anchoredPosition = new Vector2(centerX, rt.anchoredPosition.y);
            float w = Mathf.Min(nameText.preferredWidth, rt.sizeDelta.x);
            Include(new Vector2(centerX, rt.anchoredPosition.y), w, rt.sizeDelta.y * .8f, ref left, ref right, ref bottom, ref top);
        }

        _card.anchoredPosition = new Vector2((left + right) * .5f, (bottom + top) * .5f);
        _card.sizeDelta = new Vector2(right - left + padX * 2f, top - bottom + padY * 2f);
    }

    private static void Include(Vector2 center, float w, float h, ref float left, ref float right, ref float bottom, ref float top)
    {
        left = Mathf.Min(left, center.x - w * .5f);
        right = Mathf.Max(right, center.x + w * .5f);
        bottom = Mathf.Min(bottom, center.y - h * .5f);
        top = Mathf.Max(top, center.y + h * .5f);
    }

    /// <summary>안내문을 감춘다.</summary>
    public void Hide()
    {
        _targetAlpha = 0f;
    }
}
