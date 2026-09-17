using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage1IntroCutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Font captionFont;

    public float startOrthoSize = 10f;
    public float revealOrthoSize = 32f;
    public float zoomOutDuration = 3f;

    [TextArea] public string dialogueLine = "언니랑은... 꿈이 많이 다르네...";
    public float dialogueDelay = 0.5f;
    public float dialogueDisplayDuration = 2.5f;

    private float gameplayOrthoSize;
    private Text captionText;

    void Start()
    {
        if (cam == null || player == null) return;

        gameplayOrthoSize = cam.orthographicSize;
        cam.orthographicSize = startOrthoSize;
        player.enabled = false;

        CreateCaption();
        StartCoroutine(PlayIntro());
    }

    private void CreateCaption()
    {
        GameObject canvasGO = new GameObject("IntroCaptionCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject textGO = new GameObject("IntroCaptionText");
        textGO.transform.SetParent(canvasGO.transform, false);

        captionText = textGO.AddComponent<Text>();
        captionText.font = captionFont;
        captionText.text = "";
        captionText.fontSize = 36;
        captionText.alignment = TextAnchor.MiddleCenter;
        captionText.color = Color.white;

        RectTransform rt = captionText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta = new Vector2(900f, 100f);
    }

    private IEnumerator PlayIntro()
    {
        yield return ZoomTo(startOrthoSize, revealOrthoSize, zoomOutDuration);

        yield return new WaitForSeconds(dialogueDelay);
        if (captionText != null) captionText.text = dialogueLine;

        yield return new WaitForSeconds(dialogueDisplayDuration);
        if (captionText != null) Destroy(captionText.transform.parent.gameObject);

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
