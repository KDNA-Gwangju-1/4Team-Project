using System.Collections;
using UnityEngine;

/// <summary>
/// 침대에 누워 있는 환자를 "손대기" 로 만져 볼 수 있게 해 주는 컴포넌트.
///
/// - 화면에 뜨는 이름은 BedSlot 에 배정된 환자에게서 자동으로 가져온다.
///   (즉 Room Data 에서 환자를 바꾸면 안내문도 따라 바뀐다)
/// - 빈 침대면 E 가 아예 안 뜬다.
/// - 만지면 손이 살짝 움찔하고, 차트가 Console 에 찍힌다.
///
/// 다른 반응을 붙이고 싶으면 Inspector 의 On Interact 이벤트에 연결하면 된다.
/// </summary>
public class PatientTouchInteractable : Interactable
{
    [Header("환자")]
    [Tooltip("이 환자가 누워 있는 침대. 이름은 여기서 자동으로 가져온다.")]
    [SerializeField] private BedSlot bedSlot;

    [Header("만졌을 때 반응")]
    [Tooltip("살짝 움찔할 부위. 보통 손이나 팔을 넣는다. 비워 두면 움직이지 않는다.")]
    [SerializeField] private Transform twitchTarget;

    [Tooltip("움찔하는 각도 (도)")]
    [SerializeField] private float twitchAngle = 10f;

    [Tooltip("움찔했다가 제자리로 돌아오기까지 걸리는 시간 (초)")]
    [SerializeField] private float twitchDuration = 0.8f;

    [Tooltip("켜 두면 만질 때마다 환자 차트를 Console 에 찍는다.")]
    [SerializeField] private bool logChartOnTouch = true;

    [Header("만지면 꿈으로")]
    [Tooltip("대사가 다 끝난 뒤 꿈 로딩 화면으로 넘어갈지")]
    [SerializeField] private bool enterDreamOnTouch = true;

    [Tooltip("로딩 화면 다음에 갈 씬.\n" +
             "비워 두면 로딩 화면만 뜨고 그대로 머문다. (꿈 씬을 아직 안 만들었을 때)")]
    [SerializeField] private string dreamScene = "";

    [Tooltip("꿈 로딩 화면 배경.\n" +
             "비워 두면 아래 이름으로 Resources 폴더에서 찾아온다.")]
    [SerializeField] private Sprite dreamLoadingBackground;

    [Tooltip("Assets/Resources 안에 있는 배경 파일 이름 (확장자 없이)")]
    [SerializeField] private string dreamBackgroundResource = "DreamLoadingBackground";

    [Tooltip("대사가 끝나고 로딩 화면이 뜰 때까지의 뜸 (초)")]
    [SerializeField] private float afterDialogueDelay = 0.4f;

    /// <summary>지금 이 침대에 누워 있는 환자. 빈 침대면 null.</summary>
    public PatientData Patient => bedSlot != null ? bedSlot.Patient : null;

    /// <summary>몇 번 만졌는지</summary>
    public int TouchCount { get; private set; }

    private Coroutine twitchRoutine;
    private bool enteringDream;
    // 안내문 윗줄에 뭐라고 띄울지 정한다.
    // Inspector 의 Display Name 을 적어 두면 그걸 쓰고(예: "쌍둥이 언니"),
    // 비워 두면 환자 데이터의 실명을 그대로 보여 준다.
    public override string DisplayName
    {
        get
        {
            var patient = Patient;
            // Inspector 의 Display Name 을 적어 두면 그걸 먼저 쓴다.
            // (환자 실명 대신 '쌍둥이 언니' 처럼 보여 주고 싶을 때)
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName;

            return patient != null ? patient.PatientName : "";
        }
    }

    // 빈 침대면 E 가 뜨지 않는다.
    public override bool CanInteract => isActiveAndEnabled && Patient != null;

    /// <summary>컴포넌트를 처음 붙였을 때의 기본값</summary>
    /// <summary>컴포넌트를 처음 붙였을 때의 기본값</summary>
    private void Reset()
    {
        actionLabel   = "손대기";
        interactRange = 1.5f;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        TouchCount++;

        var patient = Patient;

        if (logChartOnTouch && patient != null)
        {
            if (TouchCount == 1)
            {
                // 처음 만졌을 때만 차트를 통째로 보여 준다.
                Debug.Log($"[손대기] {DisplayName}의 손을 잡았다. 손끝이 얼음처럼 차다.\n{patient.BuildChartText()}", this);
            }
            else
            {
                Debug.Log($"[손대기] {DisplayName}의 손을 다시 잡았다. ({TouchCount}번째)", this);
            }
        }

        // 손이 살짝 움찔한다.
        // StopAllCoroutines 를 쓰면 아래 꿈 전환까지 같이 끊기므로 이것만 따로 잡는다.
        if (twitchTarget != null && gameObject.activeInHierarchy)
        {
            if (twitchRoutine != null) StopCoroutine(twitchRoutine);
            twitchRoutine = StartCoroutine(TwitchRoutine());
        }

        // Inspector 에 연결해 둔 On Interact 이벤트 실행 (여기서 대사가 시작된다)
        base.Interact(interactor);

        // 대사가 끝나면 꿈으로 들어간다.
        if (enterDreamOnTouch && !enteringDream && gameObject.activeInHierarchy)
        {
            enteringDream = true;
            StartCoroutine(EnterDreamRoutine());
        }
    }

    /// <summary>대사를 끝까지 읽고 나서 꿈 로딩 화면으로 넘긴다.</summary>
    private IEnumerator EnterDreamRoutine()
    {
        // On Interact 로 시작된 대사가 실제로 켜질 때까지 한 프레임 기다린다.
        yield return null;

        // "들어간다." 까지 다 읽고 나서 넘어가야 한다.
        while (SubtitleUI.Blocking) yield return null;

        if (afterDialogueDelay > 0f)
            yield return new WaitForSecondsRealtime(afterDialogueDelay);

        var art = dreamLoadingBackground;
        if (art == null && !string.IsNullOrWhiteSpace(dreamBackgroundResource))
            art = Resources.Load<Sprite>(dreamBackgroundResource);

        if (art == null)
            Debug.LogWarning("[손대기] 꿈 로딩 배경을 못 찾았습니다. " +
                             "Assets/Resources/" + dreamBackgroundResource + " 가 있는지 보세요.", this);

        LoadingScreen.Go(dreamScene, art);
    }

    /// <summary>한 번 움찔했다가 천천히 제자리로 돌아온다.</summary>
    private IEnumerator TwitchRoutine()
    {
        Quaternion start = twitchTarget.localRotation;

        float elapsed = 0f;
        while (elapsed < twitchDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / twitchDuration);

            // 앞쪽에서 크게 한 번, 뒤로 갈수록 잦아드는 파형
            float wave = Mathf.Sin(k * Mathf.PI) * Mathf.Sin(k * Mathf.PI * 3f);

            twitchTarget.localRotation = start * Quaternion.Euler(twitchAngle * wave, 0f, 0f);
            yield return null;
        }

        twitchTarget.localRotation = start;
    }
}
