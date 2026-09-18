using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class DevModeStage3 : MonoBehaviour
{
    public bool grantLanternOnStart = true;
    public bool skipBossIntroCutscene = true;

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
}
