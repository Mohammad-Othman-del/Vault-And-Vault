using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

    private bool playerInZone = false;
    private GameObject promptPanel;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        BuildPromptUI();
    }

    void Update()
    {
        if (!playerInZone) return;
        if (Input.GetKeyDown(interactKey))
        {
            LoadingData.TargetScene = nextScene;
            SceneManager.LoadScene("Loading");
        }
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
}
