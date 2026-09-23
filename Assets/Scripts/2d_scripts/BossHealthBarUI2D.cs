using UnityEngine;
using UnityEngine.UI;

// Long red boss bar across the top, MapleStory style: the red fill snaps to the
// real value while a paler ghost bar drains behind it so hits stay readable.
public class BossHealthBarUI2D : MonoBehaviour
{
    public Boss2D boss;
    public Image fill;
    public Image ghostFill;
    public Text nameLabel;
    public Text valueLabel;
    public CanvasGroup group;

    public string bossName = "???";
    public float ghostDrainSpeed = 0.35f;
    public float ghostDelay = 0.4f;
    public float fadeSpeed = 2.5f;
    public bool hideWhenBossGone = true;

    private float ghost = 1f;
    private float ghostHoldTimer;

    void Start()
    {
        if (boss == null) boss = Object.FindFirstObjectByType<Boss2D>();
        if (nameLabel != null) nameLabel.text = bossName;
        ghost = 1f;
    }

    void Update()
    {
        if (boss == null && hideWhenBossGone)
        {
            if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, 0f, fadeSpeed * Time.deltaTime);
            return;
        }
        if (boss == null) return;

        if (group != null) group.alpha = Mathf.MoveTowards(group.alpha, 1f, fadeSpeed * Time.deltaTime);

        float ratio = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;

        if (fill != null) fill.fillAmount = ratio;

        if (ratio < ghost)
        {
            ghostHoldTimer += Time.deltaTime;
            if (ghostHoldTimer >= ghostDelay)
            {
                ghost = Mathf.MoveTowards(ghost, ratio, ghostDrainSpeed * Time.deltaTime);
            }
        }
        else
        {
            ghost = ratio;
            ghostHoldTimer = 0f;
        }

        if (ghostFill != null) ghostFill.fillAmount = ghost;
        if (valueLabel != null) valueLabel.text = boss.CurrentHealth + " / " + boss.MaxHealth;
    }
}
