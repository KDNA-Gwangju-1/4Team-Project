using UnityEngine;

/// <summary>
/// 레벨 블록아웃을 직접 걸어 보기 위한 최소 기능 1인칭 컨트롤러.
///
/// 프로젝트에 아직 정식 Player / FPS Controller 가 없어서 테스트용으로 만든 것이다.
/// 나중에 팀의 정식 컨트롤러가 나오면 이 스크립트는 걷어내면 된다.
///
/// 조작
///   WASD  이동 / Shift 달리기 / Space 점프
///   마우스  시점 (감도는 GameSettings.MouseSensitivity 를 그대로 쓴다)
///   P     자동 걷기 on/off - 길을 따라 끝까지 걸으며 소요 시간을 Console 에 기록
///   Esc   일시정지 메뉴 (PauseMenu 가 커서를 풀고, 닫을 때 되돌린다)
///
/// 자동 걷기 중에는 E 상호작용 키를 누를 수 없어서, 단서 조사(ClueInteractable)가
/// IsAutoWalking을 보고 자동으로 조사 처리한다 - 안 그러면 단서를 하나도 못 모아서
/// 단서 4개를 요구하는 게이트(Gate_CombatArena)에 막혀 끝까지 못 걷는다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimpleFirstPersonController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -18f;

    [Header("시점")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSpeed = 2.0f;
    [SerializeField] private float pitchLimit = 85f;

    [Header("자동 걷기 테스트")]
    [SerializeField] private BrightDreamPath path;
    [SerializeField] private bool autoWalkOnStart;

    private CharacterController controller;
    private float pitch;
    private float verticalVelocity;

    /// <summary>지금 이 프레임에 바닥을 딛고 있는지 - 보스 점프 착지 공격의 회피 판정 등 외부에서 참조한다.</summary>
    public bool IsGrounded => controller.isGrounded;

    /// <summary>자동 걷기 중인지 - E키 상호작용(단서 조사)이 안 눌리는 자동 걷기 중에는
    /// ClueInteractable/ClueManager가 조사 UI로 멈추지 않고 조용히 자동 수집하도록 참조한다.</summary>
    public static bool IsAutoWalking { get; private set; }

    /// <summary>디버그 스테이지 스킵 등 외부에서 즉시 순간이동시킬 때 쓴다.
    /// CharacterController는 transform.position을 직접 바꾸는 걸 막으므로 잠깐 꺼야 한다.</summary>
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        controller.enabled = true;
    }

    // ---- 자동 걷기 상태 ----
    private bool autoWalking;
    private int autoWaypointIndex;
    private float autoElapsed;
    private float autoDistance;
    private Vector3 autoLastPosition;
    private string autoCurrentArea = string.Empty;
    private float autoAreaEnteredAt;
    private float autoAreaEnteredDistance;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        // 에디터 창이 뒤로 가면 Play Mode 가 멈춰서 자동 걷기 측정이 중단된다.
        // 프로젝트 설정(모두가 공유)을 건드리지 않고 이 테스트 Scene 에서만 켠다.
        // 빌드에서는 켜지 않는다 - 켜 두면 Alt+Tab 중에도 타이머와 몬스터가 계속 돈다.
#if UNITY_EDITOR
        Application.runInBackground = true;
#endif
    }

    private void Start()
    {
        LockCursor(true);
        if (autoWalkOnStart) StartAutoWalk();
    }

    private void Update()
    {
        // ESC 일시정지 중에는 입력을 받지 않는다.
        if (PauseMenu.IsPaused) return;
        // ESC 는 일시정지 메뉴가 맡는다 (메뉴가 커서를 풀고, 닫을 때 되돌린다).
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.P)) ToggleAutoWalk();
#endif

        if (autoWalking) UpdateAutoWalk();
        else UpdateManual();
    }

    // ==========================================================
    // 직접 조작
    // ==========================================================
    private void UpdateManual()
    {
        if (Cursor.lockState == CursorLockMode.Locked) ApplyLook();

        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 wish = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !JumpBlockedByDialogue()) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((wish * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void ApplyLook()
    {
        float sensitivity = lookSpeed * GameSettings.MouseSensitivity;
        transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * sensitivity, Space.World);

        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity, -pitchLimit, pitchLimit);
        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ==========================================================
    // 자동 걷기 - 길 동선과 소요 시간 측정용
    // ==========================================================
    private void ToggleAutoWalk()
    {
        if (autoWalking) StopAutoWalk("사용자가 중단");
        else StartAutoWalk();
    }

    private void StartAutoWalk()
    {
        if (path == null || path.waypoints == null || path.waypoints.Length < 2)
        {
            Debug.LogWarning("[AutoWalk] BrightDreamPath 가 없어서 자동 걷기를 시작할 수 없다.");
            return;
        }

        // 첫 Waypoint 로 순간이동한 뒤 출발한다.
        controller.enabled = false;
        transform.position = path.waypoints[0] + Vector3.up * 0.1f;
        controller.enabled = true;

        autoWalking = true;
        IsAutoWalking = true;
        autoWaypointIndex = 1;
        autoElapsed = 0f;
        autoDistance = 0f;
        autoLastPosition = transform.position;
        autoAreaEnteredAt = 0f;
        autoAreaEnteredDistance = 0f;
        autoCurrentArea = path.AreaNameAtDistance(0f);

        Debug.Log($"[AutoWalk] 시작 - waypoint {path.waypoints.Length}개 / 경로 길이 {path.TotalLength:F1}m / 걷기 속도 {walkSpeed:F1}m/s");
    }

    private void UpdateAutoWalk()
    {
        Vector3 target = path.waypoints[autoWaypointIndex];
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        Vector3 toTarget = flatTarget - transform.position;

        // 이동 방향을 바라보게 해서 1인칭 화면도 실제 플레이와 비슷하게 만든다.
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(toTarget.x, 0f, toTarget.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 180f * Time.deltaTime);
        }

        verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + gravity * Time.deltaTime;
        controller.Move((transform.forward * walkSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

        autoElapsed += Time.deltaTime;
        Vector3 moved = transform.position - autoLastPosition;
        autoDistance += new Vector3(moved.x, 0f, moved.z).magnitude;
        autoLastPosition = transform.position;

        ReportAreaChange();

        if (toTarget.sqrMagnitude < 0.5f * 0.5f)
        {
            autoWaypointIndex++;
            if (autoWaypointIndex >= path.waypoints.Length) StopAutoWalk("목적지 도착");
        }

        // 길이 막혀 제자리걸음일 때 무한 대기를 막는다.
        if (autoElapsed > 300f) StopAutoWalk("시간 초과 - 길이 막혔을 가능성");
    }

    private void ReportAreaChange()
    {
        // 맵이 꺾이므로 구간 판정은 Z 좌표가 아니라 걸어온 거리로 한다.
        string area = path.AreaNameAtDistance(autoDistance);
        if (area == autoCurrentArea || string.IsNullOrEmpty(area)) return;

        if (!string.IsNullOrEmpty(autoCurrentArea))
        {
            Debug.Log($"[AutoWalk] 구간 '{autoCurrentArea}' 통과 - " +
                      $"{autoDistance - autoAreaEnteredDistance:F1}m / {autoElapsed - autoAreaEnteredAt:F1}초 " +
                      $"(누적 {autoDistance:F1}m / {autoElapsed:F1}초)");
        }

        autoCurrentArea = area;
        autoAreaEnteredAt = autoElapsed;
        autoAreaEnteredDistance = autoDistance;
    }

    private void StopAutoWalk(string reason)
    {
        autoWalking = false;
        IsAutoWalking = false;

        if (!string.IsNullOrEmpty(autoCurrentArea))
        {
            Debug.Log($"[AutoWalk] 구간 '{autoCurrentArea}' 통과 - " +
                      $"{autoDistance - autoAreaEnteredDistance:F1}m / {autoElapsed - autoAreaEnteredAt:F1}초");
        }

        Debug.Log($"[AutoWalk] 종료({reason}) - 총 이동 {autoDistance:F1}m / 총 소요 {autoElapsed:F1}초 " +
                  $"({autoElapsed / 60f:F2}분)");
    }

    // 대사 넘기기도 스페이스바라서, 대사를 넘기려고 누른 키로 점프하지 않게 막는다.
    // 대사가 떠 있는 동안, 대사가 닫힌 프레임, 대사/조사 뒤 이 컨트롤러가 다시 켜진 프레임이 해당된다.
    private int enabledFrame = -1;

    private void OnEnable()
    {
        enabledFrame = Time.frameCount;
    }

    private bool JumpBlockedByDialogue()
    {
        return BrightDream.DialogueUI.IsShowing
            || Time.frameCount == BrightDream.DialogueUI.LastClosedFrame
            || Time.frameCount == enabledFrame;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
