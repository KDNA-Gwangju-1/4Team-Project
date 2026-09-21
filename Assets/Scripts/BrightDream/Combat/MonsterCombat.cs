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
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float contactDamage = 20f;
        [SerializeField] private float wrongShotDamage = 20f;
        [SerializeField] private float purifyFlashDuration = 0.35f;
        [SerializeField] private Renderer[] bodyRenderers;

        [Header("몬스터 간 겹침 방지")]
        [Tooltip("이 반경 안에 다른 몬스터가 있으면 서로 밀어낸다. Rigidbody가 kinematic이라 물리 충돌로는 절대 안 밀리므로 이동 로직에서 직접 처리한다.")]
        [SerializeField] private float separationRadius = 0.9f;
        [Tooltip("밀어내는 힘의 세기 - toPlayer 방향과 합산되는 가중치.")]
        [SerializeField] private float separationStrength = 1.6f;

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
            Vector3 moveDir = (toPlayer + separation * separationStrength + obstacleAvoidance * obstacleAvoidStrength).normalized;

            Vector3 nextPos = transform.position + moveDir * (moveSpeed * Time.deltaTime);
            if (arenaBoundsSet)
            {
                nextPos.x = Mathf.Clamp(nextPos.x, arenaMinX, arenaMaxX);
                nextPos.z = Mathf.Clamp(nextPos.z, arenaMinZ, arenaMaxZ);
            }
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
                bullet.OnHitMonster();
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
