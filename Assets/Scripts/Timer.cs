using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Timer : MonoBehaviour
{
    public enum TimerMode { CountUp, CountDown }

    [Header("Timer Settings")]
    [Tooltip("CountUp counts up from 0 (like a stopwatch). CountDown counts down from the starting time to 0.")]
    public TimerMode mode = TimerMode.CountDown;
    
    [Tooltip("Starting time in seconds (only used in CountDown mode).")]
    public float startingTime = 60f;
    
    [Tooltip("Should the timer start ticking automatically when the scene loads?")]
    public bool playOnAwake = true;
    
    [Tooltip("Should milliseconds be displayed in the UI?")]
    public bool showMilliseconds = false;

    [Header("UI Reference (Optional)")]
    [Tooltip("If left empty, a beautiful HUD panel and text will be created automatically at the top center of the screen.")]
    public TextMeshProUGUI timerText;

    [Header("Events")]
    [Tooltip("Actions/Events to trigger when the countdown timer reaches 0.")]
    public UnityEvent onTimerFinished;

    private float currentTime;
    private bool isRunning = false;
    private GameObject dynamicCanvasGo;

    /// <summary>
    /// Gets the current time remaining (or elapsed) in seconds.
    /// </summary>
    public float CurrentTime => currentTime;

    /// <summary>
    /// Checks if the timer is currently active and ticking.
    /// </summary>
    public bool IsRunning => isRunning;

    void Awake()
    {
        // Initialize timer starting value
        currentTime = mode == TimerMode.CountDown ? startingTime : 0f;

        // If no text component was dragged in, auto-build a clean, floating HUD panel at runtime
        if (timerText == null)
        {
            BuildTimerUI();
        }
    }

    void Start()
    {
        if (playOnAwake)
        {
            StartTimer();
        }
    }

    void OnDestroy()
    {
        if (dynamicCanvasGo != null)
        {
            Destroy(dynamicCanvasGo);
        }
    }

    void Update()
    {
        if (!isRunning) return;

        if (mode == TimerMode.CountDown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isRunning = false;
                UpdateText();
                onTimerFinished?.Invoke();
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        UpdateText();
    }

    /// <summary>
    /// Starts or resumes the timer.
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// Pauses the timer, preserving the current time.
    /// </summary>
    public void PauseTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Resumes the timer after it was paused.
    /// </summary>
    public void ResumeTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Resets the timer to its initial state.
    /// </summary>
    public void ResetTimer()
    {
        currentTime = mode == TimerMode.CountDown ? startingTime : 0f;
        UpdateText();
    }

    /// <summary>
    /// Adds seconds to the timer (useful for timer bonuses or pickups).
    /// </summary>
    public void AddTime(float amount)
    {
        currentTime += amount;
        UpdateText();
    }

    /// <summary>
    /// Subtracts seconds from the timer (useful for hazard or obstacle penalties).
    /// </summary>
    public void SubtractTime(float amount)
    {
        currentTime = Mathf.Max(0f, currentTime - amount);
        UpdateText();
        
        if (mode == TimerMode.CountDown && currentTime <= 0f)
        {
            isRunning = false;
            onTimerFinished?.Invoke();
        }
    }

    // Formats and displays the current time on the text component
    private void UpdateText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (showMilliseconds)
        {
            int milliseconds = Mathf.FloorToInt((currentTime % 1f) * 100f);
            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
        else
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // Programmatically builds the UI elements so the user doesn't have to manually configure UI objects in the editor hierarchy
    private void BuildTimerUI()
    {
        // 1. Create a Screen Space Canvas
        dynamicCanvasGo = new GameObject("DynamicTimerCanvas");
        Canvas canvas = dynamicCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // Renders overlay above game elements but behind top-level pause overlays

        CanvasScaler scaler = dynamicCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Create the Background Panel container
        GameObject panelGo = new GameObject("TimerPanel");
        panelGo.transform.SetParent(dynamicCanvasGo.transform, false);
        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f); // Anchored at the top center of the screen
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.sizeDelta = new Vector2(220f, 65f);
        panelRt.anchoredPosition = new Vector2(0f, -25f); // 25 units down from the top edge

        // 3. Create the TextMeshPro text object
        GameObject textGo = new GameObject("TimerText");
        textGo.transform.SetParent(panelGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        timerText = textGo.AddComponent<TextMeshProUGUI>();
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontSize = 32f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Bold orange color accent used in the UI
        
        UpdateText();
    }
}
