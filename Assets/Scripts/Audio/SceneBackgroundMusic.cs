using UnityEngine;

/// <summary>
/// 씬이 활성화되어 있는 동안 하나의 배경음악을 반복 재생한다.
/// 씬을 직접 실행해도 저장된 마스터 볼륨이 적용된다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SceneBackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;
    [SerializeField] private bool loop = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (!TryGetComponent(out audioSource))
        {
            Debug.LogError("[SceneBackgroundMusic] AudioSource가 필요합니다.", this);
            enabled = false;
            return;
        }

        ApplySourceSettings();
    }

    private void Start()
    {
        AudioListener.volume = GameSettings.MasterVolume;

        if (music == null)
        {
            Debug.LogWarning("[SceneBackgroundMusic] 재생할 음악이 연결되지 않았습니다.", this);
            return;
        }

        audioSource.Play();
    }

    public void Configure(AudioClip clip, float sourceVolume)
    {
        music = clip;
        volume = Mathf.Clamp01(sourceVolume);

        if (TryGetComponent(out AudioSource source))
        {
            audioSource = source;
            ApplySourceSettings();
        }
    }

    private void ApplySourceSettings()
    {
        if (audioSource == null) return;

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
        audioSource.clip = music;
    }
}
