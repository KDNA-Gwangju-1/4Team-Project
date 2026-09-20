using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class DevModeStage3 : MonoBehaviour
{
    public bool grantLanternOnStart = true;
    public bool skipBossIntroCutscene = true;

    [Header("Shortcuts")]
    [Tooltip("Drops the boss straight to the phase 2 threshold, so the transition cutscene plays now.")]
    public KeyCode jumpToPhase2Key = KeyCode.F2;
    [Tooltip("Kills the boss outright.")]
    public KeyCode killBossKey = KeyCode.F4;
    [Tooltip("Jumps straight to the ending cutscene without fighting.")]
    public KeyCode endingKey = KeyCode.F5;

    void Awake()
    {
        if (grantLanternOnStart)
        {
            PlayerMovement2D.LanternObtained = true;
        }

        if (skipBossIntroCutscene)
        {
            var cutscene = FindObjectOfType<Stage3BossIntroCutscene>();
            if (cutscene != null)
            {
                cutscene.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(jumpToPhase2Key)) DamageBossTo(PhaseThreshold());
        if (Input.GetKeyDown(killBossKey)) DamageBossTo(0);

        if (Input.GetKeyDown(endingKey))
        {
            var ending = FindObjectOfType<Stage3EndingCutscene>();
            if (ending != null) ending.PlayNow();
        }
    }

    private static int PhaseThreshold()
    {
        var controller = FindObjectOfType<BossPhaseController2D>();
        return controller != null ? controller.phase2AtHealth : 0;
    }

    // Hits it one point at a time so every listener fires exactly as it would in
    // a real fight - the phase controller watches OnDamaged, not the raw value.
    private static void DamageBossTo(int target)
    {
        var boss = FindObjectOfType<Boss2D>();
        if (boss == null) return;

        boss.Invulnerable = false;
        boss.requireLightToDamage = false;

        for (int guard = 0; guard < 200; guard++)
        {
            if (boss == null || boss.CurrentHealth <= target) break;
            boss.TakeDamage(1);
        }
    }
}
