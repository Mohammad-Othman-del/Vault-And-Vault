using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(CharacterController))]
public class BarSwinging : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask barMask;
    public float detectionRadius = 0.6f;
    public float detectionDistance = 1.5f;

    [Header("Swing Physics")]
    public float maxSwingAngle = 45f;
    public float swingSpeed = 4f;
    public float dampingRate = 5f; // How fast max angle decays
    public float armLength = 1.5f; // Distance from bar to player center

    [Header("Jump Off")]
    public float jumpLaunchForce = 12f;
    public float jumpUpForce = 6f;
    // public float flipDuration = 0.5f;

    private PlayerMovement playerMovement;
    private CameraAnimator cameraAnimator;
    private CharacterController controller;
    
    private Transform currentBar;
    private Vector3 hingePoint;
    private Vector3 swingAxis; 
    
    private float swingTimer = 0f;
    private float currentMaxAngle;
    private float currentAngle = 0f;
    private float cooldownTimer = 0f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        cameraAnimator = GetComponentInChildren<CameraAnimator>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (!playerMovement.isSwinging)
        {
            if (cooldownTimer <= 0f && !playerMovement.isGrounded && !playerMovement.isClimbing && !playerMovement.isWallRunning)
            {
                CheckForBar();
            }
        }
        else
        {
            HandleSwinging();
            if (Input.GetButtonDown("Jump"))
            {
                JumpOff();
            }
        }
    }

    private void CheckForBar()
    {
        // Cast slightly forward and up to detect the bar
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(origin, detectionRadius, transform.forward + Vector3.up, out RaycastHit hit, detectionDistance, barMask))
        {
            StartSwinging(hit);
        }
    }

    private void StartSwinging(RaycastHit hit)
    {
        playerMovement.isSwinging = true;
        currentBar = hit.transform;
        
        // We swing forward/backward relative to where the player is looking when they grabbed the bar
        swingAxis = transform.right; 
        swingAxis.y = 0;
        swingAxis.Normalize();

        // Approximate the center of the bar
        hingePoint = hit.point;
        
        // Start the swing timer at an offset so the player drops into the swing naturally
        swingTimer = Mathf.PI / 2f; 
        currentMaxAngle = maxSwingAngle;
        
        playerMovement.SetVelocity(Vector3.zero);
        controller.enabled = false; 
    }

    private void HandleSwinging()
    {
        swingTimer += Time.deltaTime * swingSpeed;
        
        // Dampen the swing over time
        currentMaxAngle = Mathf.Max(0f, currentMaxAngle - dampingRate * Time.deltaTime);

        // Calculate current arc angle
        currentAngle = Mathf.Cos(swingTimer) * currentMaxAngle;

        // Remove forced camera pitch so the player has complete free look
        if (cameraAnimator != null)
        {
            cameraAnimator.SetSwingPitch(0f);
        }

        // Restore the physical Pendulum Math: 
        // This swings the body back and forth beneath the bar.
        Vector3 offset = Vector3.down * armLength;
        Quaternion rotation = Quaternion.AngleAxis(currentAngle, swingAxis);
        offset = rotation * offset;

        transform.position = hingePoint + offset;

        // Automatically drop if momentum dies completely
        if (currentMaxAngle <= 0.1f)
        {
            Drop();
        }
    }

    private void JumpOff()
    {
        playerMovement.isSwinging = false;
        controller.enabled = true;
        cooldownTimer = 0.5f; 
        
        if (cameraAnimator != null)
        {
            cameraAnimator.SetSwingPitch(0f);
        }

        // If currentAngle < 0, the pendulum is in the forward half of the swing
        if (currentAngle < 0f)
        {
            // if (cameraAnimator != null)
            // {
            //     cameraAnimator.TriggerSwingFlip(flipDuration);
            // }

            // Launch forward and up
            Vector3 launchVelocity = transform.forward * jumpLaunchForce + Vector3.up * jumpUpForce;
            playerMovement.SetVelocity(launchVelocity);
        }
        else
        {
            // The pendulum is in the backward half of the swing. Just let them fall.
            // We give a tiny hop upward so it still feels like a jump, but no flip or forward momentum.
            playerMovement.SetVelocity(Vector3.up * 2f);
        }
    }

    private void Drop()
    {
        playerMovement.isSwinging = false;
        controller.enabled = true;
        cooldownTimer = 0.5f;

        if (cameraAnimator != null)
        {
            cameraAnimator.SetSwingPitch(0f);
        }
    }
}
