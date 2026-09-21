using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 정화총 총알. 직선으로 날아가며 궤적(TrailRenderer)을 그린다.
    /// 실제 충돌 판정은 MonsterCombat 쪽 트리거에서 이루어지고, 맞았을 때 OnHitMonster()로 스스로를 정리한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PurifierProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 28f;
        [SerializeField] private float maxLifetime = 3f;

        private float elapsed;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            transform.position += transform.forward * (speed * Time.deltaTime);

            elapsed += Time.deltaTime;
            if (elapsed >= maxLifetime) Destroy(gameObject);
        }

        public void OnHitMonster()
        {
            Destroy(gameObject);
        }
    }
}
