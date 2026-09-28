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
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.volume = 0f;
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

        // 음악: 밝은 꿈은 씬에 음악이 없어서 여기서 튼다 (보스전·처치 시 전환은 BrightDreamMusic 이 맡는다).
        // 다른 씬은 각자 음악을 가지고 있으니 이쪽 음악은 끈다.
        if (scene.name == "SD_BrightDream_Blockout_Rect")
        {
            PlayMusic("BGM_Garden", 1.5f);
            if (FindFirstObjectByType<BrightDreamMusic>() == null) new GameObject("BrightDreamMusic").AddComponent<BrightDreamMusic>();
        }
        else StopMusic(.4f);
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

    // ============================================================
    // 음악 (Resources/Audio/Music) - 한 곡씩, 부드럽게 바꿔 튼다
    // ============================================================

    private AudioSource music;
    private Coroutine musicFade;
    private const float MusicVolume = .38f;

    // 곡마다 원본 음량이 달라서 게임 안에서 들리는 크기를 맞춘다 (없으면 MusicVolume).
    // 기준은 엔딩 곡(.38) - 정원은 비슷하게, 보스전은 원본이 2dB 커서 줄이되 정원보다 살짝 크게 들리도록.
    private static readonly Dictionary<string, float> TrackVolume = new Dictionary<string, float>
    {
        { "BGM_Garden", .36f },
        { "BGM_GardenBoss", .34f },
    };

    // 나레이션이 나오는 동안 음악을 줄인다 (fade 가 정한 크기에 곱한다).
    private const float DuckLevel = .5f;
    private float musicLevel;
    private float duck = 1f;
    private AudioSource duckSource;

    /// <summary>이 소스가 재생되는 동안 음악을 줄여 둔다 - 끝나면 알아서 원래대로 돌아온다.</summary>
    public static void DuckMusicWhile(AudioSource source)
    {
        if (!Application.isPlaying) return;
        if (instance == null) Initialize();
        instance.duckSource = source;
    }

    private void Update()
    {
        if (music == null) return;
        bool ducking = duckSource != null && duckSource.isPlaying;
        if (!ducking) duckSource = null;
        // 줄일 땐 빠르게(0.25초), 돌아올 땐 천천히(1초) - unscaled 라 대사로 멈춘 동안에도 움직인다.
        duck = Mathf.MoveTowards(duck, ducking ? DuckLevel : 1f,
            Time.unscaledDeltaTime * (ducking ? 2.2f : .55f));
        music.volume = musicLevel * duck;
    }

    /// <summary>음악을 바꿔 튼다. 같은 곡이 이미 나오고 있으면 그대로 둔다.</summary>
    public static void PlayMusic(string name, float fade = 1f)
    {
        if (!Application.isPlaying) return;
        if (instance == null) Initialize();
        var clip = Resources.Load<AudioClip>("Audio/Music/" + name);
        if (clip == null) return;
        if (instance.music.clip == clip && instance.music.isPlaying) return;
        instance.StartMusicFade(clip, fade);
    }

    /// <summary>음악을 서서히 끈다.</summary>
    public static void StopMusic(float fade = 1f)
    {
        if (!Application.isPlaying || instance == null || instance.music == null) return;
        if (!instance.music.isPlaying) return;
        instance.StartMusicFade(null, fade);
    }

    private void StartMusicFade(AudioClip next, float fade)
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(MusicFade(next, Mathf.Max(.01f, fade)));
    }

    private System.Collections.IEnumerator MusicFade(AudioClip next, float fade)
    {
        // 나가는 곡을 먼저 줄이고, 다음 곡을 처음부터 키운다 (unscaled - 일시정지·사망 화면에서도 진행)
        // 실제 music.volume 은 Update 가 musicLevel × duck 으로 매 프레임 적용한다.
        float from = musicLevel;
        for (float t = 0; t < fade * .5f && music.isPlaying; t += Time.unscaledDeltaTime)
        {
            musicLevel = Mathf.Lerp(from, 0f, t / (fade * .5f));
            yield return null;
        }
        music.Stop();
        musicLevel = 0f;
        if (next == null) { musicFade = null; yield break; }
        float target;
        if (!TrackVolume.TryGetValue(next.name, out target)) target = MusicVolume;
        music.clip = next;
        music.Play();
        for (float t = 0; t < fade * .5f; t += Time.unscaledDeltaTime)
        {
            musicLevel = Mathf.Lerp(0f, target, t / (fade * .5f));
            yield return null;
        }
        musicLevel = target;
        musicFade = null;
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
