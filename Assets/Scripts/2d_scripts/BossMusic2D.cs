using UnityEngine;

// 보스전 음악. BossAttack2D.Phase를 매 프레임 읽어서 1페이즈·2페이즈 곡을 교차 페이드한다.
// 페이즈 0(인트로 컷신·페이즈 전환 컷신·엔딩)에서는 곡이 빠진다. 전환 시점을 따로 알려줄 필요가 없다.
public class BossMusic2D : MonoBehaviour
{
    public BossAttack2D attack;
    public AudioClip phase1Clip;
    public AudioClip phase2Clip;
    [Range(0f, 1f)] public float volume = 0.4f;
    public float fadeInTime = 1.2f;
    public float fadeOutTime = 1.5f;

    [Tooltip("페이즈가 이 시간 이상 유지돼야 곡을 바꾼다. 인트로에서 보스가 켜지는 순간 1페이즈가 한 프레임 스치는 것을 거른다.")]
    public float phaseSettleTime = 0.2f;

    private AudioSource phase1;
    private AudioSource phase2;
    private int seenPhase = -1;
    private int settledPhase;
    private float seenFor;

    void Awake()
    {
        if (attack == null) attack = FindFirstObjectByType<BossAttack2D>();
        phase1 = MakeSource(phase1Clip);
        phase2 = MakeSource(phase2Clip);
    }

    private AudioSource MakeSource(AudioClip clip)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = 0f;
        return s;
    }

    void Update()
    {
        int phase = attack != null ? attack.Phase : 0;
        if (phase != seenPhase) { seenPhase = phase; seenFor = 0f; }
        seenFor += Time.unscaledDeltaTime;
        if (seenFor >= phaseSettleTime) settledPhase = phase;

        Drive(phase1, settledPhase == 1);
        Drive(phase2, settledPhase == 2);
    }

    private void Drive(AudioSource s, bool on)
    {
        if (s == null || s.clip == null) return;

        float target = on ? volume : 0f;
        float time = on ? fadeInTime : fadeOutTime;
        float step = time <= 0f ? 1f : Time.unscaledDeltaTime * volume / time;
        s.volume = Mathf.MoveTowards(s.volume, target, step);

        if (on && !s.isPlaying) s.Play();
        else if (!on && s.isPlaying && s.volume <= 0.001f) s.Stop();
    }
}
