using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea]
    public string text;

    [Min(0)]
    public int standingSpriteIndex;
}