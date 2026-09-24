using UnityEngine;

/// <summary>
/// 1인칭 플레이어 이동 + 시점 회전.
///
/// - WASD / 방향키 : 이동
/// - Shift         : 빠르게 걷기
/// - 마우스        : 시점 회전 (감도는 옵션에서 정한 GameSettings.MouseSensitivity 를 따른다)
/// - ESC           : 마우스 커서 잠금 해제 / 다시 잠금
///
/// 카메라는 이 오브젝트의 자식인 cameraPivot 에 붙어 있어야 한다.
/// (몸통은 좌우로만 돌고, 위아래는 cameraPivot 만 돈다)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class FirstPersonController : MonoBehaviour
{
    [Header("이동")]
    [Tooltip("평소 걷는 속도 (m/s). 병실이라 천천히 걷는다.")]
    [SerializeField] private float walkSpeed = 1.9f;

    [Tooltip("Shift 를 눌렀을 때 속도 (m/s)")]
    [SerializeField] private float runSpeed = 3.4f;

    [Tooltip("속도가 바뀔 때 부드럽게 따라가는 정도. 클수록 즉각적이다.")]
    [SerializeField] private float acceleration = 12f;

    [SerializeField] private float gravity = -14f;

    [Header("시점")]
    [Tooltip("카메라가 달려 있는 자식 오브젝트 (플레이어의 '머리')")]
    [SerializeField] private Transform cameraPivot;

    [Tooltip("기본 감도. 여기에 GameSettings.MouseSensitivity 가 곱해진다.")]
    [SerializeField] private float lookSensitivity = 2.0f;

    [Tooltip("위아래로 볼 수 있는 한계 각도")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("머리 흔들림")]
    [SerializeField] private bool useHeadBob = true;
    [Tooltip("걸을 때 머리가 흔들리는 빠르기")]
    [SerializeField] private float bobFrequency = 7.5f;
    [Tooltip("걸을 때 머리가 흔들리는 폭 (m)")]
    [SerializeField] private float bobAmplitude = 0.035f;

    [Header("커서")]
    [SerializeField] private bool lockCursorOnStart = true;

    // ------------------------------------------------------------
    // 내부 상태
    // ------------------------------------------------------------
    private CharacterController _controller;
    private Vector3 _planarVelocity;      // 지면 위 이동 속도 (y 제외)
    private float _verticalVelocity;      // 중력으로 생기는 아래 방향 속도
    private float _pitch;                 // 위아래 시점 각도
    private float _bobTimer;
    private Vector3 _cameraBasePosition;  // 흔들림을 적용하기 전 카메라 제자리
    private bool _cursorLocked;

    // ------------------------------------------------------------
    // 다른 스크립트(손 흔들림 등)에서 읽어 가는 값
    // ------------------------------------------------------------

    /// <summary>지면 위 이동 속도 (y 제외)</summary>
    public Vector3 PlanarVelocity => _planarVelocity;

    /// <summary>0(정지) ~ 1(전력 이동). 손/머리 흔들림 세기를 정할 때 쓴다.</summary>
    public float MoveAmount01 => Mathf.Clamp01(_planarVelocity.magnitude / Mathf.Max(runSpeed, 0.01f));

    /// <summary>걷고 있으면 true</summary>
    public bool IsMoving => _planarVelocity.sqrMagnitude > 0.01f;

    /// <summary>이번 프레임 마우스 좌우 입력 (손 스웨이에 쓴다)</summary>
    public float LookDeltaX { get; private set; }

    /// <summary>이번 프레임 마우스 상하 입력 (손 스웨이에 쓴다)</summary>
    public float LookDeltaY { get; private set; }

    // ============================================================
    // 유니티 이벤트
    // ============================================================

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            Debug.LogError("[FirstPersonController] Camera Pivot 이 비어 있습니다. 카메라가 달린 자식을 넣어 주세요.", this);
            enabled = false;
            return;
        }

        _cameraBasePosition = cameraPivot.localPosition;
    }

    private void Start()
    {
        if (lockCursorOnStart) SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleLook();
        HandleMove();
        HandleHeadBob();
    }

    // ============================================================
    // 시점
    // ============================================================

    private void HandleLook()
    {
        // 커서가 풀려 있을 때(메뉴 등)나 대사·안내창이 떠 있을 때는 시점을 돌리지 않는다.
        if (!_cursorLocked || InputBlocked)
        {
            LookDeltaX = 0f;
            LookDeltaY = 0f;
            return;
        }

        // 옵션에서 정한 감도를 그대로 반영한다.
        float sensitivity = lookSensitivity * GameSettings.MouseSensitivity;

        // Mouse X/Y 는 이미 "이번 프레임에 움직인 양"이라 deltaTime 을 곱하지 않는다.
        LookDeltaX = Input.GetAxis("Mouse X") * sensitivity;
        LookDeltaY = Input.GetAxis("Mouse Y") * sensitivity;

        // 좌우는 몸통을 통째로 돌린다.
        transform.Rotate(Vector3.up, LookDeltaX, Space.Self);

        // 위아래는 머리만 돌리고, 너무 젖혀지지 않게 막는다.
        _pitch = Mathf.Clamp(_pitch - LookDeltaY, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ============================================================
    // 이동
    // ============================================================

    private void HandleMove()
    {
        // 대사·안내창이 떠 있는 동안은 걷지 않는다. 중력과 감속은 그대로 돌린다.
        bool blocked = InputBlocked;
        float inputX = blocked ? 0f : Input.GetAxisRaw("Horizontal");
        float inputZ = blocked ? 0f : Input.GetAxisRaw("Vertical");

        // 대각선으로 갈 때 빨라지지 않도록 길이를 1로 맞춘다.
        Vector3 wish = transform.right * inputX + transform.forward * inputZ;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        bool running = !blocked && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float targetSpeed = running ? runSpeed : walkSpeed;

        // 목표 속도까지 부드럽게 따라간다.
        Vector3 targetVelocity = wish * targetSpeed;
        _planarVelocity = Vector3.Lerp(_planarVelocity, targetVelocity, acceleration * Time.deltaTime);

        // 중력. 땅에 붙어 있을 때는 살짝 눌러 줘야 경사에서 덜덜 떨지 않는다.
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = _planarVelocity;
        motion.y = _verticalVelocity;
        _controller.Move(motion * Time.deltaTime);
    }

    // ============================================================
    // 머리 흔들림
    // ============================================================

    private void HandleHeadBob()
    {
        if (!useHeadBob)
        {
            cameraPivot.localPosition = _cameraBasePosition;
            return;
        }

        float amount = MoveAmount01;

        if (_controller.isGrounded && amount > 0.05f)
        {
            _bobTimer += Time.deltaTime * bobFrequency * Mathf.Max(amount, 0.2f);
        }
        else
        {
            // 멈추면 흔들림을 0 으로 되돌린다.
            _bobTimer = Mathf.MoveTowards(_bobTimer, 0f, Time.deltaTime * bobFrequency);
        }

        float offsetY = Mathf.Sin(_bobTimer * 2f) * bobAmplitude * amount;
        float offsetX = Mathf.Cos(_bobTimer) * bobAmplitude * 0.5f * amount;

        cameraPivot.localPosition = _cameraBasePosition + new Vector3(offsetX, offsetY, 0f);
    }

    // ============================================================
    // 커서
    // ============================================================

    /// <summary>대사나 조작법 안내창이 떠 있는 동안 true. 그동안 이동과 시점을 멈춘다.</summary>
    private static bool InputBlocked => SubtitleUI.Blocking || ControlGuideUI.Blocking;

    private void HandleCursorToggle()
    {
        // 조작법 안내창은 아무 키로나 닫히는데, 그 키가 ESC 면 커서까지 같이 풀려 버렸다.
        if (Input.GetKeyDown(KeyCode.Escape) && !ControlGuideUI.Blocking)
        {
            SetCursorLocked(!_cursorLocked);
        }

        // 풀린 커서는 화면을 클릭하면 다시 잠근다 (BrightDream 과 같은 방식).
        if (!_cursorLocked && Input.GetMouseButtonDown(0) && !InputBlocked)
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible  = !locked;
    }

    private void OnDisable()
    {
        // 씬을 넘어갈 때 커서가 잠긴 채로 남지 않게 풀어 준다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
