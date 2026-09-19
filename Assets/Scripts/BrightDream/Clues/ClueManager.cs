using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Clues
{
    /// <summary>
    /// 단서 전체 진행도를 관리한다. 최종 기획 기준 단서는 4개이며,
    /// 실제로 씬에 몇 개가 배치되어 있는지와 무관하게 분모는 항상 4로 고정한다.
    /// 4번째 단서가 조사되면 OnAllCluesCollected 가 발생한다 (정화총 지급/전투 전환은 이후 다른 시스템에서 구독해서 처리).
    /// </summary>
    public class ClueManager : MonoBehaviour
    {
        public static ClueManager Instance { get; private set; }

        public const int TotalClueCount = 4;

        [Header("UI 참조")]
        [SerializeField] private Text progressText;
        [SerializeField] private Text investigateText;
        [SerializeField] private float investigateTextDuration = 4f;
        [SerializeField] private float pauseBetweenLines = 1f;

        public event Action<string> OnClueCollected;
        public event Action OnAllCluesCollected;

        private readonly HashSet<string> collectedClueIds = new HashSet<string>();
        private Coroutine hideTextRoutine;

        public int CollectedCount => collectedClueIds.Count;

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
            UpdateProgressUI();
            if (investigateText != null) investigateText.gameObject.SetActive(false);
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

        /// <summary>Stage 1 안내 문구가 뜨는 시점(=CurrentStage가 1이 되는 시점)에 단서 진행도 UI를 노출한다.</summary>
        private void HandleStageChanged(int currentStage)
        {
            if (currentStage == 1 && progressText != null) progressText.gameObject.SetActive(true);
        }

        public bool IsCollected(string clueId) => collectedClueIds.Contains(clueId);

        /// <summary>단서 조사 완료 처리. 이미 조사된 단서면 아무 것도 하지 않는다 (재조사 시 카운트 증가 방지).</summary>
        public void CollectClue(ClueInteractable clue)
        {
            if (clue == null || collectedClueIds.Contains(clue.ClueId)) return;

            collectedClueIds.Add(clue.ClueId);
            ShowInvestigateText(clue.InvestigateText);
            UpdateProgressUI();

            OnClueCollected?.Invoke(clue.ClueId);
            if (collectedClueIds.Count >= TotalClueCount) OnAllCluesCollected?.Invoke();
        }

        private void UpdateProgressUI()
        {
            if (progressText != null) progressText.text = $"단서 {collectedClueIds.Count} / {TotalClueCount}";
        }

        /// <summary>
        /// investigateText 안에 빈 줄("\n\n")이 있으면 그걸 기준으로 잘라 각 줄을 순서대로 보여준다
        /// (예: 편지 단서처럼 "...\n\n잠시 후...\n\n..." 형태로 대사를 두 번에 나눠 띄우고 싶을 때).
        /// </summary>
        private void ShowInvestigateText(string text)
        {
            if (investigateText == null) return;
            if (hideTextRoutine != null) StopCoroutine(hideTextRoutine);
            string[] parts = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            hideTextRoutine = StartCoroutine(ShowTextSequence(parts));
        }

        private IEnumerator ShowTextSequence(string[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                investigateText.text = parts[i].Trim();
                investigateText.gameObject.SetActive(true);
                yield return new WaitForSeconds(investigateTextDuration);
                investigateText.gameObject.SetActive(false);
                if (i < parts.Length - 1) yield return new WaitForSeconds(pauseBetweenLines);
            }
            hideTextRoutine = null;
        }
    }
}
