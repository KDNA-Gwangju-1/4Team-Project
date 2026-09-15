using UnityEngine;

/// <summary>
/// 환자 한 명의 정보를 담아 두는 데이터 에셋(ScriptableObject).
///
/// - 새로 만들기 : Project 창 우클릭 > Create > Hospital > Patient Data
/// - 값 고치기   : Inspector 에서 직접 수정한다. (더미데이터라 값이 자주 바뀐다)
/// - 코드에서    : patient.PatientName 처럼 읽기 전용 프로퍼티로만 읽는다.
///
/// "몇 번 병실 몇 번 침대에 눕는지"는 이 파일이 아니라 HospitalRoomData 가 정한다.
/// (여기 적힌 RoomNumber / BedNumber 는 차트에 표시하기 위한 참고용 값)
/// </summary>
[CreateAssetMenu(fileName = "Patient_New", menuName = "Hospital/Patient Data")]
public class PatientData : ScriptableObject
{
    // ============================================================
    // 열거형
    // ============================================================

    /// <summary>성별</summary>
    public enum Gender
    {
        Female,     // 여
        Male,       // 남
        Unknown     // 미상
    }

    /// <summary>의식 수준. 위로 갈수록 또렷하고, 아래로 갈수록 반응이 없다.</summary>
    public enum Consciousness
    {
        Alert,      // 명료   - 말을 걸면 바로 대답한다
        Drowsy,     // 기면   - 자꾸 잠들지만 깨우면 반응한다
        Stupor,     // 혼미   - 강한 자극에만 반응한다
        SemiComa,   // 반혼수 - 통증에만 겨우 반응한다
        Coma        // 혼수   - 어떤 자극에도 반응이 없다
    }

    /// <summary>바이탈 사인. 환자 모니터에 찍히는 숫자들.</summary>
    [System.Serializable]
    public struct VitalSigns
    {
        [Tooltip("심박수 (회/분)")]     public int   heartRate;
        [Tooltip("수축기 혈압 (mmHg)")] public int   systolic;
        [Tooltip("이완기 혈압 (mmHg)")] public int   diastolic;
        [Tooltip("체온 (섭씨)")]        public float temperature;
        [Tooltip("산소포화도 (%)")]     public int   oxygenSaturation;

        /// <summary>"HR 58 / BP 92-60 / 35.9C / SpO2 97%" 형태의 한 줄 요약</summary>
        public string Summary =>
            $"HR {heartRate} / BP {systolic}-{diastolic} / {temperature:0.0}C / SpO2 {oxygenSaturation}%";
    }

    // ============================================================
    // 기본 정보
    // ============================================================
    [Header("기본 정보")]
    [Tooltip("차트 번호. 병원 안에서 환자를 구분하는 고유 번호")]
    [SerializeField] private string patientId = "P-0000";

    [SerializeField] private string patientName = "이름 없음";

    [Range(0, 120)]
    [SerializeField] private int age = 0;

    [SerializeField] private Gender gender = Gender.Unknown;

    [SerializeField] private string bloodType = "A형";

    // ============================================================
    // 입원 정보
    // ============================================================
    [Header("입원 정보")]
    [SerializeField] private string roomNumber = "000";

    [Tooltip("병실 안에서 몇 번 침대인지 (1부터). 씬의 BedSlot 번호와 맞춘다.")]
    [SerializeField] private int bedNumber = 1;

    [Tooltip("YYYY-MM-DD 형식으로 적는다")]
    [SerializeField] private string admissionDate = "2026-01-01";

    [SerializeField] private string diagnosis = "미상";

    [SerializeField] private string attendingDoctor = "미정";

    [SerializeField] private Consciousness consciousness = Consciousness.Alert;

    // ============================================================
    // 바이탈
    // ============================================================
    [Header("바이탈")]
    [SerializeField]
    private VitalSigns vitals = new VitalSigns
    {
        heartRate = 72,
        systolic = 118,
        diastolic = 76,
        temperature = 36.5f,
        oxygenSaturation = 98,
    };

    // ============================================================
    // 가족 관계
    // ============================================================
    [Header("쌍둥이 / 가족 관계")]
    [Tooltip("쌍둥이 형제자매가 있으면 그 PatientData 를 넣는다. 서로를 가리키게 해 두면 편하다.")]
    [SerializeField] private PatientData twinSibling;

    [Tooltip("쌍둥이 중 먼저 태어난 쪽이면 체크")]
    [SerializeField] private bool isElderTwin = false;

    // ============================================================
    // 연출용 (침대 위 환자 표현물에 쓰인다)
    // ============================================================
    [Header("연출용")]
    [SerializeField] private Color hairColor = new Color(0.16f, 0.12f, 0.10f, 1f);
    [SerializeField] private Color skinColor = new Color(0.93f, 0.82f, 0.74f, 1f);

    [Header("메모")]
    [TextArea(3, 8)]
    [SerializeField] private string chartNote = "";

    // ============================================================
    // 읽기 전용 프로퍼티
    // ============================================================
    public string PatientId      => patientId;
    public string PatientName    => patientName;
    public int    Age            => age;
    public Gender PatientGender  => gender;
    public string BloodType      => bloodType;

    public string RoomNumber     => roomNumber;
    public int    BedNumber      => bedNumber;
    public string AdmissionDate  => admissionDate;
    public string Diagnosis      => diagnosis;
    public string AttendingDoctor => attendingDoctor;
    public Consciousness ConsciousnessLevel => consciousness;

    public VitalSigns Vitals     => vitals;

    public PatientData TwinSibling => twinSibling;
    public bool   IsTwin         => twinSibling != null;
    public bool   IsElderTwin    => isElderTwin;

    public Color  HairColor      => hairColor;
    public Color  SkinColor      => skinColor;
    public string ChartNote      => chartNote;

    // ============================================================
    // 화면에 뿌리기 좋은 형태로 가공해 주는 것들
    // ============================================================

    /// <summary>"여" / "남" / "미상"</summary>
    public string GenderLabel
    {
        get
        {
            switch (gender)
            {
                case Gender.Female: return "여";
                case Gender.Male:   return "남";
                default:            return "미상";
            }
        }
    }

    /// <summary>"명료" / "기면" / "혼미" / "반혼수" / "혼수"</summary>
    public string ConsciousnessLabel
    {
        get
        {
            switch (consciousness)
            {
                case Consciousness.Alert:    return "명료";
                case Consciousness.Drowsy:   return "기면";
                case Consciousness.Stupor:   return "혼미";
                case Consciousness.SemiComa: return "반혼수";
                case Consciousness.Coma:     return "혼수";
                default:                     return "미상";
            }
        }
    }

    /// <summary>"서하린 (12세 / 여)" — 이름표나 대화창 머리말에 쓰기 좋다.</summary>
    public string DisplayName => $"{patientName} ({age}세 / {GenderLabel})";

    /// <summary>"302호 1번 침대"</summary>
    public string BedLabel => $"{roomNumber}호 {bedNumber}번 침대";

    /// <summary>차트 전체를 여러 줄 문자열로 만든다. 디버그 출력이나 차트 UI 에 그대로 쓸 수 있다.</summary>
    public string BuildChartText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[{patientId}] {patientName}  {age}세 / {GenderLabel} / {bloodType}");
        sb.AppendLine($"  위치   : {BedLabel}");
        sb.AppendLine($"  입원일 : {admissionDate}");
        sb.AppendLine($"  진단   : {diagnosis}");
        sb.AppendLine($"  담당의 : {attendingDoctor}");
        sb.AppendLine($"  의식   : {ConsciousnessLabel}");
        sb.AppendLine($"  바이탈 : {vitals.Summary}");

        if (IsTwin)
        {
            string order = isElderTwin ? "언니" : "동생";
            sb.AppendLine($"  쌍둥이 : {twinSibling.PatientName} ({order}은 본인)");
        }

        if (!string.IsNullOrWhiteSpace(chartNote))
        {
            sb.AppendLine($"  메모   : {chartNote.Replace("\n", "\n           ")}");
        }

        return sb.ToString().TrimEnd();
    }
}
