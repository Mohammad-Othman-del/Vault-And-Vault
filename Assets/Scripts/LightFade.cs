using UnityEngine;

public class LightFade : MonoBehaviour
{
    public float minIntensity = 0f;
    public float maxIntensity = 1f;
    public float fadeSpeed = 1f;

    private Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light == null)
            Debug.LogWarning("LightFade: no Light component found on " + gameObject.name);
    }

    void Update()
    {
        if (_light == null) return;

        _light.intensity = minIntensity + (Mathf.Sin(Time.time * fadeSpeed) * 0.5f + 0.5f) * (maxIntensity - minIntensity);
    }
}
