using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 정화총을 획득한 뒤(WeaponPickup.PlayerHasWeapon) 좌클릭으로 총알을 발사한다.
    ///
    /// 총알은 총구에서 나가되 화면 한가운데 조준점으로 모인다.
    /// 예전에는 총구 위치에서 카메라 정면 방향으로 그냥 쐈는데, 총구가 카메라에서
    /// 오른쪽 아래로 비켜 있어서 두 직선이 평행이 된다. 평행하면 아무리 멀리 가도
    /// 만나지 않으므로 총알이 조준점을 영영 비껴간다 - 화면을 돌리면 그 어긋남이
    /// 눈에 띄게 드러났다. 그래서 조준점에서 레이를 쏴 실제로 겨눈 지점을 먼저 찾고,
    /// 총구에서 그 지점을 향해 쏜다.
    /// </summary>
    public class PurifierGunController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [Tooltip("총알이 나가는 자리. 총구 끝에 맞춰 둔 빈 오브젝트를 연결한다.")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float fireCooldown = 0.3f;

        [Header("조준")]
        [Tooltip("조준점 레이가 아무것도 맞히지 못했을 때 이 거리의 허공을 겨눈 것으로 본다.")]
        [SerializeField] private float maxAimDistance = 200f;

        private float cooldownTimer;
        private Transform playerRoot;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            playerRoot = transform.root;
        }

        private void Update()
        {
            // ESC 일시정지 중에는 입력을 받지 않는다.
            if (PauseMenu.IsPaused) return;
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

            if (!WeaponPickup.PlayerHasWeapon) return;
            if (cooldownTimer > 0f) return;

            if (Input.GetMouseButton(0))
            {
                Fire();
                cooldownTimer = fireCooldown;
            }
        }

        private void Fire()
        {
            if (projectilePrefab == null || playerCamera == null) return;

            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : playerCamera.transform.position;
            Vector3 direction = (GetAimPoint() - origin).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = playerCamera.transform.forward;

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            projectile.SetActive(true);
        }

        /// <summary>
        /// 화면 한가운데(조준점)가 실제로 겨누고 있는 월드 지점.
        /// 플레이어 자신의 콜라이더는 건너뛴다 - 카메라가 캐릭터 안에 있어서 그냥 쏘면
        /// 제 몸을 맞히고 코앞을 겨눈 것으로 나온다.
        /// </summary>
        private Vector3 GetAimPoint()
        {
            Transform camTr = playerCamera.transform;
            var ray = new Ray(camTr.position, camTr.forward);

            RaycastHit[] hits = Physics.RaycastAll(ray, maxAimDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            Vector3 point = ray.GetPoint(maxAimDistance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (playerRoot != null && hit.collider.transform.IsChildOf(playerRoot)) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    point = hit.point;
                }
            }
            return point;
        }
    }
}
