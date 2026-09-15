using UnityEngine;

/// <summary>
/// 화면 아래에 보이는 1인칭 "손"을 흔들어 주는 스크립트.
///
/// 손 모델은 ViewModel 전용 카메라의 자식으로 붙어 있어야 한다.
/// (그래야 벽에 가까이 붙어도 손이 벽을 뚫고 잘리지 않는다)
///
/// - 마우스를 돌리면 손이 반대쪽으로 살짝 끌려간다(스웨이)
/// - 걸으면 8자를 그리며 흔들린다(보브)
/// </summary>
[DisallowMultipleComponent]
public class ViewModelHands : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워 두면 부모 쪽에서 알아서 찾는다.")]
    [SerializeField] private FirstPersonController player;

    [Header("마우스 스웨이")]
    [Tooltip("마우스를 돌릴 때 손이 끌려가는 정도 (m)")]
    [SerializeField] private float swayAmount = 0.022f;

    [Tooltip("스웨이가 최대로 갈 수 있는 한계 (m)")]
    [SerializeField] private float swayLimit = 0.05f;

    [Tooltip("손이 제자리로 돌아오는 빠르기")]
    [SerializeField] private float swaySmooth = 8f;

    [Header("걷기 흔들림")]
    [SerializeField] private float bobFrequency = 7.5f;
    [SerializeField] private float bobHorizontal = 0.022f;
    [SerializeField] private float bobVertical = 0.016f;

    [Header("기울기")]
    [Tooltip("좌우로 움직일 때 손이 기우는 각도")]
    [SerializeField] private float tiltAmount = 4f;

    // ------------------------------------------------------------
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _bobTimer;

    private void Awake()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;

        if (player == null) player = GetComponentInParent<FirstPersonController>();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        float amount = player.MoveAmount01;

        // ---------- 마우스 스웨이 ----------
        // 마우스를 오른쪽으로 돌리면 손은 왼쪽으로 밀린다.
        float swayX = Mathf.Clamp(-player.LookDeltaX * swayAmount, -swayLimit, swayLimit);
        float swayY = Mathf.Clamp(-player.LookDeltaY * swayAmount, -swayLimit, swayLimit);

        // ---------- 걷기 보브 ----------
        if (amount > 0.05f)
        {
            _bobTimer += Time.deltaTime * bobFrequency * Mathf.Max(amount, 0.2f);
        }
        else
        {
            _bobTimer = Mathf.MoveTowards(_bobTimer, 0f, Time.deltaTime * bobFrequency);
        }

        float bobX = Mathf.Cos(_bobTimer) * bobHorizontal * amount;
        float bobY = Mathf.Sin(_bobTimer * 2f) * bobVertical * amount;

        Vector3 target = _basePosition + new Vector3(swayX + bobX, swayY + bobY, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, swaySmooth * Time.deltaTime);

        // ---------- 기울기 ----------
        // 옆으로 걸을 때 손이 살짝 기울면 훨씬 자연스럽다.
        float strafe = Vector3.Dot(player.PlanarVelocity.normalized, player.transform.right) * amount;
        Quaternion targetRotation = _baseRotation * Quaternion.Euler(0f, 0f, -strafe * tiltAmount);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, swaySmooth * Time.deltaTime);
    }
}
