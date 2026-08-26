using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private enum FootstepType { Walk, Run, Wallrun, Climb }

    [Header("Step Intervals")]
    public float walkStepInterval = 0.42f;
    public float runStepInterval = 0.28f;
    public float wallrunStepInterval = 0.16f;
    public float climbStepInterval = 0.28f;

    private PlayerMovement movement;
    private AudioSource footstepSource;
    private AudioSource impactSource;
    private AudioSource slideSource;
    private AudioSource barHangSource;

    private AudioClip[] walkClips;
    private AudioClip[] runClips;
    private AudioClip[] wallrunClips;
    private AudioClip[] climbClips;
    private AudioClip[] jumpClips;
    private AudioClip[] landClips;
    private AudioClip[] vaultClips;
    private AudioClip slideClip;
    private AudioClip barGrabReleaseClip;
    private AudioClip barHangClip;
    private AudioClip collectClip;

    private float stepTimer = 0f;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();

        // Create and configure AudioSources
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.loop = false;
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;

        impactSource = gameObject.AddComponent<AudioSource>();
        impactSource.loop = false;
        impactSource.playOnAwake = false;
        impactSource.spatialBlend = 0f;

        slideSource = gameObject.AddComponent<AudioSource>();
        slideSource.loop = true;
        slideSource.playOnAwake = false;
        slideSource.spatialBlend = 0f;

        barHangSource = gameObject.AddComponent<AudioSource>();
        barHangSource.loop = true;
        barHangSource.playOnAwake = false;
        barHangSource.spatialBlend = 0f;

        LoadAudioClips();
    }

    void LoadAudioClips()
    {
        walkClips = Resources.LoadAll<AudioClip>("Audio/Player/Footsteps");
        runClips = Resources.LoadAll<AudioClip>("Audio/Player/Run");
        wallrunClips = Resources.LoadAll<AudioClip>("Audio/Player/Wallrun");
        climbClips = Resources.LoadAll<AudioClip>("Audio/Player/Climbing");
        jumpClips = Resources.LoadAll<AudioClip>("Audio/Player/Jump");
        landClips = Resources.LoadAll<AudioClip>("Audio/Player/Land");
        vaultClips = Resources.LoadAll<AudioClip>("Audio/Player/Vaulting");
        
        slideClip = Resources.Load<AudioClip>("Audio/Player/roll");
        barGrabReleaseClip = Resources.Load<AudioClip>("Audio/Player/metal creak");
        barHangClip = Resources.Load<AudioClip>("Audio/Player/pole hanging");
        collectClip = Resources.Load<AudioClip>("Audio/Player/collect item");

        Debug.Log($"PlayerAudio Loaded: {walkClips.Length} Walk, {runClips.Length} Run, {wallrunClips.Length} Wallrun, {climbClips.Length} Climb, {jumpClips.Length} Jump, {landClips.Length} Land, {vaultClips.Length} Vault clips. Slide Clip {(slideClip != null ? "found" : "NOT found")}. Bar Grab/Release Clip {(barGrabReleaseClip != null ? "found" : "NOT found")}. Bar Hang Clip {(barHangClip != null ? "found" : "NOT found")}. Collect {(collectClip != null ? "found" : "NOT found")}.");
    }

    void Update()
    {
        if (movement == null) return;

        // Scale volumes with Master Volume * SFX Volume programmatically
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        float finalVolume = sfxVol * masterVol;

        footstepSource.volume = finalVolume * 0.8f;
        impactSource.volume = finalVolume;

        // 1. Sliding/Rolling Audio Logic (Continuous Loop + Smooth Fade)
        if (movement.isSliding)
        {
            if (!slideSource.isPlaying)
            {
                slideSource.clip = slideClip;
                slideSource.volume = finalVolume * 0.85f;
                slideSource.Play();
            }
            else
            {
                slideSource.volume = finalVolume * 0.85f;
            }
        }
        else
        {
            if (slideSource.isPlaying)
            {
                slideSource.volume = Mathf.MoveTowards(slideSource.volume, 0f, Time.deltaTime * 6f);
                if (slideSource.volume <= 0f)
                {
                    slideSource.Stop();
                }
            }
        }

        // 2. Bar Hanging Loop Volume control (Fades in/out smoothly)
        if (movement.isSwinging)
        {
            if (!barHangSource.isPlaying)
            {
                barHangSource.clip = barHangClip;
                barHangSource.volume = 0f;
                barHangSource.Play();
            }
            else
            {
                barHangSource.volume = Mathf.MoveTowards(barHangSource.volume, finalVolume * 0.65f, Time.deltaTime * 1.5f);
            }
        }
        else
        {
            if (barHangSource.isPlaying)
            {
                barHangSource.volume = Mathf.MoveTowards(barHangSource.volume, 0f, Time.deltaTime * 5f);
                if (barHangSource.volume <= 0f)
                {
                    barHangSource.Stop();
                }
            }
        }

        // 3. Footstep Timing & Climbing Logic
        if (movement.isGrounded && movement.isPhysicallyMoving && !movement.isSliding && !movement.isCrouching && !movement.isVaulting)
        {
            float speed = movement.currentMoveSpeed;
            float interval = speed > 8f ? runStepInterval : walkStepInterval;
            FootstepType type = speed > 8f ? FootstepType.Run : FootstepType.Walk;

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayFootstep(type);
            }
        }
        else if (movement.isWallRunning)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= wallrunStepInterval)
            {
                stepTimer = 0f;
                PlayFootstep(FootstepType.Wallrun);
            }
        }
        else if (movement.isClimbing)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= climbStepInterval)
            {
                stepTimer = 0f;
                PlayFootstep(FootstepType.Climb);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep(FootstepType type)
    {
        AudioClip[] clips = null;
        if (type == FootstepType.Walk) clips = walkClips;
        else if (type == FootstepType.Run) clips = runClips;
        else if (type == FootstepType.Wallrun) clips = wallrunClips;
        else if (type == FootstepType.Climb) clips = climbClips;

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.pitch = Random.Range(0.88f, 1.12f);
        float volumeScale = type == FootstepType.Wallrun ? 1.5f : Random.Range(0.85f, 1f);
        footstepSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayJump()
    {
        if (jumpClips == null || jumpClips.Length == 0) return;

        AudioClip clip = jumpClips[Random.Range(0, jumpClips.Length)];
        impactSource.pitch = Random.Range(0.94f, 1.06f);
        impactSource.PlayOneShot(clip, 0.75f);
    }

    public void PlayLand()
    {
        if (landClips == null || landClips.Length == 0) return;

        AudioClip clip = landClips[Random.Range(0, landClips.Length)];
        impactSource.pitch = Random.Range(0.9f, 1.1f);
        impactSource.PlayOneShot(clip, 0.9f);
    }

    public void PlayVault()
    {
        if (vaultClips == null || vaultClips.Length == 0) return;

        AudioClip clip = vaultClips[Random.Range(0, vaultClips.Length)];
        impactSource.pitch = Random.Range(0.95f, 1.05f);
        impactSource.PlayOneShot(clip, 0.85f);
    }

    public void PlayBarGrab()
    {
        if (barGrabReleaseClip != null && impactSource != null)
        {
            impactSource.pitch = Random.Range(0.96f, 1.04f);
            impactSource.PlayOneShot(barGrabReleaseClip, 1.0f);
        }
    }

    public void PlayCollect()
    {
        if (collectClip != null && impactSource != null)
        {
            impactSource.pitch = 1f;
            impactSource.PlayOneShot(collectClip, 0.9f);
        }
    }

    public void StopSlide()
    {
        if (slideSource != null) slideSource.Stop();
        if (barHangSource != null) barHangSource.Stop();
        if (footstepSource != null) footstepSource.Stop();
        if (impactSource != null) impactSource.Stop();
    }
}
