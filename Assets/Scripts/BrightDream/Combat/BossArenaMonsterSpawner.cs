using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스 스테이지 동안 보스 기준 뒤쪽 180도 범위에 Stage2와 같은 5종 몬스터를 랜덤 스폰한다.
    /// 동시 마릿수는 maxActiveMonsters 로 묶고, 그 안에서 어둠 몹과 일반 몹의 비율을 유지한다.
    /// 정화 필요/불필요 판정과 피격·접촉 처리는 MonsterCombat을 그대로 재사용하고,
    /// 정화 성공 시 MonsterCombat.OnPurified를 구독해 보스 약점 카운트(BossWeakpointController)에 연결한다.
    /// "뒤쪽"은 스폰할 때마다 보스의 현재 방향으로 다시 잡는다. 보스는 보통 플레이어를 보고 쫓아오므로
    /// 몬스터가 보스 너머(플레이어 반대편)에서 나온다 - 방향을 처음 한 번만 고정하면 보스가 돌아선 뒤
    /// 플레이어 바로 옆에서 튀어나올 수 있었다.
    ///
    /// 씬에 보스 등장 연출(BossRiftEntrance)이 있으면, 균열에서 나온 보스가 아레나에 완전히 착지해
    /// 연출이 끝난 뒤(OnBossEntranceFinished)부터 spawnStartDelay 만큼 더 기다렸다가 스폰을 시작한다.
    /// 예전에는 스테이지 진입과 동시에 스폰해서 보스가 나오기도 전에 잡몹이 먼저 깔렸다.
    /// </summary>
    public class BossArenaMonsterSpawner : MonoBehaviour
    {
        [Tooltip("Stage2와 같은 5종 몬스터 템플릿(비활성 상태). 그대로 Instantiate해서 사용한다.")]
        [SerializeField] private GameObject[] monsterTemplates;
        [SerializeField] private Transform bossTransform;
        [Tooltip("스폰 위치를 이 범위 안으로 한정한다 - 05_boss_platform의 MeshCollider.")]
        [SerializeField] private Collider arenaFloorCollider;
        [SerializeField] private float minSpawnRadius = 4f;
        [SerializeField] private float maxSpawnRadius = 14f;
        [Tooltip("바닥면 위로 띄우는 여유 높이. 05_boss_platform은 굴곡진 모델이라 실제 바닥 Y가 위치마다 달라서," +
                 "이 값 자체를 스폰 높이로 쓰지 않고 아래로 레이캐스트해 찾은 바닥 높이에 더한다.")]
        [SerializeField] private float spawnSurfaceOffset = 0.06f;
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float minSpawnSpacing = 3f;
        [SerializeField] private int maxSpawnAttempts = 8;
        [Tooltip("일반 몬스터 접촉 피해 - 하트 반개.")]
        [SerializeField] private float contactDamage = 10f;
        [Tooltip("동시에 존재할 수 있는 최대 마릿수 - 이 이상이면 기존 개체가 죽을 때까지 새로 스폰하지 않는다.")]
        [SerializeField] private int maxActiveMonsters = 10;

        [Header("어둠 몹 / 일반 몹 비율")]
        [Tooltip("어둠 몹(정화가 필요한 개체) 최소 마릿수. 이만큼은 항상 먼저 채운다 - " +
                 "약점 전환이 이 개체들을 정화해야 열리기 때문에 모자라면 전투가 막힌다.")]
        [SerializeField] private int minDarkMonsters = 3;
        [Tooltip("어둠 몹 최대 마릿수.")]
        [SerializeField] private int maxDarkMonsters = 4;
        [Tooltip("일반 몹 최소 마릿수.")]
        [SerializeField] private int minNormalMonsters = 6;
        [Tooltip("일반 몹 최대 마릿수.")]
        [SerializeField] private int maxNormalMonsters = 7;
        [SerializeField] private int stageIndex = 3;
        [Tooltip("보스 등장 연출이 끝난 뒤(착지 후 전투 시작) 첫 몬스터를 내기까지 더 기다리는 시간 - 착지 포효가 끝날 즈음.")]
        [SerializeField] private float spawnStartDelay = 1.5f;

        private readonly List<Transform> activeMonsters = new List<Transform>();
        private bool spawning;
        private bool waitingForEntrance;

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            BossWeakpointController.OnBossDefeated += HandleBossDefeated;
            BossRiftEntrance.OnBossEntranceFinished += HandleEntranceFinished;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
            BossRiftEntrance.OnBossEntranceFinished -= HandleEntranceFinished;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != stageIndex || spawning || waitingForEntrance || bossTransform == null) return;

            // 보스가 아직 균열에서 나오는 중이면 연출이 끝날 때까지 기다린다.
            var entrance = FindObjectOfType<BossRiftEntrance>();
            if (entrance != null && !entrance.IsFinished)
            {
                waitingForEntrance = true;
                return;
            }
            BeginSpawning();
        }

        private void HandleEntranceFinished()
        {
            if (!waitingForEntrance) return;
            waitingForEntrance = false;
            BeginSpawning();
        }

        private void BeginSpawning()
        {
            if (spawning) return;
            spawning = true;
            StartCoroutine(SpawnLoop());
        }

        private void HandleBossDefeated()
        {
            spawning = false;
            waitingForEntrance = false;
        }

        private IEnumerator SpawnLoop()
        {
            if (spawnStartDelay > 0f) yield return new WaitForSeconds(spawnStartDelay);
            if (!spawning) yield break;

            while (spawning && monsterTemplates.Length > 0)
            {
                activeMonsters.RemoveAll(t => t == null);

                if (activeMonsters.Count >= maxActiveMonsters)
                {
                    // 최대 마릿수에 도달했다 - 기존 개체가 줄어들 때까지 스폰만 쉬고 대기한다.
                    yield return new WaitForSeconds(spawnInterval);
                    continue;
                }

                GameObject template = monsterTemplates[Random.Range(0, monsterTemplates.Length)];
                Vector3 spawnPos;
                if (!TryFindSpawnPosition(out spawnPos))
                {
                    yield return new WaitForSeconds(spawnInterval);
                    continue;
                }

                bool spawnDark = ShouldSpawnDark();

                GameObject instance = Instantiate(template, spawnPos, Quaternion.identity);
                var combat = instance.GetComponent<MonsterCombat>();
                if (combat != null)
                {
                    combat.SetNeedsPurification(spawnDark);
                    combat.SetContactDamage(contactDamage);
                    combat.SetGroundSnapCollider(arenaFloorCollider);
                    combat.OnPurified += HandleMonsterPurified;
                }
                instance.SetActive(true);
                activeMonsters.Add(instance.transform);

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        /// <summary>
        /// 다음에 낼 몹이 어둠 몹인지 정한다.
        ///
        /// 예전에는 50% 동전던지기였는데, 그러면 어둠 몹이 한 마리도 없는 구간이 생겨
        /// 약점 전환이 열리지 않고 전투가 멈춰 버린다. 최소치를 먼저 채우고 그 다음에
        /// 최대치를 넘지 않는 선에서 섞는다.
        /// </summary>
        private bool ShouldSpawnDark()
        {
            int dark = 0, normal = 0;
            foreach (Transform t in activeMonsters)
            {
                if (t == null) continue;
                var c = t.GetComponent<MonsterCombat>();
                if (c == null) continue;
                if (c.NeedsPurification) dark++; else normal++;
            }

            if (dark < minDarkMonsters) return true;        // 약점 전환에 필요한 몫을 먼저 확보한다
            if (normal < minNormalMonsters) return false;
            if (dark >= maxDarkMonsters) return false;
            if (normal >= maxNormalMonsters) return true;
            return Random.value < 0.35f;
        }

        private void HandleMonsterPurified()
        {
            BossWeakpointController.Instance?.RegisterMonsterPurified();
        }

        private bool TryFindSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (arenaFloorCollider == null || !arenaFloorCollider.enabled || !arenaFloorCollider.gameObject.activeInHierarchy)
                return false;
            Vector3 rear = -bossTransform.forward;
            rear.y = 0f;
            rear = rear.sqrMagnitude > 0.0001f ? rear.normalized : Vector3.back;
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                // 뒤쪽 180도(±90도) 부채꼴 안에서 랜덤 각도/반경을 고른다.
                float angle = Random.Range(-90f, 90f);
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * rear;
                float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 candidate = bossTransform.position + dir * radius;

                if (arenaFloorCollider != null)
                {
                    Bounds b = arenaFloorCollider.bounds;
                    candidate.x = Mathf.Clamp(candidate.x, b.min.x, b.max.x);
                    candidate.z = Mathf.Clamp(candidate.z, b.min.z, b.max.z);
                    RaycastHit ground;
                    if (!arenaFloorCollider.Raycast(new Ray(new Vector3(candidate.x, b.max.y + 5f, candidate.z), Vector3.down), out ground, b.size.y + 10f))
                        continue;
                    candidate.y = ground.point.y + spawnSurfaceOffset;
                }
                else
                {
                    candidate.y = spawnSurfaceOffset;
                }

                bool overlaps = false;
                foreach (Transform t in activeMonsters)
                {
                    if (t == null) continue;
                    if (Vector3.Distance(candidate, t.position) < minSpawnSpacing) { overlaps = true; break; }
                }
                if (!overlaps) { position = candidate; return true; }
            }
            // A crowded or invalid floor must not funnel every retry into one fallback point.
            return false;
        }

        private static readonly Vector2[] SampleOffsets =
        {
            Vector2.zero, new Vector2(0.15f, 0f), new Vector2(-0.15f, 0f), new Vector2(0f, 0.15f), new Vector2(0f, -0.15f)
        };

        /// <summary>
        /// 굴곡진 05_boss_platform 표면 높이를 arenaFloorCollider에만 직접 레이캐스트해서 찾는다.
        /// NavMesh로 해봤지만 나무·장식품 표면까지 같이 구워져 있어서 몬스터가 그런 엉뚱한 높은
        /// 지점을 바닥으로 잘못 인식하는 문제가 있었다 - arenaFloorCollider 하나만 겨냥하는 레이캐스트로
        /// 되돌리되, 유기적인 메시의 삼각형 틈에서 정중앙 레이가 빗나갈 때를 대비해 주변 몇 지점도 같이 쏜다.
        /// </summary>
        private float SampleGroundHeight(Vector3 point, Bounds floorBounds)
        {
            float rayLength = floorBounds.size.y + 10f;
            foreach (Vector2 offset in SampleOffsets)
            {
                Vector3 origin = new Vector3(point.x + offset.x, floorBounds.max.y + 5f, point.z + offset.y);
                if (arenaFloorCollider.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, rayLength))
                    return hit.point.y + spawnSurfaceOffset;
            }
            return floorBounds.max.y + spawnSurfaceOffset;
        }
    }
}
