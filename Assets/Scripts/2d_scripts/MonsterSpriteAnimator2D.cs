using System.Collections;
using UnityEngine;

public class MonsterSpriteAnimator2D : MonoBehaviour
{
    public Sprite[] activeFrames;
    public float activeFrameDuration = 0.11f;

    public Sprite[] dissolveFrames;
    public float dissolveFrameDuration = 0.13f;

    private SpriteRenderer sr;
    private int frameIndex;
    private float frameTimer;
    private bool isDissolving;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && activeFrames != null && activeFrames.Length > 0)
        {
            sr.sprite = activeFrames[0];
        }
    }

    void Update()
    {
        if (isDissolving || sr == null || activeFrames == null || activeFrames.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= activeFrameDuration)
        {
            frameTimer -= activeFrameDuration;
            frameIndex = (frameIndex + 1) % activeFrames.Length;
        }
        sr.sprite = activeFrames[frameIndex];
    }

    public IEnumerator PlayDissolveRoutine()
    {
        if (sr == null || dissolveFrames == null || dissolveFrames.Length == 0) yield break;

        isDissolving = true;
        for (int i = 0; i < dissolveFrames.Length; i++)
        {
            sr.sprite = dissolveFrames[i];
            yield return new WaitForSeconds(dissolveFrameDuration);
        }
    }

    public void ResetAnimation()
    {
        frameIndex = 0;
        frameTimer = 0f;
        isDissolving = false;
        if (sr != null && activeFrames != null && activeFrames.Length > 0)
        {
            sr.sprite = activeFrames[0];
        }
    }
}
