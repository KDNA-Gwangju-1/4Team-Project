using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Plays when the boss dies. The monster comes apart and what is left is the
// little sister, so the scene has to stop being a fight before anyone speaks.
// Hooks itself to Boss2D.OnDied - nothing else needs to call it.
public class Stage3EndingCutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Boss2D boss;
    public SpriteRenderer bossRenderer;
    public Font captionFont;

    [Header("Opening blackout")]
    public float blackoutInDuration = 0.4f;
    public int blackoutBlinks = 2;
    public float blackoutBlinkTime = 0.1f;
    public float blackoutHold = 0.5f;
    public float blackoutOutDuration = 0.8f;

    [Header("Dissolve")]
    [Tooltip("The boss coming apart. The last frame is what stays on screen - the sister.")]
    public Sprite[] dissolveFrames;
    public float dissolveFrameDuration = 0.16f;
    [Tooltip("Drawn width of what is left. She is a child, not a boss.")]
    public float sisterWidth = 2.4f;
    public float sisterGroundY = -2.63f;
    public float holdAfterDissolve = 0.9f;

    [Header("Camera")]
    public float shotOrthoSize = 5f;
    public float panDuration = 1f;
    public Vector3 shotOffset = new Vector3(0f, 0.6f, -10f);

    [Header("Dialogue")]
    public Sprite playerDialogueFrame;
    [Tooltip("The sister's portrait. Falls back to the boss frame until her own art exists.")]
    public Sprite sisterDialogueFrame;
    public DialogueLine2D[] lines;
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.88f;
    public float dialogueFrameBottomMargin = 24f;
    public float dialogueFrameXOffset = -120f;
    public float lineMinDuration = 0.5f;
    public float lineAutoAdvance = 6f;
    public float lineGap = 0.3f;

    [Header("Walk to her")]
    [Tooltip("How close he gets before the screen takes over.")]
    public float stopGap = 2.2f;
    public float walkSpeed = 2.4f;
    public float walkFrameDuration = 0.13f;

    [Header("Fade to white")]
    public float whiteFadeDuration = 2.6f;
    public float whiteHold = 1.5f;

    private DialogueWindow2D window;
    private GameObject overlayCanvas;
    private Image overlay;
    private bool played;

    void Start()
    {
        if (boss == null) boss = FindObjectOfType<Boss2D>();
        if (boss != null) boss.OnDied += HandleBossDied;
    }

    void OnDestroy()
    {
        if (boss != null) boss.OnDied -= HandleBossDied;
    }

    private void HandleBossDied()
    {
        if (played) return;
        played = true;
        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        if (player == null) player = PlayerMovement2D.Instance;
        if (cam == null) cam = Camera.main;
        if (player == null || cam == null) yield break;

        player.enabled = false;
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow != null) follow.enabled = false;

        BuildOverlay();
        BuildWindow();

        // --- black, so the fight is over before anything else happens ---
        yield return Fade(Color.black, 0f, 1f, blackoutInDuration);
        yield return Blink();
        ClearBattlefield();
        yield return new WaitForSeconds(blackoutHold);
        yield return Fade(Color.black, 1f, 0f, blackoutOutDuration);

        // --- she comes apart, and the child is what is left ---
        yield return DissolveRoutine();

        Vector3 sisterPos = boss != null ? boss.transform.position : player.transform.position;
        Vector3 twoShot = new Vector3((player.transform.position.x + sisterPos.x) * 0.5f,
                                      sisterGroundY + 1.6f, shotOffset.z);
        yield return PanTo(twoShot, shotOrthoSize, panDuration);
        yield return new WaitForSeconds(holdAfterDissolve);

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine2D line = lines[i];
            if (line == null || string.IsNullOrEmpty(line.text)) continue;

            Sprite frame = line.isBoss
                ? (sisterDialogueFrame != null ? sisterDialogueFrame : playerDialogueFrame)
                : playerDialogueFrame;
            if (window != null && window.Ready) yield return window.Show(frame, line.text);
        }

        if (window != null) window.Dispose();

        yield return WalkToHer(sisterPos);

        // --- out, on white ---
        yield return Fade(Color.white, 0f, 1f, whiteFadeDuration);
        yield return new WaitForSeconds(whiteHold);
    }

    // Anything still flying or crawling would undercut the moment.
    private void ClearBattlefield()
    {
        foreach (TentacleStrike2D t in FindObjectsOfType<TentacleStrike2D>()) Destroy(t.gameObject);
        foreach (BossBullet2D b in FindObjectsOfType<BossBullet2D>()) Destroy(b.gameObject);
        foreach (Monster2D m in FindObjectsOfType<Monster2D>()) m.gameObject.SetActive(false);
        foreach (RangedMonster2D m in FindObjectsOfType<RangedMonster2D>()) m.gameObject.SetActive(false);
    }

    private IEnumerator DissolveRoutine()
    {
        if (bossRenderer == null || dissolveFrames == null || dissolveFrames.Length == 0) yield break;

        // the animator would overwrite every frame we set
        MonsterSpriteAnimator2D animator = bossRenderer.GetComponent<MonsterSpriteAnimator2D>();
        if (animator != null) animator.enabled = false;

        LightSilhouette2D shadow = bossRenderer.GetComponent<LightSilhouette2D>();
        if (shadow != null) shadow.Active = false;

        BossFlight2D flight = bossRenderer.GetComponent<BossFlight2D>();
        if (flight != null) flight.Stop();

        bossRenderer.enabled = true;
        bossRenderer.color = Color.white;

        // she is a child now, and she is on the ground
        Sprite last = dissolveFrames[dissolveFrames.Length - 1];
        float native = last.bounds.size.x;
        if (native > 0.001f)
        {
            float factor = sisterWidth / native;
            bossRenderer.transform.localScale = new Vector3(factor, factor, bossRenderer.transform.localScale.z);
        }
        Vector3 p = bossRenderer.transform.position;
        bossRenderer.transform.position = new Vector3(p.x, sisterGroundY, p.z);

        for (int i = 0; i < dissolveFrames.Length; i++)
        {
            bossRenderer.sprite = dissolveFrames[i];
            yield return new WaitForSeconds(dissolveFrameDuration);
        }
    }

    private IEnumerator WalkToHer(Vector3 sisterPos)
    {
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        PlayerSpriteAnimator2D anim = player.GetComponent<PlayerSpriteAnimator2D>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (anim != null) anim.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;

        float dir = Mathf.Sign(sisterPos.x - player.transform.position.x);
        if (Mathf.Approximately(dir, 0f)) dir = 1f;
        if (sr != null) sr.flipX = dir < 0f;

        Sprite[] walk = (anim != null && anim.walkFrames != null && anim.walkFrames.Length > 0) ? anim.walkFrames : null;
        float targetX = sisterPos.x - dir * stopGap;
        float frameTimer = 0f;
        int frame = 0;

        while (Mathf.Abs(targetX - player.transform.position.x) > 0.05f)
        {
            Vector3 next = Vector3.MoveTowards(player.transform.position,
                new Vector3(targetX, player.transform.position.y, player.transform.position.z),
                walkSpeed * Time.deltaTime);
            player.transform.position = next;
            if (body != null) body.position = next;

            if (walk != null)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= walkFrameDuration)
                {
                    frameTimer -= walkFrameDuration;
                    frame = (frame + 1) % walk.Length;
                    if (sr != null) sr.sprite = walk[frame];
                }
            }
            yield return null;
        }

        if (walk != null && sr != null) sr.sprite = walk[0];
    }

    // ---------- helpers ----------

    private void BuildWindow()
    {
        Sprite reference = playerDialogueFrame != null ? playerDialogueFrame : sisterDialogueFrame;
        if (reference == null) return;

        window = gameObject.AddComponent<DialogueWindow2D>();
        window.font = captionFont;
        window.textArea = dialogueTextArea;
        window.fontSize = dialogueFontSize;
        window.frameWidth01 = dialogueFrameWidth01;
        window.frameBottomMargin = dialogueFrameBottomMargin;
        window.frameXOffset = dialogueFrameXOffset;
        window.lineMinDuration = lineMinDuration;
        window.lineAutoAdvance = lineAutoAdvance;
        window.lineGap = lineGap;
        window.Build(reference);
    }

    private void BuildOverlay()
    {
        overlayCanvas = new GameObject("EndingOverlay");
        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        overlay = overlayCanvas.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = false;
        RectTransform rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator Fade(Color colour, float from, float to, float duration)
    {
        if (overlay == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            overlay.color = new Color(colour.r, colour.g, colour.b, a);
            yield return null;
        }
        overlay.color = new Color(colour.r, colour.g, colour.b, to);
    }

    private IEnumerator Blink()
    {
        for (int i = 0; i < blackoutBlinks; i++)
        {
            overlay.color = new Color(0f, 0f, 0f, 0.55f);
            yield return new WaitForSeconds(blackoutBlinkTime);
            overlay.color = Color.black;
            yield return new WaitForSeconds(blackoutBlinkTime);
        }
    }

    private IEnumerator PanTo(Vector3 target, float targetOrtho, float duration)
    {
        Vector3 from = cam.transform.position;
        float fromOrtho = cam.orthographicSize;
        Vector3 to = new Vector3(target.x, target.y, from.z);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            cam.transform.position = Vector3.Lerp(from, to, k);
            cam.orthographicSize = Mathf.Lerp(fromOrtho, targetOrtho, k);
            yield return null;
        }
        cam.transform.position = to;
        cam.orthographicSize = targetOrtho;
    }
}
