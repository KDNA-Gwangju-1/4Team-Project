using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 정화총을 획득한 뒤(WeaponPickup.PlayerHasWeapon) 좌클릭으로 총알을 발사한다.
    /// </summary>
    public class PurifierGunController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float fireCooldown = 0.3f;

        private float cooldownTimer;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
        }

        private void Update()
        {
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
            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(playerCamera.transform.forward));
            projectile.SetActive(true);
        }
    }
}
