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

        public const int TargetCount = 15;

        [SerializeField] private Text progressText;

        public event Action<int> OnPurifyCountChanged;

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
            }
        }

        private void UpdateUI()
        {
            if (progressText != null) progressText.text = $"몬스터 정화 {PurifiedCount} / {TargetCount}";
        }
    }
}
