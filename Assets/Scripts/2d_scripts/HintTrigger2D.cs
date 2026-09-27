using UnityEngine;

// 플레이어가 트리거에 들어오면 화면 가운데에 안내 한 줄을 띄운다.
// 보스전의 "촉수가 약점인 듯 하다!"와 같은 ScreenHint2D를 쓰므로 생김새가 통일된다.
[RequireComponent(typeof(Collider2D))]
public class HintTrigger2D : MonoBehaviour
{
    [TextArea] public string message = "빛을 비추어 길을 찾아라";
    public float duration = 4.5f;
    [Tooltip("끄면 들어올 때마다 다시 띄운다.")]
    public bool showOnce = true;

    private ScreenHint2D hint;
    private bool shown;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        hint = GetComponent<ScreenHint2D>();
        if (hint == null) hint = gameObject.AddComponent<ScreenHint2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (showOnce && shown) return;
        if (other.GetComponent<PlayerMovement2D>() == null) return;

        shown = true;
        hint.Show(message, duration);
    }
}
