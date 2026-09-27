using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병실 하나의 정보 + "몇 번 침대에 누가 눕는지"를 담아 두는 데이터 에셋.
///
/// - 새로 만들기 : Project 창 우클릭 > Create > Hospital > Hospital Room Data
/// - 씬 쪽에서는 HospitalRoomController 가 이 에셋을 읽어서
///   씬에 놓인 BedSlot 들에 환자를 자동으로 배정한다.
///
/// 환자를 바꾸고 싶으면 씬을 건드릴 필요 없이 이 에셋의 Beds 목록만 고치면 된다.
/// </summary>
[CreateAssetMenu(fileName = "Room_New", menuName = "Hospital/Hospital Room Data")]
public class HospitalRoomData : ScriptableObject
{
    /// <summary>침대 1개에 환자 1명을 묶어 두는 짝.</summary>
    [System.Serializable]
    public struct BedAssignment
    {
        [Tooltip("침대 번호 (1부터). 씬에 놓인 BedSlot 의 Bed Number 와 같아야 한다.")]
        public int bedNumber;

        [Tooltip("이 침대에 누워 있는 환자. 비워 두면 빈 침대가 된다.")]
        public PatientData patient;
    }

    // ============================================================
    // 병실 정보
    // ============================================================
    [Header("병실 정보")]
    [SerializeField] private string roomNumber = "000";
    [SerializeField] private string roomName   = "일반 병실";
    [SerializeField] private string ward       = "본관 3층";

    [Tooltip("이 병실에 놓인 침대 개수 (빈 침대 포함)")]
    [Range(1, 8)]
    [SerializeField] private int bedCount = 3;

    // ============================================================
    // 침대 배정
    // ============================================================
    [Header("침대 배정")]
    [SerializeField] private List<BedAssignment> beds = new List<BedAssignment>();

    // ============================================================
    // 읽기 전용 프로퍼티
    // ============================================================
    public string RoomNumber => roomNumber;
    public string RoomName   => roomName;
    public string Ward       => ward;
    public int    BedCount   => bedCount;

    public IReadOnlyList<BedAssignment> Beds => beds;

    /// <summary>"본관 3층 302호 (소아 중환자 병동)"</summary>
    public string FullLabel => $"{ward} {roomNumber}호 ({roomName})";

    /// <summary>실제로 환자가 누워 있는 침대 수</summary>
    public int OccupiedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < beds.Count; i++)
            {
                if (beds[i].patient != null) count++;
            }
            return count;
        }
    }

    /// <summary>해당 번호 침대에 누워 있는 환자를 돌려준다. 빈 침대면 null.</summary>
    public PatientData GetPatient(int bedNumber)
    {
        for (int i = 0; i < beds.Count; i++)
        {
            if (beds[i].bedNumber == bedNumber) return beds[i].patient;
        }
        return null;
    }

    /// <summary>이 병실에 누워 있는 환자들만 모아서 돌려준다. (빈 침대는 건너뛴다)</summary>
    public List<PatientData> GetPatients()
    {
        var result = new List<PatientData>();
        for (int i = 0; i < beds.Count; i++)
        {
            if (beds[i].patient != null) result.Add(beds[i].patient);
        }
        return result;
    }

    /// <summary>병실 전체 차트를 여러 줄 문자열로 만든다. 디버그 출력용.</summary>
    public string BuildRosterText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {FullLabel} / 침대 {bedCount}개 중 {OccupiedCount}개 사용 중 ===");

        for (int i = 0; i < beds.Count; i++)
        {
            var bed = beds[i];
            if (bed.patient == null)
            {
                sb.AppendLine($"[{bed.bedNumber}번 침대] (비어 있음)");
            }
            else
            {
                sb.AppendLine($"[{bed.bedNumber}번 침대]");
                sb.AppendLine(bed.patient.BuildChartText());
            }
        }

        return sb.ToString().TrimEnd();
    }
}
