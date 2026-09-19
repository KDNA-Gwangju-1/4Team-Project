using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage3BossIntroCutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Font captionFont;
    public Transform boss;
    public SpriteRenderer bossRenderer;

    public float walkDuration = 1.5f;
    public float walkStepDistance = 4f;

    public float playerShotOrthoSize = 4f;
    public float bossShotOrthoSize = 6f;
    public float panDuration = 0.9f;
    public Vector3 shotOffset = new Vector3(0f, 0.6f, -10f);

    public float questionMarkDelay = 0.3f;
    public float questionMarkDuration = 1f;

    [TextArea] public string bossLine1 = "...누구야, 여기가 어딘 줄 알고.";
    [TextArea] public string playerLine = "네가 감추고 있는 진실을 찾으러 왔어.";
    [TextArea] public string bossLine2 = "그럼... 뺏어봐!";
    public string bossSpeakerName = "???";
    public string playerSpeakerName = "꿈탐정";
    public float lineDisplayDuration = 2f;
    public float lineGap = 0.3f;

    [Header("Staging")]
    [Tooltip("Hidden until the camera turns on her - this is her first appearance.")]
    public GameObject[] revealWithBoss;
    [Tooltip("Hidden for the whole cutscene, switched on when control returns - monsters, HUD.")]
    public GameObject[] revealAfterCutscene;

    public Sprite[] tendrilAttackFrames;
    public Sprite[] tendrilDissolveFrames;
    public float tendrilFrameDuration = 0.08f;
    public float tendrilXOffsetFromBoss = 2.5f;
    public float tendrilFloorY = -1.654f;
    public float tendrilScale = 3f;

    private Rigidbody2D playerRb;
    private CameraFollow2D camFollow;
    private float gameplayOrthoSize;
    private Vector3 gameplayCamOffset;
    private Text captionText;
    private Text speakerText;

    void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        if (player == null || cam == null || boss == null) yield break;

        playerRb = player.GetComponent<Rigidbody2D>();
        camFollow = cam.GetComponent<CameraFollow2D>();
        gameplayOrthoSize = cam.orthographicSize;
        if (camFollow != null) gameplayCamOffset = camFollow.offset;

        player.enabled = false;
        // nothing on stage but the player until the script says otherwise
        SetActiveAll(revealWithBoss, false);
        SetActiveAll(revealAfterCutscene, false);
        CreateCaption();

        StartCoroutine(ZoomOrthoTo(cam.orthographicSize, playerShotOrthoSize, panDuration));

        float startX = playerRb != null ? playerRb.position.x : player.transform.position.x;
        float stopX = startX + walkStepDistance;

        if (playerRb != null)
        {
            float t = 0f;
            while (t < walkDuration)
            {
                t += Time.deltaTime;
                float newX = Mathf.Lerp(startX, stopX, Mathf.Clamp01(t / walkDuration));

                playerRb.linearVelocity = new Vector2(1f, playerRb.linearVelocity.y);
                playerRb.position = new Vector2(newX, playerRb.position.y);
                player.transform.position = new Vector3(newX, player.transform.position.y, player.transform.position.z);
                yield return null;
            }
            playerRb.position = new Vector2(stopX, playerRb.position.y);
            player.transform.position = new Vector3(stopX, player.transform.position.y, player.transform.position.z);
        }
        if (playerRb != null) playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);

        yield return new WaitForSeconds(questionMarkDelay);
        yield return StartCoroutine(ShowQuestionMark());

        if (camFollow != null) camFollow.enabled = false;

        if (bossRenderer != null) bossRenderer.flipX = true;

        // she appears as the camera swings over - that is the reveal
        SetActiveAll(revealWithBoss, true);

        Vector3 bossShotPos = boss.position + shotOffset;
        Vector3 playerShotPos = (Vector3)(playerRb != null ? (Vector2)playerRb.position : (Vector2)player.transform.position) + shotOffset;

        yield return StartCoroutine(PanCameraTo(bossShotPos, bossShotOrthoSize, panDuration));
        yield return StartCoroutine(ShowLine(bossSpeakerName, bossLine1));

        yield return StartCoroutine(PanCameraTo(playerShotPos, playerShotOrthoSize, panDuration));
        yield return StartCoroutine(ShowLine(playerSpeakerName, playerLine));

        yield return StartCoroutine(PanCameraTo(bossShotPos, bossShotOrthoSize, panDuration));
        yield return StartCoroutine(PlayTendrilClaw());
        yield return StartCoroutine(ShowLine(bossSpeakerName, bossLine2));

        DestroyCaption();

        yield return StartCoroutine(PanCameraTo(playerShotPos, gameplayOrthoSize, panDuration));

        if (camFollow != null)
        {
            camFollow.offset = gameplayCamOffset;
            camFollow.enabled = true;
        }

        // cutscene over: the fight starts, and everything else walks on
        SetActiveAll(revealAfterCutscene, true);
        player.enabled = true;
        Destroy(gameObject);
    }

    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(active);
        }
    }

    private IEnumerator ShowQuestionMark()
    {
        GameObject qmGO = new GameObject("BossIntroQuestionMark");
        qmGO.transform.position = player.transform.position + new Vector3(0f, 1.6f, 0f);
        TextMesh tm = qmGO.AddComponent<TextMesh>();
        tm.text = "?";
        tm.characterSize = 0.2f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.92f, 0.3f);
        MeshRenderer mr = qmGO.GetComponent<MeshRenderer>();
        mr.sortingLayerName = "Default";
        mr.sortingOrder = 100;

        Vector3 baseScale = Vector3.one * 0.6f;
        qmGO.transform.localScale = Vector3.zero;

        float popDuration = 0.2f;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            qmGO.transform.localScale = Vector3.Lerp(Vector3.zero, baseScale * 1.3f, t / popDuration);
            yield return null;
        }
        t = 0f;
        float settleDuration = 0.12f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            qmGO.transform.localScale = Vector3.Lerp(baseScale * 1.3f, baseScale, t / settleDuration);
            yield return null;
        }

        yield return new WaitForSeconds(questionMarkDuration);

        t = 0f;
        float fadeDuration = 0.2f;
        Vector3 fromScale = qmGO.transform.localScale;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            qmGO.transform.localScale = Vector3.Lerp(fromScale, Vector3.zero, t / fadeDuration);
            yield return null;
        }

        Destroy(qmGO);
    }

    private IEnumerator PlayTendrilClaw()
    {
        if (tendrilAttackFrames == null || tendrilAttackFrames.Length == 0) yield break;

        GameObject tendrilGO = new GameObject("BossIntroTendrilClaw");
        tendrilGO.transform.position = new Vector3(boss.position.x + tendrilXOffsetFromBoss, tendrilFloorY, 0f);
        tendrilGO.transform.localScale = Vector3.one * tendrilScale;
        SpriteRenderer sr = tendrilGO.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = bossRenderer != null ? bossRenderer.sortingLayerName : "Default";
        sr.sortingOrder = (bossRenderer != null ? bossRenderer.sortingOrder : 0) + 1;

        foreach (Sprite frame in tendrilAttackFrames)
        {
            sr.sprite = frame;
            yield return new WaitForSeconds(tendrilFrameDuration);
        }

        if (tendrilDissolveFrames != null)
        {
            foreach (Sprite frame in tendrilDissolveFrames)
            {
                sr.sprite = frame;
                yield return new WaitForSeconds(tendrilFrameDuration);
            }
        }

        Destroy(tendrilGO);
    }

    private IEnumerator PanCameraTo(Vector3 targetPos, float targetOrtho, float duration)
    {
        Vector3 fromPos = cam.transform.position;
        float fromOrtho = cam.orthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float lerpT = Mathf.Clamp01(elapsed / duration);
            cam.transform.position = Vector3.Lerp(fromPos, targetPos, lerpT);
            cam.orthographicSize = Mathf.Lerp(fromOrtho, targetOrtho, lerpT);
            yield return null;
        }
        cam.transform.position = targetPos;
        cam.orthographicSize = targetOrtho;
    }

    private IEnumerator ZoomOrthoTo(float from, float to, float duration)
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

    private IEnumerator ShowLine(string speaker, string line)
    {
        if (speakerText != null) speakerText.text = speaker;
        if (captionText != null) captionText.text = line;
        yield return new WaitForSeconds(lineDisplayDuration);
        if (speakerText != null) speakerText.text = "";
        if (captionText != null) captionText.text = "";
        yield return new WaitForSeconds(lineGap);
    }

    private void CreateCaption()
    {
        GameObject canvasGO = new GameObject("BossIntroCaptionCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject speakerGO = new GameObject("BossIntroSpeakerText");
        speakerGO.transform.SetParent(canvasGO.transform, false);
        speakerText = speakerGO.AddComponent<Text>();
        speakerText.font = captionFont;
        speakerText.text = "";
        speakerText.fontSize = 28;
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.alignment = TextAnchor.MiddleCenter;
        speakerText.color = new Color(1f, 0.85f, 0.5f);
        RectTransform srt = speakerText.rectTransform;
        srt.anchorMin = new Vector2(0.5f, 0f);
        srt.anchorMax = new Vector2(0.5f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 170f);
        srt.sizeDelta = new Vector2(900f, 50f);

        GameObject textGO = new GameObject("BossIntroCaptionText");
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

    private void DestroyCaption()
    {
        if (captionText != null) Destroy(captionText.transform.parent.gameObject);
    }
}
