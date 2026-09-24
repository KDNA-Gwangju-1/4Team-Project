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
        [SerializeField] private bool useChapterSkin = true;
        [Tooltip("단서 텍스트가 떠 있는 동안 꺼둘 컴포넌트 - 이동/시점 컨트롤러, 상호작용 스크립트 등.")]
        [SerializeField] private MonoBehaviour[] disableWhileInvestigating;

        public event Action<string> OnClueCollected;
        public event Action OnAllCluesCollected;

        private readonly HashSet<string> collectedClueIds = new HashSet<string>();
        private string[] investigateParts;
        private int investigatePartIndex;
        private bool pendingAllCluesCollected;
        private DialogueSkinSession skinSession;
        private AudioSource clueVoice;
        private string investigatingClueId;

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
            HangulFont.Apply(progressText);
            HangulFont.Apply(investigateText);
            // 목표(퀘스트) 카드는 모든 씬 공통으로 오른쪽 위에 쌓는다 - 0번 칸.
            ChapterObjectiveStyle.Apply(progressText, ChapterDialogueSkin.Theme.BrightDream, 0);
            if (useChapterSkin && investigatePanel != null)
            {
                var art = ChapterDialogueSkin.Apply(investigatePanel.transform as RectTransform,
                    investigateText, investigatePanel.GetComponent<Graphic>(), null,
                    ChapterDialogueSkin.Theme.BrightDream);
                if (art != null)
                    skinSession = new DialogueSkinSession(art, ChapterDialogueSkin.Theme.BrightDream);
            }
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
            if (clueVoice != null) clueVoice.Stop();
        }

        /// <summary>Stage 1 안내 문구가 뜨는 시점(=CurrentStage가 1이 되는 시점)에 단서 진행도 UI를 노출한다.</summary>
        private void HandleStageChanged(int currentStage)
        {
            if (currentStage == 1 && progressText != null) progressText.gameObject.SetActive(true);
        }

        public bool IsCollected(string clueId) => collectedClueIds.Contains(clueId);

        /// <summary>디버그 스테이지 스킵 등, 단서를 실제로 모으지 않고 건너뛸 때 체크리스트 UI를 정리한다.</summary>
        public void DebugHideProgressUI()
        {
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        /// <summary>단서 조사 완료 처리. 이미 조사된 단서면 아무 것도 하지 않는다 (재조사 시 카운트 증가 방지).</summary>
        public void CollectClue(ClueInteractable clue)
        {
            if (clue == null || collectedClueIds.Contains(clue.ClueId)) return;

            collectedClueIds.Add(clue.ClueId);
            UpdateProgressUI();
            OnClueCollected?.Invoke(clue.ClueId);
            bool allCollected = collectedClueIds.Count >= TotalClueCount;

            // 자동 걷기 중에는 E키를 누를 사람이 없어 대사 UI(멈춤 + 클릭 대기)를 못 넘기니,
            // 조사 대사 없이 조용히 수집 처리만 한다 - 안 그러면 여기서 영원히 멈춘다.
            if (SimpleFirstPersonController.IsAutoWalking)
            {
                if (allCollected) FinishAllClueCollection();
                return;
            }

            investigatingClueId = clue.ClueId;
            ShowInvestigateText(clue.InvestigateText);
            if (allCollected) pendingAllCluesCollected = true;
        }

        /// <summary>단서 4개를 다 모았을 때의 효과 - 정상 흐름(대사 마지막)과 자동 걷기(조용히) 양쪽에서 공유한다.</summary>
        private void FinishAllClueCollection()
        {
            OnAllCluesCollected?.Invoke();
            StageMessageUI.Instance?.ShowMessage("스테이지 1 클리어\n정화총 획득 가능");
            // Stage1이 끝나면 단서 체크리스트는 더 볼 일이 없으므로 끈다
            // (Stage2 정화 진행도 UI가 같은 자리를 이어서 쓴다).
            if (progressText != null) progressText.gameObject.SetActive(false);
        }

        private void UpdateProgressUI()
        {
            if (progressText == null) return;

            // 제목은 굵게 + 개수는 강조색, 찾은 단서는 강조색 ●, 아직 못 찾은 단서는 흐린 ○
            string text = $"<b>기억의 단서</b>   <color=#3E86B0><b>{collectedClueIds.Count} / {TotalClueCount}</b></color>";
            foreach ((string id, string label) in ClueChecklist)
            {
                text += collectedClueIds.Contains(id)
                    ? $"\n<color=#3E86B0>●</color>  {label}"
                    : $"\n<color=#9AA8B2>○  {label}</color>";
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
            skinSession?.Begin();

            foreach (MonoBehaviour mb in disableWhileInvestigating) if (mb != null) mb.enabled = false;
            Time.timeScale = 0f;

            SetInvestigateTextActive(true);
            ShowCurrentInvestigatePart();
        }

        private void Update()
        {
            // ESC 일시정지 중에는 입력을 받지 않는다.
            if (PauseMenu.IsPaused) return;
            if (investigateParts == null) return;
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            investigatePartIndex++;
            if (investigatePartIndex >= investigateParts.Length) EndInvestigateText();
            else ShowCurrentInvestigatePart();
        }

        private void ShowCurrentInvestigatePart()
        {
            // 단서 텍스트는 꿈탐정의 대사가 아니라 언니의 기억(나레이션)이다.
            // 초상화 없는 빈 대사창에 이름은 ???, 문장은 괄호로 감싼다.
            skinSession?.ShowLine(false, "???");
            string part = investigateParts[investigatePartIndex].Trim();
            if (!part.StartsWith("(")) part = "(" + part + ")";
            investigateText.text = part;
            PlayClueVoice();
        }

        private void PlayClueVoice()
        {
            if (clueVoice != null) clueVoice.Stop();
            if (string.IsNullOrEmpty(investigatingClueId)) return;
            var clip = Resources.Load<AudioClip>($"Audio/Chapter1Clues/{investigatingClueId}_{investigatePartIndex + 1:00}");
            if (clip == null) return;
            if (clueVoice == null)
            {
                clueVoice = gameObject.AddComponent<AudioSource>();
                clueVoice.playOnAwake = false;
                clueVoice.spatialBlend = 0f;
                clueVoice.ignoreListenerPause = true;
                clueVoice.volume = 0.85f;
            }
            clueVoice.clip = clip;
            clueVoice.Play();
        }

        private void EndInvestigateText()
        {
            if (clueVoice != null) clueVoice.Stop();
            investigatingClueId = null;
            investigateParts = null;
            SetInvestigateTextActive(false);

            Time.timeScale = 1f;
            foreach (MonoBehaviour mb in disableWhileInvestigating) if (mb != null) mb.enabled = true;

            if (pendingAllCluesCollected)
            {
                pendingAllCluesCollected = false;
                FinishAllClueCollection();
            }
        }

        private void SetInvestigateTextActive(bool active)
        {
            if (investigatePanel != null) investigatePanel.SetActive(active);
            else if (investigateText != null) investigateText.gameObject.SetActive(active);
        }
    }
}
