using System.Collections;
using UnityEngine;

public class WatchingEye2D : MonoBehaviour
{
    public float minBlinkInterval = 2f;
    public float maxBlinkInterval = 6f;
    public float blinkDuration = 0.12f;
    public float eyeSize = 1f;

    void Start()
    {
        BuildEye();
        StartCoroutine(BlinkLoop());
    }

    private void BuildEye()
    {
        GameObject sclera = new GameObject("Sclera");
        sclera.transform.SetParent(transform, false);
        sclera.transform.localScale = new Vector3(eyeSize, eyeSize, 1f);
        SpriteRenderer scleraRenderer = sclera.AddComponent<SpriteRenderer>();
        scleraRenderer.sprite = CreateCircleSprite(64, Color.white);
        scleraRenderer.sortingOrder = -90;

        GameObject pupil = new GameObject("Pupil");
        pupil.transform.SetParent(transform, false);
        pupil.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        pupil.transform.localScale = new Vector3(eyeSize * 0.45f, eyeSize * 0.45f, 1f);
        SpriteRenderer pupilRenderer = pupil.AddComponent<SpriteRenderer>();
        pupilRenderer.sprite = CreateCircleSprite(64, Color.black);
        pupilRenderer.sortingOrder = -89;
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? color : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator BlinkLoop()
    {
        Vector3 openScale = transform.localScale;
        Vector3 closedScale = new Vector3(openScale.x, openScale.y * 0.05f, openScale.z);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkInterval, maxBlinkInterval));
            yield return ScaleOver(openScale, closedScale, blinkDuration);
            yield return ScaleOver(closedScale, openScale, blinkDuration);
        }
    }

    private IEnumerator ScaleOver(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        transform.localScale = to;
    }
}
