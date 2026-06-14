using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    [Tooltip("Name shown above the dialogue text (leave blank to hide)")]
    public string speakerName;
    [TextArea(2, 5)]
    public string text;
    [Tooltip("Character portrait sprite shown for this line (leave null to hide portrait)")]
    public Sprite portrait;
}
