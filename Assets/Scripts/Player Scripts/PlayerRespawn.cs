using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("The Y-axis height at which the player will die and respawn.")]
    public float fallThreshold = -100f;

    [Tooltip("Optional: Starting spawn point override. Checkpoints will override this at runtime.")]
    public Transform customSpawnPoint;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
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
        initialSpawnRotation = transform.rotation;
        activeCheckpoint = customSpawnPoint;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint != null)
        {
            activeCheckpoint = checkpoint;
            Debug.Log($"Active Checkpoint set to GameObject: {checkpoint.gameObject.name}");
        }
    }

    void Update()
    {
        // Check if player has fallen below the map
        if (transform.position.y < fallThreshold && Time.timeScale > 0f)
        {
            GameOverManager gom = FindAnyObjectByType<GameOverManager>();
            if (gom == null)
            {
                GameObject gomGo = new GameObject("GameOverManager");
                gom = gomGo.AddComponent<GameOverManager>();
            }
            gom.ShowGameOver("FELL OFF THE MAP");
        }
    }

    public void Respawn()
    {
        // Stop the chase music and emergency alarm upon respawning/death
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopChaseBGM();
            GameAudioManager.Instance.StopEmergencyAlarm();
        }

        // Respawn the data fragment collectible item if collected
        CollectibleItem.ResetCollectible();

        // Reset camera look pitch & prevent delta jump spikes
        MouseLook mouseLook = GetComponentInChildren<MouseLook>();
        if (mouseLook != null)
        {
            mouseLook.ResetCameraLook();
        }

        // Reset the chasing bot position and breadcrumbs
        RubberBandBot bot = FindAnyObjectByType<RubberBandBot>();
        if (bot != null)
        {
            bot.ResetBotToSpawn();
        }

        // Reset player parkour, sliding, crouching, and swinging states!
        if (playerMovement != null)
        {
            playerMovement.ResetParkourStates();
        }

        BarSwinging barSwinging = GetComponent<BarSwinging>();
        if (barSwinging != null)
        {
            barSwinging.ResetBarSwing();
        }

        // 1. Disable CharacterController. In Unity, CharacterControllers override transform.position.
        // You MUST disable them before attempting to teleport the player.
        if (controller != null)
        {
            controller.enabled = false;
        }

        // 2. Teleport the player to the initial level spawn point
        transform.position = initialSpawnPosition;
        transform.rotation = initialSpawnRotation;

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
