using UnityEngine;

public class ZeroGravity : MonoBehaviour
{
    [Header("Drift")]
    public float driftAmount = 0.3f;
    public float driftSpeed = 0.4f;

    [Header("Tumble")]
    public float tumbleSpeed = 12f;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private Vector3 _tumbleAxis;

    // Per-axis offsets so X/Y/Z drift never sync up
    private float _offsetX;
    private float _offsetY;
    private float _offsetZ;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.useGravity = false;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0f;
        }

        _startPosition = transform.position;

        // Random phase offsets so each axis drifts independently
        _offsetX = Random.Range(0f, 100f);
        _offsetY = Random.Range(0f, 100f);
        _offsetZ = Random.Range(0f, 100f);

        // Random tumble axis so every object rolls differently
        _tumbleAxis = Random.onUnitSphere;
    }

    void Update()
    {
        // Smooth random drift using Perlin noise on each axis independently
        float t = Time.time * driftSpeed;
        float dx = (Mathf.PerlinNoise(t + _offsetX, 0f) - 0.5f) * 2f * driftAmount;
        float dy = (Mathf.PerlinNoise(t + _offsetY, 1f) - 0.5f) * 2f * driftAmount;
        float dz = (Mathf.PerlinNoise(t + _offsetZ, 2f) - 0.5f) * 2f * driftAmount;

        transform.position = _startPosition + new Vector3(dx, dy, dz);

        // Slow continuous tumble on the random axis
        transform.Rotate(_tumbleAxis, tumbleSpeed * Time.deltaTime, Space.World);
    }
}
