using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using BrightDream.Clues;
using BrightDream.Combat;

namespace BrightDream
{
    /// <summary>
    /// 단서 4/4를 모으고 마지막 조사 대사가 끝나면(ClueManager.OnAllCluesCollected) 재생되는
    /// Stage1 → Stage2 암시 연출. 약간의 여유 뒤 플레이어 조작을 잠그고 Cinemachine 다리 와이드 샷으로 전환하면,
    /// 오염 몬스터가 이미 다리 위(BridgeCenter)에 서 있는 채로 화면이 열린다.
    /// 그 자리에서 대사 두 줄이 이어서 나오고, 대사가 끝나면 그제서야 몬스터가 다리를 마저 건너
    /// 실제 길(Stage2Path)을 따라 Combat Arena 안으로 들어간다.
    /// 몬스터가 아레나에 들어가면 높은 각도의 리빌 샷으로 전환되고, 그 시점이 완전히 전환된 뒤에
    /// 앞서와 같은 대사 두 줄이 다시 나온 뒤 게임플레이로 복귀한다.
    /// 여기 등장하는 몬스터는 연출 전용 - MonsterCombat 컴포넌트를 비활성 상태로 붙여 두어
    /// (Awake만 돌고 Update/OnTriggerEnter는 안 돎, OnEnable도 안 불려 분리 로직에도 안 끼임)
    /// 실제 정화 카운트/데미지 판정 및 MonsterSpawner에는 전혀 관여하지 않는다.
    /// MonsterCorruptionVisual은 그대로 살아있어 NeedsPurification=true를 읽어 기존 mesh-projection
    /// 오염 얼룩을 정상적으로 표시한다.
    /// </summary>
    public class Stage1ToStage2Cutscene : MonoBehaviour
    {
        [Header("카메라 - Main Camera에 붙은 CinemachineBrain은 평소 꺼둔다")]
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [Tooltip("다리를 건너는 동안 보여줄 와이드 샷 (기존 구도 그대로 유지).")]
        [SerializeField] private CinemachineCamera bridgeCamera;
        [Tooltip("몬스터가 아레나에 들어간 뒤 잠깐 보여줄 높은 각도의 리빌 샷.")]
        [SerializeField] private CinemachineCamera arenaRevealCamera;

        [Header("플레이어 잠금")]
        [SerializeField] private SimpleFirstPersonController playerController;
        [SerializeField] private PlayerInteraction playerInteraction;
        [Tooltip("Main Camera는 Player의 자식이라 Cinemachine이 월드 좌표를 직접 써 버리면 " +
                 "브랜치를 꺼도 로컬 좌표가 남는다. 컷신 시작 전 로컬 위치/회전을 저장해뒀다가 끝나면 그대로 복원한다.")]
        [SerializeField] private Transform playerCameraTransform;

        [Header("연출용 몬스터 - 실제 Path를 따라가는 웨이포인트")]
        [SerializeField] private GameObject monsterTemplate;
        [Tooltip("순서대로: Spawn → MainPath → BridgeEntrance → BridgeCenter → BridgeExit → Stage2Path → ArenaEntrance → ArenaInside. " +
                 "몬스터는 BridgeCenter(index 3)에 처음부터 서 있는 채로 등장하고, 대사가 끝나면 그 뒤부터 실제로 이동한다. " +
                 "전부 실제 MainPath/Bridge 위의 지점이라 울타리·나무를 뚫지 않는다.")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private int startWaypointIndex = 3;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 6f;
        [Tooltip("다리 위에서 이쪽을 보고 서 있다가 진행 방향으로 돌아설 때의 회전 속도(초당 각도).")]
        [SerializeField] private float turnAroundSpeed = 180f;

        [Header("타이밍")]
        [Tooltip("마지막 대사가 끝난 뒤 조작을 잠그기까지의 여유 시간.")]
        [SerializeField] private float preLockDelay = 1f;
        [Tooltip("항공뷰(아레나 리빌) 대사가 다 끝난 뒤에도 추가로 더 유지할 시간.")]
        [SerializeField] private float arenaRevealHold = 3f;

        [Header("대사 - 탐정(플레이어) 반응")]
        [SerializeField] private DialogueUI dialogueUI;
        [Tooltip("다리 위에 서 있는 오염 몬스터를 처음 발견했을 때 이어서 나오는 두 줄 (스킵 가능). " +
                 "이 대사가 끝나야 몬스터가 움직이기 시작한다.")]
        [SerializeField] private string lineMonsterAppears = "잠깐… 저건?";
        [SerializeField] private string lineCorruptionNoticed = "인형 몸에도 같은 먹물이 묻어 있군. 저게 오염의 흔적인가.";
        [Tooltip("항공뷰로 완전히 전환된 뒤 다시 나오는 같은 두 줄 (스킵 가능).")]
        [SerializeField] private string lineArenaReveal1 = "저 녀석은 깨끗하군. 건드릴 이유는 없겠어.";
        [SerializeField] private string lineArenaReveal2 = "같은 모습이라고 다 오염된 건 아니야. 몸에 묻은 먹물을 확인하자.";
        [Tooltip("플레이어 시점으로 돌아온 직후 (스킵 가능, 안 기다리고 바로 조작 복구).")]
        [SerializeField] private string lineReturnToPlayer = "따라가 보자.";

        private ClueManager subscribedTo;

        private void Update()
        {
            // ClueManager 초기화 순서에 의존하지 않도록 Instance가 나타나는 프레임에 한 번만 구독한다.
            if (subscribedTo != null || ClueManager.Instance == null) return;
            subscribedTo = ClueManager.Instance;
            subscribedTo.OnAllCluesCollected += HandleAllCluesCollected;
        }

        private void OnDisable()
        {
            if (subscribedTo != null) subscribedTo.OnAllCluesCollected -= HandleAllCluesCollected;
        }

        private void HandleAllCluesCollected()
        {
            StartCoroutine(PlayCutscene());
        }

        private IEnumerator PlayCutscene()
        {
            yield return new WaitForSeconds(preLockDelay);

            Vector3 cachedCamLocalPos = Vector3.zero;
            Quaternion cachedCamLocalRot = Quaternion.identity;
            if (playerCameraTransform != null)
            {
                cachedCamLocalPos = playerCameraTransform.localPosition;
                cachedCamLocalRot = playerCameraTransform.localRotation;
            }

            if (playerController != null) playerController.enabled = false;
            if (playerInteraction != null) playerInteraction.enabled = false;
            if (cinemachineBrain != null) cinemachineBrain.enabled = true;
            if (arenaRevealCamera != null) arenaRevealCamera.gameObject.SetActive(false);
            if (bridgeCamera != null) bridgeCamera.gameObject.SetActive(true);

            GameObject monster = null;
            if (monsterTemplate != null && waypoints != null && waypoints.Length > startWaypointIndex)
            {
                Transform startPoint = waypoints[startWaypointIndex];
                monster = Instantiate(monsterTemplate, startPoint.position, Quaternion.identity);
                MonsterCombat combat = monster.GetComponent<MonsterCombat>();
                if (combat != null)
                {
                    combat.SetNeedsPurification(true);
                    combat.enabled = false;
                }

                // 진행 방향의 정반대(=이쪽)를 보고 서 있게 한다 - 다리 위에서 플레이어를 마주 본 채로 발견된다.
                // 대사가 끝나면 그 자리에서 180도 돌아선 뒤 Stage2 쪽으로 건너간다.
                if (startWaypointIndex + 1 < waypoints.Length)
                {
                    Vector3 flatDir = waypoints[startWaypointIndex + 1].position - startPoint.position;
                    flatDir.y = 0f;
                    if (flatDir.sqrMagnitude > 0.0001f) monster.transform.rotation = Quaternion.LookRotation(-flatDir.normalized, Vector3.up);
                }

                monster.SetActive(true);
                if (bridgeCamera != null) bridgeCamera.LookAt = monster.transform;

                // 다리 위에 이미 서 있는 몬스터를 두고 대사 두 줄이 이어서 나온다 - 끝나야 움직이기 시작한다.
                // 이 네 줄(발견 대사 두 줄 + 항공뷰 대사 두 줄)은 Stage2가 어떤 곳인지 설명하는
                // 나레이션 성격이라, 첫 줄이 뜨는 순간부터 마지막 줄이 끝날 때까지(중간의 몬스터 이동/
                // 카메라 전환 구간 포함) 타임어택 시간이 흐르지 않는다.
                TimeAttackTimer.Instance?.StopTimer();
                if (dialogueUI != null)
                    yield return StartCoroutine(ShowSequenceAndWait(new[] { lineMonsterAppears, lineCorruptionNoticed }));

                // 먼저 제자리에서 진행 방향으로 돌아선다. MoveAlongWaypoints의 Slerp는 정확히 180도일 때
                // 회전 축이 불안정해지므로, 돌아서는 동작만 yaw 기준으로 따로 처리한다.
                yield return StartCoroutine(TurnToFace(monster.transform, waypoints[startWaypointIndex + 1].position));

                // BridgeCenter 다음부터 ArenaInside까지 실제로 이동.
                yield return StartCoroutine(MoveAlongWaypoints(monster.transform, startWaypointIndex + 1, waypoints.Length - 1));
            }

            if (arenaRevealCamera != null) arenaRevealCamera.gameObject.SetActive(true);
            if (bridgeCamera != null) bridgeCamera.gameObject.SetActive(false);

            // "항공뷰로 완전히 시점이 전환되었을 때"에 대사가 나오도록 블렌드가 끝날 때까지 기다린다.
            yield return null;
            float blendWait = 0f;
            while (cinemachineBrain != null && cinemachineBrain.IsBlending && blendWait < 3f)
            {
                blendWait += Time.deltaTime;
                yield return null;
            }

            if (dialogueUI != null)
                yield return StartCoroutine(ShowSequenceAndWait(new[] { lineArenaReveal1, lineArenaReveal2 }));

            // 네 줄짜리 나레이션이 여기서 끝나므로 타임어택 시간을 다시 흐르게 한다.
            TimeAttackTimer.Instance?.ResumeTimer();

            yield return new WaitForSeconds(arenaRevealHold);

            if (monster != null) Destroy(monster);
            if (arenaRevealCamera != null) arenaRevealCamera.gameObject.SetActive(false);
            if (cinemachineBrain != null) cinemachineBrain.enabled = false;

            // Cinemachine이 Main Camera(=Player의 자식)에 남겨 놓은 월드 좌표 기반 로컬 값을
            // 컷신 시작 전 값으로 되돌려서 시점이 엉뚱한 곳에 남지 않게 한다.
            if (playerCameraTransform != null)
            {
                playerCameraTransform.localPosition = cachedCamLocalPos;
                playerCameraTransform.localRotation = cachedCamLocalRot;
            }

            if (playerController != null) playerController.enabled = true;
            if (playerInteraction != null) playerInteraction.enabled = true;

            if (dialogueUI != null) dialogueUI.ShowSequence(new[] { lineReturnToPlayer }, null);
        }

        /// <summary>제자리에서 target 쪽을 향해 yaw만 일정 속도로 돌린다 (180도 반전도 안전하게 처리된다).</summary>
        private IEnumerator TurnToFace(Transform mover, Vector3 target)
        {
            Vector3 flat = target - mover.position;
            flat.y = 0f;
            if (flat.sqrMagnitude <= 0.0001f) yield break;

            float targetYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            while (Mathf.Abs(Mathf.DeltaAngle(mover.eulerAngles.y, targetYaw)) > 1f)
            {
                float yaw = Mathf.MoveTowardsAngle(mover.eulerAngles.y, targetYaw, turnAroundSpeed * Time.deltaTime);
                mover.rotation = Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }
            mover.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }

        /// <summary>웨이포인트를 순서대로 지나가며, 이동 방향으로 부드럽게(Slerp) 회전한다.</summary>
        private IEnumerator MoveAlongWaypoints(Transform mover, int fromIndex, int toIndex)
        {
            for (int i = fromIndex; i <= toIndex; i++)
            {
                Vector3 start = mover.position;
                Vector3 end = waypoints[i].position;

                Vector3 flatDir = end - start;
                flatDir.y = 0f;
                Quaternion targetRot = flatDir.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(flatDir.normalized, Vector3.up)
                    : mover.rotation;

                float dist = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(end.x, 0f, end.z));
                float duration = Mathf.Max(0.05f, dist / moveSpeed);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    mover.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
                    mover.rotation = Quaternion.Slerp(mover.rotation, targetRot, Time.deltaTime * turnSpeed);
                    yield return null;
                }
                mover.position = end;
            }
        }

        /// <summary>
        /// 컷신 대사는 좌클릭/스페이스바/E키 어느 것으로도 넘길 수 없다 (allowSkip: false).
        /// 직전까지 단서 조사 텍스트를 좌클릭·스페이스바로 넘겨 오던 흐름이라,
        /// 그대로 누르고 있으면 연출 대사가 통째로 날아가 버리기 때문이다.
        /// </summary>
        private IEnumerator ShowSequenceAndWait(string[] lines)
        {
            bool done = false;
            dialogueUI.ShowSequence(lines, () => done = true, allowSkip: false);
            while (!done) yield return null;
        }
    }
}
