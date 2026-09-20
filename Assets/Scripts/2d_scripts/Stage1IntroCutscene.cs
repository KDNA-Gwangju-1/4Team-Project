using System.Collections;
using UnityEngine;

// The dream pulls back and the detective takes it in. Three lines of him
// thinking out loud, in the same talking-head window the rest of the game uses.
public class Stage1IntroCutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Font captionFont;

    public float startOrthoSize = 10f;
    public float revealOrthoSize = 32f;
    public float zoomOutDuration = 3f;

    [Header("Dialogue")]
    [Tooltip("His own window - the art carries his portrait and name plate.")]
    public Sprite playerDialogueFrame;
    [TextArea] public string[] lines = {
        "...언니 쪽이랑은 많이 다른 꿈이네...",
        "...그러고 보니 동생 쪽이 언니를 별로 안 좋아했다고 들었던 것 같은데...",
        "...뭐가 됐든 쉽지 않아 보여. 정신 바짝 차리고 가 보자."
    };
    public float dialogueDelay = 0.5f;
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    public Color dialogueTextColor = Color.white;
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.88f;
    public float dialogueFrameBottomMargin = 24f;
    public float dialogueFrameXOffset = -120f;
    public float lineAutoAdvance = 6f;
    public float lineGap = 0.3f;

    private float gameplayOrthoSize;
    private DialogueWindow2D window;

    void Start()
    {
        if (cam == null || player == null) return;

        gameplayOrthoSize = cam.orthographicSize;
        cam.orthographicSize = startOrthoSize;
        player.enabled = false;

        BuildWindow();
        StartCoroutine(PlayIntro());
    }

    private void BuildWindow()
    {
        if (playerDialogueFrame == null) return;

        window = gameObject.AddComponent<DialogueWindow2D>();
        window.font = captionFont;
        window.textArea = dialogueTextArea;
        window.fontSize = dialogueFontSize;
        window.textColor = dialogueTextColor;
        window.frameWidth01 = dialogueFrameWidth01;
        window.frameBottomMargin = dialogueFrameBottomMargin;
        window.frameXOffset = dialogueFrameXOffset;
        window.lineAutoAdvance = lineAutoAdvance;
        window.lineGap = lineGap;
        window.Build(playerDialogueFrame);
    }

    private IEnumerator PlayIntro()
    {
        yield return ZoomTo(startOrthoSize, revealOrthoSize, zoomOutDuration);
        yield return new WaitForSeconds(dialogueDelay);

        if (window != null && window.Ready)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                yield return window.Show(playerDialogueFrame, lines[i]);
            }
            window.Dispose();
        }

        cam.orthographicSize = gameplayOrthoSize;
        player.enabled = true;
    }

    private IEnumerator ZoomTo(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cam.orthographicSize = to;
    }
}
