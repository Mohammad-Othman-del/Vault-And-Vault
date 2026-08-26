using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    [Tooltip("Name shown above the dialogue text (leave blank to hide)")]
    public string speakerName;
    [TextArea(2, 5)]
    public string text;
    [Tooltip("First portrait sprite (alternates with portrait2 while dialogue is active)")]
    public Sprite portrait;
    [Tooltip("Second portrait sprite to alternate with (leave null to use only portrait)")]
    public Sprite portrait2;
}
