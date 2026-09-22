using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 스폰된 몬스터 개별 동작. 공격 패턴 없이 플레이어를 향해 직진하다가,
    /// 정화가 필요한 몬스터는 총에 맞으면 붉게 변한 뒤 사라지며 정화 카운트를 올리고,
    /// 플레이어와 부딪히면 피해를 주고 사라진다.
    /// 정화가 필요 없는 몬스터는 플레이어를 그대로 통과하며 사라지고, 잘못 쏘면 플레이어가 피해를 입는다.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    [RequireComponent(typeof(MonsterCorruptionVisual))]
    public class MonsterCombat : MonoBehaviour
    {
        [Tooltip("체크하면 총으로 정화해야 하는 몬스터. 해제하면 플레이어를 통과하며, 잘못 쏘면 플레이어가 피해를 입는다.\n" +
                 "MonsterSpawner가 스폰 시 1회 랜덤으로 덮어쓰므로, 여기 Inspector 값은 스폰되지 않는 원본/에디터 미리보기에만 의미가 있다.")]
        [SerializeField] private bool needsPurification;

        /// <summary>Target/Innocent 여부의 단일 source of truth. MonsterCorruptionVisual과 판정 로직 모두 이 값만 읽는다.</summary>
        public bool NeedsPurification => needsPurification;

        /// <summary>스폰 직후(Awake 이후, Start 이전) MonsterSpawner가 1회만 호출해서 이 개체의 Target/Innocent를 확정한다.</summary>
        public void SetNeedsPurification(bool value) => needsPurification = value;

        /// <summary>정화됐을 때(총에 맞아 사라지기 직전) 1회 발생. BossArenaMonsterSpawner가 보스 약점 카운트에 연결할 때 쓴다.</summary>
        public event Action OnPurified;

        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float contactDamage = 20f;
        /// <summary>스폰 직후 접촉 피해량을 덮어쓴다 (보스 아레나 몬스터는 하트 반개로 낮춰서 스폰한다).</summary>
        public void SetContactDamage(float value) => contactDamage = value;
        [SerializeField] private float wrongShotDamage = 20f;
        [SerializeField] private float purifyFlashDuration = 0.35f;
        [SerializeField] private Renderer[] bodyRenderers;

        [Header("몬스터 간 겹침 방지")]
        [Tooltip("이 반경 안에 다른 몬스터가 있으면 서로 밀어낸다. Rigidbody가 kinematic이라 물리 충돌로는 절대 안 밀리므로 이동 로직에서 직접 처리한다.")]
        [SerializeField] private float separationRadius = 0.9f;
        [Tooltip("밀어내는 힘의 세기 - toPlayer 방향과 합산되는 가중치.")]
        [SerializeField] private float separationStrength = 1.6f;

        [Header("보스와 겹침 방지")]
        [Tooltip("보스 스테이지에서 이 반경 안에 보스가 있으면 밀려난다 - BossAI.Instance가 없으면(보스 스테이지가 아니면) 아무 효과 없다.")]
        [SerializeField] private float bossAvoidRadius = 3f;
        [SerializeField] private float bossAvoidStrength = 2.2f;

        [Header("바닥 높이 추적 (굴곡진 바닥용)")]
        [Tooltip("설정하면 매 프레임 이 콜라이더 위로 레이캐스트해 바닥 높이에 맞춰 Y를 보정한다. " +
                 "05_boss_platform처럼 평평하지 않은 바닥에서, 스폰 시점 높이만 갖고 계속 이동하면 " +
                 "움직이는 동안 지형 굴곡에 따라 조금씩 파묻히거나 뜨는 문제를 막는다. " +
                 "Stage2처럼 평평한 바닥에서는 비워두면 기존과 동일하게 동작한다.")]
        [SerializeField] private Collider groundSnapCollider;
        [SerializeField] private float groundSnapOffset = 0.06f;

        /// <summary>보스 아레나처럼 굴곡진 바닥에 스폰될 때 BossArenaMonsterSpawner가 1회 호출해서 연결한다.</summary>
        public void SetGroundSnapCollider(Collider floor) => groundSnapCollider = floor;

        [Header("환경 장애물 회피")]
        [Tooltip("이름에 \"Crate\"가 들어간 오브젝트만 장애물로 보고 이 반경 안에서 피해 옆으로 돈다. " +
                 "몬스터도 kinematic + trigger 콜라이더라 물리적으로는 절대 막히지 않으므로 직접 계산해서 우회시킨다.")]
        [SerializeField] private float obstacleAvoidRadius = 1.0f;
        [Tooltip("장애물을 피하는 힘의 세기 - toPlayer 방향과 합산되는 가중치.")]
        [SerializeField] private float obstacleAvoidStrength = 2.2f;

        private static readonly List<MonsterCombat> activeInstances = new List<MonsterCombat>();
        private static readonly Collider[] obstacleHitsBuffer = new Collider[16];

        // 몬스터는 kinematic Rigidbody + trigger 콜라이더라 벽 콜라이더로는 절대 못 막는다 (플레이어와 달리).
        // Stage2 아레나 컨테인먼트용으로 ArenaLockdown이 시작 시 1회 설정하면, 이 사각형 밖으로 못 나가도록 매 프레임 위치를 고정한다.
        private static bool arenaBoundsSet;
        private static float arenaMinX, arenaMaxX, arenaMinZ, arenaMaxZ;

        /// <summary>Stage2 클리어 시점에 아직 살아 있는 몬스터를 모두 즉시 제거한다.
        /// 방금 총에 맞아 붉게 변하는 연출 중인 개체(isDone)는 그 연출을 마치도록 건드리지 않는다.</summary>
        public static void DespawnAll()
        {
            for (int i = activeInstances.Count - 1; i >= 0; i--)
            {
                MonsterCombat monster = activeInstances[i];
                if (monster == null || monster.isDone) continue;
                monster.isDone = true;
                Destroy(monster.gameObject);
            }
        }

        /// <summary>아레나 봉쇄가 풀릴 때 위치 고정도 함께 해제한다.</summary>
        public static void ClearArenaBounds() => arenaBoundsSet = false;

        public static void SetArenaBounds(Bounds bounds)
        {
            arenaMinX = bounds.min.x;
            arenaMaxX = bounds.max.x;
            arenaMinZ = bounds.min.z;
            arenaMaxZ = bounds.max.z;
            arenaBoundsSet = true;
        }

        private Transform player;
        private bool isDone;

        private void Awake()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            GetComponent<Collider>().isTrigger = true;

            if (bodyRenderers == null || bodyRenderers.Length == 0)
                bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            if (PlayerHealth.Instance != null) player = PlayerHealth.Instance.transform;
        }

        private void OnEnable() => activeInstances.Add(this);
        private void OnDisable() => activeInstances.Remove(this);

        private void Update()
        {
            if (isDone || player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.0001f) return;
            toPlayer.Normalize();

            Vector3 separation = ComputeSeparation();
            Vector3 obstacleAvoidance = ComputeObstacleAvoidance();
            Vector3 bossAvoidance = ComputeBossAvoidance();
            Vector3 moveDir = (toPlayer + separation * separationStrength + obstacleAvoidance * obstacleAvoidStrength
                + bossAvoidance * bossAvoidStrength).normalized;

            Vector3 nextPos = transform.position + moveDir * (moveSpeed * Time.deltaTime);
            if (arenaBoundsSet)
            {
                nextPos.x = Mathf.Clamp(nextPos.x, arenaMinX, arenaMaxX);
                nextPos.z = Mathf.Clamp(nextPos.z, arenaMinZ, arenaMaxZ);
            }
            if (groundSnapCollider != null) nextPos.y = SampleGroundHeight(nextPos);
            transform.position = nextPos;
            transform.forward = toPlayer; // 밀어내기/회피와 무관하게 항상 플레이어를 바라본다.
        }

        /// <summary>가까운 다른 몬스터들로부터 밀려나는 방향(정규화 안 됨) - 겹침 방지용.</summary>
        private Vector3 ComputeSeparation()
        {
            Vector3 push = Vector3.zero;
            for (int i = 0; i < activeInstances.Count; i++)
            {
                MonsterCombat other = activeInstances[i];
                if (other == this || other == null || other.isDone) continue;

                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist <= 0.0001f || dist >= separationRadius) continue;

                push += (away / dist) * (1f - dist / separationRadius);
            }
            return push;
        }

        /// <summary>보스 본체로부터 밀려나는 방향(정규화 안 됨) - 보스 스테이지가 아니면(Instance 없음) 항상 0.</summary>
        private Vector3 ComputeBossAvoidance()
        {
            BossAI boss = BossAI.Instance;
            if (boss == null) return Vector3.zero;

            Vector3 away = transform.position - boss.transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist <= 0.0001f || dist >= bossAvoidRadius) return Vector3.zero;

            return (away / dist) * (1f - dist / bossAvoidRadius);
        }

        /// <summary>
        /// 가까운 Crate류 소품(이름에 "Crate" 포함)으로부터 밀려나는 방향(정규화 안 됨).
        /// 맵의 벽/게이트/바닥/펜스 등 다른 모든 solid 콜라이더는 몬스터가 원래 설계대로 그대로 통과해야 하므로
        /// 이름으로 화이트리스트를 걸어 Crate만 피하게 한다 - "아무 solid 콜라이더나 회피" 방식은
        /// StageGate, 맵 경계 Wall처럼 플레이어 전용/구조용 콜라이더까지 걸려 몬스터가 못 나오는 문제가 있었다.
        /// 걸어서 통과하도록 설계된 non-convex MeshCollider도 안전하게 제외한다.
        /// </summary>
        private Vector3 ComputeObstacleAvoidance()
        {
            Vector3 push = Vector3.zero;
            Vector3 checkCenter = transform.position + Vector3.up * 0.3f;
            int count = Physics.OverlapSphereNonAlloc(checkCenter, obstacleAvoidRadius, obstacleHitsBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = obstacleHitsBuffer[i];
                if (hit == null) continue;
                if (hit.GetComponentInParent<MonsterCombat>() == this) continue;
                if (!IsCrateObstacle(hit.transform)) continue;
                if (hit is MeshCollider meshCollider && !meshCollider.convex) continue;

                Vector3 closest = hit.ClosestPoint(transform.position);
                Vector3 away = transform.position - closest;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist >= obstacleAvoidRadius) continue;
                if (dist <= 0.0001f)
                {
                    // 드물게 장애물 안쪽에 있으면 옆으로라도 밀어낸다.
                    away = Vector3.Cross(Vector3.up, transform.forward);
                    dist = 0.01f;
                }

                push += (away / dist) * (1f - dist / obstacleAvoidRadius);
            }
            return push;
        }

        /// <summary>
        /// 그 XZ 위치의 실제 바닥 높이를 groundSnapCollider(05_boss_platform 등)에만 직접 레이캐스트해서 찾는다.
        /// NavMesh를 잠깐 써봤지만, 나무·장식품처럼 바닥과 무관한 다른 표면까지 같이 구워져 있어서
        /// 몬스터가 그런 엉뚱한 높은 지점을 바닥으로 잘못 인식해 공중에 뜨는 문제가 있었다 - 그래서
        /// groundSnapCollider 하나만 정확히 겨냥하는 레이캐스트로 되돌린다.
        /// 유기적인 형태의 MeshCollider는 삼각형 틈에서 정중앙 레이가 가끔 빗나갈 수 있어
        /// 중심과 그 주변 몇 지점을 같이 쏴서 보완한다.
        /// </summary>
        private static readonly Vector2[] SampleOffsets =
        {
            Vector2.zero, new Vector2(0.15f, 0f), new Vector2(-0.15f, 0f), new Vector2(0f, 0.15f), new Vector2(0f, -0.15f)
        };

        private float SampleGroundHeight(Vector3 point)
        {
            Bounds b = groundSnapCollider.bounds;
            float rayLength = b.size.y + 10f;
            foreach (Vector2 offset in SampleOffsets)
            {
                Vector3 origin = new Vector3(point.x + offset.x, b.max.y + 5f, point.z + offset.y);
                if (groundSnapCollider.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, rayLength))
                    return hit.point.y + groundSnapOffset;
            }
            return point.y; // 전부 빗나가면(경계 바깥 등) 현재 높이를 그대로 유지한다.
        }

        /// <summary>이 콜라이더 또는 그 조상 중 이름에 "Crate"가 포함된 것이 있는지 확인한다.</summary>
        private static bool IsCrateObstacle(Transform t)
        {
            while (t != null)
            {
                if (t.name.IndexOf("Crate", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
                t = t.parent;
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDone) return;

            PurifierProjectile bullet = other.GetComponentInParent<PurifierProjectile>();
            if (bullet != null)
            {
                if (!bullet.TryConsume()) return; // 이미 다른 대상을 맞힌 총알 - 이 몬스터는 못 맞는다.
                HandleShot();
                return;
            }

            if (other.GetComponentInParent<CharacterController>() != null)
            {
                HandlePlayerContact();
            }
        }

        private void HandleShot()
        {
            isDone = true;
            if (needsPurification)
            {
                MonsterPurifyManager.Instance?.RegisterPurify();
                OnPurified?.Invoke();
                StartCoroutine(FlashRedThenDestroy());
            }
            else
            {
                PlayerHealth.Instance?.TakeDamage(wrongShotDamage);
                CameraShake.Instance?.Shake();
                Destroy(gameObject);
            }
        }

        private void HandlePlayerContact()
        {
            if (needsPurification)
            {
                // 무적 상태에서는 부딪혀도 아무 일도 일어나지 않는다 (몬스터도 그대로 유지).
                if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsInvincible) return;

                isDone = true;
                PlayerHealth.Instance?.TakeDamage(contactDamage, grantInvincibility: true);
                // 오발 피격과 정확히 같은 세기로 흔든다 (CameraShake 기본값 공용).
                CameraShake.Instance?.Shake();
                Destroy(gameObject);
            }
            else
            {
                isDone = true;
                Destroy(gameObject);
            }
        }

        private IEnumerator FlashRedThenDestroy()
        {
            foreach (Renderer r in bodyRenderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_Color")) m.color = Color.red;
                    else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.red);
                }
            }
            yield return new WaitForSeconds(purifyFlashDuration);
            Destroy(gameObject);
        }
    }
}
