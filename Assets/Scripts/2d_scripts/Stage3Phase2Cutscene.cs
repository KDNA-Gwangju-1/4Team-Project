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
    [TextArea] public string bossLine1 = "...";
    [TextArea] public string bossLine2 = "너 싫어... 언니 같이 날 힘들게 해";
    [TextArea] public string playerLine = "괴물, 어서 쌍둥이 동생 몸에서 나가";
    [TextArea] public string bossLine3 = "...내가 괴물?";
    [TextArea] public string bossKillLine = "...죽어";

    [Header("Dialogue window")]
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.88f;
    public float dialogueFrameBottomMargin = 24f;
    public float dialogueFrameXOffset = -120f;
    public float lineMinDuration = 0.5f;
    public float lineAutoAdvance = 6f;
    public float lineGap = 0.3f;

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
    public float shadowSweepDuration = 1.2f;
    public float shadowHoldTime = 0.5f;
    public float shadowShake = 0.12f;

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

        float playerX = player.transform.position.x;
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();

        yield return TentacleDeathBeat(playerX);
        yield return BossReturn(playerX);

        Vector3 bossShot = ShotOn(boss.position.x, bossRenderer);
        Vector3 playerShot = ShotOn(playerX, playerRenderer);
        Vector3 twoShot = new Vector3((bossShot.x + playerShot.x) * 0.5f,
                                      (bossShot.y + playerShot.y) * 0.5f, shotOffset.z);

        yield return PanTo(twoShot, talkOrthoSize, panDuration);

        yield return Say(bossDialogueFrame, bossLine1);
        yield return Say(bossDialogueFrame, bossLine2);
        yield return Say(playerDialogueFrame, playerLine);
        yield return Say(bossDialogueFrame, bossLine3);

        yield return ShadowTurn();
        yield return KillLine(bossShot);

        if (window != null) window.Dispose();
        ClearProps();

        // rush back out to the gameplay framing - it doubles as the beat where
        // the floor is about to give way
        yield return PanTo(ShotOn(player.transform.position.x, playerRenderer),
                           gameplayOrthoSize, pullOutDuration);

        // control and the camera go back to BossPhaseController2D, which
        // drops the floor out from under him next
        if (camFollow != null) camFollow.enabled = true;
        player.enabled = true;
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
        Vector3 beatShot = new Vector3((playerX + x) * 0.5f, tentacleGroundY + 4.2f, shotOffset.z);
        yield return PanTo(beatShot, 6.4f, panDuration * 0.7f);

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
        AimFlashlightAt(sr.bounds.center);
        yield return new WaitForSeconds(lightHoldTime);

        yield return FireShot(sr.bounds.center);
        yield return HurtAndDie(sr, risen);
    }

    private void AimFlashlightAt(Vector3 target)
    {
        if (flashlight == null) return;
        Vector3 dir = target - flashlight.position;
        flashlight.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
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

        boss.position = basePos;
        bossRenderer.color = bossShadowTint;
        if (bossShadowSprite != null) bossRenderer.sprite = bossShadowSprite;

        // every line from here on wears the shadowed portrait
        if (bossShadowDialogueFrame != null) bossDialogueFrame = bossShadowDialogueFrame;

        yield return new WaitForSeconds(shadowHoldTime);
    }

    // ---------- "...죽어" ----------

    private IEnumerator KillLine(Vector3 bossShot)
    {
        // creep in first so the snap has something to land against
        yield return PanTo(bossShot, creepOrthoSize, creepDuration);
        yield return PanTo(bossShot, snapOrthoSize, snapDuration);

        if (camFollow != null) camFollow.Shake(0.35f, snapShake);
        yield return new WaitForSeconds(holdBeforeKillLine);

        yield return Say(bossDialogueFrame, bossKillLine);
        yield return new WaitForSeconds(holdAfterKillLine);
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
