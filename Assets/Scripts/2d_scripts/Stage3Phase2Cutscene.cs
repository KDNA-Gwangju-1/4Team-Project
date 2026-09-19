using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The phase 1 -> phase 2 hand-off. Driven by BossPhaseController2D, which holds
// its collapse until Play() returns.
//
// She walked away from the player in the first cutscene; here she walks back.
// That is the whole point of the staging - the safe distance she fought from is
// gone, and the scene ends on her deciding to kill him herself.
public class Stage3Phase2Cutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Transform boss;
    public SpriteRenderer bossRenderer;
    public Font captionFont;
    public Transform flashlight;

    [Header("Dialogue")]
    public Sprite bossDialogueFrame;
    public Sprite playerDialogueFrame;
    [Tooltip("The shadowed portrait. Shown from the moment the shadow takes her.")]
    public Sprite bossShadowDialogueFrame;
    // beatAfter: 1 = the shadow takes her, then the creep-and-snap zoom.
    // Every line after that beat wears the shadowed portrait.
    public DialogueLine2D[] lines;

    [Header("Dialogue window")]
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.88f;
    public float dialogueFrameBottomMargin = 24f;
    public float dialogueFrameXOffset = -120f;
    public float lineMinDuration = 0.5f;
    public float lineAutoAdvance = 6f;
    public float lineGap = 0.3f;

    [Header("Staging")]
    // The fight is wherever it happened to be when her health broke. None of that
    // composes, so the arena is cleared and re-set on marks, the same way the
    // intro stages itself - the scene has to read as its own space, not as a
    // pause in the middle of gameplay.
    [Tooltip("Switched off for the scene and back on at the end - monsters, HUD.")]
    public GameObject[] hideDuringCutscene;
    [Tooltip("Where the player is placed for the scene. Their live fight position is discarded.")]
    public float playerStageX = 18.06f;
    [Tooltip("Ground line the player is stood on.")]
    public float playerStageY = -2.63f;
    public float stageSettleTime = 0.35f;
    [Tooltip("The light cone. Dark for the staged shots, lit only while he burns the tentacle.")]
    public GameObject flashlightCone;
    // The walk-with-flashlight frames have his legs apart mid-stride, which reads
    // as "paused mid-run" rather than as a man standing his ground. The scene uses
    // an idle pose for the confrontation and only swaps to the arm-out frame for
    // the shot itself.
    [Tooltip("Standing pose for the confrontation. Left empty, his first idle frame is used.")]
    public Sprite playerStageSprite;
    [Tooltip("Arm-out pose, used only while he lights and shoots the tentacle.")]
    public Sprite playerAimSprite;
    [Tooltip("The flashlight he carries. Hidden for the idle pose, shown for the shot.")]
    public GameObject heldFlashlight;

    [Header("Opening blackout")]
    // Cutting straight from the fight makes this read as a pause in gameplay. The
    // screen goes black first, so what comes back up is understood as its own scene.
    public float blackoutInDuration = 0.35f;
    public float blackoutHoldDuration = 0.5f;
    public float blackoutOutDuration = 0.6f;
    [Tooltip("Extra blink while it is dark. 0 = a clean fade.")]
    public int blackoutBlinks = 2;
    public float blackoutBlinkTime = 0.09f;

    [Header("Camera")]
    public float panDuration = 0.9f;
    public Vector3 shotOffset = new Vector3(0f, 0.6f, -10f);
    public float talkOrthoSize = 5.2f;

    [Header("Tentacle death beat")]
    public Sprite[] tentacleFrames;
    public Vector2 tentacleSize = new Vector2(6f, 11.25f);
    [Tooltip("Sinks the drawn base into the ledge so it erupts from the floor.")]
    public float tentacleSink = 0.4f;
    [Tooltip("How far in front of the player it comes up.")]
    public float tentacleGapFromPlayer = 6.5f;
    public float tentacleGroundY = -2.63f;
    public float tentacleFrameDuration = 0.07f;
    public float lightHoldTime = 0.8f;

    [Header("The shot")]
    public Sprite bulletSprite;
    public float bulletSize = 1f;
    public float bulletSpeed = 26f;

    [Header("Tentacle hurt")]
    public Color hurtFlashColor = new Color(1f, 0.25f, 0.25f);
    public int hurtFlashes = 3;
    public float hurtDuration = 0.5f;
    public float hurtShake = 0.24f;

    [Header("Boss return")]
    [Tooltip("Where she lands, measured from the player. This is a confrontation, not a stand-off.")]
    public float bossReturnGap = 7f;
    public float bossReturnWidth = 4.8f;
    public int bossForegroundSortingOrder = 2;
    public float bossGroundY = -2.63f;
    public float bossRiseHeight = 3.2f;
    public float bossRiseDuration = 0.5f;
    public float bossTravelDuration = 1.25f;
    public float bossSettleDuration = 0.35f;
    public float bossHoverAmplitude = 0.18f;

    [Header("Shadow turn")]
    [Tooltip("Optional in-scene sprite for the shadowed form. Falls back to tinting her down.")]
    public Sprite bossShadowSprite;
    public Color bossShadowTint = new Color(0.10f, 0.06f, 0.14f, 1f);
    [Tooltip("Second pass: it keeps going until nothing of her is left.")]
    public Color bossShadowDeepColor = Color.black;
    public float shadowSweepDuration = 1.2f;
    public float shadowDeepenDuration = 1.4f;
    public float shadowHoldTime = 0.5f;
    public float shadowShake = 0.12f;

    [Header("Phase 2 transformation")]
    [Tooltip("What she becomes. Left empty, the phase controller's own phase2Frames[0] is used.")]
    public Sprite phase2Sprite;
    public Color phase2Tint = Color.white;
    [Tooltip("Width she swells to as she turns.")]
    public float phase2Width = 6.4f;
    public float transformShakeDuration = 1.1f;
    public float transformShake = 0.3f;
    public float flashDuration = 0.22f;
    public float transformHoldTime = 0.9f;

    [Header("Kill line")]
    [Tooltip("The slow creep in - dread, not action.")]
    public float creepOrthoSize = 4.2f;
    public float creepDuration = 2.4f;
    [Tooltip("Then the snap. Short and hard.")]
    public float snapOrthoSize = 2.0f;
    public float snapDuration = 0.1f;
    public float snapShake = 0.35f;
    public float holdBeforeKillLine = 0.3f;
    public float holdAfterKillLine = 0.5f;
    [Tooltip("Rush back out to the gameplay framing before the floor drops.")]
    public float pullOutDuration = 0.45f;

    private CameraFollow2D camFollow;
    private float gameplayOrthoSize;
    private PlayerSpriteAnimator2D playerAnimator;
    private Rigidbody2D playerBody;
    private RigidbodyType2D originalBodyType;
    private SpriteRenderer stageRenderer;
    private GameObject blackoutCanvas;
    private UnityEngine.UI.Image blackout;
    private HeldFlashlightVisual2D heldVisual;
    private DialogueWindow2D window;
    private Parallax2D bossParallax;
    private FloatBob2D bossBob;
    private readonly List<GameObject> props = new List<GameObject>();

    public IEnumerator Play()
    {
        if (player == null || boss == null) yield break;
        if (cam == null) cam = Camera.main;
        if (cam == null) yield break;

        camFollow = cam.GetComponent<CameraFollow2D>();
        if (camFollow != null) camFollow.enabled = false;
        player.enabled = false;

        // CameraFollow2D owns position but not zoom, so the gameplay framing has
        // to be put back by hand or phase 2 starts locked inside the snap zoom
        gameplayOrthoSize = cam.orthographicSize;

        BuildWindow();
        BuildBlackout();

        // black out BEFORE the stage is rearranged, so the player never sees
        // themselves teleport onto their mark
        yield return FadeBlackout(0f, 1f, blackoutInDuration);
        yield return Blink();

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        float playerX = ClearStage(playerRenderer);

        // frame the opening shot while it is still dark
        yield return PanTo(ShotOn(playerX + tentacleGapFromPlayer * 0.5f, null), 6.4f, 0f);
        yield return new WaitForSeconds(blackoutHoldDuration);
        yield return FadeBlackout(1f, 0f, blackoutOutDuration);

        yield return new WaitForSeconds(stageSettleTime);

        yield return TentacleDeathBeat(playerX);
        yield return BossReturn(playerX);

        Vector3 bossShot = ShotOn(boss.position.x, bossRenderer);
        Vector3 playerShot = ShotOn(playerX, playerRenderer);
        Vector3 twoShot = new Vector3((bossShot.x + playerShot.x) * 0.5f,
                                      (bossShot.y + playerShot.y) * 0.5f, shotOffset.z);

        yield return PanTo(twoShot, talkOrthoSize, panDuration);

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine2D line = lines[i];
            if (line == null || string.IsNullOrEmpty(line.text)) continue;

            yield return Say(line.isBoss ? bossDialogueFrame : playerDialogueFrame, line.text);

            if (line.beatAfter == 1)
            {
                yield return ShadowTurn();
            }
            else if (line.beatAfter == 2)
            {
                yield return TransformToPhase2(bossShot);
            }
        }

        yield return new WaitForSeconds(holdAfterKillLine);

        if (window != null) window.Dispose();
        DestroyBlackout();
        ClearProps();

        RestoreStage();

        // rush back out to the gameplay framing - it doubles as the beat where
        // the floor is about to give way
        yield return PanTo(ShotOn(playerX, playerRenderer), gameplayOrthoSize, pullOutDuration);

        // control and the camera go back to BossPhaseController2D, which
        // drops the floor out from under him next
        if (camFollow != null) camFollow.enabled = true;
        player.enabled = true;
    }

    // Wipes the fight off the screen and puts the player on their mark. Returns
    // the staged x, which every shot in this scene is composed from.
    private float ClearStage(SpriteRenderer playerRenderer)
    {
        // anything the boss threw goes with the fight
        TentacleStrike2D[] live = UnityEngine.Object.FindObjectsOfType<TentacleStrike2D>();
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] != null) Destroy(live[i].gameObject);
        }

        for (int i = 0; i < hideDuringCutscene.Length; i++)
        {
            if (hideDuringCutscene[i] != null) hideDuringCutscene[i].SetActive(false);
        }

        // feet on the mark - the pivot is not the bottom of the drawing
        float footOffset = playerRenderer != null
            ? player.transform.position.y - playerRenderer.bounds.min.y : 0f;
        Vector3 mark = new Vector3(playerStageX, playerStageY + footOffset, player.transform.position.z);

        playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = mark;
            // nothing nudges him off his mark for the rest of the scene
            originalBodyType = playerBody.bodyType;
            playerBody.bodyType = RigidbodyType2D.Kinematic;
        }
        player.transform.position = mark;

        // The gameplay animator reads velocity and light state, so left running it
        // freezes him mid-stride with the beam still up. A staged scene needs a
        // chosen pose, not whatever frame the fight happened to end on.
        playerAnimator = player.GetComponent<PlayerSpriteAnimator2D>();
        if (playerAnimator != null) playerAnimator.enabled = false;
        stageRenderer = playerRenderer;

        // it decides its own visibility from live player state, which is frozen
        // for the whole scene - the cutscene places it by hand instead
        if (heldFlashlight != null)
        {
            heldVisual = heldFlashlight.GetComponent<HeldFlashlightVisual2D>();
            if (heldVisual != null) heldVisual.enabled = false;
        }

        SetPlayerPose(false);
        if (flashlightCone != null) flashlightCone.SetActive(false);

        // looking at her, not at wherever he was running
        if (playerRenderer != null) playerRenderer.flipX = false;

        return playerStageX;
    }

    private void SetPlayerPose(bool aiming)
    {
        if (stageRenderer == null) return;

        Sprite pose = aiming ? playerAimSprite : playerStageSprite;
        if (pose == null && playerAnimator != null)
        {
            Sprite[] frames = aiming ? playerAnimator.walkFlashlightFrames : playerAnimator.idleFrames;
            if (frames != null && frames.Length > 0) pose = frames[0];
        }
        if (pose != null) stageRenderer.sprite = pose;

        // he only raises the flashlight for the shot; the rest of the scene he
        // just stands there
        if (heldFlashlight != null)
        {
            heldFlashlight.SetActive(aiming);
            SpriteRenderer heldRenderer = heldFlashlight.GetComponent<SpriteRenderer>();
            if (heldRenderer != null) heldRenderer.enabled = aiming;
        }
    }

    private void RestoreStage()
    {
        for (int i = 0; i < hideDuringCutscene.Length; i++)
        {
            if (hideDuringCutscene[i] != null) hideDuringCutscene[i].SetActive(true);
        }

        if (playerAnimator != null) playerAnimator.enabled = true;
        if (heldFlashlight != null) heldFlashlight.SetActive(true);
        if (heldVisual != null) heldVisual.enabled = true;
        if (playerBody != null) playerBody.bodyType = originalBodyType;
        if (flashlightCone != null) flashlightCone.SetActive(true);
    }

    private void BuildWindow()
    {
        Sprite reference = bossDialogueFrame != null ? bossDialogueFrame : playerDialogueFrame;
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

    private void BuildBlackout()
    {
        blackoutCanvas = new GameObject("CutsceneBlackout");
        Canvas canvas = blackoutCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;   // over the dialogue window too

        UnityEngine.UI.Image img = blackoutCanvas.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        blackout = img;
    }

    private IEnumerator FadeBlackout(float from, float to, float duration)
    {
        if (blackout == null) yield break;
        if (duration <= 0f)
        {
            SetBlackout(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetBlackout(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetBlackout(to);
    }

    private IEnumerator Blink()
    {
        for (int i = 0; i < blackoutBlinks; i++)
        {
            SetBlackout(0.55f);
            yield return new WaitForSeconds(blackoutBlinkTime);
            SetBlackout(1f);
            yield return new WaitForSeconds(blackoutBlinkTime);
        }
    }

    private void SetBlackout(float alpha)
    {
        if (blackout == null) return;
        Color c = blackout.color;
        c.a = alpha;
        blackout.color = c;
    }

    private void DestroyBlackout()
    {
        if (blackoutCanvas != null) Destroy(blackoutCanvas);
        blackoutCanvas = null;
        blackout = null;
    }

    private IEnumerator Say(Sprite frame, string line)
    {
        if (window == null || !window.Ready) yield break;
        yield return window.Show(frame, line);
    }

    // ---------- the tentacle he kills ----------

    private IEnumerator TentacleDeathBeat(float playerX)
    {
        if (tentacleFrames == null || tentacleFrames.Length == 0) yield break;

        float x = playerX + tentacleGapFromPlayer;

        SpriteRenderer sr = NewProp("CutsceneTentacle", x, tentacleGroundY - tentacleSink, 5);
        sr.sprite = tentacleFrames[0];

        float nw = sr.sprite.bounds.size.x;
        float nh = sr.sprite.bounds.size.y;
        if (nw > 0.001f && nh > 0.001f)
        {
            sr.transform.localScale = new Vector3(tentacleSize.x / nw, tentacleSize.y / nh, 1f);
        }

        int risen = Mathf.Min(2, tentacleFrames.Length - 1);
        for (int i = 0; i <= risen; i++)
        {
            sr.sprite = tentacleFrames[i];
            yield return new WaitForSeconds(tentacleFrameDuration);
        }

        // he puts the beam on it first - that is the whole verb of this fight
        SetPlayerPose(true);
        if (flashlightCone != null) flashlightCone.SetActive(true);
        AimFlashlightAt(sr.bounds.center);
        yield return new WaitForSeconds(lightHoldTime);

        yield return FireShot(sr.bounds.center);
        yield return HurtAndDie(sr, risen);

        // beam down, arm down - he squares up for what comes next
        if (flashlightCone != null) flashlightCone.SetActive(false);
        SetPlayerPose(false);
    }

    private void AimFlashlightAt(Vector3 target)
    {
        if (flashlight == null) return;

        Vector3 dir = target - flashlight.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        flashlight.rotation = Quaternion.Euler(0f, 0f, angle);

        // the cone renderer keeps whatever alpha gameplay last left on it
        SpriteRenderer coneRenderer = flashlight.GetComponent<SpriteRenderer>();
        if (coneRenderer != null)
        {
            Color c = coneRenderer.color;
            c.a = 1f;
            coneRenderer.color = c;
            coneRenderer.enabled = true;
        }

        if (heldFlashlight != null && heldFlashlight.activeSelf)
        {
            heldFlashlight.transform.position = flashlight.position;
            heldFlashlight.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private IEnumerator FireShot(Vector3 target)
    {
        if (bulletSprite == null) yield break;

        Vector3 from = flashlight != null ? flashlight.position : player.transform.position;
        SpriteRenderer sr = NewProp("CutsceneShot", from.x, from.y, 21);
        sr.sprite = bulletSprite;

        float nw = sr.sprite.bounds.size.x;
        if (nw > 0.001f) sr.transform.localScale = Vector3.one * (bulletSize / nw);

        float travel = Vector3.Distance(from, target);
        float duration = travel / Mathf.Max(1f, bulletSpeed);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            sr.transform.position = Vector3.Lerp(from, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    // Writhes and flashes red, the same reaction the gameplay tentacle gives,
    // so the cutscene reads as something the player just did rather than a clip.
    private IEnumerator HurtAndDie(SpriteRenderer sr, int risenFrame)
    {
        Vector3 basePos = sr.transform.position;
        int lastFrame = tentacleFrames.Length - 1;
        int frame = risenFrame;
        float writheStep = hurtDuration / Mathf.Max(1, lastFrame - risenFrame + 1);
        float frameTimer = 0f;
        float elapsed = 0f;

        while (elapsed < hurtDuration)
        {
            float k = elapsed / hurtDuration;
            float decay = 1f - k;

            sr.transform.position = basePos + new Vector3(
                Random.Range(-1f, 1f) * hurtShake * decay,
                Random.Range(-0.5f, 0.5f) * hurtShake * decay, 0f);

            float blink = Mathf.Repeat(k * hurtFlashes * 2f, 2f);
            sr.color = blink < 1f ? hurtFlashColor : Color.white;

            frameTimer += Time.deltaTime;
            if (frameTimer >= writheStep)
            {
                frameTimer -= writheStep;
                frame = (frame >= lastFrame) ? risenFrame : frame + 1;
                sr.sprite = tentacleFrames[frame];
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.transform.position = basePos;
        sr.color = Color.white;

        // sinks back into the floor on the emergence frames, reversed
        for (int i = risenFrame; i >= 0; i--)
        {
            sr.sprite = tentacleFrames[i];
            yield return new WaitForSeconds(tentacleFrameDuration);
        }
        Destroy(sr.gameObject);
    }

    // ---------- she comes back ----------

    private IEnumerator BossReturn(float playerX)
    {
        bossParallax = boss.GetComponent<Parallax2D>();
        if (bossParallax != null) bossParallax.enabled = false;

        bossBob = boss.GetComponent<FloatBob2D>();
        if (bossBob != null) bossBob.enabled = false;

        Vector3 from = boss.position;
        Vector3 ledgeScale = boss.localScale;

        Vector3 foregroundScale = ledgeScale;
        if (bossRenderer != null)
        {
            float drawnWidth = bossRenderer.bounds.size.x;
            if (drawnWidth > 0.001f)
            {
                float factor = bossReturnWidth / drawnWidth;
                foregroundScale = new Vector3(ledgeScale.x * factor, ledgeScale.y * factor, ledgeScale.z);
            }
        }

        float landX = playerX + bossReturnGap;
        Vector3 apex = new Vector3(from.x, from.y + bossRiseHeight, from.z);
        Vector3 approach = new Vector3(landX, bossGroundY + bossRiseHeight, from.z);
        Vector3 land = new Vector3(landX, bossGroundY, from.z);

        yield return MoveBoss(from, apex, ledgeScale, ledgeScale, bossRiseDuration, false);

        // she crosses into his plane the moment she leaves the ledge behind
        if (bossRenderer != null) bossRenderer.sortingOrder = bossForegroundSortingOrder;

        yield return MoveBoss(apex, approach, ledgeScale, foregroundScale, bossTravelDuration, true);
        yield return MoveBoss(approach, land, foregroundScale, foregroundScale, bossSettleDuration, false);

        boss.position = land;
        boss.localScale = foregroundScale;
        if (bossRenderer != null) bossRenderer.flipX = true;
    }

    private IEnumerator MoveBoss(Vector3 from, Vector3 to, Vector3 scaleFrom, Vector3 scaleTo, float duration, bool hover)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            Vector3 pos = Vector3.Lerp(from, to, k);
            if (hover) pos.y += Mathf.Sin(t * 6f) * bossHoverAmplitude;
            boss.position = pos;
            boss.localScale = Vector3.Lerp(scaleFrom, scaleTo, k);
            yield return null;
        }
        boss.position = to;
        boss.localScale = scaleTo;
    }

    // ---------- the shadow takes her ----------

    private IEnumerator ShadowTurn()
    {
        if (bossRenderer == null) yield break;

        Color from = bossRenderer.color;
        Vector3 basePos = boss.position;
        float t = 0f;

        while (t < shadowSweepDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / shadowSweepDuration);
            bossRenderer.color = Color.Lerp(from, bossShadowTint, k);
            boss.position = basePos + new Vector3(Random.Range(-1f, 1f) * shadowShake * k,
                                                  Random.Range(-1f, 1f) * shadowShake * k, 0f);
            yield return null;
        }

        bossRenderer.color = bossShadowTint;
        if (bossShadowSprite != null) bossRenderer.sprite = bossShadowSprite;

        // and it keeps going, until she is a hole in the picture
        t = 0f;
        while (t < shadowDeepenDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / shadowDeepenDuration);
            bossRenderer.color = Color.Lerp(bossShadowTint, bossShadowDeepColor, k);
            boss.position = basePos + new Vector3(Random.Range(-1f, 1f) * shadowShake * k,
                                                  Random.Range(-1f, 1f) * shadowShake * k, 0f);
            yield return null;
        }

        boss.position = basePos;
        bossRenderer.color = bossShadowDeepColor;

        // every line from here on wears the shadowed portrait
        if (bossShadowDialogueFrame != null) bossDialogueFrame = bossShadowDialogueFrame;

        yield return new WaitForSeconds(shadowHoldTime);
    }

    // The silhouette swells, the screen goes white, and what comes back is the
    // phase 2 boss. The controller's claw swing and floor collapse follow.
    private IEnumerator TransformToPhase2(Vector3 bossShot)
    {
        yield return PanTo(bossShot, creepOrthoSize, creepDuration);

        Vector3 basePos = boss.position;
        Vector3 fromScale = boss.localScale;
        Vector3 toScale = fromScale;
        if (bossRenderer != null && bossRenderer.bounds.size.x > 0.001f)
        {
            float factor = phase2Width / bossRenderer.bounds.size.x;
            toScale = new Vector3(fromScale.x * factor, fromScale.y * factor, fromScale.z);
        }

        float t = 0f;
        while (t < transformShakeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / transformShakeDuration);
            boss.localScale = Vector3.Lerp(fromScale, toScale, k);
            boss.position = basePos + new Vector3(Random.Range(-1f, 1f) * transformShake * k,
                                                  Random.Range(-1f, 1f) * transformShake * k, 0f);
            yield return null;
        }
        boss.position = basePos;
        boss.localScale = toScale;

        yield return PanTo(bossShot, snapOrthoSize, snapDuration);
        if (camFollow != null) camFollow.Shake(0.5f, snapShake);

        // white out, swap, come back
        yield return FlashWhite();

        Sprite target = phase2Sprite;
        if (target == null)
        {
            BossPhaseController2D controller = boss.GetComponent<BossPhaseController2D>();
            if (controller != null && controller.phase2Frames != null && controller.phase2Frames.Length > 0)
            {
                target = controller.phase2Frames[0];
            }
        }
        if (target != null && bossRenderer != null) bossRenderer.sprite = target;
        if (bossRenderer != null) bossRenderer.color = phase2Tint;

        yield return PanTo(bossShot, creepOrthoSize, 0.3f);
        yield return new WaitForSeconds(transformHoldTime);
    }

    private IEnumerator FlashWhite()
    {
        if (blackout == null) yield break;

        blackout.color = new Color(1f, 1f, 1f, 0f);
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            blackout.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / flashDuration));
            yield return null;
        }
        blackout.color = Color.white;
        yield return new WaitForSeconds(0.08f);

        t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            blackout.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(t / flashDuration));
            yield return null;
        }
        blackout.color = new Color(0f, 0f, 0f, 0f);
    }

    // ---------- "...죽어" ----------

    private IEnumerator ZoomForKillLine(Vector3 bossShot)
    {
        // creep in first so the snap has something to land against
        yield return PanTo(bossShot, creepOrthoSize, creepDuration);
        yield return PanTo(bossShot, snapOrthoSize, snapDuration);

        if (camFollow != null) camFollow.Shake(0.35f, snapShake);
        yield return new WaitForSeconds(holdBeforeKillLine);
    }

    // ---------- helpers ----------

    private Vector3 ShotOn(float x, SpriteRenderer subject)
    {
        float y = subject != null ? subject.bounds.center.y : bossGroundY;
        return new Vector3(x + shotOffset.x, y + shotOffset.y, shotOffset.z);
    }

    private SpriteRenderer NewProp(string label, float x, float y, int sortingOrder)
    {
        GameObject go = new GameObject(label);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = bossRenderer != null ? bossRenderer.sortingLayerName : "Default";
        sr.sortingOrder = sortingOrder;
        props.Add(go);
        return sr;
    }

    private void ClearProps()
    {
        for (int i = 0; i < props.Count; i++)
        {
            if (props[i] != null) Destroy(props[i]);
        }
        props.Clear();
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
