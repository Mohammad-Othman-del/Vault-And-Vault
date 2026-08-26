using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional renderer whose color will change when this checkpoint is activated.")]
    [SerializeField] private Renderer checkpointRenderer;

    [SerializeField] private Color activatedColor = Color.green;

    private Color originalColor;

    void Start()
    {
        if (checkpointRenderer != null)
            originalColor = checkpointRenderer.material.color;
    }

    void OnTriggerEnter(Collider other)
    {
        // Checkpoints removed: restart starts from the beginning of the level
        return;
    }
}
