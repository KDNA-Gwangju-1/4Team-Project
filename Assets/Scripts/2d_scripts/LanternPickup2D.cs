using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The flashlight lying on the floor. Walking over it is not enough any more -
// the player has to stop and take it, so the moment registers as picking
// something up rather than as passing through a trigger.
public class LanternPickup2D : MonoBehaviour
{
    [Tooltip("How close the player has to be before the prompt appears.")]
    public float pickupRange = 2.6f;
    public Key pickupKey = Key.Z;

    [Header("Prompt")]
    [TextArea] public string promptText = "Z키를 눌러서 줍기";
    public Font font;
    public int fontSize = 30;
    public Color textColor = new Color(1f, 0.95f, 0.75f);
    [Tooltip("Height on screen, 0 = bottom, 1 = top.")]
    [Range(0f, 1f)] public float screenHeight01 = 0.32f;
    public float fadeDuration = 0.15f;
    [Tooltip("Slow pulse, so it reads as something to act on.")]
    public float pulseSpeed = 3f;
    public float pulseDepth = 0.2f;

    [Header("Idle motion")]
    [Tooltip("Gentle bob while it waits to be found.")]
    public float bobAmplitude = 0.12f;
    public float bobPeriod = 2.2f;

    private InteractPrompt2D prompt;
    private float baseY;
    private bool taken;

    void Awake()
    {
        if (PlayerMovement2D.LanternObtained)
        {
            Destroy(gameObject);
            return;
        }
        baseY = transform.position.y;
    }

    void Update()
    {
        if (taken) return;

        // it drifts a little so the eye finds it on a dark floor
        if (bobPeriod > 0.01f && bobAmplitude > 0f)
        {
            Vector3 p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time / bobPeriod * Mathf.PI * 2f) * bobAmplitude;
            transform.position = p;
        }

        PlayerMovement2D player = PlayerMovement2D.Instance;
        bool inRange = player != null
            && Vector2.Distance(player.transform.position, transform.position) <= pickupRange;

        if (prompt == null && inRange)
        {
            prompt = gameObject.AddComponent<InteractPrompt2D>();
            prompt.font = font;
            prompt.fontSize = fontSize;
            prompt.textColor = textColor;
            prompt.screenHeight01 = screenHeight01;
            prompt.fadeDuration = fadeDuration;
            prompt.pulseSpeed = pulseSpeed;
            prompt.pulseDepth = pulseDepth;
        }
        if (prompt != null) prompt.SetVisible(inRange, promptText);

        if (!inRange) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[pickupKey].wasPressedThisFrame) return;

        taken = true;
        player.PickUpLantern();
        Destroy(gameObject);
    }
}
