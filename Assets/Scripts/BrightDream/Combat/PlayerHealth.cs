using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 플레이어 체력. 무적 상태일 때는 어떤 피해도 무시한다.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Text healthText;

        public event Action<float> OnHealthChanged;

        public float CurrentHealth { get; private set; }
        public bool IsInvincible => invincibleTimer > 0f;

        private float invincibleTimer;
        private bool isDead;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            UpdateHealthBar();
        }

        private void Update()
        {
            if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        }

        /// <summary>무적 상태면 무시된다. grantInvincibility가 true면 이번 피격 이후 invincibilityDuration만큼 무적이 된다.</summary>
        public void TakeDamage(float amount, bool grantInvincibility = false)
        {
            if (IsInvincible || isDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            UpdateHealthBar();
            if (grantInvincibility) invincibleTimer = invincibilityDuration;

            if (CurrentHealth <= 0f)
            {
                isDead = true;
                GameOverController.Instance?.TriggerGameOver();
            }
        }

        private void UpdateHealthBar()
        {
            float ratio = CurrentHealth / maxHealth;
            if (healthBarFill != null) healthBarFill.fillAmount = ratio;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(CurrentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            OnHealthChanged?.Invoke(ratio);
        }
    }
}
