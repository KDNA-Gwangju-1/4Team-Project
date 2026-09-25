using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Small shared voice pool for the newly authored cues; existing chapter 2 audio stays intact.
public sealed class GameSfx : MonoBehaviour
{
    private static GameSfx instance;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private readonly AudioSource[] voices = new AudioSource[12];
    private AudioSource ambience;
    private int nextVoice;
    private int configuredScene = -1;
    public int PlayedCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        var go = new GameObject("Game SFX");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameSfx>();
    }

    private void Awake()
    {
        AudioListener.volume = GameSettings.MasterVolume;
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = new GameObject("Voice " + i).AddComponent<AudioSource>();
            voices[i].transform.SetParent(transform);
            voices[i].playOnAwake = false;
            voices[i].rolloffMode = AudioRolloffMode.Linear;
            voices[i].minDistance = 2f;
            voices[i].maxDistance = 30f;
        }
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        ambience.volume = .12f;
        foreach (var clip in Resources.LoadAll<AudioClip>("Audio/Generated")) clips[clip.name] = clip;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; }

    private void Start() { SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single && scene != SceneManager.GetActiveScene()) return;
        if (configuredScene == scene.handle) return;
        configuredScene = scene.handle;
        foreach (var voice in voices)
            if (voice.clip == null || (voice.clip.name != "Portal" && voice.clip.name != "Door")) voice.Stop();
        lastPlayed.Clear();
        ambience.Stop();
        string bed = scene.name == "HospitalRoom" ? "AmbHospital" : scene.name == "SD_BrightDream_Blockout_Rect" ? "AmbGarden" : null;
        AudioClip clip;
        if (bed != null && clips.TryGetValue(bed, out clip)) { ambience.clip = clip; ambience.Play(); }
        foreach (var root in scene.GetRootGameObjects())
        {
            BindButtons(root);
            foreach (var controller in root.GetComponentsInChildren<CharacterController>(true))
                if (controller.GetComponent<FirstPersonController>() != null || controller.GetComponent<SimpleFirstPersonController>() != null)
                    if (controller.GetComponent<Footsteps3D>() == null) controller.gameObject.AddComponent<Footsteps3D>();
        }
    }

    public static void BindButtons(GameObject root)
    {
        if (root == null) return;
        foreach (var button in root.GetComponentsInChildren<Button>(true))
            if (button.GetComponent<UiButtonSound>() == null) button.gameObject.AddComponent<UiButtonSound>();
    }

    public static void Play(string cue, float volume = .5f, bool ui = false)
    {
        PlayInternal(cue, Vector3.zero, false, volume, ui);
    }

    public static void At(string cue, Vector3 position, float volume = .5f)
    {
        PlayInternal(cue, position, true, volume, false);
    }

    private static void PlayInternal(string cue, Vector3 position, bool spatial, float volume, bool ui)
    {
        if (!Application.isPlaying || (!ui && AudioListener.pause)) return;
        if (instance == null) Initialize();
        AudioClip clip;
        if (!instance.clips.TryGetValue(cue, out clip)) return;
        float last;
        float interval = cue == "UiHover" ? .09f : cue == "Warning" ? .9f : .055f;
        if (instance.lastPlayed.TryGetValue(cue, out last) && Time.unscaledTime - last < interval) return;
        instance.lastPlayed[cue] = Time.unscaledTime;
        AudioSource source = null;
        foreach (var voice in instance.voices) if (!voice.isPlaying) { source = voice; break; }
        if (source == null) source = instance.voices[instance.nextVoice++ % instance.voices.Length];
        source.Stop();
        source.transform.position = position;
        source.spatialBlend = spatial ? .85f : 0f;
        source.ignoreListenerPause = ui;
        source.pitch = 1f;
        source.volume = Mathf.Clamp01(volume);
        source.clip = clip;
        source.Play();
        instance.PlayedCount++;
    }
}
