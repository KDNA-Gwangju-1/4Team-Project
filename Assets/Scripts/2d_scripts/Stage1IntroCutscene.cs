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
        "...딱 봐도 질 나쁜 악몽 같은데 추가 요금을 청구해야겠어.",
        "...그러고 보니 동생 쪽이 언니를 별로 안 좋아했다고 들었던 것 같은데... 그게 이유일지도...",
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

    [Header("Opening - falling in through a crack in the sky")]
    [Tooltip("Chapter 1 ends with the dream tearing open; chapter 2 starts with the detective dropping out of that tear.")]
    public bool playRiftFall = true;
    [Tooltip("How far above his spawn point he appears inside the crack.")]
    public float fallHeight = 9f;
    [Tooltip("The rift art is small pixel art; this scales it up in the world.")]
    public float riftScale = 1.8f;
    public float riftOpenTime = 1.3f;
    public float riftHoldTime = 0.45f;
    public float riftCloseTime = 0.6f;
    public string chapterLabel = "챕터 2";
    public string chapterTitle = "어두운 꿈";
    public float titleHoldTime = 1.8f;

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
        if (playRiftFall) yield return RiftFall();
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

    // ============================================================
    // Opening: black -> crack tears open in the sky -> he drops out -> lands -> chapter title
    // Sprites come from Resources/Sprites/ChapterRift (Tools/Art/Build Chapter 2 Rift Sprites).
    // ============================================================

    private IEnumerator RiftFall()
    {
        var frames = new Sprite[8];
        for (int i = 0; i < frames.Length; i++) frames[i] = Resources.Load<Sprite>("Sprites/ChapterRift/rift_" + i);
        Sprite shard = Resources.Load<Sprite>("Sprites/ChapterRift/shard");
        Sprite dust = Resources.Load<Sprite>("Sprites/ChapterRift/dust");
        if (frames[7] == null) yield break;   // art not built - skip straight to the old intro

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector3 landing = player.transform.position;

        // the spawn point hugs the level's left bound, where the camera can't centre on it;
        // land a little further right so the whole crack is on screen
        float riftHalfWidth = frames[7].bounds.extents.x * riftScale;
        if (follow != null && follow.clampToBounds)
            landing.x = Mathf.Max(landing.x, follow.minX + riftHalfWidth + 1f);

        // while he falls the camera sits below him: the crack is high on screen and the ground comes up to meet him
        Vector3 gameplayOffset = follow != null ? follow.offset : Vector3.zero;
        if (follow != null) follow.offset = new Vector3(gameplayOffset.x, -2.5f, gameplayOffset.z);

        // hide him up inside the crack; the camera follows him there
        var renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        var wasVisible = new bool[renderers.Length];
        var orders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            wasVisible[i] = renderers[i].enabled; orders[i] = renderers[i].sortingOrder;
            renderers[i].enabled = false;
            renderers[i].sortingOrder += 40;   // in front of the crack while he falls out of it
        }
        if (rb != null) { rb.simulated = false; rb.linearVelocity = Vector2.zero; }
        player.transform.position = landing + Vector3.up * fallHeight;

        // black veil that lifts as the crack starts
        var ui = new GameObject("RiftIntroCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = ui.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        var veil = NewUiImage(ui.transform, new Color(0.02f, 0.01f, 0.04f, 1f));

        // the crack itself
        var riftGO = new GameObject("SkyRift");
        riftGO.transform.position = landing + Vector3.up * (fallHeight + 0.5f);
        riftGO.transform.localScale = Vector3.one * riftScale;
        var rift = riftGO.AddComponent<SpriteRenderer>();
        rift.sortingOrder = 35;
        rift.sprite = frames[0];

        // veil lifts on the empty sky, then the first crack
        yield return FadeImage(veil, 1f, 0f, 0.7f);
        GameSfx.Play("RiftOpen", .55f);
        if (follow != null) follow.Shake(riftOpenTime, 0.1f);
        for (float t = 0f; t < riftOpenTime; t += Time.deltaTime)
        {
            rift.sprite = frames[Mathf.Min(7, Mathf.FloorToInt(t / riftOpenTime * 8f))];
            if (shard != null && Random.value < Time.deltaTime * 6f) SpawnShard(shard, riftGO.transform.position);
            yield return null;
        }
        for (float t = 0f; t < riftHoldTime; t += Time.deltaTime)
        {
            rift.sprite = frames[(int)(t * 10f) % 2 == 0 ? 7 : 6];   // the rim flickers while it hangs open
            yield return null;
        }

        // he drops out - arms up, flailing, instead of the standing pose.
        // The animator would put the jump sheet back every frame, so it sits out until he lands.
        var body = player.GetComponent<SpriteRenderer>();
        var animator = player.GetComponent<PlayerSpriteAnimator2D>();
        bool animatorWasOn = animator != null && animator.enabled;
        var fallPose = new[] { Resources.Load<Sprite>("Sprites/ChapterRift/player_fall_0"), Resources.Load<Sprite>("Sprites/ChapterRift/player_fall_1") };
        bool hasFallPose = body != null && fallPose[0] != null && fallPose[1] != null;
        if (hasFallPose && animator != null) animator.enabled = false;
        if (body != null) body.flipX = false;

        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = wasVisible[i];
        if (rb != null) { rb.simulated = true; rb.linearVelocity = new Vector2(0f, -3f); }
        float fallTimer = 0f;
        while (fallTimer < 2.5f)
        {
            fallTimer += Time.deltaTime;
            bool settled = rb == null || (player.transform.position.y <= landing.y + 0.25f && rb.linearVelocity.y > -0.2f && fallTimer > 0.2f);
            if (settled) break;
            if (rb == null) player.transform.position = Vector3.MoveTowards(player.transform.position, landing, 14f * Time.deltaTime);
            if (hasFallPose)
            {
                body.sprite = fallPose[(int)(fallTimer / 0.08f) % 2];
                // a small tumble that settles out before touchdown
                float nearGround = Mathf.Clamp01((player.transform.position.y - landing.y) / 2f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fallTimer * 14f) * 6f * nearGround);
            }
            yield return null;
        }
        player.transform.rotation = Quaternion.identity;

        // touchdown crouch from the jump sheet, then the animator takes over again
        if (hasFallPose)
        {
            if (animator != null && animator.jumpFrames != null && animator.jumpFrames.Length > 5)
            {
                body.sprite = animator.jumpFrames[5];
                StartCoroutine(ResumeAnimatorAfter(animator, animatorWasOn, 0.22f));
            }
            else if (animator != null) animator.enabled = animatorWasOn;
        }

        // landing
        GameSfx.Play("Land", .6f);
        GameSfx.Play("Slam", .3f);
        if (follow != null) follow.Shake(0.3f, 0.22f);
        if (dust != null)
        {
            Vector3 feet = new Vector3(player.transform.position.x, landing.y - 1.0f, 0f);
            FadeAwayPuff2D.Spawn(feet + Vector3.left * 0.9f, dust, Color.white, 2.4f, 45, 0.9f, 1.6f);
            FadeAwayPuff2D.Spawn(feet + Vector3.right * 0.9f, dust, Color.white, 2.0f, 45, 0.85f, 1.6f);
            FadeAwayPuff2D.Spawn(feet + Vector3.up * 0.2f, dust, new Color(1f, 1f, 1f, .7f), 1.6f, 44, 0.7f, 2.2f);
        }
        for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = orders[i];

        // the sky seals itself again while the camera eases back to its gameplay framing
        GameSfx.Play("RiftClose", .45f);
        Vector3 fallOffset = follow != null ? follow.offset : Vector3.zero;
        for (float t = 0f; t < riftCloseTime; t += Time.deltaTime)
        {
            rift.sprite = frames[Mathf.Clamp(7 - Mathf.FloorToInt(t / riftCloseTime * 8f), 0, 7)];
            if (follow != null) follow.offset = Vector3.Lerp(fallOffset, gameplayOffset, Mathf.SmoothStep(0f, 1f, t / riftCloseTime));
            yield return null;
        }
        if (follow != null) follow.offset = gameplayOffset;
        Destroy(riftGO);

        // chapter title
        yield return ShowChapterTitle(ui.transform);
        Destroy(ui);
    }

    private IEnumerator ResumeAnimatorAfter(PlayerSpriteAnimator2D animator, bool enable, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null) animator.enabled = enable;
    }

    private IEnumerator ShowChapterTitle(Transform parent)
    {
        var group = new GameObject("ChapterTitle", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        var root = (RectTransform)group.transform;
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .62f);
        root.sizeDelta = new Vector2(900f, 220f);
        group.alpha = 0f;

        var label = NewUiText(root, chapterLabel, 30, new Color(.78f, .72f, 1f), 50f, false);
        var title = NewUiText(root, chapterTitle, 76, new Color(.97f, .95f, 1f), -18f, true);
        // thin rule between the two lines, in the chapter's violet
        var rule = NewUiImage(root, new Color(.58f, .45f, .92f, .9f));
        rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(.5f, .5f);
        rule.rectTransform.sizeDelta = new Vector2(360f, 2f);
        rule.rectTransform.anchoredPosition = new Vector2(0f, 26f);

        GameSfx.Play("Brand", .4f);
        for (float t = 0f; t < .6f; t += Time.deltaTime) { group.alpha = t / .6f; yield return null; }
        group.alpha = 1f;
        yield return new WaitForSeconds(titleHoldTime);
        for (float t = 0f; t < .6f; t += Time.deltaTime) { group.alpha = 1f - t / .6f; yield return null; }
        group.alpha = 0f;
    }

    private void SpawnShard(Sprite sprite, Vector3 around)
    {
        var go = new GameObject("RiftShard");
        go.transform.position = around + new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(-0.3f, 0.3f), 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 36;
        go.AddComponent<RiftShard2D>();
    }

    private static UnityEngine.UI.Image NewUiImage(Transform parent, Color color)
    {
        var image = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static UnityEngine.UI.Text NewUiText(RectTransform parent, string value, int size, Color color, float y, bool emphasis)
    {
        var text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
        text.transform.SetParent(parent, false);
        text.font = emphasis ? HangulFont.GetEmphasis() : HangulFont.Get();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = value;
        var shadow = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(.08f, .03f, .16f, .9f);
        shadow.effectDistance = new Vector2(0f, -3f);
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(900f, size + 20f);
        rt.anchoredPosition = new Vector2(0f, y);
        return text;
    }

    private static IEnumerator FadeImage(UnityEngine.UI.Image image, float from, float to, float duration)
    {
        Color c = image.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / duration);
            image.color = c;
            yield return null;
        }
        c.a = to;
        image.color = c;
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
