using System.Collections;
using UnityEngine;

// Spawns telegraphed tentacle eruptions at fixed ground points.
public class TentacleStrikeField2D : MonoBehaviour
{
    public Boss2D boss;
    public Transform[] strikePoints;

    public Sprite[] tentacleFrames;
    public float bottomPad = 0.0234f;
    public Sprite warningSprite;

    public Vector2 tentacleSize = new Vector2(2.2f, 6f);
    public int sortingOrder = 5;
    public string sortingLayer = "Default";

    public Color warnColorA = new Color(0.05f, 0.05f, 0.05f, 0.85f);
    public Color warnColorB = new Color(1f, 0.15f, 0.15f, 0.95f);
    public float warnBlink = 0.18f;

    public float warnDuration = 1f;
    public float exposedTime = 3f;
    public float betweenStrikes = 0.25f;
    public bool waitForWaveToFinish = true;

    // tentacles must never spawn in front of the boss and hide it
    public Transform bossBody;
    public float bossClearance = 1.2f;

    private readonly System.Collections.Generic.List<int> busy = new System.Collections.Generic.List<int>();

    public bool HasPoints => strikePoints != null && strikePoints.Length > 0;

    public IEnumerator StrikeRoutine(Vector2 focus, int count, bool includeFocusPoint)
    {
        if (!HasPoints) yield break;

        int[] order = SortByDistance(focus);
        var wave = new System.Collections.Generic.List<TentacleStrike2D>();
        int spawned = 0;
        for (int i = 0; i < order.Length && spawned < count; i++)
        {
            int idx = order[i];
            if (busy.Contains(idx)) continue;
            if (i == 0 && !includeFocusPoint) continue;
            if (IsBlockedByBoss(strikePoints[idx])) continue;

            wave.Add(Spawn(idx));
            spawned++;
            if (spawned < count) yield return new WaitForSeconds(betweenStrikes);
        }

        // a wave has to resolve before the boss throws the next one, otherwise
        // strikes pile on top of each other and the fight loses its rhythm
        if (!waitForWaveToFinish) yield break;
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

    private bool IsBlockedByBoss(Transform point)
    {
        if (point == null) return true;
        if (bossBody == null) return false;

        float bossHalfWidth = 0f;
        SpriteRenderer bossRenderer = bossBody.GetComponent<SpriteRenderer>();
        if (bossRenderer != null) bossHalfWidth = bossRenderer.bounds.size.x * 0.5f;

        float forbidden = bossHalfWidth + bossClearance + tentacleSize.x * 0.5f;
        return Mathf.Abs(point.position.x - bossBody.position.x) < forbidden;
    }

    private TentacleStrike2D Spawn(int index)
    {
        Transform point = strikePoints[index];
        if (point == null) return null;

        GameObject go = new GameObject("TentacleStrike");
        go.transform.position = point.position;
        TentacleStrike2D strike = go.AddComponent<TentacleStrike2D>();
        strike.boss = boss;
        strike.warnDuration = warnDuration;
        strike.exposedTime = exposedTime;
        strike.Build(tentacleFrames, warningSprite, tentacleSize, bottomPad, sortingOrder, sortingLayer);

        busy.Add(index);
        StartCoroutine(RunStrike(strike, index));
        return strike;
    }

    private IEnumerator RunStrike(TentacleStrike2D strike, int index)
    {
        yield return strike.StrikeRoutine(warnColorA, warnColorB, warnBlink);
        busy.Remove(index);
    }

    private int[] SortByDistance(Vector2 focus)
    {
        int n = strikePoints.Length;
        int[] idx = new int[n];
        float[] dist = new float[n];
        for (int i = 0; i < n; i++)
        {
            idx[i] = i;
            dist[i] = strikePoints[i] != null
                ? Mathf.Abs(strikePoints[i].position.x - focus.x)
                : float.MaxValue;
        }
        for (int i = 1; i < n; i++)
        {
            float d = dist[i];
            int v = idx[i];
            int j = i - 1;
            while (j >= 0 && dist[j] > d)
            {
                dist[j + 1] = dist[j];
                idx[j + 1] = idx[j];
                j--;
            }
            dist[j + 1] = d;
            idx[j + 1] = v;
        }
        return idx;
    }
}
