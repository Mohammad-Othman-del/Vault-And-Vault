using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
public class MainMenuManager : MonoBehaviour
{
    private GameObject playBtnGo;
    private GameObject howToPlayBtnGo;
    private GameObject levelsBtnGo;
    private GameObject settingsBtnGo;
    private GameObject creditsBtnGo;
    private GameObject quitBtnGo;

    private GameObject levelSelectPanel;
    private GameObject settingsPanel;
    private GameObject howToPlayPanel;
    private GameObject creditsPanel;

    // Settings data (defaults or loaded from PlayerPrefs)
    private float masterVolume = 0.8f;
    private float musicVolume = 0.8f;
    private float sfxVolume = 0.8f;
    private float mouseSensitivity = 50f; // Scale from 1 to 100

    // UI text references for updating display value
    private TextMeshProUGUI masterValText;
    private TextMeshProUGUI musicValText;
    private TextMeshProUGUI sfxValText;
    private TextMeshProUGUI sensitivityValText;
    private Slider sensitivitySlider;

    void Awake()
    {
        AlignAndFormatButtons();

        if (Application.isPlaying)
        {
            // Load saved settings or set defaults
            LoadSettings();

            // Build level select, settings, how to play, and credits panels
            levelSelectPanel = BuildLevelSelectPanel();
            levelSelectPanel.SetActive(false);

            settingsPanel = BuildSettingsPanel();
            settingsPanel.SetActive(false);

            howToPlayPanel = BuildHowToPlayPanel();
            howToPlayPanel.SetActive(false);

            creditsPanel = BuildCreditsPanel();
            creditsPanel.SetActive(false);

            // Update settings UI text displays
            UpdateSettingsUI();

            // Ensure cursor is unlocked and visible in Main Menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    [ContextMenu("Realign Buttons in Editor")]
    public void AlignAndFormatButtons()
    {
        // 1. Resolve exact Button GameObjects (avoiding child Text objects)
        playBtnGo = FindButtonGameObject("Play", "PLAY", "PlayBtn", "Play Button");
        howToPlayBtnGo = FindButtonGameObject("How To Play", "HowToPlay", "HOW TO PLAY", "HowToPlayBtn", "How To Play Button");
        levelsBtnGo = FindButtonGameObject("Levels", "LEVELS", "LevelsBtn", "Levels Button");
        settingsBtnGo = FindButtonGameObject("Settings", "SETTINGS", "SettingsBtn", "Settings Button");
        creditsBtnGo = FindButtonGameObject("Credits", "CREDITS", "CreditsBtn", "Credits Button");
        quitBtnGo = FindButtonGameObject("Quit", "QUIT", "QuitBtn", "Quit Button");

        // 2. Format & re-align all 6 buttons cleanly in 1 line each (X=-490f, 125px spacing)
        FormatAndPositionButton(playBtnGo, "PLAY", new Vector2(-490.0f, 260f), PlayGame);
        FormatAndPositionButton(howToPlayBtnGo, "HOW TO PLAY", new Vector2(-490.0f, 135f), ToggleHowToPlay);
        FormatAndPositionButton(levelsBtnGo, "LEVELS", new Vector2(-490.0f, 10f), ToggleLevelSelect);
        FormatAndPositionButton(settingsBtnGo, "SETTINGS", new Vector2(-490.0f, -115f), ToggleSettings);
        FormatAndPositionButton(creditsBtnGo, "CREDITS", new Vector2(-490.0f, -240f), ToggleCredits);
        FormatAndPositionButton(quitBtnGo, "QUIT", new Vector2(-490.0f, -365f), QuitGame);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            AlignAndFormatButtons();
        };
    }
#endif

    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 50f); // Default 50 (midpoint of 1-100)

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

        // Apply updated BGM/SFX volumes programmatically in GameAudioManager
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.LoadVolumeSettings();
        }
    }

    void CancelSettings()
    {
        LoadSettings();
        UpdateSettingsUI();
        ToggleSettings();
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
        {
            sensitivityValText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    private GameObject FindButtonGameObject(params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                if (go.GetComponent<Button>() != null) return go;
                if (go.transform.parent != null && go.transform.parent.GetComponent<Button>() != null) return go.transform.parent.gameObject;
            }
        }

        // Fallback: Search all Button components in active/inactive scene hierarchy
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button b in allButtons)
        {
            if (b == null) continue;
            string bName = b.gameObject.name.Replace(" ", "").Replace("_", "").ToLower();
            foreach (string target in possibleNames)
            {
                string tName = target.Replace(" ", "").Replace("_", "").ToLower();
                if (bName.Equals(tName) || bName.Contains(tName))
                {
                    return b.gameObject;
                }
            }
        }
        return null;
    }

    void FormatAndPositionButton(GameObject btnGo, string labelText, Vector2 pos, UnityAction action)
    {
        if (btnGo == null)
        {
            Debug.LogWarning($"Main Menu Button for '{labelText}' not found in scene!");
            return;
        }

        btnGo.SetActive(true);

        // Check scale factor on parent button GameObject (e.g. Scale = (4, 4, 1))
        Vector3 localScale = btnGo.transform.localScale;
        float scaleFactor = (localScale.x > 0f) ? localScale.x : 1.0f;

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Unscaled sizeDelta so that sizeDelta * scale matches desired on-screen size
            float targetUnscaledWidth = (scaleFactor >= 2.0f) ? 180f : 450f;
            float targetUnscaledHeight = (scaleFactor >= 2.0f) ? 30f : 60f;
            rt.sizeDelta = new Vector2(targetUnscaledWidth, targetUnscaledHeight);
            rt.anchoredPosition = pos;
        }

        Button btn = btnGo.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        TextMeshProUGUI tmp = btnGo.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
            tmp.text = labelText;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            
            // Unscaled font size: 22f when Scale = 4 (effective 88pt on screen), 38f when Scale = 1
            tmp.fontSize = (scaleFactor >= 2.0f) ? 22f : 38f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;

            RectTransform textRt = tmp.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                textRt.anchoredPosition = Vector2.zero;
                textRt.sizeDelta = Vector2.zero;
            }
        }
    }

    void SetMainMenuButtonsActive(bool active)
    {
        ResetButtonHoverState(playBtnGo);
        ResetButtonHoverState(howToPlayBtnGo);
        ResetButtonHoverState(levelsBtnGo);
        ResetButtonHoverState(settingsBtnGo);
        ResetButtonHoverState(creditsBtnGo);
        ResetButtonHoverState(quitBtnGo);

        if (playBtnGo != null) playBtnGo.SetActive(active);
        if (howToPlayBtnGo != null) howToPlayBtnGo.SetActive(active);
        if (levelsBtnGo != null) levelsBtnGo.SetActive(active);
        if (settingsBtnGo != null) settingsBtnGo.SetActive(active);
        if (creditsBtnGo != null) creditsBtnGo.SetActive(active);
        if (quitBtnGo != null) quitBtnGo.SetActive(active);
    }

    private void ResetButtonHoverState(GameObject btnGo)
    {
        if (btnGo == null) return;
        ButtonHoverEffect bhe = btnGo.GetComponent<ButtonHoverEffect>();
        if (bhe != null) bhe.ResetHoverState();

        PauseMenuButtonHover pmbh = btnGo.GetComponent<PauseMenuButtonHover>();
        if (pmbh != null) pmbh.ResetHoverState();
    }

    public void ToggleLevelSelect()
    {
        bool show = !levelSelectPanel.activeSelf;
        levelSelectPanel.SetActive(show);
        settingsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        SetMainMenuButtonsActive(!show);
    }

    public void ToggleSettings()
    {
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        levelSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        SetMainMenuButtonsActive(!show);
    }

    public void ToggleHowToPlay()
    {
        bool show = !howToPlayPanel.activeSelf;
        howToPlayPanel.SetActive(show);
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        SetMainMenuButtonsActive(!show);
    }

    public void ToggleCredits()
    {
        bool show = !creditsPanel.activeSelf;
        creditsPanel.SetActive(show);
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        SetMainMenuButtonsActive(!show);
    }

    GameObject BuildLevelSelectPanel()
    {
        GameObject panel = new GameObject("LevelSelectPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = 5;

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(850f, 520f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.96f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.layer = 5;
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, 220f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "LEVEL SELECTION";
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

        // Level Cards
        AddLevelCard(panel.transform, "Level 1", "the Breach", "Level 1", -270f);
        AddLevelCard(panel.transform, "Level 2", "the Twist", "Level 2", 0f);
        AddLevelCard(panel.transform, "Level 3", "The Prestige", "Level 3", 270f);

        // Back Button
        GameObject backGo = new GameObject("BackBtn");
        backGo.transform.SetParent(panel.transform, false);
        backGo.layer = 5;
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.sizeDelta = new Vector2(200f, 40f);
        backRt.anchoredPosition = new Vector2(0f, -220f);

        Image backImg = backGo.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.onClick.AddListener(ToggleLevelSelect);
        backGo.AddComponent<ButtonHoverEffect>();

        GameObject backTextGo = new GameObject("Label");
        backTextGo.transform.SetParent(backGo.transform, false);
        backTextGo.layer = 5;
        RectTransform btRt = backTextGo.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btTmp = backTextGo.AddComponent<TextMeshProUGUI>();
        btTmp.text = "BACK";
        btTmp.fontSize = 18f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        return panel;
    }

    GameObject AddLevelCard(Transform parent, string levelName, string subtitle, string sceneName, float xOffset)
    {
        GameObject cardGo = new GameObject(levelName + "_Card");
        cardGo.transform.SetParent(parent, false);
        cardGo.layer = 5;

        RectTransform rt = cardGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(250f, 360f); // Enlarged card dimensions
        rt.anchoredPosition = new Vector2(xOffset, 20f);

        Image bg = cardGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button btn = cardGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        btn.onClick.AddListener(() => LoadWithScreen(sceneName));

        // 1. Preview box
        GameObject previewGo = new GameObject("PreviewBox");
        previewGo.transform.SetParent(cardGo.transform, false);
        previewGo.layer = 5;
        RectTransform prevRt = previewGo.AddComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0.5f, 0.5f);
        prevRt.anchorMax = new Vector2(0.5f, 0.5f);
        prevRt.sizeDelta = new Vector2(220f, 150f);
        prevRt.anchoredPosition = new Vector2(0f, 85f);
        Image prevImg = previewGo.AddComponent<Image>();
        prevImg.color = new Color(0.06f, 0.06f, 0.06f, 1f);

        // Preview placeholder text
        GameObject prevTextGo = new GameObject("PreviewText");
        prevTextGo.transform.SetParent(previewGo.transform, false);
        prevTextGo.layer = 5;
        RectTransform prevTextRt = prevTextGo.AddComponent<RectTransform>();
        prevTextRt.anchorMin = Vector2.zero;
        prevTextRt.anchorMax = Vector2.one;
        prevTextRt.offsetMin = Vector2.zero;
        prevTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI prevTmp = prevTextGo.AddComponent<TextMeshProUGUI>();
        prevTmp.text = "[ SECURE AREA ]";
        prevTmp.fontSize = 14f;
        prevTmp.alignment = TextAlignmentOptions.Center;
        prevTmp.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Load preview image dynamically (supports both Sprite and Texture2D assets)
        Sprite previewSprite = Resources.Load<Sprite>("UI/Previews/" + sceneName);
        if (previewSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("UI/Previews/" + sceneName);
            if (tex != null)
            {
                previewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (previewSprite != null)
        {
            prevImg.sprite = previewSprite;
            prevImg.color = Color.white; // Full visibility, no dark tint
            prevTextGo.SetActive(false); // Hide the placeholder text
        }

        // 2. Title Text (e.g. "LEVEL 1")
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(cardGo.transform, false);
        titleGo.layer = 5;
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(220f, 35f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = levelName.ToUpper();
        titleTmp.fontSize = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;

        // 3. Subtitle Text (e.g. "The Breach")
        GameObject subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(cardGo.transform, false);
        subGo.layer = 5;
        RectTransform subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.5f);
        subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.sizeDelta = new Vector2(220f, 30f);
        subRt.anchoredPosition = new Vector2(0f, -55f);
        TextMeshProUGUI subTmp = subGo.AddComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.fontSize = 16f;
        subTmp.fontStyle = FontStyles.Italic;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

        // 4. Action Text (e.g. "LAUNCH")
        GameObject actGo = new GameObject("ActionText");
        actGo.transform.SetParent(cardGo.transform, false);
        actGo.layer = 5;
        RectTransform actRt = actGo.AddComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0.5f, 0.5f);
        actRt.anchorMax = new Vector2(0.5f, 0.5f);
        actRt.sizeDelta = new Vector2(220f, 30f);
        actRt.anchoredPosition = new Vector2(0f, -120f);
        TextMeshProUGUI actTmp = actGo.AddComponent<TextMeshProUGUI>();
        actTmp.text = "LAUNCH";
        actTmp.fontSize = 18f;
        actTmp.fontStyle = FontStyles.Bold;
        actTmp.alignment = TextAlignmentOptions.Center;
        actTmp.color = Color.white;

        cardGo.AddComponent<ButtonHoverEffect>();

        return cardGo;
    }

    GameObject BuildSettingsPanel()
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = 5;

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 480f); // Settings panel size
        rt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.96f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.layer = 5;
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, 190f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SETTINGS";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

        // Section: AUDIO
        AddSectionHeader(panel.transform, "AUDIO", 135f);
        AddAdjusterRow(panel.transform, "MASTER VOLUME", 90f, () => AdjustMasterVolume(-0.1f), () => AdjustMasterVolume(0.1f), out masterValText);
        AddAdjusterRow(panel.transform, "MUSIC VOLUME", 45f, () => AdjustMusicVolume(-0.1f), () => AdjustMusicVolume(0.1f), out musicValText);
        AddAdjusterRow(panel.transform, "SFX VOLUME", 0f, () => AdjustSFXVolume(-0.1f), () => AdjustSFXVolume(0.1f), out sfxValText);

        // Section: CONTROLS
        AddSectionHeader(panel.transform, "CONTROLS", -55f);
        AddSensitivitySliderRow(panel.transform, "MOUSE SENSITIVITY", -100f, 1f, 100f, mouseSensitivity, OnSensitivitySliderChanged, out sensitivityValText);

        // Split Back and Apply buttons at the bottom
        // Back Button
        GameObject backGo = new GameObject("BackBtn");
        backGo.transform.SetParent(panel.transform, false);
        backGo.layer = 5;
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.sizeDelta = new Vector2(200f, 45f);
        backRt.anchoredPosition = new Vector2(-130f, -180f);

        Image backImg = backGo.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.onClick.AddListener(CancelSettings);
        backGo.AddComponent<ButtonHoverEffect>();

        GameObject backTextGo = new GameObject("Label");
        backTextGo.transform.SetParent(backGo.transform, false);
        backTextGo.layer = 5;
        RectTransform btRt = backTextGo.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btTmp = backTextGo.AddComponent<TextMeshProUGUI>();
        btTmp.text = "BACK";
        btTmp.fontSize = 20f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        // Apply Button
        GameObject applyGo = new GameObject("ApplyBtn");
        applyGo.transform.SetParent(panel.transform, false);
        applyGo.layer = 5;
        RectTransform applyRt = applyGo.AddComponent<RectTransform>();
        applyRt.anchorMin = new Vector2(0.5f, 0.5f);
        applyRt.anchorMax = new Vector2(0.5f, 0.5f);
        applyRt.sizeDelta = new Vector2(200f, 45f);
        applyRt.anchoredPosition = new Vector2(130f, -180f);

        Image applyImg = applyGo.AddComponent<Image>();
        applyImg.color = new Color(0.91f, 0.44f, 0.08f, 1f);
        Button applyBtn = applyGo.AddComponent<Button>();
        applyBtn.onClick.AddListener(() => {
            SaveSettings();
            ToggleSettings();
        });
        applyGo.AddComponent<ButtonHoverEffect>();

        GameObject applyTextGo = new GameObject("Label");
        applyTextGo.transform.SetParent(applyGo.transform, false);
        applyTextGo.layer = 5;
        RectTransform atRt = applyTextGo.AddComponent<RectTransform>();
        atRt.anchorMin = Vector2.zero;
        atRt.anchorMax = Vector2.one;
        atRt.offsetMin = Vector2.zero;
        atRt.offsetMax = Vector2.zero;
        TextMeshProUGUI atTmp = applyTextGo.AddComponent<TextMeshProUGUI>();
        atTmp.text = "APPLY";
        atTmp.fontSize = 20f;
        atTmp.fontStyle = FontStyles.Bold;
        atTmp.alignment = TextAlignmentOptions.Center;
        atTmp.color = Color.white;

        return panel;
    }

    void AddSectionHeader(Transform parent, string sectionName, float yPos)
    {
        // Container
        GameObject headerGo = new GameObject(sectionName + "_Header");
        headerGo.transform.SetParent(parent, false);
        headerGo.layer = 5;
        RectTransform rt = headerGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 30f); // Spans full modal width
        rt.anchoredPosition = new Vector2(0f, yPos);

        // 1. Left Line (stretches from x = -340 to x = -270)
        GameObject leftLineGo = new GameObject("LeftLine");
        leftLineGo.transform.SetParent(headerGo.transform, false);
        leftLineGo.layer = 5;
        RectTransform leftLineRt = leftLineGo.AddComponent<RectTransform>();
        leftLineRt.anchorMin = new Vector2(0.5f, 0.5f);
        leftLineRt.anchorMax = new Vector2(0.5f, 0.5f);
        leftLineRt.sizeDelta = new Vector2(70f, 2f); // 2px height line
        leftLineRt.anchoredPosition = new Vector2(-305f, 0f); // Spans from -340 to -270
        Image leftLineImg = leftLineGo.AddComponent<Image>();
        leftLineImg.color = new Color(0.91f, 0.44f, 0.08f, 0.7f); // Orange translucent accent

        // 2. Text Label
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(headerGo.transform, false);
        labelGo.layer = 5;
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = sectionName;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.91f, 0.44f, 0.08f, 0.9f); // Bold orange
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        // Dynamically measure exact text width so line never overlaps long words like PARKOUR MECHANICS
        float measuredWidth = tmp.GetPreferredValues(sectionName).x;
        float labelWidth = Mathf.Max(measuredWidth, 70f);
        labelRt.sizeDelta = new Vector2(labelWidth, 30f);

        float labelX = -260f + (labelWidth / 2f);
        labelRt.anchoredPosition = new Vector2(labelX, 0f);

        // 3. Right Line (starts 10px after text end, ends at 340f)
        float rightLineStart = -260f + labelWidth + 10f;
        float rightLineEnd = 340f;
        float rightLineWidth = Mathf.Max(rightLineEnd - rightLineStart, 20f);
        float rightLineX = rightLineStart + (rightLineWidth / 2f);

        GameObject rightLineGo = new GameObject("RightLine");
        rightLineGo.transform.SetParent(headerGo.transform, false);
        rightLineGo.layer = 5;
        RectTransform rightLineRt = rightLineGo.AddComponent<RectTransform>();
        rightLineRt.anchorMin = new Vector2(0.5f, 0.5f);
        rightLineRt.anchorMax = new Vector2(0.5f, 0.5f);
        rightLineRt.sizeDelta = new Vector2(rightLineWidth, 2f);
        rightLineRt.anchoredPosition = new Vector2(rightLineX, 0f);
        Image rightLineImg = rightLineGo.AddComponent<Image>();
        rightLineImg.color = new Color(0.91f, 0.44f, 0.08f, 0.7f); // Orange translucent accent
    }

    void AddAdjusterRow(Transform parent, string labelText, float yPos, UnityAction onMinus, UnityAction onPlus, out TextMeshProUGUI valueTextRef)
    {
        // 1. Label
        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.layer = 5;
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(250f, 30f);
        labelRt.anchoredPosition = new Vector2(-220f, yPos); // Left-aligned
        
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.white;

        // 2. Minus button
        GameObject minusGo = new GameObject(labelText + "_MinusBtn");
        minusGo.transform.SetParent(parent, false);
        minusGo.layer = 5;
        RectTransform minusRt = minusGo.AddComponent<RectTransform>();
        minusRt.anchorMin = new Vector2(0.5f, 0.5f);
        minusRt.anchorMax = new Vector2(0.5f, 0.5f);
        minusRt.sizeDelta = new Vector2(40f, 30f);
        minusRt.anchoredPosition = new Vector2(60f, yPos);
        
        Image minusImg = minusGo.AddComponent<Image>();
        minusImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button minusBtn = minusGo.AddComponent<Button>();
        minusBtn.onClick.AddListener(onMinus);
        minusGo.AddComponent<ButtonHoverEffect>();

        GameObject minusTextGo = new GameObject("Label");
        minusTextGo.transform.SetParent(minusGo.transform, false);
        minusTextGo.layer = 5;
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

        // 3. Value display Text
        GameObject valGo = new GameObject(labelText + "_ValText");
        valGo.transform.SetParent(parent, false);
        valGo.layer = 5;
        RectTransform valRt = valGo.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0.5f);
        valRt.anchorMax = new Vector2(0.5f, 0.5f);
        valRt.sizeDelta = new Vector2(220f, 30f);
        valRt.anchoredPosition = new Vector2(200f, yPos); // Centered
        
        valueTextRef = valGo.AddComponent<TextMeshProUGUI>();
        valueTextRef.text = "...";
        valueTextRef.fontSize = 24f; // Larger font size for volume blocks
        valueTextRef.alignment = TextAlignmentOptions.Center;
        valueTextRef.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

        // 4. Plus button
        GameObject plusGo = new GameObject(labelText + "_PlusBtn");
        plusGo.transform.SetParent(parent, false);
        plusGo.layer = 5;
        RectTransform plusRt = plusGo.AddComponent<RectTransform>();
        plusRt.anchorMin = new Vector2(0.5f, 0.5f);
        plusRt.anchorMax = new Vector2(0.5f, 0.5f);
        plusRt.sizeDelta = new Vector2(40f, 30f);
        plusRt.anchoredPosition = new Vector2(340f, yPos);
        
        Image plusImg = plusGo.AddComponent<Image>();
        plusImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button plusBtn = plusGo.AddComponent<Button>();
        plusBtn.onClick.AddListener(onPlus);
        plusGo.AddComponent<ButtonHoverEffect>();

        GameObject plusTextGo = new GameObject("Label");
        plusTextGo.transform.SetParent(plusGo.transform, false);
        plusTextGo.layer = 5;
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
        // 1. Label (Left aligned at x = -220f)
        GameObject labelGo = new GameObject(labelText + "_Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.layer = 5;
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(250f, 30f);
        labelRt.anchoredPosition = new Vector2(-220f, yPos);
        
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 20f;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.white;

        // 2. Real Slider (Centered at x = 180f, width = 280f - slightly shifted left and narrower)
        sensitivitySlider = CreateSlider(parent, new Vector2(280f, 30f), new Vector2(180f, yPos), minVal, maxVal, defaultVal, onValueChanged);

        // 3. Value display Text (Placed at x = 360f - moved left so it doesn't clip the right wall)
        GameObject valGo = new GameObject(labelText + "_ValText");
        valGo.transform.SetParent(parent, false);
        valGo.layer = 5;
        RectTransform valRt = valGo.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0.5f);
        valRt.anchorMax = new Vector2(0.5f, 0.5f);
        valRt.sizeDelta = new Vector2(40f, 30f);
        valRt.anchoredPosition = new Vector2(360f, yPos);
        
        valueTextRef = valGo.AddComponent<TextMeshProUGUI>();
        valueTextRef.text = Mathf.RoundToInt(defaultVal).ToString();
        valueTextRef.fontSize = 20f;
        valueTextRef.alignment = TextAlignmentOptions.Center;
        valueTextRef.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent
    }

    Slider CreateSlider(Transform parent, Vector2 size, Vector2 position, float minVal, float maxVal, float defaultVal, UnityAction<float> onValueChanged)
    {
        // 1. Slider Root
        GameObject sliderGo = new GameObject("SensitivitySlider");
        sliderGo.transform.SetParent(parent, false);
        sliderGo.layer = 5;
        RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.sizeDelta = size;
        sliderRt.anchoredPosition = position;

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = true;
        slider.transition = Selectable.Transition.None;

        // 2. Background (the horizontal track line - 8px tall)
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        bgGo.layer = 5;
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.sizeDelta = new Vector2(0f, 8f);
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark track line

        // 3. Fill Area (stretches full width, no padding to prevent gray gaps on the left)
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        fillAreaGo.layer = 5;
        RectTransform fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.sizeDelta = new Vector2(0f, 8f);
        fillAreaRt.anchoredPosition = Vector2.zero;

        // 4. Fill (the orange progress line)
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        fillGo.layer = 5;
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Vibrant orange fill

        // 5. Handle Slide Area
        GameObject handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        handleAreaGo.layer = 5;
        RectTransform handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.sizeDelta = Vector2.zero;
        handleAreaRt.anchoredPosition = Vector2.zero;

        // 6. Handle (the circular knob)
        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        handleGo.layer = 5;
        RectTransform handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.offsetMin = new Vector2(-15f, 0f);
        handleRt.offsetMax = new Vector2(15f, 0f);
        Image handleImg = handleGo.AddComponent<Image>();
        
        // Generate and assign the circle sprite programmatically
        handleImg.sprite = CreateCircleSprite();
        handleImg.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange circle knob

        // Setup slider references
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

    void LoadWithScreen(string sceneName)
    {
        LoadingData.TargetScene = sceneName;
        SceneManager.LoadScene("Loading");
    }

    public void PlayGame() => LoadWithScreen("Level 1");

    public void OpenSettings() => ToggleSettings();

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    GameObject BuildHowToPlayPanel()
    {
        GameObject panel = new GameObject("HowToPlayPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = 5;

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 650f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.96f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.91f, 0.44f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.layer = 5;
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, 275f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "HOW TO PLAY";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        // Section 1: OBJECTIVE
        AddSectionHeader(panel.transform, "OBJECTIVE", 225f);
        AddInfoTextRow(panel.transform, "Infiltrate the vault, press <color=#E87014>E</color> to collect the Data Fragment, and escape to the extraction door before the AI pursuer catches you!", 185f, 35f);

        // Section 2: MOVEMENT & CONTROLS
        AddSectionHeader(panel.transform, "CONTROLS", 135f);
        AddInfoTextRow(panel.transform, "<color=#E87014>WASD</color> - Move  |  <color=#E87014>MOUSE</color> - Look Around  |  <color=#E87014>SPACEBAR</color> - Jump / Leap", 95f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Q</color> - 180° Camera Flip / Wall Turn  |  <color=#E87014>LEFT CTRL</color> - Crouch / Slide", 60f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>E</color> - Interact / Collect  |  <color=#E87014>ESC</color> - Pause Menu", 25f, 26f);

        // Section 3: PARKOUR MECHANICS
        AddSectionHeader(panel.transform, "PARKOUR MECHANICS", -20f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Wall Run & Climb:</color> Sprint toward walls to run along or climb. Press <color=#E87014>Q</color> to flip around!", -55f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Pole Swing:</color> Jump onto horizontal bars to swing and launch across large gaps.", -85f, 26f);

        // Section 4: AI PURSUER WARNING
        AddSectionHeader(panel.transform, "AI PURSUER", -130f);
        AddInfoTextRow(panel.transform, "The AI pursuer activates upon data collection and phases through solid walls — keep moving and use parkour routes to outrun it!", -165f, 30f);

        // Back Button
        GameObject backGo = new GameObject("BackBtn");
        backGo.transform.SetParent(panel.transform, false);
        backGo.layer = 5;
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.sizeDelta = new Vector2(220f, 50f);
        backRt.anchoredPosition = new Vector2(0f, -265f);

        Image backImg = backGo.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.onClick.AddListener(ToggleHowToPlay);
        backGo.AddComponent<ButtonHoverEffect>();

        GameObject backTextGo = new GameObject("Label");
        backTextGo.transform.SetParent(backGo.transform, false);
        backTextGo.layer = 5;
        RectTransform btRt = backTextGo.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btTmp = backTextGo.AddComponent<TextMeshProUGUI>();
        btTmp.text = "BACK";
        btTmp.fontSize = 20f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        return panel;
    }

    GameObject BuildCreditsPanel()
    {
        GameObject panel = new GameObject("CreditsPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = 5;

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 650f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.96f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.91f, 0.44f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        titleGo.layer = 5;
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(500f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, 285f);
        TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CREDITS";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f);

        // Section 1: ACADEMIC PROJECT
        AddSectionHeader(panel.transform, "ACADEMIC PROJECT", 240f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Course:</color> 3D Interactive Development & Game Design", 205f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Institution:</color> Azrieli College of Engineering", 175f, 26f);

        // Section 2: DEVELOPMENT TEAM
        AddSectionHeader(panel.transform, "DEVELOPMENT TEAM", 135f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Mohammad Othman</color>  —  Project Lead, Level Design & Environment Architecture", 100f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Ibrahim Amo</color>  —  Audio Direction, UI/UX Systems & Asset Integration", 70f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Mohammad Enaby</color>  —  Parkour Movement Systems & Pursuer AI Intelligence", 40f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Nazieh Sayegh</color>  —  Game AI Behaviors, Game Over & Ending Modals", 10f, 26f);

        // Section 3: ENGINE & TECH STACK
        AddSectionHeader(panel.transform, "ENGINE & TECH STACK", -30f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Game Engine:</color> Unity 3D (Universal Render Pipeline - URP)", -60f, 26f);
        AddInfoTextRow(panel.transform, "<color=#E87014>Systems:</color> Custom Character Physics & Dynamic Audio Engine", -90f, 26f);

        // Section 4: DEVELOPMENT ASSISTANCE
        AddSectionHeader(panel.transform, "DEVELOPMENT ASSISTANCE", -130f);
        AddInfoTextRow(panel.transform, "<color=#E87014>AI Tooling:</color> Code Optimization & Technical Assistance via Google DeepMind Antigravity", -165f, 26f);

        // Back Button
        GameObject backGo = new GameObject("BackBtn");
        backGo.transform.SetParent(panel.transform, false);
        backGo.layer = 5;
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.sizeDelta = new Vector2(220f, 50f);
        backRt.anchoredPosition = new Vector2(0f, -265f);

        Image backImg = backGo.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.onClick.AddListener(ToggleCredits);
        backGo.AddComponent<ButtonHoverEffect>();

        GameObject backTextGo = new GameObject("Label");
        backTextGo.transform.SetParent(backGo.transform, false);
        backTextGo.layer = 5;
        RectTransform btRt = backTextGo.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btTmp = backTextGo.AddComponent<TextMeshProUGUI>();
        btTmp.text = "BACK";
        btTmp.fontSize = 20f;
        btTmp.fontStyle = FontStyles.Bold;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        return panel;
    }

    void AddInfoTextRow(Transform parent, string textContent, float yPos, float height)
    {
        GameObject textGo = new GameObject("InfoRowText");
        textGo.transform.SetParent(parent, false);
        textGo.layer = 5;
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(830f, height);
        textRt.anchoredPosition = new Vector2(0f, yPos);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = textContent;
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.9f, 0.95f, 1f, 1f);
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }
}
