using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class WallRunning : MonoBehaviour
{
    [Header("Wall Running")]
    public float wallRunSpeed = 8f;
    [Tooltip("Small force pushing the player into the wall to ensure they stay stuck to it.")]
    public float wallStickForce = 4f;
    
    [Header("Wall Jumping")]
    public float wallJumpUpForce = 5f;
    public float wallJumpSideForce = 8f;
    public float wallJumpForwardForce = 4f;
    public float exitWallCooldown = 0.2f;

    [Header("Detection")]
    public LayerMask wallMask;
    public float wallCheckDistance = 0.8f;
    
    private CharacterController controller;
    private PlayerMovement playerMovement;

    private bool isWallRight;
    private bool isWallLeft;
    private RaycastHit rightWallHit;
    private RaycastHit leftWallHit;

    // Tracks walls we've already run on so we can't use them again
    private List<Transform> usedWalls = new List<Transform>();
    private Transform lastWall;

    private float exitWallTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, wallMask);
        if (isWallRight && usedWalls.Contains(rightWallHit.transform)) isWallRight = false;

        isWallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, wallMask);
        if (isWallLeft && usedWalls.Contains(leftWallHit.transform)) isWallLeft = false;
    }

    private void StateMachine()
    {
        bool movingForward = Input.GetAxisRaw("Vertical") > 0;
        
        if (exitWallTimer > 0)
        {
            exitWallTimer -= Time.deltaTime;
        }

        // Trigger Wall Run: Wall on either side, moving forward, not grounded, not climbing
        if ((isWallLeft || isWallRight) && movingForward && !playerMovement.isGrounded && !playerMovement.isClimbing && exitWallTimer <= 0)
        {
            if (!playerMovement.isWallRunning)
            {
                StartWallRun();
            }
        }
        else
        {
            if (playerMovement.isWallRunning)
            {
                StopWallRun();
            }
        }

        if (playerMovement.isWallRunning)
        {
            WallRunningMovement();
            
            if (Input.GetButtonDown("Jump"))
            {
                WallJump();
            }
        }

        // Reset the memory of used walls if the player touches the ground
        if (playerMovement.isGrounded)
        {
            ResetUsedWalls();
        }
    }

    private void StartWallRun()
    {
        playerMovement.isWallRunning = true;
        // Reset velocity so previous gravity buildup is cleared
        playerMovement.SetVelocity(Vector3.zero); 
        
        // Remember which wall we are currently on so we can mark it as used when we leave it
        lastWall = isWallRight ? rightWallHit.transform : leftWallHit.transform;
    }

    private void WallRunningMovement()
    {
        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;

        // Calculate the direction forward along the wall
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        // Ensure wallForward points in the direction the player is generally facing
        if (Vector3.Dot(transform.forward, wallForward) < 0)
        {
            wallForward = -wallForward;
        }

        // Apply movement forward along the wall
        Vector3 moveVelocity = wallForward * wallRunSpeed;
        
        // Add a slight force pushing into the wall so we don't fall off
        moveVelocity += -wallNormal * wallStickForce;

        // PlayerMovement.cs still applies gravity, dragging the player down automatically
        controller.Move(moveVelocity * Time.deltaTime);
    }

    private void WallJump()
    {
        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if (Vector3.Dot(transform.forward, wallForward) < 0)
        {
            wallForward = -wallForward;
        }

        // Apply velocity outwards, upwards, and slightly forwards
        Vector3 jumpVelocity = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce + wallForward * wallJumpForwardForce;

        playerMovement.SetVelocity(jumpVelocity);

        StopWallRun();
        
        // Prevent immediately sticking back to the same wall
        exitWallTimer = exitWallCooldown;
    }

    private void StopWallRun()
    {
        playerMovement.isWallRunning = false;

        // Now that we have left the wall, mark it as used so we can't stick to it again
        if (lastWall != null && !usedWalls.Contains(lastWall))
        {
            usedWalls.Add(lastWall);
            lastWall = null;
        }
    }

    // Call this method from your GameManager or Death script when the player finishes or loses
    // Or you can call it from Update() if you decide you want it to reset when touching the ground.
    public void ResetUsedWalls()
    {
        usedWalls.Clear();
    }
}
