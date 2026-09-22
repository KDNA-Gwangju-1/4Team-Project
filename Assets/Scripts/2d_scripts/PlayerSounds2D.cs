using UnityEngine;
using UnityEngine.Serialization;

// 플레이어 상태를 읽어 발소리와 손전등 소리를 켜고 끈다. PlayerMovement2D는 전 스테이지
// 공용이라 건드리지 않고, 공개된 IsGrounded/IsLightOn과 리지드바디 속도만 본다.
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerSounds2D : MonoBehaviour
{
    public AudioClip footsteps;
    [Range(0f, 1f)] public float footstepsVolume = 0.6f;
    [Tooltip("이 속도 아래면 서 있는 것으로 본다.")]
    public float walkSpeedThreshold = 0.5f;

    [Tooltip("켜는 순간 한 번. 비추는 동안은 아무 소리도 내지 않는다.")]
    [FormerlySerializedAs("flashlightHum")]
    public AudioClip flashlightOn;
    [Tooltip("끄는 순간 한 번. 배터리가 다 떨어져 꺼질 때도 난다.")]
    public AudioClip flashlightOff;
    [Range(0f, 1f)] public float flashlightVolume = 0.5f;

    [Tooltip("광탄을 쏠 때마다 이 중 하나를 무작위로.")]
    public AudioClip[] shotClips;
    [Range(0f, 1f)] public float shotVolume = 0.6f;

    public AudioClip dashClip;
    [Range(0f, 1f)] public float dashVolume = 0.6f;

    private PlayerMovement2D player;
    private Rigidbody2D body;
    private AudioSource footstepsSource;
    private AudioSource flashlightSource;
    private bool lightWasOn;

    void Awake()
    {
        player = GetComponent<PlayerMovement2D>();
        body = GetComponent<Rigidbody2D>();
        footstepsSource = MakeSource(footsteps, footstepsVolume, true);
        flashlightSource = MakeSource(null, flashlightVolume, false);
    }

    void OnEnable()
    {
        if (player == null) return;
        player.OnBulletFired += HandleShot;
        player.OnDashStarted += HandleDash;
    }

    private void HandleShot()
    {
        if (shotClips == null || shotClips.Length == 0) return;
        OneShot(shotClips[Random.Range(0, shotClips.Length)], shotVolume);
    }

    private void HandleDash()
    {
        OneShot(dashClip, dashVolume);
    }

    // 원샷들은 손전등 소스를 같이 쓴다. PlayOneShot은 소스 볼륨에 곱해지므로 나눠서 원하는 크기로 맞춘다.
    private void OneShot(AudioClip clip, float volume)
    {
        if (clip == null || flashlightSource == null) return;
        flashlightSource.PlayOneShot(clip, volume / Mathf.Max(0.01f, flashlightVolume));
    }

    private AudioSource MakeSource(AudioClip clip, float volume, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        source.spatialBlend = 0f;
        return source;
    }

    // PlayerMovement2D.Update가 손전등 상태를 정한 뒤에 읽어야 켜는 소리가 같은 프레임에 난다
    void LateUpdate()
    {
        // AudioSource는 timeScale을 모른다. 죽음 화면에서 멈춘 채로 발소리가 계속 나면 안 된다.
        if (Time.timeScale <= 0f)
        {
            Toggle(footstepsSource, false);
            return;
        }

        bool walking = player.IsGrounded && !player.IsDashing
            && body != null && Mathf.Abs(body.linearVelocity.x) > walkSpeedThreshold;
        Toggle(footstepsSource, walking);

        bool lightOn = player.IsLightOn;
        if (lightOn != lightWasOn) Click(lightOn ? flashlightOn : flashlightOff);
        lightWasOn = lightOn;
    }

    private void Click(AudioClip clip)
    {
        if (clip == null || flashlightSource == null) return;
        flashlightSource.PlayOneShot(clip);
    }

    private static void Toggle(AudioSource source, bool on)
    {
        if (source == null || source.clip == null) return;
        if (on && !source.isPlaying) source.Play();
        else if (!on && source.isPlaying) source.Stop();
    }

    void OnDisable()
    {
        if (player != null) { player.OnBulletFired -= HandleShot; player.OnDashStarted -= HandleDash; }
        Toggle(footstepsSource, false);
    }
}
