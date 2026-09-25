using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 시작 시 조작 안내 문구를 띄우고, Space를 누르면 닫는다.
/// 떠 있는 동안은 Blocking이 true가 되어 이동/상호작용/발사 스크립트가 입력을 쉰다.
/// </summary>
public class BrightDreamControlGuide : MonoBehaviour
{
    [SerializeField] private Text guideText;

    [TextArea(2, 6)]
    [SerializeField] private string message =
        "WASD 이동 / Shift 달리기 / Space 점프\n마우스로 시점 전환\nE 상호작용 / 좌클릭 발사\n\n스페이스바를 눌러 시작";

    /// <summary>안내 문구가 떠 있는 동안 true. 플레이어 조작 스크립트가 이 값을 보고 입력을 쉰다.</summary>
    public static bool Blocking { get; private set; }

    private void Awake()
    {
        Blocking = true;
        if (guideText != null)
        {
            ChapterNoticeStyle.Apply(guideText, ChapterDialogueSkin.Theme.BrightDream, 820f, 280f, 30);
            guideText.text = message;
            guideText.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        // 플레이 모드를 껐다 켜도 true 로 남지 않게.
        Blocking = false;
    }

    private void Update()
    {
        if (!Blocking) return;
        if (Input.GetKeyDown(KeyCode.Space)) Close();
    }

    private void Close()
    {
        Blocking = false;
        if (guideText != null) guideText.gameObject.SetActive(false);
    }
}
