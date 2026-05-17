using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float initialMoveSpeed = 7f;
    public float maxMoveSpeed = 15f;
    public float timeToMaxSpeed = 3f;
    public float jumpHeight = 1.8f;
    public float gravity = -20f;
    [Tooltip("How much gravity affects the player while wall running (e.g., 0.2 means 20% normal gravity).")]
    public float wallRunGravityMultiplier = 0.2f;

    [Header("Camera Effects")]
    public CameraAnimator cameraAnimator;

    [Header("Sliding & Vaulting")]
    public float vaultDuration = 0.4f;
    public float vaultHeight = 1.2f;
    public float vaultForwardDistance = 2.5f;
    public LayerMask obstacleMask;

    [HideInInspector] public bool isSliding;
    [HideInInspector] public bool isVaulting;
    [HideInInspector] public bool isSwinging;
    [HideInInspector] public bool isPhysicallyMoving;
    
    private float normalHeight;
    private Vector3 normalCenter;

    [HideInInspector] public float currentMoveSpeed = 0f;
    private float runTimer = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastPosition;

    [HideInInspector] public bool isGrounded; // WallRun reads this
    [HideInInspector] public bool isClimbing; // Set by WallClimbing script
    [HideInInspector] public bool isWallSliding; // Set by WallClimbing script
    [HideInInspector] public bool isWallRunning; // Set by WallRunning script

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraAnimator == null) cameraAnimator = GetComponentInChildren<CameraAnimator>();

        normalHeight = controller.height;
        normalCenter = controller.center;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isClimbing || isVaulting || isWallSliding || isSwinging) return; // Skip normal movement and gravity when interacting with walls/obstacles/bars

        bool wasGrounded = isGrounded;
        // Ground check using both the sphere cast and the CharacterController's built-in check
        // This prevents gravity from building up if standing on an object (like a wall) that isn't in the groundMask
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask) || controller.isGrounded;

        if (!wasGrounded && isGrounded && velocity.y < -2f)
        {
            if (cameraAnimator != null) cameraAnimator.TriggerLand();
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to stay grounded
            // Clear horizontal momentum from wall jumps when landing
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Always track input for momentum
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Distance check to ensure they aren't just walking into a wall
        Vector3 currentPos = transform.position;
        currentPos.y = 0; // Ignore vertical movement (jumping/falling)
        Vector3 prevPos = lastPosition;
        prevPos.y = 0;
        
        float distanceMovedH = Vector3.Distance(currentPos, prevPos);
        isPhysicallyMoving = distanceMovedH > 0.005f; // Must be actually moving horizontally
        lastPosition = transform.position;

        bool isTryingToMove = Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f;

        // Only build sprint if grounded, pressing keys, and actually making distance
        if (isTryingToMove && isGrounded && isPhysicallyMoving)
        {
            runTimer += Time.deltaTime;
            runTimer = Mathf.Clamp(runTimer, 0f, timeToMaxSpeed);
        }
        else if (isGrounded && (!isTryingToMove || !isPhysicallyMoving))
        {
            // Reset sprint if stopped on the ground or stuck on a wall
            runTimer = 0f;
        }

        // Apply speed calculation
        if (isTryingToMove || isWallRunning || !isGrounded)
        {
            float t = runTimer / timeToMaxSpeed;
            // SmoothStep creates an organic S-curve (eases in, accelerates, then eases out)
            currentMoveSpeed = Mathf.SmoothStep(initialMoveSpeed, maxMoveSpeed, t);
        }
        else
        {
            currentMoveSpeed = 0f;
        }

        if (!isWallRunning)
        {
            // Move relative to where the player is facing
            Vector3 move = transform.right * x + transform.forward * z;
            if (move.magnitude > 1f) move.Normalize();

            // Slide Logic
            bool hittingMaxSpeed = currentMoveSpeed >= maxMoveSpeed * 0.9f;

            if (Input.GetKey(KeyCode.LeftControl) && hittingMaxSpeed && isGrounded && !isWallRunning)
            {
                if (!isSliding)
                {
                    isSliding = true;
                    controller.height = normalHeight / 2f;
                    controller.center = normalCenter + Vector3.down * (normalHeight / 4f);
                    if (cameraAnimator != null) cameraAnimator.SetSlide(true);
                }
                
                // Sliding preserves momentum but restricts steering
                move = transform.forward; // Lock movement forward while sliding
            }
            else
            {
                if (isSliding)
                {
                    isSliding = false;
                    controller.height = normalHeight;
                    controller.center = normalCenter;
                    if (cameraAnimator != null) cameraAnimator.SetSlide(false);
                }
            }

            // Vault Logic
            if (hittingMaxSpeed && isGrounded && !isWallRunning && !isSliding)
            {
                if (CheckVault()) return;
            }

            controller.Move(move * currentMoveSpeed * Time.deltaTime);

            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (cameraAnimator != null) cameraAnimator.TriggerJump();
            }
        }

        // Apply gravity
        float currentGravity = isWallRunning ? gravity * wallRunGravityMultiplier : gravity;
        velocity.y += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Called by WallRun to launch the player off a wall
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
    }

    private bool CheckVault()
    {
        // Knee height
        Vector3 lowRay = transform.position + Vector3.down * (normalHeight / 4f);
        // Chest height
        Vector3 highRay = transform.position + Vector3.up * (normalHeight / 4f);
        float rayDistance = 1.5f;

        // If low hits but high doesn't, it's a vaultable obstacle
        if (Physics.Raycast(lowRay, transform.forward, out RaycastHit hit, rayDistance, groundMask | obstacleMask))
        {
            if (!Physics.Raycast(highRay, transform.forward, rayDistance, groundMask | obstacleMask))
            {
                StartCoroutine(VaultRoutine());
                return true;
            }
        }
        return false;
    }

    private IEnumerator VaultRoutine()
    {
        isVaulting = true;
        
        if (cameraAnimator != null) cameraAnimator.TriggerVault(vaultDuration);

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * vaultForwardDistance;
        
        float t = 0f;
        controller.enabled = false; // Disable to force manual position over obstacle

        while (t < 1f)
        {
            t += Time.deltaTime / vaultDuration;
            
            float yOffset = Mathf.Sin(t * Mathf.PI) * vaultHeight;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += yOffset;

            transform.position = currentPos;

            yield return null;
        }

        controller.enabled = true;
        isVaulting = false;
    }
}