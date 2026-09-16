using UnityEngine;
using UnityEngine.UI;

public class HealthUI2D : MonoBehaviour
{
    private Text healthText;

    void Awake()
    {
        healthText = GetComponent<Text>();
    }

    void Update()
    {
        var player = PlayerMovement2D.Instance;
        if (player == null || healthText == null) return;

        healthText.text = $"HP: {player.CurrentHealth} / {player.maxHealth}";
    }
}
