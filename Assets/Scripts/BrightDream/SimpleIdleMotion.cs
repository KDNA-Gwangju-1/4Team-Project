using UnityEngine;

/// <summary>
/// 리깅/애니메이션이 없는 정적 모델(예: 보스)에 붙이는 절차적 아이들 모션.
/// 실제 뼈대 기반 Attack/Death 모션이 아니라, 위아래로 살짝 떠오르고 좌우로 흔들리는
/// 최소한의 "살아있는 느낌"만 주는 임시 처리다. 나중에 리깅된 보스 에셋이 들어오면
/// 이 컴포넌트는 떼어내면 된다.
/// </summary>
public class SimpleIdleMotion : MonoBehaviour
{
    [Header("위아래 흔들림")]
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 1.0f;

    [Header("좌우 흔들림 (Yaw)")]
    [SerializeField] private float swayAmplitudeDeg = 3f;
    [SerializeField] private float swaySpeed = 0.6f;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmplitudeDeg;

        transform.localPosition = basePosition + Vector3.up * bob;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, sway, 0f);
    }
}
