using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public Rigidbody player;
    public float speedForce = 2000f;
    public float jumpForce = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    private bool isGrounded;

    void Start()
    {
        player = GetComponent<Rigidbody>();
        
        // Software Engineering Tip: Freeze rotation so the sphere doesn't 
        // roll like a ball, but slides like a character.
        player.freezeRotation = true;
    }

    void FixedUpdate()
{
    if (player == null || Keyboard.current == null) return;

    // 1. Get raw input values
    float moveX = 0f;
    float moveZ = 0f;

    if (Keyboard.current.wKey.isPressed) moveZ += 1f;
    if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
    if (Keyboard.current.aKey.isPressed) moveX -= 1f;
    if (Keyboard.current.dKey.isPressed) moveX += 1f;

    // 2. CONVERT TO LOCAL DIRECTION
    // 'transform.forward' is the direction the sphere is facing
    // 'transform.right' is the side-to-side direction relative to the sphere
    Vector3 move = transform.right * moveX + transform.forward * moveZ;

    // 3. APPLY FORCE
    // We normalize the vector so that moving diagonally isn't faster than moving straight
    player.AddForce(move.normalized * speedForce * Time.fixedDeltaTime);
}

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            // Use Velocity Change for an instant "pop" off the ground
            player.AddVelocityChange(Vector3.up * jumpForce);
        }
    }
}

// Helper extension to make AddForce feel like a Jump
public static class RigidbodyExtensions
{
    public static void AddVelocityChange(this Rigidbody rb, Vector3 velocity)
    {
        rb.AddForce(velocity, ForceMode.VelocityChange);
    }
}