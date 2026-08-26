using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectibleItem : MonoBehaviour
{
    [Header("Hover & Spin")]
    public float hoverAmplitude = 0.3f;
    public float hoverSpeed = 1.5f;
    public float spinSpeed = 90f;

    [Header("Interaction")]
    public float pickupRadius = 3f;
    public KeyCode collectKey = KeyCode.E;
    public string promptText = "Press E to collect";

    public static bool IsCollected { get; private set; } = false;

    private Vector3 startPosition;
    private GameObject promptPanel;
    private bool playerNearby = false;
    private Transform playerTransform;

    void Awake()
    {
        IsCollected = false;
        startPosition = transform.position;
        BuildPromptUI();
    }

    public static void ResetCollectible()
    {
        IsCollected = false;
        CollectibleItem[] items = Resources.FindObjectsOfTypeAll<CollectibleItem>();
        foreach (CollectibleItem item in items)
        {
            if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded)
            {
                item.gameObject.SetActive(true);
                item.playerNearby = false;
                if (item.promptPanel != null) item.promptPanel.SetActive(false);
            }
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (IsCollected) return;

        // Hover
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Spin
        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool isNear = dist <= pickupRadius;

        if (isNear != playerNearby)
        {
            playerNearby = isNear;
            if (promptPanel != null) promptPanel.SetActive(isNear);
        }

        if (playerNearby && Input.GetKeyDown(collectKey))
            Collect();
    }

    void Collect()
    {
        IsCollected = true;
        if (promptPanel != null) promptPanel.SetActive(false);
        gameObject.SetActive(false);

        // Play collect item SFX through GameAudioManager
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayCollectSFX();
        }

        // Trigger fast-paced chasing music
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StartChaseBGM();
            GameAudioManager.Instance.StartEmergencyAlarm(); // Starts low background emergency alarm in Level 3 only
        }

        // Activate Chasing AI Bot (EVE) - Auto-spawns if not already placed in scene!
        RubberBandBot bot = FindAnyObjectByType<RubberBandBot>();
        if (bot == null)
        {
            GameObject botGo = new GameObject("ChasingAIBot");
            bot = botGo.AddComponent<RubberBandBot>();
        }
        bot.ActivateBot();
    }

    void BuildPromptUI()
    {
        GameObject canvasGo = new GameObject("CollectiblePromptCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        promptPanel = new GameObject("CollectPromptPanel");
        promptPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = promptPanel.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.15f);
        bgRt.anchorMax = new Vector2(0.5f, 0.15f);
        bgRt.sizeDelta = new Vector2(380f, 60f);
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = promptPanel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject textGo = new GameObject("PromptText");
        textGo.transform.SetParent(promptPanel.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 0f);
        textRt.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = promptText;
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        promptPanel.SetActive(false);
    }
}
