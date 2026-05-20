using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The root panel GameObject to show/hide")]
    public GameObject dialoguePanel;
    [Tooltip("TMP text for the speaker name (can be null if unused)")]
    public TMP_Text nameText;
    [Tooltip("TMP text for the dialogue body")]
    public TMP_Text dialogueText;
    [Tooltip("Optional 'Press E to continue' prompt object")]
    public GameObject continuePrompt;

    [Header("Settings")]
    [Tooltip("Characters per second for typewriter effect (0 = instant)")]
    public float typewriterSpeed = 40f;
    [Tooltip("Key to advance or skip dialogue")]
    public KeyCode advanceKey = KeyCode.E;

    public static bool IsActive { get; private set; }

    private DialogueLine[] _lines;
    private int _currentIndex;
    private Coroutine _typewriterRoutine;
    private bool _lineFinished;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!IsActive) return;

        if (Input.GetKeyDown(advanceKey))
        {
            if (!_lineFinished)
                FinishLineInstantly();
            else
                ShowNextLine();
        }
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        if (IsActive) return;

        _lines = lines;
        _currentIndex = 0;
        IsActive = true;
        dialoguePanel.SetActive(true);
        if (continuePrompt) continuePrompt.SetActive(false);
        ShowNextLine();
    }

    void ShowNextLine()
    {
        if (_currentIndex >= _lines.Length) { EndDialogue(); return; }

        DialogueLine line = _lines[_currentIndex++];

        if (nameText)
            nameText.text = line.speakerName;

        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);

        if (typewriterSpeed <= 0)
        {
            dialogueText.text = line.text;
            OnLineFinished();
        }
        else
        {
            _lineFinished = false;
            if (continuePrompt) continuePrompt.SetActive(false);
            _typewriterRoutine = StartCoroutine(Typewriter(line.text));
        }
    }

    IEnumerator Typewriter(string text)
    {
        dialogueText.text = "";
        float delay = 1f / typewriterSpeed;
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }
        OnLineFinished();
    }

    void FinishLineInstantly()
    {
        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
        dialogueText.text = _lines[_currentIndex - 1].text;
        OnLineFinished();
    }

    void OnLineFinished()
    {
        _lineFinished = true;
        if (continuePrompt) continuePrompt.SetActive(true);
    }

    void EndDialogue()
    {
        IsActive = false;
        dialoguePanel.SetActive(false);
        if (continuePrompt) continuePrompt.SetActive(false);
    }
}
