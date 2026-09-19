using System.Collections;
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
    public class MonsterCombat : MonoBehaviour
    {
        [Tooltip("체크하면 총으로 정화해야 하는 몬스터. 해제하면 플레이어를 통과하며, 잘못 쏘면 플레이어가 피해를 입는다.")]
        [SerializeField] private bool needsPurification;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float contactDamage = 20f;
        [SerializeField] private float wrongShotDamage = 20f;
        [SerializeField] private float purifyFlashDuration = 0.35f;
        [SerializeField] private Renderer[] bodyRenderers;

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

        private void Update()
        {
            if (isDone || player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.0001f) return;

            toPlayer.Normalize();
            transform.position += toPlayer * (moveSpeed * Time.deltaTime);
            transform.forward = toPlayer;
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
                Destroy(gameObject);
            }
        }

        private void HandlePlayerContact()
        {
            isDone = true;
            if (needsPurification)
            {
                PlayerHealth.Instance?.TakeDamage(contactDamage, grantInvincibility: true);
            }
            Destroy(gameObject);
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
