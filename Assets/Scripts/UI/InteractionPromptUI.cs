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

        _targetAlpha = 1f;
    }

    /// <summary>안내문을 감춘다.</summary>
    public void Hide()
    {
        _targetAlpha = 0f;
    }
}
