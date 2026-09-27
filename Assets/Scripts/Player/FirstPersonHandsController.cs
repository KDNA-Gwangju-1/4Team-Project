using UnityEngine;

/// <summary>
/// 1인칭 시점에서 화면 아래에 보이는 팔/손을 흔들어 주는 스크립트.
///
/// 붙이는 곳 : HandsRoot (카메라의 자식). LeftArm / RightArm 은 HandsRoot 의 자식으로 둔다.
///
///     Player
///     └─ Main Camera
///        └─ HandsRoot   ← 이 스크립트를 여기에 붙인다
///           ├─ LeftArm
///           └─ RightArm
///
/// 하는 일
///  - 걸을 때만 아주 약하게 8자 흔들림(bob)
///  - 마우스를 돌리면 손이 반 박자 늦게 따라옴(sway)
///  - 멈추면 기본 위치로 부드럽게 복귀
///
/// 플레이어 이동 코드나 카메라 코드는 건드리지 않는다.
/// FirstPersonController 가 있으면 그 값을 읽어 쓰고,
/// 없으면 플레이어의 실제 이동량과 마우스 입력으로 알아서 계산한다.
/// </summary>
[DisallowMultipleComponent]
public class FirstPersonHandsController : MonoBehaviour
{
    // ============================================================
    // 참조
    // ============================================================
    [Header("참조 (비워 두면 알아서 찾는다)")]
    [Tooltip("이동량을 읽어 올 플레이어 컨트롤러. 없어도 동작한다.")]
    [SerializeField] private FirstPersonController playerController;

    [Tooltip("playerController 가 없을 때 이동을 감지할 기준 오브젝트. 보통 Player 본체.")]
    [SerializeField] private Transform playerBody;

    // ============================================================
    // 걷기 흔들림
    // ============================================================
    [Header("걷기 흔들림 (Bob)")]
    [Tooltip("흔들리는 빠르기. 걸음 속도에 맞춘다.")]
    [SerializeField] private float walkBobSpeed = 5.0f;
    [Tooltip("좌우로 흔들리는 폭 (m). 0.01 만 넘어도 꽤 크게 느껴진다.")]
    [SerializeField] private float walkBobAmountX = 0.0022f;
    [Tooltip("위아래로 흔들리는 폭 (m). 보통 X 의 절반 정도가 자연스럽다.")]
    [SerializeField] private float walkBobAmountY = 0.0014f;
    // ============================================================
    // 마우스 스웨이
    // ============================================================
    [Header("마우스 스웨이 (Sway)")]
    [Tooltip("마우스를 돌릴 때 손이 끌려가는 정도.")]
    [SerializeField] private float swayAmount = 0.009f;
    [Tooltip("스웨이가 아무리 커도 이 값을 넘지 않는다 (m). 손이 화면 밖으로 튀는 걸 막아 준다.")]
    [SerializeField] private float swayMaxOffset = 0.030f;

    [Tooltip("스웨이가 따라오는 빠르기. 작을수록 더 느긋하게 따라온다.")]
    [SerializeField] private float swaySmooth = 9f;
    [Tooltip("마우스를 돌릴 때 손이 살짝 기우는 각도. 0 이면 기울지 않는다.")]
    [SerializeField] private float swayTiltAngle = 0.8f;
    // ============================================================
    // 복귀
    // ============================================================
    [Header("복귀")]
    [Tooltip("멈췄을 때 기본 위치로 돌아오는 빠르기. 작을수록 천천히 돌아온다.")]
    [SerializeField] private float returnSmooth = 5f;
    // ============================================================
    // 걸을 때만 팔을 앞으로 내미는 부분
    // ============================================================
    [Header("걸을 때만 앞으로 나오기")]
    [Tooltip("멈춰 있을 때는 화면 밖에 숨어 있다가, 걸으면 이만큼 올라오고 앞으로 나온다. (m)")]
    [SerializeField] private Vector3 walkOffset = new Vector3(0f, 0.150f, 0.030f);
    [Tooltip("걷기 시작할 때 팔이 올라오는 빠르기. 작을수록 느깋하게 나온다.")]
    [SerializeField] private float walkOffsetSmooth = 6f;
    // ============================================================
    // 팔 번갈아 흔들기
    // ============================================================
    [Header("팔 번갈아 흔들기 (비워 두면 안 흔들림)")]
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;

    [Tooltip("왼팔·오른팔이 반대로 앞뒤로 흔들리는 각도. 0 이면 안 흔든다.")]
    [SerializeField] private float walkSwingAngle = 4f;

    [Tooltip("한쪽 팔이 앞으로 나갈 때 반대쪽은 뒤로 빠진다. 그 앞뒤 거리 (m)")]
    [SerializeField] private float armAlternateDistance = 0.070f;
    [Tooltip("앞으로 나간 팔이 살짝 더 올라오는 높이 (m). 있어야 번갈아 걷는 느낌이 산다.")]
    [SerializeField] private float armAlternateLift = 0.028f;

    [Tooltip("켜면 한 팔씩 차례로 나왔다 들어간다. \n" +
             "끄면 시소처럼 한쪽이 나올 때 반대쪽이 동시에 뒤로 빠진다.")]
    [SerializeField] private bool oneArmAtATime = true;
    // ============================================================
    // 이동 판정
    // ============================================================
    [Header("이동 판정")]
    [Tooltip("이 속도(m/s)를 넘어야 걷는 것으로 친다.")]
    [SerializeField] private float moveThreshold = 0.15f;
    [Tooltip("이 속도(m/s)면 팔이 완전히 나온다. 보통 걷기 속도를 넣는다.")]
    [SerializeField] private float maxSpeedForBob = 1.9f;
    // ------------------------------------------------------------
    // 내부 상태
    // ------------------------------------------------------------
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private Vector3 _lastPlayerPosition;
    private Vector2 _sway;
    private float _bobTimer;
    private float _moveAmount;
    private float _walkBlend;
    private Quaternion _leftArmBase = Quaternion.identity;
    private Quaternion _rightArmBase = Quaternion.identity;
    private Vector3 _leftArmBasePosition;
    private Vector3 _rightArmBasePosition;
    /// <summary>0(정지) ~ 1(최대 속도). 다른 스크립트에서 참고할 수 있게 열어 둔다.</summary>
    public float MoveAmount01 => _moveAmount;

    // ============================================================
    // 유니티 이벤트
    // ============================================================

    private void Awake()
    {
        // 씨에 배치해 둔 위치를 '기본 위치'로 기억한다.
        // 이 자리가 멈춰 있을 때의 자리(= 화면 밖)이 된다.
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;

        if (playerController == null) playerController = GetComponentInParent<FirstPersonController>();

        if (playerBody == null)
        {
            playerBody = playerController != null
                ? playerController.transform
                : (transform.root != null ? transform.root : transform);
        }

        _lastPlayerPosition = playerBody.position;

        // 팔을 안 넣었으면 이름으로 찾아 본다.
        if (leftArm == null)  leftArm  = transform.Find("LeftArm");
        if (rightArm == null) rightArm = transform.Find("RightArm");
        if (leftArm != null)
        {
            _leftArmBase = leftArm.localRotation;
            _leftArmBasePosition = leftArm.localPosition;
        }
        if (rightArm != null)
        {
            _rightArmBase = rightArm.localRotation;
            _rightArmBasePosition = rightArm.localPosition;
        }
    }

    private void LateUpdate()
    {
        float delta = Time.deltaTime;
        if (delta <= 0f) return;

        _moveAmount = ReadMoveAmount01(delta);
        bool moving = _moveAmount > 0.01f;

        // 걷기 정도를 부드럽게 따라간다.
        // 나올 때는 walkOffsetSmooth, 들어갈 때는 returnSmooth 속도를 쓴다.
        float blendSpeed = _moveAmount > _walkBlend ? walkOffsetSmooth : returnSmooth;
        _walkBlend = Mathf.Lerp(_walkBlend, _moveAmount, blendSpeed * delta);

        Vector3 bob = UpdateBob(delta, moving);
        Vector2 sway = UpdateSway(delta);

        // 여기서는 바로 대입한다.
        // _walkBlend 와 _sway 가 이미 부드럽게 움직이고 있어서
        // 여기서 또 Lerp 를 걸면 부드럽게 처리가 두 번 겹쳐
        // 팔이 물컹거리며 늦게 따라온다.
        transform.localPosition = _basePosition
                                + walkOffset * _walkBlend
                                + bob
                                + new Vector3(sway.x, sway.y, 0f);

        // 아주 약한 기울기
        if (swayTiltAngle > 0f)
        {
            float tilt = Mathf.Clamp(sway.x / Mathf.Max(swayMaxOffset, 0.0001f), -1f, 1f);
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, -tilt * swayTiltAngle);
        }
        else
        {
            transform.localRotation = _baseRotation;
        }

        UpdateArmSwing();
    }

    /// <summary>왼팔과 오른팔을 반대 방향으로 앞뒤 흔들어 걷는 느낌을 낸다.</summary>
    /// <summary>
    /// 왼팔과 오른팔을 반대로 움직여 번갈아 걷는 느낌을 낸다.
    ///
    /// 한쪽이 앞으로 나가면 반대쪽은 뒤로 빠지고,
    /// 앞으로 나간 쪽이 살짝 더 올라와 화면에 더 많이 보인다.
    /// 그래야 두 팔이 동시에 나오지 않고 왜다갔다 하는 것처럼 보인다.
    /// </summary>
    /// <summary>
    /// 왼팔과 오른팔을 번갈아 움직여 걷는 느낌을 낸다.
    ///
    /// oneArmAtATime 이 켜져 있으면
    ///   왼팔이 나왔다 들어가고 → 오른팔이 나왔다 들어가고 → 반복.
    ///   (사인파의 위쪽 반만 써서, 한 번에 한 팔만 앞으로 나온다)
    ///
    /// 꺼져 있으면
    ///   시소처럼 한쪽이 나갈 때 반대쪽은 동시에 뒤로 빠진다.
    /// </summary>
    private void UpdateArmSwing()
    {
        if (leftArm == null && rightArm == null) return;

        float phase = Mathf.Sin(_bobTimer);

        float leftAmount, rightAmount;

        if (oneArmAtATime)
        {
            // 사인파를 반씩 잘라 나눠 갖는다.
            // 앞 반주기엔 왼팔만, 뒤 반주기엔 오른팔만 움직인다.
            // 쉬는 쪽은 정확히 제자리(0)에 멈춰 있다.
            leftAmount  = Mathf.Max(phase, 0f);
            rightAmount = Mathf.Max(-phase, 0f);
        }
        else
        {
            leftAmount  = phase;
            rightAmount = -phase;
        }

        leftAmount  *= _walkBlend;
        rightAmount *= _walkBlend;

        // 두 팔 모두 '앞으로' 가는 방향은 같다. 나오는 타이밍만 다르다.
        Vector3 step = new Vector3(0f, armAlternateLift, armAlternateDistance);

        if (leftArm != null)
        {
            leftArm.localPosition = _leftArmBasePosition + step * leftAmount;
            leftArm.localRotation = Quaternion.Euler(-walkSwingAngle * leftAmount, 0f, 0f) * _leftArmBase;
        }

        if (rightArm != null)
        {
            rightArm.localPosition = _rightArmBasePosition + step * rightAmount;
            rightArm.localRotation = Quaternion.Euler(-walkSwingAngle * rightAmount, 0f, 0f) * _rightArmBase;
        }
    }

    // ============================================================
    // 걷기 흔들림
    // ============================================================

    private Vector3 UpdateBob(float delta, bool moving)
    {
        if (moving)
        {
            // 느리게 걸으면 느리게, 빠르게 걸으면 빠르게 흔들린다.
            _bobTimer += delta * walkBobSpeed * Mathf.Max(_moveAmount, 0.3f);
        }
        else
        {
            // 멈추면 흔들림 위상을 0 으로 되돌려 다음 걸음이 깔끔하게 시작되게 한다.
            _bobTimer = Mathf.MoveTowards(_bobTimer, 0f, delta * walkBobSpeed);
            return Vector3.zero;
        }

        // 좌우는 한 걸음에 한 번, 위아래는 두 번 흔들려야 걷는 느낌이 난다.
        float x = Mathf.Cos(_bobTimer) * walkBobAmountX * _moveAmount;
        float y = Mathf.Sin(_bobTimer * 2f) * walkBobAmountY * _moveAmount;

        return new Vector3(x, y, 0f);
    }

    // ============================================================
    // 마우스 스웨이
    // ============================================================

    private Vector2 UpdateSway(float delta)
    {
        Vector2 look = ReadLookDelta();

        // 마우스를 오른쪽으로 돌리면 손은 왼쪽으로 밀린다. (관성처럼 보이게)
        float targetX = Mathf.Clamp(-look.x * swayAmount, -swayMaxOffset, swayMaxOffset);
        float targetY = Mathf.Clamp(-look.y * swayAmount, -swayMaxOffset, swayMaxOffset);

        _sway = Vector2.Lerp(_sway, new Vector2(targetX, targetY), swaySmooth * delta);
        return _sway;
    }

    // ============================================================
    // 입력 읽기
    // ============================================================

    /// <summary>0(정지) ~ 1(최대 속도)</summary>
    /// <summary>0(정지) ~ 1(이 속도면 충분히 걷는 것)</summary>
    private float ReadMoveAmount01(float delta)
    {
        float speed;

        if (playerController != null)
        {
            // 컨트롤러가 알려 주는 실제 속도를 쓴다.
            // MoveAmount01 은 '달리기 속도 기준'이라 그대로 쓰면
            // 보통 걸음에서 팔이 절반밖에 안 올라온다.
            speed = playerController.PlanarVelocity.magnitude;
        }
        else if (playerBody != null)
        {
            // 컨트롤러가 없으면 실제로 움직인 거리로 속도를 잴다.
            Vector3 moved = playerBody.position - _lastPlayerPosition;
            _lastPlayerPosition = playerBody.position;

            moved.y = 0f;   // 위아래 움직임은 걷기로 치지 않는다
            speed = moved.magnitude / delta;
        }
        else
        {
            return 0f;
        }

        if (speed < moveThreshold) return 0f;
        return Mathf.Clamp01(speed / Mathf.Max(maxSpeedForBob, 0.01f));
    }

    /// <summary>이번 프레임 마우스 이동량</summary>
    private Vector2 ReadLookDelta()
    {
        if (playerController != null)
            return new Vector2(playerController.LookDeltaX, playerController.LookDeltaY);

        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 손 위치를 직접 옮긴 뒤 이 메뉴를 누르면 그 자리를 기본 위치로 삼는다.
    /// (컴포넌트 우클릭 > 지금 위치를 기본 위치로 저장)
    /// </summary>
    [ContextMenu("지금 위치를 기본 위치로 저장")]
    private void CaptureBasePose()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
        Debug.Log($"[FirstPersonHandsController] 기본 위치를 {_basePosition} / {_baseRotation.eulerAngles} 로 저장했습니다.", this);
    }
#endif
}
