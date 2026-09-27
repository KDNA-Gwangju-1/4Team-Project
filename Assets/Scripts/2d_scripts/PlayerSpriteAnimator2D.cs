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
    public Sprite idleFlashlightSprite;

    public Sprite[] jumpFrames;
    public float jumpFrameDuration = 0.12f;
    public float[] jumpFrameDurations;

    [Tooltip("Optional dedicated dash pose. Falls back to a walk frame.")]
    public Sprite[] dashFrames;
    [Tooltip("Which walk frame to hold while dashing - the widest, most forward-leaning stride.")]
    public int dashWalkFrameIndex = 4;

    [Header("Airborne")]
    // The jump sheet is a closed cycle: windup, rise, apex, descent, landing crouch.
    // Playing it straight through leaves the player floating in the LANDING pose,
    // which is wrong the moment a jump lasts longer than the clip - and in the
    // low-gravity phase that is every jump. So past the launch burst the frame is
    // chosen from vertical speed instead of from a timer.
    [Tooltip("Pick the airborne frame from vertical speed instead of playing once and freezing.")]
    public bool velocityDrivenJump = true;
    [Tooltip("Last frame of the launch burst; also held while still rising.")]
    public int jumpRiseFrameEnd = 2;
    [Tooltip("Held while vertical speed sits inside the apex band.")]
    public int jumpApexFrame = 3;
    [Tooltip("Held while falling. Must not be the landing crouch.")]
    public int jumpFallFrame = 4;
    [Tooltip("Vertical speed under this magnitude counts as the apex.")]
    public float jumpApexBand = 2.5f;
    [Tooltip("With the light up, hold one jump pose instead of running the arc. The raised arm makes the cycle read as flailing.")]
    public bool singleJumpPoseWithLight = true;
    public int jumpLightFrame = 3;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement2D player;
    private AnimState currentState = AnimState.Idle;
    private bool usingFlashlightWalk;
    private bool usingFlashlightIdle;
    public bool HasHoldingArtwork => player != null && player.HasLantern
        && walkFlashlightFrames != null && walkFlashlightFrames.Length >= 8;
    private static readonly float[] HoldingLensY = { 228f, 242f, 243f, 232f, 228f, 242f, 243f, 232f };

    // Lens positions in the eight 512px holding frames (top-left image coordinates).
    // Resolve the displayed frame, not velocity: cutscenes can drive the animation too.
    public bool TryGetFlashlightEmitter(out Vector3 position)
    {
        position = transform.position;
        if (!HasHoldingArtwork || sr == null) return false;
        int index = System.Array.IndexOf(walkFlashlightFrames, sr.sprite);
        if (sr.sprite == idleFlashlightSprite && idleFlashlightSprite != null) index = 6;
        if (index < 0) return false;
        Vector2 pixel = new Vector2(448f, 512f - HoldingLensY[index % 8]);
        Vector2 local = (pixel / 512f * sr.sprite.rect.size - sr.sprite.pivot) / sr.sprite.pixelsPerUnit;
        // Input may change direction before this animator's Update runs.
        bool flipped = player.IsLightOn ? player.LightDirection.x < 0f : player.FacingDirection < 0;
        if (flipped) local.x = -local.x;
        if (sr.flipY) local.y = -local.y;
        position = transform.TransformPoint(local);
        return true;
    }
    private int frameIndex;
    private float frameTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerMovement2D>();
        if (idleFlashlightSprite == null)
            idleFlashlightSprite = Resources.Load<Sprite>("Chapter2/Player/player-idle-flashlight");
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

        bool nextUsingFlashlightIdle = next == AnimState.Idle && HasHoldingArtwork;

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

        if (HasHoldingArtwork && currentState != AnimState.Walk)
        {
            if (currentState == AnimState.Idle && idleFlashlightSprite != null)
            {
                sr.sprite = idleFlashlightSprite;
                return;
            }
            // Frame 7 is the narrow planted stance; frame 4 raises a knee for flight.
            // Keep the same hand, lamp and pixel scale through every equipped state.
            int holdingFrame = currentState == AnimState.Dash ? dashWalkFrameIndex
                : currentState == AnimState.Jump ? 3 : 6;
            sr.sprite = walkFlashlightFrames[Mathf.Clamp(holdingFrame, 0, walkFlashlightFrames.Length - 1)];
            return;
        }

        if (currentState == AnimState.Dash)
        {
            // one held pose reads as a dash; a walk cycle at 20 u/s reads as sliding
            int index = (dashFrames != null && dashFrames.Length > 0) ? 0 : dashWalkFrameIndex;
            sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            return;
        }

        if (currentState == AnimState.Jump && velocityDrivenJump && rb != null)
        {
            UpdateAirborneFrame(frames);
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

    // Launch burst runs on the timer so the takeoff still reads as a push-off;
    // after that the pose tracks the arc, so a long float never goes stale.
    private void UpdateAirborneFrame(Sprite[] frames)
    {
        int last = frames.Length - 1;

        // one steady pose while the beam is up
        if (singleJumpPoseWithLight && player != null && player.IsLightOn)
        {
            frameIndex = Mathf.Clamp(jumpLightFrame, 0, last);
            sr.sprite = frames[frameIndex];
            return;
        }

        float verticalSpeed = rb.linearVelocity.y;
        bool rising = verticalSpeed > jumpApexBand;

        if (rising && frameIndex < jumpRiseFrameEnd)
        {
            float duration = CurrentFrameDuration(frameIndex);
            frameTimer += Time.deltaTime;
            if (frameTimer >= duration)
            {
                frameTimer -= duration;
                frameIndex = Mathf.Min(frameIndex + 1, jumpRiseFrameEnd);
            }
        }
        else if (rising)
        {
            frameIndex = jumpRiseFrameEnd;
        }
        else if (verticalSpeed < -jumpApexBand)
        {
            frameIndex = jumpFallFrame;
        }
        else
        {
            frameIndex = jumpApexFrame;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, last);
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
            default: return idleFrames;
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
