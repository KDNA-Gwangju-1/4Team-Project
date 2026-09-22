using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스 스테이지 동안 보스 기준 뒤쪽 180도 범위에 Stage2와 같은 5종 몬스터를 랜덤 스폰한다.
    /// 정화 필요/불필요 판정과 피격·접촉 처리는 MonsterCombat을 그대로 재사용하고,
    /// 정화 성공 시 MonsterCombat.OnPurified를 구독해 보스 약점 카운트(BossWeakpointController)에 연결한다.
    /// "뒤쪽" 기준 방향은 보스가 전투 중 회전해도 흔들리지 않도록 스테이지 진입 시점에 한 번만 고정한다.
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
        [SerializeField] private int maxActiveMonsters = 20;
        [SerializeField] private int stageIndex = 3;

        private readonly List<Transform> activeMonsters = new List<Transform>();
        private Vector3 rearDirection;
        private bool spawning;

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            BossWeakpointController.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != stageIndex || spawning || bossTransform == null) return;

            spawning = true;
            rearDirection = -bossTransform.forward;
            StartCoroutine(SpawnLoop());
        }

        private void HandleBossDefeated()
        {
            spawning = false;
        }

        private IEnumerator SpawnLoop()
        {
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
                Vector3 spawnPos = FindSpawnPosition();

                GameObject instance = Instantiate(template, spawnPos, Quaternion.identity);
                var combat = instance.GetComponent<MonsterCombat>();
                if (combat != null)
                {
                    combat.SetNeedsPurification(Random.value < 0.5f);
                    combat.SetContactDamage(contactDamage);
                    combat.SetGroundSnapCollider(arenaFloorCollider);
                    combat.OnPurified += HandleMonsterPurified;
                }
                instance.SetActive(true);
                activeMonsters.Add(instance.transform);

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void HandleMonsterPurified()
        {
            BossWeakpointController.Instance?.RegisterMonsterPurified();
        }

        private Vector3 FindSpawnPosition()
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                // 뒤쪽 180도(±90도) 부채꼴 안에서 랜덤 각도/반경을 고른다.
                float angle = Random.Range(-90f, 90f);
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * rearDirection;
                float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 candidate = bossTransform.position + dir * radius;

                if (arenaFloorCollider != null)
                {
                    Bounds b = arenaFloorCollider.bounds;
                    candidate.x = Mathf.Clamp(candidate.x, b.min.x, b.max.x);
                    candidate.z = Mathf.Clamp(candidate.z, b.min.z, b.max.z);
                    candidate.y = SampleGroundHeight(candidate, b);
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
                if (!overlaps) return candidate;
            }
            return bossTransform.position + rearDirection * minSpawnRadius;
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
