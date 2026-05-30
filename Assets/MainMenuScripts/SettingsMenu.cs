using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    void Awake()
    {
        GameObject backGo = GameObject.Find("Back");
        if (backGo == null) return;
        Button btn = backGo.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(BackToMenu);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
