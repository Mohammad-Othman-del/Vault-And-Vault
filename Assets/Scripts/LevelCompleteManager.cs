using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class LevelCompleteManager : MonoBehaviour
{
    [Header("Rank Thresholds (Seconds)")]
    [Tooltip("Time limit to get an S rank (under 95 seconds / 1:35).")]
    public float timeForS = 95f;

    [Tooltip("Time limit to get an A rank (under 115 seconds / 1:55).")]
    public float timeForA = 115f;

    [Tooltip("Time limit to get a B rank (under 135 seconds / 2:15).")]
    public float timeForB = 135f;

    [Tooltip("Time limit to get a C rank (under 155 seconds / 2:35).")]
    public float timeForC = 155f;

    [Header("UI Customization")]
    [Tooltip("Background color of the popup panel.")]
    public Color panelColor = new Color(0.04f, 0.05f, 0.08f, 0.96f);

    [Tooltip("Accent color used for borders, title text, highlights, and button selections.")]
    public Color accentColor = new Color(0f, 0.85f, 1f, 1f);

    [Tooltip("Text color of the total time text and buttons.")]
    public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);

    [Tooltip("Background color of the buttons (idle state).")]
    public Color buttonColor = new Color(0.08f, 0.12f, 0.2f, 1f);

    public static bool isLevelCompleteActive = false;

    private GameObject levelCompleteCanvasGo;
    private string nextSceneName;
    
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    void Awake()
    {
        isLevelCompleteActive = false;
    }

    void OnDestroy()
    {
        isLevelCompleteActive = false;
    }

    /// <summary>
    /// Stops the timer, pauses the game, calculates the player's rank, and displays the Level Complete screen.
    /// </summary>
    /// <param name="targetScene">The name of the next scene to load when clicking Continue.</param>
    public void ShowLevelComplete(string targetScene)
    {
        isLevelCompleteActive = true;

        // Level 3 Rank Adjustments (S = 1:35 / 95s with tight 20-second steps)
        if (SceneManager.GetActiveScene().name.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            timeForS = 95f;  // <= 95 sec (1 min 35 sec)
            timeForA = 115f; // <= 115 sec (1 min 55 sec)
            timeForB = 135f; // <= 135 sec (2 min 15 sec)
            timeForC = 155f; // <= 155 sec (2 min 35 sec)
        }
        // Play level complete congrats SFX
        // 0. Disable Chasing AI Bot completely upon level victory!
        RubberBandBot.isLevelFinished = true;
        RubberBandBot bot = FindAnyObjectByType<RubberBandBot>();
        if (bot != null)
        {
            bot.DisableBotOnVictory();
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopChaseBGM();
            GameAudioManager.Instance.PlayVictorySFX();
        }

        nextSceneName = targetScene;

        // 1. Find and pause the timer in the scene
        Timer timer = FindAnyObjectByType<Timer>();
        float finalTime = 0f;
        if (timer != null)
        {
            timer.PauseTimer();
            finalTime = timer.CurrentTime;
        }
        else
        {
            // Fallback if no timer is found
            finalTime = Time.timeSinceLevelLoad;
        }

        // 2. Pause the game
        Time.timeScale = 0f;

        // 3. Save and unlock cursor
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Calculate Rank
        string rank = CalculateRank(finalTime, out Color rankColor);

        // 5. Build and show the UI Screen
        BuildLevelCompleteUI(finalTime, rank, rankColor);
    }

    private string CalculateRank(float time, out Color rankColor)
    {
        if (time <= timeForS)
        {
            rankColor = new Color(1f, 0.85f, 0f); // Brilliant Gold
            return "S";
        }
        if (time <= timeForA)
        {
            rankColor = new Color(0.88f, 0.9f, 0.95f); // Ice Silver
            return "A";
        }
        if (time <= timeForB)
        {
            rankColor = new Color(1f, 0.55f, 0.25f); // Neon Bronze/Orange
            return "B";
        }
        if (time <= timeForC)
        {
            rankColor = new Color(0f, 0.8f, 1f); // Electric Cyan/Blue
            return "C";
        }
        
        rankColor = new Color(1f, 0.25f, 0.25f); // Neon Red
        return "F";
    }

    private void BuildLevelCompleteUI(float time, string rank, Color rankColor)
    {
        // Ensure EventSystem exists in the scene so UI buttons can receive mouse clicks!
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 1. Create Canvas
        levelCompleteCanvasGo = new GameObject("LevelCompleteCanvas");
        Canvas canvas = levelCompleteCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // Just under pause menu (100)

        CanvasScaler scaler = levelCompleteCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        levelCompleteCanvasGo.AddComponent<GraphicRaycaster>();

        // 2. Dark Background Overlay
        GameObject overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(levelCompleteCanvasGo.transform, false);
        RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.85f); // Slightly darker overlay

        // 3. Center Popup Container
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(levelCompleteCanvasGo.transform, false);
        RectTransform containerRt = containerGo.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(650f, 550f);
        containerRt.anchoredPosition = Vector2.zero;

        Image containerImg = containerGo.AddComponent<Image>();
        containerImg.color = panelColor; // Matches custom panel color

        // Add a clean Outline component around the container (prevents solid color bleed behind it)
        Outline outline = containerGo.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.8f);
        outline.effectDistance = new Vector2(4f, -4f); // Draws outline around the panel border

        // 4. Header: LEVEL COMPLETED!
        GameObject headerGo = new GameObject("Header");
        headerGo.transform.SetParent(containerGo.transform, false);
        RectTransform headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(500f, 60f);
        headerRt.anchoredPosition = new Vector2(0f, -60f);

        TextMeshProUGUI headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
        headerTmp.text = "LEVEL COMPLETED";
        headerTmp.fontSize = 42f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = accentColor;

        // 5. Total Time Display
        GameObject timeGo = new GameObject("TotalTime");
        timeGo.transform.SetParent(containerGo.transform, false);
        RectTransform timeRt = timeGo.AddComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0.5f, 0.5f);
        timeRt.anchorMax = new Vector2(0.5f, 0.5f);
        timeRt.sizeDelta = new Vector2(500f, 40f);
        timeRt.anchoredPosition = new Vector2(0f, 80f);

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
        
        string hexColor = ColorUtility.ToHtmlStringRGBA(accentColor);
        TextMeshProUGUI timeTmp = timeGo.AddComponent<TextMeshProUGUI>();
        timeTmp.text = string.Format("TOTAL TIME: <color=#{0}>{1:00}:{2:00}.{3:00}</color>", hexColor, minutes, seconds, milliseconds);
        timeTmp.fontSize = 28f;
        timeTmp.alignment = TextAlignmentOptions.Center;
        timeTmp.color = textColor;

        // 6. Rank Display
        GameObject rankLabelGo = new GameObject("RankLabel");
        rankLabelGo.transform.SetParent(containerGo.transform, false);
        RectTransform rlRt = rankLabelGo.AddComponent<RectTransform>();
        rlRt.anchorMin = new Vector2(0.5f, 0.5f);
        rlRt.anchorMax = new Vector2(0.5f, 0.5f);
        rlRt.sizeDelta = new Vector2(500f, 40f);
        rlRt.anchoredPosition = new Vector2(0f, 10f);

        TextMeshProUGUI rlTmp = rankLabelGo.AddComponent<TextMeshProUGUI>();
        rlTmp.text = "CLEAR RANK";
        rlTmp.fontSize = 20f;
        rlTmp.alignment = TextAlignmentOptions.Center;
        rlTmp.color = new Color(textColor.r, textColor.g, textColor.b, 0.6f);

        GameObject rankGo = new GameObject("RankText");
        rankGo.transform.SetParent(containerGo.transform, false);
        RectTransform rankRt = rankGo.AddComponent<RectTransform>();
        rankRt.anchorMin = new Vector2(0.5f, 0.5f);
        rankRt.anchorMax = new Vector2(0.5f, 0.5f);
        rankRt.sizeDelta = new Vector2(300f, 130f);
        rankRt.anchoredPosition = new Vector2(0f, -70f);

        TextMeshProUGUI rankTmp = rankGo.AddComponent<TextMeshProUGUI>();
        rankTmp.text = rank;
        rankTmp.fontSize = 110f;
        rankTmp.fontStyle = FontStyles.Bold;
        rankTmp.alignment = TextAlignmentOptions.Center;
        rankTmp.color = rankColor;

        // 7. Buttons Setup
        float buttonY = -180f;
        float buttonSpacing = 150f;

        CreateButton(containerGo.transform, "RESTART", new Vector2(-buttonSpacing, buttonY), RestartLevel);
        CreateButton(containerGo.transform, "CONTINUE", new Vector2(buttonSpacing, buttonY), ContinueToNextLevel);
    }

    private void CreateButton(Transform parent, string label, Vector2 position, UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(label + "Btn");
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 55f);
        rt.anchoredPosition = position;

        Image bg = btnGo.AddComponent<Image>();
        bg.color = buttonColor;

        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);

        // Highlight colors
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = accentColor;
        colors.pressedColor = new Color(accentColor.r * 0.7f, accentColor.g * 0.7f, accentColor.b * 0.7f, 1f);
        btn.colors = colors;

        // Setup hover sound effect component if PauseMenuButtonHover is general-purpose
        PauseMenuButtonHover hover = btnGo.AddComponent<PauseMenuButtonHover>();

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
    }

    private void ContinueToNextLevel()
    {
        isLevelCompleteActive = false;

        // Restore time scale
        Time.timeScale = 1f;

        // Clean up UI Canvas
        if (levelCompleteCanvasGo != null)
        {
            Destroy(levelCompleteCanvasGo);
        }

        // Stop the chase music & emergency alarm
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopChaseBGM();
            GameAudioManager.Instance.StopEmergencyAlarm();
        }

        // Disable all LevelTransitionTriggers to prevent infinite E key presses or re-triggering
        LevelTransitionTrigger[] triggers = FindObjectsByType<LevelTransitionTrigger>(FindObjectsInactive.Include);
        foreach (var tr in triggers)
        {
            tr.enabled = false;
            Collider col = tr.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // Disable DialogueManager
        DialogueManager dm = FindAnyObjectByType<DialogueManager>();
        if (dm != null) dm.enabled = false;

        // Disable player keyboard and mouse inputs (movement, parkour, look)
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.FreezeForCinematic();
            pm.enabled = false;
        }

        MouseLook ml = FindAnyObjectByType<MouseLook>();
        if (ml != null)
        {
            ml.enabled = false;
        }

        // Lock & hide cursor to deny all mouse click inputs
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If target is "Ending" or active scene is "Level 3", load Ending scene directly without showing the Loading scene!
        string currentScene = SceneManager.GetActiveScene().name;
        if (nextSceneName.Equals("Ending", System.StringComparison.OrdinalIgnoreCase) || currentScene.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Application.CanStreamedLevelBeLoaded("Ending"))
            {
                SceneManager.LoadScene("Ending");
            }
            else
            {
                // Fallback if Ending.unity is not added to Build Settings: spawn EndingSceneManager directly!
                GameObject endingGo = new GameObject("EndingSceneManager");
                endingGo.AddComponent<EndingSceneManager>();
            }
        }
        else
        {
            // Trigger transition via standard loading screen for other levels
            LoadingData.TargetScene = nextSceneName;
            SceneManager.LoadScene("Loading");
        }
    }

    private void RestartLevel()
    {
        // Restore time scale
        Time.timeScale = 1f;

        // Clean up UI Canvas
        if (levelCompleteCanvasGo != null)
        {
            Destroy(levelCompleteCanvasGo);
        }

        // Stop the chase music
        GameAudioManager.Instance.StopChaseBGM();

        // Reload active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
