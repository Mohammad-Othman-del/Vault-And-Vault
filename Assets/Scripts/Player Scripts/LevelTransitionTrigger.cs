using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class LevelTransitionTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (must match the name in Build Settings).")]
    public string nextScene = "Level 2";

    [Tooltip("Tag of the object that activates this trigger.")]
    public string playerTag = "Player";

    [Tooltip("Key the player must press to transition.")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("Text shown in the prompt (use {key} to insert the key name).")]
    public string promptText = "Press {key} to enter next level";

    [Header("Collectible Gate")]
    [Tooltip("Block the transition until the CollectibleItem in this scene has been picked up.")]
    public bool requireCollectible = false;

    private bool playerInZone = false;
    private GameObject promptPanel;
    private GameObject blockedPanel;
    private Coroutine hideBlockedCoroutine;

    private static bool isTriggered = false;

    void Awake()
    {
        isTriggered = false;
        if (SceneManager.GetActiveScene().name.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase))
        {
            nextScene = "Ending";
        }

        GetComponent<Collider>().isTrigger = true;
        BuildPromptUI();
        BuildBlockedUI();
    }

    void Update()
    {
        if (isTriggered || !playerInZone) return;
        if (Input.GetKeyDown(interactKey))
        {
            if (requireCollectible && !CollectibleItem.IsCollected)
            {
                ShowBlockedMessage();
                return;
            }

            isTriggered = true;
            if (promptPanel != null) promptPanel.SetActive(false);
            if (blockedPanel != null) blockedPanel.SetActive(false);

            // Always show the Level Complete Result Modal on level finish
            LevelCompleteManager completeManager = FindAnyObjectByType<LevelCompleteManager>();
            if (completeManager == null)
            {
                GameObject lcmGo = new GameObject("LevelCompleteManager");
                completeManager = lcmGo.AddComponent<LevelCompleteManager>();
            }
            completeManager.ShowLevelComplete(nextScene);
        }
    }

    void ShowBlockedMessage()
    {
        if (blockedPanel == null) return;
        blockedPanel.SetActive(true);
        if (hideBlockedCoroutine != null) StopCoroutine(hideBlockedCoroutine);
        hideBlockedCoroutine = StartCoroutine(HideAfterDelay(blockedPanel, 2.5f));
    }

    IEnumerator HideAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = true;
        if (promptPanel != null) promptPanel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = false;
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    void BuildPromptUI()
    {
        // Canvas — always active; we toggle the panel inside it
        GameObject canvasGo = new GameObject("TransitionPromptCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background pill — this is what we show/hide
        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = promptPanel.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.1f);
        bgRt.anchorMax = new Vector2(0.5f, 0.1f);
        bgRt.sizeDelta = new Vector2(420f, 60f);
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = promptPanel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);

        // Prompt text
        GameObject textGo = new GameObject("PromptText");
        textGo.transform.SetParent(promptPanel.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 0f);
        textRt.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = promptText.Replace("{key}", interactKey.ToString().ToUpper());
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        // Panel hidden until player enters the zone
        promptPanel.SetActive(false);
    }

    void BuildBlockedUI()
    {
        // Re-use the same canvas already in the scene if possible; otherwise build one
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        Transform canvasParent = existingCanvas != null ? existingCanvas.transform : null;

        if (canvasParent == null)
        {
            GameObject canvasGo = new GameObject("BlockedMessageCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 51;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasParent = canvasGo.transform;
        }

        blockedPanel = new GameObject("BlockedPanel");
        blockedPanel.transform.SetParent(canvasParent, false);
        RectTransform bgRt = blockedPanel.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.1f);
        bgRt.anchorMax = new Vector2(0.5f, 0.1f);
        bgRt.sizeDelta = new Vector2(520f, 60f);
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = blockedPanel.AddComponent<Image>();
        bgImg.color = new Color(0.6f, 0.1f, 0.1f, 0.85f);

        GameObject textGo = new GameObject("BlockedText");
        textGo.transform.SetParent(blockedPanel.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 0f);
        textRt.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = "You need to collect the item first!";
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        blockedPanel.SetActive(false);
    }
}
