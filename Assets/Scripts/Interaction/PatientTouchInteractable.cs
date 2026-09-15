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

    /// <summary>지금 이 침대에 누워 있는 환자. 빈 침대면 null.</summary>
    public PatientData Patient => bedSlot != null ? bedSlot.Patient : null;

    /// <summary>몇 번 만졌는지</summary>
    public int TouchCount { get; private set; }

    // 안내문 윗줄은 환자 이름을 그대로 쓴다.
    public override string DisplayName
    {
        get
        {
            var patient = Patient;
            return patient != null ? patient.PatientName : base.DisplayName;
        }
    }

    // 빈 침대면 E 가 뜨지 않는다.
    public override bool CanInteract => isActiveAndEnabled && Patient != null;

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
                Debug.Log($"[손대기] {patient.PatientName}의 손을 잡았다. 손끝이 얼음처럼 차다.\n{patient.BuildChartText()}", this);
            }
            else
            {
                Debug.Log($"[손대기] {patient.PatientName}의 손을 다시 잡았다. ({TouchCount}번째)", this);
            }
        }

        // 손이 살짝 움찔한다.
        if (twitchTarget != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(TwitchRoutine());
        }

        // Inspector 에 연결해 둔 On Interact 이벤트 실행
        base.Interact(interactor);
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
