using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpHeight = 1.8f;
    public float gravity = -20f;
    [Tooltip("How much gravity affects the player while wall running (e.g., 0.2 means 20% normal gravity).")]
    public float wallRunGravityMultiplier = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;

    [HideInInspector] public bool isGrounded; // WallRun reads this
    [HideInInspector] public bool isClimbing; // Set by WallClimbing script
    [HideInInspector] public bool isWallRunning; // Set by WallRunning script

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (isClimbing) return; // Skip normal movement and gravity when climbing

        // Ground check using both the sphere cast and the CharacterController's built-in check
        // This prevents gravity from building up if standing on an object (like a wall) that isn't in the groundMask
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask) || controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to stay grounded
            // Clear horizontal momentum from wall jumps when landing
            velocity.x = 0f;
            velocity.z = 0f;
        }

        if (!isWallRunning)
        {
            // WASD / Arrow key input
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // Move relative to where the player is facing
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);

            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
}