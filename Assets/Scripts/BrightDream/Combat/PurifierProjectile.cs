using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 정화총 총알. 직선으로 날아가며 궤적(TrailRenderer)을 그린다.
    /// 실제 충돌 판정은 MonsterCombat/BossWeakpointController 쪽 트리거에서 이루어지고,
    /// 맞았을 때 TryConsume()으로 스스로를 정리한다. Destroy()는 그 프레임이 끝나야 실제로
    /// 적용되므로, 같은 프레임에 여러 대상과 동시에 겹치면 총알 하나가 여러 명을 맞힐 수 있다 -
    /// hasHit 플래그로 맨 처음 한 번만 유효한 명중으로 인정하고 그 이후 겹침은 전부 무시한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PurifierProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 28f;
        [SerializeField] private float maxLifetime = 3f;

        private float elapsed;
        private bool hasHit;

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

        /// <summary>이 총알이 아직 아무도 맞히지 않았으면 소모 처리하고 true를 반환한다.
        /// 이미 다른 대상에 명중해 소모된 총알이면 false를 반환하니, 호출한 쪽은 명중 효과를 적용하면 안 된다.</summary>
        public bool TryConsume()
        {
            if (hasHit) return false;
            hasHit = true;
            Destroy(gameObject);
            return true;
        }
    }
}
