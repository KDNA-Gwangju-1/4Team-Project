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

        [Header("스테이지 진행에 따른 체력 변화")]
        [Tooltip("이 스테이지에 진입하면 최대 체력이 bossStageMaxHealth로 늘어나고 가득 채워진다 (하트 5개 -> 10개).")]
        [SerializeField] private int bossStageIndex = 3;
        [SerializeField] private float bossStageMaxHealth = 200f;

        public event Action<float> OnHealthChanged;

        public float CurrentHealth { get; private set; }
        /// <summary>하트 UI가 표시할 하트 개수를 여기서 계산한다 (하트 1개 = 20 HP).</summary>
        public float MaxHealth => maxHealth;
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

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
            MonsterPurifyManager.OnStageCleared += RestoreFullHealth;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
            MonsterPurifyManager.OnStageCleared -= RestoreFullHealth;
        }

        /// <summary>보스 스테이지에 들어가면 최대 체력이 늘어나며 가득 찬 상태로 시작한다.</summary>
        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != bossStageIndex || isDead) return;

            maxHealth = bossStageMaxHealth;
            CurrentHealth = maxHealth;
            UpdateHealthBar();
        }

        /// <summary>체력을 최대치까지 회복한다 (Stage2 Clear 보상).</summary>
        public void RestoreFullHealth()
        {
            if (isDead) return;

            CurrentHealth = maxHealth;
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
