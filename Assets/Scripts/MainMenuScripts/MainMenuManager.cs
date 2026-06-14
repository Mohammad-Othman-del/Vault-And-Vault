using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    private GameObject levelSelectPanel;

    void Awake()
    {
        FindAndWireButton("Play", PlayGame);
        FindAndWireButton("Levels", ToggleLevelSelect);
        FindAndWireButton("Settings", OpenSettings);
        FindAndWireButton("Quit", QuitGame);

        levelSelectPanel = BuildLevelSelectPanel();
        levelSelectPanel.SetActive(false);
    }

    void FindAndWireButton(string goName, UnityAction action)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) return;
        Button btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(action);
    }

    GameObject BuildLevelSelectPanel()
    {
        GameObject panel = new GameObject("LevelSelectPanel");
        panel.transform.SetParent(transform, false);
        panel.layer = 5;

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 240f);
        rt.anchoredPosition = new Vector2(-546.3f, 0f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.97f);

        AddLevelButton(panel.transform, "Level 1", 80f);
        AddLevelButton(panel.transform, "Level 2", 0f);
        AddLevelButton(panel.transform, "Level 3", -80f);

        return panel;
    }

    void AddLevelButton(Transform parent, string levelName, float yOffset)
    {
        GameObject btnGo = new GameObject(levelName + "Btn");
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = 5;

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 50f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.45f, 0.45f, 0.45f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        btn.colors = colors;

        string scene = levelName;
        btn.onClick.AddListener(() => LoadWithScreen(scene));

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        textGo.layer = 5;

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = levelName;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void LoadWithScreen(string sceneName)
    {
        LoadingData.TargetScene = sceneName;
        SceneManager.LoadScene("Loading");
    }

    public void PlayGame() => LoadWithScreen("Level 1");

    public void ToggleLevelSelect()
    {
        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(!levelSelectPanel.activeSelf);
    }

    public void OpenSettings() => SceneManager.LoadScene("Settings");

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
