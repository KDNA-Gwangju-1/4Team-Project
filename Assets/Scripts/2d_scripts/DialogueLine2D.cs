using UnityEngine;

// One spoken line plus whatever the scene does once it is dismissed.
// Cutscenes iterate a list of these rather than holding a named field per line,
// so rewriting the script means editing an array in the Inspector, not the code.
[System.Serializable]
public class DialogueLine2D
{
    public bool isBoss = true;
    [TextArea] public string text;
    [Tooltip("Staging beat to run AFTER this line is dismissed. 0 = nothing; the owning cutscene defines the rest.")]
    public int beatAfter = 0;
}
