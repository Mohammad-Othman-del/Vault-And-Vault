using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [HideInInspector] public float xRotation = 0f;

    private int initFrameSkip = 5; // Ignore mouse delta spike for first 5 frames after scene load

    void Awake()
    {
        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
    }

    void Start()
    {
        // Load sensitivity from PlayerPrefs if saved (stored as 1-100 slider value, scale by 2 to map to 2-200 range)
        float sensPref = PlayerPrefs.GetFloat("MouseSensitivity", 50f);
        mouseSensitivity = sensPref * 2f;

        // Locks the cursor to the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        initFrameSkip = 5;
    }

    void Update()
    {
        if (RubberBandBot.isCinematicPlaying || LevelCompleteManager.isLevelCompleteActive || GameOverManager.isGameOverActive) return;

        // Skip mouse delta during initial scene load frames to prevent cursor lock jump spike
        if (initFrameSkip > 0)
        {
            initFrameSkip--;
            xRotation = 0f;
            transform.localRotation = Quaternion.identity;
            return;
        }

        if (Pointer.current == null) return;

        // Get mouse delta (how much the mouse moved this frame)
        Vector2 mouseDelta = Pointer.current.delta.ReadValue();

        // Clamp single-frame mouse delta spike to prevent sudden camera jumps
        float clampedX = Mathf.Clamp(mouseDelta.x, -200f, 200f);
        float clampedY = Mathf.Clamp(mouseDelta.y, -200f, 200f);

        float mouseX = clampedX * mouseSensitivity * Time.deltaTime;
        float mouseY = clampedY * mouseSensitivity * Time.deltaTime;

        // Vertical rotation (Looking up and down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f); // Prevents flipping over backward

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation (Turning the whole player body)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    public void ResetCameraLook()
    {
        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        initFrameSkip = 3;
    }

    /// <summary>
    /// Rotates the player body horizontally and tilts the camera vertically to face a target world position.
    /// </summary>
    public void SetFacingDirection(Vector3 targetWorldPosition)
    {
        Vector3 eyePos = transform.position;
        Vector3 dirToTarget = targetWorldPosition - eyePos;

        if (dirToTarget == Vector3.zero) return;

        Quaternion fullRot = Quaternion.LookRotation(dirToTarget);

        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, fullRot.eulerAngles.y, 0f);
        }

        float pitch = fullRot.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        xRotation = Mathf.Clamp(pitch, -85f, 85f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
