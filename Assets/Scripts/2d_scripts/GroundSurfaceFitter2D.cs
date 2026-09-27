using UnityEngine;

// Keeps the walkable collider glued to the floor artwork.
//
// The floor sprite's bounding box is not its visible surface: FloorLarge only
// becomes solid about 5% below the top of its frame, and the drawn edge is wavy.
// Lining the collider up with the raw bounds leaves everything perched above the
// sand, which is why moving the art by hand kept breaking the ground.
[ExecuteAlways]
public class GroundSurfaceFitter2D : MonoBehaviour
{
    public SpriteRenderer floorArt;
    public BoxCollider2D floorCollider;

    [Tooltip("How far below the sprite's top edge the drawing actually becomes solid, as a fraction of its height.")]
    public float surfaceInset = 0.05f;
    [Tooltip("Keeps the player from standing on the very lip of the island.")]
    public float edgeInset = 0.6f;
    public bool matchWidth = true;
    public bool runInEditMode = true;

    public float SurfaceY
    {
        get
        {
            if (floorArt == null) return 0f;
            return floorArt.bounds.max.y - floorArt.bounds.size.y * surfaceInset;
        }
    }

    void LateUpdate()
    {
        if (Application.isPlaying) return;
        if (!runInEditMode) return;
        Fit();
    }

    public void Fit()
    {
        if (floorArt == null || floorCollider == null) return;

        Bounds art = floorArt.bounds;
        float targetTop = SurfaceY;

        float top = floorCollider.bounds.max.y;
        if (!Mathf.Approximately(top, targetTop))
        {
            float delta = targetTop - top;
            floorCollider.offset = new Vector2(floorCollider.offset.x, floorCollider.offset.y + delta / transform.lossyScale.y);
        }

        if (!matchWidth) return;

        float left = art.min.x + edgeInset;
        float right = art.max.x - edgeInset;
        float width = Mathf.Max(0.1f, right - left);
        float centre = (left + right) * 0.5f;

        Vector2 size = floorCollider.size;
        if (!Mathf.Approximately(size.x, width))
        {
            floorCollider.size = new Vector2(width, size.y);
        }

        float wantOffsetX = (centre - floorCollider.transform.position.x) / floorCollider.transform.lossyScale.x;
        if (!Mathf.Approximately(floorCollider.offset.x, wantOffsetX))
        {
            floorCollider.offset = new Vector2(wantOffsetX, floorCollider.offset.y);
        }
    }

    [ContextMenu("Snap actors to ground")]
    public void SnapActors()
    {
        Fit();
        float y = floorCollider != null ? floorCollider.bounds.max.y : SurfaceY;

        // the boss sprite is pivoted at its feet, so it sits straight on the line
        Boss2D boss = Object.FindFirstObjectByType<Boss2D>();
        if (boss != null)
        {
            boss.transform.position = new Vector3(boss.transform.position.x, y, boss.transform.position.z);

            TentacleStrikeField2D field = boss.GetComponent<TentacleStrikeField2D>();
            if (field != null)
            {
                field.fallbackGroundY = y;
                if (floorCollider != null)
                {
                    field.arenaMinX = floorCollider.bounds.min.x + 2f;
                    field.arenaMaxX = floorCollider.bounds.max.x - 2f;
                }
            }
        }

        PlayerMovement2D player = Object.FindFirstObjectByType<PlayerMovement2D>();
        if (player != null)
        {
            Collider2D col = player.GetComponent<Collider2D>();
            float feetOffset = col != null ? (player.transform.position.y - col.bounds.min.y) : 1f;
            player.transform.position = new Vector3(player.transform.position.x, y + feetOffset + 0.05f, player.transform.position.z);
        }

        CameraFollow2D follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        if (follow != null && floorCollider != null)
        {
            follow.clampToBounds = true;
            follow.minX = floorCollider.bounds.min.x;
            follow.maxX = floorCollider.bounds.max.x;
        }
    }
}
