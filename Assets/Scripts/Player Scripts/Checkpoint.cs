using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional renderer whose color will change when this checkpoint is activated.")]
    [SerializeField] private Renderer checkpointRenderer;

    [SerializeField] private Color activatedColor = Color.green;

    private bool activated = false;
    private Color originalColor;

    void Start()
    {
        if (checkpointRenderer != null)
            originalColor = checkpointRenderer.material.color;
    }

    void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null) return;

        respawn.SetCheckpoint(transform);
        activated = true;

        if (checkpointRenderer != null)
            checkpointRenderer.material.color = activatedColor;
    }
}
