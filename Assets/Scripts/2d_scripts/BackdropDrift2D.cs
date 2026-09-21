using UnityEngine;

// 카메라 자식으로 붙은 배경 레이어를 천천히 떠다니게 한다.
// FloatBob2D는 월드 좌표를 잡아두기 때문에 카메라 자식에 붙이면 카메라를 따라오지 못한다.
// 세로로만 움직이는 이유는 벽지가 화면 가로폭을 겨우 덮는 크기여서, 가로로 밀면 가장자리가 드러나기 때문.
public class BackdropDrift2D : MonoBehaviour
{
    public float amplitude = 0.9f;
    public float period = 7f;
    [Tooltip("여러 레이어를 서로 어긋나게 띄우고 싶을 때 0~1로 다르게 준다.")]
    public float phase = 0f;

    private Vector3 restLocalPosition;

    void OnEnable()
    {
        restLocalPosition = transform.localPosition;
    }

    void OnDisable()
    {
        transform.localPosition = restLocalPosition;
    }

    void LateUpdate()
    {
        if (period <= 0.001f) return;

        float offset = Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) * amplitude;
        transform.localPosition = restLocalPosition + new Vector3(0f, offset, 0f);
    }
}
