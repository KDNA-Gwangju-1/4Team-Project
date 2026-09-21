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
            // Stage2가 시작되기 전(CurrentStage < 2)에 정화가 필요한 몬스터가 총에 맞아도 진행도로 잡지 않는다.
            // 아레나에 미리 서 있는 장식용 몬스터(MonsterCombat을 비활성화해 둔 개체)를 쏴도 카운트가
            // 오르지 않아야 하는데, MonoBehaviour.enabled = false는 OnTriggerEnter 같은 물리 콜백까지
            // 막아주지는 않는다 - Collider.enabled는 꺼져 있어야 안전하지만, 혹시 다른 경로로 호출되더라도
            // 여기서 한 번 더 막아 Stage2 진행 중이 아니면 절대 카운트가 오르지 않게 한다.
            if (StageProgressManager.Instance == null || StageProgressManager.Instance.CurrentStage < 2) return;

            PurifiedCount++;
            UpdateUI();
            OnPurifyCountChanged?.Invoke(PurifiedCount);

            if (!hasCleared && PurifiedCount >= TargetCount)
            {
                hasCleared = true;
                StageMessageUI.Instance?.ShowMessage("Stage2 Clear");
                // 정화 퀘스트가 끝났으므로 좌측 상단 진행도 UI도 끈다.
                if (progressText != null) progressText.gameObject.SetActive(false);
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
