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

    private GameObject canvasGO;
    private Text label;
    private float alpha;
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

    void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
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

        SetPromptVisible(inRange);

        if (!inRange) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[pickupKey].wasPressedThisFrame) return;

        taken = true;
        player.PickUpLantern();
        Destroy(gameObject);
    }

    private void SetPromptVisible(bool visible)
    {
        float target = visible ? 1f : 0f;
        if (visible) Build();
        if (label == null) return;

        alpha = (fadeDuration <= 0.01f)
            ? target
            : Mathf.MoveTowards(alpha, target, Time.deltaTime / fadeDuration);

        float shown = visible ? alpha * (1f - Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseDepth) : alpha;
        Color c = textColor;
        c.a = shown;
        label.color = c;
        label.enabled = shown > 0.01f;
    }

    private void Build()
    {
        if (canvasGO != null) return;

        canvasGO = new GameObject("PickupPromptCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(canvasGO.transform, false);

        label = textGO.AddComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = promptText;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, screenHeight01);
        rt.anchorMax = new Vector2(1f, screenHeight01);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = Vector2.zero;

        Color c = textColor;
        c.a = 0f;
        label.color = c;
    }
}
