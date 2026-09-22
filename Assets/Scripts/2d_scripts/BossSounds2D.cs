using UnityEngine;

// 보스 효과음. BossAttack2D의 이벤트만 받는다.
// 탄막은 한 패턴에 여러 발이 같은 프레임에 나가므로, 짧은 간격 안의 발사는 한 번으로 묶어 울린다.
[RequireComponent(typeof(BossAttack2D))]
public class BossSounds2D : MonoBehaviour
{
    [Tooltip("탄막을 쏠 때 이 중 하나를 무작위로.")]
    public AudioClip[] shotClips;
    [Range(0f, 1f)] public float shotVolume = 0.6f;
    [Tooltip("이 시간 안에 연달아 나간 탄은 한 번의 발사로 친다.")]
    public float shotMergeWindow = 0.08f;

    private BossAttack2D attack;
    private AudioSource source;
    private float lastShotTime = -10f;

    void Awake()
    {
        attack = GetComponent<BossAttack2D>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 아레나가 한 화면이라 거리 감쇠 없이
    }

    void OnEnable()
    {
        attack.OnBulletSpawned += HandleShot;
    }

    void OnDisable()
    {
        attack.OnBulletSpawned -= HandleShot;
    }

    private void HandleShot()
    {
        if (shotClips == null || shotClips.Length == 0) return;
        if (Time.time - lastShotTime < shotMergeWindow) return;
        lastShotTime = Time.time;

        var clip = shotClips[Random.Range(0, shotClips.Length)];
        if (clip != null) source.PlayOneShot(clip, shotVolume);
    }
}
