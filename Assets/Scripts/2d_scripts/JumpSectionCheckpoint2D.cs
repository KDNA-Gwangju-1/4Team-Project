using UnityEngine;

public class JumpSectionCheckpoint2D : MonoBehaviour
{
    public bool isExit = false;
    public Transform anchorPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovement2D>();
        if (player == null) return;

        if (isExit)
        {
            player.ClearRespawnAnchor();
        }
        else
        {
            Vector3 pos = anchorPoint != null ? anchorPoint.position : transform.position;
            player.SetRespawnAnchor(pos);
        }
    }
}
