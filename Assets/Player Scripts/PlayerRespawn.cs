using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("The Y-axis height at which the player will die and respawn.")]
    public float fallThreshold = -10f; 
    
    [Tooltip("Optional: Drag a Transform here to use as a custom spawn point. If left empty, the player will respawn exactly where they started the level.")]
    public Transform customSpawnPoint;

    private Vector3 initialSpawnPosition;
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private WallRunning wallRunning;

    void Start()
    {
        // Cache references
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        wallRunning = GetComponent<WallRunning>();

        // Record the exact position the player spawned in at the start of the game
        initialSpawnPosition = transform.position;
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

        // 2. Teleport the player
        if (customSpawnPoint != null)
        {
            transform.position = customSpawnPoint.position;
            transform.rotation = customSpawnPoint.rotation;
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
