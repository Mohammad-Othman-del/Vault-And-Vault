using UnityEngine;

public class SwingingLight : MonoBehaviour
{
    [Header("Swing Settings")]
    [Tooltip("How fast the light swings back and forth.")]
    public float swingSpeed = 1.5f;
    
    [Tooltip("The maximum angle the light will swing from the center.")]
    public float swingAngle = 25f;
    
    [Tooltip("The axis around which the light swings (usually Z or X depending on your scene orientation).")]
    public Vector3 swingAxis = Vector3.forward;

    private Quaternion startRotation;

    void Start()
    {
        // Store the initial rotation so we swing relative to it
        startRotation = transform.rotation;
    }

    void Update()
    {
        // Calculate the current angle using a sine wave for smooth looping
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        
        // Apply the rotation relative to the starting rotation
        transform.rotation = startRotation * Quaternion.AngleAxis(angle, swingAxis);
    }
}
