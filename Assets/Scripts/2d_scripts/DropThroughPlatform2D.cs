using UnityEngine;

// Marks a platform the player can drop off with down + jump.
// The lowest platform deliberately does NOT carry this: below it is the drop.
[RequireComponent(typeof(Collider2D))]
public class DropThroughPlatform2D : MonoBehaviour
{
    [Tooltip("How long the player falls through before the platform is solid again.")]
    public float passThroughTime = 0.4f;
}
