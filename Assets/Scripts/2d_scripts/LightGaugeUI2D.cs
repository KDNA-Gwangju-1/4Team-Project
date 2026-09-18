using UnityEngine;
using UnityEngine.UI;

public class LightGaugeUI2D : MonoBehaviour
{
    public Image fill;
    public Color readyColor = new Color(1f, 0.93f, 0.6f);
    public Color lowColor = new Color(1f, 0.6f, 0.2f);
    public Color lockedColor = new Color(0.45f, 0.45f, 0.5f);
    public float lowThreshold = 0.3f;
    public float lockedBlinkSpeed = 6f;

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || fill == null) return;

        float charge = player.LightCharge01;
        fill.fillAmount = charge;

        if (player.LightLocked)
        {
            float blink = (Mathf.Sin(Time.time * lockedBlinkSpeed) + 1f) * 0.5f;
            fill.color = Color.Lerp(lockedColor, lowColor, blink * 0.35f);
        }
        else
        {
            fill.color = charge <= lowThreshold ? lowColor : readyColor;
        }
    }
}
