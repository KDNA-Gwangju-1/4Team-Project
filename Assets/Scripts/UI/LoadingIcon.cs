using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 그림 여러 장을 순서대로 갈아 끼워서 움직이는 아이콘을 만든다.
///
/// 로딩 화면의 달처럼 <b>움직임이 그림 안에 이미 그려져 있는</b> 경우에 쓴다.
/// Transform 을 흔드는 방식이 아니라서, 달만 오르내리고 아래 물결은 가만히 있는다.
///
/// 붙이는 곳 : Image 가 있는 오브젝트
/// </summary>
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class LoadingIcon : MonoBehaviour
{
    [Tooltip("순서대로 돌릴 그림들. 위에서부터 차례로 나온다.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("초당 몇 장을 넘길지. 4장짜리면 4 를 넣었을 때 1초에 한 바퀴 돈다.")]
    [SerializeField] private float framesPerSecond = 4f;

    [Tooltip("켜면 끝까지 간 뒤 거꾸로 되돌아온다.\n" +
             "그림에 이미 왕복이 그려져 있으면 꺼 두어야 한다. 켜면 아래쪽에서 두 번 머문다.")]
    [SerializeField] private bool pingPong = false;

    private Image _image;
    private float _timer;
    private int _index;
    private int _step = 1;

    private void Awake()
    {
        _image = GetComponent<Image>();

        if (frames != null && frames.Length > 0 && frames[0] != null)
            _image.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || framesPerSecond <= 0f) return;

        // 로딩 중에는 Time.timeScale 이 0 일 수도 있어서 unscaled 를 쓴다.
        _timer += Time.unscaledDeltaTime;

        float perFrame = 1f / framesPerSecond;

        // 프레임이 심하게 튀어도 한 장씩 제대로 넘어가도록 while 로 돈다.
        while (_timer >= perFrame)
        {
            _timer -= perFrame;
            Advance();
        }
    }

    private void Advance()
    {
        if (pingPong)
        {
            _index += _step;

            if (_index >= frames.Length - 1) { _index = frames.Length - 1; _step = -1; }
            else if (_index <= 0)            { _index = 0;                 _step =  1; }
        }
        else
        {
            _index = (_index + 1) % frames.Length;
        }

        if (frames[_index] != null) _image.sprite = frames[_index];
    }
}
