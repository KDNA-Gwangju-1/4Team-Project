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
    [Tooltip("Hold on the first frame before she starts coming apart, so the player sees what is dissolving.")]
    public float dissolveFirstFrameHold = 1.2f;
    public float dissolveFrameDuration = 0.16f;
    [Tooltip("Drawn width of what is left. She is a child, not a boss.")]
    public float sisterWidth = 2.4f;
    public string sortingLayer = "Default";
    public int sortingOrder = 1;
    [Tooltip("Drag an empty here to place her by hand in the Scene view. Left empty, she appears where the boss died.")]
    public Transform sisterAnchor;
    [Tooltip("Take the anchor's scale too, so what you see in the Scene view is what plays.")]
    public bool useAnchorScale = true;
    [Tooltip("Where he stands for the scene. Left empty, he stays wherever the fight left him.")]
    public Transform playerAnchor;
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
    [Tooltip("The screen whites out while he is still walking, not after he stops.")]
    public float whiteFadeDelay = 0.3f;
    public float whiteFadeDuration = 2.6f;
    public float whiteHold = 1.5f;

    private Transform sister;
    private DialogueWindow2D window;
    private GameObject overlayCanvas;
    private Image overlay;
    private bool played;

    void Start()
    {
        // the anchor carries a translucent preview so the shot can be composed in
        // the editor; it must never show up in the running game
        if (sisterAnchor != null)
        {
            SpriteRenderer marker = sisterAnchor.GetComponent<SpriteRenderer>();
            if (marker != null) marker.enabled = false;
        }

        if (boss == null) boss = FindObjectOfType<Boss2D>();
        if (boss != null) boss.OnDied += HandleBossDied;
    }

    void OnDestroy()
    {
        if (boss != null) boss.OnDied -= HandleBossDied;
    }

    private Vector3 lastBossPosition;

    void LateUpdate()
    {
        // the boss object is destroyed shortly after it dies, so its last spot
        // has to be remembered while it is still around
        if (!played && bossRenderer != null) lastBossPosition = bossRenderer.transform.position;
    }

    // Dev shortcut: skip straight here without killing the boss first.
    public void PlayNow()
    {
        if (played) return;
        played = true;

        if (boss != null)
        {
            boss.Invulnerable = true;
            BossAttack2D attack = boss.GetComponent<BossAttack2D>();
            if (attack != null) attack.SetPhase(0);
            if (bossRenderer != null) lastBossPosition = bossRenderer.transform.position;
        }
        StartCoroutine(PlayEnding());
    }

    private void HandleBossDied()
    {
        if (played) return;
        played = true;
        if (bossRenderer != null) lastBossPosition = bossRenderer.transform.position;
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

        // put him on his mark, so the ending composes the same way every run
        if (playerAnchor != null)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = playerAnchor.position; }
            player.transform.position = playerAnchor.position;
        }

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

        Vector3 sisterPos = sister != null ? sister.position : new Vector3(lastBossPosition.x, sisterGroundY, 0f);
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

        // --- he walks, and the light takes the screen while he is still walking ---
        yield return new WaitForSeconds(whiteFadeDelay);
        StartCoroutine(Fade(Color.white, 0f, 1f, whiteFadeDuration));
        yield return WalkToHer(sisterPos);

        // whatever is left of the fade, plus a beat on full white
        float remaining = Mathf.Max(0f, whiteFadeDuration - (Time.time - fadeStarted));
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
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

    // Boss2D.DieSequence plays its own dissolve and then destroys the object, so
    // anything we drive through the boss renderer vanishes mid-scene. The ending
    // spawns its own sprite instead and lets the boss tear itself down.
    private IEnumerator DissolveRoutine()
    {
        if (dissolveFrames == null || dissolveFrames.Length == 0) yield break;

        Vector3 where = lastBossPosition;
        if (bossRenderer != null)
        {
            where = bossRenderer.transform.position;
            bossRenderer.enabled = false;
        }
        // a hand-placed mark always wins, so the shot can be composed in the editor
        if (sisterAnchor != null) where = sisterAnchor.position;

        GameObject go = new GameObject("Sister");
        go.transform.position = (sisterAnchor != null)
            ? sisterAnchor.position
            : new Vector3(where.x, sisterGroundY, 0f);
        sister = go.transform;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dissolveFrames[0];
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        if (useAnchorScale && sisterAnchor != null)
        {
            // the marker in the scene IS the composition - don't recompute it
            go.transform.localScale = sisterAnchor.localScale;
        }
        else
        {
            float native = sr.sprite.bounds.size.x;
            if (native > 0.001f)
            {
                float factor = sisterWidth / native;
                go.transform.localScale = new Vector3(factor, factor, 1f);
            }
        }

        // beat on the intact form first - dissolving straight away reads as a
        // glitch rather than as something happening to her
        sr.sprite = dissolveFrames[0];
        yield return new WaitForSeconds(dissolveFirstFrameHold);

        for (int i = 1; i < dissolveFrames.Length; i++)
        {
            sr.sprite = dissolveFrames[i];
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

    private float fadeStarted;

    private IEnumerator Fade(Color colour, float from, float to, float duration)
    {
        if (overlay == null) yield break;
        fadeStarted = Time.time;
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
