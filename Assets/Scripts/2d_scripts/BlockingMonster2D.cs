using UnityEngine;

public class BlockingMonster2D : MonoBehaviour
{
    private const int RayCount = 15;
    private const float SkinMargin = 1.1f;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (col == null) return;
        if (IsCurrentlyLit())
        {
            Destroy(gameObject);
        }
    }

    private bool IsCurrentlyLit()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || !player.IsLightOn) return false;

        Vector2 origin = player.LightOrigin;
        Vector2 baseDir = player.LightDirection;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < RayCount; i++)
        {
            float t = (RayCount == 1) ? 0f : (i / (float)(RayCount - 1)) * 2f - 1f;
            float angle = (baseAngle + t * player.lightHalfAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 rayOrigin = origin + dir * SkinMargin;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, player.lightRange);
            if (hit.collider == col) return true;
        }

        return false;
    }
}
