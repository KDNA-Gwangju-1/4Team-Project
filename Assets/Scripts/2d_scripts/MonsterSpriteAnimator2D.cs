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
    private bool isOneShot;

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
        if (isDissolving || isOneShot || sr == null || activeFrames == null || activeFrames.Length == 0) return;

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

    public IEnumerator PlayOneShotRoutine(Sprite[] frames, float frameDuration)
    {
        if (sr == null || frames == null || frames.Length == 0) yield break;

        isOneShot = true;
        for (int i = 0; i < frames.Length; i++)
        {
            sr.sprite = frames[i];
            yield return new WaitForSeconds(frameDuration);
        }
        isOneShot = false;
        frameIndex = 0;
        frameTimer = 0f;
    }

    // Hold one frame, overriding the idle loop until the loop is restored or
    // another frame is shown. Used for hand-timed beats like the claw swing,
    // where the collapse has to land on a specific frame.
    public void ShowFrame(Sprite frame)
    {
        if (sr == null || frame == null) return;
        isOneShot = true;
        sr.sprite = frame;
    }

    public void ReleaseFrame()
    {
        isOneShot = false;
    }

    public void SetLoopFrames(Sprite[] frames)
    {
        activeFrames = frames;
        isOneShot = false;
        ResetAnimation();
    }

    public void ResetAnimation()
    {
        frameIndex = 0;
        frameTimer = 0f;
        isDissolving = false;
        isOneShot = false;
        if (sr != null && activeFrames != null && activeFrames.Length > 0)
        {
            sr.sprite = activeFrames[0];
        }
    }
}
