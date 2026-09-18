using UnityEngine;

public class PlayerSpriteAnimator2D : MonoBehaviour
{
    private enum AnimState { Idle, Walk, Jump }

    public Sprite[] idleFrames;
    public float idleFrameDuration = 0.16f;

    public Sprite[] walkFrames;
    public float walkFrameDuration = 0.11f;

    public Sprite[] walkFlashlightFrames;
    public float walkFlashlightFrameDuration = 0.11f;

    public Sprite[] jumpFrames;
    public float jumpFrameDuration = 0.12f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement2D player;
    private AnimState currentState = AnimState.Idle;
    private bool usingFlashlightWalk;
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
        bool isGrounded = player == null || player.IsGrounded;
        float horizontalSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;

        AnimState next;
        if (!isGrounded)
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

        if (next != currentState || nextUsingFlashlightWalk != usingFlashlightWalk)
        {
            currentState = next;
            usingFlashlightWalk = nextUsingFlashlightWalk;
            frameIndex = 0;
            frameTimer = 0f;
        }
    }

    private void UpdateFrame()
    {
        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length == 0 || sr == null) return;

        bool loops = currentState != AnimState.Jump;
        float duration = CurrentFrameDuration();
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
            default: return idleFrames;
        }
    }

    private float CurrentFrameDuration()
    {
        switch (currentState)
        {
            case AnimState.Walk: return usingFlashlightWalk ? walkFlashlightFrameDuration : walkFrameDuration;
            case AnimState.Jump: return jumpFrameDuration;
            default: return idleFrameDuration;
        }
    }
}
