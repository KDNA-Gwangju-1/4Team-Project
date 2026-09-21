using System.Collections;
using UnityEngine;

// 플레이어가 트리거에 닿으면 카메라가 살짝 빠지면서 목표(출구 등)까지 훑고 되돌아온다.
// 그동안 플레이어는 멈춘다. CameraFollow2D를 잠시 끄고 위치·ortho를 직접 잡는다.
[RequireComponent(typeof(Collider2D))]
public class CameraPeekTrigger2D : MonoBehaviour
{
    public Transform target;
    public float zoomOutOrthoSize = 14f;
    public float zoomTime = 0.6f;
    public float panOutTime = 2.6f;
    public float holdTime = 0.8f;
    public float panBackTime = 1.6f;
    public bool showOnce = true;

    private bool used;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (showOnce && used) return;
        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null || !player.enabled || target == null) return;

        used = true;
        StartCoroutine(Peek(player));
    }

    private IEnumerator Peek(PlayerMovement2D player)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        var follow = cam.GetComponent<CameraFollow2D>();

        player.enabled = false;
        player.CutsceneInvulnerable = true;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (follow != null) follow.enabled = false;

        float gameplayOrtho = cam.orthographicSize;
        Vector3 from = cam.transform.position;
        Vector3 to = new Vector3(target.position.x, target.position.y, from.z);

        // 살짝 빠지면서 출발
        yield return Move(cam, from, from, gameplayOrtho, zoomOutOrthoSize, zoomTime);
        yield return Move(cam, from, to, zoomOutOrthoSize, zoomOutOrthoSize, panOutTime);
        yield return new WaitForSeconds(holdTime);

        // 플레이어가 서 있는 곳으로 되돌아오며 원래 배율로
        Vector3 back = follow != null ? player.transform.position + follow.offset : from;
        back.z = from.z;
        yield return Move(cam, to, back, zoomOutOrthoSize, gameplayOrtho, panBackTime);

        if (follow != null) follow.enabled = true;
        player.CutsceneInvulnerable = false;
        player.enabled = true;
    }

    private IEnumerator Move(Camera cam, Vector3 a, Vector3 b, float orthoA, float orthoB, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            cam.transform.position = Vector3.Lerp(a, b, k);
            cam.orthographicSize = Mathf.Lerp(orthoA, orthoB, k);
            yield return null;
        }
        cam.transform.position = b;
        cam.orthographicSize = orthoB;
    }
}
