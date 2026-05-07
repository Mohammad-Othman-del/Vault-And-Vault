using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Wall Sliding Settings")]
    public float wallSlideSpeed = 2f;
    public int maxWallJumps = 2;
    private int jumpsRemaining;

    [Header("Wall Detection")]
    public LayerMask climbWallMask;
    public LayerMask jumpWallMask;
    public float wallDetectionDistance = 0.7f;
    
    [Tooltip("Maximum angle (in degrees) from facing the wall perfectly head-on to allow climbing or sliding.")]
    public float maxAngleFromWall = 30f; 

    [Header("Vaulting Settings")]
    [Tooltip("Height offset to check if we've reached the top of the wall.")]
    public float topCheckHeight = 1f;
    public float vaultSpeed = 6f;
    public float vaultDuration = 0.4f;

    [Header("Wall Jump Settings")]
    public float wallJumpBackForce = 6f; // Reduced from 8 to make the jump less explosive
    public float wallJumpUpForce = 5f;   // Reduced from 6
    public float turnAroundDuration = 0.6f; // Doubled from 0.3 to make the spin slower and more realistic
    private CameraAnimator cameraAnimator;

    private bool isClimbing;
    private bool isWallSliding;
    private bool isVaulting;
    private float vaultTimer;

    private RaycastHit frontClimbHit;
    private RaycastHit frontJumpHit;
    private bool climbWallInFront;
    private bool jumpWallInFront;
    private bool topClimbWallInFront;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraAnimator = GetComponentInChildren<CameraAnimator>();
        climbTimer = maxClimbTime;
        jumpsRemaining = maxWallJumps;
    }

    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void CheckForWall()
    {
        // Raycast for Climb walls
        climbWallInFront = Physics.Raycast(transform.position, transform.forward, out frontClimbHit, wallDetectionDistance, climbWallMask);
        
        // Raycast for Jump walls
        jumpWallInFront = Physics.Raycast(transform.position, transform.forward, out frontJumpHit, wallDetectionDistance, jumpWallMask);

        // Raycast from higher up to check if the climb wall has ended (ledge detection)
        Vector3 topRayStart = transform.position + Vector3.up * topCheckHeight;
        topClimbWallInFront = Physics.Raycast(topRayStart, transform.forward, wallDetectionDistance, climbWallMask);
    }

    private void StateMachine()
    {
        if (playerMovement.isWallRunning) return; // Cannot climb and run at the same time

        if (isVaulting)
        {
            VaultMovement();
            return; // Skip climbing logic while vaulting
        }

        bool isMovingForward = Input.GetAxisRaw("Vertical") > 0;
        
        float angleToClimb = climbWallInFront ? Vector3.Angle(transform.forward, -frontClimbHit.normal) : 0f;
        float angleToJump = jumpWallInFront ? Vector3.Angle(transform.forward, -frontJumpHit.normal) : 0f;

        bool isLookingAtClimbWall = angleToClimb < maxAngleFromWall;
        bool isLookingAtJumpWall = angleToJump < maxAngleFromWall;

        // 1. Evaluate Climb Wall
        if (climbWallInFront && isMovingForward && isLookingAtClimbWall && !playerMovement.isGrounded)
        {
            if (!isClimbing && climbTimer > 0)
            {
                if (isWallSliding) StopWallSliding();
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

        // 2. Evaluate Jump Wall (Only if not already climbing)
        if (jumpWallInFront && isMovingForward && isLookingAtJumpWall && !playerMovement.isGrounded && !isClimbing)
        {
            if (!isWallSliding)
            {
                StartWallSliding();
            }
        }
        else
        {
            if (isWallSliding)
            {
                StopWallSliding();
            }
        }

        // Process active states
        if (isClimbing)
        {
            ClimbingMovement();
            
            // If we are climbing and the top raycast doesn't hit anything, we reached the ledge
            if (!topClimbWallInFront)
            {
                StartVault();
            }
        }

        if (isWallSliding)
        {
            WallSlidingMovement();

            if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
            {
                WallJump();
                return;
            }
        }

        // Recharge abilities when the player lands
        if (playerMovement.isGrounded)
        {
            if (!isVaulting) climbTimer = maxClimbTime;
            jumpsRemaining = maxWallJumps;
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

    private void StartWallSliding()
    {
        isWallSliding = true;
        playerMovement.isWallSliding = true;
        // Kill existing upward momentum when attaching
        playerMovement.SetVelocity(Vector3.zero);
    }

    private void WallSlidingMovement()
    {
        // Slide down slowly
        controller.Move(Vector3.down * wallSlideSpeed * Time.deltaTime);
        // Prevent gravity buildup
        playerMovement.SetVelocity(Vector3.zero);
    }

    private void StopWallSliding()
    {
        isWallSliding = false;
        playerMovement.isWallSliding = false;
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

    private void WallJump()
    {
        StopWallSliding();
        jumpsRemaining--;
        
        // Push the player up and away from the wall
        Vector3 jumpVelocity = (transform.up * wallJumpUpForce) + (-transform.forward * wallJumpBackForce);
        playerMovement.SetVelocity(jumpVelocity);

        if (cameraAnimator != null)
        {
            cameraAnimator.TriggerTurnaroundJump(turnAroundDuration);
        }

        // Spin the player 180 degrees physically so they face the new wall
        StartCoroutine(TurnAroundRoutine());
    }

    private IEnumerator TurnAroundRoutine()
    {
        float elapsed = 0f;
        float startY = transform.eulerAngles.y;
        float targetY = startY + 180f;

        while (elapsed < turnAroundDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / turnAroundDuration;
            
            // Use SmoothStep for a snappy but smooth spin
            float newY = Mathf.SmoothStep(startY, targetY, t);
            transform.rotation = Quaternion.Euler(0f, newY, 0f);

            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, targetY, 0f);
    }
}
