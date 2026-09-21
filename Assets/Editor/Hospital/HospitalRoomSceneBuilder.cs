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
    private const string HospitalBgmPath = "Assets/Resources/Audio/Hospital/WhispersOfNight.mp3";
    private const string Dream1ScenePath = "Assets/Scenes/SD_BrightDream_Blockout_Rect.unity";
    private const string Dream1SceneName = "SD_BrightDream_Blockout_Rect";

    // 침대 옆에 세우는 사물함 모델 (다운로드받은 FBX).
    // 원본이 높이 1m 기준으로 만들어져 있고 피벗이 가운데에 있어서,
    // 배율 = 목표 높이, 높이의 절반만큼 올려 놓으면 바닥에 딱 닿는다.
    private const string CabinetModelPath = "Assets/Art/Models/Locker.fbx";
    private const float  CabinetHeight    = 1.55f;

    // 3번(빈) 침대에 쓰는 모델. 머리맛이 +Z 를 보고 있어서 추가 회전이 필요 없다.
    // ============================================================
    // 모델 에셋 경로
    // ============================================================
    private const string BedModelPath      = "Assets/Art/Models/EmptyBed.fbx";
    private const string GirlModelPath     = "Assets/Art/Models/GirlSleeping.glb";
    private const string BlanketModelPath  = "Assets/Art/Models/Blanket.glb";
    private const string IVPoleModelPath   = "Assets/Art/Models/IVPole.glb";
    private const string StoolModelPath    = "Assets/Art/Models/Stool.glb";
    private const string LoveseatModelPath = "Assets/Art/Models/Loveseat.glb";
    private const string CurtainModelPath  = "Assets/Art/Models/PrivacyCurtain.fbx";
    private const string DoorModelPath     = "Assets/Art/Models/Door.fbx";

    // ============================================================
    // 손으로 맞춘 배치값
    //
    // 아래 숫자들은 씨에서 직접 잡은 위치를 그대로 적어 둔 것이다.
    // 자동으로 맞추게 하면 손으로 맞춘 구도가 틀어지므로 건드리지 말 것.
    // 배치를 바꾸고 싶으면 씨에서 옥기고 그 값을 여기 다시 적는다.
    // ============================================================

    // 침대 모델 (침대 뿌리 기준 로컬 값)
    private static readonly Vector3 BedModelEuler = new Vector3(270f, 182.85f, 0f);
    private static readonly Vector3[] BedModelLocalPos =
    {
        new Vector3(0.4408f, 0.6100f, -0.0218f),
        new Vector3(0.9240f, 0.6100f, -0.0330f),
        new Vector3(0.0000f, 0.6076f,  0.0000f),
    };
    private static readonly float[] BedModelScale = { 2.10000f, 2.10000f, 2.10069f };

    // 사물함 (HeadUnit 뿌리 기준 로컬 값)
    private static readonly Vector3 CabinetEuler = new Vector3(270f, 180f, 0f);
    private static readonly Vector3[] CabinetLocalPos =
    {
        new Vector3(-0.0634f, 0.9092f, 3.1891f),
        new Vector3( 1.0700f, 0.9052f, 3.1891f),
        new Vector3( 0.0747f, 0.8940f, 3.1891f),
    };
    private static readonly Vector3[] CabinetLocalScale =
    {
        new Vector3(1.88110f, 1.54925f, 1.81711f),
        new Vector3(1.91038f, 1.54925f, 1.80921f),
        new Vector3(1.93904f, 1.54925f, 1.78659f),
    };





    // 링거대 (IVStand 뿌리 기준 로컬 값)
    private static readonly Vector3[] IVPoleLocalPos =
    {
        new Vector3(0.2230f, 0.9500f, -0.1450f),
        new Vector3(1.5330f, 0.9500f, -0.4950f),
    };
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
        // 이 메뉴는 씨을 코드로 통째로 다시 만든다.
        // 씨에서 손으로 옥기거나 지운 것은 전부 사라지므로 반드시 물어본다.
        bool confirmed = EditorUtility.DisplayDialog(
            "병실 씨을 다시 만드시겠습니까?",
            "HospitalRoom.unity 를 빌더 코드대로 통째로 다시 만듭니다.\n\n" +
            "씨에서 직접 옥기거나 지우신 것은 전부 없어지고,\n" +
            "빌더에 적혀 있는 값으로 되돌아갑니다.\n\n" +
            "직접 작업하신 게 있다면 취소하고 먼저 백업하세요.",
            "다시 만들기",
            "취소");

        if (!confirmed)
        {
            Debug.Log("[HospitalRoomSceneBuilder] 취소했습니다. 씨은 그대로입니다.");
            return;
        }

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

        BuildHospitalBgm();

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

    private static void BuildHospitalBgm()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(HospitalBgmPath);
        if (clip == null)
        {
            Debug.LogWarning("[HospitalRoomSceneBuilder] 병실 BGM을 찾지 못했습니다: " + HospitalBgmPath);
            return;
        }

        var musicObject = new GameObject("HospitalRoomBGM");
        var music = musicObject.AddComponent<SceneBackgroundMusic>();
        music.Configure(clip, 0.45f);
    }

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

        PlaceModel(DoorModelPath, root, "DoorModel",
                   new Vector3(3.0986f, 1.0489f, -3.5199f), new Vector3(270.02f, 0f, 0f), 209.75350f, true);
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

        // 침대 머리맛 벽 수납 유닛 (뒷판/선반은 도형, 사물함은 모델)
        float[] unitX = { -3.80f, -0.55f, 3.25f };
        for (int i = 0; i < unitX.Length; i++) MakeHeadUnit(root, i + 1, unitX[i]);

        // 링거대
        MakeIVStand(root, 1, new Vector3(-3.45f, 0f, 2.85f));
        MakeIVStand(root, 2, new Vector3(-1.05f, 0f, 2.85f));

        // 오버베드 테이블 (1번은 바퀴 없음)
        // 1번 테이블은 씨에서 부품을 따로 옥겨 놓았다.
        MakeOverbedTable(root, 1, new Vector3(-3.60f, 0f, 1.45f), 12f,
                         new Vector3(0.5020f, 0.8600f, -2.1270f),
                         new Vector3(0.4210f, 0.4400f, -2.1270f),
                         new Vector3(0.4210f, 0.0300f, -2.1020f),
                         withCasters: false);
        MakeOverbedTable(root, 2, new Vector3(3.30f, 0f, 1.45f), -72f,
                         new Vector3(0f, 0.86f, 0f),
                         new Vector3(0f, 0.44f, 0f),
                         new Vector3(0f, 0.03f, 0.10f),
                         withCasters: true);
        MakeStool(root, new Vector3(-0.30f, 0f, 0.55f));
        MakeSofa(root, new Vector3(-3.85f, 0f, -1.85f));
        MakeCurtain(root);
    }

    private static void MakeHeadUnit(Transform parent, int index, float centerX)
    {
        var root = BuildUtil.Empty($"HeadUnit_{index}", parent, new Vector3(centerX, 0f, 0f)).transform;

        float wallZ = RoomHalfZ - 0.05f;

        // 뒤판
        BuildUtil.Box("Backboard", root, new Vector3(0f, 1.70f, wallZ - 0.02f),
                      new Vector3(0.94f, 1.70f, 0.05f), _wood);

        // 사물함 모델
        PlaceModel(CabinetModelPath, root, "Cabinet",
                   CabinetLocalPos[index - 1], CabinetEuler, CabinetLocalScale[index - 1], true);

        // 위쪽 선반
        BuildUtil.Box("Shelf", root, new Vector3(0f, 1.86f, wallZ - 0.18f),
                      new Vector3(0.90f, 0.05f, 0.30f), _wood, true);

        // 의료용 콘센트 패널
        BuildUtil.Box("OutletPanel", root, new Vector3(0f, 2.12f, wallZ - 0.06f),
                      new Vector3(0.52f, 0.14f, 0.03f), _bedRail);
    }

    /// <summary>
    /// 다운로드받은 사물함 FBX 를 배치한다.
    /// 모델을 못 찾으면 예전의 나무 상자로 대신해 씨이 깨지지 않게 한다.
    /// </summary>


    /// <summary>오브젝트와 자식들의 Renderer 를 전부 감싸는 월드 바운즈를 구한다.</summary>
    private static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    /// <summary>
    /// 모델 에셋을 씨에 놓는다.
    /// 위치·회전·크기는 손으로 맞춘 값을 그대로 쓴다.
    /// </summary>
    private static GameObject PlaceModel(string assetPath, Transform parent, string name,
                                        Vector3 localPosition, Vector3 localEuler, Vector3 localScale,
                                        bool addCollider)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (model == null)
        {
            Debug.LogWarning($"[HospitalRoomSceneBuilder] 모델을 찾지 못했습니다: {assetPath}\n" +
                             "해당 오브젝트를 건너뜁니다.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale    = localScale;

        if (addCollider)
        {
            // 모델이 수십만 폴리라 MeshCollider 는 쓰지 않고
            // 메시 크기에 맞춘 상자 콜라이더를 달아 준다.
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                if (filter.GetComponent<Collider>() != null) continue;

                var box = filter.gameObject.AddComponent<BoxCollider>();
                box.center = filter.sharedMesh.bounds.center;
                box.size   = filter.sharedMesh.bounds.size;
            }
        }

        return instance;
    }

    /// <summary>크기가 세 방향 모두 같을 때 쓰는 간편 버전.</summary>
    private static GameObject PlaceModel(string assetPath, Transform parent, string name,
                                        Vector3 localPosition, Vector3 localEuler, float uniformScale,
                                        bool addCollider)
    {
        return PlaceModel(assetPath, parent, name, localPosition, localEuler,
                          Vector3.one * uniformScale, addCollider);
    }


    private static void MakeIVStand(Transform parent, int index, Vector3 position)
    {
        var root = BuildUtil.Empty($"IVStand_{index}", parent, position).transform;

        PlaceModel(IVPoleModelPath, root, "IVPoleModel",
                   IVPoleLocalPos[index - 1], Vector3.zero, 1.90f, false);
    }

    private static void MakeOverbedTable(Transform parent, int index, Vector3 position, float yaw,
                                         Vector3 topPos, Vector3 columnPos, Vector3 footPos, bool withCasters)
    {
        var root = BuildUtil.Empty($"OverbedTable_{index}", parent, position).transform;
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);

        BuildUtil.Box("Top", root, topPos, new Vector3(0.78f, 0.04f, 0.44f), _wood, true);
        BuildUtil.Cylinder("Column", root, columnPos, 0.06f, 0.84f, _metal, true);
        BuildUtil.Box("Foot", root, footPos, new Vector3(0.50f, 0.05f, 0.34f), _metal);

        if (!withCasters) return;

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

        PlaceModel(StoolModelPath, root, "StoolModel",
                   new Vector3(0f, 0.2540f, 0f), Vector3.zero, 0.57f, true);
    }

    private static void MakeSofa(Transform parent, Vector3 position)
    {
        var root = BuildUtil.Empty("Sofa", parent, position).transform;
        root.localRotation = Quaternion.Euler(0f, 90f, 0f);

        PlaceModel(LoveseatModelPath, root, "LoveseatModel",
                   new Vector3(0f, 0.5000f, 0f), Vector3.zero, 1.30f, true);
    }

    private static void MakeCurtain(Transform parent)
    {
        var root = BuildUtil.Empty("Curtain", parent, Vector3.zero).transform;

        PlaceModel(CurtainModelPath, root, "CurtainModel",
                   new Vector3(1.2000f, 1.0000f, 2.7200f), new Vector3(270f, 90f, 0f), 2.00f, false);
    }

    // ============================================================
    // 침대 + 환자
    // ============================================================

    private static List<BedSlot> BuildBedsAndPatients(PatientData harin, PatientData hayun)
    {
        var slots = new List<BedSlot>();

        var bed1 = MakeBed(1, BedCenterX[0]);
        var bed2 = MakeBed(2, BedCenterX[1]);
        var bed3 = MakeBed(3, BedCenterX[2]);

        // 1번 / 2번에 자고 있는 아이 모델을 눈힌다.
        // 2번은 X 크기를 -1 로 뒤집어 언니와 좌우 대칭이 되게 해 둔다.
        bed1.patientRoot = PlaceModel(GirlModelPath, bed1.root, "GirlModel",
            new Vector3(0.4408f, 0.9000f, 0.0892f), new Vector3(0f, 180.00f, 0f),
            new Vector3(1f, 1f, 1f), false);

        bed2.patientRoot = PlaceModel(GirlModelPath, bed2.root, "GirlModel",
            new Vector3(0.9510f, 0.9000f, 0.1000f), new Vector3(0f, 179.45f, 0f),
            new Vector3(-1f, 1f, 1f), false);

        // 이불 모델 두 장.
        // 3번 침대 밑에 매달려 있지만 실제로는 1·2번 아이를 덮는다.
        PlaceModel(BlanketModelPath, bed3.root, "BlanketModel",
            new Vector3(-4.5060f, 0.7687f, -0.3590f), Vector3.zero,
            new Vector3(1.47742f, 1.17580f, 1.47993f), false);

        PlaceModel(BlanketModelPath, bed3.root, "BlanketModel (1)",
            new Vector3(-2.9986f, 0.7687f, -0.3607f), Vector3.zero,
            new Vector3(1.58705f, 1.17580f, 1.52688f), false);

        // 빈 침대가 되면 아이가 자동으로 꿼지도록 BedSlot 에 연결
        LinkPatientVisual(bed1);
        LinkPatientVisual(bed2);

        // 왼쪽 아이(1번 침대 하린)에게만 [E] 손대기
        AddTouchInteraction(bed1);

        slots.Add(bed1.slot);
        slots.Add(bed2.slot);
        slots.Add(bed3.slot);
        return slots;
    }

    /// <summary>BedSlot 이 환자 표현물을 켜고 끔 수 있도록 연결한다.</summary>
    private static void LinkPatientVisual(BedParts bed)
    {
        if (bed.patientRoot == null) return;

        var serialized = new SerializedObject(bed.slot);
        serialized.FindProperty("patientVisual").objectReferenceValue = bed.patientRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 침대 위에 [E] 손대기 판정 상자를 만들어 준다.
    /// 아이 모델에는 콜라이더가 없어서 별도 오브젝트로 둔다.
    /// </summary>
    private static void AddTouchInteraction(BedParts bed)
    {
        if (bed.patientRoot == null) return;

        var zone = BuildUtil.Empty("TouchZone", bed.root, new Vector3(0.44f, 0.95f, 0.09f));

        var box = zone.AddComponent<BoxCollider>();
        box.size = new Vector3(0.90f, 0.60f, 1.60f);
        box.isTrigger = true;

        var touch = zone.AddComponent<PatientTouchInteractable>();

        var serialized = new SerializedObject(touch);
        serialized.FindProperty("displayName").stringValue   = "쌍둥이 언니";   // 프롬프트에 띄울 호칭
        serialized.FindProperty("actionLabel").stringValue   = "손대기";
        serialized.FindProperty("interactRange").floatValue  = 2.0f;
        serialized.FindProperty("bedSlot").objectReferenceValue      = bed.slot;
        serialized.FindProperty("twitchTarget").objectReferenceValue = bed.patientRoot.transform;
        serialized.FindProperty("twitchAngle").floatValue    = 2.5f;   // 모델 전체가 도니까 아주 조금만
        serialized.FindProperty("twitchDuration").floatValue = 0.85f;
        serialized.FindProperty("logChartOnTouch").boolValue = true;
        serialized.FindProperty("enterDreamOnTouch").boolValue = true;
        serialized.FindProperty("dreamScene").stringValue = Dream1SceneName;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>MakeBed / MakePatient 가 주고받는 부품 묶음.</summary>
    private class BedParts
    {
        public BedSlot slot;
        public Transform root;
        public GameObject patientRoot;

    }

    private static BedParts MakeBed(int bedNumber, float centerX)
    {
        var go = new GameObject($"Bed_{bedNumber:00}");
        go.transform.position = new Vector3(centerX, 0f, BedCenterZ);

        var root = go.transform;
        var parts = new BedParts { root = root };

        // 발치 이름표는 쓰지 않는다.
        // 침대 모델에 이미 이름표 자리가 그려져 있어서 겁쳐 보였다.

        // 침대 모델
        PlaceModel(BedModelPath, root, "BedModel",
                   BedModelLocalPos[bedNumber - 1], BedModelEuler, BedModelScale[bedNumber - 1], true);

        var slot = go.AddComponent<BedSlot>();
        parts.slot = slot;

        var slotSerialized = new SerializedObject(slot);
        slotSerialized.FindProperty("bedNumber").intValue = bedNumber;
        slotSerialized.ApplyModifiedPropertiesWithoutUndo();

        return parts;
    }

    /// <summary>
    /// 다운로드받은 빈 침대 FBX 로 침대를 만든다.
    /// 모델을 못 찾으면 기본 도형 침대로 대체해 씬이 깨지지 않게 한다.
    /// </summary>





    /// <summary>침대 발치에 붙는 주황색 이름표 카드 + 글자.</summary>
    /// <summary>침대 발치에 붙는 주황색 이름표 카드 + 글자.</summary>


    /// <summary>
    /// 침대 위에 누워 있는 아이를 만든다.
    /// innerSide 가 +1 이면 오른쪽(+X)으로, -1 이면 왼쪽(-X)으로 팔을 뻗어 옆 침대 아이와 손을 맞댄다.
    /// </summary>


    // ============================================================
    // 마무리
    // ============================================================

    private static void RegisterInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        string[] requiredScenes = { ScenePath, Dream1ScenePath };
        bool changed = false;
        foreach (string requiredScene in requiredScenes)
        {
            if (scenes.Exists(s => s.path == requiredScene)) continue;

            scenes.Add(new EditorBuildSettingsScene(requiredScene, true));
            changed = true;
        }

        if (changed)
            EditorBuildSettings.scenes = scenes.ToArray();
    }
}
