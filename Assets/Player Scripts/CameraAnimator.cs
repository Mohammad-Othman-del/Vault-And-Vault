using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraAnimator : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;
    public CharacterController controller;
    
    [Header("Volume Effects (High Speed)")]
    public Volume speedVolume;
    public float maxSpeedFOV = 80f;
    private float defaultFOV;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private MotionBlur motionBlur;

    [Header("Head Bob")]
    public float bobFrequency = 15f;
    public float bobAmplitudeY = 0.05f;
    public float bobAmplitudeX = 0.03f;
    public float bobTiltAmplitude = 1.5f; // Procedural Z-roll during footsteps
    private float bobTimer = 0f;
    private float bobMultiplier = 0f;

    [Header("Wall Run Tilt")]
    public float wallRunTiltAmount = 15f;
    public float tiltSmoothness = 6f;
    private float targetTiltZ = 0f;
    private float currentTiltZ = 0f;

    [Header("Jump / Land Shake")]
    public float jumpDipAmount = 5f; // Positive looks down
    public float landDipAmount = 8f;
    public float shakeRecoverSpeed = 8f;
    private float currentPitchOffset = 0f;

    [Header("Climbing Animation")]
    public float climbBobFrequency = 8f;
    public float climbBobAmplitudeY = 0.08f;
    public float climbTiltAmplitude = 2f;
    public float climbPitchAmplitude = 2f;
    private float climbTimer = 0f;

    [Header("Slide Animation")]
    public float slideYOffset = -0.5f;
    public float slidePitch = -6f; // Tilt head back slightly (looking up)
    public float slideTilt = -3f;  // Lean slightly to the side
    public float slideSmoothness = 8f;
    
    private float targetSlideY = 0f;
    private float currentSlideY = 0f;
    private float targetSlidePitch = 0f;
    private float currentSlidePitch = 0f;
    private float targetSlideTilt = 0f;
    private float currentSlideTilt = 0f;
    private float baseLocalY;

    [Header("Sprint Leaning")]
    public float maxSprintPitch = 3f; // Positive looks down slightly (leaning forward)

    private bool isVaultingCam = false;
    private float currentVaultRoll = 0f;

    void Start()
    {
        if (mouseLook == null) mouseLook = GetComponent<MouseLook>();
        
        Camera cam = GetComponent<Camera>();
        if (cam != null) defaultFOV = cam.fieldOfView;
        
        baseLocalY = transform.localPosition.y;

        if (speedVolume != null && speedVolume.profile != null)
        {
            speedVolume.profile.TryGet(out chromaticAberration);
            speedVolume.profile.TryGet(out lensDistortion);
            speedVolume.profile.TryGet(out motionBlur);
        }
    }

    void LateUpdate()
    {
        if (playerMovement == null || mouseLook == null || controller == null) return;

        UpdateHeadBob();
        UpdateVolumeEffects();

        // Smooth out tilt (Z) and slide (Y/Pitch/Tilt)
        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, Time.deltaTime * tiltSmoothness);
        currentSlideY = Mathf.Lerp(currentSlideY, targetSlideY, Time.deltaTime * slideSmoothness);
        currentSlidePitch = Mathf.Lerp(currentSlidePitch, targetSlidePitch, Time.deltaTime * slideSmoothness);
        currentSlideTilt = Mathf.Lerp(currentSlideTilt, targetSlideTilt, Time.deltaTime * slideSmoothness);

        // Smooth out pitch (X) recovery if we aren't currently overriding it with the Vault coroutine
        if (!isVaultingCam)
        {
            currentPitchOffset = Mathf.Lerp(currentPitchOffset, 0f, Time.deltaTime * shakeRecoverSpeed);
        }

        // --- Climb Logic ---
        float climbOffsetY = 0f;
        float climbTiltZ = 0f;
        float climbPitchX = 0f;

        if (playerMovement.isClimbing)
        {
            if (controller.velocity.magnitude > 0.1f)
            {
                climbTimer += Time.deltaTime * climbBobFrequency;
            }
            climbOffsetY = Mathf.Abs(Mathf.Sin(climbTimer)) * -climbBobAmplitudeY; // Pulls down every stroke
            climbTiltZ = Mathf.Sin(climbTimer / 2f) * climbTiltAmplitude; // Rocks left to right
            climbPitchX = Mathf.Abs(Mathf.Sin(climbTimer)) * climbPitchAmplitude; // Nods forward slightly
        }
        else
        {
            climbTimer = 0f;
        }

        // Apply Position Offset
        Vector3 newPos = transform.localPosition;
        newPos.y = baseLocalY + currentSlideY + GetBobOffsetY() + climbOffsetY;
        newPos.x = GetBobOffsetX();
        transform.localPosition = newPos;

        // Calculate a gentle forward lean based on how close we are to max speed
        float speedRatio = Mathf.InverseLerp(playerMovement.initialMoveSpeed, playerMovement.maxMoveSpeed, playerMovement.currentMoveSpeed);
        float sprintPitch = Mathf.Lerp(0f, maxSprintPitch, speedRatio);

        // Apply Rotational Offset 
        float totalPitch = mouseLook.xRotation + currentPitchOffset + sprintPitch + currentSlidePitch + climbPitchX;
        float totalTilt = currentTiltZ + currentSlideTilt + climbTiltZ + currentVaultRoll + GetBobOffsetZ();
        
        transform.localRotation = Quaternion.Euler(totalPitch, 0f, totalTilt);
    }

    private void UpdateHeadBob()
    {
        bool isMovingOnGround = playerMovement.isGrounded && !playerMovement.isSliding && controller.velocity.magnitude > 0.1f;
        
        // Smoothly ramp bob intensity up and down to prevent snapping when starting/stopping
        bobMultiplier = Mathf.Lerp(bobMultiplier, isMovingOnGround ? 1f : 0f, Time.deltaTime * 5f);
        
        if (bobMultiplier > 0.01f)
        {
            // Speed of bob scales slightly with velocity
            float speedRatio = Mathf.Clamp01(controller.velocity.magnitude / playerMovement.maxMoveSpeed);
            bobTimer += Time.deltaTime * bobFrequency * Mathf.Max(0.5f, speedRatio);
        }
    }

    private float GetBobOffsetY()
    {
        float amplitudeScale = 0.5f + (Mathf.Clamp01(controller.velocity.magnitude / playerMovement.maxMoveSpeed) * 0.5f);
        return Mathf.Sin(bobTimer) * bobAmplitudeY * bobMultiplier * amplitudeScale;
    }

    private float GetBobOffsetX()
    {
        float amplitudeScale = 0.5f + (Mathf.Clamp01(controller.velocity.magnitude / playerMovement.maxMoveSpeed) * 0.5f);
        return Mathf.Cos(bobTimer / 2f) * bobAmplitudeX * bobMultiplier * amplitudeScale;
    }

    private float GetBobOffsetZ()
    {
        float amplitudeScale = 0.5f + (Mathf.Clamp01(controller.velocity.magnitude / playerMovement.maxMoveSpeed) * 0.5f);
        return Mathf.Cos(bobTimer / 2f) * bobTiltAmplitude * bobMultiplier * amplitudeScale;
    }

    private float smoothEffectIntensity = 0f;
    private float smoothFovIntensity = 0f;

    private void UpdateVolumeEffects()
    {
        // Calculate the raw target intensities based on current physical speed
        float targetEffectIntensity = Mathf.InverseLerp(playerMovement.maxMoveSpeed * 0.8f, playerMovement.maxMoveSpeed, playerMovement.currentMoveSpeed);
        float targetFovIntensity = Mathf.InverseLerp(playerMovement.initialMoveSpeed, playerMovement.maxMoveSpeed, playerMovement.currentMoveSpeed);

        // Smoothly lerp the visual intensities so they fade out realistically when stopping
        smoothEffectIntensity = Mathf.Lerp(smoothEffectIntensity, targetEffectIntensity, Time.deltaTime * 1f);
        smoothFovIntensity = Mathf.Lerp(smoothFovIntensity, targetFovIntensity, Time.deltaTime * 1f);

        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(defaultFOV, maxSpeedFOV, smoothFovIntensity);
        }

        // Reduced intensities to keep the effect subtle and focused on the edges
        if (chromaticAberration != null) chromaticAberration.intensity.Override(Mathf.Lerp(0f, 0.45f, smoothEffectIntensity));
        if (lensDistortion != null) lensDistortion.intensity.Override(Mathf.Lerp(0f, -0.15f, smoothEffectIntensity));
        if (motionBlur != null) motionBlur.intensity.Override(Mathf.Lerp(0f, 0.3f, smoothEffectIntensity));
    }

    // --- Public Triggers ---

    public void TriggerJump()
    {
        currentPitchOffset = jumpDipAmount;
    }

    public void TriggerLand()
    {
        currentPitchOffset = landDipAmount;
    }

    public void TriggerVault(float duration)
    {
        StartCoroutine(VaultDipRoutine(duration));
    }

    public void SetSlide(bool isSliding)
    {
        targetSlideY = isSliding ? slideYOffset : 0f;
        targetSlidePitch = isSliding ? slidePitch : 0f;
        targetSlideTilt = isSliding ? slideTilt : 0f;
    }

    public void TiltCamera(float zAngle)
    {
        targetTiltZ = zAngle;
    }

    private IEnumerator VaultDipRoutine(float duration)
    {
        isVaultingCam = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 0 to 1

            // Complex Vault Animation:
            // Pitch: arc down to ~18 degrees, then back up to 0
            // Roll: arc to one side (e.g. -8 degrees)
            
            currentPitchOffset = Mathf.Sin(t * Mathf.PI) * 18f;
            currentVaultRoll = Mathf.Sin(t * Mathf.PI) * -8f;

            yield return null;
        }
        
        currentPitchOffset = 0f;
        currentVaultRoll = 0f;
        isVaultingCam = false;
    }

    public void TriggerTurnaroundJump(float duration)
    {
        currentPitchOffset = jumpDipAmount * 1.5f; // Harder dip than a normal jump
        StartCoroutine(TurnaroundRollRoutine(duration));
    }

    private IEnumerator TurnaroundRollRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Sideways roll to sell the centrifugal force of spinning 180
            currentVaultRoll = Mathf.Sin(t * Mathf.PI) * 15f; 

            yield return null;
        }

        currentVaultRoll = 0f;
    }
}
