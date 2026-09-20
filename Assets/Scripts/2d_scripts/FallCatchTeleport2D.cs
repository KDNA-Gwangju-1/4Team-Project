using UnityEngine;

public class FallCatchTeleport2D : MonoBehaviour
{
    public Transform respawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null || respawnPoint == null) return;

        player.transform.position = respawnPoint.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
