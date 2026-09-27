using UnityEngine;

// One spray of bullets. Phase 2 works through a list of these so the rhythm can
// be rewritten in the Inspector instead of in code.
//
// The shape matters less than the timing. Firing a whole fan on one frame reads
// as a flat wall you either fit through or do not; releasing it stream by stream
// turns the same bullets into something that sweeps past you, which is what makes
// a pattern feel dodged rather than survived.
[System.Serializable]
public class BulletFan2D
{
    public string label = "fan";

    [Header("Shape")]
    [Tooltip("How many streams. Three or five read as a fan; one reads as a shot.")]
    public int count = 3;
    [Tooltip("Total width of the fan. Wider means bigger gaps to slip through.")]
    public float spreadAngle = 60f;
    public float speed = 5f;
    [Tooltip("0 = fires straight out from her, 1 = tracks the player exactly.")]
    [Range(0f, 1f)] public float aimBlend = 0.6f;

    [Header("Timing")]
    [Tooltip("Delay between streams within one fan. Above zero the fan sweeps instead of appearing at once.")]
    public float stagger = 0.07f;
    [Tooltip("Sweep the fan from one edge to the other rather than outward from the middle.")]
    public bool sweep = true;

    [Header("Waves")]
    [Tooltip("Repeat the whole fan this many times.")]
    public int waves = 1;
    [Tooltip("Gap between repeats.")]
    public float waveInterval = 0.35f;
    [Tooltip("Rotate each repeat by this much, so the gaps move and the player has to move with them.")]
    public float waveAngleStep = 14f;
    [Tooltip("Speed change per repeat - staggered speeds stop the waves arriving as one block.")]
    public float waveSpeedStep = 0.4f;
}
