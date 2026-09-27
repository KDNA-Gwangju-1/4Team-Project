using UnityEngine;

public class FlashlightBeamAnimator2D : MonoBehaviour
{
    public Sprite[] frames;
    public float frameDuration = 0.05f;
    public Vector2 targetSize = new Vector2(4.5f, 2.5f);

    private SpriteRenderer sr;
    private int frameIndex;
    private float frameTimer;
    private bool wasEnabled;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;

        if (!sr.enabled)
        {
            wasEnabled = false;
            return;
        }

        if (!wasEnabled)
        {
            wasEnabled = true;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        if (frameIndex < frames.Length - 1)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex++;
                ApplyFrame();
            }
        }
    }

    private void ApplyFrame()
    {
        Sprite frame = frames[frameIndex];
        sr.sprite = frame;

        float scaleX = targetSize.x / frame.bounds.size.x;
        float scaleY = targetSize.y / frame.bounds.size.y;
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}
