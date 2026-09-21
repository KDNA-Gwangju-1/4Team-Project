using UnityEngine;
using UnityEngine.UI;

namespace BrightDream
{
    /// <summary>
    /// 대사 한 줄씩 보여주는 UI. E키/스페이스바/좌클릭으로 넘기거나, 아무 입력 없어도 2.5초 뒤 자동으로 다음 줄로 넘어간다.
    /// 마지막 줄에서 넘기면 패널을 닫고 onFinished 콜백을 호출한다.
    /// Time.timeScale이 0이어도(연출 중 게임 일시정지) Update의 실제 입력과 unscaledDeltaTime은 그대로 들어오므로 문제없다.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private const float AutoAdvanceDelay = 2.5f;

        public static DialogueUI Instance { get; private set; }

        /// <summary>
        /// 패널이 떠서 대사가 진행 중인 동안 true. TimeAttackTimer가 이걸 보고 시간을 멈춘다.
        /// IntroDialogueTrigger처럼 Time.timeScale을 0으로 만드는 호출자는 그것만으로도 이미
        /// 멈추지만, Stage1ToStage2Cutscene은 몬스터가 실시간으로 계속 움직여야 해서 timeScale을
        /// 건드리지 않는다 - 그런 경우까지 포함해 항상 안전하게 멈추도록 별도 플래그로 알린다.
        /// </summary>
        public static bool IsShowing { get; private set; }

        [Tooltip("보통 이 스크립트가 붙은 오브젝트 자신 - 씬에서 처음부터 비활성 상태로 둔다. " +
                 "Awake에서 다시 꺼버리면 안 된다: panel이 비활성이면 Awake 자체가 ShowSequence의 " +
                 "SetActive(true) 호출 도중에야 처음 실행되는데, 그 안에서 다시 꺼버리면 방금 켠 걸 즉시 되돌리게 된다.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text lineText;

        private string[] lines;
        private int lineIndex;
        private float lineTimer;
        private bool allowSkipInput = true;
        private System.Action onFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // 씬 재시작(Game Over -> 재시작) 시에도 이전 판에서 대사가 떠 있던 채로 멈췄을 수 있는
            // static 플래그를 깨끗하게 되돌린다.
            IsShowing = false;
        }

        /// <summary>
        /// 대사 목록을 순서대로 보여주기 시작한다. 다 보고 나면 onFinished가 호출된다.
        /// allowSkip이 false면 어떤 키로도 넘길 수 없고 자동 넘김(2.5초)으로만 진행된다
        /// - 연출 중 마구 누르다가 대사가 통째로 넘어가 버리는 걸 막기 위한 옵션이다.
        /// </summary>
        public void ShowSequence(string[] sequenceLines, System.Action onFinishedCallback, bool allowSkip = true)
        {
            if (sequenceLines == null || sequenceLines.Length == 0) return;

            lines = sequenceLines;
            lineIndex = 0;
            allowSkipInput = allowSkip;
            onFinished = onFinishedCallback;
            IsShowing = true;

            if (panel != null) panel.SetActive(true);
            ShowCurrentLine();
        }

        private void Update()
        {
            if (lines == null) return;

            lineTimer += Time.unscaledDeltaTime;
            bool skipPressed = allowSkipInput && (Input.GetKeyDown(KeyCode.E)
                                                  || Input.GetKeyDown(KeyCode.Space)
                                                  || Input.GetMouseButtonDown(0));
            bool advance = skipPressed || lineTimer >= AutoAdvanceDelay;
            if (!advance) return;

            lineIndex++;
            if (lineIndex >= lines.Length) EndSequence();
            else ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            lineTimer = 0f;
            if (lineText != null) lineText.text = lines[lineIndex];
        }

        private void EndSequence()
        {
            lines = null;
            IsShowing = false;
            if (panel != null) panel.SetActive(false);

            System.Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }
    }
}
