using UnityEngine;

namespace BrightDream
{
    /// <summary>
    /// 픽업 아이템(정화총) 위에 떠서 위치를 알려주는 화살표.
    /// 평면(Quad) 한 장이라 옆에서 보면 종잇장처럼 사라지므로 항상 카메라를 바라보게 돌려 주고,
    /// 눈에 띄도록 위아래로 둥실거린다.
    ///
    /// 켜고 끄는 건 이 스크립트가 하지 않는다 - WeaponPickup이 자식 Renderer를 전부 모아
    /// 단서 4개를 모으기 전까지 꺼 두고, 획득하면 오브젝트째로 끄기 때문에 화살표도 같이 따라간다.
    /// </summary>
    public class PickupIndicator : MonoBehaviour
    {
        [Tooltip("비워두면 Camera.main을 쓴다.")]
        [SerializeField] private Transform lookTarget;
        [Tooltip("기준 위치에서 위아래로 움직이는 폭(m).")]
        [SerializeField] private float bobAmplitude = 0.28f;
        [SerializeField] private float bobsPerSecond = 0.8f;
        [Tooltip("카메라를 향할 때 수평 회전만 할지 여부. 켜 두면 화살표가 늘 똑바로 선 채로 돈다.")]
        [SerializeField] private bool yawOnly = true;

        private Vector3 basePosition;

        private void Awake()
        {
            basePosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            // 둥실거림은 로컬 Y 기준 - 부모(픽업)가 움직여도 그대로 따라간다.
            float offset = Mathf.Sin(Time.time * Mathf.PI * 2f * bobsPerSecond) * bobAmplitude;
            transform.localPosition = basePosition + Vector3.up * (offset / Mathf.Max(0.0001f, transform.parent != null ? transform.parent.lossyScale.y : 1f));

            Transform target = lookTarget != null ? lookTarget : (Camera.main != null ? Camera.main.transform : null);
            if (target == null) return;

            Vector3 toCamera = transform.position - target.position;
            if (yawOnly) toCamera.y = 0f;
            if (toCamera.sqrMagnitude <= 0.0001f) return;

            // Quad는 +Z가 앞면이라 카메라 반대 방향을 보도록 세운다.
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
