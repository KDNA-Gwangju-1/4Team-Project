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
    [Tooltip("보스 프레임에는 이름칸이 그려져 있지 않아 글자로 얹는다. 꿈탐정 프레임은 이름이 그림에 박혀 있어 비워둔다.")]
    public string bossSpeakerName = "???";
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
    [Tooltip("Airborne pose for the rise out of the arena - standing still while floating upward reads as a bug.")]
    public Sprite playerAscendSprite;
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
    [Tooltip("What she becomes - this replaces her idle loop, not just one frame.")]
    public Sprite[] phase2Frames;
    public float phase2FrameDuration = 0.13f;
    [Tooltip("Left black until the shadowed boss art arrives - reverting to full colour would show the phase 1 girl again.")]
    public Color phase2Tint = Color.black;
    [Tooltip("Width she swells to as she turns.")]
    public float phase2Width = 6.4f;
    public float transformShakeDuration = 1.1f;
    public float transformShake = 0.3f;
    public float flashDuration = 0.22f;
    public float transformHoldTime = 0.9f;

    [Header("Ascent to phase 2")]
    // She does not smash the floor any more - her gravity simply takes hold and
    // lifts them both out of the arena. The scene changes while the screen is
    // dark, so the floating map reads as somewhere else rather than a reshuffle.
    public GameObject phase2Arena;
    [Tooltip("The phase 1 ground, switched off once they have left it.")]
    public GameObject[] hideForPhase2;
    public Transform phase2Anchor;
    public float ascentHeight = 7f;
    public float ascentDuration = 2.4f;
    public float ascentSpin = 0f;
    public float ascentBlackoutIn = 0.7f;
    [Tooltip("How long the new map sits empty before she turns up.")]
    public float arenaRevealHold = 1.3f;
    [Tooltip("She comes back far bigger than she left.")]
    public float bossBurstWidth = 11f;
    [Tooltip("Where she bursts in, measured from the player's landing spot.")]
    public Vector2 bossBurstOffset = new Vector2(9f, 3.5f);
    public float bossBurstDuration = 0.22f;
    [Tooltip("Zakum framing: the camera stops following and holds the whole arena, platforms left, boss right.")]
    public bool lockCameraForPhase2 = true;
    public Vector2 phase2CameraCentre = new Vector2(37f, 3f);
    public float phase2CameraOrtho = 7.4f;
    public float bossBurstShake = 0.5f;
    public float bossBurstHold = 0.8f;

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
        player.CutsceneInvulnerable = true;

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

            yield return Say(line.isBoss ? bossDialogueFrame : playerDialogueFrame, line.text, (line.isBoss && bossDialogueFrame != bossShadowDialogueFrame) ? bossSpeakerName : "");

            if (line.beatAfter == 1)
            {
                yield return ShadowStage1();
            }
            else if (line.beatAfter == 2)
            {
                yield return ShadowStage2();
                yield return TransformToPhase2(bossShot);
            }
            else if (line.beatAfter == 3)
            {
                yield return AscendToPhase2();
            }
        }

        yield return new WaitForSeconds(holdAfterKillLine);

        if (window != null) window.Dispose();
        DestroyBlackout();
        ClearProps();

        RestoreStage();

        // The transformation ends on a hard zoom. Without this the fight would
        // start locked inside it - CameraFollow2D owns position but not zoom.
        yield return PanTo(ShotOn(playerX, playerRenderer), gameplayOrthoSize, pullOutDuration);

        // The arena fits on one screen, so the camera stays put for the whole
        // fight - chasing him up and down the stack would swing the boss in and
        // out of frame.
        if (camFollow != null) camFollow.enabled = !lockCameraForPhase2;
        player.CutsceneInvulnerable = false;
        player.enabled = true;
    }

    // Wipes the fight off the screen and puts the player on their mark. Returns
    // the staged x, which every shot in this scene is composed from.
    private float ClearStage(SpriteRenderer playerRenderer)
    {
        // anything the boss threw goes with the fight - bullets already in the air
        // would otherwise land during the cutscene, with no way to dodge them
        TentacleStrike2D[] live = UnityEngine.Object.FindObjectsOfType<TentacleStrike2D>();
        for (int i = 0; i < live.Length; i++)
        {
            if (live[i] != null) Destroy(live[i].gameObject);
        }

        BossBullet2D[] shots = UnityEngine.Object.FindObjectsOfType<BossBullet2D>();
        for (int i = 0; i < shots.Length; i++)
        {
            if (shots[i] != null) Destroy(shots[i].gameObject);
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

    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(active);
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

    private IEnumerator Say(Sprite frame, string line, string speaker = "")
    {
        if (window == null || !window.Ready) yield break;
        yield return window.Show(frame, line, speaker);
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

    // Stage one: the shadow crawls over her while he is still calling her a monster.
    private IEnumerator ShadowStage1()
    {
        yield return DarkenBoss(bossShadowTint, shadowSweepDuration);
        if (bossShadowSprite != null && bossRenderer != null) bossRenderer.sprite = bossShadowSprite;
        yield return new WaitForSeconds(shadowHoldTime);
    }

    // Stage two: it keeps going until she is a hole in the picture.
    private IEnumerator ShadowStage2()
    {
        yield return DarkenBoss(bossShadowDeepColor, shadowDeepenDuration);
        yield return new WaitForSeconds(shadowHoldTime);
    }

    private IEnumerator DarkenBoss(Color target, float duration)
    {
        if (bossRenderer == null) yield break;

        Color from = bossRenderer.color;
        Vector3 basePos = boss.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            bossRenderer.color = Color.Lerp(from, target, k);
            boss.position = basePos + new Vector3(Random.Range(-1f, 1f) * shadowShake * k,
                                                  Random.Range(-1f, 1f) * shadowShake * k, 0f);
            yield return null;
        }
        boss.position = basePos;
        bossRenderer.color = target;
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

        // Her MonsterSpriteAnimator2D rewrites the sprite every frame, so setting
        // the renderer directly lasts exactly one frame. The loop itself has to change.
        MonsterSpriteAnimator2D animator = boss.GetComponent<MonsterSpriteAnimator2D>();
        if (phase2Frames != null && phase2Frames.Length > 0)
        {
            if (animator != null)
            {
                animator.activeFrameDuration = phase2FrameDuration;
                animator.SetLoopFrames(phase2Frames);
            }
            else if (bossRenderer != null)
            {
                bossRenderer.sprite = phase2Frames[0];
            }
        }
        if (bossRenderer != null) bossRenderer.color = phase2Tint;

        // what she is now speaks with a different face
        if (bossShadowDialogueFrame != null) bossDialogueFrame = bossShadowDialogueFrame;

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

    // Both of them come off the floor, the screen goes dark, and what fades back
    // in is the floating map with nobody else on it.
    private IEnumerator AscendToPhase2()
    {
        // he is being lifted, not standing
        if (playerAscendSprite != null && stageRenderer != null) stageRenderer.sprite = playerAscendSprite;

        Vector3 playerFrom = player.transform.position;
        Vector3 bossFrom = boss.position;
        Vector3 camFrom = cam.transform.position;

        float t = 0f;
        while (t < ascentDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ascentDuration));
            float rise = ascentHeight * k;

            Vector3 p = playerFrom + new Vector3(0f, rise, 0f);
            player.transform.position = p;
            if (playerBody != null) playerBody.position = p;

            boss.position = bossFrom + new Vector3(0f, rise * 1.15f, 0f);
            if (ascentSpin != 0f && stageRenderer != null)
            {
                player.transform.rotation = Quaternion.Euler(0f, 0f, ascentSpin * k);
            }

            cam.transform.position = camFrom + new Vector3(0f, rise * 0.9f, 0f);
            yield return null;
        }

        yield return FadeBlackout(0f, 1f, ascentBlackoutIn);

        // --- swap the world while nobody can see ---
        player.transform.rotation = Quaternion.identity;
        if (phase2Arena != null) phase2Arena.SetActive(true);
        SetActiveAll(hideForPhase2, false);

        Vector3 landing = phase2Anchor != null
            ? phase2Anchor.position + Vector3.up * 1.2f
            : new Vector3(playerStageX, playerStageY + 1.2f, 0f);
        player.transform.position = landing;
        if (playerBody != null)
        {
            playerBody.position = landing;
            playerBody.linearVelocity = Vector2.zero;
        }

        // she is simply not there when the lights come up
        if (bossRenderer != null) bossRenderer.enabled = false;

        if (lockCameraForPhase2)
        {
            cam.transform.position = new Vector3(phase2CameraCentre.x, phase2CameraCentre.y, shotOffset.z);
            cam.orthographicSize = phase2CameraOrtho;
        }
        else
        {
            cam.transform.position = new Vector3(landing.x, landing.y + 1.5f, shotOffset.z);
            cam.orthographicSize = gameplayOrthoSize;
        }

        yield return FadeBlackout(1f, 0f, blackoutOutDuration);
        yield return new WaitForSeconds(arenaRevealHold);

        // --- and then she is ---
        Vector3 burst = new Vector3(landing.x + bossBurstOffset.x, landing.y + bossBurstOffset.y, 0f);
        boss.position = burst;

        if (bossRenderer != null)
        {
            float drawn = bossRenderer.sprite != null ? bossRenderer.sprite.bounds.size.x : 1f;
            if (drawn > 0.001f)
            {
                float factor = bossBurstWidth / drawn;
                boss.localScale = new Vector3(factor, factor, boss.localScale.z);
            }
            bossRenderer.enabled = true;
        }

        Vector3 full = boss.localScale;
        float b = 0f;
        while (b < bossBurstDuration)
        {
            b += Time.deltaTime;
            float k = Mathf.Clamp01(b / bossBurstDuration);
            boss.localScale = Vector3.Lerp(full * 1.35f, full, k);
            yield return null;
        }
        boss.localScale = full;

        if (camFollow != null) camFollow.Shake(0.6f, bossBurstShake);
        yield return new WaitForSeconds(bossBurstHold);
    }

    private IEnumerator ZoomForKillLine(Vector3 bossShot)
    {
        // creep in first so the snap has something to land against
        yield return PanTo(bossShot, creepOrthoSize, creepDuration);
        yield return PanTo(bossShot, snapOrthoSize, snapDuration);

        if (camFollow != null) camFollow.Shake(0.35f, snapShake);
        yield return new WaitForSeconds(holdBeforeKillLine);
    }

    // 2페이즈에서 죽어 리트라이한 판. Play()가 암전 뒤에 하는 세계 교체와 보스 등장의
    // 끝 상태만 그대로 놓는다. 대사·상승·암전·burst 연출은 전부 생략한다.
    public void ApplyArenaInstantly()
    {
        if (player == null || cam == null || boss == null) return;

        if (phase2Arena != null) phase2Arena.SetActive(true);
        SetActiveAll(hideForPhase2, false);

        Vector3 landing = phase2Anchor != null
            ? phase2Anchor.position + Vector3.up * 1.2f
            : new Vector3(playerStageX, playerStageY + 1.2f, 0f);
        player.transform.position = landing;
        player.transform.rotation = Quaternion.identity;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = landing;
            body.linearVelocity = Vector2.zero;
        }

        Sprite phase2Look = (phase2Frames != null && phase2Frames.Length > 0) ? phase2Frames[0] : null;
        MonsterSpriteAnimator2D animator = boss.GetComponent<MonsterSpriteAnimator2D>();
        if (phase2Look != null)
        {
            if (animator != null)
            {
                animator.activeFrameDuration = phase2FrameDuration;
                animator.SetLoopFrames(phase2Frames);
            }
            else if (bossRenderer != null)
            {
                bossRenderer.sprite = phase2Look;
            }
        }

        boss.position = new Vector3(landing.x + bossBurstOffset.x, landing.y + bossBurstOffset.y, 0f);
        if (bossRenderer != null)
        {
            // 애니메이터는 다음 프레임에야 스프라이트를 바꾸므로 렌더러가 아니라 프레임에서 폭을 잰다
            Sprite measure = phase2Look != null ? phase2Look : bossRenderer.sprite;
            float drawn = measure != null ? measure.bounds.size.x : 1f;
            if (drawn > 0.001f)
            {
                float factor = bossBurstWidth / drawn;
                boss.localScale = new Vector3(factor, factor, boss.localScale.z);
            }
            bossRenderer.color = phase2Tint;
            bossRenderer.enabled = true;
        }

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (lockCameraForPhase2)
        {
            cam.transform.position = new Vector3(phase2CameraCentre.x, phase2CameraCentre.y, shotOffset.z);
            cam.orthographicSize = phase2CameraOrtho;
        }
        if (follow != null) follow.enabled = !lockCameraForPhase2;
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
