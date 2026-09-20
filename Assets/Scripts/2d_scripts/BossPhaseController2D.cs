using System.Collections;
using UnityEngine;

public class BossPhaseController2D : MonoBehaviour
{
    public Boss2D boss;
    public BossAttack2D attack;
    public CameraFollow2D cameraFollow;

    public int phase2AtHealth = 3;

    public GameObject[] phase1Objects;
    public GameObject[] phase2Objects;
    public GameObject[] collapsingFloors;

    public float slamTelegraph = 1f;
    [Tooltip("Which claw frame lands the hit. The floor breaks ON this frame, not after the whole swing.")]
    public int clawImpactFrame = 5;
    public float clawFrameStep = 0.07f;
    public float slamHoldTime = 0.35f;
    public float shakeDuration = 1.1f;
    public float shakeMagnitude = 0.4f;
    public float collapseFallSpeed = 7f;
    public float collapseFadeTime = 1.4f;

    public Transform phase2RespawnAnchor;
    public float phase2GravityScale = 0.25f;
    public float phase2FallRespawnY = -24f;
    public float phase2LaunchImpulse = 4f;
    public bool phase2AirDash = true;

    public Sprite[] phase2Frames;
    public float phase2FrameDuration = 0.13f;

    [Tooltip("Plays before phase 2 starts. Everything below waits for it to finish.")]
    public Stage3Phase2Cutscene phase2Cutscene;
    [Tooltip("The cutscene now lifts them both out of the arena itself, so the claw swing and the floor collapse are skipped.")]
    public bool cutsceneHandlesArenaChange = true;

    public bool moveBossOnPhase2 = false;
    public Vector2 phase2BossPosition;
    public float bossMoveTime = 1f;

    private int phase;
    private bool transitioning;

    public int Phase => phase;

    void Start()
    {
        if (boss == null) boss = GetComponent<Boss2D>();
        if (attack == null) attack = GetComponent<BossAttack2D>();
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();

        SetActiveAll(phase2Objects, false);
        SetActiveAll(phase1Objects, true);

        if (boss != null)
        {
            boss.requireLightToDamage = false;
            boss.OnDamaged += HandleDamaged;
        }

        phase = 1;
        if (attack != null) attack.SetPhase(1);
    }

    void OnDestroy()
    {
        if (boss != null) boss.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(int current, int max)
    {
        if (phase != 1 || transitioning || current <= 0) return;
        if (current > phase2AtHealth) return;

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        transitioning = true;
        phase = 0;

        if (attack != null) attack.SetPhase(0);
        if (boss != null) boss.Invulnerable = true;

        // she comes back down to him and says her piece before the floor goes
        if (phase2Cutscene != null) yield return phase2Cutscene.Play();

        yield return new WaitForSeconds(slamTelegraph);

        // Wind up, then break the floor ON the impact frame. Playing the whole
        // swing first and collapsing afterwards reads as two unrelated events.
        bool ownArenaChange = !cutsceneHandlesArenaChange;
        Sprite[] claw = (attack != null) ? attack.clawFrames : null;
        bool hasClaw = ownArenaChange && claw != null && claw.Length > 0 && attack.animator != null;
        int impact = hasClaw ? Mathf.Clamp(clawImpactFrame, 0, claw.Length - 1) : 0;

        if (hasClaw)
        {
            for (int i = 0; i <= impact; i++)
            {
                attack.animator.ShowFrame(claw[i]);
                yield return new WaitForSeconds(clawFrameStep);
            }
        }

        if (ownArenaChange && cameraFollow != null) cameraFollow.Shake(shakeDuration, shakeMagnitude);

        var player = PlayerMovement2D.Instance;
        if (player != null)
        {
            player.SetGravityScale(phase2GravityScale);
            player.allowAirDash = phase2AirDash;
            player.fallRespawnY = phase2FallRespawnY;
            if (phase2RespawnAnchor != null) player.SetRespawnAnchor(phase2RespawnAnchor.position);
            if (phase2LaunchImpulse > 0f) player.AddImpulse(Vector2.up * phase2LaunchImpulse);
        }

        SetActiveAll(phase2Objects, true);

        if (ownArenaChange)
        {
            for (int i = 0; i < collapsingFloors.Length; i++)
            {
                StartCoroutine(CollapseRoutine(collapsingFloors[i], i * 0.08f));
            }
        }

        // the rest of the swing follows through while the floor is already falling
        if (hasClaw)
        {
            for (int i = impact + 1; i < claw.Length; i++)
            {
                attack.animator.ShowFrame(claw[i]);
                yield return new WaitForSeconds(clawFrameStep);
            }
            attack.animator.ReleaseFrame();
        }

        yield return new WaitForSeconds(slamHoldTime);
        SetActiveAll(phase1Objects, false);

        if (phase2Frames != null && phase2Frames.Length > 0 && attack != null && attack.animator != null)
        {
            attack.animator.activeFrameDuration = phase2FrameDuration;
            attack.animator.SetLoopFrames(phase2Frames);
        }

        if (moveBossOnPhase2) yield return MoveBossRoutine();

        if (boss != null)
        {
            boss.requireLightToDamage = true;
            boss.Invulnerable = false;
        }

        phase = 2;
        if (attack != null) attack.SetPhase(2);
        transitioning = false;
    }

    private IEnumerator CollapseRoutine(GameObject floor, float delay)
    {
        if (floor == null) yield break;

        Collider2D[] cols = floor.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        yield return new WaitForSeconds(delay);

        SpriteRenderer[] renderers = floor.GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < collapseFadeTime)
        {
            elapsed += Time.deltaTime;
            floor.transform.position += Vector3.down * collapseFallSpeed * Time.deltaTime;

            float a = 1f - (elapsed / collapseFadeTime);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
            yield return null;
        }

        floor.SetActive(false);
    }

    private IEnumerator MoveBossRoutine()
    {
        if (boss == null) yield break;

        Vector3 from = boss.transform.position;
        Vector3 to = new Vector3(phase2BossPosition.x, phase2BossPosition.y, from.z);
        float elapsed = 0f;
        while (elapsed < bossMoveTime)
        {
            elapsed += Time.deltaTime;
            boss.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / bossMoveTime));
            yield return null;
        }
        boss.transform.position = to;
    }

    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null) objects[i].SetActive(active);
        }
    }
}
