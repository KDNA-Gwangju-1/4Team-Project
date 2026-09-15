using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병실 씬의 관리 스크립트.
///
/// 씬이 시작되면 HospitalRoomData(에셋)를 읽어서,
/// 씬에 놓인 BedSlot 들에 환자를 자동으로 배정한다.
///
/// 환자를 바꾸고 싶으면 씬이 아니라 Room Data 에셋만 고치면 된다.
/// 다른 팀원 스크립트에서 환자 정보가 필요하면:
///
///     var room = FindFirstObjectByType&lt;HospitalRoomController&gt;();
///     PatientData p = room.GetPatient(1);   // 1번 침대 환자
/// </summary>
[DisallowMultipleComponent]
public class HospitalRoomController : MonoBehaviour
{
    [Header("병실 데이터")]
    [Tooltip("이 병실이 쓸 데이터 에셋. 비워 두면 아무도 눕지 않는다.")]
    [SerializeField] private HospitalRoomData roomData;

    [Header("씬에 놓인 침대들")]
    [Tooltip("비워 두면 Awake 때 자식에서 BedSlot 을 알아서 찾아 채운다.")]
    [SerializeField] private List<BedSlot> bedSlots = new List<BedSlot>();

    [Header("디버그")]
    [Tooltip("켜 두면 시작할 때 병실 차트 전체를 Console 에 찍어 준다.")]
    [SerializeField] private bool logRosterOnStart = true;

    /// <summary>이 병실이 쓰고 있는 데이터 에셋</summary>
    public HospitalRoomData RoomData => roomData;

    /// <summary>씬에 놓인 침대 목록</summary>
    public IReadOnlyList<BedSlot> BedSlots => bedSlots;

    private void Awake()
    {
        // Inspector 에서 안 채웠으면 자식에서 직접 찾는다.
        if (bedSlots == null || bedSlots.Count == 0)
        {
            bedSlots = new List<BedSlot>(GetComponentsInChildren<BedSlot>(true));
        }

        ApplyRoomData();
    }

    private void Start()
    {
        if (logRosterOnStart && roomData != null)
        {
            Debug.Log($"[HospitalRoom] {roomData.BuildRosterText()}", this);
        }
    }

    // ============================================================
    // 배정
    // ============================================================

    /// <summary>Room Data 를 다시 읽어서 침대에 환자를 배정한다.</summary>
    [ContextMenu("병실 데이터 다시 적용")]
    public void ApplyRoomData()
    {
        if (bedSlots == null) return;

        for (int i = 0; i < bedSlots.Count; i++)
        {
            var slot = bedSlots[i];
            if (slot == null) continue;

            PatientData patient = roomData != null ? roomData.GetPatient(slot.BedNumber) : null;
            slot.Assign(patient);
        }
    }

    // ============================================================
    // 바깥에서 쓰는 조회 메서드
    // ============================================================

    /// <summary>해당 번호 침대에 누워 있는 환자. 빈 침대면 null.</summary>
    public PatientData GetPatient(int bedNumber)
    {
        var slot = GetBedSlot(bedNumber);
        return slot != null ? slot.Patient : null;
    }

    /// <summary>해당 번호 침대 오브젝트. 없으면 null.</summary>
    public BedSlot GetBedSlot(int bedNumber)
    {
        if (bedSlots == null) return null;

        for (int i = 0; i < bedSlots.Count; i++)
        {
            if (bedSlots[i] != null && bedSlots[i].BedNumber == bedNumber) return bedSlots[i];
        }
        return null;
    }

    /// <summary>이름으로 환자를 찾는다. 없으면 null.</summary>
    public PatientData FindPatientByName(string patientName)
    {
        if (bedSlots == null || string.IsNullOrEmpty(patientName)) return null;

        for (int i = 0; i < bedSlots.Count; i++)
        {
            var slot = bedSlots[i];
            if (slot != null && slot.Patient != null && slot.Patient.PatientName == patientName)
            {
                return slot.Patient;
            }
        }
        return null;
    }

    /// <summary>이 병실에 누워 있는 환자 전부 (빈 침대는 빠진다)</summary>
    public List<PatientData> GetPatients()
    {
        var result = new List<PatientData>();
        if (bedSlots == null) return result;

        for (int i = 0; i < bedSlots.Count; i++)
        {
            if (bedSlots[i] != null && bedSlots[i].Patient != null) result.Add(bedSlots[i].Patient);
        }
        return result;
    }

    /// <summary>
    /// 이 병실에 있는 쌍둥이 한 쌍을 찾아 돌려준다.
    /// 쌍둥이가 없으면 둘 다 null 로 돌아온다.
    /// </summary>
    public void GetTwins(out PatientData elder, out PatientData younger)
    {
        elder = null;
        younger = null;

        var patients = GetPatients();
        for (int i = 0; i < patients.Count; i++)
        {
            var p = patients[i];
            if (!p.IsTwin) continue;

            // 서로를 가리키고 있는 한 쌍만 인정한다.
            if (p.TwinSibling.TwinSibling != p) continue;

            if (p.IsElderTwin) { elder = p; younger = p.TwinSibling; }
            else               { younger = p; elder = p.TwinSibling; }
            return;
        }
    }
}
