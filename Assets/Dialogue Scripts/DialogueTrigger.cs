using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueLine[] lines;

    [Header("Settings")]
    [Tooltip("If true, this trigger fires only once per scene load")]
    public bool triggerOnce = true;
    [Tooltip("Tag of the object that activates this trigger")]
    public string playerTag = "Player";

    private bool _triggered;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;
        if (DialogueManager.IsActive) return;

        _triggered = true;
        DialogueManager.Instance.StartDialogue(lines);
    }
}
