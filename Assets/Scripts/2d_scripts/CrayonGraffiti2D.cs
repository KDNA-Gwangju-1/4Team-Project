using System.Collections;
using UnityEngine;

public class CrayonGraffiti2D : MonoBehaviour
{
    public float triggerDistance = 6f;
    public float charInterval = 0.12f;

    private TextMesh textMesh;
    private string fullText;
    private bool triggered;

    void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        fullText = textMesh.text;
        textMesh.text = "";
    }

    void Update()
    {
        if (triggered) return;

        var player = PlayerMovement2D.Instance;
        if (player == null) return;

        float dist = Vector2.Distance(player.transform.position, transform.position);
        if (dist <= triggerDistance)
        {
            triggered = true;
            StartCoroutine(WriteOutRoutine());
        }
    }

    private IEnumerator WriteOutRoutine()
    {
        for (int i = 1; i <= fullText.Length; i++)
        {
            textMesh.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(charInterval);
        }
    }
}
