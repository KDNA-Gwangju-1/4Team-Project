using System.Collections;
using UnityEngine;

// 2스테이지 입장: 토끼를 발견하고, 그냥 쏘면 빗나가고, 빛을 비춘 뒤 쏘면 잡힌다는 것을 보여준다.
// 토끼는 실제 Monster2D를 그대로 쓴다 - 안 비추면 탄이 통과하고 비추면 잡히는 규칙 자체가 게임 로직이라
// 흉내 내지 않고 진짜로 굴린다. 다가오고 멈추는 타이밍만 detectionRange로 조절한다.
public class Stage2IntroCutscene : MonoBehaviour
{
    public PlayerMovement2D player;
    public Camera cam;
    public Monster2D rabbit;
    public Font captionFont;
    public Sprite playerDialogueFrame;

    [Header("Timing")]
    public float openingDelay = 0.6f;
    [Tooltip("처음 다가올 때 이 거리에서 멈춘다.")]
    public float rabbitStopDistance = 6.5f;
    [Tooltip("탄이 빗나간 뒤 다시 다가와 이 거리까지 온다.")]
    public float rabbitCloseDistance = 3.5f;
    public float approachTimeout = 4f;
    public float missShotHold = 0.6f;
    public float revealHold = 0.5f;
    public float surpriseDuration = 1f;

    [Header("Dialogue")]
    public string lineNotice = "...? 저건 뭐야?";
    public string lineApproach = "이 쪽으로 다가오잖아...?";
    public string lineShoot = "에잇!";
    public string lineMissed = "빗나간 건가?";
    public string lineSeeClearly = "이제 잘 보이네";
    public string lineLesson = "빛으로 비춘 후에 탄을 쏘면 없앨 수 있겠군";
    public Rect dialogueTextArea = new Rect(0.40f, 0.10f, 0.54f, 0.19f);
    public int dialogueFontSize = 34;
    public Color dialogueTextColor = Color.white;
    [Range(0.3f, 1f)] public float dialogueFrameWidth01 = 0.88f;
    public float dialogueFrameBottomMargin = 24f;
    public float dialogueFrameXOffset = -120f;
    public float lineAutoAdvance = 3f;
    public float lineGap = 0.2f;

    private DialogueWindow2D window;
    private float rabbitDetectionRange;
    private float rabbitRespawnDelay;

    // 죽어서 리트라이로 다시 들어온 판인지. DeathRetryUI2D가 씬을 다시 올리기 직전에 세운다.
    public static bool SkipIntroOnce;

    void Start()
    {
        if (player == null || cam == null || rabbit == null) return;

        if (!player.HasLantern) player.PickUpLantern();

        if (SkipIntroOnce)
        {
            // 시범용 토끼까지 통째로 건너뛴다 - 이미 본 장면이고, 토끼는 규칙을 가르치는 소품이다
            SkipIntroOnce = false;
            Destroy(rabbit.gameObject);
            Destroy(this);
            return;
        }
        player.enabled = false;
        player.CutsceneInvulnerable = true;

        // 대사 중에는 제자리에. 다가오는 타이밍은 스크립트가 준다.
        rabbitDetectionRange = rabbit.detectionRange;
        rabbitRespawnDelay = rabbit.respawnDelay;
        rabbit.detectionRange = 0f;
        rabbit.respawnDelay = 99999f;   // 시범용 토끼는 되살아나지 않는다

        BuildWindow();
        StartCoroutine(Play());
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

    private IEnumerator Play()
    {
        Vector2 toRabbit = AimDirection();
        yield return new WaitForSeconds(openingDelay);

        yield return Line(lineNotice);

        // 다가온다 - 대사와 동시에 움직이고, 가까워지면 세운다
        LetRabbitApproach(rabbitStopDistance);
        yield return Line(lineApproach);

        // 비추지 않은 채 쏜다 - Bullet2D는 드러나지 않은 몬스터를 그냥 지나간다
        yield return Line(lineShoot);
        player.CutsceneFire(AimDirection());
        yield return new WaitForSeconds(missShotHold);

        // 빗나갔고, 토끼는 아랑곳없이 더 다가온다
        LetRabbitApproach(rabbitCloseDistance);
        yield return Line(lineMissed);

        // 비춘다 - Monster2D가 진짜로 드러난다. 토끼가 자리를 잡는 동안 계속 따라 조준한다.
        Coroutine track = StartCoroutine(KeepLightOnRabbit());
        float waited = 0f;
        while (!rabbit.IsRevealed && waited < 1.5f) { waited += Time.deltaTime; yield return null; }
        rabbit.detectionRange = 0f;   // 놀라서 멈춘다
        yield return new WaitForSeconds(revealHold);
        yield return ShowMarkOver(rabbit.transform, surpriseDuration);
        yield return Line(lineSeeClearly);

        // 드러난 채로 쏜다 - 이번엔 맞는다
        player.CutsceneFire(AimDirection());
        waited = 0f;
        while (!rabbit.IsDead && waited < 2f) { waited += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(0.4f);

        yield return Line(lineLesson);

        StopCoroutine(track);
        player.CutsceneSetLight(false, AimDirection());
        if (window != null) window.Dispose();
        player.CutsceneInvulnerable = false;
        player.enabled = true;
        // 카메라에 붙어 있으므로 오브젝트가 아니라 이 컴포넌트만 걷어낸다
        Destroy(this);
    }

    private IEnumerator KeepLightOnRabbit()
    {
        while (true)
        {
            player.CutsceneSetLight(true, AimDirection());
            yield return null;
        }
    }

    private Coroutine approachWatch;

    private void LetRabbitApproach(float stopDistance)
    {
        if (approachWatch != null) StopCoroutine(approachWatch);
        rabbit.detectionRange = rabbitDetectionRange > 0f ? Mathf.Max(rabbitDetectionRange, 40f) : 40f;
        approachWatch = StartCoroutine(StopRabbitWhenClose(stopDistance));
    }

    private IEnumerator StopRabbitWhenClose(float stopDistance)
    {
        float t = 0f;
        while (t < approachTimeout)
        {
            t += Time.deltaTime;
            float dx = Mathf.Abs(rabbit.transform.position.x - player.transform.position.x);
            if (dx <= stopDistance) break;
            yield return null;
        }
        rabbit.detectionRange = 0f;
        approachWatch = null;
    }

    private IEnumerator Line(string text)
    {
        if (window == null || !window.Ready || string.IsNullOrEmpty(text)) yield break;
        yield return window.Show(playerDialogueFrame, text);
    }

    // 램프 위치에서 토끼 몸통 중심으로. 플레이어 중심에서 재면 빛줄기가 수평으로 나가 귀만 걸린다.
    private Vector2 AimDirection()
    {
        var sr = rabbit.GetComponent<SpriteRenderer>();
        Vector2 target = sr != null ? (Vector2)sr.bounds.center : (Vector2)rabbit.transform.position;
        Vector2 d = target - (Vector2)player.FlashlightOrigin;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
    }

    private GameObject surpriseMark;
    private static Sprite surpriseBadgeSprite;

    private void OnDisable()
    {
        // The cue must not remain behind if the intro is interrupted or retried.
        if (surpriseMark != null) Destroy(surpriseMark);
    }

    private IEnumerator ShowMarkOver(Transform target, float hold)
    {
        if (target == null) yield break;
        if (surpriseMark != null) Destroy(surpriseMark);
        GameObject go = new GameObject("RabbitSurprisePixelBadge");
        surpriseMark = go;
        SpriteRenderer source = target.GetComponent<SpriteRenderer>();
        SpriteRenderer mark = go.AddComponent<SpriteRenderer>();
        mark.sprite = GetSurpriseBadgeSprite();
        mark.sortingLayerID = source != null ? source.sortingLayerID : 0;
        mark.sortingOrder = source != null ? Mathf.Max(100, source.sortingOrder + 20) : 100;
        mark.maskInteraction = SpriteMaskInteraction.None;
        const float pop = 0.16f, settle = 0.12f, fade = 0.12f;
        float duration = pop + settle + Mathf.Max(0f, hold) + fade;
        float elapsed = 0f;
        go.transform.localScale = Vector3.zero;
        try
        {
            while (elapsed < duration && target != null && go != null)
            {
                elapsed += Time.deltaTime;
                float scale;
                if (elapsed < pop)
                    scale = Mathf.Lerp(0.65f, 1.12f, Mathf.SmoothStep(0f, 1f, elapsed / pop));
                else if (elapsed < pop + settle)
                    scale = Mathf.Lerp(1.12f, 1f, (elapsed - pop) / settle);
                else scale = 1f;
                float alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.06f),
                    Mathf.Clamp01((duration - elapsed) / fade));
                mark.color = new Color(1f, 1f, 1f, alpha);
                go.transform.localScale = Vector3.one * scale;
                // World bounds clear the ears even when the rabbit hops/flips/scales.
                Vector3 anchor = source != null
                    ? new Vector3(source.bounds.center.x, source.bounds.max.y + 0.18f, target.position.z)
                    : target.position + Vector3.up * 1.6f;
                go.transform.position = anchor;
                yield return null;
            }
        }
        finally
        {
            if (go != null) Destroy(go);
            if (surpriseMark == go) surpriseMark = null;
        }
    }

    private static Sprite GetSurpriseBadgeSprite()
    {
        if (surpriseBadgeSprite != null) return surpriseBadgeSprite;
        // Same ink/lavender/ivory palette as the boss cue; coral adds surprise.
        const int width = 41, height = 35;
        Color ink = new Color32(20, 16, 35, 255);
        Color rim = new Color32(166, 137, 219, 255);
        Color light = new Color32(220, 204, 255, 255);
        Color fill = new Color32(40, 31, 60, 255);
        Color ivory = new Color32(255, 231, 153, 255);
        Color coral = new Color32(255, 145, 155, 255);
        Color[] pixels = new Color[width * height];
        for (int y = 7; y <= 32; y++)
        for (int x = 2; x <= 38; x++)
        {
            if ((x < 4 || x > 36) && (y < 9 || y > 30)) continue;
            int edge = Mathf.Min(x - 2, 38 - x, y - 7, 32 - y);
            pixels[y * width + x] = edge == 0 ? ink : edge == 1 ? rim : fill;
        }
        // Centered stepped speech tail, drawn over the lower border.
        for (int y = 1; y <= 8; y++)
        for (int x = 20; x <= 20 + y / 2; x++)
            pixels[y * width + x] = x == 20 || x == 20 + y / 2 ? rim : fill;
        for (int x = 7; x <= 33; x++) pixels[30 * width + x] = light;
        string[] question = { "01110", "11011", "00011", "00110", "00100", "00000", "00100" };
        string[] surprise = { "110", "110", "110", "110", "010", "000", "010" };
        // Pixel shadows and two solid glyphs; no font or antialiased TextMesh.
        for (int pass = 0; pass < 2; pass++)
        for (int row = 0; row < 7; row++)
        for (int col = 0; col < 5; col++)
        for (int dy = 0; dy < 2; dy++)
        for (int dx = 0; dx < 2; dx++)
        {
            int offset = pass == 0 ? 1 : 0;
            int y = 26 - row * 2 + dy - offset;
            if (question[row][col] == '1')
                pixels[y * width + 22 + col * 2 + dx + offset] = pass == 0 ? ink : ivory;
            if (col < 3 && surprise[row][col] == '1')
                pixels[y * width + 10 + col * 2 + dx + offset] = pass == 0 ? ink : coral;
        }
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "RabbitSurprisePixelBadge";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        surpriseBadgeSprite = Sprite.Create(texture, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0f), 28f);
        surpriseBadgeSprite.name = "RabbitSurprisePixelBadge";
        return surpriseBadgeSprite;
    }
}
