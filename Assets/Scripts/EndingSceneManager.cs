using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class EndingSceneManager : MonoBehaviour
{
    [Header("Settings")]
    public float fadeInDuration = 2.0f;
    public float displayDuration = 3.8f;
    public float fadeOutDuration = 1.6f;
    public string targetScene = "Main Menu";

    private CanvasGroup canvasGroup;
    private RectTransform containerRt;
    private TextMeshProUGUI titleTmp;
    private TextMeshProUGUI subTmp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitInEndingScene()
    {
        if (SceneManager.GetActiveScene().name.Equals("Ending", System.StringComparison.OrdinalIgnoreCase))
        {
            if (FindAnyObjectByType<EndingSceneManager>() == null)
            {
                GameObject managerGo = new GameObject("EndingSceneManager");
                managerGo.AddComponent<EndingSceneManager>();
            }
        }
    }

    void OnEnable()
    {
        ConfigureCameraBlack();
        if (!Application.isPlaying)
        {
            BuildEndingUI();
        }
    }

    private void ConfigureCameraBlack()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }

        // Also turn Directional Light off or dark in Ending scene if present
        Light dirLight = FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.color = Color.black;
            dirLight.intensity = 0f;
        }
    }

    void Awake()
    {
        Time.timeScale = 1f;
        ConfigureCameraBlack();

        // Clean up any Loading Screen canvas if present
        GameObject loadingCanvas = GameObject.Find("Canvas");
        if (loadingCanvas != null && loadingCanvas.name != "EndingTextCanvas" && loadingCanvas.name != "EndingBlackBGCanvas")
        {
            Destroy(loadingCanvas);
        }

        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        BuildEndingUI();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            ConfigureCameraBlack();
            BuildEndingUI();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // Display text in Editor view
            }
            if (containerRt != null)
            {
                containerRt.anchoredPosition = Vector2.zero;
            }
        }
    }

    void Start()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopChaseBGM();
            GameAudioManager.Instance.StopEmergencyAlarm();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Application.isPlaying)
        {
            StartCoroutine(EndingSequence());
        }
    }

    private void BuildEndingUI()
    {
        // Check if canvases already exist in scene hierarchy
        GameObject existingTextCanvas = GameObject.Find("EndingTextCanvas");
        GameObject existingBGCanvas = GameObject.Find("EndingBlackBGCanvas");

        if (existingTextCanvas != null && existingBGCanvas != null)
        {
            canvasGroup = existingTextCanvas.GetComponent<CanvasGroup>();
            Transform container = existingTextCanvas.transform.Find("TextContainer");
            if (container != null) containerRt = container.GetComponent<RectTransform>();
            return;
        }

        // 1. Permanent Pure Pitch-Black Background Canvas (Sorting Order 400, ALWAYS active)
        GameObject bgCanvasGo = existingBGCanvas;
        if (bgCanvasGo == null)
        {
            bgCanvasGo = new GameObject("EndingBlackBGCanvas");
            Canvas bgCanvas = bgCanvasGo.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            bgCanvas.sortingOrder = 400;

            CanvasScaler bgScaler = bgCanvasGo.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject solidBg = new GameObject("SolidBlackBG");
            solidBg.transform.SetParent(bgCanvasGo.transform, false);
            RectTransform solidRt = solidBg.AddComponent<RectTransform>();
            solidRt.anchorMin = Vector2.zero;
            solidRt.anchorMax = Vector2.one;
            solidRt.offsetMin = Vector2.zero;
            solidRt.offsetMax = Vector2.zero;
            Image solidImg = solidBg.AddComponent<Image>();
            solidImg.color = Color.black; // Pure Pitch Black
        }

        // 2. Animated Text Content Canvas (Sorting Order 500)
        GameObject canvasGo = existingTextCanvas;
        if (canvasGo == null)
        {
            canvasGo = new GameObject("EndingTextCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Text starts invisible for smooth fade-in

            // 3. Container for Text Elements
            GameObject containerGo = new GameObject("TextContainer");
            containerGo.transform.SetParent(canvasGo.transform, false);
            containerRt = containerGo.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(1200f, 600f);
            containerRt.anchoredPosition = new Vector2(0f, -25f); // Start offset down for upward float effect

            // 4. Main "THANK YOU FOR PLAYING" Header
            GameObject titleGo = new GameObject("ThankYouTitle");
            titleGo.transform.SetParent(containerGo.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(1100f, 100f);
            titleRt.anchoredPosition = new Vector2(0f, 40f);

            titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "THANK YOU FOR PLAYING";
            titleTmp.fontSize = 54f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.91f, 0.44f, 0.08f, 1f); // Glowing Orange Accent

            // 5. Subtitle
            GameObject subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(containerGo.transform, false);
            RectTransform subRt = subGo.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.5f);
            subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(900f, 50f);
            subRt.anchoredPosition = new Vector2(0f, -40f);

            subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "VAULT & VAULT — OPERATIONS COMPLETED";
            subTmp.fontSize = 22f;
            subTmp.fontStyle = FontStyles.Normal;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
#endif
    }

    private IEnumerator EndingSequence()
    {
        // Phase 0: Pure Black Delay (0.4s)
        canvasGroup.alpha = 0f;
        containerRt.anchoredPosition = new Vector2(0f, -25f);
        yield return new WaitForSeconds(0.4f);

        // Phase 1: Smooth 2.0s Cinematic Text Fade-In + Gentle Upward Float
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / fadeInDuration);
            canvasGroup.alpha = progress;
            containerRt.anchoredPosition = Vector2.Lerp(new Vector2(0f, -25f), Vector2.zero, progress);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        containerRt.anchoredPosition = Vector2.zero;

        // Phase 2: High-Visibility Display Hold (3.8s)
        yield return new WaitForSeconds(displayDuration);

        // Phase 3: Smooth 1.6s Text Fade-Out + Downward Drift back to Pure Black
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / fadeOutDuration);
            canvasGroup.alpha = 1f - progress;
            containerRt.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0f, -15f), progress);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Phase 4: Final Black Pause (0.4s)
        yield return new WaitForSeconds(0.4f);

        // Phase 5: Load Main Menu
        SceneManager.LoadScene(targetScene);
    }
}
