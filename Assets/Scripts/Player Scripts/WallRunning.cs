using System.Collections;
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

    [Header("Wall Run Duration")]
    [Tooltip("How many seconds the player can stay on a wall before falling off.")]
    public float maxWallRunTime = 2f;

    [Header("Wall Turn (Q)")]
    [Tooltip("How long the smooth 90° turn takes in seconds.")]
    public float turnDuration = 0.2f;

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
    private CameraAnimator cameraAnimator;

    private bool isWallRight;
    private bool isWallLeft;
    private RaycastHit rightWallHit;
    private RaycastHit leftWallHit;

    private List<Transform> usedWalls = new List<Transform>();
    private Transform lastWall;

    private float exitWallTimer;
    private float wallRunTimer;

    private bool isTurning = false;
    private float postTurnTimer = 0f;
    private Vector3 storedWallNormal;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraAnimator = GetComponentInChildren<CameraAnimator>();
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
            exitWallTimer -= Time.deltaTime;

        bool wallConditions = (isWallLeft || isWallRight) && movingForward && !playerMovement.isGrounded && !playerMovement.isClimbing && exitWallTimer <= 0;

        if (wallConditions)
        {
            if (!playerMovement.isWallRunning)
                StartWallRun();
        }
        else
        {
            // Don't stop during the post-turn grace — the stored wall normal keeps the player stuck
            if (playerMovement.isWallRunning && postTurnTimer <= 0f)
                StopWallRun();
        }

        if (playerMovement.isWallRunning)
        {
            if (postTurnTimer > 0f)
                postTurnTimer -= Time.deltaTime;

            wallRunTimer -= Time.deltaTime;
            if (wallRunTimer <= 0f)
            {
                StopWallRun();
                return;
            }

            WallRunningMovement();

            if (Input.GetButtonDown("Jump"))
                WallJump();

            if (Input.GetKeyDown(KeyCode.Q) && !isTurning)
                WallTurn();
        }

        if (playerMovement.isGrounded)
            ResetUsedWalls();
    }

    private void StartWallRun()
    {
        playerMovement.isWallRunning = true;
        playerMovement.SetVelocity(Vector3.zero);

        wallRunTimer = maxWallRunTime;
        lastWall = isWallRight ? rightWallHit.transform : leftWallHit.transform;

        if (cameraAnimator != null)
            cameraAnimator.TiltCamera(isWallRight ? 15f : -15f);
    }

    private void WallRunningMovement()
    {
        // During post-turn grace period: use the stored wall normal to keep the player on the wall
        if (postTurnTimer > 0f)
        {
            // If a perpendicular wall is found (corner), snap to it immediately
            if (isWallLeft || isWallRight)
            {
                postTurnTimer = 0f;
                lastWall = isWallRight ? rightWallHit.transform : leftWallHit.transform;
                if (cameraAnimator != null)
                    cameraAnimator.TiltCamera(isWallRight ? 15f : -15f);
                // Fall through to normal movement
            }
            else
            {
                // No new wall — stick to old wall and let the player move in their new facing direction
                float speedToUse = Mathf.Max(wallRunSpeed, playerMovement.currentMoveSpeed);
                controller.Move((transform.forward * speedToUse - storedWallNormal * wallStickForce) * Time.deltaTime);
                return;
            }
        }

        // Normal wall run movement
        if (lastWall == null)
        {
            lastWall = isWallRight ? rightWallHit.transform : leftWallHit.transform;
            if (cameraAnimator != null)
                cameraAnimator.TiltCamera(isWallRight ? 15f : -15f);
        }

        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if (Vector3.Dot(transform.forward, wallForward) < 0)
            wallForward = -wallForward;

        float speed = Mathf.Max(wallRunSpeed, playerMovement.currentMoveSpeed);
        controller.Move((wallForward * speed - wallNormal * wallStickForce) * Time.deltaTime);
    }

    private void WallTurn()
    {
        // Store the wall normal now, before we rotate away from it
        storedWallNormal = isWallLeft ? leftWallHit.normal : rightWallHit.normal;

        // Mark current wall as used so the player can't snap back to it
        if (lastWall != null && !usedWalls.Contains(lastWall))
            usedWalls.Add(lastWall);
        lastWall = null;

        // Wall on left → turn right (+90°), wall on right → turn left (−90°)
        float turnAngle = isWallLeft ? 90f : -90f;
        StartCoroutine(SmoothTurnCoroutine(turnAngle));

        // Give a fresh 2-second window on the new wall
        wallRunTimer = maxWallRunTime;
        postTurnTimer = maxWallRunTime;

        if (cameraAnimator != null)
            cameraAnimator.TiltCamera(0f);
    }

    // Applies the rotation as small per-frame increments so MouseLook can still run freely at the same time
    private IEnumerator SmoothTurnCoroutine(float totalAngle)
    {
        isTurning = true;
        float elapsed = 0f;
        float prevSmoothed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / turnDuration);
            float smoothed = Mathf.SmoothStep(0f, 1f, t);
            float delta = (smoothed - prevSmoothed) * totalAngle;
            prevSmoothed = smoothed;
            transform.Rotate(0f, delta, 0f, Space.World);
            yield return null;
        }

        isTurning = false;
    }

    private void WallJump()
    {
        Vector3 wallNormal = postTurnTimer > 0f ? storedWallNormal
                           : isWallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if (Vector3.Dot(transform.forward, wallForward) < 0)
            wallForward = -wallForward;

        Vector3 jumpVelocity = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce + wallForward * wallJumpForwardForce;
        playerMovement.SetVelocity(jumpVelocity);

        StopWallRun();
        exitWallTimer = exitWallCooldown;
    }

    private void StopWallRun()
    {
        playerMovement.isWallRunning = false;
        postTurnTimer = 0f;

        if (lastWall != null && !usedWalls.Contains(lastWall))
        {
            usedWalls.Add(lastWall);
            lastWall = null;
        }

        if (cameraAnimator != null)
            cameraAnimator.TiltCamera(0f);
    }

    public void ResetUsedWalls()
    {
        usedWalls.Clear();
    }
}
