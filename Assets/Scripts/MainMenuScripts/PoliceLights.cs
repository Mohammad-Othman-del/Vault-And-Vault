using UnityEngine;

public class PoliceLights : MonoBehaviour
{
    [Header("Lights Setup")]
    [Tooltip("Drag your Red spotlight here.")]
    public Light redLight;
    
    [Tooltip("Drag your Blue spotlight here.")]
    public Light blueLight;

    [Header("Flash Settings")]
    [Tooltip("How fast the lights switch (in seconds).")]
    public float flashInterval = 0.3f;

    private float timer;
    private bool isRedOn = true;

    void Start()
    {
        // Set the initial state: Red ON, Blue OFF
        if (redLight != null) redLight.enabled = true;
        if (blueLight != null) blueLight.enabled = false;
        
        timer = flashInterval;
    }

    void Update()
    {
        // Countdown the timer
        timer -= Time.deltaTime;
        
        // When timer hits 0, switch the lights!
        if (timer <= 0f)
        {
            isRedOn = !isRedOn;
            
            if (redLight != null) redLight.enabled = isRedOn;
            if (blueLight != null) blueLight.enabled = !isRedOn;
            
            // Reset the timer
            timer = flashInterval;
        }
    }
}
