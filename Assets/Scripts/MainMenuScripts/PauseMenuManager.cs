using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    private GameObject pauseCanvasGo;
    private GameObject rightPanelContainer;
    
    private GameObject settingsContentGo;
    private GameObject cheatsContentGo;
    
    public static bool isPaused = false;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    // Settings data
    private float masterVolume = 0.8f;
    private float musicVolume = 0.8f;
    private float sfxVolume = 0.8f;
    private float mouseSensitivity = 50f;

    // UI text references for settings
    private TextMeshProUGUI masterValText;
    private TextMeshProUGUI musicValText;
    private TextMeshProUGUI sfxValText;
    private TextMeshProUGUI sensitivityValText;
    private Slider sensitivitySlider;

    private MouseLook _mouseLook;

    // Cheat data (Static so they persist across scene reloads)
    public static float speedMultiplier = 1f;
    public static float jumpMultiplier = 1f;
    public static bool moonGravity = false;
    public static bool ghostMode = false;
    public static bool disableAI = false;

    private TextMeshProUGUI speedValText;
    private TextMeshProUGUI jumpValText;
    private TextMeshProUGUI moonGravityText;
    private TextMeshProUGUI ghostModeText;
    private TextMeshProUGUI disableAIText;

    void Awake()
    {
        isPaused = false;
        _mouseLook = FindAnyObjectByType<MouseLook>();
        LoadSettings();
        BuildPauseMenuUI();
        pauseCanvasGo.SetActive(false);
    }

    void Update()
    {
        // Do NOT allow opening/toggling Pause Menu if Level Complete or Game Over modals are active!
        if (LevelCompleteManager.isLevelCompleteActive || GameOverManager.isGameOverActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (LevelCompleteManager.isLevelCompleteActive || GameOverManager.isGameOverActive) return;

        // Ensure EventSystem is active
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        isPaused = true;
        Time.timeScale = 0f;
        
        // Save previous cursor state
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // Unlock cursor for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        pauseCanvasGo.SetActive(true);
        ShowSettings(); // Default to settings on right panel
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseCanvasGo.SetActive(false);
        SaveSettings(); // Save any changed settings automatically

        // If Level Complete or Game Over modal is active, keep time scale frozen and cursor unlocked!
        if (LevelCompleteManager.isLevelCompleteActive || GameOverManager.isGameOverActive)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Time.timeScale = 1f;

        // Restore cursor state
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    void ShowSettings()
    {
        settingsContentGo.SetActive(true);
        cheatsContentGo.SetActive(false);
        UpdateSettingsUI();
    }

    void ShowCheats()
    {
        settingsContentGo.SetActive(false);
        cheatsContentGo.SetActive(true);
        UpdateCheatsUI();
    }

    // ==========================================
    // UI BUILDER
    // ==========================================
    void BuildPauseMenuUI()
    {
        // Ensure an EventSystem exists in the scene so UI buttons can receive mouse clicks!
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 1. Create Canvas
        pauseCanvasGo = new GameObject("PauseMenuCanvas");
        pauseCanvasGo.transform.SetParent(transform, false);
        Canvas canvas = pauseCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = pauseCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        pauseCanvasGo.AddComponent<GraphicRaycaster>();

        // 2. Dark Background Overlay
        GameObject overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(pauseCanvasGo.transform, false);
        RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.8f);

        // 3. Main Container (Left/Right Layout)
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(pauseCanvasGo.transform, false);
        RectTransform containerRt = containerGo.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(1200f, 700f);
        containerRt.anchoredPosition = Vector2.zero;
        
        Image containerImg = containerGo.AddComponent<Image>();
        containerImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f); // Dark grey

        // 4. Left Panel (Menu Buttons)
        GameObject leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(containerGo.transform, false);
        RectTransform leftRt = leftPanel.AddComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0f, 1f);
        leftRt.sizeDelta = new Vector2(350f, 0f);
        leftRt.anchoredPosition = new Vector2(175f, 0f);

        Image leftImg = leftPanel.AddComponent<Image>();
        leftImg.color = new Color(0.05f, 0.05f, 0.05f, 1f); // Slightly darker for sidebar

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(leftPanel.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(300f, 60f);
        titleRt.anchoredPosition = new Vector2(0f, -60f);
        
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "PAUSED";
        titleTmp.fontSize = 40f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

        // Buttons
        float startY = -160f;
        float spacing = 70f;
        AddMenuButton(leftPanel.transform, "RESUME", startY, ResumeGame);
        AddMenuButton(leftPanel.transform, "RESTART", startY - spacing, RestartLevel);
        AddMenuButton(leftPanel.transform, "SETTINGS", startY - spacing * 2, ShowSettings);
        AddMenuButton(leftPanel.transform, "CHEATS", startY - spacing * 3, ShowCheats);
        AddMenuButton(leftPanel.transform, "MAIN MENU", startY - spacing * 4, LoadMainMenu);

        // 5. Right Panel Container
        rightPanelContainer = new GameObject("RightPanel");
        rightPanelContainer.transform.SetParent(containerGo.transform, false);
        RectTransform rightRt = rightPanelContainer.AddComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        rightRt.offsetMin = new Vector2(350f, 0f); // Starts after left panel
        rightRt.offsetMax = Vector2.zero;

        // Build Right Content Pages
        settingsContentGo = BuildSettingsContent(rightPanelContainer.transform);
        cheatsContentGo = BuildCheatsContent(rightPanelContainer.transform);
        
        ShowSettings(); // Default active
    }

    void AddMenuButton(Transform parent, string label, float yPos, UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(label + "Btn");
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(300f, 50f);
        rt.anchoredPosition = new Vector2(0f, yPos);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);
        
        // Setup Hover Effect colors similar to your MainMenu
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        btnGo.AddComponent<PauseMenuButtonHover>();

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    // ==========================================
    // SETTINGS PAGE
    // ==========================================
    GameObject BuildSettingsContent(Transform parent)
    {
        GameObject pageGo = new GameObject("SettingsContent");
        pageGo.transform.SetParent(parent, false);
        RectTransform rt = pageGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(pageGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, -60f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SETTINGS";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        AddSectionHeader(pageGo.transform, "AUDIO", 150f);
        AddAdjusterRow(pageGo.transform, "MASTER VOLUME", 100f, () => AdjustMasterVolume(-0.1f), () => AdjustMasterVolume(0.1f), out masterValText);
        AddAdjusterRow(pageGo.transform, "MUSIC VOLUME", 40f, () => AdjustMusicVolume(-0.1f), () => AdjustMusicVolume(0.1f), out musicValText);
        AddAdjusterRow(pageGo.transform, "SFX VOLUME", -20f, () => AdjustSFXVolume(-0.1f), () => AdjustSFXVolume(0.1f), out sfxValText);

        AddSectionHeader(pageGo.transform, "CONTROLS", -100f);
        AddSensitivitySliderRow(pageGo.transform, "MOUSE SENSITIVITY", -160f, 1f, 100f, mouseSensitivity, OnSensitivitySliderChanged, out sensitivityValText);

        return pageGo;
    }

    // ==========================================
    // CHEATS PAGE
    // ==========================================
    GameObject BuildCheatsContent(Transform parent)
    {
        GameObject pageGo = new GameObject("CheatsContent");
        pageGo.transform.SetParent(parent, false);
        RectTransform rt = pageGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(pageGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, -60f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CHEATS";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        AddSectionHeader(pageGo.transform, "GAMEPLAY MODIFIERS", 150f);
        AddAdjusterRow(pageGo.transform, "PLAYER SPEED", 100f, () => AdjustSpeedMultiplier(-0.25f), () => AdjustSpeedMultiplier(0.25f), out speedValText);
        AddAdjusterRow(pageGo.transform, "JUMP SIZE", 50f, () => AdjustJumpMultiplier(-0.25f), () => AdjustJumpMultiplier(0.25f), out jumpValText);
        
        AddToggleRow(pageGo.transform, "MOON GRAVITY", 0f, ToggleMoonGravity, out moonGravityText);
        AddToggleRow(pageGo.transform, "GHOST MODE", -50f, ToggleGhostMode, out ghostModeText);
        AddToggleRow(pageGo.transform, "DISABLE AI-CHASING", -100f, ToggleDisableAI, out disableAIText);

        AddCheatsButton(pageGo.transform, "SKIP LEVEL", -175f, SkipLevel);

        UpdateCheatsUI();

        return pageGo;
    }

    void AddToggleRow(Transform parent, string labelText, float yPos, UnityAction onToggle, out TextMeshProUGUI valueTextRef)
    {
        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(parent, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(300f, 30f);
        labelRt.anchoredPosition = new Vector2(-150f, yPos);
        
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.white;

        GameObject btnGo = new GameObject(labelText + "_Btn");
        btnGo.transform.SetParent(parent, false);
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(120f, 40f);
        btnRt.anchoredPosition = new Vector2(150f, yPos);
        
        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onToggle);
        btnGo.AddComponent<PauseMenuButtonHover>();

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        valueTextRef = textGo.AddComponent<TextMeshProUGUI>();
        valueTextRef.text = "OFF";
        valueTextRef.fontSize = 20f;
        valueTextRef.fontStyle = FontStyles.Bold;
        valueTextRef.alignment = TextAlignmentOptions.Center;
        valueTextRef.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    void AddCheatsButton(Transform parent, string label, float yPos, UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(label + "Btn");
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(250f, 45f);
        rt.anchoredPosition = new Vector2(0f, yPos);

        Image bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);
        
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        btnGo.AddComponent<PauseMenuButtonHover>();

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
        tmp.color = Color.white;
    }

    void AdjustSpeedMultiplier(float delta)
    {
        speedMultiplier = Mathf.Clamp(speedMultiplier + delta, 0.5f, 3f);
        UpdateCheatsUI();
    }

    void AdjustJumpMultiplier(float delta)
    {
        jumpMultiplier = Mathf.Clamp(jumpMultiplier + delta, 0.5f, 3f);
        UpdateCheatsUI();
    }

    void ToggleMoonGravity()
    {
        moonGravity = !moonGravity;
        UpdateCheatsUI();
    }

    void ToggleGhostMode()
    {
        ghostMode = !ghostMode;
        
        // Apply collision change instantly to player's CharacterController
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.detectCollisions = !ghostMode;
            }
        }

        UpdateCheatsUI();
    }

    void ToggleDisableAI()
    {
        disableAI = !disableAI;
        UpdateCheatsUI();
    }

    void SkipLevel()
    {
        Time.timeScale = 1f;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Level 1")
        {
            SceneManager.LoadScene("Level 2");
        }
        else if (currentScene == "Level 2")
        {
            SceneManager.LoadScene("Level 3");
        }
        else if (currentScene == "Level 3")
        {
            SceneManager.LoadScene("Ending");
        }

        if (pauseCanvasGo != null)
        {
            pauseCanvasGo.SetActive(false);
        }
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void UpdateCheatsUI()
    {
        if (speedValText != null)
        {
            speedValText.text = speedMultiplier.ToString("F2") + "x";
        }
        if (jumpValText != null)
        {
            jumpValText.text = jumpMultiplier.ToString("F2") + "x";
        }
        if (moonGravityText != null)
        {
            moonGravityText.text = moonGravity ? "ON" : "OFF";
            moonGravityText.color = moonGravity ? new Color(0.91f, 0.44f, 0.08f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (ghostModeText != null)
        {
            ghostModeText.text = ghostMode ? "ON" : "OFF";
            ghostModeText.color = ghostMode ? new Color(0.91f, 0.44f, 0.08f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (disableAIText != null)
        {
            disableAIText.text = disableAI ? "ON" : "OFF";
            disableAIText.color = disableAI ? new Color(0.91f, 0.44f, 0.08f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    // ==========================================
    // REUSABLE SETTINGS COMPONENTS
    // ==========================================
    void AddSectionHeader(Transform parent, string sectionName, float yPos)
    {
        GameObject headerGo = new GameObject(sectionName + "_Header");
        headerGo.transform.SetParent(parent, false);
        RectTransform rt = headerGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700f, 30f);
        rt.anchoredPosition = new Vector2(0f, yPos);

        GameObject leftLineGo = new GameObject("LeftLine");
        leftLineGo.transform.SetParent(headerGo.transform, false);
        RectTransform leftLineRt = leftLineGo.AddComponent<RectTransform>();
        leftLineRt.anchorMin = new Vector2(0.5f, 0.5f);
        leftLineRt.anchorMax = new Vector2(0.5f, 0.5f);
        leftLineRt.sizeDelta = new Vector2(50f, 2f);
        leftLineRt.anchoredPosition = new Vector2(-300f, 0f);
        Image leftLineImg = leftLineGo.AddComponent<Image>();
        leftLineImg.color = new Color(0.91f, 0.44f, 0.08f, 0.7f);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(headerGo.transform, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        float labelWidth = 150f;
        labelRt.sizeDelta = new Vector2(labelWidth, 30f);
        labelRt.anchoredPosition = new Vector2(-200f, 0f);
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = sectionName;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.91f, 0.44f, 0.08f, 0.9f);

        GameObject rightLineGo = new GameObject("RightLine");
        rightLineGo.transform.SetParent(headerGo.transform, false);
        RectTransform rightLineRt = rightLineGo.AddComponent<RectTransform>();
        rightLineRt.anchorMin = new Vector2(0.5f, 0.5f);
        rightLineRt.anchorMax = new Vector2(0.5f, 0.5f);
        rightLineRt.sizeDelta = new Vector2(400f, 2f);
        rightLineRt.anchoredPosition = new Vector2(100f, 0f);
        Image rightLineImg = rightLineGo.AddComponent<Image>();
        rightLineImg.color = new Color(0.91f, 0.44f, 0.08f, 0.7f);
    }

    void AddAdjusterRow(Transform parent, string labelText, float yPos, UnityAction onMinus, UnityAction onPlus, out TextMeshProUGUI valueTextRef)
    {
        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(parent, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(250f, 30f);
        labelRt.anchoredPosition = new Vector2(-150f, yPos);
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.white;

        GameObject minusGo = new GameObject(labelText + "_MinusBtn");
        minusGo.transform.SetParent(parent, false);
        RectTransform minusRt = minusGo.AddComponent<RectTransform>();
        minusRt.anchorMin = new Vector2(0.5f, 0.5f);
        minusRt.anchorMax = new Vector2(0.5f, 0.5f);
        minusRt.sizeDelta = new Vector2(40f, 30f);
        minusRt.anchoredPosition = new Vector2(30f, yPos);
        Image minusImg = minusGo.AddComponent<Image>();
        minusImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button minusBtn = minusGo.AddComponent<Button>();
        minusBtn.onClick.AddListener(onMinus);
        minusGo.AddComponent<PauseMenuButtonHover>();

        GameObject minusTextGo = new GameObject("Label");
        minusTextGo.transform.SetParent(minusGo.transform, false);
        RectTransform mtRt = minusTextGo.AddComponent<RectTransform>();
        mtRt.anchorMin = Vector2.zero;
        mtRt.anchorMax = Vector2.one;
        mtRt.offsetMin = Vector2.zero;
        mtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI mtTmp = minusTextGo.AddComponent<TextMeshProUGUI>();
        mtTmp.text = "-";
        mtTmp.fontSize = 22f;
        mtTmp.alignment = TextAlignmentOptions.Center;
        mtTmp.color = Color.white;

        GameObject valGo = new GameObject(labelText + "_ValText");
        valGo.transform.SetParent(parent, false);
        RectTransform valRt = valGo.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0.5f);
        valRt.anchorMax = new Vector2(0.5f, 0.5f);
        valRt.sizeDelta = new Vector2(150f, 30f);
        valRt.anchoredPosition = new Vector2(140f, yPos);
        valueTextRef = valGo.AddComponent<TextMeshProUGUI>();
        valueTextRef.text = "...";
        valueTextRef.fontSize = 24f;
        valueTextRef.alignment = TextAlignmentOptions.Center;
        valueTextRef.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        GameObject plusGo = new GameObject(labelText + "_PlusBtn");
        plusGo.transform.SetParent(parent, false);
        RectTransform plusRt = plusGo.AddComponent<RectTransform>();
        plusRt.anchorMin = new Vector2(0.5f, 0.5f);
        plusRt.anchorMax = new Vector2(0.5f, 0.5f);
        plusRt.sizeDelta = new Vector2(40f, 30f);
        plusRt.anchoredPosition = new Vector2(250f, yPos);
        Image plusImg = plusGo.AddComponent<Image>();
        plusImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button plusBtn = plusGo.AddComponent<Button>();
        plusBtn.onClick.AddListener(onPlus);
        plusGo.AddComponent<PauseMenuButtonHover>();

        GameObject plusTextGo = new GameObject("Label");
        plusTextGo.transform.SetParent(plusGo.transform, false);
        RectTransform ptRt = plusTextGo.AddComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero;
        ptRt.anchorMax = Vector2.one;
        ptRt.offsetMin = Vector2.zero;
        ptRt.offsetMax = Vector2.zero;
        TextMeshProUGUI ptTmp = plusTextGo.AddComponent<TextMeshProUGUI>();
        ptTmp.text = "+";
        ptTmp.fontSize = 22f;
        ptTmp.alignment = TextAlignmentOptions.Center;
        ptTmp.color = Color.white;
    }

    void AddSensitivitySliderRow(Transform parent, string labelText, float yPos, float minVal, float maxVal, float defaultVal, UnityAction<float> onValueChanged, out TextMeshProUGUI valueTextRef)
    {
        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(parent, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(250f, 30f);
        labelRt.anchoredPosition = new Vector2(-150f, yPos);
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.white;

        sensitivitySlider = CreateSlider(parent, new Vector2(200f, 30f), new Vector2(100f, yPos), minVal, maxVal, defaultVal, onValueChanged);

        GameObject valGo = new GameObject(labelText + "_ValText");
        valGo.transform.SetParent(parent, false);
        RectTransform valRt = valGo.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0.5f);
        valRt.anchorMax = new Vector2(0.5f, 0.5f);
        valRt.sizeDelta = new Vector2(40f, 30f);
        valRt.anchoredPosition = new Vector2(240f, yPos);
        valueTextRef = valGo.AddComponent<TextMeshProUGUI>();
        valueTextRef.text = Mathf.RoundToInt(defaultVal).ToString();
        valueTextRef.fontSize = 20f;
        valueTextRef.alignment = TextAlignmentOptions.Center;
        valueTextRef.color = new Color(0.91f, 0.44f, 0.08f, 1f);
    }

    Slider CreateSlider(Transform parent, Vector2 size, Vector2 position, float minVal, float maxVal, float defaultVal, UnityAction<float> onValueChanged)
    {
        GameObject sliderGo = new GameObject("SensitivitySlider");
        sliderGo.transform.SetParent(parent, false);
        RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.sizeDelta = size;
        sliderRt.anchoredPosition = position;

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = true;
        slider.transition = Selectable.Transition.None;

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.sizeDelta = new Vector2(0f, 8f);
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.sizeDelta = new Vector2(0f, 8f);
        fillAreaRt.anchoredPosition = Vector2.zero;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        GameObject handleSlideAreaGo = new GameObject("Handle Slide Area");
        handleSlideAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform handleSlideAreaRt = handleSlideAreaGo.AddComponent<RectTransform>();
        handleSlideAreaRt.anchorMin = new Vector2(0f, 0f);
        handleSlideAreaRt.anchorMax = new Vector2(1f, 1f);
        handleSlideAreaRt.offsetMin = Vector2.zero;
        handleSlideAreaRt.offsetMax = Vector2.zero;

        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleSlideAreaGo.transform, false);
        RectTransform handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.offsetMin = new Vector2(-15f, 0f);
        handleRt.offsetMax = new Vector2(15f, 0f);
        Image handleImg = handleGo.AddComponent<Image>();
        
        // Generate and assign the circle sprite programmatically
        handleImg.sprite = CreateCircleSprite();
        handleImg.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.minValue = minVal;
        slider.maxValue = maxVal;
        slider.value = defaultVal;
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
    }

    Sprite CreateCircleSprite()
    {
        int radius = 32;
        int diameter = radius * 2;
        Texture2D texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
        
        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;
        
        float rSquared = radius * radius;
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float distSquared = dx * dx + dy * dy;
                
                if (distSquared <= rSquared)
                {
                    // Smooth edges (anti-aliasing)
                    float dist = Mathf.Sqrt(distSquared);
                    float edgeWidth = 1.5f;
                    if (radius - dist < edgeWidth)
                    {
                        float alpha = (radius - dist) / edgeWidth;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0f, 0f, diameter, diameter), new Vector2(0.5f, 0.5f));
    }

    // ==========================================
    // SETTINGS LOGIC
    // ==========================================
    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 50f);

        // Apply immediately in GameAudioManager programmatically
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.LoadVolumeSettings();
        }
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.Save();

        // Apply updated volumes programmatically in GameAudioManager
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.LoadVolumeSettings();
        }
    }

    void UpdateSettingsUI()
    {
        if (masterValText != null) masterValText.text = GetVolumeBar(masterVolume) + " " + Mathf.RoundToInt(masterVolume * 100f) + "%";
        if (musicValText != null) musicValText.text = GetVolumeBar(musicVolume) + " " + Mathf.RoundToInt(musicVolume * 100f) + "%";
        if (sfxValText != null) sfxValText.text = GetVolumeBar(sfxVolume) + " " + Mathf.RoundToInt(sfxVolume * 100f) + "%";
        
        if (sensitivitySlider != null && sensitivitySlider.value != mouseSensitivity)
        {
            sensitivitySlider.value = mouseSensitivity;
        }
        if (sensitivityValText != null)
        {
            sensitivityValText.text = Mathf.RoundToInt(mouseSensitivity).ToString();
        }
    }

    string GetVolumeBar(float val)
    {
        int blocks = Mathf.RoundToInt(val * 10f);
        string bar = "";
        for (int i = 0; i < 10; i++)
        {
            bar += (i < blocks) ? "■" : "□";
        }
        return bar;
    }

    void AdjustMasterVolume(float delta)
    {
        masterVolume = Mathf.Clamp01(masterVolume + delta);
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetTemporaryMasterVolume(masterVolume); // Instant change
        }
        UpdateSettingsUI();
    }
    void AdjustMusicVolume(float delta)
    {
        musicVolume = Mathf.Clamp01(musicVolume + delta);
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetTemporaryMusicVolume(musicVolume); // Instant change
        }
        UpdateSettingsUI();
    }
    void AdjustSFXVolume(float delta)
    {
        sfxVolume = Mathf.Clamp01(sfxVolume + delta);
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetTemporarySFXVolume(sfxVolume); // Instant change
        }
        UpdateSettingsUI();
    }
    void OnSensitivitySliderChanged(float value)
    {
        mouseSensitivity = value;
        if (sensitivityValText != null)
            sensitivityValText.text = Mathf.RoundToInt(value).ToString();
        if (_mouseLook != null)
            _mouseLook.mouseSensitivity = value * 2f;
    }
}
