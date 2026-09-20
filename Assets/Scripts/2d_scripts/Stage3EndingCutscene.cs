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

    [Header("Staging")]
    // The ending has to stop looking like the fight it interrupts: HUD off, beam
    // off, animator off. Otherwise it reads as gameplay that happens to be
    // playing a cutscene on top.
    [Tooltip("Switched off for the whole scene - HUD, monsters, anything still on stage.")]
    public GameObject[] hideDuringCutscene;
    [Tooltip("The light cone. He is not searching any more.")]
    public GameObject flashlightCone;
    [Tooltip("The flashlight he carries. Hidden so the standing pose stays clean.")]
    public GameObject heldFlashlight;

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
    [Tooltip("How close the camera sits while she comes apart. Smaller is tighter - this is the one moment the art is worth looking at.")]
    public float dissolveOrthoSize = 3.8f;
    [Tooltip("Pull back out to the two-shot over this long, once she is gone.")]
    public float dissolvePullOutDuration = 1.4f;
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
    [Tooltip("Pose he settles into once he stops. Left empty, his first idle frame is used - a walk frame leaves him mid-stride.")]
    public Sprite arrivedSprite;

    [Header("Fade to white")]
    [Tooltip("The screen whites out while he is still walking, not after he stops.")]
    public float whiteFadeDelay = 0.3f;
    public float whiteFadeDuration = 2.6f;
    public float whiteHold = 1.5f;

    private Transform sister;
    private SpriteRenderer sisterRenderer;
    private DialogueWindow2D window;
    private GameObject overlayCanvas;
    private Image overlay;
    private bool played;
    private bool sceneActive;
    private Transform detective;
    private SpriteRenderer detectiveRenderer;
    private Sprite[] detectiveWalk;
    private Sprite detectiveIdle;

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

    void Update()
    {
        // Killing the source is not quite enough: a fan already mid-coroutine, a
        // monster re-enabling itself, anything we have not thought of, still puts
        // bullets on screen. While the ending runs, nothing gets to.
        if (!sceneActive) return;

        BossBullet2D[] shots = FindObjectsOfType<BossBullet2D>();
        for (int i = 0; i < shots.Length; i++)
        {
            if (shots[i] != null) Destroy(shots[i].gameObject);
        }

        TentacleStrike2D[] live = FindObjectsOfType<TentacleStrike2D>();
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] == null) continue;
            // stop the strike routine first - Destroy only lands at the end of the
            // frame, and the animation would keep writing to a dead renderer
            live[i].StopAllCoroutines();
            Destroy(live[i].gameObject);
        }
    }

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
        player.CutsceneInvulnerable = true;
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow != null) follow.enabled = false;

        // put him on his mark, so the ending composes the same way every run
        if (playerAnchor != null)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = playerAnchor.position; }
            player.transform.position = playerAnchor.position;
        }

        sceneActive = true;
        StageScene();
        BuildOverlay();
        BuildWindow();

        // --- black, so the fight is over before anything else happens ---
        yield return Fade(Color.black, 0f, 1f, blackoutInDuration);
        yield return Blink();
        ClearBattlefield();
        yield return new WaitForSeconds(blackoutHold);

        // she is built while the screen is still black, so the camera can be put
        // right on her before anyone sees where it went
        BuildSister();
        SnapTo(DissolveShotCentre(), dissolveOrthoSize);

        yield return Fade(Color.black, 1f, 0f, blackoutOutDuration);

        // --- she comes apart, and the child is what is left ---
        yield return DissolveFrames();

        Vector3 sisterPos = sister != null ? sister.position : new Vector3(lastBossPosition.x, sisterGroundY, 0f);
        Vector3 anchorX = detective != null ? detective.position : player.transform.position;
        Vector3 twoShot = new Vector3((anchorX.x + sisterPos.x) * 0.5f,
                                      sisterGroundY + 1.6f, shotOffset.z);
        yield return PanTo(twoShot, shotOrthoSize, dissolvePullOutDuration);
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

    // Everything that says "this is a fight" comes off the screen.
    private void StageScene()
    {
        // Clearing what is on screen is not enough while her attack loop is still
        // running - it just spawns the next fan a moment later. Stop the source.
        if (boss != null)
        {
            boss.Invulnerable = true;

            BossAttack2D attack = boss.GetComponent<BossAttack2D>();
            if (attack != null)
            {
                attack.StopAllCoroutines();
                attack.SetPhase(0);
                attack.enabled = false;
            }

            BossFlight2D flight = boss.GetComponent<BossFlight2D>();
            if (flight != null) { flight.Stop(); flight.enabled = false; }

            TentacleStrikeField2D field = boss.GetComponent<TentacleStrikeField2D>();
            if (field != null) { field.StopAllCoroutines(); field.enabled = false; }
        }

        ClearBattlefield();

        // her own DieSequence is already playing; hide it so the blackout covers
        // gameplay the instant her health hits zero, not after a death animation
        if (bossRenderer != null) bossRenderer.enabled = false;

        for (int i = 0; i < hideDuringCutscene.Length; i++)
        {
            if (hideDuringCutscene[i] != null) hideDuringCutscene[i].SetActive(false);
        }

        if (flashlightCone != null) flashlightCone.SetActive(false);

        HeldFlashlightVisual2D heldVisual = heldFlashlight != null
            ? heldFlashlight.GetComponent<HeldFlashlightVisual2D>() : null;
        if (heldVisual != null) heldVisual.enabled = false;
        if (heldFlashlight != null) heldFlashlight.SetActive(false);

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        // The cutscene must not inherit anything from the fight: which way he was
        // last facing, the animator's current frame, the flashlight, his physics.
        // Switching each of those off and hoping none was missed is what kept
        // leaking through, so the detective on screen is our own sprite and the
        // played character is simply gone for the duration.
        PlayerSpriteAnimator2D anim = player.GetComponent<PlayerSpriteAnimator2D>();
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();

        detectiveWalk = (anim != null) ? anim.walkFrames : null;
        detectiveIdle = arrivedSprite;
        if (detectiveIdle == null && anim != null && anim.idleFrames != null && anim.idleFrames.Length > 0)
            detectiveIdle = anim.idleFrames[0];
        if (detectiveIdle == null && sr != null) detectiveIdle = sr.sprite;

        GameObject stand = new GameObject("EndingDetective");
        stand.transform.position = player.transform.position;
        stand.transform.localScale = player.transform.lossyScale;
        detectiveRenderer = stand.AddComponent<SpriteRenderer>();
        detectiveRenderer.sprite = detectiveIdle;
        if (sr != null)
        {
            detectiveRenderer.sortingLayerID = sr.sortingLayerID;
            detectiveRenderer.sortingOrder = sr.sortingOrder;
        }
        // he is looking at her from the moment the lights come back
        detectiveRenderer.flipX = lastBossPosition.x < stand.transform.position.x;
        detective = stand.transform;

        player.gameObject.SetActive(false);
    }

    // Anything still flying or crawling would undercut the moment.
    private void ClearBattlefield()
    {
        foreach (TentacleStrike2D t in FindObjectsOfType<TentacleStrike2D>())
        {
            t.StopAllCoroutines();
            Destroy(t.gameObject);
        }
        foreach (BossBullet2D b in FindObjectsOfType<BossBullet2D>()) Destroy(b.gameObject);
        foreach (Bullet2D b in FindObjectsOfType<Bullet2D>()) Destroy(b.gameObject);
        GameObject warn = GameObject.Find("TentacleWaveWarning");
        while (warn != null) { DestroyImmediate(warn); warn = GameObject.Find("TentacleWaveWarning"); }

        // fight messages ("보스가 지쳤다!") have no business here
        foreach (ScreenHint2D hint in FindObjectsOfType<ScreenHint2D>()) hint.Hide();
        GameObject stray = GameObject.Find("ScreenHintCanvas");
        while (stray != null) { DestroyImmediate(stray); stray = GameObject.Find("ScreenHintCanvas"); }
        foreach (Monster2D m in FindObjectsOfType<Monster2D>()) m.gameObject.SetActive(false);
        foreach (RangedMonster2D m in FindObjectsOfType<RangedMonster2D>()) m.gameObject.SetActive(false);
    }

    // Boss2D.DieSequence plays its own dissolve and then destroys the object, so
    // anything we drive through the boss renderer vanishes mid-scene. The ending
    // spawns its own sprite instead and lets the boss tear itself down.
    private void BuildSister()
    {
        if (dissolveFrames == null || dissolveFrames.Length == 0) return;

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

        sisterRenderer = go.AddComponent<SpriteRenderer>();
        sisterRenderer.sprite = dissolveFrames[0];
        sisterRenderer.sortingLayerName = sortingLayer;
        sisterRenderer.sortingOrder = sortingOrder;

        if (useAnchorScale && sisterAnchor != null)
        {
            // the marker in the scene IS the composition - don't recompute it
            go.transform.localScale = sisterAnchor.localScale;
        }
        else
        {
            float native = sisterRenderer.sprite.bounds.size.x;
            if (native > 0.001f)
            {
                float factor = sisterWidth / native;
                go.transform.localScale = new Vector3(factor, factor, 1f);
            }
        }
    }

    // Centre of the drawn figure, not the mark under her feet - a camera aimed at
    // the pivot puts her head out of frame at this distance.
    private Vector3 DissolveShotCentre()
    {
        if (sisterRenderer != null)
        {
            Bounds b = sisterRenderer.bounds;
            return new Vector3(b.center.x, b.center.y, shotOffset.z);
        }
        Vector3 fallback = sister != null ? sister.position : lastBossPosition;
        return new Vector3(fallback.x, fallback.y + 1.6f, shotOffset.z);
    }

    private IEnumerator DissolveFrames()
    {
        if (sisterRenderer == null) yield break;

        // beat on the intact form first - dissolving straight away reads as a
        // glitch rather than as something happening to her
        sisterRenderer.sprite = dissolveFrames[0];
        yield return new WaitForSeconds(dissolveFirstFrameHold);

        for (int i = 1; i < dissolveFrames.Length; i++)
        {
            sisterRenderer.sprite = dissolveFrames[i];
            yield return new WaitForSeconds(dissolveFrameDuration);
        }
    }

    private void SnapTo(Vector3 target, float ortho)
    {
        cam.transform.position = new Vector3(target.x, target.y, cam.transform.position.z);
        cam.orthographicSize = ortho;
    }

    private IEnumerator WalkToHer(Vector3 sisterPos)
    {
        if (detective == null || detectiveRenderer == null) yield break;

        float dir = Mathf.Sign(sisterPos.x - detective.position.x);
        if (Mathf.Approximately(dir, 0f)) dir = 1f;
        detectiveRenderer.flipX = dir < 0f;

        Sprite[] walk = (detectiveWalk != null && detectiveWalk.Length > 0) ? detectiveWalk : null;
        float targetX = sisterPos.x - dir * stopGap;
        float frameTimer = 0f;
        int frame = 0;

        while (Mathf.Abs(targetX - detective.position.x) > 0.05f)
        {
            detective.position = Vector3.MoveTowards(detective.position,
                new Vector3(targetX, detective.position.y, detective.position.z),
                walkSpeed * Time.deltaTime);

            if (walk != null)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= walkFrameDuration)
                {
                    frameTimer -= walkFrameDuration;
                    frame = (frame + 1) % walk.Length;
                    detectiveRenderer.sprite = walk[frame];
                }
            }
            yield return null;
        }

        // walk[0] is still a stride, so he would stand there with one foot out
        if (detectiveIdle != null) detectiveRenderer.sprite = detectiveIdle;
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
