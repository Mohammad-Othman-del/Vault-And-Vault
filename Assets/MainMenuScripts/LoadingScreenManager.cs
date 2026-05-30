using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    private const float LoadingDuration = 3f;

    private TextMeshProUGUI loadingText;
    private float dotTimer;
    private int dotCount;

    void Start()
    {
        BuildUI();
        StartCoroutine(LoadAfterDelay());
    }

    void Update()
    {
        dotTimer += Time.deltaTime;
        if (dotTimer >= 0.4f)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            loadingText.text = "LOADING" + new string('.', dotCount);
        }
    }

    void BuildUI()
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen dark background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

        // Centered loading text
        GameObject textGo = new GameObject("LoadingText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(500f, 100f);
        textRt.anchoredPosition = Vector2.zero;

        loadingText = textGo.AddComponent<TextMeshProUGUI>();
        loadingText.text = "LOADING";
        loadingText.fontSize = 52f;
        loadingText.fontStyle = FontStyles.Bold;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = Color.white;
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(LoadingDuration);
        SceneManager.LoadScene(LoadingData.TargetScene);
    }
}
