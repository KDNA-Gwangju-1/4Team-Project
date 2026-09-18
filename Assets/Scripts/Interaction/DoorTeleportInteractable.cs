using UnityEngine;

/// <summary>
/// E 로 들어가면 정해 둔 자리로 플레이어를 옮겨 주는 문.
///
/// "여기로 들어가면 저기로 나온다" 를 씬 하나 안에서 만들 때 쓴다.
/// 로딩 화면을 거치는 씬 이동이 아니라 순간이동이라 끊김이 없다.
///
/// 목적지는 빈 오브젝트를 하나 놓고 Destination 에 연결하면 된다.
/// 씬 뷰에서 그 오브젝트를 옮기거나 돌리면 도착 위치와 바라보는 방향이 같이 바뀐다.
/// </summary>
public class DoorTeleportInteractable : Interactable
{
    [Header("이동할 곳")]
    [Tooltip("여기로 옮긴다. 이 오브젝트가 보는 방향으로 플레이어도 돌아선다.")]
    [SerializeField] private Transform destination;

    [Tooltip("Destination 을 비워 뒀을 때 쓸 좌표")]
    [SerializeField] private Vector3 fallbackPosition = new Vector3(3.1f, 0.1f, -3f);

    [Tooltip("Destination 을 비워 뒀을 때 바라볼 방향 (Y 각도)")]
    [SerializeField] private float fallbackYaw = -22f;

    [Header("로딩 연출")]
    [Tooltip("연결하면 로딩 화면으로 덮었다가 걷는다. 비우면 그냥 바로 옮긴다.")]
    [SerializeField] private LoadingOverlay loadingOverlay;

    [Tooltip("로딩 화면이 화면에 머무는 시간 (초)")]
    [SerializeField] private float loadingSeconds = 3f;

    /// <summary>컴포넌트를 처음 붙였을 때의 기본값</summary>
    private void Reset()
    {
        actionLabel   = "들어가기";
        interactRange = 2f;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        var player = interactor != null ? interactor.transform : null;
        if (player == null)
        {
            Debug.LogWarning("[문] 플레이어를 못 찾아서 이동하지 못했습니다.", this);
            return;
        }

        if (loadingOverlay != null)
        {
            if (loadingOverlay.IsShowing) return;   // 연출 중에 또 누르는 것 방지

            // 화면이 완전히 덮인 뒤에 옮긴다. 그래야 순간이동하는 게 안 보인다.
            loadingOverlay.Play(loadingSeconds, delegate { Move(player); });
        }
        else
        {
            Move(player);
        }

        // Inspector 의 On Interact 에 걸어 둔 것이 있으면 같이 실행한다.
        base.Interact(interactor);
    }

    private void Move(Transform player)
    {
        Vector3 pos = destination != null ? destination.position      : fallbackPosition;
        float   yaw = destination != null ? destination.eulerAngles.y : fallbackYaw;

        // CharacterController 가 켜져 있으면 transform 을 직접 옮겨도 다시 끌려온다.
        // 그래서 잠깐 껐다가 옮기고 다시 켠다.
        var cc = player.GetComponent<CharacterController>();
        bool wasOn = cc != null && cc.enabled;
        if (wasOn) cc.enabled = false;

        // 위아래 시점(pitch)은 카메라 쪽에 따로 있으므로 건드리지 않는다.
        player.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

        if (wasOn) cc.enabled = true;
    }
}
