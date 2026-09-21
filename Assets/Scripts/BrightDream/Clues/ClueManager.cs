using System;
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

        /// <summary>체크리스트 표시 순서 및 힌트 이름. 실제 판정은 clueId로 이루어진다.</summary>
        private static readonly (string Id, string Label)[] ClueChecklist =
        {
            ("clue_01_ribbon", "리본"),
            ("clue_03_tree_carving", "이름"),
            ("clue_02_photoalbum", "사진첩"),
            ("clue_04_unsent_letter", "편지"),
        };

        [Header("UI 참조")]
        [SerializeField] private Text progressText;
        [SerializeField] private Text investigateText;
        [Tooltip("investigateText를 감싸는 배경(대사ui02) 오브젝트 - 켜고 끄는 대상은 텍스트 자신이 아니라 이 패널이다. " +
                 "비워두면 기존처럼 investigateText 자신의 GameObject를 켜고 끈다.")]
        [SerializeField] private GameObject investigatePanel;
        [Tooltip("단서 텍스트가 떠 있는 동안 꺼둘 컴포넌트 - 이동/시점 컨트롤러, 상호작용 스크립트 등.")]
        [SerializeField] private MonoBehaviour[] disableWhileInvestigating;

        public event Action<string> OnClueCollected;
        public event Action OnAllCluesCollected;

        private readonly HashSet<string> collectedClueIds = new HashSet<string>();
        private string[] investigateParts;
        private int investigatePartIndex;
        private bool pendingAllCluesCollected;

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
            SetInvestigateTextActive(false);
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
            if (collectedClueIds.Count >= TotalClueCount) pendingAllCluesCollected = true;
        }

        private void UpdateProgressUI()
        {
            if (progressText == null) return;

            string text = $"단서 {collectedClueIds.Count} / {TotalClueCount}";
            foreach ((string id, string label) in ClueChecklist)
            {
                text += "\n[" + (collectedClueIds.Contains(id) ? "○" : " ") + "] " + label;
            }
            progressText.text = text;
        }

        /// <summary>
        /// investigateText 안에 빈 줄("\n\n")이 있으면 그걸 기준으로 잘라 각 줄을 순서대로 보여준다
        /// (예: 편지 단서처럼 "...\n\n잠시 후...\n\n..." 형태로 대사를 두 번에 나눠 띄우고 싶을 때).
        /// 플레이어는 멈추고, 좌클릭/스페이스바를 누를 때마다 다음 줄로 넘어가며 마지막 줄에서는 게임플레이로 복귀한다.
        /// </summary>
        private void ShowInvestigateText(string text)
        {
            if (investigateText == null) return;

            investigateParts = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            investigatePartIndex = 0;

            foreach (MonoBehaviour mb in disableWhileInvestigating) if (mb != null) mb.enabled = false;
            Time.timeScale = 0f;

            SetInvestigateTextActive(true);
            ShowCurrentInvestigatePart();
        }

        private void Update()
        {
            if (investigateParts == null) return;
            if (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space)) return;

            investigatePartIndex++;
            if (investigatePartIndex >= investigateParts.Length) EndInvestigateText();
            else ShowCurrentInvestigatePart();
        }

        private void ShowCurrentInvestigatePart()
        {
            investigateText.text = investigateParts[investigatePartIndex].Trim();
        }

        private void EndInvestigateText()
        {
            investigateParts = null;
            SetInvestigateTextActive(false);

            Time.timeScale = 1f;
            foreach (MonoBehaviour mb in disableWhileInvestigating) if (mb != null) mb.enabled = true;

            if (pendingAllCluesCollected)
            {
                pendingAllCluesCollected = false;
                OnAllCluesCollected?.Invoke();
                StageMessageUI.Instance?.ShowMessage("Stage1 Clear\n정화총 획득가능");
                // Stage1이 끝나면 단서 체크리스트는 더 볼 일이 없으므로 끈다
                // (Stage2 정화 진행도 UI가 같은 자리를 이어서 쓴다).
                if (progressText != null) progressText.gameObject.SetActive(false);
            }
        }

        private void SetInvestigateTextActive(bool active)
        {
            if (investigatePanel != null) investigatePanel.SetActive(active);
            else if (investigateText != null) investigateText.gameObject.SetActive(active);
        }
    }
}
