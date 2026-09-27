using UnityEngine;

// 항상 밟을 수 있지만 손전등 빛이 닿는 동안만 보이는 발판.
// 빛에 잡히는 판정은 몬스터와 같은 부채꼴 레이캐스트라, 손전등 범위·각도를 바꾸면 같이 바뀐다.
// 레이어는 씬에 둔 그대로(Ground) 쓴다 - 보이든 안 보이든 충돌은 늘 살아 있어야 한다.
public class LightRevealPlatform2D : MonoBehaviour
{
    private const int RayCount = 15;

    [Range(0f, 1f)] public float hiddenAlpha = 0f;
    [Range(0f, 1f)] public float litAlpha = 1f;
    public float fadeInTime = 0.5f;
    [Tooltip("빛이 벗어난 뒤 완전히 사라지기까지. 0이면 즉시.")]
    public float fadeOutTime = 0.6f;

    private SpriteRenderer sr;
    private Collider2D col;
    private float alpha;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        alpha = hiddenAlpha;
        Apply();
    }

    void Update()
    {
        if (sr == null || col == null) return;

        float target = IsCurrentlyLit() ? litAlpha : hiddenAlpha;
        float time = target > alpha ? fadeInTime : fadeOutTime;
        float span = Mathf.Max(0.0001f, litAlpha - hiddenAlpha);
        alpha = time <= 0f ? target : Mathf.MoveTowards(alpha, target, Time.deltaTime * span / time);
        Apply();
    }

    private void Apply()
    {
        sr.enabled = alpha > 0.001f;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private bool IsCurrentlyLit()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || !player.IsLightOn) return false;

        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        int layerMask = 1 << gameObject.layer;

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, player.lightRange, layerMask);
            if (hit.collider == col) return true;
        }

        return false;
    }
}
