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

    [Header("Wall Detection")]
    public LayerMask climbWallMask;
    public float wallDetectionDistance = 0.7f;

    [Tooltip("Maximum angle (in degrees) from facing the wall perfectly head-on to allow climbing or sliding.")]
    public float maxAngleFromWall = 30f;

    [Header("Vaulting Settings")]
    [Tooltip("Height offset to check if we've reached the top of the wall.")]
    public float topCheckHeight = 1f;
    public float vaultSpeed = 6f;
    public float vaultDuration = 0.4f;

    [Header("Wall Jump Settings")]
    public int maxWallJumps = 2;
    private int jumpsRemaining;
    public float wallJumpBackForce = 6f;
    public float wallJumpUpForce = 5f;

    [Header("Wall Turn Settings")]
    [Tooltip("Key to rotate 165 degrees while clinging to a wall")]
    public KeyCode wallTurnKey = KeyCode.Q;
    [Tooltip("How long (seconds) the smooth rotation takes")]
    public float wallTurnRotationDuration = 0.25f;
    [Tooltip("How long (seconds) grip is maintained after turning")]
    public float wallTurnGraceTime = 1.5f;

    private bool isClimbing;
    private bool isVaulting;
    private float vaultTimer;

    private bool isInWallTurnWindow;
    private float wallTurnTimer;
    private Coroutine wallTurnCoroutine;

    private RaycastHit frontClimbHit;
    private bool climbWallInFront;
    private bool topClimbWallInFront;
    private Vector3 climbNormal;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
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
        // When climbing, we keep checking in the direction of the wall to stay attached even if looking away
        Vector3 checkDir = isClimbing ? -climbNormal : transform.forward;

        // Raycast for Climb walls
        climbWallInFront = Physics.Raycast(transform.position, checkDir, out frontClimbHit, wallDetectionDistance, climbWallMask);

        // Raycast from higher up to check if the climb wall has ended (ledge detection)
        Vector3 topRayStart = transform.position + Vector3.up * topCheckHeight;
        topClimbWallInFront = Physics.Raycast(topRayStart, checkDir, wallDetectionDistance, climbWallMask);
    }

    private void StateMachine()
    {
        if (playerMovement.isWallRunning) return; // Cannot climb and run at the same time

        if (isVaulting)
        {
            VaultMovement();
            return; // Skip climbing logic while vaulting
        }

        // Q: initiate wall turn while climbing
        if (isClimbing && !isInWallTurnWindow && Input.GetKeyDown(wallTurnKey))
            StartWallTurn();

        // Count down the grace window
        if (isInWallTurnWindow)
        {
            wallTurnTimer -= Time.deltaTime;
            if (wallTurnTimer <= 0f)
            {
                isInWallTurnWindow = false;
                StopClimbing();
            }
        }

        bool isMovingForward = Input.GetAxisRaw("Vertical") > 0;
        bool shouldStayAttached = true;

        if (isClimbing && !isInWallTurnWindow)
        {
            // If the player turns around (more than 90 degrees away from the wall), they must detach and fall
            float angleToClimb = Vector3.Angle(transform.forward, -climbNormal);
            if (angleToClimb > 90f)
                shouldStayAttached = false;
        }

        // 1. Evaluate Climb Wall
        if (climbWallInFront && isMovingForward && !playerMovement.isGrounded && shouldStayAttached)
        {
            if (!isClimbing && climbTimer > 0)
            {
                float angleToClimb = Vector3.Angle(transform.forward, -frontClimbHit.normal);
                bool isLookingAtClimbWall = angleToClimb < maxAngleFromWall;

                if (isLookingAtClimbWall)
                {
                    StartClimbing();
                    climbNormal = frontClimbHit.normal;
                }
            }
        }
        else
        {
            if (isClimbing)
            {
                StopClimbing();
            }
        }

        // Process active states
        if (isClimbing)
        {
            ClimbingMovement();
            
            // If we are climbing and the top raycast doesn't hit anything, we reached the ledge
            if (!topClimbWallInFront)
            {
                float angleToWall = Vector3.Angle(transform.forward, -climbNormal);
                if (angleToWall < maxAngleFromWall)
                {
                    StartVault();
                }
                else
                {
                    // Fall if looking away at the ledge
                    StopClimbing();
                }
            }
        }

        // Wall Jumping - Allowed if not grounded and there is a wall behind the player (e.g., after turning around)
        if (!playerMovement.isGrounded && !isVaulting && Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            // SphereCast backwards to find the wall we just turned away from
            if (Physics.SphereCast(transform.position, 0.4f, -transform.forward, out RaycastHit backHit, wallDetectionDistance + 0.2f, climbWallMask))
            {
                WallJump(backHit.normal);
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
        // Kill existing velocity when attaching
        playerMovement.SetVelocity(Vector3.zero);
    }

    private void ClimbingMovement()
    {
        // During the wall-turn grace window, just hang — no climbing, no falling
        if (isInWallTurnWindow)
        {
            playerMovement.SetVelocity(Vector3.zero);
            return;
        }

        // Move the player directly upwards
        Vector3 climbVelocity = Vector3.up * climbSpeed;
        controller.Move(climbVelocity * Time.deltaTime);

        // Decrease the available climb time
        climbTimer -= Time.deltaTime;
        if (climbTimer < 0)
            StopClimbing();

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

    private void WallJump(Vector3 wallNormal)
    {
        isInWallTurnWindow = false;
        if (wallTurnCoroutine != null) { StopCoroutine(wallTurnCoroutine); wallTurnCoroutine = null; }
        if (isClimbing) StopClimbing();
        jumpsRemaining--;

        // Push the player up and away from the wall
        Vector3 jumpVelocity = (Vector3.up * wallJumpUpForce) + (wallNormal * wallJumpBackForce);
        playerMovement.SetVelocity(jumpVelocity);
    }

    private void StartWallTurn()
    {
        isInWallTurnWindow = true;
        wallTurnTimer = wallTurnGraceTime;

        // Detect which side (left/right) has another climbable wall; default to left
        float angle = 165f;
        Vector3 rightDir = transform.right;
        Vector3 leftDir = -transform.right;
        if (Physics.Raycast(transform.position, rightDir, wallDetectionDistance * 3f, climbWallMask))
            angle = 165f;
        else if (Physics.Raycast(transform.position, leftDir, wallDetectionDistance * 3f, climbWallMask))
            angle = -165f;

        if (wallTurnCoroutine != null) StopCoroutine(wallTurnCoroutine);
        wallTurnCoroutine = StartCoroutine(RotatePlayer(angle));
    }

    private IEnumerator RotatePlayer(float angle)
    {
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, angle, 0f);

        while (elapsed < wallTurnRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / wallTurnRotationDuration);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.rotation = endRot;
        wallTurnCoroutine = null;
    }
}
