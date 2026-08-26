using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager _instance;
    public static GameAudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameAudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameAudioManager");
                    _instance = go.AddComponent<GameAudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("BGM Clips")]
    [Tooltip("Normal level ambient background music (optional).")]
    public AudioClip normalBGM;
    
    [Tooltip("Fast-paced action music played during chases.")]
    public AudioClip chaseBGM;

    [Header("UI SFX Clips")]
    [Tooltip("Sound clip played when buttons are clicked.")]
    public AudioClip clickSFX;
    
    [Tooltip("Sound clip played when buttons are hovered.")]
    public AudioClip hoverSFX;

    [Header("Fade Settings")]
    public float fadeDuration = 1.0f;

    private AudioSource normalSource;
    private AudioSource chaseSource;
    private AudioSource sfxSource;
    private AudioSource emergencySource;
    private AudioClip emergencyClip;
    
    private float maxMusicVolume = 0.8f;
    private float maxSFXVolume = 0.8f;
    private bool isChasing = false;

    // Temporary variables for settings screen live preview
    private float tempMasterVolume = 0.8f;
    private float tempMusicVolume = 0.8f;
    private float tempSFXVolume = 0.8f;

    void Awake()
    {
        // Singleton pattern setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        DontDestroyOnLoad(gameObject);

        // Keep global AudioListener at 1.0f. We multiply Master Volume programmatically 
        // to BGM and SFX volumes so the Hover Sound remains constant at 0.15f.
        AudioListener.volume = 1f;

        // Setup AudioSources
        normalSource = gameObject.AddComponent<AudioSource>();
        normalSource.loop = true;
        normalSource.playOnAwake = false;

        chaseSource = gameObject.AddComponent<AudioSource>();
        chaseSource.loop = true;
        chaseSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        emergencySource = gameObject.AddComponent<AudioSource>();
        emergencySource.loop = true;
        emergencySource.playOnAwake = false;
        emergencySource.spatialBlend = 0f;
        emergencyClip = Resources.Load<AudioClip>("Audio/BGM/Emergency");

        LoadVolumeSettings();

        // Dynamically load Chase BGM from Resources if not assigned in Inspector
        if (chaseBGM == null)
        {
            chaseBGM = Resources.Load<AudioClip>("Audio/BGM/Chasing Audio");
            if (chaseBGM == null)
            {
                Debug.LogWarning("GameAudioManager: Chasing Audio clip not assigned and could not be loaded from Resources/Audio/BGM/Chasing Audio.");
            }
        }

        // Dynamically load click SFX from Resources if not assigned in Inspector
        if (clickSFX == null)
        {
            clickSFX = Resources.Load<AudioClip>("Audio/UI/click");
            if (clickSFX == null)
            {
                Debug.LogWarning("GameAudioManager: Click SFX clip not assigned and could not be loaded from Resources/Audio/UI/click.");
            }
        }

        // Dynamically load hover SFX from Resources if not assigned in Inspector
        if (hoverSFX == null)
        {
            hoverSFX = Resources.Load<AudioClip>("Audio/UI/hover");
            if (hoverSFX == null)
            {
                Debug.LogWarning("GameAudioManager: Hover SFX clip not assigned and could not be loaded from Resources/Audio/UI/hover.");
            }
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically stop the chase music & emergency alarm when loading a new level or screen
        StopChaseBGM();
        StopEmergencyAlarm();
        LoadVolumeSettings();
        PlayBGMForScene(scene.name);

        // Ending Scene: Auto-attach EndingSceneManager if not placed in scene
        if (scene.name == "Ending")
        {
            if (FindAnyObjectByType<EndingSceneManager>() == null)
            {
                GameObject endingGo = new GameObject("EndingSceneManager");
                endingGo.AddComponent<EndingSceneManager>();
            }
        }
    }

    private void PlayBGMForScene(string sceneName)
    {
        AudioClip targetClip = null;

        if (sceneName == "Main Menu" || sceneName == "Settings" || sceneName == "Loading")
        {
            targetClip = Resources.Load<AudioClip>("Audio/BGM/main menu BGM");
        }
        else if (sceneName == "Level 1")
        {
            targetClip = Resources.Load<AudioClip>("Audio/BGM/level 1 BGM");
        }
        else if (sceneName == "Level 2")
        {
            targetClip = Resources.Load<AudioClip>("Audio/BGM/level 2 BGM");
        }
        else if (sceneName == "Level 3")
        {
            // Level 3 fallback: play Level 2 BGM
            targetClip = Resources.Load<AudioClip>("Audio/BGM/level 2 BGM");
        }

        if (targetClip != null)
        {
            // If the track is different or not playing, transition to it
            if (normalSource.clip != targetClip || !normalSource.isPlaying)
            {
                normalBGM = targetClip;
                normalSource.clip = targetClip;
                normalSource.volume = maxMusicVolume;
                normalSource.Play();
            }
        }
        else
        {
            normalSource.Stop();
            normalSource.clip = null;
            normalBGM = null;
        }
    }

    void Start()
    {
        // If nothing is playing yet, play for the active scene
        if (normalSource.clip == null)
        {
            PlayBGMForScene(SceneManager.GetActiveScene().name);
        }

        if (chaseBGM != null)
        {
            chaseSource.clip = chaseBGM;
            chaseSource.volume = 0f;
            // Pre-warm: Play then immediately pause so Unity decompresses the audio data
            // into memory now, preventing a lag spike when StartChaseBGM() is called later.
            chaseSource.Play();
            chaseSource.Pause();
        }
    }

    /// <summary>
    /// Loads the Master, Music, and SFX volumes from PlayerPrefs to apply baseline programmatically.
    /// </summary>
    public void LoadVolumeSettings()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        // Keep local copies of values
        tempMasterVolume = masterVol;
        tempMusicVolume = musicVol;
        tempSFXVolume = sfxVol;

        // Multiply the volumes programmatically
        maxMusicVolume = musicVol * masterVol;
        maxSFXVolume = sfxVol * masterVol;
        
        // Apply instantly if not fading BGM
        if (!isChasing)
        {
            if (normalSource != null) normalSource.volume = maxMusicVolume;
            if (chaseSource != null) chaseSource.volume = 0f;
        }
        else
        {
            if (normalSource != null) normalSource.volume = 0f;
            if (chaseSource != null) chaseSource.volume = maxMusicVolume;
        }
    }

    /// <summary>
    /// Instantly sets a temporary master volume for live audio preview in the settings screen.
    /// </summary>
    public void SetTemporaryMasterVolume(float volume)
    {
        tempMasterVolume = volume;
        UpdateTemporaryVolumes();
    }

    /// <summary>
    /// Instantly sets a temporary music volume for live audio preview in the settings screen.
    /// </summary>
    public void SetTemporaryMusicVolume(float volume)
    {
        tempMusicVolume = volume;
        UpdateTemporaryVolumes();
    }

    /// <summary>
    /// Instantly sets a temporary SFX volume for live audio preview in the settings screen.
    /// </summary>
    public void SetTemporarySFXVolume(float volume)
    {
        tempSFXVolume = volume;
        UpdateTemporaryVolumes();
    }

    private void UpdateTemporaryVolumes()
    {
        maxMusicVolume = tempMusicVolume * tempMasterVolume;
        maxSFXVolume = tempSFXVolume * tempMasterVolume;
        
        if (!isChasing)
        {
            if (normalSource != null) normalSource.volume = maxMusicVolume;
            if (chaseSource != null) chaseSource.volume = 0f;
        }
        else
        {
            if (normalSource != null) normalSource.volume = 0f;
            if (chaseSource != null) chaseSource.volume = maxMusicVolume;
        }
    }

    /// <summary>
    /// Smoothly transitions from normal ambient music to high-energy chase music.
    /// </summary>
    public void StartChaseBGM()
    {
        if (isChasing) return;
        isChasing = true;

        if (chaseBGM != null && chaseSource.clip != chaseBGM)
        {
            chaseSource.clip = chaseBGM;
        }

        StopAllCoroutines();
        StartCoroutine(FadeBGM(normalSource, chaseSource, maxMusicVolume));
    }

    /// <summary>
    /// Smoothly transitions back to normal ambient music (stops chase music).
    /// </summary>
    public void StopChaseBGM()
    {
        if (!isChasing) return;
        isChasing = false;

        StopAllCoroutines();
        StartCoroutine(FadeBGM(chaseSource, normalSource, maxMusicVolume));
    }

    /// <summary>
    /// Fully stops all background music.
    /// </summary>
    public void StopAllBGM()
    {
        StopAllCoroutines();
        if (normalSource != null) normalSource.Stop();
        if (chaseSource != null) chaseSource.Stop();
        isChasing = false;
    }

    /// <summary>
    /// Play a specific normal level BGM track (e.g. when loading a new level).
    /// </summary>
    public void PlayNormalBGM(AudioClip clip)
    {
        StopAllCoroutines();
        
        normalBGM = clip;
        normalSource.clip = clip;
        normalSource.volume = maxMusicVolume;
        
        if (clip != null)
        {
            normalSource.Play();
        }
        else
        {
            normalSource.Stop();
        }

        chaseSource.Stop();
        isChasing = false;
    }

    private IEnumerator FadeBGM(AudioSource activeSource, AudioSource targetSource, float targetMaxVolume)
    {
        // Ensure target is playing (handles both fresh and pre-warmed/paused sources)
        if (targetSource.clip != null && !targetSource.isPlaying)
        {
            targetSource.volume = 0f;
            targetSource.time = 0f; // Reset playback position
            targetSource.Play();
        }

        float elapsed = 0f;
        float startActiveVol = activeSource.volume;
        float startTargetVol = targetSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled delta time in case game time is paused
            float t = elapsed / fadeDuration;

            if (activeSource.clip != null)
            {
                activeSource.volume = Mathf.Lerp(startActiveVol, 0f, t);
            }
            if (targetSource.clip != null)
            {
                targetSource.volume = Mathf.Lerp(startTargetVol, targetMaxVolume, t);
            }

            yield return null;
        }

        // Hard set final volumes
        if (activeSource.clip != null)
        {
            activeSource.volume = 0f;
            activeSource.Stop();
        }
        if (targetSource.clip != null)
        {
            targetSource.volume = targetMaxVolume;
        }
    }

    /// <summary>
    /// Plays the button click SFX using the current SFX Volume settings.
    /// Scales it down to a low, pleasant maximum volume range (0% to 12%).
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clickSFX, maxSFXVolume * 0.12f);
        }
    }

    /// <summary>
    /// Plays the button hover SFX at a crisp, clear volume scaled with SFX Volume.
    /// </summary>
    public void PlayHoverSound()
    {
        if (hoverSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(hoverSFX, maxSFXVolume * 0.25f);
        }
    }

    void Update()
    {
        // Dynamically scale Emergency alarm volume (Level 3 only) at balanced background volume
        if (emergencySource != null && emergencySource.isPlaying)
        {
            if (Time.timeScale == 0f || PauseMenuManager.isPaused)
            {
                emergencySource.volume = 0f;
            }
            else
            {
                emergencySource.volume = maxSFXVolume * 0.45f; // Clear, audible background alarm volume
            }
        }
    }

    /// <summary>
    /// Starts background emergency alarm loop in Level 3 after collecting data shard.
    /// </summary>
    public void StartEmergencyAlarm()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Equals("Level 3", System.StringComparison.OrdinalIgnoreCase)) return;

        if (emergencyClip == null) emergencyClip = Resources.Load<AudioClip>("Audio/BGM/Emergency");
        if (emergencyClip != null && emergencySource != null)
        {
            emergencySource.clip = emergencyClip;
            emergencySource.volume = maxSFXVolume * 0.45f; // Clear, audible background volume
            if (!emergencySource.isPlaying) emergencySource.Play();
        }
    }

    /// <summary>
    /// Stops the Emergency alarm sound.
    /// </summary>
    public void StopEmergencyAlarm()
    {
        if (emergencySource != null && emergencySource.isPlaying)
        {
            emergencySource.Stop();
        }
    }

    /// <summary>
    /// Plays the level complete congrats SFX, stopping normal, chase, and emergency audio loops.
    /// </summary>
    public void PlayVictorySFX()
    {
        StopChaseBGM();
        StopEmergencyAlarm();
        if (normalSource != null) normalSource.Stop();
        
        AudioClip victoryClip = Resources.Load<AudioClip>("Audio/Player/level congrats");
        if (victoryClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(victoryClip, maxSFXVolume);
        }
    }

    /// <summary>
    /// Plays the item collect SFX (data fragment pickup).
    /// </summary>
    public void PlayCollectSFX()
    {
        AudioClip collectClip = Resources.Load<AudioClip>("Audio/Player/collect item");
        if (collectClip == null) collectClip = Resources.Load<AudioClip>("Audio/UI/collect item");
        if (collectClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(collectClip, maxSFXVolume * 0.9f);
        }
    }

    /// <summary>
    /// Plays the loss SFX (caught by AI) at a lower volume, stopping normal and chase music loop.
    /// </summary>
    public void PlayDeathSFX()
    {
        StopChaseBGM();
        
        AudioClip deathClip = Resources.Load<AudioClip>("Audio/Player/caught");
        if (deathClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(deathClip, maxSFXVolume * 0.35f); // Lower pleasant volume
        }
    }

    /// <summary>
    /// Plays the fell off map death SFX (fell off.mp3).
    /// </summary>
    public void PlayFallDeathSFX()
    {
        StopChaseBGM();

        AudioClip fallClip = Resources.Load<AudioClip>("Audio/Player/fell off");
        if (fallClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(fallClip, maxSFXVolume);
        }
    }
}
