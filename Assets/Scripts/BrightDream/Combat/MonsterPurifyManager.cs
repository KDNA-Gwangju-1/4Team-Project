using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// Stage2 전투 정화 진행도를 관리하는 싱글턴. Stage2 안내 문구가 뜨는 시점(CurrentStage==2)에
    /// 좌측 상단 진행도 UI를 노출한다.
    /// </summary>
    public class MonsterPurifyManager : MonoBehaviour
    {
        public static MonsterPurifyManager Instance { get; private set; }

        public const int TargetCount = 10;

        [SerializeField] private Text progressText;

        public event Action<int> OnPurifyCountChanged;

        /// <summary>정화 목표를 채워 Stage2가 클리어된 순간 1회 발생. ArenaLockdown이 구독해 벽을 다시 연다.
        /// Instance 생성 순서에 의존하지 않도록 StageProgressManager.OnStageChanged와 같은 static 이벤트로 둔다.</summary>
        public static event Action OnStageCleared;

        public int PurifiedCount { get; private set; }

        private bool hasCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateUI();
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage == 2 && progressText != null) progressText.gameObject.SetActive(true);
        }

        public void RegisterPurify()
        {
            PurifiedCount++;
            UpdateUI();
            OnPurifyCountChanged?.Invoke(PurifiedCount);

            if (!hasCleared && PurifiedCount >= TargetCount)
            {
                hasCleared = true;
                StageMessageUI.Instance?.ShowMessage("Stage2 Clear");
                // 남아 있던 몬스터를 모두 정리하고(방금 정화돼 연출 중인 개체는 제외) 아레나 봉쇄를 푼다.
                MonsterCombat.DespawnAll();
                OnStageCleared?.Invoke();
            }
        }

        private void UpdateUI()
        {
            if (progressText != null) progressText.text = $"몬스터 정화 {PurifiedCount} / {TargetCount}";
        }
    }
}
