using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("The Y-axis height at which the player will die and respawn.")]
    public float fallThreshold = -10f;

    [Tooltip("Optional: Starting spawn point override. Checkpoints will override this at runtime.")]
    public Transform customSpawnPoint;

    private Vector3 initialSpawnPosition;
    private Transform activeCheckpoint;
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private WallRunning wallRunning;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        wallRunning = GetComponent<WallRunning>();

        initialSpawnPosition = transform.position;
        activeCheckpoint = customSpawnPoint;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        activeCheckpoint = checkpoint;
        Debug.Log($"Checkpoint set: {checkpoint.gameObject.name}");
    }

    void Update()
    {
        // Check if player has fallen below the map
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        // 1. Disable CharacterController. In Unity, CharacterControllers override transform.position.
        // You MUST disable them before attempting to teleport the player.
        if (controller != null)
        {
            controller.enabled = false;
        }

        // 2. Teleport the player to the last activated checkpoint, or the starting position
        if (activeCheckpoint != null)
        {
            transform.position = activeCheckpoint.position;
            transform.rotation = activeCheckpoint.rotation;
        }
        else
        {
            transform.position = initialSpawnPosition;
        }

        // 3. Re-enable CharacterController so physics can take over again
        if (controller != null)
        {
            controller.enabled = true;
        }

        // 4. Reset gravity buildup so they don't instantly fall again
        if (playerMovement != null)
        {
            playerMovement.SetVelocity(Vector3.zero);
        }

        // 5. Clear the memory of used walls so the player can retry the parkour puzzle!
        if (wallRunning != null)
        {
            wallRunning.ResetUsedWalls();
        }
        
        Debug.Log("Player fell out of bounds and respawned!");
    }
}
