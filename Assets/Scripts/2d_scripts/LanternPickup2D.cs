using UnityEngine;

public class LanternPickup2D : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        player.PickUpLantern();
        Destroy(gameObject);
    }
}
