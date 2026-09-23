using UnityEngine;

// 몬스터 효과음. Monster2D/RangedMonster2D는 전 스테이지 공용이라 건드리지 않고,
// 같은 오브젝트의 detectionRange와 위치 변화만 읽는다.
// 거리 감쇠를 거는 이유: 2스테이지에 나이트 라이트가 11마리라 2D로 틀면 전부 겹쳐 들린다.
public class MonsterSounds2D : MonoBehaviour
{
    [Tooltip("플레이어가 감지 범위에 들어온 순간 한 번. 범위를 벗어났다 다시 들어오면 또 난다.")]
    public AudioClip noticeClip;
    [Range(0f, 1f)] public float noticeVolume = 0.6f;

    [Tooltip("자리를 옮기는 동안 루프.")]
    public AudioClip moveClip;
    [Range(0f, 1f)] public float moveVolume = 0.5f;
    [Tooltip("한 프레임에 이보다 덜 움직이면 멈춘 것으로 본다.")]
    public float moveEpsilon = 0.002f;

    [Tooltip("이 거리 안에서는 최대 음량.")]
    public float fullVolumeDistance = 4f;
    [Tooltip("이 거리 밖에서는 들리지 않는다. 카메라 z가 -10이라 화면 한가운데도 거리 10이다.")]
    public float silentDistance = 26f;

    private float detectionRange;
    private Monster2D chaser;
    private RangedMonster2D ranged;
    private AudioSource noticeSource;
    private AudioSource moveSource;
    private bool playerInRange;
    private Vector3 lastPosition;

    // 죽은 몬스터는 20초쯤 안 보이는 채로 그 자리에 남았다가 스폰 위치로 돌아간다.
    // 그동안 소리를 내면 빈자리에서 인식음이 울리고, 되돌아갈 때 순간이동이 이동음으로 잡힌다.
    private bool Dead => (chaser != null && chaser.IsDead) || (ranged != null && ranged.IsDead);

    void Awake()
    {
        chaser = GetComponent<Monster2D>();
        ranged = GetComponent<RangedMonster2D>();
        detectionRange = chaser != null ? chaser.detectionRange : ranged != null ? ranged.detectionRange : 0f;

        noticeSource = MakeSource(noticeVolume, false);
        moveSource = MakeSource(moveVolume, true);
        moveSource.clip = moveClip;
        lastPosition = transform.position;
    }

    private AudioSource MakeSource(float volume, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = fullVolumeDistance;
        source.maxDistance = silentDistance;
        source.dopplerLevel = 0f;
        return source;
    }

    void LateUpdate()
    {
        if (Time.timeScale <= 0f || Dead)
        {
            if (moveSource.isPlaying) moveSource.Stop();
            // 되살아나서 다시 다가올 때 인식음이 한 번 더 나도록 풀어둔다
            playerInRange = false;
            lastPosition = transform.position;
            return;
        }

        UpdateNotice();
        UpdateMove();
    }

    private void UpdateNotice()
    {
        if (noticeClip == null || detectionRange <= 0f) return;

        var player = PlayerMovement2D.Instance;
        if (player == null) return;

        float sqr = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;
        // 경계에서 들락날락하며 연타되지 않게, 벗어나는 쪽 문턱을 더 멀리 둔다
        float enter = detectionRange, leave = detectionRange * 1.5f;

        if (!playerInRange && sqr <= enter * enter)
        {
            playerInRange = true;
            noticeSource.PlayOneShot(noticeClip);
        }
        else if (playerInRange && sqr > leave * leave)
        {
            playerInRange = false;
        }
    }

    private void UpdateMove()
    {
        if (moveClip == null) return;

        bool moving = (transform.position - lastPosition).sqrMagnitude > moveEpsilon * moveEpsilon;
        lastPosition = transform.position;

        if (moving && !moveSource.isPlaying) moveSource.Play();
        else if (!moving && moveSource.isPlaying) moveSource.Stop();
    }

    void OnDisable()
    {
        if (moveSource != null && moveSource.isPlaying) moveSource.Stop();
    }
}
