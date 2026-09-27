using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬에 놓인 침대 1개를 가리키는 컴포넌트.
///
/// "이 침대는 몇 번인가"와 "여기 누가 누워 있는가"만 담당한다.
/// 누구를 눕힐지는 HospitalRoomController 가 HospitalRoomData 를 보고 정해서
/// Assign() 으로 넘겨 준다.
///
/// 빈 침대면 환자 표현물(patientVisual)이 자동으로 꺼진다.
/// </summary>
[DisallowMultipleComponent]
public class BedSlot : MonoBehaviour
{
    [Header("침대 번호")]
    [Tooltip("1부터 시작. HospitalRoomData 의 Bed Number 와 같아야 짝이 맞는다.")]
    [SerializeField] private int bedNumber = 1;

    [Header("환자 표현물")]
    [Tooltip("이 침대에 누워 있는 사람 오브젝트. 빈 침대면 자동으로 꺼진다.")]
    [SerializeField] private GameObject patientVisual;

    [Tooltip("환자 머리 (PatientData 의 Skin Color 가 적용된다)")]
    [SerializeField] private Renderer headRenderer;

    [Tooltip("환자 머리카락 (PatientData 의 Hair Color 가 적용된다)")]
    [SerializeField] private Renderer hairRenderer;

    [Header("이름표 (침대 발치 카드)")]
    [Tooltip("비워 둬도 된다. 넣으면 환자 이름이 자동으로 찍힌다.")]
    [SerializeField] private Text nameplateText;

    // ------------------------------------------------------------
    // 런타임 상태
    // ------------------------------------------------------------

    /// <summary>침대 번호</summary>
    public int BedNumber => bedNumber;

    /// <summary>지금 이 침대에 누워 있는 환자. 빈 침대면 null.</summary>
    public PatientData Patient { get; private set; }

    /// <summary>환자가 누워 있으면 true</summary>
    public bool IsOccupied => Patient != null;

    private void Awake()
    {
        // 레거시 Text 의 기본 폰트에는 한글 글자가 없어서 네모로 깨진다.
        // HangulFont 가 운영체제에 깔린 한글 폰트를 찾아 갈아 끼워 준다.
        HangulFont.Apply(nameplateText);
    }

    // ============================================================
    // 환자 배정
    // ============================================================

    /// <summary>이 침대에 환자를 눕힌다. null 을 넣으면 빈 침대가 된다.</summary>
    public void Assign(PatientData patient)
    {
        Patient = patient;

        // 빈 침대면 사람 오브젝트를 꺼 버린다.
        if (patientVisual != null)
        {
            patientVisual.SetActive(patient != null);
        }

        if (patient == null)
        {
            if (nameplateText != null) nameplateText.text = $"{bedNumber}";
            return;
        }

        // 머리 / 머리카락 색을 환자에 맞춰 바꾼다.
        // (material 을 직접 만지면 머티리얼 사본이 계속 생기므로 PropertyBlock 을 쓴다)
        ApplyColor(headRenderer, patient.SkinColor);
        ApplyColor(hairRenderer, patient.HairColor);

        if (nameplateText != null)
        {
            nameplateText.text = $"{bedNumber}  {patient.PatientName}";
        }

        gameObject.name = $"Bed_{bedNumber:00}_{patient.PatientName}";
    }

    /// <summary>환자를 내보내고 빈 침대로 되돌린다.</summary>
    public void Clear()
    {
        Assign(null);
    }

    // ============================================================
    // 내부 도우미
    // ============================================================

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock _block;

    private static void ApplyColor(Renderer target, Color color)
    {
        if (target == null) return;

        if (_block == null) _block = new MaterialPropertyBlock();

        target.GetPropertyBlock(_block);
        _block.SetColor(ColorId, color);
        target.SetPropertyBlock(_block);
    }

    /// <summary>윈도우에 깔려 있는 한글 폰트를 하나 찾아 온다. 없으면 null.</summary>


#if UNITY_EDITOR
    /// <summary>Scene 뷰에서 침대 번호와 환자 이름을 글자로 띄워 준다. (에디터 전용)</summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1f, 0.55f, 0.15f) : new Color(0.4f, 0.8f, 1f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1.05f, 1f, 2.15f));

        string label = Patient != null
            ? $"{bedNumber}번 · {Patient.PatientName}"
            : $"{bedNumber}번 · (빈 침대)";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.4f, label);
    }
#endif
}
