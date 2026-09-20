using UnityEngine;

// One spray of bullets. Phase 2 works through a list of these so the rhythm can
// be rewritten in the Inspector instead of in code.
[System.Serializable]
public class BulletFan2D
{
    public string label = "fan";
    [Tooltip("How many streams. Three or five read as a fan; one reads as a shot.")]
    public int count = 3;
    [Tooltip("Total width of the fan. Wider means bigger gaps to slip through.")]
    public float spreadAngle = 60f;
    public float speed = 5f;
    [Tooltip("0 = fires straight out from her, 1 = tracks the player exactly.")]
    [Range(0f, 1f)] public float aimBlend = 0.6f;
}
