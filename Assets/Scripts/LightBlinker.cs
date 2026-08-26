using UnityEngine;

public class LightBlinker : MonoBehaviour
{
    [Tooltip("How long the light stays ON each blink (seconds)")]
    public float onDuration = 0.5f;
    [Tooltip("How long the light stays OFF each blink (seconds)")]
    public float offDuration = 0.5f;

    private Light _light;
    private float _timer;
    private bool _isOn = true;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light == null)
            Debug.LogWarning("LightBlinker: no Light component found on " + gameObject.name);

        _timer = onDuration;
    }

    void Update()
    {
        if (_light == null) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            _isOn = !_isOn;
            _light.enabled = _isOn;
            _timer = _isOn ? onDuration : offDuration;
        }
    }
}
