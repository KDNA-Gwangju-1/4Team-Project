using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TentaclePattern
{
    Single,   // one, right where the player is standing
    Chase,    // several in a row, each aimed at wherever the player is by then
    Sweep,    // a rolling wave marching outward from the player
    Pincer,   // one on each side, leaving a safe gap in the middle
    Domino    // a long line rolling across the arena, slow enough to outrun
}

// Spawns telegraphed tentacle eruptions along the arena floor.
// Strike positions are generated from the arena bounds rather than placed by
// hand, so the spacing is one number instead of a pile of scene objects.
public class TentacleStrikeField2D : MonoBehaviour
{
    public Boss2D boss;
    public Transform bossBody;
    public float bossClearance = 0.6f;

    public float arenaMinX = 19f;
    public float arenaMaxX = 48f;
    public float pointSpacing = 1.8f;

    // a tentacle always erupts from a surface, never from mid-air where the
    // player happens to be, so the spawn height is traced down to the floor
    public LayerMask groundLayer;
    public float fallbackGroundY = -0.41f;
    public float groundProbeUp = 1.5f;
    public float groundProbeDistance = 40f;

    public Sprite[] tentacleFrames;
    public Sprite warningSprite;
    public Vector2 tentacleSize = new Vector2(4f, 9f);
    public float bottomPad = 0.0234f;
    [Tooltip("Hit box width as a fraction of the drawn width.")]
    public float hitboxWidthFraction = 0.5f;
    public int sortingOrder = 6;
    public string sortingLayer = "Default";

    public Color warnColorA = new Color(0.05f, 0.05f, 0.05f, 0.85f);
    public Color warnColorB = new Color(1f, 0.15f, 0.15f, 0.95f);
    public float warnBlink = 0.18f;
    public float warnDuration = 1f;
    public float exposedTime = 3f;

    public bool waitForWaveToFinish = true;
    public float chaseDelay = 0.55f;
    public float sweepDelay = 0.3f;
    public float pincerGap = 5.5f;
    public float minSeparation = 4.5f;

    // domino: spacing / delay is the wave's travel speed. it has to stay well
    // under the player's run speed (5) or the attack is undodgeable.
    public float dominoSpacing = 2.5f;
    public float dominoDelay = 0.7f;
    public bool dominoRollsTowardPlayer = true;
    public float dominoWarnDuration = 0.55f;
    public float dominoHoldTime = 0.35f;

    // A wave is a commitment, not a reflex test. Marking each tentacle just before
    // it rises tells the player nothing about where the wave is going, so the whole
    // path lights up first and holds long enough to read and act on.
    [Tooltip("How far the wave travels. 0 = the whole arena, which marks the entire floor and tells the player nothing.")]
    public float dominoSpan = 0f;
    [Tooltip("Light the whole path before the wave rolls, instead of one spot at a time.")]
    public bool dominoWarnsWholePath = false;
    [Tooltip("How long that full-path warning holds. Longer than the per-tentacle one on purpose.")]
    public float dominoPathWarnDuration = 1.6f;
    public float dominoPathWarnHeight = 0.5f;
    [Tooltip("Per-tentacle warning once the path warning has played. Kept short so the wave stays quick.")]
    public float dominoWarnAfterPath = 0.12f;

    private readonly List<float> points = new List<float>();
    private readonly List<TentacleStrike2D> live = new List<TentacleStrike2D>();

    public bool HasPoints
    {
        get
        {
            EnsurePoints();
            return points.Count > 0;
        }
    }

    private void EnsurePoints()
    {
        if (points.Count > 0) return;

        float step = Mathf.Max(0.5f, pointSpacing);
        for (float x = arenaMinX; x <= arenaMaxX + 0.001f; x += step)
        {
            points.Add(x);
        }
    }

    // ---------- availability ----------

    private float ForbiddenRadius()
    {
        if (bossBody == null) return 0f;

        float bossHalfWidth = 0f;
        SpriteRenderer bossRenderer = bossBody.GetComponent<SpriteRenderer>();
        if (bossRenderer != null) bossHalfWidth = bossRenderer.bounds.size.x * 0.5f;

        return bossHalfWidth + bossClearance + tentacleSize.x * 0.5f;
    }

    private bool IsUsable(float x)
    {
        return IsUsable(x, minSeparation);
    }

    private bool IsUsable(float x, float separation)
    {
        if (bossBody != null && Mathf.Abs(x - bossBody.position.x) < ForbiddenRadius()) return false;

        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (live[i] == null) { live.RemoveAt(i); continue; }
            if (Mathf.Abs(live[i].transform.position.x - x) < separation) return false;
        }
        return true;
    }

    // Traces down from just above the player's feet to whatever surface is
    // under this column - main floor, raised ledge, floating platform.
    private float GroundYAt(float x, float fromY)
    {
        if (groundLayer.value == 0) return fallbackGroundY;

        Vector2 origin = new Vector2(x, fromY + groundProbeUp);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundProbeDistance, groundLayer);
        return hit.collider != null ? hit.point.y : fallbackGroundY;
    }

    // Closest usable spot to a world x, searching outward so a tentacle always
    // lands as near the player as the boss silhouette allows.
    private bool TryGetNearest(float targetX, out float chosen, float preferDirection)
    {
        EnsurePoints();
        chosen = targetX;

        float best = 0f;
        bool found = false;
        for (int i = 0; i < points.Count; i++)
        {
            float x = points[i];
            if (!IsUsable(x)) continue;

            float distance = Mathf.Abs(x - targetX);
            // a direction preference only breaks ties, it never overrides distance
            if (preferDirection != 0f && Mathf.Sign(x - targetX) != Mathf.Sign(preferDirection)) distance += 0.75f;

            if (!found || distance < best)
            {
                best = distance;
                chosen = x;
                found = true;
            }
        }
        return found;
    }

    // ---------- patterns ----------

    public IEnumerator RunPattern(TentaclePattern pattern, int count)
    {
        EnsurePoints();
        var wave = new List<TentacleStrike2D>();

        switch (pattern)
        {
            case TentaclePattern.Chase:
                yield return ChaseRoutine(count, wave);
                break;
            case TentaclePattern.Sweep:
                yield return SweepRoutine(count, wave);
                break;
            case TentaclePattern.Pincer:
                yield return PincerRoutine(wave);
                break;
            case TentaclePattern.Domino:
                yield return DominoRoutine(wave);
                break;
            default:
                yield return SingleRoutine(wave);
                break;
        }

        if (!waitForWaveToFinish) yield break;
        yield return WaitForWave(wave);
    }

    private float PlayerX()
    {
        var player = PlayerMovement2D.Instance;
        return player != null ? player.transform.position.x : (arenaMinX + arenaMaxX) * 0.5f;
    }

    private IEnumerator SingleRoutine(List<TentacleStrike2D> wave)
    {
        float x;
        if (TryGetNearest(PlayerX(), out x, 0f)) wave.Add(Spawn(x));
        yield break;
    }

    // Each one is aimed at where the player is at that moment, so standing
    // still is punished and the player is kept moving across the arena.
    private IEnumerator ChaseRoutine(int count, List<TentacleStrike2D> wave)
    {
        for (int i = 0; i < count; i++)
        {
            float x;
            if (TryGetNearest(PlayerX(), out x, 0f)) wave.Add(Spawn(x));
            if (i < count - 1) yield return new WaitForSeconds(chaseDelay);
        }
    }

    // A rolling line of eruptions marching away from the player's side,
    // readable at a glance and easy to outrun if they commit early.
    private IEnumerator SweepRoutine(int count, List<TentacleStrike2D> wave)
    {
        float playerX = PlayerX();
        float direction = (bossBody != null && playerX < bossBody.position.x) ? -1f : 1f;

        float cursor = playerX;
        for (int i = 0; i < count; i++)
        {
            float x;
            if (!TryGetNearest(cursor, out x, direction)) break;

            wave.Add(Spawn(x));
            cursor = x + direction * minSeparation;
            if (i < count - 1) yield return new WaitForSeconds(sweepDelay);
        }
    }

    // Both sides at once with a gap in the middle: the answer is to hold
    // position instead of running, the opposite lesson from Chase.
    private IEnumerator PincerRoutine(List<TentacleStrike2D> wave)
    {
        float playerX = PlayerX();
        float left, right;
        if (TryGetNearest(playerX - pincerGap, out left, -1f)) wave.Add(Spawn(left));
        if (TryGetNearest(playerX + pincerGap, out right, 1f)) wave.Add(Spawn(right));
        yield break;
    }

    // Starts at the far end and rolls across the whole arena one tentacle at a
    // time. The player sees it coming and has to commit to crossing early -
    // the boss's own no-spawn zone doubles as the mid-arena safe pocket.
    private IEnumerator DominoRoutine(List<TentacleStrike2D> wave)
    {
        float playerX = PlayerX();
        float middle = (arenaMinX + arenaMaxX) * 0.5f;

        // roll in from the side the player is NOT on, so it travels toward them
        float direction = (playerX >= middle) ? -1f : 1f;
        if (!dominoRollsTowardPlayer) direction = -direction;
        float cursor = (direction > 0f) ? arenaMinX : arenaMaxX;

        // A wave across the whole arena lights the entire floor, which reads as
        // "everywhere is dangerous" and cannot be acted on. Limit it to a stretch
        // that starts far enough out to be seen coming and still reaches the player.
        float spanLimit = dominoSpan;
        if (spanLimit > 0f)
        {
            float startX = Mathf.Clamp(playerX - direction * spanLimit, arenaMinX, arenaMaxX);
            cursor = startX;
        }

        float spacing = Mathf.Max(0.5f, dominoSpacing);
        int guard = Mathf.CeilToInt(Mathf.Abs(arenaMaxX - arenaMinX) / spacing) + 2;

        float perTentacleWarn = dominoWarnDuration;
        if (dominoWarnsWholePath)
        {
            yield return PathWarningRoutine(cursor, direction, spacing, guard, spanLimit);
            perTentacleWarn = dominoWarnAfterPath;
        }

        float travelled = 0f;
        for (int i = 0; i < guard; i++)
        {
            if (cursor < arenaMinX - 0.01f || cursor > arenaMaxX + 0.01f) break;
            if (spanLimit > 0f && travelled > spanLimit) break;

            // adjacent on purpose here, so the usual separation rule is relaxed
            if (IsUsable(cursor, spacing * 0.9f))
            {
                wave.Add(Spawn(cursor, false, perTentacleWarn, dominoHoldTime));
                yield return new WaitForSeconds(dominoDelay);
            }
            cursor += direction * spacing;
            travelled += spacing;
        }
    }

    // One strip across every column the wave will hit.
    private IEnumerator PathWarningRoutine(float startX, float direction, float spacing, int guard, float spanLimit)
    {
        float lo = startX, hi = startX;
        float cursor = startX;
        float walked = 0f;
        for (int i = 0; i < guard; i++)
        {
            if (cursor < arenaMinX - 0.01f || cursor > arenaMaxX + 0.01f) break;
            if (spanLimit > 0f && walked > spanLimit) break;
            if (IsUsable(cursor, spacing * 0.9f))
            {
                lo = Mathf.Min(lo, cursor);
                hi = Mathf.Max(hi, cursor);
            }
            cursor += direction * spacing;
            walked += spacing;
        }
        if (hi - lo < 0.5f) yield break;

        var player = PlayerMovement2D.Instance;
        float fromY = player != null ? player.transform.position.y : fallbackGroundY;
        float y = GroundYAt((lo + hi) * 0.5f, fromY);

        GameObject go = new GameObject("TentacleWaveWarning");
        go.transform.position = new Vector3((lo + hi) * 0.5f, y + 0.06f, 0f);
        go.transform.localScale = new Vector3((hi - lo) + tentacleSize.x, dominoPathWarnHeight, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = warningSprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder + 1;

        float elapsed = 0f;
        while (elapsed < dominoPathWarnDuration)
        {
            float k = Mathf.PingPong(elapsed / Mathf.Max(0.01f, warnBlink), 1f);
            sr.color = Color.Lerp(warnColorA, warnColorB, k);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator WaitForWave(List<TentacleStrike2D> wave)
    {
        bool alive = true;
        while (alive)
        {
            alive = false;
            for (int i = 0; i < wave.Count; i++)
            {
                if (wave[i] != null) { alive = true; break; }
            }
            if (alive) yield return null;
        }
    }

    // ---------- spawning ----------

    private TentacleStrike2D Spawn(float x)
    {
        return Spawn(x, true, warnDuration, -1f);
    }

    private TentacleStrike2D Spawn(float x, bool weakPoint, float warn, float hold)
    {
        var player = PlayerMovement2D.Instance;
        float fromY = player != null ? player.transform.position.y : fallbackGroundY;

        GameObject go = new GameObject("TentacleStrike");
        go.transform.position = new Vector3(x, GroundYAt(x, fromY), 0f);

        TentacleStrike2D strike = go.AddComponent<TentacleStrike2D>();
        strike.boss = boss;
        strike.isWeakPoint = weakPoint;
        strike.hitboxWidthFraction = hitboxWidthFraction;
        strike.warnDuration = warn;
        strike.exposedTime = exposedTime;
        if (hold >= 0f) strike.holdTime = hold;
        strike.Build(tentacleFrames, warningSprite, tentacleSize, bottomPad, sortingOrder, sortingLayer);

        live.Add(strike);
        StartCoroutine(strike.StrikeRoutine(warnColorA, warnColorB, warnBlink));
        return strike;
    }
}
