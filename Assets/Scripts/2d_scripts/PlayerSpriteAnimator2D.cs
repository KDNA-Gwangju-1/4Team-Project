using UnityEngine;

public class PlayerSpriteAnimator2D : MonoBehaviour
{
    private enum AnimState { Idle, Walk, Jump, Dash }

    public Sprite[] idleFrames;
    public float idleFrameDuration = 0.16f;

    public Sprite[] walkFrames;
    public float walkFrameDuration = 0.11f;

    public Sprite[] walkFlashlightFrames;
    public float walkFlashlightFrameDuration = 0.11f;

    public Sprite[] jumpFrames;
    public float jumpFrameDuration = 0.12f;
    public float[] jumpFrameDurations;

    [Tooltip("Optional dedicated dash pose. Falls back to a walk frame.")]
    public Sprite[] dashFrames;
    [Tooltip("Which walk frame to hold while dashing - the widest, most forward-leaning stride.")]
    public int dashWalkFrameIndex = 4;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement2D player;
    private AnimState currentState = AnimState.Idle;
    private bool usingFlashlightWalk;
    private bool usingFlashlightIdle;
    private int frameIndex;
    private float frameTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerMovement2D>();
    }

    void Update()
    {
        UpdateState();
        UpdateFrame();
        UpdateFacing();
    }

    private void UpdateState()
    {
        bool isGrounded = player == null || !player.enabled || player.IsGrounded;
        float horizontalSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;

        AnimState next;
        if (player != null && player.IsDashing)
        {
            next = AnimState.Dash;
        }
        else if (!isGrounded)
        {
            next = AnimState.Jump;
        }
        else if (horizontalSpeed > 0.1f)
        {
            next = AnimState.Walk;
        }
        else
        {
            next = AnimState.Idle;
        }

        bool nextUsingFlashlightWalk = next == AnimState.Walk && player != null
            && player.HasLantern && walkFlashlightFrames != null && walkFlashlightFrames.Length > 0;

        bool nextUsingFlashlightIdle = next == AnimState.Idle && player != null
            && player.IsLightOn && walkFlashlightFrames != null && walkFlashlightFrames.Length > 0;

        if (next != currentState || nextUsingFlashlightWalk != usingFlashlightWalk || nextUsingFlashlightIdle != usingFlashlightIdle)
        {
            currentState = next;
            usingFlashlightWalk = nextUsingFlashlightWalk;
            usingFlashlightIdle = nextUsingFlashlightIdle;

            // Jump frame 0 tucks the arm out of view, which looks disconnected from the
            // held-flashlight overlay, so skip straight to frame 1 while the light is on.
            bool skipFirstJumpFrame = next == AnimState.Jump && player != null && player.IsLightOn
                && jumpFrames != null && jumpFrames.Length > 1;
            frameIndex = skipFirstJumpFrame ? 1 : 0;
            frameTimer = 0f;
        }
    }

    private void UpdateFrame()
    {
        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length == 0 || sr == null) return;

        if (usingFlashlightIdle)
        {
            sr.sprite = frames[0];
            return;
        }

        if (currentState == AnimState.Dash)
        {
            // one held pose reads as a dash; a walk cycle at 20 u/s reads as sliding
            int index = (dashFrames != null && dashFrames.Length > 0) ? 0 : dashWalkFrameIndex;
            sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            return;
        }

        bool loops = currentState != AnimState.Jump;
        float duration = CurrentFrameDuration(frameIndex);
        frameTimer += Time.deltaTime;
        if (frameTimer >= duration && (loops || frameIndex < frames.Length - 1))
        {
            frameTimer -= duration;
            frameIndex = loops ? (frameIndex + 1) % frames.Length : Mathf.Min(frameIndex + 1, frames.Length - 1);
        }

        sr.sprite = frames[frameIndex];
    }

    private void UpdateFacing()
    {
        if (sr == null || player == null) return;

        if (player.IsLightOn)
        {
            sr.flipX = player.LightDirection.x < 0f;
        }
        else
        {
            sr.flipX = player.FacingDirection < 0;
        }
    }

    private Sprite[] CurrentFrames()
    {
        switch (currentState)
        {
            case AnimState.Walk: return usingFlashlightWalk ? walkFlashlightFrames : walkFrames;
            case AnimState.Jump: return jumpFrames;
            case AnimState.Dash:
                if (dashFrames != null && dashFrames.Length > 0) return dashFrames;
                return walkFrames;
            default: return usingFlashlightIdle ? walkFlashlightFrames : idleFrames;
        }
    }

    private float CurrentFrameDuration(int index)
    {
        switch (currentState)
        {
            case AnimState.Walk: return usingFlashlightWalk ? walkFlashlightFrameDuration : walkFrameDuration;
            case AnimState.Jump:
                if (jumpFrameDurations != null && index >= 0 && index < jumpFrameDurations.Length)
                {
                    return jumpFrameDurations[index];
                }
                return jumpFrameDuration;
            default: return idleFrameDuration;
        }
    }
}
