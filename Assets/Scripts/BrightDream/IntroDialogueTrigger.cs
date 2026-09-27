using UnityEngine;

namespace BrightDream
{
    /// <summary>
    /// 플레이어가 처음으로 한 발자국이라도 움직이면(이동 입력 발생) 게임을 멈추고
    /// 도입부 대사를 한 줄씩 보여준다. 대사가 끝나면 다시 움직일 수 있게 풀어준다.
    /// 이동/시점 조작을 담당하는 컴포넌트 하나로 되어 있어 그 컴포넌트를 통째로 꺼서 조작을 막는다.
    /// </summary>
    public class IntroDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private SimpleFirstPersonController playerController;
        [SerializeField] private DialogueUI dialogueUI;

        [TextArea(1, 3)]
        [SerializeField]
        private string[] lines =
        {
            "누군가 손으로 꾸며 놓은 꿈 같군.",
            "그런데 이 검보라색 자국은… 정원 장식이라고 보기엔 수상한데.",
            "분명 다른 흔적도 남아 있을 거야. 주변을 살펴보자.",
        };

        private bool triggered;

        private void Update()
        {
            if (triggered) return;
            if (Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) &&
                Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f))
                return;

            triggered = true;
            if (playerController != null) playerController.enabled = false;
            Time.timeScale = 0f;
            dialogueUI.ShowSequence(lines, OnDialogueFinished);
        }

        private void OnDialogueFinished()
        {
            Time.timeScale = 1f;
            if (playerController != null) playerController.enabled = true;
        }
    }
}
