using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelTransitionTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (must match the name in Build Settings)")]
    public string nextScene = "Level 2";
    [Tooltip("Tag of the object that activates this trigger")]
    public string playerTag = "Player";

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        LoadingData.TargetScene = nextScene;
        SceneManager.LoadScene("Loading");
    }
}
