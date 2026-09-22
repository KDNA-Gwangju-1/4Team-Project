using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스를 클리어한 뒤 남은 균열. 앞에 서서 E 를 누르면 로딩 화면을 거쳐
    /// 다음 챕터(어두운 꿈)로 넘어간다.
    ///
    /// 이 씬의 플레이어는 Interactable / PlayerInteractor 가 아니라 ClueInteractable 전용인
    /// PlayerInteraction 을 쓴다. 거기에 포탈을 끼워 넣으면 단서 시스템을 건드려야 해서,
    /// 감지와 프롬프트를 이 컴포넌트가 직접 처리한다.
    ///
    /// 프롬프트는 단서와 같은 Text 를 빌려 쓴다. PlayerInteraction 은 바라보는 대상이
    /// 바뀌는 순간에만 Text 를 켜고 끄는데, 포탈은 ClueInteractable 이 아니라 그쪽 대상이
    /// 계속 null 로 남는다. 그래서 둘이 매 프레임 Text 를 놓고 다투지 않는다.
    ///
    /// 감지는 Collider 대신 거리와 시선 각도로 한다. 균열은 파편이 흩어진 복잡한 메시라
    /// Raycast 를 쓰면 파편 사이로 빗나가기 쉽다.
    /// </summary>
    public class ChapterPortal : MonoBehaviour
    {
        [Header("이동할 곳")]
        [Tooltip("넘어갈 씬 이름. Build Settings 에 없으면 로딩 화면만 띄우고 거기 머문다.")]
        [SerializeField] private string nextScene = "BadDream_Stage1";
        [Tooltip("로딩 화면에 깔 그림. 비우면 로딩 씬에 원래 깔린 그림을 쓴다.")]
        [SerializeField] private Sprite loadingBackground;

        [Header("상호작용")]
        [Tooltip("이 거리 안까지 다가와야 E 가 뜬다 (m). 균열 표면 기준이다.")]
        [SerializeField] private float interactRange = 3.5f;
        [Tooltip("이 각도 안으로 바라봐야 E 가 뜬다 (도).")]
        [SerializeField] private float lookAngle = 45f;
        [Tooltip("프롬프트로 띄울 문구.")]
        [SerializeField] private string promptLabel = "E 들어가기";

        [Header("열리는 조건")]
        [Tooltip("보스를 잡기 전에는 들어갈 수 없다. 끄면 언제나 열려 있다 (테스트용).")]
        [SerializeField] private bool requireBossCleared = true;
        [Tooltip("이 스테이지에 도달해 있으면 이미 클리어한 것으로 본다. BossArenaLockdown 의 Clear Stage Index 와 같은 값.")]
        [SerializeField] private int clearedStageIndex = 4;

        [Header("연결")]
        [Tooltip("비우면 Camera.main 을 쓴다.")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("단서와 같이 쓰는 프롬프트 Text (ClueCanvas/PromptText).")]
        [SerializeField] private Text promptText;

        private bool isOpen;
        private bool hovering;
        private bool entered;
        private string originalPrompt;
        private Bounds riftBounds;
        private bool hasBounds;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            isOpen = !requireBossCleared;
            CacheBounds();
        }

        private void OnEnable()
        {
            BossRiftExit.OnRiftExitFinished += HandleRiftExitFinished;
        }

        private void OnDisable()
        {
            BossRiftExit.OnRiftExitFinished -= HandleRiftExitFinished;
            SetHovering(false);
        }

        private void Start()
        {
            // 이미 클리어한 상태로 시작했다면(세이브 로드·디버그 진입) 바로 열어 둔다.
            if (requireBossCleared && StageProgressManager.Instance != null &&
                StageProgressManager.Instance.CurrentStage >= clearedStageIndex)
            {
                isOpen = true;
            }
        }

        private void HandleRiftExitFinished()
        {
            isOpen = true;
        }

        /// <summary>균열의 겉넓이를 재 둔다. 연출 중에 크기가 변하므로 매번 다시 잰다.</summary>
        private void CacheBounds()
        {
            hasBounds = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (!hasBounds) { riftBounds = r.bounds; hasBounds = true; }
                else riftBounds.Encapsulate(r.bounds);
            }
        }

        private void Update()
        {
            if (!isOpen || entered) { SetHovering(false); return; }
            if (playerCamera == null) { SetHovering(false); return; }

            CacheBounds();
            if (!hasBounds) { SetHovering(false); return; }

            Vector3 eye = playerCamera.transform.position;
            float distance = Vector3.Distance(eye, riftBounds.ClosestPoint(eye));
            bool near = distance <= interactRange;

            Vector3 toPortal = riftBounds.center - eye;
            bool looking = toPortal.sqrMagnitude > 0.0001f &&
                           Vector3.Angle(playerCamera.transform.forward, toPortal) <= lookAngle;

            SetHovering(near && looking);

            if (hovering && Input.GetKeyDown(KeyCode.E)) Enter();
        }

        private void SetHovering(bool value)
        {
            if (hovering == value) return;
            hovering = value;

            if (promptText == null) return;

            if (hovering)
            {
                originalPrompt = promptText.text;
                promptText.text = promptLabel;
                promptText.gameObject.SetActive(true);
            }
            else
            {
                // 빌려 쓴 Text 를 단서 쪽이 쓰던 문구 그대로 돌려준다.
                if (!string.IsNullOrEmpty(originalPrompt)) promptText.text = originalPrompt;
                promptText.gameObject.SetActive(false);
            }
        }

        private void Enter()
        {
            entered = true;              // 로딩 중에 또 눌리지 않게 잠근다.
            SetHovering(false);

            // 목적지 씬이 아직 Build Settings 에 없으면(어두운 꿈 브랜치 병합 전) 로딩 화면만
            // 띄우고 머문다. LoadingScreen.Go 는 빈 이름을 그 용도로 받아 준다.
            bool canLoad = !string.IsNullOrWhiteSpace(nextScene) &&
                           Application.CanStreamedLevelBeLoaded(nextScene);
            if (!canLoad)
            {
                Debug.LogWarning("[ChapterPortal] '" + nextScene + "' 씬이 Build Settings 에 없어 " +
                                 "로딩 화면만 띄웁니다. 어두운 꿈 씬을 합친 뒤 Build Settings 에 넣어 주세요.", this);
            }

            LoadingScreen.Go(canLoad ? nextScene : "", loadingBackground);
        }
    }
}
