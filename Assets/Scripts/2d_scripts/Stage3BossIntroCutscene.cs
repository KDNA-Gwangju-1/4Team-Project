using System.Collections;
using System.Collections.Generic;
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

    // beatAfter: 1 = the first monster steps out, 2 = the tentacles and the rest
    public DialogueLine2D[] lines;
    public string bossSpeakerName = "???";
    public string playerSpeakerName = "꿈탐정";
    public float lineDisplayDuration = 2f;
    public float lineGap = 0.3f;

    [Header("Dialogue window")]
    // Full-screen overlays with the portrait and the name plate already drawn in,
    // so the only thing this script places is the line itself.
    public Sprite bossDialogueFrame;
    public Sprite playerDialogueFrame;
    [Tooltip("Where the line sits inside the frame, in normalized frame coordinates.")]
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    public Color dialogueTextColor = Color.white;
    public float dialogueFadeDuration = 0.18f;
    [Tooltip("Panel width as a fraction of the screen. Sized like a Maple chat box rather than a full-screen takeover.")]
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.7f;
    [Tooltip("Gap from the bottom of the screen, in 1080p reference pixels.")]
    public float dialogueFrameBottomMargin = 30f;
    [Tooltip("Shift from screen centre, in 1080p reference pixels. Negative moves the panel left.")]
    public float dialogueFrameXOffset = -120f;
    [Tooltip("A line cannot be skipped before this, so a held key never eats one.")]
    public float lineMinDuration = 0.5f;

    [Header("Staging")]
    [Tooltip("Hidden until the camera turns on her - this is her first appearance.")]
    public GameObject[] revealWithBoss;
    [Tooltip("Switched on the moment control returns - HUD.")]
    public GameObject[] revealAfterCutscene;
    [Tooltip("Switched on after attackStartDelay - monsters. Gives the player a beat to breathe.")]
    public GameObject[] revealAfterDelay;
    [Tooltip("How long the arena stays quiet after control returns.")]
    public float attackStartDelay = 1f;

    [Header("Boss staging")]
    // She opens the scene standing on the player's own ledge, so the threat is
    // physical, then withdraws into the background space she fights from. The
    // retreat IS the explanation for why she cannot be reached in phase 1.
    [Tooltip("Gap between the player's stop position and the boss during the talk.")]
    public float bossCutsceneGap = 19.2f;
    [Tooltip("Drawn width while she shares the player's ledge. She shrinks back on the way out.")]
    public float bossCutsceneWidth = 4.8f;
    [Tooltip("Sorting order while she is on the player's plane.")]
    public int bossCutsceneSortingOrder = 2;
    [Tooltip("Ground line of the player's ledge.")]
    public float bossCutsceneGroundY = -2.63f;

    [Header("Summon beat")]
    // Everything she calls up is placed BESIDE her, never in front, and spaced so
    // no two silhouettes touch. Offsets are from the boss, in world units.
    [Tooltip("Which entry of revealAfterDelay steps out first, on its own.")]
    public int firstSummonIndex = 1;
    [Tooltip("A closer shot for that first one - it is a reveal, not a line-up yet.")]
    public float firstSummonOrthoSize = 6.5f;
    public Vector2 firstSummonShotOffset = new Vector2(3.5f, 3.2f);
    [Tooltip("Camera pulls back this far to hold the whole line-up.")]
    public float summonShotOrthoSize = 9f;
    [Tooltip("Summon shot centre: x from the boss, y from the ledge floor.")]
    public Vector2 summonShotOffset = new Vector2(0.2f, 6.6f);
    [Tooltip("One tentacle either side of her.")]
    public float[] summonTentacleOffsets = new float[] { -7.3f, 7.3f };
    [Tooltip("Bigger than the gameplay tentacle - this beat is the spectacle.")]
    public Vector2 summonTentacleSize = new Vector2(7.5f, 14f);
    [Tooltip("Sinks the drawn base into the ledge. The art has empty margin below it, so without this it erupts off the lip instead of out of the floor.")]
    public float summonTentacleSink = 0.5f;
    [Tooltip("Slow writhe once it is up - a frozen tentacle reads as a prop.")]
    public float summonTentacleSwayAngle = 4f;
    public float summonTentacleSwayPeriod = 2.4f;
    public float summonTentacleIdleFrameInterval = 0.45f;
    [Tooltip("Outside the tentacles: one of each monster, matched to revealAfterDelay.")]
    public float[] summonMonsterOffsets = new float[] { -13.8f, 14.2f };
    [Tooltip("Height above the ledge floor - the flyer sits up in the air.")]
    public float[] summonMonsterHeights = new float[] { 0f, 3.8f };
    public float summonMonsterWidth = 2.4f;
    [Tooltip("Summoned monsters play their own idle loop - a frozen one reads as a cardboard cut-out.")]
    public float summonMonsterFrameInterval = 0.13f;
    public float summonMonsterBobAmplitude = 0.16f;
    public float summonMonsterBobPeriod = 1.5f;
    public float summonStagger = 0.28f;
    public float summonPopDuration = 0.3f;
    public float summonHoldTime = 0.7f;

    [Header("Boss exit")]
    public float bossExitRiseHeight = 3.2f;
    public float bossExitRiseDuration = 0.55f;
    public float bossExitTravelDuration = 1.15f;
    public float bossExitSettleDuration = 0.3f;
    public float bossExitHoverAmplitude = 0.18f;

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
    private GameObject captionCanvas;
    private DialogueWindow2D window;

    // the ledge pose is whatever the scene was authored with - captured, not hardcoded
    private Vector3 ledgePosition;
    private Vector3 ledgeScale;
    private int ledgeSortingOrder;
    private Parallax2D bossParallax;
    private BossAttack2D bossAttack;
    private readonly List<GameObject> summonProps = new List<GameObject>();

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
        SetActiveAll(revealAfterDelay, false);
        StageBossForward();
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
        // her phase controller's Start() runs at the end of this frame and opens
        // fire, so the shutdown has to land after it
        yield return null;
        if (bossAttack != null) bossAttack.SetPhase(0);

        // Frame each of them on the middle of their DRAWING, not on their transform.
        // The player's pivot sits at his waist and hers sits at her feet, so aiming
        // at raw positions dropped the camera a full unit on every cut to the boss.
        Vector3 bossShotPos = ShotOn(boss.position.x, bossRenderer);
        float playerX = playerRb != null ? playerRb.position.x : player.transform.position.x;
        Vector3 playerShotPos = ShotOn(playerX, player.GetComponent<SpriteRenderer>());

        // the camera only swings when the speaker actually changes
        bool cameraOnBoss = false;
        bool firstLine = true;

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine2D line = lines[i];
            if (line == null || string.IsNullOrEmpty(line.text)) continue;

            if (firstLine || line.isBoss != cameraOnBoss)
            {
                yield return StartCoroutine(line.isBoss
                    ? PanCameraTo(bossShotPos, bossShotOrthoSize, panDuration)
                    : PanCameraTo(playerShotPos, playerShotOrthoSize, panDuration));
                cameraOnBoss = line.isBoss;
                firstLine = false;
            }

            yield return StartCoroutine(ShowLine(line.isBoss,
                line.isBoss ? bossSpeakerName : playerSpeakerName, line.text));

            if (line.beatAfter == 1)
            {
                yield return StartCoroutine(SummonFirstMonster());
                cameraOnBoss = false;   // the shot moved, so re-frame on the next line
                firstLine = true;
            }
            else if (line.beatAfter == 2)
            {
                yield return StartCoroutine(SummonTheRest());
                cameraOnBoss = false;
                firstLine = true;
            }
        }

        DestroyCaption();

        // pull back far enough to hold both of them, then let her go
        Vector3 wideShot = new Vector3((playerShotPos.x + ledgePosition.x) * 0.5f, playerShotPos.y + 1.2f, shotOffset.z);
        yield return StartCoroutine(PanCameraTo(wideShot, gameplayOrthoSize, panDuration));
        yield return StartCoroutine(BossExitToLedge());

        yield return StartCoroutine(PanCameraTo(playerShotPos, gameplayOrthoSize, panDuration));

        if (camFollow != null)
        {
            camFollow.offset = gameplayCamOffset;
            camFollow.enabled = true;
        }

        // control first, HUD with it - the arena stays quiet a beat longer
        SetActiveAll(revealAfterCutscene, true);
        player.enabled = true;

        yield return new WaitForSeconds(attackStartDelay);

        SetActiveAll(revealAfterDelay, true);
        if (bossAttack != null) bossAttack.SetPhase(1);

        Destroy(gameObject);
    }

    // Records the authored ledge pose, then brings her down onto the player's
    // plane: full size, normal sorting, no parallax. Her attack scripts live on
    // the same object, so they are switched off until the fight actually starts.
    private void StageBossForward()
    {
        ledgePosition = boss.position;
        ledgeScale = boss.localScale;

        bossParallax = boss.GetComponent<Parallax2D>();
        if (bossParallax != null) bossParallax.enabled = false;

        // no drifting while she is standing on solid ground
        FloatBob2D bob = boss.GetComponent<FloatBob2D>();
        if (bob != null) bob.enabled = false;

        // disabling the component does NOT stop a running coroutine, and the phase
        // controller kicks the attack loop off the moment the boss is activated.
        // Phase 0 is the only thing that actually holds her fire.
        bossAttack = boss.GetComponent<BossAttack2D>();

        float stopX = (playerRb != null ? playerRb.position.x : player.transform.position.x) + walkStepDistance;

        if (bossRenderer != null)
        {
            ledgeSortingOrder = bossRenderer.sortingOrder;
            bossRenderer.sortingOrder = bossCutsceneSortingOrder;

            // scale by the drawn width so the art can change without retuning this
            float drawnWidth = bossRenderer.bounds.size.x;
            if (drawnWidth > 0.001f)
            {
                float factor = bossCutsceneWidth / drawnWidth;
                boss.localScale = new Vector3(ledgeScale.x * factor, ledgeScale.y * factor, ledgeScale.z);
            }
        }

        // feet on the ledge: the pivot is not the bottom of the drawing
        float footOffset = 0f;
        if (bossRenderer != null) footOffset = boss.position.y - bossRenderer.bounds.min.y;
        boss.position = new Vector3(stopX + bossCutsceneGap, bossCutsceneGroundY + footOffset, ledgePosition.z);
    }

    // Her retinue rises beside her: a tentacle on each flank, then one of every
    // monster that will actually fight, further out still. These are display props
    // only - the real monsters walk on after the cutscene, unarmed until then.
    private IEnumerator SummonFirstMonster()
    {
        // one of them steps out while she is still being sweet about it
        Vector3 shot = new Vector3(boss.position.x + firstSummonShotOffset.x,
                                   bossCutsceneGroundY + firstSummonShotOffset.y, shotOffset.z);
        yield return StartCoroutine(PanCameraTo(shot, firstSummonOrthoSize, panDuration * 0.8f));

        yield return StartCoroutine(PopMonsterByIndex(firstSummonIndex));
        yield return new WaitForSeconds(summonHoldTime);
    }

    // Her retinue rises beside her: a tentacle on each flank, then whatever
    // monsters have not been shown yet, further out still. These are display props
    // only - the real monsters walk on after the cutscene, unarmed until then.
    private IEnumerator SummonTheRest()
    {
        Vector3 shot = new Vector3(boss.position.x + summonShotOffset.x,
                                   bossCutsceneGroundY + summonShotOffset.y, shotOffset.z);
        yield return StartCoroutine(PanCameraTo(shot, summonShotOrthoSize, panDuration));

        if (summonTentacleOffsets != null)
        {
            for (int i = 0; i < summonTentacleOffsets.Length; i++)
            {
                StartCoroutine(RaiseTentacleProp(boss.position.x + summonTentacleOffsets[i]));
                yield return new WaitForSeconds(summonStagger);
            }
        }

        if (revealAfterDelay != null)
        {
            for (int i = 0; i < revealAfterDelay.Length; i++)
            {
                if (i == firstSummonIndex) continue;   // already out
                StartCoroutine(PopMonsterByIndex(i));
                yield return new WaitForSeconds(summonStagger);
            }
        }

        yield return new WaitForSeconds(summonHoldTime);
    }

    private IEnumerator PopMonsterByIndex(int i)
    {
        if (revealAfterDelay == null || i < 0 || i >= revealAfterDelay.Length) yield break;

        float dx = (summonMonsterOffsets != null && i < summonMonsterOffsets.Length) ? summonMonsterOffsets[i] : 0f;
        float dy = (summonMonsterHeights != null && i < summonMonsterHeights.Length) ? summonMonsterHeights[i] : 0f;
        yield return StartCoroutine(PopMonsterProp(revealAfterDelay[i], boss.position.x + dx, bossCutsceneGroundY + dy));
    }

    // Sorting sits one below her on purpose: even if a flank prop drifts wide
    // enough to touch her outline, it can never draw over her.
    private SpriteRenderer NewProp(string label, float x, float y)
    {
        GameObject go = new GameObject(label);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = bossRenderer != null ? bossRenderer.sortingLayerName : "Default";
        sr.sortingOrder = bossCutsceneSortingOrder - 1;
        summonProps.Add(go);
        return sr;
    }

    private IEnumerator RaiseTentacleProp(float x)
    {
        if (tendrilAttackFrames == null || tendrilAttackFrames.Length == 0) yield break;

        SpriteRenderer sr = NewProp("SummonTentacle", x, bossCutsceneGroundY - summonTentacleSink);
        sr.sprite = tendrilAttackFrames[0];

        float nw = sr.sprite.bounds.size.x;
        float nh = sr.sprite.bounds.size.y;
        if (nw > 0.001f && nh > 0.001f)
        {
            sr.transform.localScale = new Vector3(summonTentacleSize.x / nw, summonTentacleSize.y / nh, 1f);
        }

        // the art does the rising; the base never leaves the floor
        int last = Mathf.Min(2, tendrilAttackFrames.Length - 1);
        for (int i = 0; i <= last; i++)
        {
            sr.sprite = tendrilAttackFrames[i];
            yield return new WaitForSeconds(tendrilFrameDuration);
        }

        yield return StartCoroutine(WritheTentacleProp(sr, last));
    }

    // Leans back and forth around its own base while alternating the two risen
    // frames, so the pair reads as alive for as long as the beat holds.
    private IEnumerator WritheTentacleProp(SpriteRenderer sr, int risenFrame)
    {
        int alt = Mathf.Min(risenFrame + 1, tendrilAttackFrames.Length - 1);
        float seed = Random.value * 10f;
        float frameTimer = 0f;
        bool showAlt = false;

        while (sr != null)
        {
            float k = Mathf.Sin((Time.time / Mathf.Max(0.05f, summonTentacleSwayPeriod) + seed) * Mathf.PI * 2f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, k * summonTentacleSwayAngle);

            frameTimer += Time.deltaTime;
            if (frameTimer >= summonTentacleIdleFrameInterval)
            {
                frameTimer -= summonTentacleIdleFrameInterval;
                showAlt = !showAlt;
                sr.sprite = tendrilAttackFrames[showAlt ? alt : risenFrame];
            }
            yield return null;
        }
    }

    private IEnumerator PopMonsterProp(GameObject source, float x, float y)
    {
        if (source == null) yield break;

        SpriteRenderer from = source.GetComponentInChildren<SpriteRenderer>(true);
        if (from == null || from.sprite == null) yield break;

        // borrow the real monster's own loop so the prop moves like it will in the fight
        Sprite[] frames = null;
        MonsterSpriteAnimator2D sourceAnim = source.GetComponent<MonsterSpriteAnimator2D>();
        if (sourceAnim != null && sourceAnim.activeFrames != null && sourceAnim.activeFrames.Length > 0)
        {
            frames = sourceAnim.activeFrames;
        }

        SpriteRenderer sr = NewProp("SummonMonster", x, y);
        sr.sprite = frames != null ? frames[0] : from.sprite;
        sr.flipX = true;

        float nw = sr.sprite.bounds.size.x;
        float scale = nw > 0.001f ? summonMonsterWidth / nw : 1f;

        // y is where the feet go, not the centre, or a ground monster sinks in half
        float halfHeight = sr.sprite.bounds.size.y * scale * 0.5f;
        Vector3 rest = new Vector3(x, y + halfHeight, 0f);
        sr.transform.position = rest;

        float t = 0f;
        while (t < summonPopDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / summonPopDuration));
            sr.transform.localScale = Vector3.one * scale * k;
            yield return null;
        }
        sr.transform.localScale = Vector3.one * scale;

        StartCoroutine(IdleMonsterProp(sr, frames, rest));
    }

    // Loops its frames and drifts, for as long as the prop is on stage.
    private IEnumerator IdleMonsterProp(SpriteRenderer sr, Sprite[] frames, Vector3 rest)
    {
        float seed = Random.value * 10f;
        float frameTimer = 0f;
        int frame = 0;

        while (sr != null)
        {
            if (frames != null && frames.Length > 1)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= summonMonsterFrameInterval)
                {
                    frameTimer -= summonMonsterFrameInterval;
                    frame = (frame + 1) % frames.Length;
                    sr.sprite = frames[frame];
                }
            }

            float k = Mathf.Sin((Time.time / Mathf.Max(0.05f, summonMonsterBobPeriod) + seed) * Mathf.PI * 2f);
            sr.transform.position = rest + new Vector3(0f, k * summonMonsterBobAmplitude, 0f);
            yield return null;
        }
    }

    private void ClearSummonProps()
    {
        for (int i = 0; i < summonProps.Count; i++)
        {
            if (summonProps[i] != null) Destroy(summonProps[i]);
        }
        summonProps.Clear();
    }

    // Lifts off, drifts back to the ledge, settles. The shrink and the parallax
    // handover happen across the travel, so the move itself sells the distance.
    private IEnumerator BossExitToLedge()
    {
        Vector3 from = boss.position;
        Vector3 forwardScale = boss.localScale;
        Vector3 apex = new Vector3(from.x, from.y + bossExitRiseHeight, from.z);

        yield return StartCoroutine(MoveBoss(from, apex, forwardScale, forwardScale, bossExitRiseDuration, false));

        Vector3 travelApex = new Vector3(ledgePosition.x, ledgePosition.y + bossExitRiseHeight * 0.7f, ledgePosition.z);
        yield return StartCoroutine(MoveBoss(apex, travelApex, forwardScale, ledgeScale, bossExitTravelDuration, true));

        // she crosses into the background layer only once she is over the ledge
        if (bossRenderer != null) bossRenderer.sortingOrder = ledgeSortingOrder;

        yield return StartCoroutine(MoveBoss(travelApex, ledgePosition, ledgeScale, ledgeScale, bossExitSettleDuration, false));

        boss.position = ledgePosition;
        boss.localScale = ledgeScale;

        // parallax anchors on enable, so it has to re-capture from the ledge
        if (bossParallax != null)
        {
            bossParallax.enabled = true;
            bossParallax.Capture();
        }

        // she only starts drifting once she is standing on the floating ledge
        FloatBob2D bob = boss.GetComponent<FloatBob2D>();
        if (bob != null)
        {
            bob.enabled = true;
            bob.Recapture();
        }

        ClearSummonProps();
    }

    private IEnumerator MoveBoss(Vector3 from, Vector3 to, Vector3 scaleFrom, Vector3 scaleTo, float duration, bool hover)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            Vector3 pos = Vector3.Lerp(from, to, k);
            if (hover) pos.y += Mathf.Sin(t * 6f) * bossExitHoverAmplitude;
            boss.position = pos;
            boss.localScale = Vector3.Lerp(scaleFrom, scaleTo, k);
            yield return null;
        }
        boss.position = to;
        boss.localScale = scaleTo;
    }

    private Vector3 ShotOn(float x, SpriteRenderer subject)
    {
        float y = subject != null ? subject.bounds.center.y : bossCutsceneGroundY;
        return new Vector3(x + shotOffset.x, y + shotOffset.y, shotOffset.z);
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

    private IEnumerator ShowLine(bool isBoss, string speaker, string line)
    {
        Sprite frame = isBoss ? bossDialogueFrame : playerDialogueFrame;

        // no window art wired up yet - fall back to the plain caption
        if (window == null || !window.Ready || frame == null)
        {
            if (speakerText != null) speakerText.text = speaker;
            if (captionText != null) captionText.text = line;
            yield return new WaitForSeconds(lineDisplayDuration);
            if (speakerText != null) speakerText.text = "";
            if (captionText != null) captionText.text = "";
            yield return new WaitForSeconds(lineGap);
            yield break;
        }

        yield return window.Show(frame, line);
    }

    private void CreateCaption()
    {
        Sprite reference = playerDialogueFrame != null ? playerDialogueFrame : bossDialogueFrame;
        if (reference != null)
        {
            window = gameObject.AddComponent<DialogueWindow2D>();
            window.font = captionFont;
            window.textArea = dialogueTextArea;
            window.fontSize = dialogueFontSize;
            window.textColor = dialogueTextColor;
            window.fadeDuration = dialogueFadeDuration;
            window.frameWidth01 = dialogueFrameWidth01;
            window.frameBottomMargin = dialogueFrameBottomMargin;
            window.frameXOffset = dialogueFrameXOffset;
            window.lineMinDuration = lineMinDuration;
            window.lineAutoAdvance = lineDisplayDuration;
            window.lineGap = lineGap;
            window.Build(reference);
            if (window.Ready) return;
        }

        // fallback: a bare caption at the bottom of the screen
        captionCanvas = new GameObject("BossIntroCaptionCanvas");
        Canvas canvas = captionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        captionCanvas.AddComponent<CanvasScaler>();

        GameObject speakerGO = new GameObject("BossIntroSpeakerText");
        speakerGO.transform.SetParent(captionCanvas.transform, false);
        speakerText = speakerGO.AddComponent<Text>();
        speakerText.font = captionFont;
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
        textGO.transform.SetParent(captionCanvas.transform, false);
        captionText = textGO.AddComponent<Text>();
        captionText.font = captionFont;
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
        if (window != null) window.Dispose();
        if (captionCanvas != null) Destroy(captionCanvas);
    }
}
