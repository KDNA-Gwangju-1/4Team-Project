using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the approved opening and hospital sequence before normal scene loading.</summary>
public sealed class OpeningCinematicPlayer : MonoBehaviour
{
    private VideoPlayer player;
    private GameObject overlay;
    private RenderTexture texture;
    private Action completed, failed;
    private bool finished;
    private bool blackout;

    public void Play(Action onComplete, Action onFailure, float volume)
    {
        completed = onComplete;
        failed = onFailure;
        overlay = new GameObject("Opening Cinematic", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var backdrop = new GameObject("Black Background", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(overlay.transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = Color.black;
        var picture = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        picture.transform.SetParent(backdrop.transform, false);
        Stretch(picture.GetComponent<RectTransform>());
        var fit = picture.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 16f / 9f;
        texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        var raw = picture.GetComponent<RawImage>();
        raw.texture = texture;
        raw.color = Color.clear;
        player = overlay.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.source = VideoSource.Url;
        player.url = Application.streamingAssetsPath + "/Cinematics/OpeningAndHospital.mp4";
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.waitForFirstFrame = false;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        var audio = overlay.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0;
        audio.volume = Mathf.Clamp01(volume);
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, audio);
        player.prepareCompleted += Prepared;
        player.loopPointReached += Ended;
        player.errorReceived += Error;
        player.Prepare();
        StartCoroutine(PreparationTimeout());
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    private IEnumerator PreparationTimeout()
    {
        yield return new WaitForSecondsRealtime(20);
        if (!finished && player != null && !player.isPrepared) Error(player, "Video preparation timed out.");
    }
    private void Prepared(VideoPlayer source)
    {
        if (finished) return;
        overlay.GetComponentInChildren<RawImage>().color = Color.white;
        source.Play();
    }
    private void Ended(VideoPlayer source)
    {
        Debug.Log("[OpeningCinematicPlayer] Completed; continuing to game scene.");
        Finish(true);
    }
    private void Error(VideoPlayer source, string message)
    {
        Debug.LogError("[OpeningCinematicPlayer] " + message);
        Finish(false);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;

        if (!success)
        {
            // Nothing else takes over the screen, so put the menu back.
            Cleanup();
            var onFailure = failed;
            Destroy(this);
            onFailure?.Invoke();
            return;
        }

        // Keep the black backdrop up and stop only the video.
        // Destroy() runs before rendering while SceneManager.LoadScene() runs at the end of
        // the frame, so tearing the overlay down here lets the menu render for one frame in
        // between - that is the "cinematic -> menu -> loading" flash.
        blackout = true;
        Cleanup();
        completed?.Invoke();

        // If the callback never swaps the scene (missing build entry, bad name) we would be
        // stuck on black, so undo the blackout and fall back to the menu.
        StartCoroutine(BlackoutSafety());
    }
    private IEnumerator BlackoutSafety()
    {
        yield return new WaitForSecondsRealtime(3);
        Debug.LogWarning("[OpeningCinematicPlayer] Scene never changed; restoring the menu.");
        blackout = false;
        Cleanup();
        var onFailure = failed;
        Destroy(this);
        onFailure?.Invoke();
    }
    private void Cleanup()
    {
        if (player != null)
        {
            player.prepareCompleted -= Prepared;
            player.loopPointReached -= Ended;
            player.errorReceived -= Error;
            player.Stop();
            player.targetTexture = null;
        }
        // While blacked out the overlay stays; the scene load takes it down with everything else.
        if (overlay != null)
        {
            var raw = overlay.GetComponentInChildren<RawImage>();
            if (raw != null) raw.color = Color.clear;
            if (!blackout) Destroy(overlay);
        }
        if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
    }
    private void OnDestroy() { Cleanup(); }
}
