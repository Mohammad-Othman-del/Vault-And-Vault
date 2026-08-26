using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("Character portrait Image (the UI Image component on the portrait object)")]
    public Image portraitImage;
    [Tooltip("Root GameObject of the portrait — hidden when no portrait is set")]
    public GameObject portraitObject;

    [Header("Settings")]
    [Tooltip("Characters per second for typewriter effect (0 = instant)")]
    public float typewriterSpeed = 40f;
    [Tooltip("Key to advance or skip dialogue")]
    public KeyCode advanceKey = KeyCode.E;
    [Tooltip("Seconds each portrait is shown before switching to the other (0 = no switching)")]
    public float portraitSwitchInterval = 0.5f;

    public static bool IsActive { get; private set; }

    private DialogueLine[] _lines;
    private int _currentIndex;
    private Coroutine _typewriterRoutine;
    private Coroutine _portraitSwitchRoutine;
    private bool _lineFinished;
    private System.Action _onEndCallback;

    private AudioSource _dialogueAudioSource;
    private AudioClip _dialogueAudioClip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsActive = false; // Reset static state so dialogue works upon rejoining levels from Main Menu!

        if (dialoguePanel) dialoguePanel.SetActive(false);

        // Setup AudioSource for dialogue speech loop
        _dialogueAudioSource = gameObject.AddComponent<AudioSource>();
        _dialogueAudioSource.loop = true;
        _dialogueAudioSource.playOnAwake = false;
        _dialogueAudioSource.spatialBlend = 0f; // 2D sound

        // Load the dialogue sound effect (dialogue.mp3)
        _dialogueAudioClip = Resources.Load<AudioClip>("Audio/Dialogue/dialogue");
        if (_dialogueAudioClip == null)
        {
            Debug.LogWarning("DialogueManager: Could not find 'dialogue' audio clip in Resources/Audio/Dialogue/");
        }
    }

    void Update()
    {
        // Scale dialogue volume directly with Master Volume * SFX Volume (1.0x full multiplier) and even higher pitch (1.35x)
        if (_dialogueAudioSource != null)
        {
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            _dialogueAudioSource.volume = masterVol * sfxVol * 1.0f;
            _dialogueAudioSource.pitch = 1.35f;
        }

        // Pause dialogue audio when game is paused or time is frozen
        if (Time.timeScale == 0f || PauseMenuManager.isPaused)
        {
            if (_dialogueAudioSource != null && _dialogueAudioSource.isPlaying)
            {
                _dialogueAudioSource.Pause();
            }
            return;
        }

        // Stop dialogue audio completely if dialogue is inactive
        if (!IsActive)
        {
            if (_dialogueAudioSource != null && _dialogueAudioSource.isPlaying)
            {
                _dialogueAudioSource.Stop();
            }
            return;
        }

        // Resume dialogue audio when unpaused if text is still actively typing out
        if (IsActive && !_lineFinished && _dialogueAudioSource != null && _dialogueAudioClip != null)
        {
            if (!_dialogueAudioSource.isPlaying)
            {
                _dialogueAudioSource.UnPause();
                if (!_dialogueAudioSource.isPlaying)
                {
                    _dialogueAudioSource.clip = _dialogueAudioClip;
                    _dialogueAudioSource.Play();
                }
            }
        }

        if (Input.GetKeyDown(advanceKey))
        {
            if (!_lineFinished)
                FinishLineInstantly();
            else
                ShowNextLine();
        }
    }

    public void StartDialogue(DialogueLine[] lines, System.Action onEnd = null)
    {
        if (IsActive) return;

        _lines = lines;
        _currentIndex = 0;
        _onEndCallback = onEnd;
        IsActive = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (continuePrompt) continuePrompt.SetActive(false);

        // Start looping the dialogue sound effect while dialogue is open
        if (_dialogueAudioSource != null && _dialogueAudioClip != null)
        {
            _dialogueAudioSource.clip = _dialogueAudioClip;
            _dialogueAudioSource.Play();
        }

        ShowNextLine();
    }

    void ShowNextLine()
    {
        if (_currentIndex >= _lines.Length) { EndDialogue(); return; }

        DialogueLine line = _lines[_currentIndex++];

        if (nameText)
            nameText.text = line.speakerName;

        // Portrait: start switching coroutine when at least one sprite is assigned
        if (_portraitSwitchRoutine != null) StopCoroutine(_portraitSwitchRoutine);
        if (line.portrait != null)
        {
            if (portraitObject) portraitObject.SetActive(true);
            if (portraitImage) portraitImage.sprite = line.portrait;
            if (line.portrait2 != null && portraitSwitchInterval > 0f)
                _portraitSwitchRoutine = StartCoroutine(SwitchPortraits(line.portrait, line.portrait2));
        }
        else
        {
            if (portraitObject) portraitObject.SetActive(false);
        }

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

            // Play voice audio while text is typing out
            if (_dialogueAudioSource != null && _dialogueAudioClip != null)
            {
                _dialogueAudioSource.clip = _dialogueAudioClip;
                if (!_dialogueAudioSource.isPlaying) _dialogueAudioSource.Play();
            }

            _typewriterRoutine = StartCoroutine(Typewriter(line.text));
        }
    }

    IEnumerator SwitchPortraits(Sprite first, Sprite second)
    {
        bool showingFirst = true;
        while (true)
        {
            yield return new WaitForSeconds(portraitSwitchInterval);
            showingFirst = !showingFirst;
            if (portraitImage) portraitImage.sprite = showingFirst ? first : second;
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

        // Stop the dialogue sound effect as soon as all text is loaded!
        if (_dialogueAudioSource != null && _dialogueAudioSource.isPlaying)
        {
            _dialogueAudioSource.Stop();
        }
    }

    void EndDialogue()
    {
        if (_portraitSwitchRoutine != null) { StopCoroutine(_portraitSwitchRoutine); _portraitSwitchRoutine = null; }
        
        // Stop looping dialogue sound effect when dialogue ends/closes
        if (_dialogueAudioSource != null && _dialogueAudioSource.isPlaying)
        {
            _dialogueAudioSource.Stop();
        }

        IsActive = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continuePrompt) continuePrompt.SetActive(false);

        System.Action callback = _onEndCallback;
        _onEndCallback = null;
        callback?.Invoke();
    }

    void OnDisable()
    {
        IsActive = false;
        if (_dialogueAudioSource != null && _dialogueAudioSource.isPlaying)
        {
            _dialogueAudioSource.Stop();
        }
    }
}
