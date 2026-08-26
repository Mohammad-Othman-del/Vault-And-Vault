using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    private const float LoadingDuration = 3f;
    private const float SpinSpeed = 220f;

    private RectTransform spinnerTransform;

    void Start()
    {
        BuildUI();
        StartCoroutine(LoadAfterDelay());
    }

    void Update()
    {
        if (spinnerTransform != null)
            spinnerTransform.Rotate(0f, 0f, -SpinSpeed * Time.deltaTime);
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

        // Centre hint text
        GameObject textGo = new GameObject("HintText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(800f, 80f);
        textRt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI hint = textGo.AddComponent<TextMeshProUGUI>();
        hint.text = "To progress through the dialogue press E";
        hint.fontSize = 34f;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = Color.white;

        // Spinning ring — bottom-right corner
        GameObject spinnerGo = new GameObject("Spinner");
        spinnerGo.transform.SetParent(canvasGo.transform, false);
        RectTransform spinnerRt = spinnerGo.AddComponent<RectTransform>();
        spinnerRt.anchorMin = new Vector2(1f, 0f);
        spinnerRt.anchorMax = new Vector2(1f, 0f);
        spinnerRt.sizeDelta = new Vector2(52f, 52f);
        spinnerRt.anchoredPosition = new Vector2(-60f, 60f);

        Image spinnerImg = spinnerGo.AddComponent<Image>();
        spinnerImg.sprite = CreateRingSprite(64, 9);
        spinnerImg.type = Image.Type.Filled;
        spinnerImg.fillMethod = Image.FillMethod.Radial360;
        spinnerImg.fillOrigin = 2; // start from top
        spinnerImg.fillClockwise = true;
        spinnerImg.fillAmount = 0.78f;
        spinnerImg.color = Color.white;

        spinnerTransform = spinnerRt;
    }

    // Draws a ring into a texture and returns it as a Sprite
    Sprite CreateRingSprite(int radius, int thickness)
    {
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float outerR = radius - 1f;
        float innerR = radius - thickness - 1f;
        Vector2 centre = new Vector2(radius, radius);

        for (int i = 0; i < pixels.Length; i++)
        {
            float dist = Vector2.Distance(new Vector2(i % size, i / size), centre);
            pixels[i] = (dist <= outerR && dist >= innerR) ? Color.white : Color.clear;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(LoadingDuration);
        
        string target = LoadingData.TargetScene;
        if (string.IsNullOrEmpty(target)) target = "Main Menu";

        if (Application.CanStreamedLevelBeLoaded(target))
        {
            SceneManager.LoadScene(target);
        }
        else
        {
            Debug.LogWarning($"LoadingScreenManager: Scene '{target}' is not added to Build Settings. Running fallback.");
            if (target.Equals("Ending", System.StringComparison.OrdinalIgnoreCase))
            {
                // Instantiate EndingSceneManager on the current canvas to play the Thank You For Playing sequence!
                GameObject endingGo = new GameObject("EndingSceneManager");
                endingGo.AddComponent<EndingSceneManager>();
            }
            else
            {
                SceneManager.LoadScene("Main Menu");
            }
        }
    }
}
