using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 병실 맵(HospitalRoom.unity)과 쌍둥이 더미데이터를 통째로 만들어 주는 에디터 전용 도구.
///
///   Tools > Hospital > Rebuild Hospital Room Scene
///
/// 씬을 망가뜨렸거나 배치를 갈아엎고 싶을 때 언제든 다시 누르면 된다.
/// (환자 정보만 고칠 거면 씬을 다시 만들 필요 없이
///  Assets/Data/Rooms/Room_302.asset 과 Assets/Data/Patients/*.asset 만 고치면 된다)
///
/// 배치 요약
///   - 1번 / 2번 침대는 옆면이 딱 붙어 있고, 그 사이 난간은 내려져 있다.
///   - 쌍둥이 자매가 1번(언니 서하린) / 2번(동생 서하윤) 침대에 누워 서로 손을 맞대고 있다.
///   - 3번 침대는 비어 있다.
///   - 플레이어는 1인칭이고 화면 아래에 손이 보인다.
///   - 왼쪽 아이(1번 침대 서하린)에게 다가가면 [E] 손대기 가 뜬다.
/// </summary>
public static class HospitalRoomSceneBuilder
{
    // ============================================================
    // 경로
    // ============================================================
    private const string ScenePath      = "Assets/Scenes/HospitalRoom.unity";
    private const string MaterialFolder = "Assets/Materials/Hospital";
    private const string PatientFolder  = "Assets/Data/Patients";
    private const string RoomFolder     = "Assets/Data/Rooms";

    // ============================================================
    // 방 크기 (안쪽 기준, m)
    // ============================================================
    private const float RoomHalfX  = 4.5f;
    private const float RoomHalfZ  = 3.5f;
    private const float RoomHeight = 3.0f;
    private const float WallT      = 0.15f;

    // 창문 구멍
    private const float WindowBottom = 0.95f;
    private const float WindowTop    = 2.45f;
    private const float WindowHalfZ  = 2.60f;

    // ============================================================
    // 침대
    // ============================================================
    private const float BedWidth  = 1.00f;
    private const float BedLength = 2.10f;
    private const float BedHeadZ  = 3.30f;                       // 머리맡이 닿는 Z
    private static readonly float BedCenterZ = BedHeadZ - BedLength * 0.5f;

    // 1번과 2번은 폭(1.0m)만큼만 떨어져 있어서 옆면이 딱 붙는다. 3번은 따로 떨어져 있다.
    private static readonly float[] BedCenterX = { -2.70f, -1.70f, 2.20f };

    // ============================================================
    // 머티리얼
    // ============================================================
    private static Material _floor, _wall, _ceiling, _wainscot, _wood, _woodDark;
    private static Material _bedFrame, _bedRail, _mattress, _blanket, _pillow, _gown;
    private static Material _metal, _rubber, _skin, _hair, _nameplate, _glass;
    private static Material _nightSky, _building, _buildingLit, _curtain, _lightPanel, _fabric;

    // ============================================================
    // 메뉴
    // ============================================================

    [MenuItem("Tools/Hospital/Rebuild Hospital Room Scene")]
    public static void BuildAll()
    {
        CreateMaterials();

        // ---------- 더미데이터부터 만든다 ----------
        PatientData harin, hayun;
        HospitalRoomData room302;
        CreateDummyData(out harin, out hayun, out room302);

        // ---------- 빈 씬에서 새로 짓는다 ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildEnvironmentSettings();
        BuildRoom();
        BuildCityView();
        BuildLighting();
        BuildProps();

        var bedSlots = BuildBedsAndPatients(harin, hayun);

        // ---------- 병실 관리 오브젝트 ----------
        var roomManagerGO = new GameObject("HospitalRoom");
        var controller = roomManagerGO.AddComponent<HospitalRoomController>();

        // 침대들을 관리 오브젝트 밑으로 모아 둔다.
        foreach (var slot in bedSlots) slot.transform.SetParent(roomManagerGO.transform, true);

        var controllerSerialized = new SerializedObject(controller);
        controllerSerialized.FindProperty("roomData").objectReferenceValue = room302;

        var slotsProperty = controllerSerialized.FindProperty("bedSlots");
        slotsProperty.arraySize = bedSlots.Count;
        for (int i = 0; i < bedSlots.Count; i++)
        {
            slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = bedSlots[i];
        }
        controllerSerialized.FindProperty("logRosterOnStart").boolValue = true;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        // ---------- UI + 플레이어 ----------
        var promptUI = HospitalPlayerBuilder.BuildPromptUI();
        HospitalPlayerBuilder.BuildPlayer(new Vector3(-1.2f, 0.10f, -2.40f), 0f, MaterialFolder, promptUI);

        // ---------- 저장 ----------
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        RegisterInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HospitalRoomSceneBuilder] 병실 맵 생성 완료.\n" +
                  $"  씬     : {ScenePath}\n" +
                  $"  환자   : {PatientFolder}/Patient_SeoHarin.asset, Patient_SeoHayun.asset\n" +
                  $"  병실   : {RoomFolder}/Room_302.asset\n" +
                  $"  조작   : WASD 이동 / Shift 빠르게 / 마우스 시점 / E 손대기 / ESC 커서");
    }

    // ============================================================
    // 머티리얼
    // ============================================================

    private static void CreateMaterials()
    {
        _floor      = BuildUtil.Mat(MaterialFolder, "M_Floor",      new Color(0.84f, 0.83f, 0.80f), 0.35f);
        _wall       = BuildUtil.Mat(MaterialFolder, "M_Wall",       new Color(0.90f, 0.88f, 0.85f), 0.08f);
        _ceiling    = BuildUtil.Mat(MaterialFolder, "M_Ceiling",    new Color(0.94f, 0.94f, 0.93f), 0.05f);
        _wainscot   = BuildUtil.Mat(MaterialFolder, "M_Wainscot",   new Color(0.78f, 0.67f, 0.52f), 0.22f);
        _wood       = BuildUtil.Mat(MaterialFolder, "M_Wood",       new Color(0.80f, 0.67f, 0.49f), 0.25f);
        _woodDark   = BuildUtil.Mat(MaterialFolder, "M_WoodDark",   new Color(0.60f, 0.47f, 0.32f), 0.25f);

        _bedFrame   = BuildUtil.Mat(MaterialFolder, "M_BedFrame",   new Color(0.94f, 0.94f, 0.93f), 0.40f);
        _bedRail    = BuildUtil.Mat(MaterialFolder, "M_BedRail",    new Color(0.80f, 0.82f, 0.84f), 0.45f);
        _mattress   = BuildUtil.Mat(MaterialFolder, "M_Mattress",   new Color(0.95f, 0.93f, 0.87f), 0.12f);
        _blanket    = BuildUtil.Mat(MaterialFolder, "M_Blanket",    new Color(0.94f, 0.89f, 0.77f), 0.08f);
        _pillow     = BuildUtil.Mat(MaterialFolder, "M_Pillow",     new Color(0.98f, 0.98f, 0.97f), 0.08f);
        _gown       = BuildUtil.Mat(MaterialFolder, "M_Gown",       new Color(0.84f, 0.89f, 0.93f), 0.10f);

        _metal      = BuildUtil.Mat(MaterialFolder, "M_Metal",      new Color(0.72f, 0.75f, 0.78f), 0.62f, 0.80f);
        _rubber     = BuildUtil.Mat(MaterialFolder, "M_Rubber",     new Color(0.16f, 0.17f, 0.19f), 0.15f);
        _skin       = BuildUtil.Mat(MaterialFolder, "M_Skin",       new Color(0.91f, 0.80f, 0.74f), 0.10f);
        _hair       = BuildUtil.Mat(MaterialFolder, "M_Hair",       new Color(0.15f, 0.11f, 0.10f), 0.30f);
        _nameplate  = BuildUtil.Mat(MaterialFolder, "M_Nameplate",  new Color(0.95f, 0.66f, 0.29f), 0.20f);
        _curtain    = BuildUtil.Mat(MaterialFolder, "M_Curtain",    new Color(0.80f, 0.84f, 0.81f), 0.05f);
        _fabric     = BuildUtil.Mat(MaterialFolder, "M_Fabric",     new Color(0.52f, 0.57f, 0.61f), 0.08f);

        _glass      = BuildUtil.GlassMat(MaterialFolder, "M_Glass", new Color(0.62f, 0.74f, 0.82f, 0.16f));

        _nightSky   = BuildUtil.EmissiveMat(MaterialFolder, "M_NightSky", new Color(0.04f, 0.06f, 0.11f),
                                            new Color(0.05f, 0.08f, 0.16f));
        _building   = BuildUtil.Mat(MaterialFolder, "M_Building", new Color(0.17f, 0.20f, 0.26f), 0.20f);
        _buildingLit = BuildUtil.EmissiveMat(MaterialFolder, "M_BuildingLit", new Color(0.20f, 0.20f, 0.18f),
                                            new Color(1.00f, 0.82f, 0.52f) * 1.6f);
        _lightPanel = BuildUtil.EmissiveMat(MaterialFolder, "M_LightPanel", new Color(0.95f, 0.95f, 0.92f),
                                            new Color(1.00f, 0.96f, 0.88f) * 1.2f);
    }

    // ============================================================
    // 더미데이터 (쌍둥이 자매 + 302호)
    // ============================================================

    private static void CreateDummyData(out PatientData harin, out PatientData hayun,
                                        out HospitalRoomData room)
    {
        harin = CreateOrLoad<PatientData>(PatientFolder, "Patient_SeoHarin");
        hayun = CreateOrLoad<PatientData>(PatientFolder, "Patient_SeoHayun");
        room  = CreateOrLoad<HospitalRoomData>(RoomFolder, "Room_302");

        // ---------- 언니 : 서하린 (1번 침대, 플레이어 기준 왼쪽) ----------
        FillPatient(harin,
            id: "P-26-0117",
            name: "서하린",
            bedNumber: 1,
            elderTwin: true,
            heartRate: 58, systolic: 92, diastolic: 60, temperature: 35.9f, oxygen: 97,
            note: "입원 26일째. 한 번도 깨어나지 않았다.\n" +
                  "뇌파와 MRI 모두 이상 없음. 깨어나지 않을 이유가 없다는 소견.\n" +
                  "매일 00:17 무렵 심박이 130까지 치솟았다가 5분 안에 제자리로 돌아온다.\n" +
                  "동생 하윤과 같은 시각, 같은 파형.");

        // ---------- 동생 : 서하윤 (2번 침대) ----------
        FillPatient(hayun,
            id: "P-26-0118",
            name: "서하윤",
            bedNumber: 2,
            elderTwin: false,
            heartRate: 57, systolic: 90, diastolic: 58, temperature: 35.8f, oxygen: 96,
            note: "언니 하린과 같은 날 같은 증상으로 실려 왔다.\n" +
                  "접수 당시 동행인 미상. 이후 보호자 면회 기록 없음.\n" +
                  "손을 떼어 놓으면 두 사람 모두 심박이 흐트러져, 침대를 붙여 두었다.");

        // 서로를 쌍둥이로 가리키게 한다.
        SetObjectReference(harin, "twinSibling", hayun);
        SetObjectReference(hayun, "twinSibling", harin);

        // ---------- 302호 ----------
        var roomSerialized = new SerializedObject(room);
        roomSerialized.FindProperty("roomNumber").stringValue = "302";
        roomSerialized.FindProperty("roomName").stringValue   = "소아 중환자 병동";
        roomSerialized.FindProperty("ward").stringValue       = "본관 3층";
        roomSerialized.FindProperty("bedCount").intValue      = 3;

        var beds = roomSerialized.FindProperty("beds");
        beds.arraySize = 3;

        SetBedAssignment(beds.GetArrayElementAtIndex(0), 1, harin);
        SetBedAssignment(beds.GetArrayElementAtIndex(1), 2, hayun);
        SetBedAssignment(beds.GetArrayElementAtIndex(2), 3, null);   // 빈 침대

        roomSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(room);
    }

    /// <summary>쌍둥이 둘이 공통으로 갖는 값 + 개별 값을 한 번에 채운다.</summary>
    private static void FillPatient(PatientData patient, string id, string name, int bedNumber,
                                    bool elderTwin, int heartRate, int systolic, int diastolic,
                                    float temperature, int oxygen, string note)
    {
        var serialized = new SerializedObject(patient);

        serialized.FindProperty("patientId").stringValue   = id;
        serialized.FindProperty("patientName").stringValue = name;
        serialized.FindProperty("age").intValue            = 12;
        serialized.FindProperty("gender").enumValueIndex   = (int)PatientData.Gender.Female;
        serialized.FindProperty("bloodType").stringValue   = "AB형 Rh+";

        serialized.FindProperty("roomNumber").stringValue      = "302";
        serialized.FindProperty("bedNumber").intValue          = bedNumber;
        serialized.FindProperty("admissionDate").stringValue   = "2026-08-21";
        serialized.FindProperty("diagnosis").stringValue       = "원인 미상 의식 저하 (R40.2)";
        serialized.FindProperty("attendingDoctor").stringValue = "윤재경";
        serialized.FindProperty("consciousness").enumValueIndex = (int)PatientData.Consciousness.Coma;

        var vitals = serialized.FindProperty("vitals");
        vitals.FindPropertyRelative("heartRate").intValue          = heartRate;
        vitals.FindPropertyRelative("systolic").intValue           = systolic;
        vitals.FindPropertyRelative("diastolic").intValue          = diastolic;
        vitals.FindPropertyRelative("temperature").floatValue      = temperature;
        vitals.FindPropertyRelative("oxygenSaturation").intValue   = oxygen;

        serialized.FindProperty("isElderTwin").boolValue = elderTwin;

        // 일란성이라 겉모습은 똑같다.
        serialized.FindProperty("hairColor").colorValue = new Color(0.14f, 0.10f, 0.09f, 1f);
        serialized.FindProperty("skinColor").colorValue = new Color(0.89f, 0.80f, 0.76f, 1f);

        serialized.FindProperty("chartNote").stringValue = note;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(patient);
    }

    private static void SetBedAssignment(SerializedProperty element, int bedNumber, PatientData patient)
    {
        element.FindPropertyRelative("bedNumber").intValue = bedNumber;
        element.FindPropertyRelative("patient").objectReferenceValue = patient;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T CreateOrLoad<T>(string folder, string assetName) where T : ScriptableObject
    {
        BuildUtil.EnsureFolder(folder);

        string path = $"{folder}/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    // ============================================================
    // 환경 설정
    // ============================================================

    private static void BuildEnvironmentSettings()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.33f, 0.37f);
        RenderSettings.fog          = false;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
    }

    // ============================================================
    // 방
    // ============================================================

    private static void BuildRoom()
    {
        var root = new GameObject("Room").transform;

        float innerW = RoomHalfX * 2f;
        float innerD = RoomHalfZ * 2f;
        float outerW = innerW + WallT * 2f;
        float outerD = innerD + WallT * 2f;

        // ---------- 바닥 / 천장 ----------
        BuildUtil.Box("Floor",   root, new Vector3(0f, -0.05f, 0f),
                      new Vector3(outerW, 0.10f, outerD), _floor, true);
        BuildUtil.Box("Ceiling", root, new Vector3(0f, RoomHeight + 0.05f, 0f),
                      new Vector3(outerW, 0.10f, outerD), _ceiling, true);

        // ---------- 벽 ----------
        BuildUtil.Box("Wall_Back",  root, new Vector3(0f, RoomHeight * 0.5f, RoomHalfZ + WallT * 0.5f),
                      new Vector3(outerW, RoomHeight, WallT), _wall, true);
        BuildUtil.Box("Wall_Front", root, new Vector3(0f, RoomHeight * 0.5f, -RoomHalfZ - WallT * 0.5f),
                      new Vector3(outerW, RoomHeight, WallT), _wall, true);
        BuildUtil.Box("Wall_Right", root, new Vector3(RoomHalfX + WallT * 0.5f, RoomHeight * 0.5f, 0f),
                      new Vector3(WallT, RoomHeight, innerD), _wall, true);

        // ---------- 왼쪽 벽 : 창문 구멍을 남기고 네 조각으로 ----------
        float leftX = -RoomHalfX - WallT * 0.5f;
        float sideDepth = RoomHalfZ + WallT - WindowHalfZ;
        float sideCenterZ = (WindowHalfZ + RoomHalfZ + WallT) * 0.5f;

        BuildUtil.Box("Wall_Left_Lower", root, new Vector3(leftX, WindowBottom * 0.5f, 0f),
                      new Vector3(WallT, WindowBottom, outerD), _wall, true);
        BuildUtil.Box("Wall_Left_Upper", root, new Vector3(leftX, (WindowTop + RoomHeight) * 0.5f, 0f),
                      new Vector3(WallT, RoomHeight - WindowTop, outerD), _wall, true);
        BuildUtil.Box("Wall_Left_SideBack", root, new Vector3(leftX, (WindowBottom + WindowTop) * 0.5f, sideCenterZ),
                      new Vector3(WallT, WindowTop - WindowBottom, sideDepth), _wall, true);
        BuildUtil.Box("Wall_Left_SideFront", root, new Vector3(leftX, (WindowBottom + WindowTop) * 0.5f, -sideCenterZ),
                      new Vector3(WallT, WindowTop - WindowBottom, sideDepth), _wall, true);

        // ---------- 나무 굽도리 (사진처럼 벽 아래쪽에 두른 나무 띠) ----------
        BuildUtil.Box("Wainscot_Back",  root, new Vector3(0f, 0.50f, RoomHalfZ - 0.025f),
                      new Vector3(innerW, 1.00f, 0.05f), _wainscot);
        BuildUtil.Box("Wainscot_Front", root, new Vector3(0f, 0.50f, -RoomHalfZ + 0.025f),
                      new Vector3(innerW, 1.00f, 0.05f), _wainscot);
        BuildUtil.Box("Wainscot_Right", root, new Vector3(RoomHalfX - 0.025f, 0.50f, 0f),
                      new Vector3(0.05f, 1.00f, innerD), _wainscot);

        // ---------- 침대 사이를 나누는 나무 기둥 ----------
        float[] columnX = { -3.95f, -1.20f, 1.20f, 3.95f };
        for (int i = 0; i < columnX.Length; i++)
        {
            BuildUtil.Box($"Column_{i + 1}", root, new Vector3(columnX[i], RoomHeight * 0.5f, RoomHalfZ - 0.07f),
                          new Vector3(0.20f, RoomHeight, 0.14f), _wood);
        }

        BuildWindow(root);
        BuildDoor(root);
    }

    private static void BuildWindow(Transform parent)
    {
        var root = BuildUtil.Empty("Window", parent, Vector3.zero).transform;

        float frameX = -RoomHalfX - 0.02f;
        float centerY = (WindowBottom + WindowTop) * 0.5f;
        float height  = WindowTop - WindowBottom;

        // 창틀
        BuildUtil.Box("Frame_Top",   root, new Vector3(frameX, WindowTop + 0.05f, 0f),
                      new Vector3(0.22f, 0.10f, WindowHalfZ * 2f + 0.2f), _wall);
        BuildUtil.Box("Frame_Back",  root, new Vector3(frameX, centerY, WindowHalfZ + 0.05f),
                      new Vector3(0.22f, height, 0.10f), _wall);
        BuildUtil.Box("Frame_Front", root, new Vector3(frameX, centerY, -WindowHalfZ - 0.05f),
                      new Vector3(0.22f, height, 0.10f), _wall);

        // 창턱 (방 안쪽으로 살짝 튀어나온 선반)
        BuildUtil.Box("Sill", root, new Vector3(-RoomHalfX + 0.10f, WindowBottom - 0.04f, 0f),
                      new Vector3(0.36f, 0.08f, WindowHalfZ * 2f + 0.2f), _wood, true);

        // 세로 창살
        float[] mullionZ = { -0.87f, 0.87f };
        for (int i = 0; i < mullionZ.Length; i++)
        {
            BuildUtil.Box($"Mullion_{i + 1}", root, new Vector3(frameX, centerY, mullionZ[i]),
                          new Vector3(0.16f, height, 0.07f), _wall);
        }

        // 유리
        BuildUtil.Box("Glass", root, new Vector3(-RoomHalfX - 0.05f, centerY, 0f),
                      new Vector3(0.03f, height - 0.02f, WindowHalfZ * 2f - 0.02f), _glass);

        // 반쯤 젖혀 둔 버티컬 블라인드
        for (int i = 0; i < 4; i++)
        {
            float z = 1.88f + i * 0.19f;
            var slat = BuildUtil.Box($"Blind_{i + 1}", root, new Vector3(-RoomHalfX + 0.14f, centerY + 0.02f, z),
                                     new Vector3(0.02f, height - 0.06f, 0.11f), _curtain);
            slat.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
        }
    }

    private static void BuildDoor(Transform parent)
    {
        var root = BuildUtil.Empty("Door", parent, Vector3.zero).transform;

        BuildUtil.Box("Panel", root, new Vector3(3.10f, 1.05f, -RoomHalfZ + 0.04f),
                      new Vector3(1.10f, 2.10f, 0.08f), _woodDark, true);
        BuildUtil.Box("Frame", root, new Vector3(3.10f, 1.10f, -RoomHalfZ + 0.015f),
                      new Vector3(1.24f, 2.20f, 0.05f), _wood);
        BuildUtil.Box("Window", root, new Vector3(3.10f, 1.62f, -RoomHalfZ + 0.09f),
                      new Vector3(0.44f, 0.52f, 0.03f), _glass);
        BuildUtil.Sphere("Handle", root, new Vector3(2.66f, 1.00f, -RoomHalfZ + 0.10f),
                         new Vector3(0.07f, 0.07f, 0.07f), _metal);
    }

    // ============================================================
    // 창밖 야경
    // ============================================================

    private static void BuildCityView()
    {
        var root = new GameObject("CityView").transform;

        // 밤하늘
        BuildUtil.Box("SkyBackdrop", root, new Vector3(-42f, 8f, 0f),
                      new Vector3(0.5f, 64f, 120f), _nightSky);

        // 건물들 (같은 모양이 나오도록 씨앗을 고정해 둔다)
        Random.InitState(20260915);

        for (int i = 0; i < 14; i++)
        {
            float x = Random.Range(-30f, -9f);
            float z = Random.Range(-17f, 17f);
            float width = Random.Range(2.6f, 5.8f);
            float depth = Random.Range(2.6f, 5.8f);
            float top   = Random.Range(-5.0f, 7.0f);

            float bottom = -30f;
            float height = top - bottom;
            float centerY = (top + bottom) * 0.5f;

            var building = BuildUtil.Box($"Building_{i + 1:00}", root, new Vector3(x, centerY, z),
                                        new Vector3(width, height, depth), _building);

            // 방 쪽(+X)을 보는 면에 창문 불빛 몇 줄
            int bandCount = Random.Range(2, 5);
            for (int b = 0; b < bandCount; b++)
            {
                float bandY = top - 1.4f - b * Random.Range(1.6f, 2.6f);

                BuildUtil.Box($"Windows_{b + 1}", building.transform,
                              new Vector3(0.5f + 0.01f, (bandY - centerY) / height, 0f),
                              new Vector3(0.02f / width, 0.30f / height, 0.78f), _buildingLit);
            }
        }
    }

    // ============================================================
    // 조명
    // ============================================================

    private static void BuildLighting()
    {
        var root = new GameObject("Lighting").transform;

        // 창문으로 들어오는 푸른 달빛
        var moonGO = new GameObject("Directional Light (Moon)", typeof(Light));
        moonGO.transform.SetParent(root, false);
        moonGO.transform.SetPositionAndRotation(new Vector3(-3f, 2.4f, 0f), Quaternion.Euler(14f, 76f, 0f));

        var moon = moonGO.GetComponent<Light>();
        moon.type       = LightType.Directional;
        moon.color      = new Color(0.62f, 0.72f, 1.00f);
        moon.intensity  = 0.55f;
        moon.shadows    = LightShadows.Soft;
        moon.shadowStrength = 0.55f;

        // 천장 형광등
        Vector3[] panelPositions =
        {
            new Vector3(-2.60f, 2.93f,  1.20f),
            new Vector3( 0.20f, 2.93f, -0.70f),
            new Vector3( 2.60f, 2.93f,  1.20f),
        };

        for (int i = 0; i < panelPositions.Length; i++)
        {
            var position = panelPositions[i];

            BuildUtil.Box($"CeilingPanel_{i + 1}", root, position,
                          new Vector3(1.20f, 0.06f, 0.36f), _lightPanel);

            var lightGO = new GameObject($"CeilingLight_{i + 1}", typeof(Light));
            lightGO.transform.SetParent(root, false);
            lightGO.transform.localPosition = position + Vector3.down * 0.15f;

            var light = lightGO.GetComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1.00f, 0.96f, 0.89f);
            light.intensity = 1.05f;
            light.range     = 8.5f;
            light.shadows   = LightShadows.None;
        }

        // 침대 머리맡 무드등 (사진처럼 벽 쪽만 은은하게)
        for (int i = 0; i < BedCenterX.Length; i++)
        {
            var lightGO = new GameObject($"BedLight_{i + 1}", typeof(Light));
            lightGO.transform.SetParent(root, false);
            lightGO.transform.localPosition = new Vector3(BedCenterX[i], 1.95f, RoomHalfZ - 0.45f);

            var light = lightGO.GetComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1.00f, 0.90f, 0.74f);
            light.intensity = 0.55f;
            light.range     = 3.2f;
            light.shadows   = LightShadows.None;
        }
    }

    // ============================================================
    // 가구 / 소품
    // ============================================================

    private static void BuildProps()
    {
        var root = new GameObject("Props").transform;

        // 침대 머리맡 벽 수납 유닛
        float[] unitX = { -3.80f, -0.55f, 3.25f };
        for (int i = 0; i < unitX.Length; i++) MakeHeadUnit(root, i + 1, unitX[i]);

        // 링거대 : 1번 침대 바깥쪽, 2번 침대 바깥쪽 (붙어 있는 사이에는 못 놓는다)
        MakeIVStand(root, 1, new Vector3(-3.45f, 0f, 2.85f));
        MakeIVStand(root, 2, new Vector3(-1.05f, 0f, 2.85f));

        // 오버베드 테이블
        MakeOverbedTable(root, 1, new Vector3(-3.60f, 0f, 1.45f), 12f);
        MakeOverbedTable(root, 2, new Vector3(2.15f, 0f, 1.05f), -8f);

        // 바퀴 달린 둥근 스툴 (사진 앞쪽에 있는 것)
        MakeStool(root, new Vector3(-0.30f, 0f, 0.55f));

        // 창가 보호자 소파
        MakeSofa(root, new Vector3(-3.85f, 0f, -1.85f));

        // 3번 침대를 가리는 커튼
        MakeCurtain(root, 1.20f);
    }

    private static void MakeHeadUnit(Transform parent, int index, float centerX)
    {
        var root = BuildUtil.Empty($"HeadUnit_{index}", parent, new Vector3(centerX, 0f, 0f)).transform;

        float wallZ = RoomHalfZ - 0.05f;

        // 뒤판
        BuildUtil.Box("Backboard", root, new Vector3(0f, 1.70f, wallZ - 0.02f),
                      new Vector3(0.94f, 1.70f, 0.05f), _wood);

        // 서랍장
        BuildUtil.Box("Cabinet", root, new Vector3(0f, 0.42f, wallZ - 0.26f),
                      new Vector3(0.88f, 0.84f, 0.46f), _wood, true);
        BuildUtil.Box("DrawerLine", root, new Vector3(0f, 0.52f, wallZ - 0.50f),
                      new Vector3(0.80f, 0.02f, 0.02f), _woodDark);
        BuildUtil.Box("Handle", root, new Vector3(0f, 0.22f, wallZ - 0.50f),
                      new Vector3(0.24f, 0.03f, 0.03f), _metal);

        // 위쪽 선반
        BuildUtil.Box("Shelf", root, new Vector3(0f, 1.42f, wallZ - 0.18f),
                      new Vector3(0.90f, 0.05f, 0.30f), _wood, true);

        // 의료용 콘센트 패널
        BuildUtil.Box("OutletPanel", root, new Vector3(0f, 1.05f, wallZ - 0.06f),
                      new Vector3(0.52f, 0.14f, 0.03f), _bedRail);
    }

    private static void MakeIVStand(Transform parent, int index, Vector3 position)
    {
        var root = BuildUtil.Empty($"IVStand_{index}", parent, position).transform;

        BuildUtil.Cylinder("Base", root, new Vector3(0f, 0.02f, 0f), 0.44f, 0.04f, _metal);
        BuildUtil.Cylinder("Pole", root, new Vector3(0f, 0.95f, 0f), 0.035f, 1.90f, _metal, true);
        BuildUtil.Box("HookBar", root, new Vector3(0f, 1.88f, 0f),
                      new Vector3(0.30f, 0.025f, 0.025f), _metal);

        // 수액 봉지
        BuildUtil.Box("IVBag", root, new Vector3(0.13f, 1.68f, 0f),
                      new Vector3(0.14f, 0.26f, 0.05f), _glass);

        // 바퀴
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            BuildUtil.Sphere($"Caster_{i + 1}", root,
                             new Vector3(Mathf.Cos(angle) * 0.20f, 0.03f, Mathf.Sin(angle) * 0.20f),
                             new Vector3(0.06f, 0.06f, 0.06f), _rubber);
        }
    }

    private static void MakeOverbedTable(Transform parent, int index, Vector3 position, float yaw)
    {
        var root = BuildUtil.Empty($"OverbedTable_{index}", parent, position).transform;
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);

        BuildUtil.Box("Top", root, new Vector3(0f, 0.86f, 0f),
                      new Vector3(0.78f, 0.04f, 0.44f), _wood, true);
        BuildUtil.Cylinder("Column", root, new Vector3(0f, 0.44f, 0f), 0.06f, 0.84f, _metal, true);
        BuildUtil.Box("Foot", root, new Vector3(0f, 0.03f, 0.10f),
                      new Vector3(0.50f, 0.05f, 0.34f), _metal);

        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -0.20f : 0.20f;
            float z = (i < 2) ? -0.04f : 0.22f;
            BuildUtil.Sphere($"Caster_{i + 1}", root, new Vector3(x, 0.025f, z),
                             new Vector3(0.05f, 0.05f, 0.05f), _rubber);
        }
    }

    private static void MakeStool(Transform parent, Vector3 position)
    {
        var root = BuildUtil.Empty("Stool", parent, position).transform;

        BuildUtil.Cylinder("Seat", root, new Vector3(0f, 0.46f, 0f), 0.42f, 0.09f, _fabric, true);
        BuildUtil.Cylinder("Column", root, new Vector3(0f, 0.24f, 0f), 0.06f, 0.44f, _metal, true);

        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            var leg = BuildUtil.Box($"Leg_{i + 1}", root,
                                    new Vector3(Mathf.Cos(angle) * 0.13f, 0.05f, Mathf.Sin(angle) * 0.13f),
                                    new Vector3(0.26f, 0.03f, 0.05f), _bedRail);
            leg.transform.localRotation = Quaternion.Euler(0f, -i * 72f, 0f);

            BuildUtil.Sphere($"Caster_{i + 1}", root,
                             new Vector3(Mathf.Cos(angle) * 0.24f, 0.03f, Mathf.Sin(angle) * 0.24f),
                             new Vector3(0.055f, 0.055f, 0.055f), _rubber);
        }
    }

    private static void MakeSofa(Transform parent, Vector3 position)
    {
        var root = BuildUtil.Empty("Sofa", parent, position).transform;
        root.localRotation = Quaternion.Euler(0f, 90f, 0f);

        BuildUtil.Box("Seat",    root, new Vector3(0f, 0.40f, 0f),  new Vector3(1.30f, 0.20f, 0.66f), _fabric, true);
        BuildUtil.Box("Back",    root, new Vector3(0f, 0.66f, -0.28f), new Vector3(1.30f, 0.72f, 0.14f), _fabric, true);
        BuildUtil.Box("ArmL",    root, new Vector3(-0.64f, 0.48f, 0f), new Vector3(0.12f, 0.36f, 0.66f), _fabric);
        BuildUtil.Box("ArmR",    root, new Vector3(0.64f, 0.48f, 0f),  new Vector3(0.12f, 0.36f, 0.66f), _fabric);

        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -0.56f : 0.56f;
            float z = (i < 2) ? -0.24f : 0.24f;
            BuildUtil.Box($"Leg_{i + 1}", root, new Vector3(x, 0.15f, z),
                          new Vector3(0.07f, 0.30f, 0.07f), _woodDark);
        }
    }

    private static void MakeCurtain(Transform parent, float x)
    {
        var root = BuildUtil.Empty("Curtain", parent, Vector3.zero).transform;

        // 3번 침대 쪽을 가리는 커튼. 방 한가운데를 막지 않도록 반쯤 젯혀 둔다.
        BuildUtil.Box("Rail", root, new Vector3(x, 2.88f, 2.20f),
                      new Vector3(0.05f, 0.05f, 2.40f), _metal);
        BuildUtil.Box("Cloth", root, new Vector3(x, 1.85f, 2.72f),
                      new Vector3(0.03f, 2.00f, 1.30f), _curtain);
    }

    // ============================================================
    // 침대 + 환자
    // ============================================================

    private static List<BedSlot> BuildBedsAndPatients(PatientData harin, PatientData hayun)
    {
        var slots = new List<BedSlot>();

        // 1번 : 왼쪽 (언니 하린). 오른쪽 난간은 내려져 있다 → 2번 침대와 맞닿는 쪽
        var bed1 = MakeBed(1, BedCenterX[0], leftRail: true, rightRail: false);
        // 2번 : 가운데 (동생 하윤). 왼쪽 난간이 내려져 있다
        var bed2 = MakeBed(2, BedCenterX[1], leftRail: false, rightRail: true);
        // 3번 : 빈 침대
        var bed3 = MakeBed(3, BedCenterX[2], leftRail: true, rightRail: true);

        // 환자는 1번 / 2번에만 눕힌다. (서로 마주 보는 쪽 팔을 뻗어 손을 맞댄다)
        MakePatient(bed1, harin, innerSide: +1f, armZ: 0.30f);
        MakePatient(bed2, hayun, innerSide: -1f, armZ: 0.26f);

        // ---------- 왼쪽 아이(하린)에게만 [E] 손대기 를 붙인다 ----------
        var touch = bed1.patientRoot.AddComponent<PatientTouchInteractable>();

        var touchSerialized = new SerializedObject(touch);
        touchSerialized.FindProperty("displayName").stringValue   = "";   // 이름은 환자 데이터에서 자동으로 가져온다
        touchSerialized.FindProperty("actionLabel").stringValue   = "손대기";
        touchSerialized.FindProperty("interactRange").floatValue  = 2.0f;
        touchSerialized.FindProperty("bedSlot").objectReferenceValue    = bed1.slot;
        touchSerialized.FindProperty("twitchTarget").objectReferenceValue = bed1.innerArm;
        touchSerialized.FindProperty("twitchAngle").floatValue    = 11f;
        touchSerialized.FindProperty("twitchDuration").floatValue = 0.85f;
        touchSerialized.FindProperty("logChartOnTouch").boolValue = true;
        touchSerialized.ApplyModifiedPropertiesWithoutUndo();

        slots.Add(bed1.slot);
        slots.Add(bed2.slot);
        slots.Add(bed3.slot);
        return slots;
    }

    /// <summary>MakeBed / MakePatient 가 주고받는 부품 묶음.</summary>
    private class BedParts
    {
        public BedSlot slot;
        public Transform root;
        public GameObject patientRoot;
        public Renderer head;
        public Renderer hair;
        public Transform innerArm;
        public Text nameplate;
    }

    private static BedParts MakeBed(int bedNumber, float centerX, bool leftRail, bool rightRail)
    {
        var go = new GameObject($"Bed_{bedNumber:00}");
        go.transform.position = new Vector3(centerX, 0f, BedCenterZ);

        var root = go.transform;
        var parts = new BedParts { root = root };

        float halfW = BedWidth * 0.5f;
        float halfL = BedLength * 0.5f;

        // ---------- 다리 / 바퀴 ----------
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -0.38f : 0.38f;
            float z = (i < 2) ? -0.86f : 0.86f;

            BuildUtil.Box($"Leg_{i + 1}", root, new Vector3(x, 0.26f, z),
                          new Vector3(0.07f, 0.36f, 0.07f), _metal);
            BuildUtil.Cylinder($"Caster_{i + 1}", root, new Vector3(x, 0.05f, z),
                               0.11f, 0.07f, _rubber);
        }

        // ---------- 프레임 / 매트리스 ----------
        BuildUtil.Box("FrameBase", root, new Vector3(0f, 0.49f, 0f),
                      new Vector3(BedWidth, 0.14f, BedLength), _bedFrame, true);
        BuildUtil.Box("Mattress", root, new Vector3(0f, 0.65f, 0f),
                      new Vector3(BedWidth - 0.06f, 0.18f, BedLength - 0.10f), _mattress, true);

        // ---------- 침구 ----------
        BuildUtil.Box("Pillow", root, new Vector3(0f, 0.775f, 0.76f),
                      new Vector3(0.58f, 0.11f, 0.34f), _pillow);
        BuildUtil.Box("Blanket", root, new Vector3(0f, 0.775f, -0.29f),
                      new Vector3(BedWidth - 0.02f, 0.11f, 1.42f), _blanket);
        BuildUtil.Box("BlanketFold", root, new Vector3(0f, 0.805f, 0.41f),
                      new Vector3(BedWidth, 0.07f, 0.14f), _pillow);
        BuildUtil.Box("FeetBump", root, new Vector3(0f, 0.835f, -0.78f),
                      new Vector3(0.34f, 0.10f, 0.26f), _blanket);

        // ---------- 헤드보드 / 풋보드 ----------
        BuildUtil.Box("Headboard", root, new Vector3(0f, 0.88f, halfL - 0.015f),
                      new Vector3(BedWidth + 0.02f, 0.52f, 0.07f), _bedFrame, true);
        BuildUtil.Box("Footboard", root, new Vector3(0f, 0.82f, -halfL + 0.015f),
                      new Vector3(BedWidth + 0.02f, 0.44f, 0.07f), _bedFrame, true);

        // ---------- 옆 난간 ----------
        // 붙여 놓은 두 침대 사이는 난간을 내려 둔다. (그래야 손이 닿는다)
        if (leftRail)  MakeRail(root, "Rail_L", -halfW - 0.015f);
        if (rightRail) MakeRail(root, "Rail_R", halfW + 0.015f);

        // ---------- 발치 이름표 ----------
        parts.nameplate = MakeNameplate(root, bedNumber);

        // ---------- BedSlot ----------
        var slot = go.AddComponent<BedSlot>();
        parts.slot = slot;

        var slotSerialized = new SerializedObject(slot);
        slotSerialized.FindProperty("bedNumber").intValue = bedNumber;
        slotSerialized.FindProperty("nameplateText").objectReferenceValue = parts.nameplate;
        slotSerialized.ApplyModifiedPropertiesWithoutUndo();
        return parts;
    }

    private static void MakeRail(Transform parent, string name, float x)
    {
        var rail = BuildUtil.Empty(name, parent, new Vector3(x, 0f, 0f)).transform;

        BuildUtil.Box("Bar_Top", rail, new Vector3(0f, 1.00f, 0.20f),
                      new Vector3(0.04f, 0.05f, 1.00f), _bedRail, true);
        BuildUtil.Box("Panel", rail, new Vector3(0f, 0.87f, 0.20f),
                      new Vector3(0.03f, 0.24f, 0.94f), _bedRail, true);
        BuildUtil.Box("Post_A", rail, new Vector3(0f, 0.78f, -0.26f),
                      new Vector3(0.04f, 0.30f, 0.04f), _metal);
        BuildUtil.Box("Post_B", rail, new Vector3(0f, 0.78f, 0.66f),
                      new Vector3(0.04f, 0.30f, 0.04f), _metal);
    }

    /// <summary>침대 발치에 붙는 주황색 이름표 카드 + 글자.</summary>
    private static Text MakeNameplate(Transform parent, int bedNumber)
    {
        float z = -BedLength * 0.5f - 0.03f;

        BuildUtil.Box("NameplateCard", parent, new Vector3(0f, 0.80f, z),
                      new Vector3(0.46f, 0.22f, 0.02f), _nameplate);

        var canvasGO = new GameObject("Nameplate", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(parent, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta      = new Vector2(240f, 110f);
        canvasRT.localScale     = Vector3.one * 0.0017f;
        canvasRT.localPosition  = new Vector3(0f, 0.80f, z - 0.015f);
        canvasRT.localRotation  = Quaternion.Euler(0f, 180f, 0f);   // 발치 바깥쪽을 보게

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(canvasRT, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var text = textGO.GetComponent<Text>();
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text      = bedNumber.ToString();
        text.fontSize  = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = new Color(0.14f, 0.11f, 0.07f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow   = VerticalWrapMode.Overflow;

        return text;
    }

    /// <summary>
    /// 침대 위에 누워 있는 아이를 만든다.
    /// innerSide 가 +1 이면 오른쪽(+X)으로, -1 이면 왼쪽(-X)으로 팔을 뻗어 옆 침대 아이와 손을 맞댄다.
    /// </summary>
    private static void MakePatient(BedParts bed, PatientData patient, float innerSide, float armZ)
    {
        float outerSide = -innerSide;

        // 환자 뿌리를 몸통 높이에 둔다. (거리 계산과 상호작용 판정이 자연스러워진다)
        var patientGO = BuildUtil.Empty("Patient", bed.root, new Vector3(0f, 0.80f, 0.30f));
        var root = patientGO.transform;
        bed.patientRoot = patientGO;

        // 상호작용용 판정 상자
        var collider = patientGO.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.05f, -0.15f);
        collider.size   = new Vector3(0.90f, 0.45f, 1.70f);
        collider.isTrigger = true;

        // ---------- 머리카락 ----------
        // 누워 있으므로 얼굴은 위(+Y)를 향한다.
        // 머리카락을 먼저 낮게 깔고 그 위로 얼굴을 올려야 얼굴이 보인다.
        var hair = BuildUtil.Sphere("Hair", root, new Vector3(0f, 0.055f, 0.465f),
                                    new Vector3(0.250f, 0.240f, 0.250f), _hair);
        bed.hair = hair.GetComponent<Renderer>();

        // 베개 위로 퍼진 머리
        BuildUtil.Sphere("HairSpread", root, new Vector3(0f, -0.005f, 0.510f),
                         new Vector3(0.40f, 0.10f, 0.38f), _hair);
        BuildUtil.Sphere("HairSide_L", root, new Vector3(-0.118f, 0.025f, 0.440f),
                         new Vector3(0.085f, 0.140f, 0.270f), _hair);
        BuildUtil.Sphere("HairSide_R", root, new Vector3(0.118f, 0.025f, 0.440f),
                         new Vector3(0.085f, 0.140f, 0.270f), _hair);

        // ---------- 얼굴 ----------
        var head = BuildUtil.Sphere("Head", root, new Vector3(0f, 0.085f, 0.405f),
                                    new Vector3(0.205f, 0.235f, 0.235f), _skin);
        bed.head = head.GetComponent<Renderer>();

        // 앞머리
        BuildUtil.Sphere("Bangs", root, new Vector3(0f, 0.170f, 0.462f),
                         new Vector3(0.21f, 0.09f, 0.14f), _hair);

        // 감은 눈
        BuildUtil.Box("Eye_L", root, new Vector3(-0.047f, 0.183f, 0.415f),
                      new Vector3(0.050f, 0.022f, 0.020f), _hair);
        BuildUtil.Box("Eye_R", root, new Vector3(0.047f, 0.183f, 0.415f),
                      new Vector3(0.050f, 0.022f, 0.020f), _hair);

        // ---------- 환자복 어깨 (이불 위로 나온 부분) ----------
        BuildUtil.Box("Gown", root, new Vector3(0f, -0.01f, 0.205f),
                      new Vector3(0.50f, 0.15f, 0.25f), _gown);

        // ---------- 바깥쪽 팔 : 몸에 붙여 내려 둔다 ----------
        var outerArm = BuildUtil.Capsule("Arm_Outer", root, new Vector3(outerSide * 0.275f, 0.03f, -0.13f),
                                         0.085f, 0.50f, _gown);
        outerArm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        BuildUtil.Sphere("Hand_Outer", root, new Vector3(outerSide * 0.275f, 0.03f, -0.40f),
                         new Vector3(0.085f, 0.075f, 0.085f), _skin);

        // ---------- 안쪽 팔 : 옆 침대 아이 쪽으로 뻗어 손을 맞댄다 ----------
        var innerArmGO = BuildUtil.Empty("Arm_Inner", root,
                                         new Vector3(innerSide * 0.20f, 0.03f, armZ - 0.30f));
        var innerArm = innerArmGO.transform;
        bed.innerArm = innerArm;

        var innerArmMesh = BuildUtil.Capsule("Upper", innerArm, new Vector3(innerSide * 0.11f, 0f, 0f),
                                             0.085f, 0.40f, _gown);
        innerArmMesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        BuildUtil.Sphere("Hand_Inner", innerArm, new Vector3(innerSide * 0.30f, 0f, 0.01f),
                         new Vector3(0.085f, 0.075f, 0.085f), _skin);

        // ---------- BedSlot 에 연결 ----------
        var slotSerialized = new SerializedObject(bed.slot);
        slotSerialized.FindProperty("patientVisual").objectReferenceValue = patientGO;
        slotSerialized.FindProperty("headRenderer").objectReferenceValue  = bed.head;
        slotSerialized.FindProperty("hairRenderer").objectReferenceValue  = bed.hair;
        slotSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ============================================================
    // 마무리
    // ============================================================

    private static void RegisterInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool alreadyThere = scenes.Exists(s => s.path == ScenePath);
        if (!alreadyThere)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
