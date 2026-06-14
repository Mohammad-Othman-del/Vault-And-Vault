using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [HideInInspector] public float xRotation = 0f;

    void Start()
    {
        // Locks the cursor to the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Pointer.current == null) return;

        // Get mouse delta (how much the mouse moved this frame)
        Vector2 mouseDelta = Pointer.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // Vertical rotation (Looking up and down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevents flipping over backward

        // We only apply the baseline X rotation here.
        // CameraAnimator.cs will run in LateUpdate to add procedural effects on top of this.
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation (Turning the whole player body)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}