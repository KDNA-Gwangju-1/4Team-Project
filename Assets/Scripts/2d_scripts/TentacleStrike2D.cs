using System.Collections;
using UnityEngine;

// One telegraphed tentacle eruption. The emergence is driven entirely by the
// 8-frame art (the base stays planted on the ground in every frame), never by scaling.
//   frames 1-3 : bursts up out of the floor, then holds on 3
//   frames 4-8 : the final swipe before it sinks back down
//   frames 3-1 : played backwards to retract into the ground
public class TentacleStrike2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    public Boss2D boss;
    public int damageToBoss = 1;
    public int damageToPlayer = 1;

    // wave tentacles are pure boss attack: they erupt, threaten, and sink again
    // without ever opening up, so they cannot be shot for damage
    public bool isWeakPoint = true;

    [Tooltip("Hit box width as a fraction of the drawn width - lets the art grow without making the attack undodgeable.")]
    public float hitboxWidthFraction = 0.5f;
    public float hitboxHeightFraction = 0.8f;

    public float warnDuration = 1f;
    public float eruptFrameDuration = 0.07f;
    public float holdTime = 0.2f;
    public float exposedTime = 3f;
    public float swipeFrameDuration = 0.07f;
    public float retractFrameDuration = 0.06f;

    public int eruptFrameStart = 0;
    public int eruptFrameEnd = 2;
    public int swipeFrameStart = 3;
    public int swipeFrameEnd = 7;

    [Header("Hurt reaction")]
    public Color hurtFlashColor = new Color(1f, 0.25f, 0.25f);
    public int hurtFlashes = 3;
    public float hurtDuration = 0.42f;
    public float hurtShake = 0.22f;
    public float hurtRetractSpeedup = 1.9f;

    public Color silhouetteColor = Color.black;
    public Color silhouetteOutlineColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public float silhouetteOutlineScale = 1.06f;

    private Sprite[] frames;
    private SpriteRenderer sr;
    private SpriteRenderer silhouetteRenderer;
    private SpriteRenderer outlineRenderer;
    private BoxCollider2D hitCol;
    private SpriteRenderer warningRenderer;
    private Transform spriteTransform;
    private Vector3 spriteBaseLocalPos;

    private int raycastMask;
    private bool dangerous;
    private bool vulnerable;
    private bool killed;
    private bool lit;
    private bool bodyShown;

    // the mask decides what is DRAWN; this decides what can be SHOT
    public bool IsRevealed => lit;
    public bool CanBeKilled => vulnerable && !killed && IsRevealed;

    public void Build(Sprite[] tentacleFrames, Sprite warningSprite, Vector2 size, float bottomPad, int sortingOrder, string sortingLayer)
    {
        frames = tentacleFrames;
        raycastMask = ~(1 << LayerMask.NameToLayer("Bullet"));

        GameObject warning = new GameObject("Warning");
        warning.transform.SetParent(transform, false);
        warning.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        warning.transform.localScale = new Vector3(size.x * 0.75f, 0.3f, 1f);
        warningRenderer = warning.AddComponent<SpriteRenderer>();
        warningRenderer.sprite = warningSprite;
        warningRenderer.sortingLayerName = sortingLayer;
        warningRenderer.sortingOrder = sortingOrder + 1;
        warningRenderer.enabled = false;

        GameObject spriteGO = new GameObject("Sprite");
        spriteGO.transform.SetParent(transform, false);
        sr = spriteGO.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;
        sr.enabled = false;
        // real body only draws where the flashlight mask covers it
        sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // the sprite pivot sits at the bottom edge of the frame; sink it by the
        // empty margin baked into the art so the drawn base lands on the ground
        float nw = sr.sprite.bounds.size.x;
        float nh = sr.sprite.bounds.size.y;
        spriteGO.transform.localScale = new Vector3(size.x / nw, size.y / nh, 1f);
        spriteGO.transform.localPosition = new Vector3(0f, -size.y * bottomPad, 0f);

        spriteTransform = spriteGO.transform;
        spriteBaseLocalPos = spriteGO.transform.localPosition;

        outlineRenderer = CreateLayer(spriteGO.transform, "Outline", silhouetteOutlineColor, silhouetteOutlineScale, sortingOrder - 1, sortingLayer);
        silhouetteRenderer = CreateLayer(spriteGO.transform, "Silhouette", silhouetteColor, 1f, sortingOrder, sortingLayer);
        // ...and the silhouette covers everything the beam does not
        outlineRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        silhouetteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        hitCol = gameObject.AddComponent<BoxCollider2D>();
        hitCol.isTrigger = true;
        hitCol.size = new Vector2(size.x * hitboxWidthFraction, size.y * hitboxHeightFraction);
        hitCol.offset = new Vector2(0f, size.y * hitboxHeightFraction * 0.5f);
        hitCol.enabled = false;
    }

    private SpriteRenderer CreateLayer(Transform parent, string layerName, Color color, float scale, int order, string sortingLayer)
    {
        GameObject go = new GameObject(layerName);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.color = color;
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;
        r.enabled = false;
        return r;
    }

    private void SetFrame(int index)
    {
        Sprite s = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        sr.sprite = s;
        silhouetteRenderer.sprite = s;
        outlineRenderer.sprite = s;
    }

    private IEnumerator PlayFrames(int from, int to, float frameDuration)
    {
        int step = (to >= from) ? 1 : -1;
        for (int i = from; ; i += step)
        {
            SetFrame(i);
            yield return new WaitForSeconds(frameDuration);
            if (i == to) break;
        }
    }

    public IEnumerator StrikeRoutine(Color warnColorA, Color warnColorB, float warnBlink)
    {
        // --- telegraph on the ground ---
        warningRenderer.enabled = true;
        float elapsed = 0f;
        while (elapsed < warnDuration)
        {
            float k = Mathf.PingPong(elapsed / Mathf.Max(0.01f, warnBlink), 1f);
            warningRenderer.color = Color.Lerp(warnColorA, warnColorB, k);
            elapsed += Time.deltaTime;
            yield return null;
        }
        warningRenderer.enabled = false;

        // --- erupt: the art does the rising, the base never leaves the floor ---
        // body and silhouette both stay on from here; the mask splits them
        bodyShown = true;
        sr.enabled = true;
        silhouetteRenderer.enabled = true;
        outlineRenderer.enabled = true;
        SetFrame(eruptFrameStart);
        dangerous = true;
        hitCol.enabled = true;
        yield return PlayFrames(eruptFrameStart, eruptFrameEnd, eruptFrameDuration);
        yield return new WaitForSeconds(holdTime);

        if (isWeakPoint)
        {
            // --- vulnerable: holds on the fully risen frame, shootable once lit ---
            dangerous = false;
            vulnerable = true;
            elapsed = 0f;
            while (elapsed < exposedTime && !killed)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            vulnerable = false;

            // --- one last swipe before it sinks back down ---
            if (!killed)
            {
                dangerous = true;
                yield return PlayFrames(swipeFrameStart, swipeFrameEnd, swipeFrameDuration);
                dangerous = false;
            }
        }
        else
        {
            // wave member: stays dangerous for its whole short life, then goes
            dangerous = false;
        }

        // --- shot down: flinch before it goes, so the kill lands ---
        hitCol.enabled = false;
        if (killed) yield return HurtRoutine();

        // --- retract: the emergence frames, played backwards into the ground ---
        float retractStep = killed ? retractFrameDuration / hurtRetractSpeedup : retractFrameDuration;
        yield return PlayFrames(eruptFrameEnd, eruptFrameStart, retractStep);

        Destroy(gameObject);
    }

    // Writhes, flashes red and recoils instead of blinking out of existence.
    private IEnumerator HurtRoutine()
    {
        // show the real thing, not the silhouette, so the hit is legible even
        // if the player swings the flashlight away on the shot
        sr.enabled = true;
        sr.maskInteraction = SpriteMaskInteraction.None;   // whole body, beam or not
        silhouetteRenderer.enabled = false;
        outlineRenderer.enabled = false;
        bodyShown = false;

        float elapsed = 0f;
        int frame = swipeFrameStart;
        float frameTimer = 0f;
        float writheStep = hurtDuration / Mathf.Max(1, (swipeFrameEnd - swipeFrameStart + 1));

        while (elapsed < hurtDuration)
        {
            float t = elapsed / hurtDuration;
            float decay = 1f - t;

            // recoil shake, strongest at the moment of the hit
            spriteTransform.localPosition = spriteBaseLocalPos
                + new Vector3(Random.Range(-1f, 1f) * hurtShake * decay,
                              Random.Range(-0.5f, 0.5f) * hurtShake * decay, 0f);

            // flash red on and off
            float blink = Mathf.Repeat(t * hurtFlashes * 2f, 2f);
            sr.color = blink < 1f ? hurtFlashColor : Color.white;

            // thrash through the swipe frames while it hurts
            frameTimer += Time.deltaTime;
            if (frameTimer >= writheStep)
            {
                frameTimer -= writheStep;
                frame = (frame >= swipeFrameEnd) ? swipeFrameStart : frame + 1;
                SetFrame(frame);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteTransform.localPosition = spriteBaseLocalPos;
        sr.color = Color.white;
    }

    void Update()
    {
        if (hitCol == null) return;
        var player = PlayerMovement2D.Instance;
        lit = player != null && player.IsLightOn && hitCol.enabled && IsLit(player);
    }

    private bool IsLit(PlayerMovement2D player)
    {
        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float rayDistance = Mathf.Max(0f, player.lightRange - SkinMargin);

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 rayOrigin = origin + dir * SkinMargin;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, rayDistance, raycastMask);
            if (hit.collider == hitCol) return true;
        }
        return false;
    }

    public void Kill()
    {
        if (killed || !vulnerable) return;
        killed = true;
        if (boss != null) boss.TakeDamage(damageToBoss);
    }

    void OnTriggerEnter2D(Collider2D other) { TryHurt(other); }
    void OnTriggerStay2D(Collider2D other) { TryHurt(other); }

    private void TryHurt(Collider2D other)
    {
        if (!dangerous) return;
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        if (player != null) player.TakeDamage(damageToPlayer);
    }
}
