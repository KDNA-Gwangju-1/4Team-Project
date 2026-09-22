using System;
using UnityEngine;
using UnityEngine.UI;
using BrightDream.Combat;

namespace BrightDream
{
    /// <summary>
    /// 화면 중앙 상단에 남은 시간을 띄우는 타임어택 타이머.
    /// Stage1에 진입하는 순간(StageProgressManager.CurrentStage == 1) 시작해서 0이 되면 게임 오버다.
    ///
    /// Time.deltaTime(스케일 적용)으로 흘려 보내므로 단서 조사 대사처럼 timeScale이 0이 되는 구간에서는
    /// 시계도 같이 멈춘다 - 텍스트를 읽는 동안 시간이 깎이지 않게 하려는 의도적인 선택이다.
    /// 게임 오버 역시 timeScale을 0으로 만들기 때문에 이 타이머도 자연히 멈춘다.
    /// </summary>
    public class TimeAttackTimer : MonoBehaviour
    {
        public static TimeAttackTimer Instance { get; private set; }

        [Tooltip("보통 이 스크립트가 붙은 오브젝트 자신의 Text. 숨길 때 GameObject를 끄면 안 된다 " +
                 "- 이 컴포넌트까지 같이 멈춰서 OnDisable로 구독이 풀리고 타이머가 영영 시작되지 않는다. " +
                 "그래서 Text 컴포넌트의 enabled만 껐다 켠다.")]
        [SerializeField] private Text timerText;
        [Tooltip("제한 시간(초). 기본 3분.")]
        [SerializeField] private float timeLimit = 180f;
        [Tooltip("이 스테이지에 진입하면 카운트다운이 시작된다.")]
        [SerializeField] private int startAtStage = 1;

        [Header("남은 시간이 얼마 없을 때")]
        [Tooltip("남은 시간이 이 값 아래로 내려가면 글자가 붉게 변하며 깜빡인다.")]
        [SerializeField] private float warningTime = 30f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.30f, 0.32f);
        [SerializeField] private float warningBlinksPerSecond = 2f;

        /// <summary>시간이 다 됐을 때 1회 발생. 게임 오버 외에 다른 처리를 붙이고 싶을 때 쓴다.</summary>
        public static event Action OnTimeUp;

        public float RemainingTime { get; private set; }
        public bool IsRunning { get; private set; }

        private bool hasExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RemainingTime = timeLimit;
        }

        private void Start()
        {
            // 타이머가 시작되기 전에는 아예 보이지 않는다 (메인 메뉴 직후 화면에 숫자만 떠 있지 않도록).
            if (timerText != null) timerText.enabled = false;
            UpdateText();
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
            if (currentStage == startAtStage) StartTimer();
        }

        public void StartTimer()
        {
            if (IsRunning || hasExpired) return;

            RemainingTime = timeLimit;
            IsRunning = true;
            if (timerText != null) timerText.enabled = true;
            UpdateText();
        }

        /// <summary>남은 시간을 그대로 둔 채 카운트다운만 멈춘다 (클리어 연출 등에서 사용).</summary>
        public void StopTimer() => IsRunning = false;

        /// <summary>StopTimer로 멈춰 둔 카운트다운을 남은 시간 그대로 다시 이어서 재생한다.
        /// 이미 만료됐거나 애초에 시작된 적이 없으면(RemainingTime == timeLimit인 초기 상태와
        /// 구분할 수 없어 hasExpired만 확인) 아무 일도 하지 않는다.</summary>
        public void ResumeTimer()
        {
            if (hasExpired) return;
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning) return;
            // 단서 조사 텍스트(ClueManager)와 인트로 대사(IntroDialogueTrigger)는 Time.timeScale을
            // 0으로 만들어 Time.deltaTime 자체가 0이 되므로 아래 감산이 저절로 멈춘다.
            // 반면 Stage1ToStage2Cutscene의 대사는 몬스터를 실시간으로 계속 움직여야 해서
            // timeScale을 건드리지 않으므로, DialogueUI.IsShowing을 직접 확인해 함께 막는다.
            if (DialogueUI.IsShowing) return;

            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            UpdateText();

            if (RemainingTime > 0f) return;

            IsRunning = false;
            hasExpired = true;
            OnTimeUp?.Invoke();
            StageMessageUI.Instance?.ShowMessage("TIME OVER");
            GameOverController.Instance?.TriggerGameOver();
        }

        private void UpdateText()
        {
            if (timerText == null) return;

            // 0.4초가 남았을 때 00:00으로 보이면 안 되므로 올림 처리한다.
            int totalSeconds = Mathf.CeilToInt(RemainingTime);
            timerText.text = $"{totalSeconds / 60:0}:{totalSeconds % 60:00}";

            bool warning = IsRunning && RemainingTime <= warningTime;
            if (!warning)
            {
                timerText.color = normalColor;
                return;
            }
            // 경고 구간에서는 붉은색으로 바뀌며 밝기가 오르내린다.
            float blink = Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * warningBlinksPerSecond));
            timerText.color = Color.Lerp(warningColor * 0.6f, warningColor, blink);
        }
    }
}
