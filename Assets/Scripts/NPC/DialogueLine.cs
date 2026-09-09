using System;
using UnityEngine;

/// <summary>
/// One line of NPC dialogue for Inspector lists.
/// </summary>
[Serializable]
public class DialogueLine
{
    public string speakerName = "Gerald";

    [TextArea(2, 4)]
    public string text;
}
