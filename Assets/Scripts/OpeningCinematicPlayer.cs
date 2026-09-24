using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the approved opening and hospital sequence before normal scene loading.</summary>
public sealed class OpeningCinematicPlayer : MonoBehaviour
{
    private const float SkipHoldSeconds = 2f;
    private const string SkipSpritePath = "UI/ReDreamSkip";

    private VideoPlayer player;
    private GameObject overlay;
    private RenderTexture texture;
    private Action completed, failed;
    private bool finished;
    private bool blackout;
    private GameObject skipPrompt;
    private Image skipFill;
    private float skipHeldSeconds;
    private bool skipReady;
    private bool skipArmed;

    public void Play(Action onComplete, Action onFailure, float volume)
    {
        completed = onComplete;
        failed = onFailure;
        overlay = new GameObject("Opening Cinematic", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
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
        CreateSkipPrompt();
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

    private void CreateSkipPrompt()
    {
        var sprite = Resources.Load<Sprite>(SkipSpritePath);
        skipPrompt = new GameObject("Hold ESC to Skip", typeof(RectTransform));
        var root = skipPrompt.GetComponent<RectTransform>();
        root.SetParent(overlay.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-40f, 40f);
        root.sizeDelta = new Vector2(180f, 180f);

        var baseImage = CreateSkipImage(root, "Skip Icon", sprite);
        baseImage.color = new Color(.65f, .65f, .65f, .65f);
        var shadow = baseImage.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        skipFill = CreateSkipImage(root, "White Hold Progress", sprite);
        skipFill.color = Color.white;
        skipFill.type = Image.Type.Filled;
        skipFill.fillMethod = Image.FillMethod.Radial360;
        skipFill.fillOrigin = (int)Image.Origin360.Top;
        skipFill.fillClockwise = true;
        skipFill.fillAmount = 0f;

        var labelObject = new GameObject("Hold Hint", typeof(RectTransform), typeof(Text), typeof(Outline));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(root, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, 0f);
        labelRect.pivot = new Vector2(.5f, 0f);
        labelRect.sizeDelta = new Vector2(180f, 34f);
        var label = labelObject.GetComponent<Text>();
        label.font = HangulFont.GetEmphasis();
        label.text = "ESC 길게 누르기";
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        var outline = labelObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, .85f);
        outline.effectDistance = new Vector2(1f, -1f);
        skipPrompt.SetActive(false);
    }

    private static Image CreateSkipImage(RectTransform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.sizeDelta = new Vector2(136f, 136f);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void Update()
    {
        if (!skipReady || finished) return;
        UpdateSkipHold(Application.isFocused && Input.GetKey(KeyCode.Escape), Time.unscaledDeltaTime);
    }

    private void UpdateSkipHold(bool held, float deltaTime)
    {
        if (!skipReady || finished) return;
        if (!held)
        {
            skipArmed = true;
            ResetSkipHold();
            return;
        }
        if (!skipArmed) return;

        skipHeldSeconds = Mathf.Min(SkipHoldSeconds, skipHeldSeconds + Mathf.Max(0f, deltaTime));
        skipFill.fillAmount = skipHeldSeconds / SkipHoldSeconds;
        if (skipHeldSeconds >= SkipHoldSeconds)
        {
            Debug.Log("[OpeningCinematicPlayer] Skipped with ESC; continuing to game scene.");
            Finish(true);
        }
    }

    private void ResetSkipHold()
    {
        skipHeldSeconds = 0f;
        if (skipFill != null) skipFill.fillAmount = 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        ResetSkipHold();
        skipArmed = false;
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
        skipReady = true;
        // An ESC key already held before playback must be released first.
        skipArmed = !Input.GetKey(KeyCode.Escape);
        skipPrompt.SetActive(true);
    }
    private void Ended(VideoPlayer source)
    {
        Debug.Log("[OpeningCinematicPlayer] Completed; continuing to game scene.");
        Finish(true);
    }
    private void Error(VideoPlayer source, string message)
    {
        // A video that cannot play (missing file, unsupported codec, prepare timeout) used to
        // send the player back to the menu, and START retried the same video forever - the
        // game was unreachable on that PC. Skip the cinematic and carry on instead.
        Debug.LogError("[OpeningCinematicPlayer] " + message + " Skipping the cinematic.");
        Finish(true);
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
        skipReady = false;
        ResetSkipHold();
        if (skipPrompt != null) skipPrompt.SetActive(false);
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
