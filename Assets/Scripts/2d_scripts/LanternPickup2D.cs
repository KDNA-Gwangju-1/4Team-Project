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
    public Key pickupKey = Key.E;

    [Header("Prompt")]
    [TextArea] public string promptText = "E키를 눌러서 줍기";
    [Tooltip("Left empty, the prompt uses its own default face.")]
    public Font font;

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
