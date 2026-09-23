using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.1f;
    public float gravityScale = 0.5f;

    private Rigidbody rb;
    private bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && IsGrounded())
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        float moveInput = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) moveInput -= 1f;
            if (keyboard.dKey.isPressed) moveInput += 1f;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;

        if (jumpRequested)
        {
            velocity.y = jumpForce;
            jumpRequested = false;
        }

        rb.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }
}
