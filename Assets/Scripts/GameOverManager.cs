using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Customization")]
    [Tooltip("Background color of the popup panel.")]
    public Color panelColor = new Color(0.04f, 0.05f, 0.08f, 0.96f);

    [Tooltip("Accent color used for outline borders, text header, and button highlights.")]
    public Color accentColor = new Color(1f, 0.25f, 0.25f, 1f); // Neon Red for Game Over

    [Tooltip("Text color of subtitles and button text.")]
    public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);

    [Tooltip("Background color of the buttons (idle state).")]
    public Color buttonColor = new Color(0.08f, 0.12f, 0.2f, 1f);

    public static bool isGameOverActive = false;

    private GameObject gameOverCanvasGo;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    void Awake()
    {
        isGameOverActive = false;
    }

    void OnDestroy()
    {
        isGameOverActive = false;
    }

    /// <summary>
    /// Stops game time, disables player input, plays audio, and displays the Game Over modal with 3 options.
    /// </summary>
    public void ShowGameOver(string reason = "GAME OVER")
    {
        // Don't spawn multiple modals
        if (gameOverCanvasGo != null) return;
        isGameOverActive = true;

        // Ensure an EventSystem exists in the scene so UI buttons can receive mouse clicks!
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Play specific death audio SFX based on reason
        if (GameAudioManager.Instance != null)
        {
            if (reason.Equals("FELL OFF THE MAP", System.StringComparison.OrdinalIgnoreCase))
            {
                GameAudioManager.Instance.PlayFallDeathSFX();
            }
            else
            {
                GameAudioManager.Instance.PlayDeathSFX();
            }
        }

        // Pause the timer in scene
        Timer timer = FindAnyObjectByType<Timer>();
        if (timer != null)
        {
            timer.PauseTimer();
        }

        // Disable player keyboard & mouse input in the level while modal is visible
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

        DialogueManager dm = FindAnyObjectByType<DialogueManager>();
        if (dm != null)
        {
            dm.enabled = false;
        }

        // Pause game time
        Time.timeScale = 0f;

        // Save & unlock mouse cursor for UI interaction
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Build and show the UI Screen
        BuildGameOverUI(reason);
    }

    private void BuildGameOverUI(string reason)
    {
        // 1. Create Canvas
        gameOverCanvasGo = new GameObject("GameOverCanvas");
        Canvas canvas = gameOverCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95; // Just under pause menu (100)

        CanvasScaler scaler = gameOverCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameOverCanvasGo.AddComponent<GraphicRaycaster>();

        // 2. Dark Background Overlay (Raycast Target to block clicking game elements)
        GameObject overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(gameOverCanvasGo.transform, false);
        RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0.12f, 0f, 0f, 0.88f); // Red-tinted dark overlay

        // 3. Center Popup Container
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(gameOverCanvasGo.transform, false);
        RectTransform containerRt = containerGo.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(720f, 420f);
        containerRt.anchoredPosition = Vector2.zero;

        Image containerImg = containerGo.AddComponent<Image>();
        containerImg.color = panelColor;

        Outline outline = containerGo.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.8f);
        outline.effectDistance = new Vector2(4f, -4f);

        // 4. Header: GAME OVER
        GameObject headerGo = new GameObject("Header");
        headerGo.transform.SetParent(containerGo.transform, false);
        RectTransform headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(600f, 60f);
        headerRt.anchoredPosition = new Vector2(0f, -50f);

        TextMeshProUGUI headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
        headerTmp.text = "GAME OVER";
        headerTmp.fontSize = 48f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = accentColor;

        // 5. Subtitle message
        GameObject subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(containerGo.transform, false);
        RectTransform subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 1f);
        subRt.anchorMax = new Vector2(0.5f, 1f);
        subRt.sizeDelta = new Vector2(600f, 40f);
        subRt.anchoredPosition = new Vector2(0f, -115f);

        TextMeshProUGUI subTmp = subGo.AddComponent<TextMeshProUGUI>();
        if (reason.Equals("FELL OFF THE MAP", System.StringComparison.OrdinalIgnoreCase))
            subTmp.text = "You fell off the map!";
        else if (reason.Equals("CAUGHT BY AI BOT", System.StringComparison.OrdinalIgnoreCase))
            subTmp.text = "You were caught by the AI bot!";
        else
            subTmp.text = reason;

        subTmp.fontSize = 22f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = textColor;

        // 6. Two Option Buttons (RESTART & MAIN MENU)
        float startY = -200f;
        float spacingY = 90f;

        CreateButton(containerGo.transform, "RESTART", new Vector2(0f, startY), RestartLevel);
        CreateButton(containerGo.transform, "MAIN MENU", new Vector2(0f, startY - spacingY), LoadMainMenu);
    }

    private void CreateButton(Transform parent, string label, Vector2 position, UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(label + "Btn");
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(560f, 65f);
        rt.anchoredPosition = position;

        Image bg = btnGo.AddComponent<Image>();
        bg.color = buttonColor;

        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);

        ColorBlock colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = accentColor;
        colors.pressedColor = new Color(accentColor.r * 0.7f, accentColor.g * 0.7f, accentColor.b * 0.7f, 1f);
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
        tmp.color = textColor;
    }

    /// <summary>
    /// Option 1: Restores time and reloads the level from the beginning.
    /// </summary>
    private void RestartLevel()
    {
        isGameOverActive = false;
        Time.timeScale = 1f;

        if (gameOverCanvasGo != null)
        {
            Destroy(gameOverCanvasGo);
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopChaseBGM();
            GameAudioManager.Instance.StopEmergencyAlarm();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Option 3: Restores time and loads the Main Menu.
    /// </summary>
    private void LoadMainMenu()
    {
        isGameOverActive = false;
        Time.timeScale = 1f;

        if (gameOverCanvasGo != null)
        {
            Destroy(gameOverCanvasGo);
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopAllBGM();
            GameAudioManager.Instance.StopEmergencyAlarm();
        }

        SceneManager.LoadScene("Main Menu");
    }
}
