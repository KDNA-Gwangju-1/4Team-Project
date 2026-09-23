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
        yield return ShowMarkOver(rabbit.transform, "!?", surpriseDuration);
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

    // 보스 인트로의 "?"와 같은 만듦새. 대상 머리 위에 튀어나왔다가 사라진다.
    private IEnumerator ShowMarkOver(Transform target, string text, float hold)
    {
        GameObject go = new GameObject("IntroMark");
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.2f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.92f, 0.3f);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerName = "Default";
        mr.sortingOrder = 100;

        Vector3 baseScale = Vector3.one * 0.6f;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            go.transform.position = target.position + new Vector3(0f, 1.6f, 0f);
            go.transform.localScale = Vector3.Lerp(Vector3.zero, baseScale * 1.3f, t / 0.2f);
            yield return null;
        }
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            go.transform.position = target.position + new Vector3(0f, 1.6f, 0f);
            go.transform.localScale = Vector3.Lerp(baseScale * 1.3f, baseScale, t / 0.12f);
            yield return null;
        }
        t = 0f;
        while (t < hold)
        {
            t += Time.deltaTime;
            go.transform.position = target.position + new Vector3(0f, 1.6f, 0f);
            yield return null;
        }
        Destroy(go);
    }
}
