using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오프닝 배경의 스프라이트 시퀀스를 재생한다.
///
/// Unity 는 애니메이션 GIF 를 그대로 재생하지 못하기 때문에,
/// GIF 를 프레임 단위 PNG 로 쪼갠 뒤 이 스크립트가 순서대로 갈아 끼운다.
///
/// frames 배열과 targetImage 는 Inspector 에서 연결되어 있다.
/// </summary>
[DisallowMultipleComponent]
public class OpeningBackgroundPlayer : MonoBehaviour
{
    [Header("재생 대상")]
    [Tooltip("프레임을 갈아 끼울 UI Image")]
    [SerializeField] private Image targetImage;

    [Tooltip("순서대로 재생할 프레임들 (opening_000 ~ )")]
    [SerializeField] private Sprite[] frames;

    [Header("재생 설정")]
    [Tooltip("초당 프레임 수. 원본 GIF 는 프레임당 120ms 라 약 8.33")]
    [SerializeField] private float framesPerSecond = 8.3333f;

    [Tooltip("끝까지 재생하면 처음으로 돌아갈지")]
    [SerializeField] private bool loop = true;

    // 지금 보여 주고 있는 프레임 번호
    private int currentFrame;

    // 다음 프레임까지 남은 시간(초)
    private float timer;

    private void Awake()
    {
        // 프레임이 없으면 아무것도 하지 않는다. (씬이 깨지지 않도록)
        if (targetImage == null || frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[OpeningBackgroundPlayer] 재생할 프레임이나 대상 Image 가 없습니다.");
            enabled = false;
            return;
        }

        ShowFrame(0);
    }

    private void Update()
    {
        // 1초에 framesPerSecond 장을 넘기려면 한 장당 이만큼 기다리면 된다.
        float secondsPerFrame = 1f / Mathf.Max(framesPerSecond, 0.01f);

        timer += Time.deltaTime;

        // 프레임이 밀렸을 때를 대비해 while 로 따라잡는다.
        while (timer >= secondsPerFrame)
        {
            timer -= secondsPerFrame;

            int next = currentFrame + 1;

            if (next >= frames.Length)
            {
                if (!loop)
                {
                    enabled = false;   // 마지막 장에서 멈춘다
                    return;
                }
                next = 0;
            }

            ShowFrame(next);
        }
    }

    private void ShowFrame(int index)
    {
        currentFrame = index;
        targetImage.sprite = frames[index];
    }
}
