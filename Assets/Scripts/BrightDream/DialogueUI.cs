using UnityEngine;
using UnityEngine.UI;

namespace BrightDream
{
    /// <summary>
    /// 대사 한 줄씩 보여주는 UI. E키/스페이스바/좌클릭으로 넘기거나, 아무 입력 없어도 3초 뒤 자동으로 다음 줄로 넘어간다.
    /// 마지막 줄에서 넘기면 패널을 닫고 onFinished 콜백을 호출한다.
    /// Time.timeScale이 0이어도(연출 중 게임 일시정지) Update의 실제 입력과 unscaledDeltaTime은 그대로 들어오므로 문제없다.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private const float AutoAdvanceDelay = 3f;

        public static DialogueUI Instance { get; private set; }

        [Tooltip("보통 이 스크립트가 붙은 오브젝트 자신 - 씬에서 처음부터 비활성 상태로 둔다. " +
                 "Awake에서 다시 꺼버리면 안 된다: panel이 비활성이면 Awake 자체가 ShowSequence의 " +
                 "SetActive(true) 호출 도중에야 처음 실행되는데, 그 안에서 다시 꺼버리면 방금 켠 걸 즉시 되돌리게 된다.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text lineText;

        private string[] lines;
        private int lineIndex;
        private float lineTimer;
        private System.Action onFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>대사 목록을 순서대로 보여주기 시작한다. 다 보고 나면 onFinished가 호출된다.</summary>
        public void ShowSequence(string[] sequenceLines, System.Action onFinishedCallback)
        {
            if (sequenceLines == null || sequenceLines.Length == 0) return;

            lines = sequenceLines;
            lineIndex = 0;
            onFinished = onFinishedCallback;

            if (panel != null) panel.SetActive(true);
            ShowCurrentLine();
        }

        private void Update()
        {
            if (lines == null) return;

            lineTimer += Time.unscaledDeltaTime;
            bool advance = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
                           || Input.GetMouseButtonDown(0) || lineTimer >= AutoAdvanceDelay;
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
            if (panel != null) panel.SetActive(false);

            System.Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }
    }
}
