using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class WallClimbing : MonoBehaviour
{
    [Header("Climbing References")]
    private CharacterController controller;
    private PlayerMovement playerMovement;

    [Header("Climbing Settings")]
    public float climbSpeed = 5f;
    public float maxClimbTime = 2f;
    private float climbTimer;

    [Header("Wall Detection")]
    public LayerMask wallMask;
    public float wallDetectionDistance = 0.7f;
    
    [Tooltip("Maximum angle (in degrees) from facing the wall perfectly head-on to allow climbing.")]
    public float maxAngleFromWall = 30f; 

    [Header("Vaulting Settings")]
    [Tooltip("Height offset to check if we've reached the top of the wall.")]
    public float topCheckHeight = 1f;
    public float vaultSpeed = 6f;
    public float vaultDuration = 0.4f;

    private bool isClimbing;
    private bool isVaulting;
    private float vaultTimer;

    private RaycastHit frontWallHit;
    private bool wallInFront;
    private bool topWallInFront;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        climbTimer = maxClimbTime;
    }

    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void CheckForWall()
    {
        // Raycast from the center of the player forward to detect walls
        wallInFront = Physics.Raycast(transform.position, transform.forward, out frontWallHit, wallDetectionDistance, wallMask);
        
        // Raycast from higher up to check if the wall has ended (ledge detection)
        Vector3 topRayStart = transform.position + Vector3.up * topCheckHeight;
        topWallInFront = Physics.Raycast(topRayStart, transform.forward, wallDetectionDistance, wallMask);
    }

    private void StateMachine()
    {
        if (playerMovement.isWallRunning) return; // Cannot climb and run at the same time

        if (isVaulting)
        {
            VaultMovement();
            return; // Skip climbing logic while vaulting
        }

        // Evaluate conditions to start or maintain climbing:
        // 1. A valid wall is in front.
        // 2. The player is pressing the forward input.
        // 3. The player is not grounded.
        // 4. The player is looking mostly head-on at the wall (angle near 90 deg relative to surface, or 0 to inverse normal).
        
        float angleToWall = wallInFront ? Vector3.Angle(transform.forward, -frontWallHit.normal) : 0f;
        bool isLookingAtWall = angleToWall < maxAngleFromWall;
        bool isMovingForward = Input.GetAxisRaw("Vertical") > 0;

        if (wallInFront && isMovingForward && isLookingAtWall && !playerMovement.isGrounded)
        {
            if (!isClimbing && climbTimer > 0)
            {
                StartClimbing();
            }
        }
        else
        {
            if (isClimbing)
            {
                StopClimbing();
            }
        }

        if (isClimbing)
        {
            ClimbingMovement();
            
            // If we are climbing and the top raycast doesn't hit anything, we reached the ledge
            if (!topWallInFront)
            {
                StartVault();
            }
        }

        // Recharge climbing ability when the player lands
        if (playerMovement.isGrounded && !isVaulting)
        {
            climbTimer = maxClimbTime;
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        playerMovement.isClimbing = true;
    }

    private void ClimbingMovement()
    {
        // Move the player directly upwards
        Vector3 climbVelocity = Vector3.up * climbSpeed;
        controller.Move(climbVelocity * Time.deltaTime);

        // Decrease the available climb time
        climbTimer -= Time.deltaTime;
        if (climbTimer < 0)
        {
            StopClimbing();
        }
        
        // Clear gravity buildup in PlayerMovement so we don't shoot downwards right after climbing
        playerMovement.SetVelocity(Vector3.zero);
    }

    private void StopClimbing()
    {
        isClimbing = false;
        if (!isVaulting)
        {
            playerMovement.isClimbing = false;
            // Reset velocity to ensure no sudden drops when detaching from the wall
            playerMovement.SetVelocity(Vector3.zero);
        }
    }

    private void StartVault()
    {
        isClimbing = false;
        isVaulting = true;
        vaultTimer = vaultDuration;
    }

    private void VaultMovement()
    {
        // Move the player forward and slightly up to get over the ledge smoothly
        Vector3 vaultVelocity = (transform.forward + Vector3.up * 0.8f).normalized * vaultSpeed;
        controller.Move(vaultVelocity * Time.deltaTime);
        
        vaultTimer -= Time.deltaTime;
        if (vaultTimer <= 0)
        {
            isVaulting = false;
            playerMovement.isClimbing = false; // Restore normal movement
            // Reset velocity so gravity starts from zero when falling from the wall
            playerMovement.SetVelocity(Vector3.zero);
        }
    }
}
